using GameRecord.Gamess.SAGAPI;
using GameRecord.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Threading.Tasks;
using vm = GameRecord.Controllers.GameAPIController;

namespace GameRecord.Controllers
{
    public class SAGamingConnector : BaseConnector
	{
        // GET: SAGamingConnector
        private int gameUniqueID = 0;
		private string gameID = "SAGAMING";

		private string BaseUrl = "";
        private string BetDetailAPIURL = "";
        private string GameClientLaunchingURL = "";
        private string LobbyCode = "";
		private string SecretKey = "";
        private string MD5Key = "";
        private string EncryptKey = "";
        private SAGAMINGAPI api = null;
        private GameMapping gameMap;  

		public SAGamingConnector(string PlayerID = "", string CurrencyID = "", string IPAddr = "", GameEnvironment Environment = GameEnvironment.PROD) : base(PlayerID, CurrencyID, IPAddr, Environment)
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
                    BaseUrl = jObj["BaseUrl"].ToString();
                    BetDetailAPIURL = jObj["BetDetailAPIURL"].ToString();
                    GameClientLaunchingURL = jObj["GameClientLaunchingURL"].ToString();
                    LobbyCode = jObj["LobbyCode"].ToString();
                    SecretKey = jObj["SecretKey"].ToString();
                    MD5Key = jObj["MD5Key"].ToString();
                    EncryptKey = jObj["EncryptKey"].ToString(); 

                     api = new SAGAMINGAPI(BaseUrl, BetDetailAPIURL, GameClientLaunchingURL, LobbyCode, SecretKey, MD5Key, EncryptKey);
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
                    var apiCall = Task.Run(() => api.CreateUser(loginID,"MYR")).Result; 
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
                    
                    var apiCall = Task.Run(() => api.GetUserStatusDV(gameMap.GamePlayerID)).Result;

                     
                    InsertLog(gameID, "GetBalance", "request", "request", "URL=" + api.finalUrl + ", " + api.Parameters , "response", JsonConvert.SerializeObject(api.Response));

                    if (apiCall)
                    {
                        
                        res.PlayerID = playerID;
                        res.Balance =  api.GetUserStatusDVRes.Balance;
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

                var apiCall = Task.Run(() => api.GetUserStatusDV(gameMap.GamePlayerID)).Result;


                InsertLog(gameID, "GetGameBalance", "request", "request", "URL=" + api.finalUrl + ", " + api.Parameters, "response", JsonConvert.SerializeObject(api.Response));



                if (apiCall)
                {

                    return (decimal)api.GetUserStatusDVRes.Balance;
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
            //Console.WriteLine(timestamp);


            if (gameMap != null)
            {

                var apiCall = Task.Run(() => api.CreditBalanceDV(gameMap.GamePlayerID, "IN" + timestamp + gameMap.GamePlayerID, Amount, "MYR")).Result;


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
                var apiCall = Task.Run(() => api.DebitBalanceDV(gameMap.GamePlayerID, "OUT" + timestamp + gameMap.GamePlayerID, Amount)).Result;

                InsertLog(gameID, "Withdraw", "request", "request", "URL=" + api.finalUrl + ", " + api.Parameters, "response", JsonConvert.SerializeObject(api.Response));


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

        public override vm.GetGameUrlVM GetGameUrl(string langId, bool IsMobile = true)
        {
            var res = new vm.GetGameUrlVM();

            try
            {
                if (gameMap != null)
                {
                    langId = GetGameLanguage(gameID, langId);


                    var apiCall = Task.Run(() => api.LoginRequest(gameMap.GamePlayerID, "MYR")).Result;
                     
                    InsertLog(gameID, "GetGameUrl", "request", "URL=" + api.finalUrl + ", " + api.Parameters, "response", api.Response);

                    targetApiUrl = api.finalUrl;
                    request = api.Parameters;
                    response = api.Response;

                    if (apiCall)
                    {

                        if (api.LoginRequestRes.ErrorMsgId == 0)
                        {
                            string gamelink = api.GetGameLink(gameMap.GamePlayerID, api.LoginRequestRes.Token,this.LobbyCode,lang:langId,mobile: IsMobile);
                            res.PlayerID = playerID;
                            res.Url = gamelink;
                            res.Game = gameID;
                            res.Message = "Success";
                            res.Code = 0;
                        }
                        else
                        {
                            res.PlayerID = playerID;
                            res.Url = null;
                            res.Game = gameID;
                            res.Message = JsonConvert.SerializeObject(api.ErrorMessage, Formatting.None);
                            res.Code = 3;
                        }

                           
                    }
                    else
                    {
                        res.PlayerID = playerID;
                        res.Url = null;
                        res.Game = gameID;
                        res.Message = JsonConvert.SerializeObject(api.ErrorMessage, Formatting.None);
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
            if (gameMap != null)
            {
                var apiCall = Task.Run(() => api.KickUser(gameMap.GamePlayerID)).Result; 
                InsertLog(gameID, "KickUser", "request", "URL=" + api.finalUrl + ", " + api.Parameters, "response", api.Response);

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
    }
}