using GameRecord.Gamess.EvoGaming;
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
    public class EVONetEntConnector : BaseConnector
    {
        private string gameID = "EVONETENT";
        private string hostName = "";
        private string reportHost = "";
        private string merchantId = "";
        private string casinoKey = "";
        private string apiToken = "";
        private string country = "";
        private string brandId = "";
        private string skinId = "1";
        private string gameCategory = "slots";
        private string gameInterface = "view1";
        private string vipGroupAction = "assign";
        private string vipGroupId = "";
        private string gameListHost = "";
        private JObject urls;
        private int timeOutSec = 30;
        private GameMapping gameMap;
        private EVOGM api = null;
        private string lobbyUrl = HttpContext.Current.Request.Url.AbsoluteUri.Replace(HttpContext.Current.Request.Url.PathAndQuery, "/") + "EvoNetEnt/Lobby";

        public EVONetEntConnector(string PlayerID = "", string CurrencyID = "", string IPAddr = "", GameEnvironment Environment = GameEnvironment.PROD) : base(PlayerID, CurrencyID, IPAddr, Environment)
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
                    hostName = jObj["EVOGMHostName"].ToString();
                    reportHost = jObj["EVOGMReportHost"].ToString();
                    merchantId = jObj["EVOGMMerchantId"].ToString();
                    casinoKey = jObj["EVOGMCasinoKey"].ToString();
                    apiToken = jObj["EVOGMApiToken"].ToString();
                    country = jObj["EVOGMCountry"].ToString();
                    brandId = jObj["EVOGMBrandId"].ToString();
                    skinId = jObj["EVOGMSkinId"].ToString();
                    gameCategory = jObj["EVOGMGameCategory"].ToString();
                    gameInterface = jObj["EVOGMGameInterface"].ToString();
                    vipGroupAction = jObj["EVOGMVIPAction"].ToString();
                    vipGroupId = jObj["EVOGMVIPId"].ToString();
                    gameListHost = jObj["EVOGMGameListHost"].ToString();
                    urls = JObject.Parse(jObj["EVOGMUrls"].ToString());
                    timeOutSec = Convert.ToInt32(jObj["EVOGMTimeOutSec"].ToString());

                    api = new EVOGM(hostName, merchantId, casinoKey, apiToken, timeOutSec);
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
                    var sessionId = Utils.SubStringTrackerId(new Guid().ToString());
                    //if (!string.IsNullOrEmpty(ipAddr)) ipAddr = ipAddr.Split(':')[0];
                    ipAddr = Utils.GetClientIp();

                    var apiCall = api.UserAuth(loginID, loginID, loginID, country, "en", currencyID, sessionId, ipAddr, brandId, skinId, gameCategory, gameInterface, urls);
                    InsertLog(gameID, "UserAuth", "request", api.request, "response", api.response);

                    targetApiUrl = api.targetApiUrl;
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
                        res.Message = api.errorMessage;
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
                _common.LogError("UserAuth" + " " + DateTime.Now.ToString() + " " + string.Format("{0}", playerID) + " " + ex.Message.ToString(), gameID);

                var err = new
                {
                    ErrorCode = "999",
                    ErrorDesc = ex.Message.ToString()
                };

                InsertLog(gameID, "UserAuth_Exception", "Error", JsonConvert.SerializeObject(err));

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
                TrackerId = Utils.SubStringTrackerId(TrackerId);
                var apiCall = api.Deposit(gameMap.GamePlayerID, Amount, TrackerId);
                InsertLog(gameID, "Deposit", "request", api.request, "response", api.response);

                targetApiUrl = api.targetApiUrl;
                request = api.request;
                response = api.response;

                if (apiCall)
                {
                    Error = new Error
                    {
                        ErrorCode = "0",
                        ErrorDesc = ""
                    };
                    return true;
                }
                else
                {
                    Error = new Error
                    {
                        ErrorCode = "-9999",
                        ErrorDesc = api.errorMessage
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
                TrackerId = Utils.SubStringTrackerId(TrackerId);
                var apiCall = api.Withdraw(gameMap.GamePlayerID, Amount, TrackerId);
                InsertLog(gameID, "Withdraw", "request", api.request, "response", api.response);

                targetApiUrl = api.targetApiUrl;
                request = api.request;
                response = api.response;

                if (apiCall)
                {
                    Error = new Error
                    {
                        ErrorCode = "0",
                        ErrorDesc = ""
                    };
                    return true;
                }
                else
                {
                    Error = new Error
                    {
                        ErrorCode = "-9999",
                        ErrorDesc = api.errorMessage
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

                    targetApiUrl = api.targetApiUrl;
                    request = api.request;
                    response = api.response;

                    if (apiCall)
                    {
                        res.PlayerID = playerID;
                        res.Balance = api.GetBalanceSuccessResponse.tbalance;
                        res.Game = gameID;
                        res.Message = "Successfully get balance.";
                        res.Code = 0;
                    }
                    else
                    {
                        res.PlayerID = playerID;
                        res.Balance = null;
                        res.Game = gameID;
                        res.Message = api.errorMessage;
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
                InsertLog(gameID, "GetGameBalance", "request", api.request, "response", api.response);

                targetApiUrl = api.targetApiUrl;
                request = api.request;
                response = api.response;

                if (apiCall)
                {
                    return Convert.ToDecimal(api.GetBalanceSuccessResponse.tbalance);
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

        public vm.GetGameUrlVM GetEVONetEntGameUrl(string langId, string gameCode, bool IsMobile = false)
        {
            var res = new vm.GetGameUrlVM();

            try
            {
                if (gameMap != null)
                {
                    var sessionId = new Guid().ToString();
                    langId = GetGameLanguage(gameID, langId);
                    if (!string.IsNullOrEmpty(ipAddr)) ipAddr = ipAddr.Split(':')[0];
                    ipAddr = Utils.GetClientIp();

                    var apiCall = api.UserAuth(gameMap.GamePlayerID, playerID, playerID, country, langId, currencyID, sessionId, ipAddr, brandId, skinId, gameCategory, gameInterface, urls, IsMobile, true, vipGroupId, vipGroupAction, gameCode);
                    InsertLog(gameID, "UserAuth", "request", api.request, "response", api.response);

                    targetApiUrl = api.targetApiUrl;
                    request = api.request;
                    response = api.response;

                    if (apiCall)
                    {
                        res.PlayerID = playerID;
                        res.Url = api.UserAuthSuccessResponse.entry;
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
                _common.LogError("UserAuth" + " " + DateTime.Now.ToString() + " " + string.Format("{0}", playerID) + " " + ex.Message.ToString(), gameID);

                var err = new Error
                {
                    ErrorCode = "999",
                    ErrorDesc = ex.Message.ToString()
                };

                InsertLog(gameID, "UserAuth_Exception", "Error", JsonConvert.SerializeObject(err));

                res.PlayerID = playerID;
                res.Url = null;
                res.Game = gameID;
                res.Message = ex.Message.ToString();
                res.Code = 99;
            }

            return res;
        }

        public GameRecord.Gamess.EvoGaming.CasinoStreamGamesDetail.CasinoStreamGamesDetailResponse GetBetHistory(DateTime dateFrom, DateTime dateTo)
        {
            api = new EVOGM(reportHost, merchantId, casinoKey, apiToken, timeOutSec);

            var apiCall = api.CasinoStreamGamesDetail(dateFrom.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"), dateTo.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"));

            targetApiUrl = api.targetApiUrl;
            request = api.request;
            response = api.response;

            if (apiCall)
            {
                return api.CasinoStreamGamesDetailResponse;
            }
            return null;
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

        public List<EVONetEntLobbyGame> GetGameList(string gameId)
        {
            api = new EVOGM(hostName, merchantId, casinoKey, apiToken, timeOutSec, "", gameListHost);

            var apiCall = api.GetGameList();
            targetApiUrl = api.targetApiUrl;
            request = api.request;
            response = api.response;

            var data = api.GameListResponse.Data
                .Where(x => x.GameProvider == gameId)
                .Select(x =>
                    new EVONetEntLobbyGame
                    {
                        TableID = x.TableID,
                        TableName = x.TableName,
                        GameType = x.GameType,
                        GameProvider = x.GameProvider,
                        ThumbS = x.Thumbnails?.S,
                        ThumbM = x.Thumbnails?.M,
                        ThumbL = x.Thumbnails?.L,
                        ThumbXL = x.Thumbnails?.XL,
                        AllSeq = 1,
                        IsAll = true
                    })
                .ToList();

            if (apiCall)
            {
                return data;
            }
            else
            {
                return null;
            }
        }
    }
}