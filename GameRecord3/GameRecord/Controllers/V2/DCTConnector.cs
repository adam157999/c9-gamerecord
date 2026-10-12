using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using GameRecord.Gamess.DCT;
using GameRecord.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using vm = GameRecord.Controllers.GameAPIController;

namespace GameRecord.Controllers.V2
{
    public class DCTConnector : BaseConnector
    {
        private string gameID = "DCT";
        private string apiUrl = "";
        private string betDataUrl = "";
        private string prefix = "";
        private string provider = "";
        private string brandId = "";
        private string apiKey = "";
        //private string currency = "";
        private string countryCode = "";
        private string currency = "";
        private int defaultGameId = 0;
        //private string defaultLanguage = "";
        //private int defaultGameId = 0;
        private int timeOutSec = 30;
        private string ip = "";

        private GameMapping gameMap;
        private DCT api = null;

        public DCTConnector(string PlayerID = "", string CurrencyID = "", string IPAddr = "", GameEnvironment Environment = GameEnvironment.PROD) : base(PlayerID, CurrencyID, IPAddr, Environment)
        {
            using (BingoConn db = new BingoConn())
            {
                gameMap = db.GameMappings.Where(x => x.Game.ToUpper().Trim() == gameID.ToUpper().Trim() &&
                                                        x.PlayerID == playerID &&
                                                        x.Currency.ToUpper() == currencyID &&
                                                        x.IsActive == true).FirstOrDefault();

                var apiConfig = CatchData.GetApiKeys(gameID.ToUpper().Trim(), Environment.ToString(), currencyID.ToUpper().Trim());
                if (string.IsNullOrEmpty(CurrencyID)) apiConfig = CatchData.GetApiKeys(gameID.ToUpper().Trim(), Environment.ToString());

                if (apiConfig != null)
                {
                    JObject jObj = JObject.Parse(apiConfig.ApiConfig);
                    apiUrl = jObj["apiUrl"].ToString();
                    betDataUrl = jObj["betDataUrl"].ToString();
                    prefix = jObj["prefix"].ToString();
                    provider = jObj["provider"].ToString();
                    brandId = jObj["brandId"].ToString();
                    apiKey = jObj["apiKey"].ToString();
                    countryCode = jObj["countryCode"].ToString();
                    currency = jObj["currency"].ToString();
                    defaultGameId = Convert.ToInt32(jObj["defaultGameId"].ToString());
                    //defaultLanguage = jObj["DCTDefaultLanguage"].ToString();
                    timeOutSec = Convert.ToInt32(jObj["timeOutSec"].ToString());
                    api = new DCT(apiUrl, betDataUrl, prefix, provider, brandId, apiKey, currency, countryCode, defaultGameId, timeOutSec);
                    ip = IPAddr;
                }
            }
        }

        public override vm.CheckAndCreateAccountVM CreatePlayer(string loginID, string loginPass, string fullName, string mcUID)
        {
            //throw new NotImplementedException();
            var res = new vm.CheckAndCreateAccountVM();
            try
            {
                if (gameMap == null)
                {
                    string requestUrl = "QQQ";
                    var apiCheck = api.LoginGame(loginID, defaultGameId, "en");
                    string curlCmd = $"curl -X POST {requestUrl} -H \"Content-Type: application/json\" -d '{JsonConvert.SerializeObject(api.Parameters)}'";

                    InsertLog(gameID, "CreatePlayer", "Request", api.Request, "Parameters", api.Parameters, "Response", api.Response, "ErrorMessage", api.ErrorMessage, "IP", ip, "curl", curlCmd);
                    if (apiCheck)
                    {
                        InsertGameMapping(gameID, loginID, loginPass, playerID);
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
                var err = new { ErrorCode = "999", ErrorDesc = ex.Message.ToString() };
                InsertLog(gameID, "CreateUser", "Error", JsonConvert.SerializeObject(err));

                res.PlayerID = playerID;
                res.GamePlayerID = loginID;
                res.Game = gameID;
                res.Message = ex.Message.ToString();
                res.Code = 99;
            }

            return res;
        }
        public vm.CheckAndCreateAccountVM CreatePlayer_DCT(string loginID, string loginPass, int gameId, string language = "en")
        {
            throw new NotImplementedException();

            //var res = new vm.CheckAndCreateAccountVM();

            //try
            //{
            //    if (gameMap == null)
            //    {
            //        var apiCheck = api.LoginGame(loginID, gameId, language);
            //        InsertLog(gameID, "CreatePlayer", "Request", api.Request, "Parameters", api.Parameters, "Response", api.Response, "ErrorMessage", api.ErrorMessage);

            //        if (apiCheck)
            //        {
            //            InsertGameMapping(gameID, loginID, loginPass, playerID);
            //            gameMap = LoadGameMap(gameID, playerID, currencyID);

            //            res.PlayerID = playerID;
            //            res.GamePlayerID = loginID;
            //            res.Game = gameID;
            //            res.Message = "Success";
            //            res.Code = 0;
            //        }
            //        else
            //        {
            //            res.PlayerID = playerID;
            //            res.GamePlayerID = null;
            //            res.Game = gameID;
            //            res.Message = api.ErrorMessage;
            //            res.Code = 2;
            //        }
            //    }
            //    else
            //    {
            //        res.PlayerID = playerID;
            //        res.GamePlayerID = gameMap.GamePlayerID;
            //        res.Game = gameID;
            //        res.Message = "Retrieve Success";
            //        res.Code = 1;
            //    }
            //}
            //catch (Exception ex)
            //{
            //    _common.LogError("CreateUser" + " " + DateTime.Now.ToString() + " " + string.Format("{0}", playerID) + " " + ex.Message.ToString(), gameID);
            //    var err = new { ErrorCode = "999", ErrorDesc = ex.Message.ToString() };
            //    InsertLog(gameID, "CreateUser", "Error", JsonConvert.SerializeObject(err));

            //    res.PlayerID = playerID;
            //    res.GamePlayerID = loginID;
            //    res.Game = gameID;
            //    res.Message = ex.Message.ToString();
            //    res.Code = 99;
            //}

            //return res;
        }
        public override bool GameDeposit(decimal Amount, string TrackerId, ref Error Error)
        {
            if (gameMap != null)
            {
                var apiCall = api.Credit(gameMap.GamePlayerID, Amount, TrackerId);
                InsertLog(gameID, "Deposit", "request", api.Request, "response", api.Response);

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
                var apiCall = api.Withdraw(gameMap.GamePlayerID, Amount, TrackerId);
                InsertLog(gameID, "Withdraw", "request", api.Request, "response", api.Response);

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
                    var apiCall = api.GetBalance(gameMap.GamePlayerID);
                    InsertLog(gameID, "GetBalance", "request", api.Request, "response", api.Response);

                    targetApiUrl = api.Request;
                    request = api.Request;
                    response = api.Response;

                    if (apiCall)
                    {
                        res.PlayerID = playerID;
                        res.Balance = api.GetBalanceRes.data.balance;
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
            if (gameMap != null)
            {
                var apiCall = api.GetBalance(gameMap.GamePlayerID);
                InsertLog(gameID, "GetBalance", "request", api.Request, "response", api.Response);

                targetApiUrl = api.Request;
                request = api.Request;
                response = api.Response;

                if (apiCall)
                {
                    return api.GetBalanceRes.data.balance;
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
                    string lobbyUrl = HttpContext.Current.Request.Url.AbsoluteUri.Replace(HttpContext.Current.Request.Url.PathAndQuery, "/") + "dct/Lobby";
                    lobbyUrl += "?PlayerId=" + gameMap.GamePlayerID;
                    lobbyUrl += "&LangCode=" + langId;

                    InsertLog(gameID, "GetGameUrl", "lobbyUrl", lobbyUrl, "IsMobile", IsMobile);

                    res.PlayerID = playerID;
                    res.Url = lobbyUrl;
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
                _common.LogError("GetGameUrl: " + playerID + " " + ex.Message.ToString(), gameID);
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
        public GetGameListRes GetGamesList()
        {
            var apiCall = api.GetGameList();
            InsertLog(gameID, "GetGameList", "Request", api.Request, "Parameters", api.Parameters, "Response", api.Response, "ErrorMessage", api.ErrorMessage, "IP", ip);

            if (apiCall)
                return api.GetGameListRes;
            else
                return null;
        }

        public LoginGameRes GetLaunchGameUrl(int gameId, string langCode, bool isMobile, string returnUrl, bool isFullScrren)
        {
            langCode = GetGameLanguage(gameID, langCode);

            var channel = isMobile ? "mobile" : "pc";
            var apiCall = api.LoginGame(gameMap.GamePlayerID, gameId, langCode, channel, returnUrl, isFullScrren);
            InsertLog(gameID, "LoginGame", "Request", api.Request, "Parameters", api.Parameters, "Response", api.Response, "ErrorMessage", api.ErrorMessage, "Url", api.Request);

            if (apiCall)
                return api.LoginGameRes;
            else
                return null;
        }
        public override bool KickPlayer(string GamePlayerId, ref Error Error)
        {
            if (gameMap != null)
            {
                var apiCall = api.KickPlayer(gameMap.GamePlayerID);
                InsertLog(gameID, "KickPlayer", "request", api.Request, "response", api.Response);

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

        public GetBetDataRes GetBetData(DateTime dateFrom, DateTime dateTo, int page = 1)
        {
            api = new DCT(apiUrl, betDataUrl, prefix, provider, brandId, apiKey, gameMap.Currency, countryCode, timeOutSec);

            var apiCall = api.GetBetData(dateFrom, dateTo, page, gameMap.GamePlayerID);
            InsertLog(gameID, "GetBetData", "Request", api.Request, "Parameters", api.Parameters, "Response", api.Response, "ErrorMessage", api.ErrorMessage);

            if (apiCall)
                return api.GetBetDataRes;
            else
                return null;
        }
    }
}