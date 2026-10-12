using GameRecord.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data;
using vm = GameRecord.Controllers.GameAPIController;

namespace GameRecord.Controllers
{
    public class PlayStarConnector : BaseConnector
    {
        // GET: NextSpinConnector
        private string gameID = "PLAYSTAR";
        private string apiUrl = "";
        private string gameUrl = "";
        private string hostId = "";
        private string secretKey = "";
        private string currency = "";
        private int timeOutSec = 30;
        private GameMapping gameMap;
        private PlayStar.PlayStar api = null;
        private string lobbyUrl = HttpContext.Current.Request.Url.AbsoluteUri.Replace(HttpContext.Current.Request.Url.PathAndQuery, "/") + "PlayStar/Lobby";

        Common _common = new Common();

        public PlayStarConnector(string PlayerID = "", string CurrencyID = "", string IPAddr = "", GameEnvironment Environment = GameEnvironment.PROD) : base(PlayerID, CurrencyID, IPAddr, Environment)
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
                    apiUrl = jObj["PlayStarApiUrl"].ToString();
                    gameUrl = jObj["PlayStarGameUrl"].ToString();
                    hostId = jObj["PlayStarHostId"].ToString();
                    currency = jObj["PlayStarCurrency"].ToString();
                    timeOutSec = Convert.ToInt32(jObj["PlayStarTimeOutSec"].ToString());
                    api = new PlayStar.PlayStar(apiUrl, hostId, currency, timeOutSec);
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
                    //decimal amt = Convert.ToDecimal(0.01);
                    var apiCheck = api.CreatePlayer(loginID);
                    InsertLog(gameID, "CreateUser", "request", api.Request, "response", api.Response, "ExternalIP", api.ExternalIP);

                    request = api.Request;
                    response = api.Response;
                    string error = api.ErrorMessage;


                    if (apiCheck)
                    {
                        //var apiCall = api.CreateMember(loginID);
                        InsertGameMapping(gameID, loginID, loginPass, playerID);
                        gameMap = LoadGameMap(gameID, playerID, currencyID);
                        //var apiCheck1 = api.Withdraw(loginID, amt);

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
                long amount = Convert.ToInt64(Amount * 100);
                var apiCall = api.Deposit(gameMap.GamePlayerID, amount);
                InsertLog(gameID, "Deposit", "request", api.Request, "response", api.Response, "ExternalIP", api.ExternalIP);

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
                long amount = Convert.ToInt64(Amount * 100);
                var apiCall = api.Withdraw(gameMap.GamePlayerID, amount);
                InsertLog(gameID, "Withdraw", "request", api.Request, "response", api.Response, "ExternalIP", api.ExternalIP);

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
                    InsertLog(gameID, "GetBalance", "request", api.Request, "response", api.Response, "ExternalIP", api.ExternalIP);

                    targetApiUrl = api.Request;
                    request = api.Request;
                    response = api.Response;

                    if (apiCall)
                    {
                        decimal balanceDivided = Convert.ToDecimal(api.GetBalanceRes.balance) / 100m;
                        res.PlayerID = playerID;
                        res.Balance = Convert.ToDecimal(balanceDivided);
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
                var apiCall = api.GetBalance(gameMap.GamePlayerID);
                InsertLog(gameID, "GetBalance", "request", api.Request, "response", api.Response, "ExternalIP", api.ExternalIP);

                targetApiUrl = api.Request;
                request = api.Request;
                response = api.Response;

                if (apiCall)
                {
                    decimal balanceDivided = Convert.ToDecimal(api.GetBalanceRes.balance) / 100m;
                    return Convert.ToDecimal(balanceDivided);
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
                    string token = GetToken().ToLower();

                    InsertPlayStarGamingToken(gameMap.PlayerID, gameMap.GamePlayerID, token);
                    InsertLog(gameID, "GetGameUrl", "request", api.Request, "response", api.Response, "ExternalIP", api.ExternalIP);

                    var sessionId = new Guid().ToString();
                    langId = GetGameLanguage(gameID, langId);

                    string url = string.Format(gameUrl, hostId, "PS-LOBBY", langId, token);

                    InsertLog(gameID, "GetGameUrlPlayStar", "langId", langId, "IsMobile", IsMobile, "gameID", gameID, "url", url);

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
                _common.LogError("GetGameUrlPlayStar" + " " + DateTime.Now.ToString() + " " + string.Format("{0}", playerID) + " " + ex.Message.ToString(), gameID);

                var err = new Error
                {
                    ErrorCode = "999",
                    ErrorDesc = ex.Message.ToString()
                };

                InsertLog(gameID, "GetGameUrlPlayStar", "Error", JsonConvert.SerializeObject(err));

                res.PlayerID = playerID;
                res.Url = null;
                res.Game = gameID;
                res.Message = ex.Message.ToString();
                res.Code = 99;
            }

            return res;
        }

        public vm.GetGameUrlVM GetSlotGameUrl(string username, string langId, string gameCode)
        {
            var res = new vm.GetGameUrlVM();

            try
            {
                if (gameMap != null)
                {
                    langId = GetGameLanguage(gameID, langId);
                    //string token = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
                    string token = GetToken().ToLower();

                    var apiLoginCall = api.GetGameUrl(gameCode, langId, token);
                    InsertPlayStarGamingToken(gameMap.PlayerID, gameMap.GamePlayerID, token);
                    InsertLog(gameID, "GetGameUrl", "request", api.Request, "response", api.Response, "ExternalIP", api.ExternalIP);

                    targetApiUrl = api.Request;
                    request = api.Request;
                    response = api.Response;

                    if (apiLoginCall)
                    {
                        res.PlayerID = playerID;
                        res.Url = api.GameLoginRes.url;
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


        //lobby
        public vm.GetBalanceVM GetBalanceLobby(string username)
        {
            var res = new vm.GetBalanceVM();

            try
            {
                if (!string.IsNullOrEmpty(username))
                {
                    var apiCall = api.GetBalance(username);
                    InsertLog(gameID, "GetBalance", "request", api.Request, "response", api.Response, "ExternalIP", api.ExternalIP);

                    targetApiUrl = api.Request;
                    request = api.Request;
                    response = api.Response;

                    if (apiCall)
                    {
                        res.PlayerID = playerID;
                        res.Balance = api.GetBalanceRes.balance;
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

        public PlayStar.GetGameListRes GetGamesList()
        {
            var apiCall = api.GetGameList();
            InsertLog(gameID, "GetGamesList", "request", api.Request, "response", api.Response, "ExternalIP", api.ExternalIP);

            targetApiUrl = api.Request;
            request = api.Request;
            response = api.Response;

            if (apiCall)
            {
                return api.GetGameListRes;
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


        public void InsertPlayStarGamingToken(string playerID, string gamePlayerID, string token)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(BingoConn))
                {
                    con.Open();
                    SqlCommand cmd = new SqlCommand("sp_PlayStarGameToken_Insert", con);
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

        public string GetToken()
        {
            Random random = new Random();
            string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, 20)
                    .Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}