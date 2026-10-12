using GameRecord.Gamess.JiLi;
using GameRecord.Gamess.VPOWER;
using GameRecord.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using vm = GameRecord.Controllers.GameAPIController;

namespace GameRecord.Controllers.V2
{
    public class VPowerFishConnector : BaseConnector
    {
        private string gameID = "VPOWERFISH";
        private string apiUrl = "";
        private string merchantSecret = "";
        private string merchantID = "";
        private string gameLaunchUrl = "";
        private VPOWERAPI api = null;
        private GameMapping gameMap;
        private string lobbyUrl = HttpContext.Current.Request.Url.AbsoluteUri.Replace(HttpContext.Current.Request.Url.PathAndQuery, "/") + "VPowerFish/Lobby";

        public VPowerFishConnector(string playerID = "", string currencyID = "", string ipAddr = "", GameEnvironment environment = GameEnvironment.PROD)
            : base(playerID, currencyID, ipAddr, environment)
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
                    apiUrl = jObj["APIUrl"].ToString();
                    merchantSecret = jObj["MerchantSecret"].ToString();
                    merchantID = jObj["MerchantID"].ToString();
                    gameLaunchUrl = jObj["GameLaunchUrl"].ToString();

                    api = new VPOWERAPI(apiUrl, merchantID, merchantSecret, gameLaunchUrl);
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
                    var apiCall = Task.Run(() => api.GetUserAccount(loginID)).Result;
                    if (apiCall)
                    {
                        InsertLog(gameID, "CreateMember", "request", "URL=" + api.finalUrl + ", " + api.Parameters, "response", JsonConvert.SerializeObject(api.Response), api.Request);
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
                        InsertLog(gameID, "CreateMember", "request", "URL=" + api.finalUrl + ", " + api.Parameters, "response", JsonConvert.SerializeObject(api.Response));
                        res.PlayerID = playerID;
                        res.GamePlayerID = null;
                        res.Game = gameID;
                        res.Message = JsonConvert.SerializeObject(api.ErrorMessage, Formatting.None);
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
                _common.LogError("CreateMember" + " " + DateTime.Now.ToString() + " " + string.Format("{0}", playerID) + " " + ex.Message.ToString(), gameID);

                var err = new
                {
                    ErrorCode = "999",
                    ErrorDesc = ex.Message.ToString()
                };

                InsertLog(gameID, "CreateMember", "Error", JsonConvert.SerializeObject(err));

                res.PlayerID = playerID;
                res.GamePlayerID = loginID;
                res.Game = gameID;
                res.Message = ex.Message.ToString();
                res.Code = 99;
            }

            return res;
        }

        public override vm.GetBalanceVM GetBalance()
        {
            var res = new vm.GetBalanceVM();

            try
            {
                if (gameMap != null)
                {
                    var apiCall = Task.Run(() => api.GetUserBalanceAsync(gameMap.GamePlayerID)).Result;

                    InsertLog(gameID, "GetBalance", "request", "URL=" + api.finalUrl + ", " + api.Parameters, "response", JsonConvert.SerializeObject(api.Response));

                    if (apiCall)
                    {
                        res.PlayerID = playerID;
                        res.Balance = api.GetUserBalanceRes.Credit;
                        res.Game = gameID;
                        res.Message = "Successfully get balance.";
                        res.Code = 0;
                    }
                    else
                    {
                        res.PlayerID = playerID;
                        res.Balance = null;
                        res.Game = gameID;
                        res.Message = JsonConvert.SerializeObject(api.ErrorMessage, Formatting.None);
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
                var apiCall = Task.Run(() => api.GetUserBalanceAsync(gameMap.GamePlayerID)).Result;

                InsertLog(gameID, "GetGameBalance", "request", "URL=" + api.finalUrl + ", " + api.Parameters, "response", JsonConvert.SerializeObject(api.Response));

                if (apiCall)
                {
                    return (decimal)api.GetUserBalanceRes.Credit;
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

        public override bool GameDeposit(decimal Amount, string TrackerId, ref Error Error)
        {
            if (gameMap != null)
            {
                var apiCall = Task.Run(() => api.TransferUserCreditAsync(gameMap.GamePlayerID, Amount)).Result;

                InsertLog(gameID, "GameDeposit", "request", "URL=" + api.finalUrl + ", " + api.Parameters, "response", JsonConvert.SerializeObject(api.Response));

                if (apiCall)
                {
                    return true;
                }
                else
                {
                    Error = new Error
                    {
                        ErrorCode = "-9999",
                        ErrorDesc = JsonConvert.SerializeObject(api.ErrorMessage, Formatting.None)
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
                var apiCall = Task.Run(() => api.TransferUserCreditAsync(gameMap.GamePlayerID, Amount * -1)).Result;

                InsertLog(gameID, "GameWithdraw", "request", "URL=" + api.finalUrl + ", " + api.Parameters, "response", JsonConvert.SerializeObject(api.Response));

                if (apiCall)
                {
                    return true;
                }
                else
                {
                    Error = new Error
                    {
                        ErrorCode = "-9999",
                        ErrorDesc = JsonConvert.SerializeObject(api.ErrorMessage, Formatting.None)
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

        public override vm.GetGameUrlVM GetGameUrl(string langId, bool IsMobile = true)
        {
            var res = new vm.GetGameUrlVM();

            try
            {
                if (gameMap != null)
                {
                    string url = string.Format("{0}?playerid={1}&currency={2}&ip={3}&lang={4}", lobbyUrl, playerID, currencyID, ipAddr, langId);

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

        public vm.GetGameUrlVM GetFishGameUrl(string langId, string gameCode, bool IsMobile = false)
        {
            var res = new vm.GetGameUrlVM();

            try
            {
                if (gameMap != null)
                {
                    langId = GetGameLanguage(gameID, langId);
                    var apiCall = Task.Run(() => api.GetUserTokenAsync(gameMap.GamePlayerID)).Result;

                    InsertLog(gameID, "GetGameLink", "request", "URL=" + api.finalUrl + ", " + api.Parameters, "response", api.Response);

                    if (apiCall)
                    {
                        res.PlayerID = playerID;
                        res.Url = api.LaunchGameAsync(gameCode.ToString(), api.GetUserTokenRes.Token, "");
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
                _common.LogError("GetGameLink" + " " + DateTime.Now.ToString() + " " + string.Format("{0}", playerID) + " " + ex.Message.ToString(), gameID);

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
                var apiCall = Task.Run(() => api.SignOutAsync(gameMap.GamePlayerID)).Result;

                InsertLog(gameID, "PlayerLogout", "request", "URL=" + api.finalUrl + ", " + api.Parameters, "response", api.Response);

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

        public GameRecord.Gamess.VPOWER.VPOWERAPI.GetGameListResponse GetFishingGames(string gameName = "", string lang = "")
        {
            var res = new GameRecord.Gamess.VPOWER.VPOWERAPI.GetGameListResponse();

            try
            {
                var apiCall = Task.Run(() => api.GetGameListAsync()).Result;

                InsertLog(gameID, "GetFishingGames", "request", "URL=" + api.finalUrl + ", " + api.Parameters, "response", JsonConvert.SerializeObject(api.Response));

                if (apiCall)
                {
                    res.GameList = api.GetGameListRes.GameList
                        .Where(game => game.GameType.Equals("Fishing", StringComparison.OrdinalIgnoreCase))
                        .ToList();

                    if(!string.IsNullOrEmpty(gameName))
                    {
                        if(lang.Equals("en"))
                        {
                            res.GameList = res.GameList.Where(g => g.GameNameEN.Contains(gameName)).ToList();
                        }
                        else
                        {
                            res.GameList = res.GameList.Where(g => g.GameName.Contains(gameName)).ToList();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _common.LogError("GetFishingGames" + " " + DateTime.Now.ToString() + " " + ex.Message.ToString(), gameID);
            }

            return res;
        }

        public override string RetryDeposit(decimal Amount, string TrackerId, ref BaseConnector.Error Error)
        {
            // Implement the logic for retrying a deposit here
            // For now, returning a placeholder string
            return "RetryDeposit not implemented.";
        }

        public override string RetryWithdraw(decimal Amount, string TrackerId, ref BaseConnector.Error Error)
        {
            // Implement the logic for retrying a withdrawal here
            // For now, returning a placeholder string
            return "RetryWithdraw not implemented.";
        }
    }
}