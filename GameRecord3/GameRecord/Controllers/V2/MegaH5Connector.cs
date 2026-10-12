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
using MegaH5;

namespace GameRecord.Controllers.V2
{
	public class MegaH5Connector : BaseConnector
	{
		private string gameID = "MEGAH5";

		private string ApiUrl = "";
		private string OperatorId = "";
		private string SecretKey = "";
		private string Currency = "";
		private int TimeOutSec = 30000;
		private string Ip = "";

		private GameMapping gameMap;
		private MegaH5.MegaH5 api = null;
		Common _common = new Common();

		public MegaH5Connector(string PlayerID = "", string CurrencyID = "", string IPAddr = "", GameEnvironment Environment = GameEnvironment.PROD) : base(PlayerID, CurrencyID, IPAddr, Environment)
		{
			using (BingoConn db = new BingoConn())
			{
				gameMap = db.GameMappings.FirstOrDefault(x => x.Game.ToUpper().Trim() == gameID.ToUpper().Trim() && x.PlayerID == playerID && x.Currency.ToUpper() == currencyID && x.IsActive == true);

				var apiConfig = CatchData.GetApiKeys(gameID.ToUpper().Trim(), Environment.ToString(), currencyID.ToUpper().Trim());
				if (string.IsNullOrEmpty(CurrencyID)) apiConfig = CatchData.GetApiKeys(gameID.ToUpper().Trim(), Environment.ToString());

				if (apiConfig != null)
				{
					JObject jObj = JObject.Parse(apiConfig.ApiConfig);
					ApiUrl = jObj["ApiUrl"].ToString();
					OperatorId = jObj["OperatorId"].ToString();
					SecretKey = jObj["SecretKey"].ToString();
					Currency = jObj["Currency"].ToString();
					TimeOutSec = Convert.ToInt32(jObj["TimeOutSec"].ToString());
					api = new MegaH5.MegaH5(ApiUrl, OperatorId, SecretKey, Currency, TimeOutSec);
					Ip = IPAddr;
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
					InsertLog(gameID, "CreatePlayer", "Url", api.Url, "Request", api.Request, "Response", api.Response, "ErrorMessage", api.ErrorMessage);
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
				var apiCall = api.Deposit(gameMap.GamePlayerID, Amount);
				InsertLog(gameID, "Deposit", "Url", api.Url, "Request", api.Request, "Response", api.Response, "ErrorMessage", api.ErrorMessage);
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
				var apiCall = api.Withdraw(gameMap.GamePlayerID, Amount);
				InsertLog(gameID, "Withdraw", "Url", api.Url, "Request", api.Request, "Response", api.Response, "ErrorMessage", api.ErrorMessage);
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
					var apiCall = api.CheckBalance(gameMap.GamePlayerID);
					InsertLog(gameID, "CheckBalance", "Url", api.Url, "Request", api.Request, "Response", api.Response, "ErrorMessage", api.ErrorMessage);
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
				var apiCall = api.CheckBalance(gameMap.GamePlayerID);
				InsertLog(gameID, "CheckBalance", "Url", api.Url, "Request", api.Request, "Response", api.Response, "ErrorMessage", api.ErrorMessage);
				if (apiCall)
					return api.CheckBalanceRes.CurrentBalance;
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
					var apiCall = api.GameLogin(gameMap.GamePlayerID, Ip, GetGameLanguage(gameID, langId));
					InsertLog(gameID, "GameLogin", "Url", api.Url, "Request", api.Request, "Response", api.Response, "ErrorMessage", api.ErrorMessage);
					if (apiCall)
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
	}
}