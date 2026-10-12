using GameRecord.Gamess.JiLi;
using GameRecord.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using vm = GameRecord.Controllers.GameAPIController;

namespace GameRecord.Controllers
{
    public class JiliFishConnector : BaseConnector
    {
        private int gameUniqueID = 0;
        private string gameID = "JILIFISH";
        private string apiUrl = "";
        private string gameUrl = "";
        private string agentId = "";
        private string agentKey = "";
        private int timeOutSec = 30;
        private GameMapping gameMap;
        private GameRecord.Gamess.JiLi.JiLi api = null;
        private string lobbyUrl => HttpContext.Current.Request.Url.AbsoluteUri.Replace(HttpContext.Current.Request.Url.PathAndQuery, "/") + "JiLiFish/Lobby";

        public JiliFishConnector(string playerID = "", string currencyID = "", string ipAddr = "", GameEnvironment environment = GameEnvironment.PROD) : base(playerID, currencyID, ipAddr, environment)
        {
            using (BingoConn db = new BingoConn())
            {
                gameMap = db.GameMappings.Where(x => x.Game.ToUpper().Trim() == gameID.ToUpper().Trim() &&
                                                     x.PlayerID == playerID &&
                                                     x.Currency.ToUpper() == currencyID &&
                                                     x.IsActive == true).FirstOrDefault();

                var apiConfig = CatchData.GetApiKeys(gameID.ToUpper().Trim(), environment.ToString(), currencyID.ToUpper().Trim());
                if (string.IsNullOrEmpty(currencyID)) apiConfig = CatchData.GetApiKeys(gameID.ToUpper().Trim(), environment.ToString());

                if (apiConfig != null)
                {
                    JObject jObj = JObject.Parse(apiConfig.ApiConfig);
                    apiUrl = jObj["JiLiApiUrl"].ToString();
                    agentId = jObj["JiLiAgentId"].ToString();
                    agentKey = jObj["JiLiAgentKey"].ToString();
                    timeOutSec = Convert.ToInt32(jObj["JiLiTimeOutSec"].ToString());

                    api = new GameRecord.Gamess.JiLi.JiLi(apiUrl, agentId, agentKey, timeOutSec);
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
                    var apiCheck = api.CreateMember(loginID);
                    if (apiCheck)
                    {
                        InsertLog(gameID, "CreateUser", "request", api.Request, "response", api.Response);
                        targetApiUrl = api.TargetApiUrl;
                        request = api.Request;
                        response = api.Response;
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

        public override bool GameDeposit(decimal Amount, string TransactionID, ref Error Error)
        {
            if (gameMap != null)
            {
                byte transferType = 2;
                var apiCall = api.ExchangeTransferByAgentId(gameMap.GamePlayerID, TransactionID, Amount, transferType);
                InsertLog(gameID, "Deposit", "request", api.Request, "response", api.Response);
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

        public override bool GameWithdraw(decimal Amount, string TransactionID, ref Error Error)
        {
            if (gameMap != null)
            {
                byte transferType = 3;
                var apiCall = api.ExchangeTransferByAgentId(gameMap.GamePlayerID, TransactionID, Amount, transferType);
                InsertLog(gameID, "Withdraw", "request", api.Request, "response", api.Response);
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
                    var apiCall = api.GetMemberInfo(gameMap.GamePlayerID);
                    InsertLog(gameID, "GetBalance", "request", api.Request, "response", api.Response);
                    targetApiUrl = api.TargetApiUrl;
                    request = api.Request;
                    response = api.Response;
                    if (apiCall)
                    {
                        res.PlayerID = playerID;
                        res.Balance = api.GetMemberInfoResponse.Data[0].Balance;
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
                var apiCall = api.GetMemberInfo(gameMap.GamePlayerID);
                InsertLog(gameID, "GetBalance", "request", api.Request, "response", api.Response);
                targetApiUrl = api.TargetApiUrl;
                request = api.Request;
                response = api.Response;
                if (apiCall)
                {
                    return Convert.ToDecimal(api.GetMemberInfoResponse.Data[0].Balance);
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

        public vm.GetGameUrlVM GetFishGameUrl(string langId,int gameId, bool IsMobile = false)
        {
            var res = new vm.GetGameUrlVM();
            try
            {
                if (gameMap != null)
                {
                    var sessionId = new Guid().ToString();
                    langId = GetGameLanguage(gameID, langId);

                    var apiCall = api.LoginWithoutRedirect(gameMap.GamePlayerID,gameId, langId);
                    InsertLog(gameID, "GetGameUrl", "request", api.Request, "Parameters", api.Parameters, "response", api.Response);
                    targetApiUrl = api.TargetApiUrl;
                    request = api.Request;
                    response = api.Response;
                    if (apiCall)
                    {
                        res.PlayerID = playerID;
                        res.Url = string.Format(api.LoginWithoutRedirectResponse.Data, "", IsMobile, langId);
                        res.Game = gameID;
                        res.Message = "Success";
                        res.Code = 0;
                    }
                    else
                    {
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
                _common.LogError("UserLogin" + " " + DateTime.Now.ToString() + " " + string.Format("{0}", playerID) + " " + ex.Message.ToString(), gameID);
                var err = new Error
                {
                    ErrorCode = "999",
                    ErrorDesc = ex.Message.ToString()
                };
                InsertLog(gameID, "Login", "Error", JsonConvert.SerializeObject(err));
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
                var apiCall = api.KickMember(gameMap.GamePlayerID);
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

        public GameRecord.Gamess.JiLi.GetBetRecordByTimeResponse GetBetRecordByTime(DateTime dateFrom, DateTime dateTo, int page, int pageLimit, bool filterAgent, int? gameID = null)
        {
            api = new GameRecord.Gamess.JiLi.JiLi(apiUrl, agentId, agentKey, timeOutSec);
            var apiCall = api.GetBetRecordByTime(dateFrom, dateTo, page, pageLimit, filterAgent, gameID);
            targetApiUrl = api.TargetApiUrl;
            request = api.Request;
            response = api.Response;
            if (apiCall)
            {
                return api.GetBetRecordByTimeResponse;
            }
            return null;
        }

        public GetGameListDtoResponse GetGameList()
        {
            if (api == null)
                api = new GameRecord.Gamess.JiLi.JiLi(apiUrl, agentId, agentKey, timeOutSec);
            var apiCall = api.GetGameList();
            targetApiUrl = api.TargetApiUrl;
            request = api.Request;
            response = api.Response;
            var data = api.GetGameListResponse.Data.Where(x => x.GameCategoryId == 5).Select(x => 
            new GetGameListDto
            {
                GameId = x.GameId,
                GameName = x.name.enUS,
                GameNameCn = x.name.zhCN,
                GameCategoryId = x.GameCategoryId,
                JP = x.JP,
                Freespin = x.Freespin
            }).ToList();

            GetGameListDtoResponse responseDto = new GetGameListDtoResponse();
            responseDto.Data = data;

            if (apiCall)
            {
                return responseDto;
            }
            else
            {
                return null;
            }
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
    }
}