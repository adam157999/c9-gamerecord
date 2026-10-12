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
	public class AESexyConnector : BaseConnector
	{
		private string gameID = "SEXYGAMING";
		private string url = "";
		private string fetchUrl = "";
		private string cert = "";
		private string agentId = "";
		private string platform = "";
		private string gameType = "";
		private string gameCode = "";
		//private int providerId = 6;
		private string betLimitId = "";
		private int timeOutSec = 30;
		private GameMapping gameMap;
		private AESexy.SXB api = null;

		public AESexyConnector(string PlayerID = "", string CurrencyID = "", string IPAddr = "", GameEnvironment Environment = GameEnvironment.PROD) : base(PlayerID, CurrencyID, IPAddr, Environment)
		{
			gameMap = LoadGameMap(gameID, playerID, currencyID);

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
					url = jObj["SXBUrl"].ToString();
					fetchUrl = jObj["SXBFetchUrl"].ToString();
					cert = jObj["SXBCert"].ToString();
					agentId = jObj["SXBAgentId"].ToString();
					gameType = jObj["SXBGameType"].ToString();
					gameCode = jObj["SXBGameCode"].ToString();
					platform = jObj["SXBPlatform"].ToString();
					betLimitId = jObj["SXBBetLimitId"].ToString();
					timeOutSec = Convert.ToInt32(jObj["SXBTimeOutSec"].ToString());

					api = new AESexy.SXB(url, cert, agentId, "POST", timeOutSec);
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
					var sessionId = new Guid().ToString();
					var apiCall = api.CreateMember(loginID, currencyID, platform, gameType, betLimitId);
					InsertLog(gameID, "CreateMember", "request", api.request, "response", api.response, "url", api.targetApiUrl, "error", api.errorMessage);

					targetApiUrl = api.targetApiUrl;
					request = api.request;
					response = api.response;

					if (apiCall)
					{
						InsertGameMapping(gameID, loginID, loginPass, mcUID);
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
				_common.LogError("CreateMember" + " " + DateTime.Now.ToString() + " " + string.Format("{0}", playerID) + " " + ex.Message.ToString(), gameID);

				var err = new
				{
					ErrorCode = "999",
					ErrorDesc = ex.Message.ToString()
				};

				InsertLog(gameID, "CreateMember_Exception", "Error", JsonConvert.SerializeObject(err));

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
				var apiCall = api.Deposit(gameMap.GamePlayerID, TrackerId, Amount);
				InsertLog(gameID, "Deposit", "request", api.request, "response", api.response);

				targetApiUrl = api.targetApiUrl;
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
				var apiCall = api.Withdraw(gameMap.GamePlayerID, TrackerId, Amount);
				InsertLog(gameID, "Withdraw", "request", api.request, "response", api.response);

				targetApiUrl = api.targetApiUrl;
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
					var apiCall = api.GetBalance(gameMap.GamePlayerID);
					InsertLog(gameID, "GetBalance", "request", api.request, "response", api.response);

					targetApiUrl = api.targetApiUrl;
					request = api.request;
					response = api.response;

					if (apiCall)
					{
						res.PlayerID = playerID;
						res.Balance = api.getBalanceResponse.get_balance(gameMap.GamePlayerID);
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
				var apiCall = api.GetBalance(gameMap.GamePlayerID);
				InsertLog(gameID, "GetBalance", "request", api.request, "response", api.response);

				targetApiUrl = api.targetApiUrl;
				request = api.request;
				response = api.response;

				if (apiCall)
				{
					return Convert.ToDecimal(api.getBalanceResponse.get_balance(gameMap.GamePlayerID));
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
					var apiCall = api.DoLoginAndLaunchGame(gameMap.GamePlayerID, gameType, platform, IsMobile, langId, gameCode);

					targetApiUrl = api.targetApiUrl;
					request = api.request;
					response = api.response;

					InsertLog(gameID, "LaunchGame", "request", api.request, "response", api.response);

					if (apiCall)
					{
						res.PlayerID = playerID;
						res.Url = api.doLoginAndLaunchGameResponse.url;
						res.Game = gameID;
						res.Message = "Success";
						res.Code = 0;
					}
					else
					{
						res.PlayerID = playerID;
						res.Url = null;
						res.Game = gameID;
						res.Message = api.errorMessage;
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
				_common.LogError("LaunchGame" + " " + DateTime.Now.ToString() + " " + string.Format("{0}", playerID) + " " + ex.Message.ToString(), gameID);

				var err = new Error
				{
					ErrorCode = "999",
					ErrorDesc = ex.Message.ToString()
				};

				InsertLog(gameID, "LaunchGame_Exception", "Error", JsonConvert.SerializeObject(err));

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

		public AESexy.getTransactionByUpdateDateResponse getTransactionByUpdateDate(DateTime dateFrom)
		{
			api = new AESexy.SXB(fetchUrl, cert, agentId, "POST", timeOutSec);
			var apiCall = api.GetTransactionByUpdateDate(dateFrom.ToString("yyyy-MM-ddTHH:mm:sszzz"), platform: platform, currency: currencyID, gameType: gameType);

			//InsertLog(gameID, "SEXYGameRecord", "request", api.request, "response", api.response, "dateFrom", dateFrom.ToString("yyyy-MM-ddTHH:mm:sszzz"));

			targetApiUrl = api.targetApiUrl;
			request = api.request;
			response = api.response;

			if (apiCall)
			{
				return api.getTransactionByUpdateDateResponse;
			}
			return null;
		}

		public AESexy.getSummaryByTxTimeHourResponse getSummaryByTxTimeHour(DateTime startTime, DateTime endTime)
		{
			api = new AESexy.SXB(fetchUrl, cert, agentId, "POST", timeOutSec);
			var apiCall = api.GetSummaryByTxTimeHour(startTime.ToString("yyyy-MM-ddTHHzzz"), endTime.ToString("yyyy-MM-ddTHHzzz"), platform: platform, gameType: gameType);

			targetApiUrl = api.targetApiUrl;
			request = api.request;
			response = api.response;

			if (apiCall)
			{
				return api.getSummaryByTxTimeHourResponse;
			}
			return null;
		}

		public AESexy.getTransactionByTxTimeResponse getTransactionByTxTime(DateTime startTime, DateTime endTime)
		{
			api = new AESexy.SXB(fetchUrl, cert, agentId, "POST", timeOutSec);
			var apiCall = api.GetTransactionByTxTime(startTime.ToString("yyyy-MM-ddTHH:mm:sszzz"), endTime.ToString("yyyy-MM-ddTHH:mm:sszzz"), platform: platform, gameType: gameType);

			//InsertLog(gameID, "SEXYGameRecord", "request", api.request, "response", api.response, "startTime", startTime.ToString("yyyy-MM-ddTHH:mm:sszzz"), "endTime", endTime.ToString("yyyy-MM-ddTHH:mm:sszzz"));


			targetApiUrl = api.targetApiUrl;
			request = api.request;
			response = api.response;

			if (apiCall)
			{
				return api.getTransactionByTxTimeResponse;
			}
			return null;
		}
	}
}