using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text;
using GameRecord.Models;
using System.Data.Entity.Validation;
using vm = GameRecord.Controllers.GameAPIController;

namespace GameRecord.Controllers
{
	public class KISS918Connector : BaseConnector
	{
		private string gameID = "KISS918";
		private string apiUrl = "";
		private string api2Url = "";
		private string agent = "";
		private string secretKey = "";
		private string authCode = "";
		private string downloadUrl = "";
		private int timeOutSec = 30;
		private GameMapping gameMap;
		private KISS918.KISS918 api = null;

		public KISS918Connector(string PlayerID = "", string CurrencyID = "", string IPAddr = "", GameEnvironment Environment = GameEnvironment.PROD) : base(PlayerID, CurrencyID, IPAddr, Environment)
		{
			using (BingoConn db = new BingoConn())
			{
				gameMap = db.GameMappings.Where(x => x.Game.ToUpper().Trim() == gameID.ToUpper().Trim() &&
																								x.PlayerID == playerID &&
																								x.Currency == currencyID &&
																								x.IsActive == true).FirstOrDefault();

				var apiConfig = CatchData.GetApiKeys(gameID.ToUpper().Trim(), Environment.ToString(), currencyID.ToUpper().Trim());
				if (string.IsNullOrEmpty(CurrencyID)) apiConfig = CatchData.GetApiKeys(gameID.ToUpper().Trim(), Environment.ToString());

				if (apiConfig != null)
				{
					JObject jObj = JObject.Parse(apiConfig.ApiConfig);
					apiUrl = jObj["KISS918ApiUrl"].ToString();
					api2Url = jObj["KISS918Api2Url"].ToString();
					agent = jObj["KISS918Agent"].ToString();
					secretKey = jObj["KISS918SecretKey"].ToString();
					authCode = jObj["KISS918AuthCode"].ToString();
					downloadUrl = jObj["KISS918DownloadUrl"].ToString();
					timeOutSec = Convert.ToInt32(jObj["KISS918TimeOutSec"].ToString());

					api = new KISS918.KISS918(apiUrl, api2Url, agent, secretKey, authCode, timeOutSec);
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
					var rapiCall = api.RandomUserName();
					if (rapiCall)
					{
						string passwords = _common.RandomString(6);
						var apiCall = api.CreateUser(fullName, api.randomUserNameResponse.account, passwords);
						InsertLog(gameID, "CreateUser", "request", api.request, "response", api.response);

						targetApiUrl = api.targetUrl;
						request = api.request;
						response = api.response;

						if (apiCall)
						{
							InsertGameMapping(gameID, api.randomUserNameResponse.account, passwords, playerID);
							gameMap = LoadGameMap(gameID, playerID, currencyID);

							res.PlayerID = playerID;
							res.GamePlayerID = api.randomUserNameResponse.account;
							res.Game = gameID;
							res.Message = "Success";
							res.Code = 0;
						}
						else
						{
							res.PlayerID = playerID;
							res.GamePlayerID = null;
							res.Game = gameID;
							res.Message = api.errorMessage;
							res.Code = 2;
						}
					}
					else
					{
						res.PlayerID = playerID;
						res.GamePlayerID = null;
						res.Game = gameID;
						res.Message = api.errorMessage;
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

				InsertLog(gameID, "CreateUser_Exception", "Error", JsonConvert.SerializeObject(err));

				res.PlayerID = playerID;
				res.GamePlayerID = loginID;
				res.Game = gameID;
				res.Message = ex.Message.ToString();
				res.Code = 99;
			}

			return res;
		}

		public override bool GameDeposit(decimal Amount, string TrackerId, ref Error Error)
		{
			if (gameMap != null)
			{
				var apiCall = api.Deposit(gameMap.GamePlayerID, Amount, TrackerId);
				InsertLog(gameID, "Deposit", "request", api.request, "response", api.response);

				targetApiUrl = api.targetUrl;
				request = api.request;
				response = api.response;

				if (apiCall)
				{
					return true;
				}
				else
				{
					Error = new Error
					{
						ErrorCode = "-9999",
						ErrorDesc = api.errorMessage
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

		public override bool GameWithdraw(decimal Amount, string TrackerId, ref Error Error)
		{
			if (gameMap != null)
			{
				var apiCall = api.Withdraw(gameMap.GamePlayerID, Amount, TrackerId);
				InsertLog(gameID, "Withdraw", "request", api.request, "response", api.response);

				targetApiUrl = api.targetUrl;
				request = api.request;
				response = api.response;

				if (apiCall)
				{
					return true;
				}
				else
				{
					Error = new Error
					{
						ErrorCode = "-9999",
						ErrorDesc = api.errorMessage
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
					var apiCall = api.GetUserInfo(gameMap.GamePlayerID);
					InsertLog(gameID, "GetBalance", "request", api.request, "response", api.response);

					targetApiUrl = api.targetUrl;
					request = api.request;
					response = api.response;

					if (apiCall)
					{
						res.PlayerID = playerID;
						res.Balance = api.getUserInfoResponse.ScoreNum;
						res.Game = gameID;
						res.Message = "Successfully get balance.";
						res.Code = 0;
					}
					else
					{
						res.PlayerID = playerID;
						res.Balance = null;
						res.Game = gameID;
						res.Message = api.errorMessage;
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
				_common.LogError("GetBalance" + " " + DateTime.Now.ToString() + " " + string.Format("{0}", playerID) + " " + ex.Message.ToString(), gameID);

				var err = new Error
				{
					ErrorCode = "999",
					ErrorDesc = ex.Message.ToString()
				};

				InsertLog(gameID, "GetBalance_Exception", "Error", JsonConvert.SerializeObject(err));

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
				var apiCall = api.GetUserInfo(gameMap.GamePlayerID);
				InsertLog(gameID, "GetBalance", "request", api.request, "response", api.response);

				targetApiUrl = api.targetUrl;
				request = api.request;
				response = api.response;

				if (apiCall)
				{
					return api.getUserInfoResponse.ScoreNum;
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
					string deeplink = null;

					res.PlayerID = playerID;
					res.Url = downloadUrl;
					res.Game = gameID;
					res.Message = "Success";
					res.Code = 0;
					res.ApkInfo = new vm.GameApkVM()
					{
						Android = new vm.GameApkInfoVM()
						{
							DownloadLink = downloadUrl,
							Package = null,
							Activity = null,
							DeepLink = deeplink,
							GamePlayerID = gameMap.GamePlayerID,
							Passwords = gameMap.GamePassword
						},
						iOS = new vm.GameApkInfoVM()
						{
							DownloadLink = downloadUrl,
							Package = null,
							Activity = null,
							DeepLink = deeplink,
							GamePlayerID = gameMap.GamePlayerID,
							Passwords = gameMap.GamePassword
						}
					};
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

		public KISS918.AgentTotalReportResponse AgentTotalReport(string userId, DateTime startTime, DateTime endTime)
		{
			var apiCall = api.AgentTotalReport(userId, startTime, endTime);

			targetApiUrl = api.targetUrl;
			request = api.request;
			response = api.response;

			if (apiCall)
			{
				return api.agentTotalReportResponse;
			}
			return null;
		}

		public KISS918.GameLogResponse GameLog(string userId, DateTime startTime, DateTime endTime, int pageIndex = 1, int pageSize = 1000)
		{
			var apiCall = api.GameLog(userId, startTime, endTime, pageIndex, pageSize);

			targetApiUrl = api.targetUrl;
			request = api.request;
			response = api.response;

			if (apiCall)
			{
				return api.gameLogResponse;
			}
			return null;
		}
	}
}