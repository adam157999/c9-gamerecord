using GameRecord.Models;
using GameRecord.Gamess.AFB;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using vm = GameRecord.Controllers.GameAPIController;

namespace GameRecord.Controllers
{
	public class AFBConnector : BaseConnector
	{
		// GET: JiLiConnector
		private int gameUniqueID = 0;
		private string gameID = "AFB";
		private string apiUrl = "";
		private string apiKey = "";
		private string apiCurrency = "";
		private string token = "";
		private int siteId = 0;
		private int timeOutSec = 30;
		private string ip = "";

		private GameMapping gameMap;
		private AFB api = null;
		//private string lobbyUrl = HttpContext.Current.Request.Url.AbsoluteUri.Replace(HttpContext.Current.Request.Url.PathAndQuery, "/") + "GFG/Lobby";

		public AFBConnector(string PlayerID = "", string CurrencyID = "", string IPAddr = "", GameEnvironment Environment = GameEnvironment.PROD) : base(PlayerID, CurrencyID, IPAddr, Environment)
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
					apiUrl = jObj["AFBApiUrl"].ToString();
					apiKey = jObj["AFBApiKey"].ToString();
					siteId = Convert.ToInt32(jObj["AFBSiteId"].ToString());
					apiCurrency = jObj["AFBCurrency"].ToString();
					timeOutSec = Convert.ToInt32(jObj["AFBTimeOutSec"].ToString());

					api = new AFB(apiUrl, apiKey, siteId);
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
					//loginID = agentKey + "_" + loginID;
					//token = Guid.NewGuid().ToString();
					string timeStamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
					var apiCheck = api.Login("lobby", fullName, loginID, "en", apiCurrency);
					if (apiCheck)
					{
						InsertLog(gameID, "CreateUser", "request", api.Request, "response", api.Response, "error", api.ErrorMessage);

						targetApiUrl = api.Request;
						request = api.Request;
						response = api.Response;
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
						InsertLog(gameID, "CreateUser", "request", api.Request, "response", api.Response, "error", api.ErrorMessage);

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

		public override bool GameDeposit(decimal Amount, string OrderID, ref Error Error)
		{
			if (gameMap != null)
			{
				int money = Convert.ToInt32(Amount*100);
				string timeStamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
				InsertLog(gameID, "Deposit", "playerid", gameMap.GamePlayerID, "money", money.ToString());
				var apiCall = api.Deposit(gameMap.GamePlayerID, money);
				InsertLog(gameID, "Deposit", "playerid", gameMap.GamePlayerID, "request", api.Request, "response", api.Response);

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

		public override bool GameWithdraw(decimal Amount, string OrderID, ref Error Error)
		{
			if (gameMap != null)
			{
				int money = Convert.ToInt32(Amount*100);
				string timeStamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
				InsertLog(gameID, "Withdraw", "playerid", gameMap.GamePlayerID, "money", money.ToString());
				var apiCall = api.Withdraw(gameMap.GamePlayerID, money);
				InsertLog(gameID, "Withdraw", "playerid", gameMap.GamePlayerID, "request", api.Request, "response", api.Response);

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
					string timeStamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
					var apiCall = api.GetBalance(gameMap.GamePlayerID);
					InsertLog(gameID, "GetBalance", "request", api.Request, "response", api.Response);

					targetApiUrl = api.Request;
					request = api.Request;
					response = api.Response;

					if (apiCall)
					{
						res.PlayerID = playerID;
						res.Balance = Convert.ToInt32(api.BalanceRes.result.balance);
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
				string timeStamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
				var apiCall = api.GetBalance(gameMap.GamePlayerID);
				InsertLog(gameID, "GetBalance", "request", api.Request, "response", api.Response);

				targetApiUrl = api.Request;
				request = api.Request;
				response = api.Response;

				if (apiCall)
				{
					return Convert.ToInt32(api.BalanceRes.result.balance);
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
					token = Guid.NewGuid().ToString();
					langId = GetGameLanguage(gameID, langId);
					string timeStamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();

					var apiLogin = api.Login("lobby", gameMap.GamePlayerID, gameMap.GamePlayerID, langId, currencyID);
					InsertLog(gameID, "GetGameUrlVM-GameLogin", "request", api.Request, "response", api.Response, "Parameters", api.Parameters);

					if (apiLogin)
					{
						res.PlayerID = playerID;
						res.Url = api.LoginRes.result.link;
						res.Game = gameID;
						res.Message = "Success";
						res.Code = 0;
					}
					else
					{
						res.PlayerID = playerID;
						res.Url = null;
						res.Game = gameID;
						res.Message = "Fail Login to Provider lobby URL.";
						res.Code = 0;
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

		//public TakeBetLogsResponse RetrieveTransactionByTime(DateTime dateFrom, DateTime dateTo, int page, int size)
		//{
		//	api = new AFB(apiUrl, companyKey, secretKey, agentKey, gameTheme, timeOutSec);

		//	string timeStamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
		//	var apiCall = api.TakeBetLogs(dateFrom.ToString("yyyy-MM-dd HH:mm:ss"), dateTo.ToString("yyyy-MM-dd HH:mm:ss"), size, page, timeStamp);

		//	targetApiUrl = api.Request;
		//	request = api.Request;
		//	response = api.Response;

		//	if (apiCall)
		//	{
		//		return api.TakeBetLogsResponse;
		//	}
		//	return null;
		//}

		//public GetGameListResult GetGamesList()
		//{
		//	string timeStamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
		//	var apiCall = api.GetGameList(timeStamp);

		//	targetApiUrl = api.Request;
		//	request = api.Request;
		//	response = api.Response;

		//	if (apiCall)
		//	{
		//		return api.GetGameListResponse.data;
		//	}
		//	return null;
		//}

		//public string GetGameIconUrl(string language)
  //      {
		//	if(language.ToUpper().Contains("EN"))
  //          {
		//		return iconUrl = iconUrl + "/EN/";
  //          }
  //          else
  //          {
		//		return iconUrl = iconUrl + "/CN/";
  //          }
  //      }

		#region Lobby
		//public vm.GetBalanceVM GetBalanceLobby(string username)
		//{
		//	var res = new vm.GetBalanceVM();

		//	try
		//	{
		//		if (!string.IsNullOrEmpty(username))
		//		{
		//			string timeStamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
		//			var apiCall = api.QueryUserScore(username, timeStamp);
		//			InsertLog(gameID, "GetBalance", "request", api.Request, "response", api.Response);

		//			targetApiUrl = api.Request;
		//			request = api.Request;
		//			response = api.Response;

		//			if (apiCall)
		//			{
		//				res.PlayerID = playerID;
		//				res.Balance = Convert.ToDecimal(api.QueryUserScoreResponse.data.money);
		//				res.Game = gameID;
		//				res.Message = "Successfully get balance.";
		//				res.Code = 0;
		//			}
		//			else
		//			{
		//				res.PlayerID = playerID;
		//				res.Balance = null;
		//				res.Game = gameID;
		//				res.Message = api.ErrorMessage;
		//				res.Code = 2;
		//			}
		//		}
		//		else
		//		{
		//			res.PlayerID = playerID;
		//			res.Balance = null;
		//			res.Game = gameID;
		//			res.Message = "Game account not found.";
		//			res.Code = 1;
		//		}
		//	}
		//	catch (Exception ex)
		//	{
		//		_common.LogError("Balance" + " " + DateTime.Now.ToString() + " " + string.Format("{0}", playerID) + " " + ex.Message.ToString(), gameID);

		//		var err = new Error
		//		{
		//			ErrorCode = "999",
		//			ErrorDesc = ex.Message.ToString()
		//		};

		//		InsertLog(gameID, "GetCredit", "Error", JsonConvert.SerializeObject(err));

		//		res.PlayerID = playerID;
		//		res.Balance = null;
		//		res.Game = gameID;
		//		res.Message = ex.Message.ToString();
		//		res.Code = 99;
		//	}

		//	return res;
		//}

		//public vm.GetGameUrlVM GetSlotGameUrl(string username, string langId, int gameCode, string currency)
		//{
		//	var res = new vm.GetGameUrlVM();

		//	try
		//	{
		//		if (!string.IsNullOrEmpty(username))
		//		{
		//			string displayUserName = "";
		//			if(username.Contains(agentKey+"_"))
  //                  {
		//				displayUserName = username.Replace(agentKey + "_","");
  //                  }
		//			var sessionId = new Guid().ToString();
		//			//langId = GetGameLanguage(gameID, langId);
		//			token = Guid.NewGuid().ToString();
		//			string timeStamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
		//			string lobby = HttpUtility.UrlEncode(string.Format("{0}?username={1}&langid={2}&currency={3}", lobbyUrl, username, langId, currency));
		//			var apiCall = api.Login(username, ipAddr, token, username, timeStamp, langId, gameCode); ;
		//			InsertLog(gameID, "GetGFGSlotGameUrl", "request", api.Request, "response", api.Response);

		//			targetApiUrl = api.Request;
		//			request = api.Request;
		//			response = api.Response;

		//			if (apiCall)
		//			{
		//				res.PlayerID = playerID;
		//				res.Url = api.LoginResponse.data.url.ToString();
		//				res.Game = gameID;
		//				res.Message = "Success";
		//				res.Code = 0;
		//			}
		//			else
		//			{
		//				res.PlayerID = playerID;
		//				res.Url = null;
		//				res.Game = gameID;
		//				res.Message = api.ErrorMessage;
		//				res.Code = 2;
		//			}
		//		}
		//		else
		//		{
		//			res.PlayerID = playerID;
		//			res.Url = null;
		//			res.Game = gameID;
		//			res.Message = "Game account not found.";
		//			res.Code = 1;
		//		}
		//	}
		//	catch (Exception ex)
		//	{
		//		_common.LogError("GetGFGSlotGameUrl" + " " + DateTime.Now.ToString() + " " + string.Format("{0}", playerID) + " " + ex.Message.ToString(), gameID);

		//		var err = new Error
		//		{
		//			ErrorCode = "999",
		//			ErrorDesc = ex.Message.ToString()
		//		};

		//		InsertLog(gameID, "Login", "Error", JsonConvert.SerializeObject(err));

		//		res.PlayerID = playerID;
		//		res.Url = null;
		//		res.Game = gameID;
		//		res.Message = ex.Message.ToString();
		//		res.Code = 99;
		//	}

		//	return res;
		//}
		#endregion
	}
}