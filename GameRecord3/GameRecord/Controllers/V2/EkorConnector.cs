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
	public class EkorConnector : BaseConnector
	{
		private int gameUniqueID = 0;
		private string gameID = "EKOR";
		private string apiUrl = "";
		private string domainName = "";
		private string sn = "";
		private string apiSecretKey = "";
		private int timeOutSec = 30;
		private string noneAllCashOutFlag = "0";
		private GameMapping gameMap;
		private Ekor.Ekor api = null;
		Common _common = new Common();

		public EkorConnector(string PlayerID = "", string CurrencyID = "", string IPAddr = "", GameEnvironment Environment = GameEnvironment.PROD) : base(PlayerID, CurrencyID, IPAddr, Environment)
		{
			using (BingoConn db = new BingoConn())
			{
				gameMap = db.GameMappings
					.FirstOrDefault(x => x.Game.ToUpper().Trim() == gameID.ToUpper().Trim() && x.PlayerID == playerID && x.Currency.ToUpper() == currencyID && x.IsActive == true);

				var apiConfig = CatchData.GetApiKeys(gameID.ToUpper().Trim(), Environment.ToString(), currencyID.ToUpper().Trim());
				if (string.IsNullOrEmpty(CurrencyID)) apiConfig = CatchData.GetApiKeys(gameID.ToUpper().Trim(), Environment.ToString());

				if (apiConfig != null)
				{
					JObject jObj = JObject.Parse(apiConfig.ApiConfig);
					apiUrl = jObj["ApiUrl"].ToString();
					sn = jObj["Sn"].ToString();
					domainName = jObj["DomainName"].ToString();
					apiSecretKey = jObj["ApiSecretKey"].ToString();
					timeOutSec = Convert.ToInt32(jObj["TimeOutSec"].ToString());
					api = new Ekor.Ekor(apiUrl, sn, apiSecretKey, timeOutSec);
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

					var apiCheck = api.Login(guid, loginID);
					InsertLog(gameID, "Login", "TargetApiUrl", api.TargetApiUrl, "Request", api.Request, "Response", api.Response, "ErrorMessage", api.ErrorMessage, "apiCheck", apiCheck.ToString());

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
				if (Amount >= 0)
				{
					var apiCall = api.TransferBalance(RequestID, gameMap.GamePlayerID, 0, Amount);
					InsertLog(gameID, "GameDeposit-TransferBalance", "TargetApiUrl", api.TargetApiUrl, "Request", api.Request, "Response", api.Response, "ErrorMessage", api.ErrorMessage, "apiCheck", apiCall.ToString());

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
				var apiCall = api.TransferBalance(RequestID, gameMap.GamePlayerID, 0, Amount * -1);
				InsertLog(gameID, "GameWithdraw-TransferBalance", "TargetApiUrl", api.TargetApiUrl, "Request", api.Request, "Response", api.Response, "ErrorMessage", api.ErrorMessage, "apiCheck", apiCall.ToString());

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
					InsertLog(gameID, "GetBalance", "TargetApiUrl", api.TargetApiUrl, "Request", api.Request, "Response", api.Response, "ErrorMessage", api.ErrorMessage, "apiCheck", apiCall.ToString());

					if (apiCall)
					{
						res.PlayerID = playerID;
						res.Balance = api.GetBalanceResponse.Body.Balance;
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
				InsertLog(gameID, "GetGameBalance-GetBalance", "TargetApiUrl", api.TargetApiUrl, "Request", api.Request, "Response", api.Response, "ErrorMessage", api.ErrorMessage, "apiCheck", apiCall.ToString());

				if (apiCall)
				{
					return Convert.ToDecimal(api.GetBalanceResponse.Body.Balance);
				}
				else if (api.Response.Contains("F0006")) //retry due to player deleted by vendor
				{
					var apiCall2 = api.Login(Guid.NewGuid().ToString(), gameMap.GamePlayerID);
					InsertLog(gameID, "GetGameBalance-Login", "TargetApiUrl", api.TargetApiUrl, "Request", api.Request, "Response", api.Response, "ErrorMessage", api.ErrorMessage, "apiCall2", apiCall2.ToString());

					if (apiCall2)
					{
						var apiCall3 = api.GetBalance(Guid.NewGuid().ToString(), gameMap.GamePlayerID);
						InsertLog(gameID, "GetGameBalance-GetBalance-Retry", "TargetApiUrl", api.TargetApiUrl, "Request", api.Request, "Response", api.Response, "ErrorMessage", api.ErrorMessage, "apiCall3", apiCall3.ToString());

						if (apiCall3)
							return Convert.ToDecimal(api.GetBalanceResponse.Body.Balance);
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

					apiLoginCall = api.Login(guid, gameMap.GamePlayerID);
					InsertLog(gameID, "Login", "TargetApiUrl", api.TargetApiUrl, "Request", api.Request, "Response", api.Response, "ErrorMessage", api.ErrorMessage, "apiLoginCall", apiLoginCall.ToString());

					if (apiLoginCall)
					{
						res.Url = api.LoginResponse.Body;
						res.PlayerID = playerID;
						res.Game = gameID;
						res.Message = "Success";
						res.Code = 0;
					}
					else if (api.Response.Contains("E102")) //retry due to player deleted by vendor
					{
						var apiCall2 = api.Login(Guid.NewGuid().ToString(), gameMap.GamePlayerID);
						InsertLog(gameID, "Login-Retry", "TargetApiUrl", api.TargetApiUrl, "Request", api.Request, "Response", api.Response, "ErrorMessage", api.ErrorMessage, "apiCall2", apiCall2.ToString());
						if (apiCall2)
						{
							if (apiCall2)
							{
								res.Url = api.LoginResponse.Body;
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
	}
}