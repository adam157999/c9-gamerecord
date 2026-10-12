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
using GameRecord.Gamess.AdvantPlay;

namespace GameRecord.Controllers.V2
{
    public class AdvantPlayConnector : BaseConnector
    {
		private string gameID = "AP";
		private string apiUrl = "";
		private string webUrl = "";
		private string prefix = "";
		private string secretKey = "";
		private string currency = "";
		private int timeOutSec = 30000;
		private string ip = "";

		private GameMapping gameMap;
		private AdvantPlay api = null;
		Common _common = new Common();

		public AdvantPlayConnector(string PlayerID = "", string CurrencyID = "", string IPAddr = "", GameEnvironment Environment = GameEnvironment.PROD) : base(PlayerID, CurrencyID, IPAddr, Environment)
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
					webUrl = jObj["webUrl"].ToString();
					prefix = jObj["prefix"].ToString();
					secretKey = jObj["secretKey"].ToString();
					currency = jObj["currency"].ToString();
					timeOutSec = Convert.ToInt32(jObj["timeOutSec"].ToString());
					api = new AdvantPlay(apiUrl, webUrl, prefix, secretKey, currency, timeOutSec);
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
				var err = new { ErrorCode = "999", ErrorDesc = ex.Message.ToString() };
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
				var apiCall = api.TransferIn(gameMap.GamePlayerID, Amount);
				InsertLog(gameID, "TransferIn", "Request", api.Request, "Parameters", api.Parameters, "Response", api.Response, "ErrorMessage", api.ErrorMessage);

				targetApiUrl = api.Request;
				request = api.Request;
				response = api.Response;

				if (apiCall)
				{
					return true;
				}
				else
				{
					Error = new Error { ErrorCode = "-9999", ErrorDesc = api.ErrorMessage };
					return false;
				}
			}
			else
			{
				Error = new Error { ErrorCode = "-9999", ErrorDesc = "Retrieve Success" };
				return false;
			}
		}

		public override bool GameWithdraw(decimal Amount, string RequestID, ref Error Error)
		{
			if (gameMap != null)
			{
				var apiCall = api.TransferOut(gameMap.GamePlayerID, Amount);
				InsertLog(gameID, "TransferOut", "Request", api.Request, "Parameters", api.Parameters, "Response", api.Response, "ErrorMessage", api.ErrorMessage);

				targetApiUrl = api.Request;
				request = api.Request;
				response = api.Response;

				if (apiCall)
				{
					return true;
				}
				else
				{
					Error = new Error { ErrorCode = "-9999", ErrorDesc = api.ErrorMessage };
					return false;
				}
			}
			else
			{
				Error = new Error { ErrorCode = "-9999", ErrorDesc = "Retrieve Success" };
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
					InsertLog(gameID, "GetBalance", "Request", api.Request, "Parameters", api.Parameters, "Response", api.Response, "ErrorMessage", api.ErrorMessage);

					targetApiUrl = api.Request;
					request = api.Request;
					response = api.Response;

					if (apiCall)
					{
						res.PlayerID = playerID;
						res.Balance = api.GetBalanceRes.Balance;
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
				var err = new Error { ErrorCode = "999", ErrorDesc = ex.Message.ToString() };
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
				InsertLog(gameID, "GetBalance", "Request", api.Request, "Parameters", api.Parameters, "Response", api.Response, "ErrorMessage", api.ErrorMessage);

				targetApiUrl = api.Request;
				request = api.Request;
				response = api.Response;

				if (apiCall)
					return api.GetBalanceRes.Balance;
				else
					return null;
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
					//AP: only use GetGameLanguage during LaunchGame, lobby no need GetGameLanguage
					//langId = GetGameLanguage(gameID, langId);

					string lobbyUrl = HttpContext.Current.Request.Url.AbsoluteUri.Replace(HttpContext.Current.Request.Url.PathAndQuery, "/") + "AdvantPlay/Lobby";
					lobbyUrl += "?PlayerId=" + gameMap.GamePlayerID;
					lobbyUrl += "&LangCode=" + langId;

					InsertLog(gameID, "GetGameUrl", "lobbyUrl", lobbyUrl, "IsMobile", IsMobile);

					res.PlayerID = playerID;
					res.Url = lobbyUrl;
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
				_common.LogError("GetGameUrl: " + playerID + " " + ex.Message.ToString(), gameID);
				var err = new Error { ErrorCode = "999", ErrorDesc = ex.Message.ToString() };
				InsertLog(gameID, "GetGameUrl", "Error", JsonConvert.SerializeObject(err));

				res.PlayerID = playerID;
				res.Url = null;
				res.Game = gameID;
				res.Message = ex.Message.ToString();
				res.Code = 99;
			}

			return res;
		}

		public GetGameListRes GetGamesList()
		{
			var apiCall = api.GetGameList();
			InsertLog(gameID, "GetGameList", "Request", api.Request, "Parameters", api.Parameters, "Response", api.Response, "ErrorMessage", api.ErrorMessage);
			//InsertLog(gameID, "GetGameList-Debug", "api.GetGameListRes", JsonConvert.SerializeObject(api.GetGameListRes));

			if (apiCall)
				return api.GetGameListRes;
			else
				return null;
		}

		public GetPlayerTokenRes GetLaunchGameUrl(string PlayerId, string GameCode, string LangCode, string BackUrl)
		{
			LangCode = GetGameLanguage(gameID, LangCode);

			var apiCall = api.GetPlayerToken(PlayerId, GameCode, LangCode, BackUrl);
			InsertLog(gameID, "GetPlayerToken", "Request", api.Request, "Parameters", api.Parameters, "Response", api.Response, "ErrorMessage", api.ErrorMessage, "Url", api.Url);

			if (apiCall)
				return api.GetPlayerTokenRes;
			else
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

		public GetBatchHistoryRes GetBetHistory(string DateFrom, string DateTo, int Page = 1, int PageSize = 1000)
		{
			api = new AdvantPlay(apiUrl, webUrl, prefix, secretKey, currency, timeOutSec);

			var apiCall = api.GetBatchHistory(DateFrom, DateTo, Page, PageSize);
			InsertLog(gameID, "GetBatchHistory", "Request", api.Request, "Parameters", api.Parameters, "Response", api.Response, "ErrorMessage", api.ErrorMessage);

			if (apiCall)
				return api.GetBatchHistoryRes;
			else
				return null;
		}

		public string GetCurrency()
		{
			return currency;
		}

		public decimal? GetGameBalanceV2(string GamePlayerID)
		{
			var apiCall = api.GetBalance(GamePlayerID);
			InsertLog(gameID, "GetBalance", "Request", api.Request, "Parameters", api.Parameters, "Response", api.Response, "ErrorMessage", api.ErrorMessage);

			targetApiUrl = api.Request;
			request = api.Request;
			response = api.Response;

			if (apiCall)
				return api.GetBalanceRes.Balance;
			else
				return null;
		}
	}
}