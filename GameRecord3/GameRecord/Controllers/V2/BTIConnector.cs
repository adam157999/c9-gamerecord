using GameRecord.Gamess.BTI;
using GameRecord.Gamess.BTI.Models;
using GameRecord.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Web;
using vm = GameRecord.Controllers.GameAPIController;

namespace GameRecord.Controllers.V2
{
    public class BTIConnector : BaseConnector
    {
        private string gameID = "BTI";
        private string apiUrl = "";
        private string gameUrl = "";
        private string agentUserName = "";
        private string agentPassword = "";
        private string merchantCode = "";
        private int timeout = 30;
        private GameMapping gameMap;
        private BTI api = null;

        public BTIConnector(string playerID = "", string currencyID = "", string ipAddr = "", GameEnvironment environment = GameEnvironment.PROD) : base(playerID, currencyID, ipAddr, environment)
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
                    apiUrl = jObj["BTIApiUrl"].ToString();
                    gameUrl = jObj["BTIGameUrl"].ToString();
                    agentUserName = jObj["BTIAgentUserName"].ToString();
                    agentPassword = jObj["BTIAgentPassword"].ToString();
                    merchantCode = jObj["BTIMerchantCode"].ToString();
                    timeout = int.Parse(jObj["BTITimeOutSec"].ToString());

                    api = new BTI(apiUrl, agentUserName, agentPassword, timeout);
                }
            }
        }

        public override vm.CheckAndCreateAccountVM CreatePlayer(string loginID, string loginPass, string fullName, string mcUID)
        {
            var res = new vm.CheckAndCreateAccountVM();

            try
            {
                var createUserRequest = new CreateUserNewRequest
                {
                    MerchantCustomerCode = loginID,
                    LoginName = loginID,
                    CurrencyCode = gameMap == null ? "MYR" : gameMap.Currency,
                    CountryCode = "MY",
                    City = "KL",
                    FirstName = string.IsNullOrEmpty(fullName) ? loginID : fullName,
                    LastName = string.IsNullOrEmpty(fullName) ? loginID : fullName,
                    Group1ID = 1
                };
                InsertLog(gameID, "CreateUser - Start", "request", JsonConvert.SerializeObject(createUserRequest), "response", "");

                if (api.CreateUserNew(createUserRequest))
                {
                    InsertLog(gameID, "CreateUser - Success", "request", JsonConvert.SerializeObject(createUserRequest), "response", JsonConvert.SerializeObject(api.CreateUserNewResponse));

                    InsertGameMapping(gameID, loginID, loginPass, mcUID);

                    res.PlayerID = playerID;
                    res.GamePlayerID = loginID;
                    res.Game = gameID;
                    res.Message = "Success";
                    res.Code = 0;
                }
                else
                {
                    InsertLog(gameID, "CreateUser - Failed", "request", JsonConvert.SerializeObject(createUserRequest), "response", JsonConvert.SerializeObject(api.ErrorMessage));

                    res.PlayerID = playerID;
                    res.GamePlayerID = null;
                    res.Game = gameID;
                    res.Message = api.ErrorMessage;
                    res.Code = 2;
                }
            }
            catch (Exception ex)
            {
                _common.LogError("CreateUser - Error" + " " + DateTime.Now.ToString() + " " + string.Format("{0}", playerID) + " " + ex.Message.ToString(), gameID);

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
            var depositRequest = new DepositRequest
            {
                MerchantCustomerCode = gameMap.GamePlayerID,
                Amount = amount,
                RefTransactionCode = transactionID
            };

            InsertLog(gameID, "Deposit - Start", "request", JsonConvert.SerializeObject(depositRequest), "response", "");

            if (api.Deposit(depositRequest))
            {
                InsertLog(gameID, "Deposit - Success", "request", JsonConvert.SerializeObject(depositRequest), "response", JsonConvert.SerializeObject(api.DepositResponse));
                return true;
            }
            else
            {
                InsertLog(gameID, "Deposit - Failed", "request", JsonConvert.SerializeObject(depositRequest), "response", JsonConvert.SerializeObject(api.ErrorMessage));
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
            var withdrawRequest = new WithdrawRequest
            {
                MerchantCustomerCode = gameMap.GamePlayerID,
                Amount = amount,
                RefTransactionCode = transactionID
            };
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
                var getBalanceRequest = new GetBalanceRequest
                {
                    MerchantCustomerCode = gameMap.GamePlayerID
                };
                if (api.GetBalance(getBalanceRequest))
                {
                    InsertLog(gameID, "GetBalance", "request", JsonConvert.SerializeObject(getBalanceRequest), "response", JsonConvert.SerializeObject(api.GetBalanceResponse));
                    res.PlayerID = playerID;
                    res.Balance = api.GetBalanceResponse.Balance;
                    res.Game = gameID;
                    res.Message = "Successfully retrieved balance.";
                    res.Code = 0;
                }
                else
                {
                    InsertLog(gameID, "GetBalance", "request", JsonConvert.SerializeObject(getBalanceRequest), "response", JsonConvert.SerializeObject(api.ErrorMessage));
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
            var getBalanceRequest = new GetBalanceRequest
            {
                MerchantCustomerCode = gameMap.GamePlayerID
            };
            if (api.GetBalance(getBalanceRequest))
            {
                InsertLog(gameID, "GetGameBalance", "request", JsonConvert.SerializeObject(getBalanceRequest), "response", JsonConvert.SerializeObject(api.GetBalanceResponse));
                return (decimal)api.GetBalanceResponse.Balance;
            }
            else
            {
                return null;
            }
        }

        public vm.GetGameUrlVM GetBTIGameUrl(string langId, string gameId, bool isMobile = false)
        {
            ipAddr = Utils.GetClientIp();
            var res = new vm.GetGameUrlVM();

            try
            {
                var authTokenRequest = new GetCustomerAuthTokenRequest
                {
                    MerchantCustomerCode = gameMap.GamePlayerID
                };

                if (api.GetCustomerAuthToken(authTokenRequest))
                {
                    InsertLog(gameID, "GetGameUrl", "request", JsonConvert.SerializeObject(authTokenRequest), "response", JsonConvert.SerializeObject(api.GetCustomerAuthTokenResponse));

                    var launchUrl = $"{gameUrl}/{langId}/?operatorToken={api.GetCustomerAuthTokenResponse.AuthToken}";
                    res.PlayerID = playerID;
                    res.Url = launchUrl;
                    res.Message = "Success";
                    res.Code = 0;
                }
                else
                {
                    InsertLog(gameID, "GetGameUrl", "request", JsonConvert.SerializeObject(authTokenRequest), "response", JsonConvert.SerializeObject(api.ErrorMessage));
                    res.PlayerID = playerID;
                    res.Url = null;
                    res.Message = api.ErrorMessage;
                    res.Code = 2;
                }
            }
            catch (Exception ex)
            {
                _common.LogError("GetGameUrl" + " " + DateTime.Now.ToString() + " " + string.Format("{0}", playerID) + " " + ex.Message.ToString(), gameID);
                var err = new Error
                {
                    ErrorCode = "999",
                    ErrorDesc = ex.Message.ToString()
                };
                InsertLog(gameID, "GetGameUrl", "Error", JsonConvert.SerializeObject(err));
                res.Url = null;
                res.Message = ex.Message.ToString();
                res.Code = 99;
            }

            return res;
        }

        public override vm.GetGameUrlVM GetGameUrl(string langId, bool isMobile = false)
        {
            return GetBTIGameUrl(langId, "", isMobile);
        }

        public override bool KickPlayer(string gamePlayerId, ref Error error)
        {
            error = new Error { ErrorCode = "999", ErrorDesc = "KickPlayer not implemented for BTI." };
            return false;
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
