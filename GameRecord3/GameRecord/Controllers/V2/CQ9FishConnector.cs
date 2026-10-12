using GameRecord.Gamess.CQ9Fish;
using GameRecord.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using static GameRecord.Gamess.CQ9Fish.CQ9Fish;
using vm = GameRecord.Controllers.GameAPIController;

namespace GameRecord.Controllers.V2
{
    public class CQ9FishConnector : BaseConnector
    {
        private string gameID = "CQ9FISH";
        private string apiUrl = "";
        private string token = "";
        private int timeOutSec = 30;
        private GameMapping gameMap;
        private CQ9Fish api = null;
        private string lobbyUrl => HttpContext.Current.Request.Url.AbsoluteUri.Replace(HttpContext.Current.Request.Url.PathAndQuery, "/") + "CQ9FISH/Lobby";

        public CQ9FishConnector(string playerID = "", string currencyID = "", string ipAddr = "", GameEnvironment environment = GameEnvironment.PROD)
            : base(playerID, currencyID, ipAddr, environment)
        {
            using (var db = new BingoConn())
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
                    apiUrl = jObj["CQ9ApiUrl"].ToString();
                    token = jObj["CQ9Token"].ToString();
                    timeOutSec = Convert.ToInt32(jObj["CQ9TimeOutSec"].ToString());

                    api = new CQ9Fish(apiUrl, token, timeOutSec);
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
                    var apiCall = api.CreatePlayer(loginID, loginPass, loginID);
                    InsertLog(gameID, "CreatePlayer", "request", api.request, "response", api.response);

                    targetApiUrl = api.targetUrl;
                    request = api.request;
                    response = api.response;

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
                        res.Message = (api.CreatePlayerResponses != null ? api.CreatePlayerResponses.status.message : api.errorMessage);
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
                _common.LogError("CreatePlayer" + " " + DateTime.Now.ToString() + " " + string.Format("{0}", playerID) + " " + ex.Message.ToString(), gameID);

                var err = new
                {
                    ErrorCode = "999",
                    ErrorDesc = ex.Message.ToString()
                };

                InsertLog(gameID, "CreatePlayer_Exception", "Error", JsonConvert.SerializeObject(err));

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
                var apiCall = api.Deposit(gameMap.GamePlayerID, Amount);
                InsertLog(gameID, "Deposit", "request", api.request, "response", api.response);

                targetApiUrl = api.targetUrl;
                request = api.request;
                response = api.response;

                if (apiCall)
                {
                    return true;
                }
                else
                {
                    Error = new Error
                    {
                        ErrorCode = "-9999",
                        ErrorDesc = (api.DepositResponses != null ? api.DepositResponses.status.message : api.errorMessage)
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
                var apiCall = api.Withdrawal(gameMap.GamePlayerID, Amount);
                InsertLog(gameID, "Withdrawal", "request", api.request, "response", api.response);

                targetApiUrl = api.targetUrl;
                request = api.request;
                response = api.response;

                if (apiCall)
                {
                    return true;
                }
                else
                {
                    Error = new Error
                    {
                        ErrorCode = "-9999",
                        ErrorDesc = (api.WithdrawResponses != null ? api.WithdrawResponses.status.message : api.errorMessage)
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
                    InsertLog(gameID, "GetBalance", "request", api.request, "response", api.response);

                    targetApiUrl = api.targetUrl;
                    request = api.request;
                    response = api.response;

                    if (apiCall)
                    {
                        res.PlayerID = playerID;
                        res.Balance = Convert.ToDecimal(api.GetBalanceResponses.data.balance);
                        res.Game = gameID;
                        res.Message = "Successfully get balance.";
                        res.Code = 0;
                    }
                    else
                    {
                        res.PlayerID = playerID;
                        res.Balance = null;
                        res.Game = gameID;
                        res.Message = (api.GetBalanceResponses != null ? api.GetBalanceResponses.status.message : api.errorMessage);
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
                _common.LogError("GetBalance" + " " + DateTime.Now.ToString() + " " + string.Format("{0}", playerID) + " " + ex.Message.ToString(), gameID);

                var err = new Error
                {
                    ErrorCode = "999",
                    ErrorDesc = ex.Message.ToString()
                };

                InsertLog(gameID, "GetBalance_Exception", "Error", JsonConvert.SerializeObject(err));

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
                InsertLog(gameID, "GetBalance", "request", api.request, "response", api.response);

                targetApiUrl = api.targetUrl;
                request = api.request;
                response = api.response;

                if (apiCall)
                {
                    return Convert.ToDecimal(api.GetBalanceResponses.data.balance);
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
                    string url = string.Format("{0}?playerid={1}&currency={2}&ip={3}&lang={4}&mobile={5}",
                        lobbyUrl, playerID, currencyID, Utils.GetClientIp(), langId, IsMobile.ToString().ToLower());
                    InsertLog(gameID, "GetGameUrl", "playerid", playerID, "currency", currencyID, "ip", Utils.GetClientIp(), "lang", langId, "url", url);
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

        public vm.GetGameUrlVM GetFishGameUrl(string langId, string gameId, string gameHall, bool IsMobile = false)
        {
            var res = new vm.GetGameUrlVM();
            string gamePlat = "WEB";
            try
            {
                if (gameMap != null)
                {
                    var sessionId = new Guid().ToString();
                    langId = GetGameLanguage(gameID, langId);
                    if (IsMobile)
                    {
                        gamePlat = "MOBILE";
                    }

                    var apiCall = api.GetGameLink(gameMap.GamePlayerID,gameMap.GamePassword, gameHall, gameId, gamePlat, langId);
                    InsertLog(gameID, "GetGameUrl", "request", api.request, "Parameters", api.parameter, "response", api.response);
                    targetApiUrl = api.targetUrl;
                    request = api.request;
                    response = api.response;
                    if (apiCall)
                    {
                        res.PlayerID = playerID;
                        res.Url = string.Format(api.GetGameLinkResponses.data.url, "", IsMobile, langId);
                        res.Game = gameID;
                        res.Message = "Success";
                        res.Code = 0;
                    }
                    else
                    {
                        res.PlayerID = playerID;
                        res.Url = null;
                        res.Game = gameID;
                        res.Message = api.errorMessage;
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
            throw new NotImplementedException();
        }

        public override string RetryDeposit(decimal Amount, string TrackerId, ref Error Error)
        {
            throw new NotImplementedException();
        }

        public override string RetryWithdraw(decimal Amount, string TrackerId, ref Error Error)
        {
            throw new NotImplementedException();
        }

        public CQ9Fish.GetBetHistoryResponse GetBetHistory(DateTime dateFrom, DateTime dateTo, int page, int pagesize = 500)
        {
            var apiCall = api.GetBetHistory(dateFrom, dateTo, page, pagesize);

            targetApiUrl = api.targetUrl;
            request = api.request;
            response = api.response;

            if (apiCall)
            {
                return api.GetBetHistoryResponses;
            }
            return null;
        }

        //Add another method to get game list
        public List<CQ9FishLobbyGame> GetGameList(string gameHall)
        {
            var games = new List<CQ9FishLobbyGame>();
            try
            {
                var apiCall = api.GetGameList(gameHall);
                InsertLog(gameID, "GetGameList", "request", api.request, "response", api.response);
                targetApiUrl = api.targetUrl;
                request = api.request;
                response = api.response;
                if (apiCall)
                {
                    if (api.GetGameListResponses != null && api.GetGameListResponses.data != null)
                    {
                        var fishGame = api.GetGameListResponses.data.Where(x => x.gametype == "fish");
                        foreach (var item in fishGame)
                        {
                            var gameNameCn = item.nameset?.FirstOrDefault(n => n.lang == "zh-cn")?.name;

                            games.Add(new CQ9FishLobbyGame
                            {
                                GameType = item.gametype,
                                GameHall = item.gamehall,
                                Status = item.status,
                                Maintain = item.maintain,
                                GameCode = item.gamecode,
                                GameName = item.gamename,
                                GamePlat = item.gameplat,
                                GameNameCn = gameNameCn,
                                AllSeq = 1,
                                IsAll = true
                            });
                        }

                    }
                }
            }
            catch (Exception ex)
            {
                _common.LogError("GetGameList" + " " + DateTime.Now.ToString() + " " + string.Format("{0}", playerID) + " " + ex.Message.ToString(), gameID);
            }
            return games;
        }

        // Add method to get game hall list
        public GetGameProviderListResponse GetGameProviderList()
        {
            try
            {
                var apiCall = api.GetGameProviderList();
                InsertLog(gameID, "GetGameHallList", "request", api.request, "response", api.response);
                targetApiUrl = api.targetUrl;
                request = api.request;
                response = api.response;
                if (apiCall)
                {
                    if (api.GetGameProviderListResponses != null && api.GetGameProviderListResponses.data != null)
                    {
                        return api.GetGameProviderListResponses;
                    }
                }
            }
            catch (Exception ex)
            {
                _common.LogError("GetGameHallList" + " " + DateTime.Now.ToString() + " " + string.Format("{0}", playerID) + " " + ex.Message.ToString(), gameID);
            }

            return null;
        }

    }
}