using GameRecord.Gamess.VPLUS;
using GameRecord.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Database;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using vm = GameRecord.Controllers.GameAPIController;

namespace GameRecord.Controllers
{
    public class VPlusConnector : BaseConnector
    {
        // GET: VPlusConnector
        private int gameUniqueID = 0;
        private string gameID = "VPLUS";

        private string APIUrl = "";
        private string MerchantSecret = "";
        private string MerchantID = "";
        private string GameLaunchUrl = "";

        private VPLUSAPI api = null;
        private GameMapping gameMap;
        private string lobbyUrl = HttpContext.Current.Request.Url.AbsoluteUri.Replace(HttpContext.Current.Request.Url.PathAndQuery, "/") + "VPlus/Lobby";

        public VPlusConnector(string PlayerID = "", string CurrencyID = "", string IPAddr = "", GameEnvironment Environment = GameEnvironment.PROD) : base(PlayerID, CurrencyID, IPAddr, Environment)
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
                    APIUrl = jObj["APIUrl"].ToString();
                    MerchantSecret = jObj["MerchantSecret"].ToString();
                    MerchantID = jObj["MerchantID"].ToString();
                    GameLaunchUrl = jObj["GameLaunchUrl"].ToString();

                    api = new VPLUSAPI(APIUrl, MerchantSecret, MerchantID, GameLaunchUrl);
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
                    var apiCall = Task.Run(() => api.createAccount(loginID)).Result;
                    if (apiCall)
                    {
                        InsertLog(gameID, "CreateMember", "request", "URL=" + api.finalUrl + ", " + api.Parameters, "response", JsonConvert.SerializeObject(api.Response), api.Request);
                        InsertGameMapping(gameID, loginID, loginPass, mcUID);
                        gameMap = LoadGameMap(gameID, playerID, currencyID);

                        string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");

                        var apiCall1 = Task.Run(() => api.InitWalletAsync(gameMap.GamePlayerID, "0",1,timestamp)).Result;

                        if (apiCall1)
                        {
                            res.PlayerID = playerID;
                            res.GamePlayerID = loginID;
                            res.Game = gameID;
                            res.Message = "Success";
                            res.Code = 0;
                        }
                        else
                        {
                            InsertLog(gameID, "CreateWallet", "request", "URL=" + api.finalUrl + ", " + api.Parameters, "response", JsonConvert.SerializeObject(api.Response));

                            res.PlayerID = playerID;
                            res.GamePlayerID = null;
                            res.Game = gameID;
                            res.Message = JsonConvert.SerializeObject(api.ErrorMessage, Formatting.None);
                            res.Code = 3;
                        }
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

                    var apiCall = Task.Run(() => api.CheckWalletBalance(gameMap.GamePlayerID)).Result;


                    InsertLog(gameID, "CheckPlayerBalance", "request", "request", "URL=" + api.finalUrl + ", " + api.Parameters, "response", JsonConvert.SerializeObject(api.Response));

                    if (apiCall)
                    {

                        res.PlayerID = playerID;
                        res.Balance = decimal.Parse(api.CheckWalletBalanceRes.data);
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
                _common.LogError("CheckPlayerBalance" + " " + DateTime.Now.ToString() + " " + string.Format("{0}", playerID) + " " + ex.Message.ToString(), gameID);

                var err = new Error
                {
                    ErrorCode = "999",
                    ErrorDesc = ex.Message.ToString()
                };

                InsertLog(gameID, "CheckPlayerBalance", "Error", JsonConvert.SerializeObject(err));

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

                var apiCall = Task.Run(() => api.CheckWalletBalance(gameMap.GamePlayerID)).Result;


                InsertLog(gameID, "CheckPlayerBalance", "request", "request", "URL=" + api.finalUrl + ", " + api.Parameters, "response", JsonConvert.SerializeObject(api.Response));



                if (apiCall)
                {

                    return decimal.Parse(api.CheckWalletBalanceRes.data);
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

                var apiCall = Task.Run(() => api.DepositWalletAsync(gameMap.GamePlayerID, Amount.ToString("F2"), TransactionID)).Result;


                InsertLog(gameID, "DepositPlayer", "request", "request", "URL=" + api.finalUrl + ", " + api.Parameters, "response", JsonConvert.SerializeObject(api.Response));



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

        public override bool GameWithdraw(decimal Amount, string TransactionID, ref Error Error)
        {
            if (gameMap != null)
            {
                InsertLog(gameID, "GameWithdraw", "request", "amount", Amount.ToString(), "amount2", Amount.ToString("F2"));
                //var apiCall = Task.Run(() => api.WithdrawWalletAsync(gameMap.GamePlayerID, Amount.ToString("F2"), TransactionID)).Result;
                
                var apiCall = Task.Run(() => api.WithdrawWalletAsync(gameMap.GamePlayerID, Amount.ToString(), TransactionID)).Result;

                InsertLog(gameID, "GameWithdraw", "request", "request", "URL=" + api.finalUrl + ", " + api.Parameters, "response", JsonConvert.SerializeObject(api.Response));


                if (apiCall)
                {
                    return true;
                }
                else
                {
                    Error = new Error { ErrorCode = "-9999", ErrorDesc = JsonConvert.SerializeObject(api.ErrorMessage, Formatting.None) };
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

                    string url = string.Format("{0}?playerid={1}&currency={2}&ip={3}&lang={4}", lobbyUrl, playerID, "MYR", GetClientIp(), langId);

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


        public vm.GetGameUrlVM GetSlotGameUrl(string langId, string gameCode)
        {
            var res = new vm.GetGameUrlVM();

            try
            {
                if (gameMap != null)
                {
                    langId = GetGameLanguage(gameID, langId);


                    var apiCall = Task.Run(() => api.login(gameMap.GamePlayerID)).Result;
                    string x = "";
                    if (apiCall)
                    {
                        x = Task.Run(() => api.OpenPlayerGameAsync(api.LoginRes.data.token, gameCode, "", "0")).Result;



                        Console.WriteLine("GetGameList successful!");
                    }
                    else
                    {
                        Console.WriteLine($"GetGameList failed: {api.ErrorMessage}");
                    }



                    InsertLog(gameID, "GetGameLink", "request", "URL=" + api.finalUrl + ", " + api.Parameters, "response", api.Response);

                    targetApiUrl = api.finalUrl;
                    request = api.Parameters;
                    response = api.Response;

                    if (apiCall)
                    {
                        res.PlayerID = playerID;
                        res.Url = x;
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
                var apiCall = Task.Run(() => api.KickPlayer(gameMap.GamePlayerID)).Result;
                InsertLog(gameID, "PlayerLogout", "request", "URL=" + api.finalUrl + ", " + api.Parameters, "response", api.Response);

                targetApiUrl = api.finalUrl;
                request = api.Parameters;
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


        public GameRecord.Gamess.VPLUS.VPLUSAPI.GameResponse GetGameList()
        {
            var res = new GameRecord.Gamess.VPLUS.VPLUSAPI.GameResponse();

            try
            {

                var apiCall = Task.Run(() => api.gameList()).Result;

                InsertLog(gameID, "GetGameList", "request", "request", "URL=" + api.finalUrl + ", " + api.Parameters, "response", JsonConvert.SerializeObject(api.Response));

                if (apiCall)
                {
                    res = api.GameRes;
                }


            }
            catch (Exception ex)
            {


            }

            return res;
        }
    }
}