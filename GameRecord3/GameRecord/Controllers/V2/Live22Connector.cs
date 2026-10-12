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
using GameRecord.Gamess.Live22;

namespace GameRecord.Controllers.V2
{
    public class Live22Connector : BaseConnector
    {
		private string gameID = "LIVE22";
		private string apiUrl = "";
		private string operatorId = "";
		private string operatorPrefix = "";
		private string secretKey = "";
		private string currency = "";
		private int timeOutSec = 30000;
		private string ip = "";

		private GameMapping gameMap;
		private Live22 api = null;
		Common _common = new Common();
		private string lobbyUrl = HttpContext.Current.Request.Url.AbsoluteUri.Replace(HttpContext.Current.Request.Url.PathAndQuery, "/") + "Live22/Lobby";

		public Live22Connector(string PlayerID = "", string CurrencyID = "", string IPAddr = "", GameEnvironment Environment = GameEnvironment.PROD) : base(PlayerID, CurrencyID, IPAddr, Environment)
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
					apiUrl = jObj["apiUrl"].ToString();
					operatorId = jObj["operatorId"].ToString();
					operatorPrefix = jObj["operatorPrefix"].ToString();
					secretKey = jObj["secretKey"].ToString();
					currency = jObj["currency"].ToString();
					timeOutSec = Convert.ToInt32(jObj["timeOutSec"].ToString());
					api = new Live22(apiUrl, operatorId, operatorPrefix, secretKey, currency, timeOutSec);

					ip = IPAddr;
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
					var apiCheck = api.CreatePlayer(loginID);
					InsertLog(gameID, "CreatePlayer", "Request", api.Request, "Parameters", api.Parameters, "Response", api.Response, "ErrorMessage", api.ErrorMessage);

					if (apiCheck)
					{
						InsertGameMapping(gameID, loginID, loginPass, playerID);
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
				InsertLog(gameID, "Deposit", "Request", api.Request, "Parameters", api.Parameters, "Response", api.Response, "ErrorMessage", api.ErrorMessage);

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
				InsertLog(gameID, "GetGameUrl", "Request", api.Request, "Parameters", api.Parameters, "Response", api.Response, "ErrorMessage", api.ErrorMessage);

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
					var apiCall = api.CheckBalance(gameMap.GamePlayerID);
					InsertLog(gameID, "CheckBalance", "Request", api.Request, "Parameters", api.Parameters, "Response", api.Response, "ErrorMessage", api.ErrorMessage);

					targetApiUrl = api.Request;
					request = api.Request;
					response = api.Response;

					if (apiCall)
					{
						res.PlayerID = playerID;
						res.Balance = api.CheckBalanceRes.CurrentBalance;
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
				var apiCall = api.CheckBalance(gameMap.GamePlayerID);
				InsertLog(gameID, "CheckBalance", "Request", api.Request, "Parameters", api.Parameters, "Response", api.Response, "ErrorMessage", api.ErrorMessage);

				targetApiUrl = api.Request;
				request = api.Request;
				response = api.Response;

				if (apiCall)
				{
					return api.CheckBalanceRes.CurrentBalance;
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
					var sessionId = new Guid().ToString();
					langId = GetGameLanguage(gameID, langId);

					string url = string.Format("{0}?Username={1}&Lang={2}&Currency={3}", lobbyUrl, gameMap.GamePlayerID, langId, gameMap.Currency);

					InsertLog(gameID, "GetGameUrlLive22", "langId", langId, "IsMobile", IsMobile, "gameID", gameID, "url", url);

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
				_common.LogError("GetGameUrlLive22" + " " + DateTime.Now.ToString() + " " + string.Format("{0}", playerID) + " " + ex.Message.ToString(), gameID);

				var err = new Error
				{
					ErrorCode = "999",
					ErrorDesc = ex.Message.ToString()
				};

				InsertLog(gameID, "GetGameUrlLive22", "Error", JsonConvert.SerializeObject(err));

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

			//if (gameMap != null)
			//{
			//	var apiCall = api.KickAcct(gameMap.GamePlayerID);
			//	InsertLog(gameID, "KickPlayer", "request", api.Request, "response", api.Response);

			//	targetApiUrl = api.TargetApiUrl;
			//	request = api.Request;
			//	response = api.Response;

			//	if (apiCall)
			//	{
			//		return true;
			//	}
			//	else
			//	{
			//		Error = new Error
			//		{
			//			ErrorCode = "998",
			//			ErrorDesc = api.ErrorMessage
			//		};
			//		return false;
			//	}
			//}
			//else
			//{
			//	Error = new Error
			//	{
			//		ErrorCode = "998",
			//		ErrorDesc = "Retrieve Success"
			//	};
			//	return false;
			//}
		}

		public override string RetryDeposit(decimal Amount, string TrackerId, ref Error Error)
		{
			throw new NotImplementedException();
		}

		public override string RetryWithdraw(decimal Amount, string TrackerId, ref Error Error)
		{
			throw new NotImplementedException();
		}

		public PullLogRes GetBetHistory()
		{
			api = new Live22(apiUrl, operatorId, operatorPrefix, secretKey, currency, timeOutSec);

			var apiCall = api.PullLog();
			InsertLog(gameID, "PullLog", "Request", api.Request, "Parameters", api.Parameters, "Response", api.Response, "ErrorMessage", api.ErrorMessage);

			//targetApiUrl = api.Request;
			//request = api.Request;
			//response = api.Response;

			if (apiCall)
				return api.PullLogRes;
			else
				return null;
		}

		public bool FlagBetHistory(List<long> ids)
		{
			api = new Live22(apiUrl, operatorId, operatorPrefix, secretKey, currency, timeOutSec);

			var apiCall = api.FlagLog(ids);
			InsertLog(gameID, "FlagLog", "Request", api.Request, "Parameters", api.Parameters, "Response", api.Response, "ErrorMessage", api.ErrorMessage);

			//targetApiUrl = api.Request;
			//request = api.Request;
			//response = api.Response;

			return apiCall;
		}

		public GetGameListRes GetGamesList()
		{
			api = new Live22(apiUrl, operatorId, operatorPrefix, secretKey, currency, timeOutSec);

			var apiCall = api.GetGameList();
			InsertLog(gameID, "GetGamesList", "Request", api.Request, "Parameters", api.Parameters, "Response", api.Response, "ErrorMessage", api.ErrorMessage);

			targetApiUrl = api.Request;
			request = api.Request;
			response = api.Response;

			if (apiCall)
				return api.GetGameListRes;
			else
				return null;
		}

		public string GetSlotGameUrl(string Username, string GameCode, string Lang, string Ip)
		{
			api = new Live22(apiUrl, operatorId, operatorPrefix, secretKey, currency, timeOutSec);

			var apiCall = api.GameLogin(Username, Ip, Lang, GameCode);
			InsertLog(gameID, "GameLogin", "Request", api.Request, "Parameters", api.Parameters, "Response", api.Response, "ErrorMessage", api.ErrorMessage);

			targetApiUrl = api.Request;
			request = api.Request;
			response = api.Response;

			if (apiCall)
				return api.GameLoginRes.Url;
			else
				return null;
		}

		public string GetCurrency()
		{
			return currency;
		}

		public string GetToken()
		{
			Random random = new Random();
			string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
			return new string(Enumerable.Repeat(chars, 20)
					.Select(s => s[random.Next(s.Length)]).ToArray());
		}

		public decimal? GetGameBalanceV2(string GamePlayerID)
		{
			var apiCall = api.CheckBalance(GamePlayerID);
			InsertLog(gameID, "CheckBalance", "Request", api.Request, "Parameters", api.Parameters, "Response", api.Response, "ErrorMessage", api.ErrorMessage);

			targetApiUrl = api.Request;
			request = api.Request;
			response = api.Response;

			if (apiCall)
			{
				return api.CheckBalanceRes.CurrentBalance;
			}
			else
			{
				return null;
			}
		}
	}
}