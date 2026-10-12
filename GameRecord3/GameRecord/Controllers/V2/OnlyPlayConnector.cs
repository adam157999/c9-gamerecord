using GameRecord.Gamess.OnlyPlay;
using GameRecord.Gamess.OnlyPlay.Models;
using GameRecord.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using vm = GameRecord.Controllers.GameAPIController;

namespace GameRecord.Controllers
{
    public class OnlyPlayConnector : BaseConnector
    {
        private string gameID = "ONLYPLAY";
        private string apiUrl = "";
        private string operatorName = "";
        private string secretKey = "";
        private int timeout = 1;
        private GameMapping gameMap;
        private OnlyPlay api = null;
        private const string product = "ONLYPLAY";
        private string lobbyUrl = HttpContext.Current.Request.Url.AbsoluteUri.Replace(HttpContext.Current.Request.Url.PathAndQuery, "/") + "OnlyPlay/Lobby";


        public OnlyPlayConnector(string playerID = "", string currencyID = "", string ipAddr = "", GameEnvironment environment = GameEnvironment.PROD) : base(playerID, currencyID, ipAddr, environment)
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
                    apiUrl = jObj["OnlyPlayApiUrl"].ToString();
                    operatorName = jObj["OnlyPlayOperator"].ToString();
                    secretKey = jObj["OnlyPlaySecretKey"].ToString();
                    timeout = int.Parse(jObj["OnlyPlayTimeOutSec"].ToString());

                    api = new OnlyPlay(apiUrl, operatorName, secretKey,timeout);
                }
            }
        }

        public override vm.CheckAndCreateAccountVM CreatePlayer(string loginID, string loginPass, string fullName, string mcUID)
        {
            var res = new vm.CheckAndCreateAccountVM();

            try
            {
                var createAccountRequest = new CreateAccountRequest
                {
                    Operator = operatorName,
                    Product = product,
                    Username = loginID,
                    Password = loginPass,
                    Language = "en-US"
                };

                if (api.CreateAccount(createAccountRequest))
                {
                    InsertLog(gameID, "CreatePlayer", "request", JsonConvert.SerializeObject(createAccountRequest), "response", JsonConvert.SerializeObject(api.CreateAccountResponse));

                    InsertGameMapping(gameID, loginID, loginPass, mcUID);

                    res.PlayerID = playerID;
                    res.GamePlayerID = loginID;
                    res.Game = gameID;
                    res.Message = "Success";
                    res.Code = 0;
                }
                else
                {
                    InsertLog(gameID, "CreatePlayer", "request", JsonConvert.SerializeObject(createAccountRequest), "response", JsonConvert.SerializeObject(api.ErrorMessage));

                    res.PlayerID = playerID;
                    res.GamePlayerID = null;
                    res.Game = gameID;
                    res.Message = api.ErrorMessage;
                    res.Code = 2;
                }
            }
            catch (Exception ex)
            {
                _common.LogError("CreatePlayer" + " " + DateTime.Now.ToString() + " " + string.Format("{0}", playerID) + " " + ex.Message.ToString(), gameID);

                var err = new
                {
                    ErrorCode = "999",
                    ErrorDesc = ex.Message.ToString()
                };

                InsertLog(gameID, "CreatePlayer", "Error", JsonConvert.SerializeObject(err));

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
                Operator = operatorName,
                Product = product,
                Username = gameMap.GamePlayerID,
                Amount = amount,
                TransactionID = transactionID
            };

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
            var withdrawalRequest = new WithdrawalRequest
            {
                Operator = operatorName,
                Product = product,
                Username = gameMap.GamePlayerID,
                Amount = amount,
                TransactionID = transactionID
            };

            if (api.Withdraw(withdrawalRequest))
            {
                InsertLog(gameID, "Withdraw", "request", JsonConvert.SerializeObject(withdrawalRequest), "response", JsonConvert.SerializeObject(api.WithdrawalResponse));
                return true;
            }
            else
            {
                InsertLog(gameID, "Withdraw", "request", JsonConvert.SerializeObject(withdrawalRequest), "response", JsonConvert.SerializeObject(api.ErrorMessage));
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
                var getBalanceRequest = new Gamess.OnlyPlay.Models.GetBalanceRequest
                {
                    Operator = operatorName,
                    Product = product,
                    Username = gameMap.GamePlayerID
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
            var getBalanceRequest = new Gamess.OnlyPlay.Models.GetBalanceRequest
            {
                Operator = operatorName,
                Product = product,
                Username = gameMap.GamePlayerID
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

        public vm.GetGameUrlVM GetOnlyPlayGameUrl(string langId,string gameId, bool isMobile = false)
        {
            var startGameRequest = new Gamess.OnlyPlay.Models.StartGameRequest
            {
                Operator = operatorName,
                Product = product,
                Username = gameMap.GamePlayerID,
                Category = "EGAMES",
                Game = gameId,
                IsMobile = isMobile
            };

            var res = new vm.GetGameUrlVM();

            if (api.StartGame(startGameRequest))
            {
                InsertLog(gameID, "StartGame", "request", JsonConvert.SerializeObject(startGameRequest), "response", JsonConvert.SerializeObject(api.StartGameResponse));

                res.PlayerID = playerID;
                res.Url = api.StartGameResponse.Url;
                res.Game = gameID;
                res.Message = "Success";
                res.Code = 0;
            }
            else
            {
                InsertLog(gameID, "StartGame", "request", JsonConvert.SerializeObject(startGameRequest), "response", JsonConvert.SerializeObject(api.ErrorMessage));

                res.PlayerID = playerID;
                res.Url = null;
                res.Game = gameID;
                res.Message = api.ErrorMessage;
                res.Code = 2;
            }

            return res;
        }

        public override vm.GetGameUrlVM GetGameUrl(string langId, bool IsMobile = false)
        {
            var res = new vm.GetGameUrlVM();
            try
            {
                if (gameMap != null)
                {
                    string url = string.Format("{0}?playerid={1}&currency={2}&ip={3}&lang={4}&mobile={5}", lobbyUrl, playerID, "MYR", Utils.GetClientIp(), langId, IsMobile.ToString().ToLower());
                    InsertLog(gameID, "GetGameUrl", "playerid", playerID, "currency", "MYR", "ip", Utils.GetClientIp(), "lang", langId, "url", url);
                    res.PlayerID = playerID;
                    res.Url = url;
                    res.Game = gameID;
                    res.Message = "Success";
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
            }
            catch (Exception ex)
            {
                _common.LogError("GetGameUrl" + " " + DateTime.Now.ToString() + " " + string.Format("{0}", playerID) + " " + ex.Message.ToString(), gameID);
                var err = new Error { ErrorCode = "999", ErrorDesc = ex.Message.ToString() };
                InsertLog(gameID, "Login", "Error", JsonConvert.SerializeObject(err));
                res.PlayerID = playerID;
                res.Url = null;
                res.Game = gameID;
                res.Message = ex.Message.ToString();
                res.Code = 99;
            }
            return res;
        }

        public override bool KickPlayer(string gamePlayerId, ref Error error)
        {
            throw new NotImplementedException();
        }

        public override string RetryDeposit(decimal amount, string trackerId, ref Error error)
        {
            throw new NotImplementedException();
        }

        public override string RetryWithdraw(decimal amount, string trackerId, ref Error error)
        {
            throw new NotImplementedException();
        }

        public List<OnlyPlayLobbyGame> GetGameList()
        {
            api = new OnlyPlay(apiUrl, operatorName, secretKey, timeout);

            var getEGameRequest = new GetEgamesListRequest
            {
                Operator = operatorName,
                Product = product
            };

            var apiCall = api.GetEgamesList(getEGameRequest);

            if (apiCall && api.GetEgamesListResponse != null && api.GetEgamesListResponse.GameList != null)
            {
                InsertLog(gameID, "GetEgamesList", "request", JsonConvert.SerializeObject(getEGameRequest), "response", JsonConvert.SerializeObject(api.GetEgamesListResponse));

                var data = api.GetEgamesListResponse.GameList.Game
                    .Select(x =>
                        new OnlyPlayLobbyGame
                        {
                            AllSeq = 1,
                            GameCode = x.Code.Text,
                            ChineseName = x.ChineseName,
                            EnglishName = x.EnglishName,
                            HTML5 = x.IsHtml5,
                            ImageName = x.Image,
                            IsAll = true
                        })
                    .ToList();

                return data;
            }
            else
            {
                return new List<OnlyPlayLobbyGame>();
            }
        }
    }
}