using System;
using System.Linq;
using System.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using GameRecord.Models;
using vm = GameRecord.Controllers.GameAPIController;
using System.Collections.Generic;
using GameRecord.Gamess.BNG;

namespace GameRecord.Controllers
{
    public class BNGConnector : BaseConnector
    {
        private string gameID = "BNG";
        private string apiUrl = "";
        private string gameUrl = "";
        private string reportUrl = "";
        private string projectName = "";
        private string apiToken = "";
        private string wl = "";
        private int timeOutSec = 30;
        private string language = "en";
        private GameMapping gameMap;
        private BNG api = null;

        public BNGConnector(string PlayerID = "", string CurrencyID = "", string IPAddr = "", GameEnvironment Environment = GameEnvironment.PROD) : base(PlayerID, CurrencyID, IPAddr, Environment)
        {
            using (BingoConn db = new BingoConn())
            {
                gameMap = db.GameMappings
                    .Where(x => x.Game.ToUpper().Trim() == gameID.ToUpper().Trim() && x.PlayerID == playerID && x.Currency.ToUpper() == currencyID && x.IsActive == true)
                    .FirstOrDefault();

                var apiConfig = CatchData.GetApiKeys(gameID.ToUpper().Trim(), Environment.ToString(), currencyID.ToUpper().Trim());
                if (string.IsNullOrEmpty(CurrencyID)) apiConfig = CatchData.GetApiKeys(gameID.ToUpper().Trim(), Environment.ToString());

                if (apiConfig != null)
                {
                    JObject jObj = JObject.Parse(apiConfig.ApiConfig);
                    apiUrl = jObj["BNGApiUrl"].ToString();
                    gameUrl = jObj["BNGGameUrl"].ToString();
                    projectName = jObj["BNGProjectName"].ToString();
                    apiToken = jObj["BNGApiToken"].ToString();
                    wl = jObj["BNGWL"].ToString();
                    reportUrl = jObj["BNGReportUrl"].ToString();
                    timeOutSec = Convert.ToInt32(jObj["BNGTimeOutSec"].ToString());
                    //apiUrl = apiUrl.Substring(0, apiUrl.LastIndexOf('/')); ;
                    api = new BNG(apiUrl,reportUrl,apiToken,wl,projectName, timeOutSec);
                }
            }
        }

        public override vm.CheckAndCreateAccountVM CreatePlayer(string loginID, string loginPass, string fullName, string mcUID)
        {
            var res = new vm.CheckAndCreateAccountVM();

            try
            {
                if (gameMap == null)
                {
                    string trackerId = Guid.NewGuid().ToString();
                    string language = "en";
                    string langId = GetGameLanguage(gameID, language);
                    var apiCall = api.UserCreate(loginID,currencyID);
                    InsertLog(gameID, "UserCreate", "request", api.Request, "Parameters", api.Parameters, "response", api.Response);

                    targetApiUrl = api.Request;
                    request = api.Request;
                    response = api.Response;

                    if (apiCall)
                    {
                        InsertGameMapping(gameID, loginID, loginPass, mcUID);
                        gameMap = LoadGameMap(gameID, playerID, currencyID);

                        res.PlayerID = playerID;
                        res.GamePlayerID = loginID;
                        res.Game = gameID;
                        res.Message = "Success";
                        res.Code = 0;
                    }
                    else
                    {
                        res.PlayerID = playerID;
                        res.GamePlayerID = null;
                        res.Game = gameID;
                        res.Message = api.ErrorMessage;
                        res.Code = 2;
                    }
                }
                else
                {
                    res.PlayerID = playerID;
                    res.GamePlayerID = gameMap.GamePlayerID;
                    res.Game = gameID;
                    res.Message = "Retrieve Success";
                    res.Code = 1;
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

        public override bool GameDeposit(decimal Amount, string TrackerId, ref Error Error)
        {
            if (gameMap != null)
            {
                string decimalString = Amount.ToString();
                var apiCall = api.BalanceTransfer(gameMap.GamePlayerID, decimalString, gameMap.Currency,"CREDIT",TrackerId);
                InsertLog(gameID, "BalanceTransfer", "request", api.Request, "Parameters", api.Parameters, "response", api.Response);

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
                string decimalString = Amount.ToString();
                var apiCall = api.BalanceTransfer(gameMap.GamePlayerID, decimalString, gameMap.Currency, "DEBIT", TrackerId);
                InsertLog(gameID, "BalanceTransfer", "request", api.Request, "Parameters", api.Parameters, "response", api.Response);

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

        public override vm.GetBalanceVM GetBalance()
        {
            var res = new vm.GetBalanceVM();

            try
            {
                if (gameMap != null)
                {
                    string trackerId = Guid.NewGuid().ToString();
                    var apiCall = api.GetPlayer(gameMap.GamePlayerID, gameMap.Currency);
                    InsertLog(gameID, "GetPlayer", "request", api.Request, "Parameters", api.Parameters, "response", api.Response);

                    targetApiUrl = api.Request;
                    request = api.Request;
                    response = api.Response;

                    if (apiCall)
                    {
                        res.PlayerID = playerID;
                        res.Balance = Convert.ToDecimal(api.Get_Player_Response.balance);
                        res.Game = gameID;
                        res.Message = "Successfully get balance.";
                        res.Code = 0;
                    }
                    else
                    {
                        res.PlayerID = playerID;
                        res.Balance = null;
                        res.Game = gameID;
                        res.Message = api.ErrorMessage;
                        res.Code = 2;
                    }
                }
                else
                {
                    res.PlayerID = playerID;
                    res.Balance = null;
                    res.Game = gameID;
                    res.Message = "Game account not found.";
                    res.Code = 1;
                }
            }
            catch (Exception ex)
            {
                _common.LogError("Balance" + " " + DateTime.Now.ToString() + " " + string.Format("{0}", playerID) + " " + ex.Message.ToString(), gameID);

                var err = new Error
                {
                    ErrorCode = "999",
                    ErrorDesc = ex.Message.ToString()
                };

                InsertLog(gameID, "GetCredit", "Error", JsonConvert.SerializeObject(err));

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
            if (gameMap != null)
            {
                string trackerId = Guid.NewGuid().ToString();
                var apiCall = api.GetPlayer(gameMap.GamePlayerID, gameMap.Currency);
                InsertLog(gameID, "GetPlayer", "request", api.Request, "Parameters", api.Parameters, "response", api.Response);

                targetApiUrl = api.Request;
                request = api.Request;
                response = api.Response;

                if (apiCall)
                {
                    return Convert.ToDecimal(api.Get_Player_Response.balance);
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

        public override vm.GetGameUrlVM GetGameUrl(string langId, bool IsMobile = false)
        {
            var res = new vm.GetGameUrlVM();
            try
            {
                if (gameMap != null)
                {
                    InsertLog(gameID, "GetGameUrlBNG", "langId", langId, "IsMobile", IsMobile, "gameID", gameID);

                    var sessionId = new Guid().ToString();
                    string platform = "";
                    if(IsMobile)
                        platform = "mobile";
                    else
                        platform = "desktop";
                    langId = GetGameLanguage(gameID, langId);

                    var apiCall = api.GetPlayerToken(gameMap.GamePlayerID, gameMap.Currency);
                    InsertLog(gameID, "GetPlayerToken", "request", api.Request, "Parameters", api.Parameters, "response", api.Response);

                    if (apiCall) 
                    {
                        string url = string.Format(gameUrl,projectName);
                        url = url + $"token={api.Get_Player_Token_Response.player_token}&ts=100000&platform={platform}&lang={langId}&demo=0";
                        InsertLog(gameID, "GetGameUrlBNG", "langId", langId, "IsMobile", IsMobile, "gameID", gameID, "url", url);

                        res.PlayerID = playerID;
                        res.Url = url;
                        res.Game = gameID;
                        res.Message = "Success";
                        res.Code = 0;
                    }
                    else
                    {
                        res.PlayerID = playerID;
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
                _common.LogError("GetGameUrlBNG" + " " + DateTime.Now.ToString() + " " + string.Format("{0}", playerID) + " " + ex.Message.ToString(), gameID);

                var err = new Error
                {
                    ErrorCode = "999",
                    ErrorDesc = ex.Message.ToString()
                };

                InsertLog(gameID, "GetGameUrlBNG", "Error", JsonConvert.SerializeObject(err));

                res.PlayerID = playerID;
                res.Url = null;
                res.Game = gameID;
                res.Message = ex.Message.ToString();
                res.Code = 99;
            }

            return res;
        }


        public override bool KickPlayer(string GamePlayerId, ref Error Error)
        {
            if (gameMap != null)
            {
                var sessionId = new Guid().ToString();
                var apiCall = api.UserLogout(gameMap.GamePlayerID, gameMap.Currency);
                InsertLog(gameID, "UserLogout", "request", api.Request, "Parameters", api.Parameters, "response", api.Response);

                targetApiUrl = api.TargetApiUrl;
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
                        ErrorCode = "998",
                        ErrorDesc = api.ErrorMessage
                    };
                    return false;
                }
            }
            else
            {
                Error = new Error
                {
                    ErrorCode = "998",
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

        public transactionListResponse RetrieveTransactionByMinute(string startDate, string endDate)
        {
            var sessionId = new Guid().ToString();

            var apiCall = api.GetReport(startDate,endDate);
            InsertLog(gameID, "GetReport", "request", api.Request, "Parameters", api.Parameters, "response", api.Response);

            targetApiUrl = api.Request;
            request = api.Request;
            response = api.Response;

            if (apiCall)
                return api.TransactionListResponse;

            return null;
        }
    }
}