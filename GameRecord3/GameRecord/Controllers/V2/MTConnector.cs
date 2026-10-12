using GameRecord.Gamess.MT;
using GameRecord.Gamess.MT.Models; // Added to resolve missing types
using GameRecord.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Database;
using RestSharp; // Add RestSharp for HTTP requests
using System;
using System.Linq;
using System.Web.Util;
using vm = GameRecord.Controllers.GameAPIController;

namespace GameRecord.Controllers
{
    public class MTConnector : BaseConnector
    {
        private string gameID = "MT";
        private string apiUrl = "";
        private string clientId = "";
        private string clientSecret = "";
        private string desKey = "";
        private string desIV = "";
        private string systemCode = "";
        private string webId = "";
        private int timeout = 1;
        private GameMapping gameMap;
        private MT api = null;

        public MTConnector(string playerID = "", string currencyID = "", string ipAddr = "", GameEnvironment environment = GameEnvironment.PROD) : base(playerID, currencyID, ipAddr, environment)
        {
            using (BingoConn db = new BingoConn())
            {
                gameMap = db.GameMappings.Where(x => x.Game.ToUpper().Trim() == gameID &&
                                                     x.PlayerID == playerID &&
                                                     x.Currency.ToUpper() == currencyID &&
                                                     x.IsActive == true).FirstOrDefault();

                var apiConfig = CatchData.GetApiKeys(gameID.ToUpper().Trim(), environment.ToString(), currencyID.ToUpper().Trim());
                if (string.IsNullOrEmpty(currencyID)) apiConfig = CatchData.GetApiKeys(gameID.ToUpper().Trim(), environment.ToString());

                if (apiConfig != null)
                {
                    JObject jObj = JObject.Parse(apiConfig.ApiConfig);
                    apiUrl = jObj["MTApiUrl"].ToString();
                    clientId = jObj["MTClientId"].ToString();
                    clientSecret = jObj["MTClientSecret"].ToString();
                    timeout = int.Parse(jObj["MTTimeOutSec"].ToString());
                    desKey = jObj["MTDesKey"].ToString();
                    desIV = jObj["MTDesIV"].ToString();
                    systemCode = jObj["MTSystemCode"].ToString();
                    webId = jObj["MTWebId"].ToString();
                    api = new MT(apiUrl, clientId, clientSecret, desKey, desIV, systemCode, webId, timeout);
                }
            }
        }

        public override vm.CheckAndCreateAccountVM CreatePlayer(string loginID, string loginPass, string fullName, string mcUID)
        {
            var res = new vm.CheckAndCreateAccountVM();

            try
            {
                var createUserRequest = new CreateUserRequest(webId, systemCode, loginID, loginID, currencyID);

                if (api.CreateUser(createUserRequest))
                {
                    InsertLog(gameID, "CreateUser", "request", JsonConvert.SerializeObject(createUserRequest), "response", JsonConvert.SerializeObject(api.CreateUserResponse));

                    InsertGameMapping(gameID, loginID, loginPass, mcUID);

                    res.PlayerID = playerID;
                    res.GamePlayerID = loginID;
                    res.Game = gameID;
                    res.Message = "Success";
                    res.Code = 0;
                }
                else
                {
                    InsertLog(gameID, "CreateUser", "request", JsonConvert.SerializeObject(createUserRequest), "response", JsonConvert.SerializeObject(api.ErrorMessage));

                    res.PlayerID = playerID;
                    res.GamePlayerID = null;
                    res.Game = gameID;
                    res.Message = api.ErrorMessage;
                    res.Code = 2;
                }
            }
            catch (Exception ex)
            {
                _common.LogError("CreateUser" + " " + DateTime.Now.ToString() + " " + string.Format("{0}", playerID) + " " + ex.Message.ToString(), gameID);

                var err = new
                {
                    ErrorCode = "999",
                    ErrorDesc = ex.Message.ToString()
                };

                InsertLog(gameID, "CreateUser", "Error", JsonConvert.SerializeObject(err));

                res.PlayerID = playerID;
                res.GamePlayerID = loginID;
                res.Game = gameID;
                res.Message = ex.Message.ToString();
                res.Code = 99;
            }

            return res;
        }

        public override bool GameDeposit(decimal amount, string transactionID, ref Error error)
        {
            var depositRequest = new DepositRequest(webId, systemCode, amount, transactionID, gameMap.GamePlayerID);

            if (api.Deposit(depositRequest))
            {
                InsertLog(gameID, "Deposit", "request", JsonConvert.SerializeObject(depositRequest), "response", JsonConvert.SerializeObject(api.DepositResponse));
                return true;
            }
            else
            {
                InsertLog(gameID, "Deposit", "request", JsonConvert.SerializeObject(depositRequest), "response", JsonConvert.SerializeObject(api.ErrorMessage));
                error = new Error
                {
                    ErrorCode = "-9999",
                    ErrorDesc = api.ErrorMessage
                };
                return false;
            }
        }

        public override bool GameWithdraw(decimal amount, string transactionID, ref Error error)
        {
            var withdrawRequest = new WithdrawRequest(webId, systemCode, amount, transactionID, gameMap.GamePlayerID);

            if (api.Withdraw(withdrawRequest))
            {
                InsertLog(gameID, "Withdraw", "request", JsonConvert.SerializeObject(withdrawRequest), "response", JsonConvert.SerializeObject(api.WithdrawResponse));
                return true;
            }
            else
            {
                InsertLog(gameID, "Withdraw", "request", JsonConvert.SerializeObject(withdrawRequest), "response", JsonConvert.SerializeObject(api.ErrorMessage));
                error = new Error
                {
                    ErrorCode = "-9999",
                    ErrorDesc = api.ErrorMessage
                };
                return false;
            }
        }

        public override vm.GetBalanceVM GetBalance()
        {
            var res = new vm.GetBalanceVM();

            try
            {
               var balanceRequest = new GetBalanceRequest(webId, systemCode, gameMap.GamePlayerID);

                if (api.GetBalance(balanceRequest))
                {
                    InsertLog(gameID, "GetBalance", "request", JsonConvert.SerializeObject(balanceRequest), "response", JsonConvert.SerializeObject(api.GetBalanceResponse));

                    res.PlayerID = playerID;
                    res.Balance = (decimal)api.GetBalanceResponse.Data.balance;
                    res.Game = gameID;
                    res.Message = "Successfully retrieved balance.";
                    res.Code = 0;
                }
                else
                {
                    InsertLog(gameID, "GetBalance", "request", JsonConvert.SerializeObject(balanceRequest), "response", JsonConvert.SerializeObject(api.ErrorMessage));
                    res.PlayerID = playerID;
                    res.Balance = null;
                    res.Game = gameID;
                    res.Message = api.ErrorMessage;
                    res.Code = 2;
                }
            }
            catch (Exception ex)
            {
                _common.LogError("GetBalance" + " " + DateTime.Now.ToString() + " " + string.Format("{0}", playerID) + " " + ex.Message.ToString(), gameID);

                var err = new Error
                {
                    ErrorCode = "999",
                    ErrorDesc = ex.Message.ToString()
                };

                InsertLog(gameID, "GetBalance", "Error", JsonConvert.SerializeObject(err));

                res.PlayerID = playerID;
                res.Balance = null;
                res.Game = gameID;
                res.Message = ex.Message.ToString();
                res.Code = 99;
            }

            return res;
        }

        public override decimal? GetGameBalance()
        {
            var balanceRequest = new GetBalanceRequest(webId, systemCode, gameMap.GamePlayerID);

            if (api.GetBalance(balanceRequest))
            {
                InsertLog(gameID, "GetGameBalance", "request", JsonConvert.SerializeObject(balanceRequest), "response", JsonConvert.SerializeObject(api.GetBalanceResponse));
                return (decimal)api.GetBalanceResponse.Data.balance;
            }
            else
            {
                return null;
            }
        }

        public override vm.GetGameUrlVM GetGameUrl(string langId, bool isMobile = false)
        {
            var getGameURLTokenRequest = new GetURLTokenRequest(webId, systemCode, gameMap.GamePlayerID, langId);
            var res = new vm.GetGameUrlVM();
            if (api.GetGameURLToken(getGameURLTokenRequest))
            {
                InsertLog(gameID, "GetGameURLToken", "request", JsonConvert.SerializeObject(getGameURLTokenRequest), "response", JsonConvert.SerializeObject(api.GetURLTokenResponse));
                res.PlayerID = playerID;
                res.Url = api.GetURLTokenResponse.Data.url;
                res.Code = 0;
            }
            else
            {
                res.PlayerID = playerID;
                res.Url = null;
                res.Game = gameID;
                res.Message = "Game account not found.";
                res.Code = 1;
            }

            return res;
        }

        public override bool KickPlayer(string gamePlayerId, ref Error error)
        {
            var kickPlayerRequest = new kickOutRequest(webId, systemCode, gameMap.GamePlayerID);

            if (api.KickOutPlayer(kickPlayerRequest))
            {
                InsertLog(gameID, "KickPlayer", "request", JsonConvert.SerializeObject(kickPlayerRequest), "response", JsonConvert.SerializeObject(api.KickOutResponse));
                return true;
            }
            else
            {
                InsertLog(gameID, "KickPlayer", "request", JsonConvert.SerializeObject(kickPlayerRequest), "response", JsonConvert.SerializeObject(api.KickOutResponse));
                error = new Error
                {
                    ErrorCode = "-9999",
                    ErrorDesc = api.ErrorMessage
                };
                return false;
            }
        }

        public override string RetryDeposit(decimal amount, string trackerId, ref Error error)
        {
            throw new NotImplementedException();
        }

        public override string RetryWithdraw(decimal amount, string trackerId, ref Error error)
        {
            throw new NotImplementedException();
        }
    }
}