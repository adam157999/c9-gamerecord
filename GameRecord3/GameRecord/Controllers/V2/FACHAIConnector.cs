using GameRecord.Gamess.FACHAI;
using GameRecord.Gamess.Joker;
using GameRecord.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using static GameRecord.Gamess.FACHAI.FCAPI;
using static GameRecord.Gamess.Joker.JokerIntegration;
using vm = GameRecord.Controllers.GameAPIController;

namespace GameRecord.Controllers
{
    public class FACHAIConnector : BaseConnector
	{
        // GET: WIN28Connector
        private int gameUniqueID = 0;
		private string gameID = "FACHAI";

		private string APIUrl = "";
		private string AgentCode = "";
		private string Currency = "";
        private string AgentKey = "";
       
        private FCAPI api = null;
        private GameMapping gameMap;
        private string lobbyUrl = HttpContext.Current.Request.Url.AbsoluteUri.Replace(HttpContext.Current.Request.Url.PathAndQuery, "/") + "Fachai/Lobby";


        public FACHAIConnector(string PlayerID = "", string CurrencyID = "", string IPAddr = "", GameEnvironment Environment = GameEnvironment.PROD) : base(PlayerID, CurrencyID, IPAddr, Environment)
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
                    AgentCode = jObj["AgentCode"].ToString();
                    Currency = jObj["Currency"].ToString();
                    AgentKey = jObj["AgentKey"].ToString();
                   

                    api = new FCAPI(APIUrl, AgentCode, Currency, AgentKey);
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
                    var apiCall = Task.Run(() => api.CreateMember(loginID)).Result;  
                    if (apiCall && api.CreateUserRes.Result==0)
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
                    
                    var apiCall =  Task.Run(() => api.SearchMemberInfo(gameMap.GamePlayerID)).Result;


                    InsertLog(gameID, "SearchMemberInfo", "request", "request", "URL=" + api.finalUrl + ", " + api.Parameters , "response", JsonConvert.SerializeObject(api.Response), api.Request);

                    if (apiCall && api.SearchMemberInfoRes.Result == 0)
                    {
                        
                        res.PlayerID = playerID;
                        res.Balance =  api.SearchMemberInfoRes.Points;
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
                _common.LogError("SearchMemberInfo" + " " + DateTime.Now.ToString() + " " + string.Format("{0}", playerID) + " " + ex.Message.ToString(), gameID);

                var err = new Error
                {
                    ErrorCode = "999",
                    ErrorDesc = ex.Message.ToString()
                };

                InsertLog(gameID, "SearchMemberInfo", "Error", JsonConvert.SerializeObject(err));

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

                var apiCall = Task.Run(() => api.SearchMemberInfo(gameMap.GamePlayerID)).Result;


                InsertLog(gameID, "CheckPlayerBalance", "request", "request", "URL=" + api.finalUrl + ", " + api.Parameters, "response", JsonConvert.SerializeObject(api.Response), api.Request);



                if (apiCall && api.SearchMemberInfoRes.Result == 0)
                {

                    return api.SearchMemberInfoRes.Points;
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
            string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");


            if (gameMap != null)
            {
                
                var apiCall =  Task.Run(() => api.DepositAndWithdrawal(gameMap.GamePlayerID,TrsID: ("IN" + timestamp + gameMap.GamePlayerID).Length > 30 ? ("IN" + timestamp + gameMap.GamePlayerID).Substring(0, 30) : ("IN" + timestamp + gameMap.GamePlayerID), Points: Amount)).Result;


                InsertLog(gameID, "DepositPlayer", "request", "request", "URL=" + api.finalUrl + ", " + api.Parameters, "response", JsonConvert.SerializeObject(api.Response), api.Request);



                if (apiCall && api.SetPointsRes.Result == 0)
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
            string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");

            if (gameMap != null)
            {
                var apiCall = Task.Run(() => api.DepositAndWithdrawal(gameMap.GamePlayerID, TrsID: ("OUT" + timestamp + gameMap.GamePlayerID).Length > 30 ? ("OUT" + timestamp + gameMap.GamePlayerID).Substring(0, 30) : ("OUT" + timestamp + gameMap.GamePlayerID), Points: (Amount*-1))).Result;

                InsertLog(gameID, "GameWithdraw", "apiCall", "request", "URL=" + api.finalUrl + ", " + api.Parameters, "response", JsonConvert.SerializeObject(api.Response), api.Request);


                if (apiCall && api.SetPointsRes.Result == 0)
                {
                    InsertLog(gameID, "GameWithdraw", "apiCall:true", "TransactionID", TransactionID, "Player", gameMap.GamePlayerID);
                    return true;
                }
                else
                {
                    InsertLog(gameID, "GameWithdraw", "apiCall:false", "TransactionID", TransactionID, "Player", gameMap.GamePlayerID);
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


        public vm.GetGameUrlVM GetSlotGameUrl(string langId, int gameCode, bool IsMobile = true)
        {
            var res = new vm.GetGameUrlVM();

            try
            {
                if (gameMap != null)
                {
                    langId = GetGameLanguage(gameID, langId);


                    

                    var apiCall = Task.Run(() => api.GameLogin(gameMap.GamePlayerID, gameCode, false,LanguageID:int.Parse(langId))).Result;


                    InsertLog(gameID, "GetGameLink", "request", "URL=" + api.finalUrl + ", " + api.Parameters, "response", api.Response, api.Request);

                    targetApiUrl = api.finalUrl;
                    request = api.Parameters;
                    response = api.Response;

                    if (apiCall && api.GameLoginRes.Result == 0)
                    {
                        res.PlayerID = playerID;
                        res.Url = api.GameLoginRes.Url;
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
               
                var apiCall = Task.Run(() => api.KickoutMember(gameMap.GamePlayerID)).Result;
                InsertLog(gameID, "PlayerLogout", "request", "URL=" + api.finalUrl + ", " + api.Parameters, "response", api.Response,api.Request);

                targetApiUrl = api.finalUrl;
                request = api.Parameters;
                response = api.Response;

                if (apiCall && api.KickoutMemberRes.Result == 0)
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

        public GetGameListandIconResponse GetGameList()
        {
            var res = new GetGameListandIconResponse();

            try
            {

                var apiCall = Task.Run(() => api.GetGameListandIcon()).Result;

                InsertLog(gameID, "GetGameList", "request", "request", "URL=" + api.finalUrl + ", " + api.Parameters, "response", JsonConvert.SerializeObject(api.Response), api.Request);

                if (apiCall && api.GetGameListandIconRes.Result == 0)
                {
                    res = api.GetGameListandIconRes;
                }


            }
            catch (Exception ex)
            {


            }

            return res;
        }

    }
}