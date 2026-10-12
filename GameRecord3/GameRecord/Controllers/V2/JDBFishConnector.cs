using GameRecord.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Web;
using System.Collections.Generic;
using GameRecord.Gamess.JDBFish;
using vm = GameRecord.Controllers.GameAPIController;

namespace GameRecord.Controllers
{
    public class JDBFishConnector : BaseConnector
    {
        private string gameID = "JDBFISH";
        private string apiUrl = "";
        private string gameUrl = "";
        private string key = "";
        private string iv = "";
        private string dc = "";
        private string manager = "";
        private string masterAgent = "";
        private string agent = "";
        private int timeOutSec = 30;
        private string noneAllCashOutFlag = "0";
        private GameMapping gameMap;
        private const string gType = "7";
        private JDBFish api = null; // Corrected namespace
        //private GameRecord.Games.JDB api = null;
        Common _common = new Common();
        private string lobbyUrl => HttpContext.Current.Request.Url.AbsoluteUri.Replace(HttpContext.Current.Request.Url.PathAndQuery, "/") + "JDBFish/Lobby";

        public JDBFishConnector(string PlayerID = "", string CurrencyID = "", string IPAddr = "", GameEnvironment Environment = GameEnvironment.PROD) : base(PlayerID, CurrencyID, IPAddr, Environment)
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
                    apiUrl = jObj["JDBApiUrl"].ToString();
                    key = jObj["JDBKey"].ToString();
                    iv = jObj["JDBiv"].ToString();
                    dc = jObj["JDBDc"].ToString();
                    manager = jObj["JDBManager"].ToString();
                    masterAgent = jObj["JDBMasterAgent"].ToString();
                    agent = jObj["JDBAgent"].ToString();
                    timeOutSec = Convert.ToInt32(jObj["JDBTimeOutSec"].ToString());
                    api = new JDBFish(apiUrl, iv, key, manager, masterAgent, agent, dc, timeOutSec);
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
                    string ipAddr = _common.GetIPAddress();
                    string userAgent = HttpContext.Current.Request.UserAgent;
                    var apiCheck = api.CreatePlayer(loginID, fullName, 0);
                    targetApiUrl = api.TargetApiUrl;
                    request = api.Request;
                    response = api.Response;
                    string error = api.ErrorMessage;
                    if (apiCheck)
                    {
                        InsertLog(gameID, "CreateUser", "request", api.Request, "response", api.Response);
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
                        InsertLog(gameID, "CreateUser", "request", api.Request, "response", api.ErrorMessage);
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

        public override bool GameDeposit(decimal Amount, string RequestID, ref Error Error)
        {
            if (gameMap != null)
            {
                if (Amount >= 0)
                {
                    var apiCall = api.WithdrawDeposite(gameMap.GamePlayerID, RequestID, noneAllCashOutFlag, Decimal.ToDouble(Amount), nameof(GameDeposit));
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
                        Error = new Error { ErrorCode = "-9999", ErrorDesc = api.ErrorMessage };
                        return false;
                    }
                }
                else
                {
                    Error = new Error { ErrorCode = "-9999", ErrorDesc = "Deposite Only Accept Positive Value" };
                    return false;
                }
            }
            else
            {
                Error = new Error { ErrorCode = "-9999", ErrorDesc = "Retrieve Success" };
                return false;
            }
        }

        public override bool GameWithdraw(decimal Amount, string RequestID, ref Error Error)
        {
            if (gameMap != null)
            {
                var apiCall = api.WithdrawDeposite(gameMap.GamePlayerID, RequestID, noneAllCashOutFlag, -1 * Decimal.ToDouble(Amount), nameof(GameDeposit));
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
                    Error = new Error { ErrorCode = "-9999", ErrorDesc = api.ErrorMessage };
                    return false;
                }
            }
            else
            {
                Error = new Error { ErrorCode = "-9999", ErrorDesc = "Retrieve Success" };
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
                    var apiCall = api.QueryPlayer(gameMap.GamePlayerID);
                    InsertLog(gameID, "GetBalance", "request", api.Request, "response", api.Response);
                    targetApiUrl = api.Request;
                    request = api.Request;
                    response = api.Response;
                    if (apiCall)
                    {
                        res.PlayerID = playerID;
                        res.Balance = api.Action15QueryPlayerResponse.data.FirstOrDefault().balance;
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
                var err = new Error { ErrorCode = "999", ErrorDesc = ex.Message.ToString() };
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
                var apiCall = api.QueryPlayer(gameMap.GamePlayerID);
                InsertLog(gameID, "GetBalance", "request", api.Request, "response", api.Response);
                targetApiUrl = api.Request;
                request = api.Request;
                response = api.Response;
                if (apiCall)
                {
                    return Convert.ToDecimal(api.Action15QueryPlayerResponse.data.FirstOrDefault().balance);
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

        // FachaiFish-style GetGameUrl for lobby
        public override vm.GetGameUrlVM GetGameUrl(string langId, bool IsMobile = true)
        {
            var res = new vm.GetGameUrlVM();
            try
            {
                if (gameMap != null)
                {
                    string url = string.Format("{0}?playerid={1}&currency={2}&ip={3}&lang={4}&mobile={5}",
                        lobbyUrl, playerID, currencyID, GetClientIp(), langId, IsMobile.ToString().ToLower());
                    InsertLog(gameID, "GetGameUrl", "playerid", playerID, "currency", currencyID, "ip", GetClientIp(), "lang", langId, "url", url);
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

        // Rename old GetGameUrl to GetFishGameUrl and add gType, mType
        public vm.GetGameUrlVM GetFishGameUrl(string langId, string gType, string mType, bool IsMobile = false)
        {
            var res = new vm.GetGameUrlVM();
            try
            {
                if (gameMap != null)
                {
                    if (!string.IsNullOrEmpty(ipAddr)) ipAddr = ipAddr.Split(':')[0];
                    string userAgent = HttpContext.Current.Request.UserAgent;
                    langId = GetGameLanguage(gameID, langId);
                    // Use Token with 3 params: uid, lang, gType. mType is not supported in Token signature.
                    var apiLoginCall = api.Token(gameMap.GamePlayerID, langId, gType, mType); 
                    InsertLog(gameID, "GetFishGameUrl", "request", api.Request, "response", api.Response);
                    targetApiUrl = api.Request;
                    request = api.Request;
                    response = api.Response;
                    if (apiLoginCall)
                    {
                        res.PlayerID = playerID;
                        res.Url = api.TokenResponse.path;
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
                _common.LogError("GetFishGameUrl" + " " + DateTime.Now.ToString() + " " + string.Format("{0}", playerID) + " " + ex.Message.ToString(), gameID);
                var err = new Error { ErrorCode = "999", ErrorDesc = ex.Message.ToString() };
                InsertLog(gameID, "GetFishGameUrl", "Error", JsonConvert.SerializeObject(err));
                res.PlayerID = playerID;
                res.Url = null;
                res.Game = gameID;
                res.Message = ex.Message.ToString();
                res.Code = 99;
            }
            return res;
        }

        // Returns the list of games for the JDBFish lobby by calling QueryGameList and filtering gType == 7
        public List<JDBFishGame> GetGameList(string lang = "en-us")
        {
            var games = new List<JDBFishGame>();
            if (api.QueryGameList(lang))
            {
                try
                {
                    var response = JsonConvert.DeserializeObject<QueryGameListResponse>(api.Response);
                    if (response != null && response.data != null)
                    {
                        foreach (var group in response.data)
                        {
                            if (group.gType == 7 && group.list != null)
                            {
                                games.AddRange(group.list);
                            }
                        }
                    }
                }
                catch { }
            }
            return games;
        }

        private string GetClientIp()
        {
            var request = HttpContext.Current.Request;
            string ip = request.ServerVariables["HTTP_X_FORWARDED_FOR"];
            if (!string.IsNullOrEmpty(ip))
            {
                string[] ipList = ip.Split(',');
                return ipList[0].Trim();
            }
            return request.ServerVariables["REMOTE_ADDR"];
        }

        public override bool KickPlayer(string GamePlayerId, ref Error Error)
        {
            if (gameMap != null)
            {
                var apiCall = api.KickPlayer(gameMap.GamePlayerID);
                InsertLog(gameID, "KickPlayer", "request", api.Request, "response", api.Response);
                targetApiUrl = api.TargetApiUrl;
                request = api.Request;
                response = api.Response;
                if (apiCall)
                {
                    return true;
                }
                else
                {
                    Error = new Error { ErrorCode = "998", ErrorDesc = api.ErrorMessage };
                    return false;
                }
            }
            else
            {
                Error = new Error { ErrorCode = "998", ErrorDesc = "Retrieve Success" };
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

        public GetDetailReportResponse GetDetailReport(string dateFrom, string dateTo)
        {
            api = new JDBFish(apiUrl, iv, key, manager, masterAgent, agent, dc, timeOutSec);
            var apiCall = api.GetDetailReport(Convert.ToDateTime(dateFrom).ToString("dd-MM-yyyy HH:mm:ss"), Convert.ToDateTime(dateTo).ToString("dd-MM-yyyy HH:mm:ss"));
            InsertLog(gameID, "JDBRecord", "request", api.Request, "response", api.Response);
            targetApiUrl = api.Request;
            request = api.Request;
            response = api.Response;
            if (apiCall)
            {
                return api.GetDetailReportResponse;
            }
            return null;
        }

        public GetDetailReportResponse GetHistoryDetailReport(string dateFrom, string dateTo)
        {
            api = new JDBFish(apiUrl, iv, key, manager, masterAgent, agent, dc, timeOutSec);
            var apiCall = api.GetHistoryDetailReport(Convert.ToDateTime(dateFrom).ToString("dd-MM-yyyy HH:mm:ss"), Convert.ToDateTime(dateTo).ToString("dd-MM-yyyy HH:mm:ss"));
            targetApiUrl = api.Request;
            request = api.Request;
            response = api.Response;
            if (apiCall)
            {
                return api.GetDetailReportResponse;
            }
            return null;
        }
    }
}
