using GameRecord.Gamess.BTG;
using GameRecord.Gamess.BTG.Models;
using GameRecord.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Web;
using vm = GameRecord.Controllers.GameAPIController;

namespace GameRecord.Controllers.V2
{
    public class BTGConnector : BaseConnector
    {
        private string gameID = "BTG";
        private string apiUrl = "";
        private string accountId = "";
        private string securityCode = "";
        private int timeout = 30;
        private GameMapping gameMap;
        private BTG api = null;

        public BTGConnector(string playerID = "", string currencyID = "", string ipAddr = "", GameEnvironment environment = GameEnvironment.PROD) : base(playerID, currencyID, ipAddr, environment)
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
                    apiUrl = jObj["BTGApiUrl"].ToString();
                    accountId = jObj["BTGAccountId"].ToString();
                    securityCode = jObj["BTGSecurityCode"].ToString();
                    timeout = int.Parse(jObj["BTGTimeOutSec"].ToString());

                    api = new BTG(apiUrl, accountId, securityCode, timeout);
                }
            }
        }

        public override vm.CheckAndCreateAccountVM CreatePlayer(string loginID, string loginPass, string fullName, string mcUID)
        {
            var res = new vm.CheckAndCreateAccountVM();

            try
            {
                // BTG doesn't have a separate create user API, user is created automatically on first game launch
                // Call UserLogin to verify the account can be accessed and create the user if needed
                string defaultGameCode = "AB1001"; // Default game code for account creation/verification
                string defaultLang = "en";
                string device = "desktop";
                string homeUrl = HttpContext.Current != null ? 
                    HttpContext.Current.Request.Url.AbsoluteUri.Replace(HttpContext.Current.Request.Url.PathAndQuery, "/") : 
                    "";

                if (api.EnterLobby(loginID, defaultLang))
                {
                    InsertLog(gameID, "CreateUser_UserLogin", "username", loginID, "gameCode", defaultGameCode, "response", JsonConvert.SerializeObject(api.UserLoginResponse));
                    
                    InsertGameMapping(gameID, loginID, loginPass, mcUID);

                    res.PlayerID = playerID;
                    res.GamePlayerID = loginID;
                    res.Game = gameID;
                    res.Message = "Success";
                    res.Code = 0;
                }
                else
                {
                    InsertLog(gameID, "CreateUser_UserLogin", "username", loginID, "gameCode", defaultGameCode, "error", api.ErrorMessage);
                    
                    res.PlayerID = playerID;
                    res.GamePlayerID = loginID;
                    res.Game = gameID;
                    res.Message = "Failed to create/verify user: " + api.ErrorMessage;
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
            if (api.DepositAmount(gameMap.GamePlayerID, amount.ToString("F2"), transactionID))
            {
                InsertLog(gameID, "Deposit", "username", gameMap.GamePlayerID, "amount", amount.ToString("F2"), "transactionID", transactionID, "response", JsonConvert.SerializeObject(api.DepositAmountResponse));
                return true;
            }
            else
            {
                InsertLog(gameID, "Deposit", "username", gameMap.GamePlayerID, "amount", amount.ToString("F2"), "transactionID", transactionID, "error", api.ErrorMessage);
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
            if (api.WithdrawAmount(gameMap.GamePlayerID, amount.ToString("F2"), transactionID, true))
            {
                InsertLog(gameID, "Withdraw", "username", gameMap.GamePlayerID, "amount", amount.ToString("F2"), "transactionID", transactionID, "response", JsonConvert.SerializeObject(api.WithdrawAmountResponse));
                return true;
            }
            else
            {
                InsertLog(gameID, "Withdraw", "username", gameMap.GamePlayerID, "amount", amount.ToString("F2"), "transactionID", transactionID, "error", api.ErrorMessage);
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
                if (api.GetBalance(gameMap.GamePlayerID))
                {
                    InsertLog(gameID, "GetBalance", "username", gameMap.GamePlayerID, "response", JsonConvert.SerializeObject(api.GetBalanceResponse));
                    res.PlayerID = playerID;
                    res.Balance = api.GetBalanceResponse.Data.Balance;
                    res.Game = gameID;
                    res.Message = "Successfully retrieved balance.";
                    res.Code = 0;
                }
                else
                {
                    InsertLog(gameID, "GetBalance", "username", gameMap.GamePlayerID, "error", api.ErrorMessage);
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
            if (api.GetBalance(gameMap.GamePlayerID))
            {
                InsertLog(gameID, "GetGameBalance", "username", gameMap.GamePlayerID, "response", JsonConvert.SerializeObject(api.GetBalanceResponse));
                return api.GetBalanceResponse.Data.Balance;
            }
            else
            {
                return null;
            }
        }

        public vm.GetGameUrlVM GetBTGGameUrl(string langId, string gameCode, bool isMobile = false)
        {
            ipAddr = Utils.GetClientIp();
            string homeUrl = HttpContext.Current.Request.Url.AbsoluteUri.Replace(HttpContext.Current.Request.Url.PathAndQuery, "/");
            string device = isMobile ? "mobile" : "desktop";

            var res = new vm.GetGameUrlVM();
            if (api.UserLogin(gameMap.GamePlayerID, gameCode, langId, null, homeUrl, "world", 1, false, device))
            {
                InsertLog(gameID, "UserLogin", "username", gameMap.GamePlayerID, "gameCode", gameCode, "lang", langId, "device", device, "response", JsonConvert.SerializeObject(api.UserLoginResponse));
                res.PlayerID = playerID;
                res.Url = api.UserLoginResponse.Data.game_url;
                res.Game = gameID;
                res.Message = "Success";
                res.Code = 0;
            }
            else
            {
                InsertLog(gameID, "UserLogin", "username", gameMap.GamePlayerID, "gameCode", gameCode, "lang", langId, "device", device, "error", api.ErrorMessage);
                res.PlayerID = playerID;
                res.Url = null;
                res.Game = gameID;
                res.Message = api.ErrorMessage;
                res.Code = 2;
            }
            return res;
        }

        public override vm.GetGameUrlVM GetGameUrl(string langId, bool isMobile = false)
        {
            var res = new vm.GetGameUrlVM();
            try
            {
                if (gameMap != null)
                {
                    ipAddr = Utils.GetClientIp();
                    string homeUrl = HttpContext.Current.Request.Url.AbsoluteUri.Replace(HttpContext.Current.Request.Url.PathAndQuery, "/");

                    if (api.EnterLobby(gameMap.GamePlayerID, langId))
                    {
                        InsertLog(gameID, "EnterLobby", "username", gameMap.GamePlayerID, "lang", langId, "response", JsonConvert.SerializeObject(api.EnterLobbyResponse));
                        res.PlayerID = playerID;
                        res.Url = api.EnterLobbyResponse.Data.LobbyUrl;
                        res.Game = gameID;
                        res.Message = "Success";
                        res.Code = 0;
                    }
                    else
                    {
                        InsertLog(gameID, "EnterLobby", "username", gameMap.GamePlayerID, "lang", langId, "error", api.ErrorMessage);
                        res.PlayerID = playerID;
                        res.Url = null;
                        res.Game = gameID;
                        res.Message = api.ErrorMessage;
                        res.Code = 2;
                    }
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
                InsertLog(gameID, "GetGameUrl", "Error", JsonConvert.SerializeObject(err));
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
            try
            {
                if (api.UserLogout(gamePlayerId))
                {
                    InsertLog(gameID, "KickPlayer", "username", gamePlayerId, "response", JsonConvert.SerializeObject(api.UserLogoutResponse));
                    return true;
                }
                else
                {
                    InsertLog(gameID, "KickPlayer", "username", gamePlayerId, "error", api.ErrorMessage);
                    error = new Error
                    {
                        ErrorCode = "-9999",
                        ErrorDesc = api.ErrorMessage
                    };
                    return false;
                }
            }
            catch (Exception ex)
            {
                error = new Error { ErrorCode = "999", ErrorDesc = ex.Message.ToString() };
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

        public List<GameItem> GetGameList(string langId = "en", string gameType = "slot")
        {
            api = new BTG(apiUrl, accountId, securityCode, timeout);

            if (api.GetGameList(langId, gameType))
            {
                if (api.GetGameListResponse != null && api.GetGameListResponse.Data != null)
                {
                    InsertLog(gameID, "GetGameList", "lang", langId, "gameType", gameType, "response", JsonConvert.SerializeObject(api.GetGameListResponse));
                    return api.GetGameListResponse.Data;
                }
            }
            
            return new List<GameItem>();
        }
    }
}
