using GameRecord.Models;
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
	public class JDBConnector : BaseConnector
	{
		// GET: HabaneroConnector
		private int gameUniqueID = 0;
		private string gameID = "JDB";
		private string apiUrl = "";
		private string gameUrl = "";
		private string key = "";
		private string iv = "";
		private string dc = "";
		private string manager = "";
		private string masterAgent = "";
		private string agent = "";
		private int timeOutSec = 30;
		private string noneAllCashOutFlag = "0";
		private GameMapping gameMap;
		private JDB.JDB api = null;
		Common _common = new Common();

		public JDBConnector(string PlayerID = "", string CurrencyID = "", string IPAddr = "", GameEnvironment Environment = GameEnvironment.PROD) : base(PlayerID, CurrencyID, IPAddr, Environment)
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
					apiUrl = jObj["JDBApiUrl"].ToString();
					key = jObj["JDBKey"].ToString();
					iv = jObj["JDBiv"].ToString();
					dc = jObj["JDBDc"].ToString();
					manager = jObj["JDBManager"].ToString();
					masterAgent = jObj["JDBMasterAgent"].ToString();
					agent = jObj["JDBAgent"].ToString();
					timeOutSec = Convert.ToInt32(jObj["JDBTimeOutSec"].ToString());
					api = new JDB.JDB(apiUrl, iv, key, manager, masterAgent, agent, dc, timeOutSec);
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
					//if (!string.IsNullOrEmpty(ipAddr)) ipAddr = ipAddr.Split(':')[0];
					string ipAddr = _common.GetIPAddress();
					string userAgent = HttpContext.Current.Request.UserAgent;

					var apiCheck = api.CreatePlayer(loginID, fullName, 0);

					targetApiUrl = api.TargetApiUrl;
					request = api.Request;
					response = api.Response;
					string error = api.ErrorMessage;


					if (apiCheck)
					{
						//var apiCall = api.CreateMember(loginID);
						InsertLog(gameID, "CreateUser", "request", api.Request, "response", api.Response);

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
				if (Amount >= 0)
				{
					var apiCall = api.WithdrawDeposite(gameMap.GamePlayerID, RequestID, noneAllCashOutFlag, Decimal.ToDouble(Amount), nameof(GameDeposit));
					InsertLog(gameID, "Deposit", "request", api.Request, "response", api.Response);

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
						ErrorDesc = "Deposite Only Accept Positive Value"
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

				var apiCall = api.WithdrawDeposite(gameMap.GamePlayerID, RequestID, noneAllCashOutFlag, -1 * Decimal.ToDouble(Amount), nameof(GameDeposit));
				InsertLog(gameID, "Withdraw", "request", api.Request, "response", api.Response);

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
					var apiCall = api.QueryPlayer(gameMap.GamePlayerID);
					InsertLog(gameID, "GetBalance", "request", api.Request, "response", api.Response);

					targetApiUrl = api.Request;
					request = api.Request;
					response = api.Response;

					if (apiCall)
					{
						res.PlayerID = playerID;
						res.Balance = api.Action15QueryPlayerResponse.data.FirstOrDefault().balance;
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
				var apiCall = api.QueryPlayer(gameMap.GamePlayerID);
				InsertLog(gameID, "GetBalance", "request", api.Request, "response", api.Response);

				targetApiUrl = api.Request;
				request = api.Request;
				response = api.Response;

				if (apiCall)
				{
					return Convert.ToDecimal(api.Action15QueryPlayerResponse.data.FirstOrDefault().balance);
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
					if (!string.IsNullOrEmpty(ipAddr)) ipAddr = ipAddr.Split(':')[0];
					string userAgent = HttpContext.Current.Request.UserAgent;

					langId = GetGameLanguage(gameID, langId);
					var apiLoginCall = api.Token(gameMap.GamePlayerID, langId);
					InsertLog(gameID, "GetGameUrl", "request", api.Request, "response", api.Response);

					targetApiUrl = api.Request;
					request = api.Request;
					response = api.Response;

					if (apiLoginCall)
					{
						res.PlayerID = playerID;
						res.Url = api.TokenResponse.path;
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
				var apiCall = api.KickPlayer(gameMap.GamePlayerID);
				InsertLog(gameID, "KickPlayer", "request", api.Request, "response", api.Response);

				targetApiUrl = api.TargetApiUrl;
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

		public override string RetryDeposit(decimal Amount, string TrackerId, ref Error Error)
		{
			throw new NotImplementedException();
		}

		public override string RetryWithdraw(decimal Amount, string TrackerId, ref Error Error)
		{
			throw new NotImplementedException();
		}

		public JDB.Model.GetDetailReportResponse GetDetailReport(string dateFrom, string dateTo)
		{
			api = new JDB.JDB(apiUrl, iv, key, manager, masterAgent, agent, dc, timeOutSec);

			var apiCall = api.GetDetailReport(Convert.ToDateTime(dateFrom).ToString("dd-MM-yyyy HH:mm:ss"), Convert.ToDateTime(dateTo).ToString("dd-MM-yyyy HH:mm:ss"));

			InsertLog(gameID, "JDBRecord", "request", api.Request, "response", api.Response);

			targetApiUrl = api.Request;
			request = api.Request;
			response = api.Response;

			if (apiCall)
			{
				return api.GetDetailReportResponse;
			}
			return null;
		}

		public JDB.Model.GetDetailReportResponse GetHistoryDetailReport(string dateFrom, string dateTo)
		{
			api = new JDB.JDB(apiUrl, iv, key, manager, masterAgent, agent, dc, timeOutSec);

			var apiCall = api.GetHistoryDetailReport(Convert.ToDateTime(dateFrom).ToString("dd-MM-yyyy HH:mm:ss"), Convert.ToDateTime(dateTo).ToString("dd-MM-yyyy HH:mm:ss"));

			//InsertLog(gameID, "JDBHistoryRecord", "request", api.Request, "response", api.Response);

			targetApiUrl = api.Request;
			request = api.Request;
			response = api.Response;

			if (apiCall)
			{
				return api.GetDetailReportResponse;
			}
			return null;
		}
	}
}