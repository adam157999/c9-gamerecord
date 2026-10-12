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
	public class BGConnector : BaseConnector
	{
		// GET: BGConnector
		private int gameUniqueID = 0;
		private string gameID = "BG";
		private string apiUrl = "";
		private string sn = "";
		private string secretKey = "";
		private string agentLoginId = "";
		private string agentPassword = "";
		private int timeOutSec = 30;
		private GameMapping gameMap;
		private BG.BG api = null;
		Common _common = new Common();

		public BGConnector(string PlayerID = "", string CurrencyID = "", string IPAddr = "", GameEnvironment Environment = GameEnvironment.PROD) : base(PlayerID, CurrencyID, IPAddr, Environment)
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
					apiUrl = jObj["BGApiUrl"].ToString();
					sn = jObj["BGSn"].ToString();
					secretKey = jObj["BGSecretKey"].ToString();
					agentLoginId = jObj["BGAgentLoginId"].ToString();
					agentPassword = jObj["BGAgentPassword"].ToString();
					timeOutSec = Convert.ToInt32(jObj["BGTimeOutSec"].ToString());
					api = new BG.BG(apiUrl, secretKey, agentPassword, agentLoginId, sn, timeOutSec);
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
					string UUID = Guid.NewGuid().ToString();
					var apiCheck = api.UserCreate(loginID, fullName, ipAddr, UUID);

					targetApiUrl = api.TargetApiUrl;
					request = api.Request;
					response = api.Response;
					string error = api.ErrorMessage;

					InsertLog(gameID, "CreateUser", "request", api.Request, "response", api.Response, "error", api.ErrorMessage);


					if (apiCheck)
					{
						//var apiCall = api.CreateMember(loginID);

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
					string UUID = Guid.NewGuid().ToString();
					var apiCall = api.BalanceTransfer(gameMap.GamePlayerID, Amount.ToString("0.00"), 0, RequestID, UUID);
					InsertLog(gameID, "Deposit", "request", api.Request, "response", api.Response, "error", api.ErrorMessage);

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
				Amount = -Amount;
				if (Amount <= 0)
				{
					string UUID = Guid.NewGuid().ToString();
					var apiCall = api.BalanceTransfer(gameMap.GamePlayerID, Amount.ToString("0.00"), 0, RequestID, UUID);
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

		public override vm.GetBalanceVM GetBalance()
		{
			var res = new vm.GetBalanceVM();

			try
			{
				if (gameMap != null)
				{
					string UUID = Guid.NewGuid().ToString();
					var apiCall = api.BalanceGet(gameMap.GamePlayerID, UUID);
					InsertLog(gameID, "GetBalance", "request", api.Request, "response", api.Response);

					targetApiUrl = api.Request;
					request = api.Request;
					response = api.Response;

					if (apiCall)
					{
						res.PlayerID = playerID;
						res.Balance = Convert.ToDecimal(api.BalanceGetResponse.result);
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
				string UUID = Guid.NewGuid().ToString();
				var apiCall = api.BalanceGet(gameMap.GamePlayerID, UUID);
				InsertLog(gameID, "GetBalance", "request", api.Request, "response", api.Response);

				targetApiUrl = api.Request;
				request = api.Request;
				response = api.Response;

				if (apiCall)
				{
					return Convert.ToDecimal(api.BalanceGetResponse.result);
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
					bool keepExistingToken = true;
					if (!string.IsNullOrEmpty(ipAddr)) ipAddr = ipAddr.Split(':')[0];
					string userAgent = HttpContext.Current.Request.UserAgent;

					langId = GetGameLanguage(gameID, langId);
					int mobileFlag = IsMobile == true ? 1 : 0;
					string UUID = Guid.NewGuid().ToString();
					var apiLoginCall = api.GameGetUrl(gameMap.GamePlayerID, langId, mobileFlag, 0, string.Empty, ipAddr, UUID);
					InsertLog(gameID, "GetGameUrl", "request", api.Request, "response", api.Response);

					targetApiUrl = api.Request;
					request = api.Request;
					response = api.Response;

					if (apiLoginCall)
					{
						res.PlayerID = playerID;
						res.Url = api.GameUrlGetResponse.result;
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
				string uuid = System.Guid.NewGuid().ToString();
				var apiCall = api.UserLogout(gameMap.GamePlayerID, uuid);
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

		public BG.Model.GetReportResult GetBrandCompletedGameResultsV2(string dateFrom, string dateTo, int pageIndex = 1, int pageSize = 500)
		{
			api = new BG.BG(apiUrl, secretKey, agentPassword, agentLoginId, sn, timeOutSec);
			string UUID = Guid.NewGuid().ToString();
			var apiCall = api.GetReport(UUID, null, null, dateFrom, dateTo, pageIndex, pageSize);

			targetApiUrl = api.Request;
			request = api.Request;
			response = api.Response;

			//InsertLog(gameID, "BGGameRecord", "request", api.Request, "response", api.Response);

			if (apiCall)
			{
				return api.GetReportResponse.result;
			}
			return null;
		}
	}
}