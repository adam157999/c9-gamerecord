using GameRecord.Gamess.MicroGaming;
using GameRecord.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using static GameRecord.Gamess.MicroGaming.MGAPI;
using vm = GameRecord.Controllers.GameAPIController;

namespace GameRecord.Controllers
{
    public class MGConnector : BaseConnector
    {
        private int gameUniqueID = 0;
        private string gameID = "MG";

        private string api_url = "";
        private string token_url = "";
        private string agent_code_id = "";
        private string api_secret_key = "";
        private MGAPI api = null;
        private GameMapping gameMap;
        private string lobbyUrl = HttpContext.Current.Request.Url.AbsoluteUri.Replace(HttpContext.Current.Request.Url.PathAndQuery, "/") + "MG/Lobby";

        public MGConnector(string PlayerID = "", string CurrencyID = "", string IPAddr = "", GameEnvironment Environment = GameEnvironment.PROD) : base(PlayerID, CurrencyID, IPAddr, Environment)
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
                    api_url = jObj["api_url"].ToString();
                    token_url = jObj["token_url"].ToString();
                    agent_code_id = jObj["agent_code_id"].ToString();
                    api_secret_key = jObj["api_secret_key"].ToString();

                    api = new MGAPI(api_url, token_url, agent_code_id, api_secret_key);
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
                    var apiCall = Task.Run(() => api.PostPlayerAsync(loginID)).Result;
                    if (apiCall)
                    {
                        InsertLog(gameID, "CreateUser", "request", "URL=" + api.finalUrl + ", " + api.Parameters, "response", JsonConvert.SerializeObject(api.Response));
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
                        res.Message = JsonConvert.SerializeObject(api.ErrorRes, Formatting.None);
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

        public override string RetryDeposit(decimal Amount, string TrackerId, ref Error Error)
        {
            throw new NotImplementedException();
        }

        public override string RetryWithdraw(decimal Amount, string TrackerId, ref Error Error)
        {
            throw new NotImplementedException();
        }

        public override vm.GetBalanceVM GetBalance()
        {
            var res = new vm.GetBalanceVM();

            try
            {
                if (gameMap != null)
                {

                    var apiCall = Task.Run(() => api.GetPlayerAsync(gameMap.GamePlayerID, true)).Result;


                    InsertLog(gameID, "GetBalance", "request", "request", "URL=" + api.finalUrl + ", " + api.Parameters, "response", JsonConvert.SerializeObject(api.Response));

                    if (apiCall)
                    {

                        res.PlayerID = playerID;
                        //res.Balance = (decimal)api.GetPlayerRes.balance.details.walletBalance.total;
                        res.Balance = (decimal)api.GetPlayerRes.balance.total;
                        res.Game = gameID;
                        res.Message = "Successfully get balance.";
                        res.Code = 0;
                    }
                    else
                    {
                        res.PlayerID = playerID;
                        res.Balance = null;
                        res.Game = gameID;
                        res.Message = JsonConvert.SerializeObject(api.ErrorRes, Formatting.None);
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

        public vm.GetGameUrlVM GetSlotGameUrl(string langId, string gameCode, string platform)
        {
            var res = new vm.GetGameUrlVM();

            try
            {
                if (gameMap != null)
                {
                    langId = GetGameLanguage(gameID, langId);


                    var apiCall = Task.Run(() => api.PostPlayerSessionAsync(gameMap.GamePlayerID, gameCode, platform, langId)).Result;


                    InsertLog(gameID, "GetSlotGameUrl", "request", "URL=" + api.finalUrl + ", " + api.Parameters, "response", api.Response);

                    targetApiUrl = api.finalUrl;
                    request = api.Parameters;
                    response = api.Response;

                    if (apiCall)
                    {
                        res.PlayerID = playerID;
                        res.Url = api.PostPlayerSessionRes.gameURL;
                        res.Game = gameID;
                        res.Message = "Success";
                        res.Code = 0;
                    }
                    else
                    {
                        res.PlayerID = playerID;
                        res.Url = null;
                        res.Game = gameID;
                        res.Message = JsonConvert.SerializeObject(api.ErrorRes, Formatting.None);
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
                _common.LogError("GetSlotGameUrl" + " " + DateTime.Now.ToString() + " " + string.Format("{0}", playerID) + " " + ex.Message.ToString(), gameID);

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

        public override decimal? GetGameBalance()
        {
            if (gameMap != null)
            {

                var apiCall = Task.Run(() => api.GetPlayerAsync(gameMap.GamePlayerID, true)).Result;


                InsertLog(gameID, "GetGameBalance", "request", "request", "URL=" + api.finalUrl + ", " + api.Parameters, "response", JsonConvert.SerializeObject(api.Response));



                if (apiCall)
                {

                    //return (decimal)api.GetPlayerRes.balance.details.walletBalance.total;
                    return (decimal)api.GetPlayerRes.balance.total;
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

        public override bool GameDeposit(decimal Amount, string TransactionID, ref Error Error)
        {
            if (gameMap != null)
            {

                var apiCall = Task.Run(() => api.PostWalletTransactionsAsync(gameMap.GamePlayerID, "deposit", Amount, Guid.NewGuid().ToString(), TransactionID)).Result;


                InsertLog(gameID, "Deposit", "request", "request", "URL=" + api.finalUrl + ", " + api.Parameters, "response", JsonConvert.SerializeObject(api.Response));



                if (apiCall)
                {
                    return true;
                }
                else
                {
                    Error = new Error
                    {
                        ErrorCode = "-9999",
                        ErrorDesc = JsonConvert.SerializeObject(api.ErrorRes, Formatting.None)
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
                var apiCall = Task.Run(() => api.PostWalletTransactionsAsync(gameMap.GamePlayerID, "withdraw", Amount, Guid.NewGuid().ToString(), TransactionID)).Result;

                InsertLog(gameID, "Withdraw", "request", "request", "URL=" + api.finalUrl + ", " + api.Parameters, "response", JsonConvert.SerializeObject(api.Response));


                if (apiCall)
                {
                    return true;
                }
                else
                {
                    Error = new Error { ErrorCode = "-9999", ErrorDesc = JsonConvert.SerializeObject(api.ErrorRes, Formatting.None) };
                    return false;
                }
            }
            else
            {
                Error = new Error { ErrorCode = "-9999", ErrorDesc = "Retrieve Success" };
                return false;
            }
        }

        public string GetClientIp()
        {
            var request = HttpContext.Current.Request;

            // Check if the request passed through a proxy
            string ip = request.ServerVariables["HTTP_X_FORWARDED_FOR"];

            if (!string.IsNullOrEmpty(ip))
            {
                // In case of multiple IPs, take the first one (the original client)
                string[] ipList = ip.Split(',');
                return ipList[0].Trim();
            }

            // If no proxy was used, get the direct remote IP
            return request.ServerVariables["REMOTE_ADDR"];
        }

        public override vm.GetGameUrlVM GetGameUrl(string langId, bool IsMobile = true)
        {
            var res = new vm.GetGameUrlVM();

            try
            {
                if (gameMap != null)
                {
                    string url = string.Format("{0}?playerid={1}&currency={2}&ip={3}&lang={4}", lobbyUrl, playerID, "MYR", GetClientIp(), langId.ToUpper());

                    InsertLog(gameID, "GetGameUrl", "playerid", playerID, "currency", "MYR", "ip", GetClientIp(), "lang", langId, "url", url);

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

        public override bool KickPlayer(string GamePlayerId, ref Error Error)
        {
            throw new NotImplementedException();
        }

        public List<GetGamesResponse> GetGameList()
        {
            var res = new List<GetGamesResponse>();

            try
            {

                var apiCall = Task.Run(() => api.GetGamesAsync()).Result;

                InsertLog(gameID, "GetGameList", "request", "request", "URL=" + api.finalUrl + ", " + api.Parameters, "response", JsonConvert.SerializeObject(api.Response));

                if (apiCall)
                {
                    res = api.GetGamesRes;
                }


            }
            catch (Exception ex)
            {


            }

            return res;
        }
    }
}