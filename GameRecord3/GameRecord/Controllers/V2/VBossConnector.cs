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
using GameRecord.Games.VBoss;
using System.Data.Entity.Migrations;

namespace GameRecord.Controllers
{
	public class VBossConnector : BaseConnector
	{
		private string gameID = "VBOSS";
		private string apiUrl = "";
		private string apiUser = "";
		private string apiPass = "";
		private string user = "";
		private string pass = "";
		private string currency = "";
		private int timeOutSec = 30000;
		private GameMapping gameMap;
		private VBoss api = null;
		Common _common = new Common();

		public VBossConnector(string PlayerID = "", string CurrencyID = "", string IPAddr = "", GameEnvironment Environment = GameEnvironment.PROD) : base(PlayerID, CurrencyID, IPAddr, Environment)
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
					apiUser = jObj["apiUser"].ToString();
					apiPass = jObj["apiPass"].ToString();
					user = jObj["user"].ToString();
					pass = jObj["pass"].ToString();
					currency = jObj["currency"].ToString();
					timeOutSec = Convert.ToInt32(jObj["timeOutSec"].ToString());

					api = new VBoss(apiUrl, apiUser, apiPass, user, pass, currency, timeOutSec);
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
					var apiCheck = api.CreatePlayer(loginID, loginPass, fullName);
					InsertLog(gameID, "CreateUser", "Request", api.Request, "Response", api.Response);

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
						InsertLog(gameID, "CreateUser", "ErrorMessage", api.ErrorMessage);

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
				InsertLog(gameID, "Deposit", "Request", api.Request, "Response", api.Response);

				targetApiUrl = api.Request;
				request = api.Request;
				response = api.Response;

				if (apiCall)
				{
					return true;
				}
				else
				{
					InsertLog(gameID, "Deposit", "ErrorMessage", api.ErrorMessage);

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
				InsertLog(gameID, "Withdraw", "Request", api.Request, "Response", api.Response);

				targetApiUrl = api.Request;
				request = api.Request;
				response = api.Response;

				if (apiCall)
				{
					using (BingoConn db = new BingoConn())
					{
						var entry = db.VBossEntry.Where(x => x.PlayerId == gameMap.GamePlayerID).FirstOrDefault();
						if (entry != null)
						{
							entry.QuitDt = DateTime.Now;
							db.SaveChanges();
						}

						return true;
					}
				}
				else
				{
					InsertLog(gameID, "Withdraw", "ErrorMessage", api.ErrorMessage);

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
					var apiCall = api.GetProfile(gameMap.GamePlayerID);
					InsertLog(gameID, "GetBalance", "Request", api.Request, "Response", api.Response);

					targetApiUrl = api.Request;
					request = api.Request;
					response = api.Response;

					if (apiCall)
					{
						res.PlayerID = playerID;
						res.Balance = api.GetProfileRes.balance;
						res.Game = gameID;
						res.Message = "Successfully get balance.";
						res.Code = 0;
					}
					else
					{
						InsertLog(gameID, "GetBalance", "ErrorMessage", api.ErrorMessage);

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
				var apiCall = api.GetProfile(gameMap.GamePlayerID);
				InsertLog(gameID, "GetBalance", "Request", api.Request, "Response", api.Response);

				targetApiUrl = api.Request;
				request = api.Request;
				response = api.Response;

				if (apiCall)
				{
					return api.GetProfileRes.balance;
				}
				else
				{
					InsertLog(gameID, "GetBalance", "ErrorMessage", api.ErrorMessage);

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
					langId = GetGameLanguage(gameID, langId);
					string token = GetToken().ToLower();

					var apiLogin = api.BetLogin(gameMap.GamePlayerID, gameMap.GamePassword, langId);
					InsertLog(gameID, "GetGameUrl-BetLogin", "Request", api.Request, "Response", api.Response, "Url", api.BetLoginRes?.url);
					targetApiUrl = api.Request;
					request = api.Request;
					response = api.Response;

					if (apiLogin)
					{
						using (BingoConn db = new BingoConn())
						{
							var entry = db.VBossEntry.Where(x => x.PlayerId == gameMap.GamePlayerID).FirstOrDefault();
							if (entry != null)
							{
								entry.EnterDt = DateTime.Now;
								entry.QuitDt = null;
								entry.FetchedAll = false;
								db.SaveChanges();
							}
							else
							{
								var newEntry = new VBossEntry
								{
									PlayerId = gameMap.GamePlayerID,
									EnterDt = DateTime.Now,
								};
								db.VBossEntry.Add(newEntry);
								db.SaveChanges();
							}

							res.PlayerID = playerID;
							res.Url = api.BetLoginRes.url;
							res.Game = gameID;
							res.Message = "Success";
							res.Code = 0;
						}
					}
					else
					{
						InsertLog(gameID, "GetGameUrl-BetLogin", "ErrorMessage", api.ErrorMessage);

						res.PlayerID = playerID;
						res.Url = null;
						res.Game = gameID;
						res.Message = "Game account not found.";
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
				_common.LogError("GetGameUrl" + " " + DateTime.Now.ToString() + " " + string.Format("{0}", playerID) + " " + ex.Message.ToString(), gameID);
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

		public List<BetHistoryRes> GetBetHistory(string loginId, string dateFrom, string dateTo, int page)
		{
			api = new VBoss(apiUrl, apiUser, apiPass, user, pass, currency, timeOutSec);

			var apiCall = api.BetHistory(loginId, dateFrom, dateTo, page);
			InsertLog(gameID, "GetBetHistory", "Request", api.Request, "Response", api.Response);

			targetApiUrl = api.Request;
			request = api.Request;
			response = api.Response;

			if (apiCall)
			{
				return api.lBetHistoryRes;
			}
			else
			{
				InsertLog(gameID, "GetBetHistory", "ErrorMessage", api.ErrorMessage);
				return null;
			}
		}

		public WinningNumberRes GetWinningNumber(string loginId, string dateFrom, string dateTo)
		{
			api = new VBoss(apiUrl, apiUser, apiPass, user, pass, currency, timeOutSec);

			var apiCall = api.WinningNumber(loginId, dateFrom, dateTo);
			InsertLog(gameID, "GetWinningNumber", "Request", api.Request, "Response", api.Response);

			targetApiUrl = api.Request;
			request = api.Request;
			response = api.Response;

			if (apiCall)
			{
				return api.WinningNumberRes;
			}
			else
			{
				InsertLog(gameID, "GetWinningNumber", "ErrorMessage", api.ErrorMessage);
				return null;
			}
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
			var apiCall = api.GetProfile(GamePlayerID);
			InsertLog(gameID, "GetBalance", "Request", api.Request, "Response", api.Response);

			targetApiUrl = api.Request;
			request = api.Request;
			response = api.Response;

			if (apiCall)
			{
				return api.GetProfileRes.balance;
			}
			else
			{
				InsertLog(gameID, "GetBalance", "ErrorMessage", api.ErrorMessage);
				return null;
			}

		}
	}
}