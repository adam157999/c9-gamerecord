using GameRecord.Gamess.BeSoft;
using GameRecord.Gamess.BeSoft.Models;
using GameRecord.Gamess.EvoGaming;
using GameRecord.Gamess.OctoPlay;
using GameRecord.Gamess.OctoPlay.Models;
using GameRecord.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Web;
using vm = GameRecord.Controllers.GameAPIController;

namespace GameRecord.Controllers.V2
{
    public class BeSoftConnector : BaseConnector
    {
        private string gameID = "BESOFT";
        private string apiUrl = "";
        private string gameUrl = "";
        private string cert = "";
        private string agentId = "";
        private int timeout = 30;
        private GameMapping gameMap;
        private BeSoft api = null;
        private string lobbyUrl = HttpContext.Current.Request.Url.AbsoluteUri.Replace(HttpContext.Current.Request.Url.PathAndQuery, "/") + "BeSoft/Lobby";

        public BeSoftConnector(string playerID = "", string currencyID = "", string ipAddr = "", GameEnvironment environment = GameEnvironment.PROD)
            : base(playerID, currencyID, ipAddr, environment)
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
                    apiUrl = jObj["BeSoftApiUrl"].ToString();
                    cert = jObj["BeSoftCert"].ToString();
                    agentId = jObj["BeSoftAgentId"].ToString();
                    timeout = int.Parse(jObj["BeSoftTimeOutSec"].ToString());
                    gameUrl = jObj["BeSoftGameUrl"].ToString();

                    api = new BeSoft(apiUrl, cert, agentId, timeout);
                }
            }
        }

        public override vm.CheckAndCreateAccountVM CreatePlayer(string loginID, string loginPass, string fullName, string mcUID)
        {
            var res = new vm.CheckAndCreateAccountVM();

            try
            {
                var getKeyRequest = new GetKeyRequest
                {
                    User = loginID
                };

                if (api.GetKey(getKeyRequest))
                {
                    InsertLog(gameID, "CreateUser", "request", JsonConvert.SerializeObject(getKeyRequest), "response", JsonConvert.SerializeObject(api.GetKeyResponse));

                    InsertGameMapping(gameID, loginID, loginPass, mcUID);

                    res.PlayerID = playerID;
                    res.GamePlayerID = loginID;
                    res.Game = gameID;
                    res.Message = "Success";
                    res.Code = 0;
                }
                else
                {
                    InsertLog(gameID, "CreateUser", "request", JsonConvert.SerializeObject(getKeyRequest), "response", JsonConvert.SerializeObject(api.ErrorMessage));

                    res.PlayerID = playerID;
                    res.GamePlayerID = null;
                    res.Game = gameID;
                    res.Message = api.ErrorMessage;
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
            amount = -amount;
            var withdrawRequest = new WithdrawRequest
            {
                Cert = cert,
                User = gameMap.GamePlayerID,
                Extension1 = agentId,
                Score = amount.ToString(),
                TraceId = transactionID
            };
            if (api.Withdraw(withdrawRequest))
            {
                return true;
            }
            else
            {
                error = new Error { ErrorCode = "-9999", ErrorDesc = api.ErrorMessage };
                return false;
            }
        }

        public override bool GameWithdraw(decimal amount, string transactionID, ref Error error)
        {
            var withdrawRequest = new WithdrawRequest
            {
                Cert = cert,
                User = gameMap.GamePlayerID,
                Extension1 = agentId,
                Score = amount.ToString(),
                TraceId = transactionID
            };
            if (api.Withdraw(withdrawRequest))
            {
                return true;
            }
            else
            {
                error = new Error { ErrorCode = "-9999", ErrorDesc = api.ErrorMessage };
                return false;
            }
        }

        public override vm.GetBalanceVM GetBalance()
        {
            var res = new vm.GetBalanceVM();
            var balanceRequest = new GetBalanceRequest
            {
                Cert = cert,
                Extension1 = agentId,
                Users = gameMap.GamePlayerID
            };
            if (api.GetBalance(balanceRequest))
            {
                res.PlayerID = playerID;
                res.Balance = decimal.TryParse(api.GetBalanceResponse?.TotalScore, out var balance) ? balance : (decimal?)null;
                res.Game = gameID;
                res.Message = "Success";
                res.Code = 0;
            }
            else
            {
                res.PlayerID = playerID;
                res.Balance = null;
                res.Game = gameID;
                res.Message = api.ErrorMessage;
                res.Code = 1;
            }
            return res;
        }

        public override decimal? GetGameBalance()
        {
            var balanceRequest = new GetBalanceRequest
            {
                Cert = cert,
                Extension1 = agentId,
                Users = gameMap.GamePlayerID
            };
            if (api.GetBalance(balanceRequest))
            {
                return decimal.TryParse(api.GetBalanceResponse?.TotalScore, out var balance) ? balance : (decimal?)null;
            }
            else
            {
                return null;
            }
        }

        public vm.GetGameUrlVM GetBeSoftGameUrl(string langId, string gid, bool isMobile = false)
        {
            var res = new vm.GetGameUrlVM();
            var keyRequest = new GetKeyRequest
            {
                Cert = cert,
                User = gameMap.GamePlayerID,
                Extension1 = agentId
            };
            if (api.GetKey(keyRequest))
            {
                var url =  api.GetKeyResponse.ReturnUrl + "&gid=" + gid + "&lang=" + (string.IsNullOrEmpty(langId) ? "en" : langId.ToLower());
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
                res.Message = api.ErrorMessage;
                res.Code = 1;
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
                    string url = string.Format("{0}?playerid={1}&currency={2}&ip={3}&lang={4}&mobile={5}", lobbyUrl, playerID, currencyID, ipAddr, langId, isMobile.ToString().ToLower());
                    InsertLog(gameID, "GetGameUrl", "playerid", playerID, "currency", currencyID, "ip", ipAddr, "lang", langId, "url", url);
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
            var kickRequest = new KickOutPlayerRequest
            {
                Extension1 = agentId,
                Cert = cert,
                User = gamePlayerId
            };
            if (api.KickOutPlayer(kickRequest))
            {
                return true;
            }
            else
            {
                error = new Error { ErrorCode = "998", ErrorDesc = api.ErrorMessage };
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

        public List<BeSoftLobbyGame> GetGameList()
        {
            api = new BeSoft(apiUrl, cert, agentId, timeout);

            var gameListRequest = new GetGameListRequest
            {
                Extension1 = agentId
            };

            var apiCall = api.GetGameList(gameListRequest);

            if (apiCall && api.GetGameListResponse != null && api.GetGameListResponse.GameList != null)
            {
                InsertLog(gameID, "GetGameList", "request", JsonConvert.SerializeObject(gameListRequest), "response", JsonConvert.SerializeObject(api.GetGameListResponse));
                string gameIconBaseUrl = api.GetGameListResponse.Gameicourl;
                var data = api.GetGameListResponse.GameList
                    .Select(x =>
                        new BeSoftLobbyGame
                        {
                            AllSeq = 1,
                            Gid = x.Gid,
                            Name = x.Name,
                            GameType = x.GameType,
                            GameIcon = gameIconBaseUrl + $"/{x.Gid}_game_icon_300x300_en.png",
                            GameIconCn = gameIconBaseUrl + $"/{x.Gid}_game_icon_300x300_zh.png",
                            HotSeq = 1,
                            IsHot = false,
                            IsAll = true
                        })
                    .ToList();

                return data;
            }
            else
            {
                return new List<BeSoftLobbyGame>();
            }
        }
    }
}