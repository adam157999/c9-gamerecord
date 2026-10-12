using GameRecord.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using vm = GameRecord.Controllers.GameAPIController;
using GameRecord.Gamess.WIN28;

namespace GameRecord.Controllers
{
    public class WIN28Connector : BaseConnector
	{
        // GET: WIN28Connector
        private int gameUniqueID = 0;
		private string gameID = "WIN28";

		private string API_Url = "";
		private string API_apiUser = "";
		private string API_apiPass = "";
        private string API_user = "";
        private string API_pass = "";
        private string API_TimeOutSec = "";
        private WIN28 api = null;
        private GameMapping gameMap;
        

        public WIN28Connector(string PlayerID = "", string CurrencyID = "", string IPAddr = "", GameEnvironment Environment = GameEnvironment.PROD) : base(PlayerID, CurrencyID, IPAddr, Environment)
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
                    API_Url = jObj["API_Url"].ToString();
                    API_apiUser = jObj["API_apiUser"].ToString();
                    API_apiPass = jObj["API_apiPass"].ToString();
                    API_user = jObj["API_user"].ToString();
                    API_pass = jObj["API_pass"].ToString();
                    API_TimeOutSec = jObj["API_TimeOutSec"].ToString();

                    api = new WIN28(API_Url, API_apiUser, API_apiPass, API_user, API_pass,int.Parse(API_TimeOutSec));
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
                    var apiCall = api.RegisterUser(loginID, loginPass, fullName);
                    if (apiCall)
                    {
                        InsertLog(gameID, "RegisterUser", "request", "URL=" + api.finalUrl + ", " + api.Parameters, "response", JsonConvert.SerializeObject(api.Response));
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

                        InsertLog(gameID, "RegisterUser", "request", "URL=" + api.finalUrl + ", " + api.Parameters, "response", JsonConvert.SerializeObject(api.Response));

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
                _common.LogError("RegisterUser" + " " + DateTime.Now.ToString() + " " + string.Format("{0}", playerID) + " " + ex.Message.ToString(), gameID);

                var err = new
                {
                    ErrorCode = "999",
                    ErrorDesc = ex.Message.ToString()
                };

                InsertLog(gameID, "RegisterUser", "Error", JsonConvert.SerializeObject(err));

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
                    
                    var apiCall =   api.GetProfile(gameMap.GamePlayerID);

                     
                    InsertLog(gameID, "CheckPlayerBalance", "request", "request", "URL=" + api.finalUrl + ", " + api.Parameters , "response", JsonConvert.SerializeObject(api.Response));

                    if (apiCall)
                    {
                        
                        res.PlayerID = playerID;
                        res.Balance =  decimal.Parse(api.GetProfileResponse.balance);
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

                var apiCall = api.GetProfile(gameMap.GamePlayerID);


                InsertLog(gameID, "CheckPlayerBalance", "request", "request", "URL=" + api.finalUrl + ", " + api.Parameters, "response", JsonConvert.SerializeObject(api.Response));



                if (apiCall)
                {

                    return  decimal.Parse(api.GetProfileResponse.balance);
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

                var apiCall = api.Deposit(gameMap.GamePlayerID, Amount);


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
                var apiCall = api.Withdraw(gameMap.GamePlayerID, Amount);

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



        public override vm.GetGameUrlVM GetGameUrl(string langId, bool IsMobile = true)
        {
            var res = new vm.GetGameUrlVM();

            try
            {
                if (gameMap != null)
                {
                    langId = GetGameLanguage(gameID, langId);


                    var apiCall = api.BetLogin(gameMap.GamePlayerID, gameMap.GamePassword, IsMobile);

                    InsertLog(gameID, "GetGameUrl", "request", "URL=" + api.finalUrl + ", " + api.Parameters, "response", api.Response);

                    targetApiUrl = api.finalUrl;
                    request = api.Parameters;
                    response = api.Response;

                    if (apiCall)
                    {
                        string gamelink = "";

                        //if (IsMobile)
                        //{
                        //    gamelink = $"{API_Url}/wap/main.aspx?user={gameMap.GamePlayerID}&tokenCode={api.BetLoginResponse.tokenCode}&sessionID={api.BetLoginResponse.sessionID}";
                        //}
                        //else
                        //{
                        //    gamelink = $"{API_Url}/bet.aspx?user={gameMap.GamePlayerID}&tokenCode={api.BetLoginResponse.tokenCode}&sessionID={api.BetLoginResponse.sessionID}";
                        //}

                        gamelink = $"{API_Url}/bet.aspx?user={gameMap.GamePlayerID}&tokenCode={api.BetLoginResponse.tokenCode}&sessionID={api.BetLoginResponse.sessionID}";

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
            throw new NotImplementedException();
        }


         
    }
}