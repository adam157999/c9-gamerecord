using System;
using System.Linq;
using System.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using GameRecord.Models;
using vm = GameRecord.Controllers.GameAPIController;
using System.Collections.Generic;
using GameRecord.Gamess.Relax;

namespace GameRecord.Controllers
{
    public class RelaxConnector : BaseConnector
	{
		private string gameID = "RELAX";
		private string currencyID = "MYR";
		private string apiUrl = "";
		private string reportUrl = "";
		private string apiKey = "";
		public string iconUrl = "";
		private string brandId = "";
		private string country = "MY";
		private string channel = "pc";
		private string provider = "";
		private int timeOutSec = 30;
		private GameMapping gameMap;
		private Relax api = null;
		private string lobbyUrl = HttpContext.Current.Request.Url.AbsoluteUri.Replace(HttpContext.Current.Request.Url.PathAndQuery, "/") + "Relax/Lobby";


		public RelaxConnector(string PlayerID = "", string CurrencyID = "", string IPAddr = "", GameEnvironment Environment = GameEnvironment.PROD) : base(PlayerID, CurrencyID, IPAddr, Environment)
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
					apiUrl = jObj["RelaxApiUrl"].ToString();
					apiKey = jObj["RelaxApiKey"].ToString();
					brandId = jObj["RelaxBrandId"].ToString();
					reportUrl = jObj["RelaxReportUrl"].ToString();
					provider = jObj["RelaxProvider"].ToString();
					iconUrl = jObj["RelaxIconUrl"].ToString();
					timeOutSec = Convert.ToInt32(jObj["RelaxTimeOutSec"].ToString());

					api = new Relax(apiUrl,apiKey,brandId,reportUrl,provider);
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
					//var apiCheck = api.GetBalance(loginID,gameMap.Currency);
					//if (apiCheck)
					//{

					//}
					//else
					//{
					//	res.PlayerID = playerID;
					//	res.GamePlayerID = null;
					//	res.Game = gameID;
					//	res.Message = api.ErrorMessage;
					//	res.Code = 2;
					//}
					string language = "en";
					string langId = GetGameLanguage(gameID, language);
					var apiCall = api.LoginGame(loginID, "150205", currencyID, langId, channel, country);
					InsertLog(gameID, "LoginGame", "request", api.Request, "Parameters", api.Parameters, "response", api.Response);

					targetApiUrl = api.Request;
					request = api.Request;
					response = api.Response;

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

		public override bool GameDeposit(decimal Amount, string TrackerId, ref Error Error)
		{
			if (gameMap != null)
			{
				TrackerId = gameMap.PlayerID + DateTime.Now.ToString("yyyyMMddHHmmss");
				var apiCall = api.Credit(gameMap.GamePlayerID, Amount, TrackerId,gameMap.Currency,country);
				InsertLog(gameID, "RelaxDeposit", "request", api.Request, "Parameters", api.Parameters, "response", api.Response);

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

		public override bool GameWithdraw(decimal Amount, string TrackerId, ref Error Error)
		{
			if (gameMap != null)
			{
				TrackerId = gameMap.PlayerID + DateTime.Now.ToString("yyyyMMddHHmmss");
				var apiCall = api.Withdraw(gameMap.GamePlayerID, Amount, TrackerId,gameMap.Currency,country);
				InsertLog(gameID, "Withdraw", "request", api.Request, "Parameters", api.Parameters, "response", api.Response);

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
					var apiCall = api.GetBalance(gameMap.GamePlayerID,gameMap.Currency);
					InsertLog(gameID, "GetBalance", "request", api.Request, "Parameters", api.Parameters, "response", api.Response);

					targetApiUrl = api.Request;
					request = api.Request;
					response = api.Response;

					if (apiCall)
					{
						res.PlayerID = playerID;
						res.Balance = api.GetBalanceResponse.data.balance;
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
				var apiCall = api.GetBalance(gameMap.GamePlayerID,gameMap.Currency);
				InsertLog(gameID, "GetBalance", "request", api.Request, "Parameters", api.Parameters, "response", api.Response);

				targetApiUrl = api.Request;
				request = api.Request;
				response = api.Response;

				if (apiCall)
				{
					return Convert.ToDecimal(api.GetBalanceResponse.data.balance);
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
					InsertLog(gameID, "GetGameUrlRelax", "langId", langId, "IsMobile", IsMobile, "gameID", gameID);

					var sessionId = new Guid().ToString();
					langId = GetGameLanguage(gameID, langId);
					//var apiCall = api.PlayGame(gameMap.GamePlayerID);

					//targetApiUrl = api.Request;
					//request = api.Request;
					//response = api.Response;

					string url = string.Format("{0}?username={1}&langid={2}&currency={3}", lobbyUrl, gameMap.GamePlayerID, langId,gameMap.Currency);

					InsertLog(gameID, "GetGameUrlRelax", "langId", langId, "IsMobile", IsMobile, "gameID", gameID, "url", url);

					res.PlayerID = playerID;
					res.Url = url;
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
			catch (Exception ex)
			{
				_common.LogError("GetGameUrlRelax" + " " + DateTime.Now.ToString() + " " + string.Format("{0}", playerID) + " " + ex.Message.ToString(), gameID);

				var err = new Error
				{
					ErrorCode = "999",
					ErrorDesc = ex.Message.ToString()
				};

				InsertLog(gameID, "GetGameUrlRelax", "Error", JsonConvert.SerializeObject(err));

				res.PlayerID = playerID;
				res.Url = null;
				res.Game = gameID;
				res.Message = ex.Message.ToString();
				res.Code = 99;
			}

			return res;
		}

        #region Lobby
        public vm.GetBalanceVM GetBalanceLobby(string username)
		{
			var res = new vm.GetBalanceVM();

			try
			{
				if (!string.IsNullOrEmpty(username))
				{
					var apiCall = api.GetBalance(gameMap.GamePlayerID, gameMap.Currency);
					InsertLog(gameID, "GetBalance", "request", api.Request, "Parameters", api.Parameters, "response", api.Response);

					targetApiUrl = api.Request;
					request = api.Request;
					response = api.Response;

					if (apiCall)
					{
						res.PlayerID = playerID;
						res.Balance = api.GetBalanceResponse.data.balance;
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

		public vm.GetGameUrlVM GetSlotGameUrl(string username, string langId, string gameCode,string currency)
		{
			var res = new vm.GetGameUrlVM();

			try
			{
				if (!string.IsNullOrEmpty(username))
				{
					var sessionId = new Guid().ToString();
					//langId = GetGameLanguage(gameID, langId);
					string lobby = HttpUtility.UrlEncode(string.Format("{0}?username={1}&langid={2}&currency={3}", lobbyUrl, username, langId,currency));
					var apiCall = api.LoginGame(username, gameCode, currency, langId, channel,country);
					InsertLog(gameID, "LoginGame", "request", api.Request, "Parameters", api.Parameters, "response", api.Response);

					targetApiUrl = api.Request;
					request = api.Request;
					response = api.Response;

					if (apiCall)
					{
						res.PlayerID = playerID;
						res.Url = api.LoginResponse.data.game_url;
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
				_common.LogError("GetRelaxSlotGameUrl" + " " + DateTime.Now.ToString() + " " + string.Format("{0}", playerID) + " " + ex.Message.ToString(), gameID);

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

        public override bool KickPlayer(string GamePlayerId, ref Error Error)
		{
			if (gameMap != null)
			{
				var apiCall = api.KickPlayer(gameMap.GamePlayerID,gameMap.Currency);
				InsertLog(gameID, "KickPlayer", "request", api.Request, "Parameters", api.Parameters, "response", api.Response);

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

		public ReportResponse RetrieveTransactionByMinute(string startTime, string endTime,string currency, int pageIndex, int pageSize = 5000)
		{
			var apiCall = api.Report(pageIndex,startTime,endTime,currency,provider);

			InsertLog(gameID, "RelaxJob", "request", api.Request, "Parameters", api.Parameters, "response", api.Response);

			targetApiUrl = api.Request;
			request = api.Request;
			response = api.Response;

			if (apiCall)
			{
				return api.ReportResponse;
			}
			return null;
		}

		public GetGameListResponse GetGamesList()
		{
			var apiCall = api.GetGameList();

			targetApiUrl = api.Request;
			request = api.Request;
			response = api.Response;

			if (apiCall)
			{
				return api.GetGameListResponse;
			}
			return null;
		}
	}
}