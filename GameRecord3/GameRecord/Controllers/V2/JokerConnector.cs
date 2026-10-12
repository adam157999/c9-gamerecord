using GameRecord.Gamess.Joker;
using GameRecord.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using static GameRecord.Gamess.Joker.JokerIntegration;
using static GameRecord.Gamess.SAGAPI.SAGAMINGAPI;
using vm = GameRecord.Controllers.GameAPIController;

namespace GameRecord.Controllers
{
    public class JokerConnector : BaseConnector
	{
        // GET: JokerConnector
        private int gameUniqueID = 0;
		private string gameID = "JOKER";

		private string baseUrl = "";
		private string appId = "";
		private string secretKey = "";
        private string forwardurl = "";
        private JokerIntegration api = null;
        private GameMapping gameMap;
        private string lobbyUrl = HttpContext.Current.Request.Url.AbsoluteUri.Replace(HttpContext.Current.Request.Url.PathAndQuery, "/") + "Joker/Lobby";

        public JokerConnector(string PlayerID = "", string CurrencyID = "", string IPAddr = "", GameEnvironment Environment = GameEnvironment.PROD) : base(PlayerID, CurrencyID, IPAddr, Environment)
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
                    baseUrl = jObj["baseUrl"].ToString();
                    appId = jObj["appId"].ToString();
                    secretKey = jObj["secretKey"].ToString();
                    forwardurl = jObj["forwardurl"].ToString();


                    api = new JokerIntegration(baseUrl, appId, secretKey);
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

                    var request = new CreateUserRequest
                    {
                        Username = loginID
                    };

                    var apiCall = Task.Run(() => api.CreateUser(request)).Result;
                    if (apiCall.Success)
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

                        InsertLog(gameID, "CreateUser", "request", "URL=" + api.finalUrl + ", " + api.Parameters, "response", JsonConvert.SerializeObject(api.Response));

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

                    var request = new GetUserCreditRequest
                    {
                        Username = gameMap.GamePlayerID
                    };


                    var apiCall = Task.Run(() => api.GetUserCredit(request)).Result;

                     
                    InsertLog(gameID, "CheckPlayerBalance", "request", "request", "URL=" + api.finalUrl + ", " + api.Parameters , "response", JsonConvert.SerializeObject(api.Response));

                    if (apiCall.Success)
                    {
                        
                        res.PlayerID = playerID;
                        res.Balance = apiCall.Credit;
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

                var request = new GetUserCreditRequest
                {
                    Username = gameMap.GamePlayerID
                };


                var apiCall = Task.Run(() => api.GetUserCredit(request)).Result;


                InsertLog(gameID, "CheckPlayerBalance", "request", "request", "URL=" + api.finalUrl + ", " + api.Parameters, "response", JsonConvert.SerializeObject(api.Response));



                if (apiCall.Success)
                {

                    return apiCall.Credit;
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

                var request = new TransferCreditRequest
                {
                    Amount = Amount,
                    RequestID = TransactionID,
                    Username = gameMap.GamePlayerID
                };


                var apiCall = Task.Run(() => api.TransferCredit(request)).Result;


                InsertLog(gameID, "DepositPlayer", "request", "request", "URL=" + api.finalUrl + ", " + api.Parameters, "response", JsonConvert.SerializeObject(api.Response));



                if (apiCall.Status == TransferCreditStatus.Success)
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
                var request = new WithdrawAllCreditRequest
                {
                    
                    RequestID = TransactionID,
                    Username = gameMap.GamePlayerID
                };


                var apiCall = Task.Run(() => api.WithdrawAllCredit(request)).Result;

                InsertLog(gameID, "GameWithdraw", "request", "request", "URL=" + api.finalUrl + ", " + api.Parameters, "response", JsonConvert.SerializeObject(api.Response));


                if (apiCall.Success)
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

                    string url = string.Format("{0}?playerid={1}&currency={2}&ip={3}&lang={4}&mobile={5}", lobbyUrl, playerID, "MYR", GetClientIp(), langId, IsMobile.ToString().ToLower());

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

        public vm.GetGameUrlVM GetSlotGameUrl(string langId, string gameCode, bool IsMobile = true)
        {
            var res = new vm.GetGameUrlVM();

            try
            {
                if (gameMap != null)
                {
                    langId = GetGameLanguage(gameID, langId);


                    var Request = new PlayGameRequest
                    {

                        Username = gameMap.GamePlayerID
                    };

                    var apiCall = Task.Run(() => api.PlayGame(Request)).Result;


                    InsertLog(gameID, "GetGameLink", "request", "URL=" + api.finalUrl + ", " + api.Parameters, "response", api.Response);

                    targetApiUrl = api.finalUrl;
                    request = api.Parameters;
                    response = api.Response;

                    if (apiCall.Success)
                    {
                        res.PlayerID = playerID;
                        res.Url = $"{forwardurl}?Token={apiCall.Token}&game={gameCode}&mobile={IsMobile}";
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
                var Request = new SignOutRequest
                {
                    Username = gameMap.GamePlayerID
                };

                var apiCall = Task.Run(() => api.SignOut(Request)).Result; 
                InsertLog(gameID, "PlayerLogout", "request", "URL=" + api.finalUrl + ", " + api.Parameters, "response", api.Response);

                targetApiUrl = api.finalUrl;
                request = api.Parameters;
                response = api.Response;

                if (apiCall.Success)
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


        public ExtGetListGameV2Response GetGameList()
        {
            var res = new ExtGetListGameV2Response();

            try
            {

                var apiCall = Task.Run(() => api.GetListGamesV2()).Result;

                InsertLog(gameID, "GetGameList", "request", "request", "URL=" + api.finalUrl + ", " + api.Parameters, "response", JsonConvert.SerializeObject(api.Response));

                if (apiCall)
                {
                    res = api.ExtGetListGameV2Res;
                }


            }
            catch (Exception ex)
            {


            }

            return res;
        }
    }
}