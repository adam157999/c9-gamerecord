using GameRecord.Models;
using GameRecord.Gamess.GFG;
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
	public class GFGConnector : BaseConnector
	{
		// GET: JiLiConnector
		private int gameUniqueID = 0;
		private string gameID = "GFG";
		private string apiUrl = "";
		private string agentKey = "";
		private string gameUrl = "";
		private string secretKey = "";
		private string companyKey = "";
		private string iconUrl = "";
		private string gameTheme = "";
		private string token = "";
		private int timeOutSec = 30;
		private GameMapping gameMap;
		private GFG api = null;
		private string lobbyUrl = HttpContext.Current.Request.Url.AbsoluteUri.Replace(HttpContext.Current.Request.Url.PathAndQuery, "/") + "GFG/Lobby";

		public GFGConnector(string PlayerID = "", string CurrencyID = "", string IPAddr = "", GameEnvironment Environment = GameEnvironment.PROD) : base(PlayerID, CurrencyID, IPAddr, Environment)
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
					apiUrl = jObj["GFGApiUrl"].ToString();
					companyKey = jObj["GFGCompanyKey"].ToString();
					agentKey = jObj["GFGAgentKey"].ToString();
					secretKey = jObj["GFGSecretKey"].ToString();
					gameTheme = jObj["GFGGameTheme"].ToString();
					iconUrl = jObj["GFGIconUrl"].ToString();
					timeOutSec = Convert.ToInt32(jObj["GFGTimeOutSec"].ToString());

					api = new GFG(apiUrl, companyKey, secretKey, agentKey, gameTheme, timeOutSec);
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
					loginID = agentKey + "_" + loginID;
					token = Guid.NewGuid().ToString();
					string timeStamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
					var apiCheck = api.Login(loginID, ipAddr, token, fullName, timeStamp, "en_us");
					if (apiCheck)
					{
						//var apiCall = api.CreateMember(loginID);
						InsertLog(gameID, "CreateUser", "request", api.Request, "response", api.Response, "error", api.ErrorMessage);

						targetApiUrl = api.Request;
						request = api.Request;
						response = api.Response;
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
						InsertLog(gameID, "CreateUser", "request", api.Request, "response", api.Response, "error", api.ErrorMessage);

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

		public override bool GameDeposit(decimal Amount, string OrderID, ref Error Error)
		{
			if (gameMap != null)
			{
				float money = Convert.ToSingle(Amount);
				string timeStamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
				var apiCall = api.DoTransferDepositTask(gameMap.GamePlayerID, OrderID, money, timeStamp);
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
					ErrorDesc = "Retrieve Success"
				};
				return false;
			}
		}

		public override bool GameWithdraw(decimal Amount, string OrderID, ref Error Error)
		{
			if (gameMap != null)
			{
				float money = Convert.ToSingle(Amount);
				string timeStamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
				var apiCall = api.DoTransferWithdrawTask(gameMap.GamePlayerID, OrderID, money, timeStamp);
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
					string timeStamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
					var apiCall = api.QueryUserScore(gameMap.GamePlayerID, timeStamp);
					InsertLog(gameID, "GetBalance", "request", api.Request, "response", api.Response);

					targetApiUrl = api.Request;
					request = api.Request;
					response = api.Response;

					if (apiCall)
					{
						res.PlayerID = playerID;
						res.Balance = Convert.ToDecimal(api.QueryUserScoreResponse.data.money);
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
				string timeStamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
				var apiCall = api.QueryUserScore(gameMap.GamePlayerID, timeStamp);
				InsertLog(gameID, "GetBalance", "request", api.Request, "response", api.Response);

				targetApiUrl = api.Request;
				request = api.Request;
				response = api.Response;

				if (apiCall)
				{
					return Convert.ToDecimal(api.QueryUserScoreResponse.data.money);
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
			int platform = 0;
			try
			{
				if (gameMap != null)
				{
					token = Guid.NewGuid().ToString();
					//langId = GetGameLanguage(gameID, langId);
					langId = GetGameLanguage(gameID, langId);
					string timeStamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();

					if (IsMobile == true)
					{
						platform = 1;
					}
					
					//var apiCall = api.Login(gameMap.GamePlayerID, ipAddr, token, gameMap.PlayerID, timeStamp, langId, 0);

					string url = string.Format("{0}?username={1}&langid={2}&currency={3}", lobbyUrl, gameMap.GamePlayerID, langId, gameMap.Currency);
					InsertLog(gameID, "GetGameUrl", "request", api.Request, "response", api.Response, "error", api.ErrorMessage);
					res.PlayerID = playerID;
					res.Url = url;
					res.Game = gameID;
					res.Message = "Success";
					res.Code = 0;

					//targetApiUrl = api.Request;
					//request = api.Request;
					//response = api.Response;

					//if (apiCall)
					//{
					//	res.PlayerID = playerID;
					//	res.Url = api.LoginResponse.data.url.ToString();
					//	res.Game = gameID;
					//	res.Message = "Success";
					//	res.Code = 0;
					//}
					//else
					//{
					//	res.PlayerID = playerID;
					//	res.Url = null;
					//	res.Game = gameID;
					//	res.Message = api.ErrorMessage;
					//	res.Code = 2;
					//}



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
				_common.LogError("UserLogin" + " " + DateTime.Now.ToString() + " " + string.Format("{0}", playerID) + " " + ex.Message.ToString(), gameID);

				var err = new Error
				{
					ErrorCode = "999",
					ErrorDesc = ex.Message.ToString()
				};

				InsertLog(gameID, "Login", "Error", JsonConvert.SerializeObject(err));

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
				var apiCall = api.KickUser(gameMap.GamePlayerID);
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

		public TakeBetLogsResponse RetrieveTransactionByTime(DateTime dateFrom, DateTime dateTo, int page, int size)
		{
			api = new GFG(apiUrl, companyKey, secretKey, agentKey, gameTheme, timeOutSec);

			string timeStamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
			var apiCall = api.TakeBetLogs(dateFrom.ToString("yyyy-MM-dd HH:mm:ss"), dateTo.ToString("yyyy-MM-dd HH:mm:ss"), size, page, timeStamp);

			targetApiUrl = api.Request;
			request = api.Request;
			response = api.Response;

			if (apiCall)
			{
				return api.TakeBetLogsResponse;
			}
			return null;
		}

		public GetGameListResult GetGamesList()
		{
			string timeStamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
			var apiCall = api.GetGameList(timeStamp);

			targetApiUrl = api.Request;
			request = api.Request;
			response = api.Response;

			if (apiCall)
			{
				return api.GetGameListResponse.data;
			}
			return null;
		}

		public string GetGameIconUrl(string language)
        {
			if(language.ToUpper().Contains("EN"))
            {
				return iconUrl = iconUrl + "/EN/";
            }
            else
            {
				return iconUrl = iconUrl + "/CN/";
            }
        }

		#region Lobby
		public vm.GetBalanceVM GetBalanceLobby(string username)
		{
			var res = new vm.GetBalanceVM();

			try
			{
				if (!string.IsNullOrEmpty(username))
				{
					string timeStamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
					var apiCall = api.QueryUserScore(username, timeStamp);
					InsertLog(gameID, "GetBalance", "request", api.Request, "response", api.Response);

					targetApiUrl = api.Request;
					request = api.Request;
					response = api.Response;

					if (apiCall)
					{
						res.PlayerID = playerID;
						res.Balance = Convert.ToDecimal(api.QueryUserScoreResponse.data.money);
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

		public vm.GetGameUrlVM GetSlotGameUrl(string username, string langId, int gameCode, string currency)
		{
			var res = new vm.GetGameUrlVM();

			try
			{
				if (!string.IsNullOrEmpty(username))
				{
					string displayUserName = "";
					if(username.Contains(agentKey+"_"))
                    {
						displayUserName = username.Replace(agentKey + "_","");
                    }
					var sessionId = new Guid().ToString();
					//langId = GetGameLanguage(gameID, langId);
					token = Guid.NewGuid().ToString();
					string timeStamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
					string lobby = HttpUtility.UrlEncode(string.Format("{0}?username={1}&langid={2}&currency={3}", lobbyUrl, username, langId, currency));
					var apiCall = api.Login(username, ipAddr, token, username, timeStamp, langId, gameCode); ;
					InsertLog(gameID, "GetGFGSlotGameUrl", "request", api.Request, "response", api.Response);

					targetApiUrl = api.Request;
					request = api.Request;
					response = api.Response;

					if (apiCall)
					{
						res.PlayerID = playerID;
						res.Url = api.LoginResponse.data.url.ToString();
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
				_common.LogError("GetGFGSlotGameUrl" + " " + DateTime.Now.ToString() + " " + string.Format("{0}", playerID) + " " + ex.Message.ToString(), gameID);

				var err = new Error
				{
					ErrorCode = "999",
					ErrorDesc = ex.Message.ToString()
				};

				InsertLog(gameID, "Login", "Error", JsonConvert.SerializeObject(err));

				res.PlayerID = playerID;
				res.Url = null;
				res.Game = gameID;
				res.Message = ex.Message.ToString();
				res.Code = 99;
			}

			return res;
		}
		#endregion
	}
}