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
	public class LKL365Connector : BaseConnector
	{
		// GET: HabaneroConnector
		private int gameUniqueID = 0;
		private string gameID = "LKL365";
		private string apiUrl = "";
		private string domainName = "";
		private string sn = "";
		private string apiSecretKey = "";
		private int timeOutSec = 30;
		private string noneAllCashOutFlag = "0";
		private GameMapping gameMap;
		private LK_L365.LKL365 api = null;
		Common _common = new Common();

		public LKL365Connector(string PlayerID = "", string CurrencyID = "", string IPAddr = "", GameEnvironment Environment = GameEnvironment.PROD) : base(PlayerID, CurrencyID, IPAddr, Environment)
		{
			using (BingoConn db = new BingoConn())
			{
				gameMap = db.GameMappings.FirstOrDefault(x => x.Game.ToUpper().Trim() == gameID.ToUpper().Trim() && x.PlayerID == playerID && x.Currency.ToUpper() == currencyID && x.IsActive == true);

				var apiConfig = CatchData.GetApiKeys(gameID.ToUpper().Trim(), Environment.ToString(), currencyID.ToUpper().Trim());
				if (string.IsNullOrEmpty(CurrencyID)) apiConfig = CatchData.GetApiKeys(gameID.ToUpper().Trim(), Environment.ToString());

				if (apiConfig != null)
				{
					JObject jObj = JObject.Parse(apiConfig.ApiConfig);
					apiUrl = jObj[$"LKL365ApiUrl"].ToString();
					sn = jObj[$"LKL365Sn"].ToString();
					domainName = jObj[$"LKL365DomainName"].ToString();
					apiSecretKey = jObj[$"LKL365ApiSecretKey"].ToString();
					timeOutSec = Convert.ToInt32(jObj[$"LKL365TimeOutSec"].ToString());
					api = new LK_L365.LKL365(apiUrl, sn, apiSecretKey, timeOutSec);
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
					string guid = Guid.NewGuid().ToString();
					string userAgent = HttpContext.Current.Request.UserAgent;

					var apiCheck = api.CreatePlayer(guid, fullName, loginID);

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
					var apiCall = api.TransferBalance(RequestID, gameMap.GamePlayerID, Amount, 0);
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
				var apiCall = api.TransferBalance(RequestID, gameMap.GamePlayerID, -1 * Amount, 0);
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
					var apiCall = api.GetBalance(Guid.NewGuid().ToString(), gameMap.GamePlayerID);
					InsertLog(gameID, "GetBalance", "request", api.Request, "response", api.Response);

					targetApiUrl = api.Request;
					request = api.Request;
					response = api.Response;

					if (apiCall)
					{
						res.PlayerID = playerID;
						res.Balance = api.GetBalanceResponse.data.Result;
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
				var apiCall = api.GetBalance(Guid.NewGuid().ToString(), gameMap.GamePlayerID);
				InsertLog(gameID, "GetGameBalance-GetBalance", "request", api.Request, "response", api.Response);

				if (apiCall)
				{
					return Convert.ToDecimal(api.GetBalanceResponse.data.Result);
				}
				else if (api.Response.Contains("F0006")) //retry due to player deleted by vendor
				{
					var apiCall2 = api.CreatePlayer(Guid.NewGuid().ToString(), gameMap.GamePlayerID, gameMap.GamePlayerID);
					InsertLog(gameID, "GetGameBalance-CreateUser", "request", api.Request, "response", api.Response);
					if (apiCall2)
					{
						var apiCall3 = api.GetBalance(Guid.NewGuid().ToString(), gameMap.GamePlayerID);
						InsertLog(gameID, "GetGameBalance-GetBalance-Retry", "request", api.Request, "response", api.Response);

						if (apiCall3)
							return Convert.ToDecimal(api.GetBalanceResponse.data.Result);
						else
							return null;
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
			else
			{
				return null;
			}
		}

		public override vm.GetGameUrlVM GetGameUrl(string langId, bool IsMobile = false)
		{
			var res = new vm.GetGameUrlVM();
			bool apiLoginCall = false;

			try
			{
				if (gameMap != null)
				{
					if (!string.IsNullOrEmpty(ipAddr)) ipAddr = ipAddr.Split(':')[0];
					string userAgent = HttpContext.Current.Request.UserAgent;
					string guid = Guid.NewGuid().ToString();
					langId = GetGameLanguage(gameID, langId);

					apiLoginCall = api.Login(guid, gameMap.GamePlayerID, langId, "");
					InsertLog(gameID, "GetGameUrl", "request", api.Request, "response", api.Response);

					if (apiLoginCall)
					{
						res.Url = domainName + api.LoginResponse.data.LoginUrl;
						res.PlayerID = playerID;
						res.Game = gameID;
						res.Message = "Success";
						res.Code = 0;
					}
					else if (api.Response.Contains("E102")) //retry due to player deleted by vendor
					{
						var apiCall2 = api.CreatePlayer(Guid.NewGuid().ToString(), gameMap.GamePlayerID, gameMap.GamePlayerID);
						InsertLog(gameID, "GetGameUrl-CreateUser", "request", api.Request, "response", api.Response);
						if (apiCall2)
						{
							var apiCall3 = api.Login(guid, gameMap.GamePlayerID, langId, "");
							InsertLog(gameID, "GetGameUrl-Retry", "request", api.Request, "response", api.Response);
							if (apiCall3)
							{
								res.Url = domainName + api.LoginResponse.data.LoginUrl;
								res.PlayerID = playerID;
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

		public LK_L365.Model.GetGameRecordByTimeResponseObject GetBrandCompletedGameResultsV2(string startTime, string endTime, int pageIndex, int pageSize = 30)
		{
			api = new LK_L365.LKL365(apiUrl, sn, apiSecretKey, timeOutSec);
			string guid = Guid.NewGuid().ToString();
			var apiCall = api.GetGameRecordByTime(guid, startTime, endTime, pageSize, pageIndex);

			InsertLog(gameID, "LKL365GameRecord", "request", api.Request, "response", api.Response);

			targetApiUrl = api.Request;
			request = api.Request;
			response = api.Response;

			if (apiCall)
			{
				return api.GetGameRecordByTimeResponse.data;
			}
			return null;
		}
	}
}