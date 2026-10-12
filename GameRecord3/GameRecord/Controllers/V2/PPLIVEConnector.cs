using GameRecord.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using vm = GameRecord.Controllers.GameAPIController;

namespace GameRecord.Controllers.V2
{
	public class PPLiveConnector : BaseConnector
	{
		// GET: JiLiConnector
		private string gameUniqueID = "101";
		private string gameID = "PPLIVE";
		private string apiUrl = "";
		private string imgUrl = "";
		private string secureLogin = "";
		private string secretKey = "";
		private int timeOutSec = 30;
		private GameMapping gameMap;
		private Pragmatic.DLL.Pragmatic api = null;

		public PPLiveConnector(string PlayerID = "", string CurrencyID = "", string IPAddr = "", GameEnvironment Environment = GameEnvironment.PROD) : base(PlayerID, CurrencyID, IPAddr, Environment)
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
					apiUrl = jObj["PragmaticApiUrl"].ToString();
					imgUrl = jObj["PragmaticImgUrl"].ToString();
					secretKey = jObj["PragmaticSecretKey"].ToString();
					secureLogin = jObj["PragmaticSecureLogin"].ToString();
					timeOutSec = Convert.ToInt32(jObj["PragmaticTimeOutSec"].ToString());

					api = new Pragmatic.DLL.Pragmatic(apiUrl, secretKey, timeOutSec);
				}
			}
		}

		public override vm.CheckAndCreateAccountVM CreatePlayer(string loginID, string loginPass, string fullName, string mcUID)
		{
			throw new NotImplementedException();
		}

		public override bool GameDeposit(decimal Amount, string TransactionID, ref Error Error)
		{
			throw new NotImplementedException();
		}

		public override bool GameWithdraw(decimal Amount, string TransactionID, ref Error Error)
		{
			throw new NotImplementedException();
		}

		public override vm.GetBalanceVM GetBalance()
		{
			throw new NotImplementedException();
		}

		public override decimal? GetGameBalance()
		{
			throw new NotImplementedException();
		}

		public override vm.GetGameUrlVM GetGameUrl(string langId, bool IsMobile = false)
		{
			throw new NotImplementedException();
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

		public vm.GetGameUrlVM GetGameSeamlessUrl(string langId, string token, bool IsMobile = false)
		{
			var res = new vm.GetGameUrlVM();

			try
			{
				langId = GetGameLanguage(gameID, langId);
				var apiCall = api.StartGameSeamless(secureLogin, token, gameUniqueID, langId, "");
				InsertLog(gameID, "GetGameSeamlessUrl", "request", api.TargetApiUrl, "response", api.Response);

				targetApiUrl = api.Request;
				request = api.Request;
				response = api.Response;

				if (apiCall)
				{
					res.PlayerID = playerID;
					res.Url = string.Format(api.StartGameSeamlessResponse.gameURL);
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
			catch (Exception ex)
			{
				_common.LogError("UserLogin" + " " + DateTime.Now.ToString() + " " + string.Format("{0}", playerID) + " " + ex.Message.ToString(), gameID);

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

		public Pragmatic.Model.GetGameRoundsResponse RetrieveTransactionByTime(string timePoint, string dataType)
		{

			var apiCall = api.GetGameRounds(secureLogin, timePoint, dataType);

			targetApiUrl = api.Request;
			request = api.Request;
			response = api.Response;

			if (apiCall)
			{
				return api.GetGameResponse;
			}
			return null;
		}
	}
}