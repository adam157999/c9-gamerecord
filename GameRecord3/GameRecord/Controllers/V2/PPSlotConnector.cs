using GameRecord.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Pragmatic.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using vm = GameRecord.Controllers.GameAPIController;

namespace GameRecord.Controllers.V2
{
    public class PPSlotConnector : BaseConnector
    {
        // GET: JiLiConnector
        private string gameUniqueID = "vs50aladdin";
        private string gameID = "PPSLOT";
        private string apiUrl = "";
        private string imgUrl = "";
        private string secureLogin = "";
        private string secretKey = "";
        private int timeOutSec = 30;
        private GameMapping gameMap;
        private Pragmatic.DLL.Pragmatic api = null;
        //private PPSlot.PPSlot apiFRB = null;
        private string lobbyUrl = HttpContext.Current.Request.Url.AbsoluteUri.Replace(HttpContext.Current.Request.Url.PathAndQuery, "/") + "PPSlot/Lobby";

        public PPSlotConnector(string PlayerID = "", string CurrencyID = "MYR", string IPAddr = "", GameEnvironment Environment = GameEnvironment.PROD) : base(PlayerID, CurrencyID, IPAddr, Environment)
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
                    apiUrl = jObj["PragmaticApiUrl"].ToString();
                    imgUrl = jObj["PragmaticImgUrl"].ToString();
                    secretKey = jObj["PragmaticSecretKey"].ToString();
                    secureLogin = jObj["PragmaticSecureLogin"].ToString();
                    timeOutSec = Convert.ToInt32(jObj["PragmaticTimeOutSec"].ToString());

                    api = new Pragmatic.DLL.Pragmatic(apiUrl, secretKey, timeOutSec);
                }
            }
        }

        public override vm.CheckAndCreateAccountVM CreatePlayer(string loginID, string loginPass, string fullName, string mcUID)
        {
            throw new NotImplementedException();
        }

        public override bool GameDeposit(decimal Amount, string TransactionID, ref Error Error)
        {
            throw new NotImplementedException();
        }

        public override bool GameWithdraw(decimal Amount, string TransactionID, ref Error Error)
        {
            throw new NotImplementedException();
        }

        public override vm.GetBalanceVM GetBalance()
        {
            throw new NotImplementedException();
        }

        public override decimal? GetGameBalance()
        {
            throw new NotImplementedException();
        }

        public override vm.GetGameUrlVM GetGameUrl(string langId, bool IsMobile = false)
        {
            throw new NotImplementedException();
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

        public vm.GetGameUrlVM GetGameSeamlessUrl(string langId, string token, bool IsMobile = false)
        {
            var res = new vm.GetGameUrlVM();

            try
            {
                InsertLog(gameID, "GetGameSeamlessUrlPPSLOT", "langId", langId, "IsMobile", IsMobile, "token", token, "gameID", gameID);

                langId = GetGameLanguage(gameID, langId);

                string url = string.Format("{0}?username={1}&langid={2}&token={3}", lobbyUrl, playerID, langId, token);

                InsertLog(gameID, "GetGameSeamlessUrlPPSLOT", "langId", langId, "IsMobile", IsMobile, "token", token, "gameID", gameID, "url", url);

                res.PlayerID = playerID;
                res.Url = url;
                res.Game = gameID;
                res.Message = "Success";
                res.Code = 0;
            }
            catch (Exception ex)
            {
                _common.LogError("GetGameSeamlessUrlPPSLOT" + " " + DateTime.Now.ToString() + " " + string.Format("{0}", playerID) + " " + ex.Message.ToString(), gameID);

                var err = new Error
                {
                    ErrorCode = "999",
                    ErrorDesc = ex.Message.ToString()
                };

                InsertLog(gameID, "GetGameSeamlessUrlPPSLOT", "Error", JsonConvert.SerializeObject(err));

                res.PlayerID = playerID;
                res.Url = null;
                res.Game = gameID;
                res.Message = ex.Message.ToString();
                res.Code = 99;
            }

            _common.InsertLog("GetGameSeamlessUrl", "langId", langId, "token", token, "IsMobile", IsMobile);

            return res;
        }

        public Pragmatic.Model.GetGameRoundsResponse RetrieveTransactionByTime(string timePoint, string dataType)
        {

            var apiCall = api.GetGameRounds(secureLogin, timePoint, dataType);

            targetApiUrl = api.Request;
            request = api.Request;
            response = api.Response;

            if (apiCall)
            {
                return api.GetGameResponse;
            }
            return null;
        }

        public Pragmatic.Model.GetCasinoGamesResponseV2 GetGamesList()
        {
            var apiCall = api.GetCasinoGames(secureLogin);

            targetApiUrl = api.Request;
            request = api.Request;
            response = api.Response;

            if (apiCall)
            {
                Pragmatic.Model.GetCasinoGamesResponseV2 result = new Pragmatic.Model.GetCasinoGamesResponseV2()
                {
                    gameList = api.GetCasinoGamesResponse.gameList,
                    error = api.GetCasinoGamesResponse.error,
                    description = api.GetCasinoGamesResponse.description,
                    imgUrl = imgUrl
                };
                return result;
            }
            return null;
        }

        //lobby
        public vm.GetBalanceVM GetBalanceLobby(string username)
        {
            var res = new vm.GetBalanceVM();

            try
            {
                if (!string.IsNullOrEmpty(username))
                {
                    using (BingoConn db = new BingoConn())
                    {
                        var foundRecord = db.SeamlessWallets.SingleOrDefault(x => x.Game == gameID && x.Username == username);
                        if (foundRecord != null)
                        {
                            res.PlayerID = playerID;
                            res.Balance = foundRecord.Cash + foundRecord.Chip;
                            res.Game = gameID;
                            res.Message = "Successfully get balance.";
                            res.Code = 0;
                        }
                        else
                        {
                            res.PlayerID = playerID;
                            res.Balance = null;
                            res.Game = gameID;
                            res.Message = "Failed to get balance";
                            res.Code = 2;
                        }
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

        public vm.GetGameUrlVM GetSlotGameUrl(string username, string langId, string gameCode, string token)
        {
            var res = new vm.GetGameUrlVM();

            try
            {
                if (!string.IsNullOrEmpty(username))
                {
                    //var token = new Guid().ToString();
                    //langId = GetGameLanguage(gameID, langId);
                    //string url = string.Format("{0}?username={1}&langid={2}&token={3}", lobbyUrl, username, langId, token);
                    var apiCall = api.StartGameSeamless(secureLogin, token, gameCode, langId, "");
                    InsertLog(gameID, "GetSlotGameUrl", "request", api.TargetApiUrl, "response", api.Response, "parameter", api.Parameters);

                    targetApiUrl = api.Request;
                    request = api.Request;
                    response = api.Response;

                    if (apiCall)
                    {
                        res.PlayerID = playerID;
                        res.Url = string.Format(api.StartGameSeamlessResponse.gameURL);
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

        public (bool success, string message) AddFRBPlayers(string bonusCode, List<string> players)
        {
            try
            {
                bool success = api.AddPlayerFRB(secureLogin, bonusCode, players);
                //bool success = true;
                InsertLog(gameID, "AddPlayerFRB", "request", api.TargetApiUrl, "response", api.Response, "parameter", api.Parameters);
                
                string message = "";

                if (!success || api.ParseError)
                    message = api.ErrorMessage;

                return (success, message);
            }
            catch (Exception ex)
            {
                _common.LogError("AddPlayerFRB" + " " + DateTime.Now.ToString() + " " + string.Format("{0}", playerID) + " " + ex.Message.ToString(), gameID);

                var err = new Error
                {
                    ErrorCode = "999",
                    ErrorDesc = ex.Message.ToString()
                };

                InsertLog(gameID, "AddPlayerFRB", "Error", JsonConvert.SerializeObject(err));
                throw;
            }
        }

        public (bool success, string message) RemoveFRBPlayers(string bonusCode, List<string> players)
        {
            try
            {
                bool success = api.RemovePlayerFRB(secureLogin, bonusCode, players);
                //bool success = true;
                InsertLog(gameID, "RemovePlayerFRB", "request", api.TargetApiUrl, "response", api.Response, "parameter", api.Parameters);
                
                string message = "";

                if (!success || api.ParseError)
                    message = api.ErrorMessage;

                return (success, message);
            }
            catch (Exception ex)
            {
                _common.LogError("RemovePlayerFRB" + " " + DateTime.Now.ToString() + " " + string.Format("{0}", playerID) + " " + ex.Message.ToString(), gameID);

                var err = new Error
                {
                    ErrorCode = "999",
                    ErrorDesc = ex.Message.ToString()
                };

                InsertLog(gameID, "RemovePlayerFRB", "Error", JsonConvert.SerializeObject(err));
                throw;
            }
        }

        public (bool success, string message) GetPlayerFRB(string playerId)
        {
            try
            {
                bool success = api.GetPlayerFRB(secureLogin, playerId);
                //bool success = true;
                InsertLog(gameID, "GetPlayerFRB", "request", api.TargetApiUrl, "response", api.Response, "parameter", api.Parameters);
                
                string message = "";

                if (!success || api.ParseError)
                {
                    message = api.ErrorMessage;
                }
                else
                {
                    message = api.Response;

                }

                return (success, message);
            }
            catch (Exception ex)
            {
                _common.LogError("GetPlayerFRB" + " " + DateTime.Now.ToString() + " " + string.Format("{0}", playerID) + " " + ex.Message.ToString(), gameID);

                var err = new Error
                {
                    ErrorCode = "999",
                    ErrorDesc = ex.Message.ToString()
                };

                InsertLog(gameID, "GetPlayerFRB", "Error", JsonConvert.SerializeObject(err));
                throw;
            }
        }
    }
}