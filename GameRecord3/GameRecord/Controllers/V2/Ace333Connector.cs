using GameRecord.Gamess.Ace333;
using GameRecord.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using vm = GameRecord.Controllers.GameAPIController;

namespace GameRecord.Controllers.V2
{
    public class Ace333Connector : BaseConnector
    {
        private string GAME_ID = "ACE333";
        private string API_URL { get; set; }
        private string H5_LOGIN_API_URL { get; set; }
        private string H5_LOBBY_API_URL { get; set; }
        private string POST_FIX { get; set; }
        //private string TOKEN { get; set; }
        private string API_SERVICE_TOKEN { get; set; }
        private string SECRET_KEY { get; set; }
        private string MD5_KEY { get; set; }
        private string ENCRYPT_KEY { get; set; }
        private string DELIMETER { get; set; }
        private string CURRENCY { get; set; }
        private int timeOutSec { get; set; }
        private string ip = "";

        private GameMapping gameMap;
        private Ace333 api = null;

        public Ace333Connector(string PlayerID = "", string CurrencyID = "", string IPAddr = "", GameEnvironment Environment = GameEnvironment.PROD) : base(PlayerID, CurrencyID, IPAddr, Environment)
        {
            using (BingoConn db = new BingoConn())
            {
                gameMap = db.GameMappings.Where(x => x.Game.ToUpper().Trim() == GAME_ID.ToUpper().Trim() &&
                                                        x.PlayerID == playerID &&
                                                        x.Currency.ToUpper() == currencyID &&
                                                        x.IsActive == true).FirstOrDefault();

                var apiConfig = CatchData.GetApiKeys(GAME_ID.ToUpper().Trim(), Environment.ToString(), currencyID.ToUpper().Trim());
                if (string.IsNullOrEmpty(CurrencyID)) apiConfig = CatchData.GetApiKeys(GAME_ID.ToUpper().Trim(), Environment.ToString());

                if (apiConfig != null)
                {
                    JObject jObj = JObject.Parse(apiConfig.ApiConfig);
                    API_URL = jObj["Ace333ApiUrl"].ToString();
                    H5_LOGIN_API_URL = jObj["Ace333H5LoginApiUrl"].ToString();
                    H5_LOBBY_API_URL = jObj["Ace333H5LobbyApiUrl"].ToString();
                    POST_FIX = jObj["Ace333PostFix"].ToString();
                    API_SERVICE_TOKEN = jObj["Ace333APIServiceToken"].ToString();
                    SECRET_KEY = jObj["Ace333SecretKey"].ToString();
                    MD5_KEY = jObj["Ace333Md5Key"].ToString();
                    ENCRYPT_KEY = jObj["Ace333EncryptKey"].ToString();
                    DELIMETER = jObj["Ace333Delimeter"].ToString();
                    CURRENCY = jObj["Ace333Currency"].ToString();
                    timeOutSec = Convert.ToInt32(jObj["Ace333TimeOutSec"].ToString());

                    api = new Ace333(API_URL, H5_LOGIN_API_URL, H5_LOBBY_API_URL, POST_FIX, API_SERVICE_TOKEN, SECRET_KEY, MD5_KEY, ENCRYPT_KEY, DELIMETER, CURRENCY, timeOutSec);
                    ip = IPAddr;
                }
            }
        }

        public override GameAPIController.CheckAndCreateAccountVM CreatePlayer(string loginID, string loginPass, string fullName, string mcUID)
        {
            var res = new vm.CheckAndCreateAccountVM();
            try
            {
                if (gameMap == null)
                {

                    var apiCheck = api.CreatePlayer(mcUID, fullName);
                    InsertLog(GAME_ID, "CreatePlayer", "Request", api.Request, "Parameters", api.Parameters, "Response", api.Response, "ErrorMessage", api.ErrorMessage, "IP", ip);
                    if (apiCheck)
                    {
                        InsertGameMapping(GAME_ID, api.CreatePlayerRes.playerID.ToString(), loginPass, playerID);
                        gameMap = LoadGameMap(GAME_ID, playerID, currencyID);

                        res.PlayerID = playerID;
                        res.GamePlayerID = loginID;
                        res.Game = GAME_ID;
                        res.Message = "Success";
                        res.Code = 0;
                    }
                    else
                    {
                        res.PlayerID = playerID;
                        res.GamePlayerID = null;
                        res.Game = GAME_ID;
                        res.Message = api.ErrorMessage;
                        res.Code = 2;
                    }
                }
                else
                {
                    res.PlayerID = playerID;
                    res.GamePlayerID = gameMap.GamePlayerID;
                    res.Game = GAME_ID;
                    res.Message = "Retrieve Success";
                    res.Code = 1;
                }
            }
            catch (Exception ex)
            {
                _common.LogError("CreateUser" + " " + DateTime.Now.ToString() + " " + string.Format("{0}", playerID) + " " + ex.Message.ToString(), GAME_ID);
                var err = new { ErrorCode = "999", ErrorDesc = ex.Message.ToString() };
                InsertLog(GAME_ID, "CreateUser", "Error", JsonConvert.SerializeObject(err));

                res.PlayerID = playerID;
                res.GamePlayerID = loginID;
                res.Game = GAME_ID;
                res.Message = ex.Message.ToString();
                res.Code = 99;
            }

            return res;
        }

        public override bool GameDeposit(decimal Amount, string TrackerId, ref Error Error)
        {
            if (gameMap != null)
            {
                var apiCall = api.Credit(Convert.ToInt32(gameMap.GamePlayerID), Amount, TrackerId);
                InsertLog(GAME_ID, "Deposit", "request", api.Request, "Parameter", api.Parameters,  "response", api.Response);

                targetApiUrl = api.Request;
                request = api.Request;
                response = api.Response;

                if (apiCall)
                {
                    return true;
                }
                else
                {
                    Error = new Error
                    {
                        ErrorCode = "-9999",
                        ErrorDesc = api.ErrorMessage
                    };
                    return false;
                }
            }
            else
            {
                Error = new Error
                {
                    ErrorCode = "-9999",
                    ErrorDesc = "Retrieve Success"
                };
                return false;
            }
        }

        public override bool GameWithdraw(decimal Amount, string TrackerId, ref Error Error)
        {
            if (gameMap != null)
            {
                var apiLogout = api.GameLogout(gameMap.GamePlayerID);

                if (apiLogout)
                {
                    var apiCall = api.Withdraw(Convert.ToInt32(gameMap.GamePlayerID), Amount, TrackerId);
                    InsertLog(GAME_ID, "Withdraw", "request", api.Request, "parameter", api.Parameters, "response", api.Response);

                    targetApiUrl = api.Request;
                    request = api.Request;
                    response = api.Response;

                    if (apiCall)
                    {
                        return true;
                    }
                    else
                    {
                        Error = new Error
                        {
                            ErrorCode = "-9999",
                            ErrorDesc = api.ErrorMessage
                        };
                        return false;
                    }
                }
                else
                {
                    Error = new Error
                    {
                        ErrorCode = "-9999",
                        ErrorDesc = api.ErrorMessage
                    };
                    return false;
                }
            }
            else
            {
                Error = new Error
                {
                    ErrorCode = "-9999",
                    ErrorDesc = "Retrieve Success"
                };
                return false;
            }
        }

        public override GameAPIController.GetBalanceVM GetBalance()
        {
            var res = new vm.GetBalanceVM();

            try
            {
                if (gameMap != null)
                {
                    var apiCall = api.GetBalance(Convert.ToInt32(gameMap.GamePlayerID));
                    InsertLog(GAME_ID, "GetBalance", "request", api.Request, "parameter", api.Parameters, "response", api.Response);

                    targetApiUrl = api.Request;
                    request = api.Request;
                    response = api.Response;

                    if (apiCall)
                    {
                        res.PlayerID = playerID;
                        res.Balance = api.GetBalanceRes.balance;
                        res.Game = GAME_ID;
                        res.Message = "Successfully get balance.";
                        res.Code = 0;
                    }
                    else
                    {
                        res.PlayerID = playerID;
                        res.Balance = null;
                        res.Game = GAME_ID;
                        res.Message = api.ErrorMessage;
                        res.Code = 2;
                    }
                }
                else
                {
                    res.PlayerID = playerID;
                    res.Balance = null;
                    res.Game = GAME_ID;
                    res.Message = "Game account not found.";
                    res.Code = 1;
                }
            }
            catch (Exception ex)
            {
                _common.LogError("Balance" + " " + DateTime.Now.ToString() + " " + string.Format("{0}", playerID) + " " + ex.Message.ToString(), GAME_ID);

                var err = new Error
                {
                    ErrorCode = "999",
                    ErrorDesc = ex.Message.ToString()
                };

                InsertLog(GAME_ID, "GetBalance", "Error", JsonConvert.SerializeObject(err));

                res.PlayerID = playerID;
                res.Balance = null;
                res.Game = GAME_ID;
                res.Message = ex.Message.ToString();
                res.Code = 99;
            }

            return res;
        }

        public override decimal? GetGameBalance()
        {
            if (gameMap != null)
            {
                var apiCall = api.GetBalance(Convert.ToInt32(gameMap.GamePlayerID));
                InsertLog(GAME_ID, "GetBalance", "request", api.Request, "parameter", api.Parameters, "response", api.Response);

                targetApiUrl = api.Request;
                request = api.Request;
                response = api.Response;

                if (apiCall)
                {
                    return api.GetBalanceRes.balance;
                }
                else
                {
                    return null;
                }
            }
            else
            {
                return null;
            }
        }

        public override GameAPIController.GetGameUrlVM GetGameUrl(string langId, bool IsMobile = true)
        {
            var res = new vm.GetGameUrlVM();

            try
            {
                if (gameMap != null)
                {
                    var apiLogin = api.GameLogin(gameMap.PlayerID, POST_FIX, "Password", gameMap.PlayerID);
                    InsertLog(GAME_ID, "GetGameUrlVM-GameLogin", "request", api.Request, "response", api.Response, "Parameters", api.Parameters);

                    if (apiLogin)
                    {
                        var apiLobby = api.GetLobbyUrl(api.GameLoginRes.actk);
                        InsertLog(GAME_ID, "GetGameUrlVM-GetLobbyUrl", "request", api.Request, "response", api.Response);

                        res.PlayerID = playerID;
                        res.Url = api.GetLobbyUrlRes.LobbyUrl;
                        res.Game = GAME_ID;
                        res.Message = "Success";
                        res.Code = 0;
                    }
                    else
                    {
                        res.PlayerID = playerID;
                        res.Url = null;
                        res.Game = GAME_ID;
                        res.Message = "Fail Login to Provider Platform.";
                        res.Code = 0;
                    }
                }
                else
                {
                    res.PlayerID = playerID;
                    res.Url = null;
                    res.Game = GAME_ID;
                    res.Message = "Game account not found.";
                    res.Code = 1;
                }
            }
            catch (Exception ex)
            {
                _common.LogError("GetGameUrl: " + playerID + " " + ex.Message.ToString(), GAME_ID);
                var err = new Error { ErrorCode = "999", ErrorDesc = ex.Message.ToString() };
                InsertLog(GAME_ID, "GetGameUrl", "Error", JsonConvert.SerializeObject(err));

                res.PlayerID = playerID;
                res.Url = null;
                res.Game = GAME_ID;
                res.Message = ex.Message.ToString();
                res.Code = 99;
            }

            return res;
        }

        //public string GetLobbyUrl()
        //{
        //    string lobbyUrl = "";
        //    try
        //    {
        //        if (gameMap != null)
        //        {
        //            var apiCall = api.GetLobbyUrl(gameMap.PlayerID, POST_FIX, "Password", gameMap.PlayerID);
        //            InsertLog(GAME_ID, "GetGameUrlVM", "request", api.Request, "response", api.Response);

        //            lobbyUrl= api.GetLobbyUrlRes.LobbyUrl;
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        _common.LogError("GetLobbyUrl: " + playerID + " " + ex.Message.ToString(), GAME_ID);
        //        var err = new Error { ErrorCode = "999", ErrorDesc = ex.Message.ToString() };
        //        InsertLog(GAME_ID, "GetLobbyUrl", "Error", JsonConvert.SerializeObject(err));
        //    }

        //    return lobbyUrl;
        //}

        public GameListRes GetGamesList()
        {
            var apiCall = api.GetGameList();
            InsertLog(GAME_ID, "GetGameList", "Request", api.Request, "Parameters", api.Parameters, "Response", api.Response, "ErrorMessage", api.ErrorMessage, "IP", ip);

            if (apiCall)
                return api.GameListRes;
            else
                return null;
        }

        public GameLoginRes GetGameLogin(string playerId)
        {
            var apiCall = api.GameLogin(gameMap.PlayerID, POST_FIX, "Password", playerId);
            InsertLog(GAME_ID, "GameLogin", "Request", api.Request, "Parameters", api.Parameters, "Response", api.Response, "ErrorMessage", api.ErrorMessage, "IP", ip);

            if (apiCall)
                return api.GameLoginRes;
            else
                return null;
        }

        public override bool KickPlayer(string GamePlayerId, ref Error Error)
        {
            if (gameMap != null)
            {
                var apiCall = api.GameLogout(gameMap.GamePlayerID);
                InsertLog(GAME_ID, "KickPlayer", "request", api.Request, "parameter", api.Parameters, "response", api.Response);

                targetApiUrl = api.Request;
                request = api.Request;
                response = api.Response;

                if (apiCall)
                {
                    return true;
                }
                else
                {
                    Error = new Error
                    {
                        ErrorCode = "-9999",
                        ErrorDesc = api.ErrorMessage
                    };
                    return false;
                }
            }
            else
            {
                Error = new Error
                {
                    ErrorCode = "-9999",
                    ErrorDesc = "Retrieve Success"
                };
                return false;
            }
        }

        public override string RetryDeposit(decimal Amount, string TrackerId, ref Error Error)
        {
            throw new NotImplementedException();
        }

        public override string RetryWithdraw(decimal Amount, string TrackerId, ref Error Error)
        {
            throw new NotImplementedException();
        }

        public GetAccountTransactionsRes GetAccountTransactions(DateTime timeFrom)
        {
            api = new Ace333(API_URL, H5_LOGIN_API_URL, H5_LOBBY_API_URL, POST_FIX, API_SERVICE_TOKEN, SECRET_KEY, MD5_KEY, ENCRYPT_KEY, DELIMETER, CURRENCY, timeOutSec);

            var apiCall = api.GetAccountTransactions(timeFrom);
            InsertLog(GAME_ID, "GetAccountTransactions", "Request", api.Request, "Parameters", api.Parameters, "Response", api.Response, "ErrorMessage", api.ErrorMessage);

            if (apiCall)
                return api.GetAccountTransactionsRes;
            else
                return null;
        }
    }
}