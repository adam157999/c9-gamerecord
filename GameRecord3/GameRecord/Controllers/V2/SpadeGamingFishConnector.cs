using GameRecord.Gamess.SpadeGaming;
using GameRecord.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using static GameRecord.Gamess.SpadeGaming.SpadeGamingFish;
using vm = GameRecord.Controllers.GameAPIController;

namespace GameRecord.Controllers.V2
{
    public class SpadeGamingFishConnector : BaseConnector
    {
        private string gameID = "SPADEGAMINGFISH";
        private string apiUrl = "";
        private string gameUrl = "";
        private string gameLaunchUrl = "";
        private string merchantCode = "";
        private string currency = "";
        private int timeOutSec = 30;
        private GameMapping gameMap;
        private SpadeGamingFish api = null;
        private string lobbyUrl = HttpContext.Current.Request.Url.AbsoluteUri.Replace(HttpContext.Current.Request.Url.PathAndQuery, "/") + "SpadeGamingFish/Lobby";

        public SpadeGamingFishConnector(string playerID = "", string currencyID = "", string ipAddr = "", GameEnvironment environment = GameEnvironment.PROD)
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
                    apiUrl = jObj["SpadeGamingApiUrl"].ToString();
                    gameUrl = jObj["SpadeGamingGameUrl"].ToString();
                    merchantCode = jObj["SpadeGamingMerchantCode"].ToString();
                    currency = jObj["SpadeGamingCurrency"].ToString();
                    timeOutSec = Convert.ToInt32(jObj["SpadeGamingTimeOutSec"].ToString());

                    api = new SpadeGamingFish(apiUrl, merchantCode, currency, timeOutSec);
                }
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

                InsertLog(gameID, "GetGameUrl", "Error", JsonConvert.SerializeObject(err));

                res.PlayerID = playerID;
                res.Url = null;
                res.Game = gameID;
                res.Message = ex.Message.ToString();
                res.Code = 99;
            }

            return res;
        }

        public vm.GetGameUrlVM GetFishGameUrl(string langId, string gameCode, string currency, bool IsMobile = false)
        {
            var res = new vm.GetGameUrlVM();

            try
            {
                if (gameMap != null)
                {
                    langId = GetGameLanguage(gameID, langId);
                    var acctInfo = new AcctInfo { acctId = gameMap.GamePlayerID, currency = currency};
                    string token = GetToken().ToLower();

                    InsertSpadeGamingGamingToken(gameMap.PlayerID, gameMap.GamePlayerID, token);
                    var apiCall = api.GetGameUrl(acctInfo, IsMobile, langId, token, gameCode, Utils.GetClientIp());

                    InsertLog(gameID, "GetFishGameUrl", "request", api.Request, "response", api.Response);

                    if (apiCall)
                    {
                        res.PlayerID = playerID;
                        res.Url = api.GetGameUrlResponses.gameUrl;
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
                _common.LogError("GetFishGameUrl" + " " + DateTime.Now.ToString() + " " + string.Format("{0}", playerID) + " " + ex.Message.ToString(), gameID);

                var err = new Error
                {
                    ErrorCode = "999",
                    ErrorDesc = ex.Message.ToString()
                };

                InsertLog(gameID, "GetFishGameUrl", "Error", JsonConvert.SerializeObject(err));

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
                var apiCall = api.KickAcct(gameMap.GamePlayerID);
                InsertLog(gameID, "KickPlayer", "request", api.Request, "response", api.Response);

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

        public override vm.CheckAndCreateAccountVM CreatePlayer(string loginID, string loginPass, string fullName, string mcUID)
        {
            var res = new vm.CheckAndCreateAccountVM();

            try
            {
                if (gameMap == null)
                {
                    decimal amt = Convert.ToDecimal(0.01);
                    var apiCheck = api.Deposit(loginID, amt);

                    targetApiUrl = api.TargetApiUrl;
                    request = api.Request;
                    response = api.Response;
                    string error = api.ErrorMessage;

                    if (apiCheck)
                    {
                        InsertGameMapping(gameID, loginID, loginPass, playerID);
                        gameMap = LoadGameMap(gameID, playerID, currencyID);
                        var apiCheck1 = api.Withdraw(loginID, amt);

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

        public override bool GameDeposit(decimal Amount, string RequestID, ref Error Error)
        {
            if (gameMap != null)
            {
                var apiCall = api.Deposit(gameMap.GamePlayerID, Amount);
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

        public override bool GameWithdraw(decimal Amount, string RequestID, ref Error Error)
        {
            if (gameMap != null)
            {
                var apiCall = api.Withdraw(gameMap.GamePlayerID, Amount);
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
                    var apiCall = api.GetAcctInfo(gameMap.GamePlayerID);
                    InsertLog(gameID, "GetBalance", "request", api.Request, "response", api.Response);

                    targetApiUrl = api.Request;
                    request = api.Request;
                    response = api.Response;

                    if (apiCall)
                    {
                        if (api.GetAcctInfoResponses.list.Count > 0)
                        {
                            res.PlayerID = playerID;
                            res.Balance = api.GetAcctInfoResponses.list[0].balance;
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
                var apiCall = api.GetAcctInfo(gameMap.GamePlayerID);
                InsertLog(gameID, "GetBalance", "request", api.Request, "response", api.Response);

                targetApiUrl = api.Request;
                request = api.Request;
                response = api.Response;

                if (apiCall)
                {
                    if (api.GetAcctInfoResponses.list.Count > 0)
                    {
                        return Convert.ToDecimal(api.GetAcctInfoResponses.list[0].balance);
                    }
                    else
                        return null;
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

        public override string RetryDeposit(decimal Amount, string TrackerId, ref Error Error)
        {
            throw new NotImplementedException();
        }

        public override string RetryWithdraw(decimal Amount, string TrackerId, ref Error Error)
        {
            throw new NotImplementedException();
        }


        public bool ValidateCallbackMerchantCode(string code)
        {
            if (merchantCode == code)
                return true;

            return false;
        }

        public string GetToken()
        {
            Random random = new Random();
            string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, 20)
                    .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        public string GetCurrency()
        {
            return currency;
        }

        public void InsertSpadeGamingGamingToken(string playerID, string gamePlayerID, string token)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(BingoConn))
                {
                    con.Open();
                    SqlCommand cmd = new SqlCommand("sp_SpadeGamingFishGameToken_Insert", con);
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.Add(new SqlParameter("@PlayerID", playerID));
                    cmd.Parameters.Add(new SqlParameter("@GamePlayerID", gamePlayerID));
                    cmd.Parameters.Add(new SqlParameter("@Token", token));

                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }
    }
}