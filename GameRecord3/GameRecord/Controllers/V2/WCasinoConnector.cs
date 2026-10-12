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
using GameRecord.Gamess.WCasino;

namespace GameRecord.Controllers.V2
{
    public class WCasinoConnector : BaseConnector
    {
		private string gameID = "WCASINO";
		private string apiurl = "";
		private string appid = "";
		private string key = "";
		private string qids = "";
		private string currency = "";
		private int timeOutSec = 30000;
		private string ip = "";

		private GameMapping gameMap;
		private WCasino api = null;
		Common _common = new Common();

		public WCasinoConnector(string PlayerID = "", string CurrencyID = "", string IPAddr = "", GameEnvironment Environment = GameEnvironment.PROD) : base(PlayerID, CurrencyID, IPAddr, Environment)
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
					apiurl = jObj["apiurl"].ToString();
					appid = jObj["appid"].ToString();
					key = jObj["key"].ToString();
					qids = jObj["qids"].ToString();
					currency = jObj["currency"].ToString();
					timeOutSec = Convert.ToInt32(jObj["timeOutSec"].ToString());
					api = new WCasino(apiurl, appid, key, qids, currency, timeOutSec);

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
					var apiCheck = api.Login(loginID);
					InsertLog(gameID, "Login-CreatePlayer", "Request", api.Request, "Parameters", api.Parameters, "Response", api.Response, "ErrorMessage", api.ErrorMessage);

					if (apiCheck)
					{
                        //InsertGameMapping(gameID, loginID, loginPass, playerID);
                        //gameMap = LoadGameMap(gameID, playerID, currencyID);

                        //res.PlayerID = playerID;
                        //res.GamePlayerID = loginID;
                        //res.Game = gameID;
                        //res.Message = "Success";
                        //res.Code = 0;

                        var apiCall = api.BetLimitSet(loginID);
                        InsertLog(gameID, "BetLimitSet", "Request", api.Request, "Parameters", api.Parameters, "Response", api.Response, "ErrorMessage", api.ErrorMessage);
                        if (apiCall)
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
				var apiCall = api.DepositAndWithdraw(gameMap.GamePlayerID, Amount);
				InsertLog(gameID, "DepositAndWithdraw-Deposit", "Request", api.Request, "Parameters", api.Parameters, "Response", api.Response, "ErrorMessage", api.ErrorMessage);

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
				var apiCall = api.DepositAndWithdraw(gameMap.GamePlayerID, Amount * -1);
				InsertLog(gameID, "DepositAndWithdraw-Withdraw", "Request", api.Request, "Parameters", api.Parameters, "Response", api.Response, "ErrorMessage", api.ErrorMessage);

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
					var apiCall = api.GetUserBalance(gameMap.GamePlayerID);
					InsertLog(gameID, "GetUserBalance", "Request", api.Request, "Parameters", api.Parameters, "Response", api.Response, "ErrorMessage", api.ErrorMessage);

					if (apiCall)
					{
						res.PlayerID = playerID;
						res.Balance = api.UserBalanceRes.balance;
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
				var apiCall = api.GetUserBalance(gameMap.GamePlayerID);
				InsertLog(gameID, "GetUserBalance", "Request", api.Request, "Parameters", api.Parameters, "Response", api.Response, "ErrorMessage", api.ErrorMessage);

				if (apiCall)
					return api.UserBalanceRes.balance;
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
					langId = GetGameLanguage(gameID, langId);

					var apiLogin = api.Login(gameMap.GamePlayerID, IsMobile ? 2 : 1, Convert.ToInt32(langId));
					InsertLog(gameID, "Login", "Request", api.Request, "Parameters", api.Parameters, "Response", api.Response, "ErrorMessage", api.ErrorMessage, "Url", api.LoginRes?.openurl);

					if (apiLogin)
					{
						res.PlayerID = playerID;
						res.Url = api.LoginRes.openurl;
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
						res.Code = 1;
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
				_common.LogError("Error at GetGameUrl (" + playerID + "): " + ex.Message.ToString(), gameID);
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
			if (gameMap != null)
			{
				var apiCall = api.Kick(gameMap.GamePlayerID);
				InsertLog(gameID, "KickPlayer", "Request", api.Request, "Parameters", api.Parameters, "Response", api.Response, "ErrorMessage", api.ErrorMessage);

				if (apiCall)
				{
					return true;
				}
				else
				{
					Error = new Error { ErrorCode = "998", ErrorDesc = api.ErrorMessage };
					return false;
				}
			}
			else
			{
				Error = new Error { ErrorCode = "998", ErrorDesc = "Retrieve Success" };
				return false;
			}
		}

		public override string RetryDeposit(decimal Amount, string TrackerId, ref Error Error)
		{
			throw new NotImplementedException();
		}

		public override string RetryWithdraw(decimal Amount, string TrackerId, ref Error Error)
		{
			throw new NotImplementedException();
		}

		public BetsDetailRes GetBetHistory(int begintime, int endtime, int size = 2000, int page = 0)
		{
			api = new WCasino(apiurl, appid, key, currency, qids, timeOutSec);

			var apiCall = api.GetBetsDetail(begintime, endtime, size, page);
			InsertLog(gameID, "GetBetsDetail", "Request", api.Request, "Parameters", api.Parameters, "Response", api.Response, "ErrorMessage", api.ErrorMessage);

			if (apiCall)
				return api.BetsDetailRes;
			else
				return null;
		}

		public string GetCurrency()
		{
			return currency;
		}

		public decimal? GetGameBalanceV2(string GamePlayerID)
		{
			var apiCall = api.GetUserBalance(GamePlayerID);
			InsertLog(gameID, "GetUserBalance", "Request", api.Request, "Parameters", api.Parameters, "Response", api.Response, "ErrorMessage", api.ErrorMessage);

			if (apiCall)
				return api.UserBalanceRes.balance;
			else
				return null;
		}

		public BetLimitInterfaceRes BetLimitInterface()
		{
			api = new WCasino(apiurl, appid, key, qids, currency, timeOutSec);

			var apiCall = api.BetLimitInterface();
			InsertLog(gameID, "BetLimitInterface", "Request", api.Request, "Parameters", api.Parameters, "Response", api.Response, "ErrorMessage", api.ErrorMessage);

			if (apiCall)
				return api.BetLimitInterfaceRes;
			else
				return null;
		}

		public void BetLimitSet(string username)
		{
			api = new WCasino(apiurl, appid, key, qids, currency, timeOutSec);

			var apiCall = api.BetLimitSet(username);
			InsertLog(gameID, "BetLimitSet", "Request", api.Request, "Parameters", api.Parameters, "Response", api.Response, "ErrorMessage", api.ErrorMessage);
		}
	}
}