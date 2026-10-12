using System;
using System.Linq;
using System.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using GameRecord.Models;
using vm = GameRecord.Controllers.GameAPIController;
using System.Collections.Generic;

namespace GameRecord.Controllers
{
	public class ABSConnector : BaseConnector
	{
		private string gameID = "ABS";
		private string apiUrl = "";
		private string reportUrl = "";
		private string agent = "";
		private string parentId = "";
		private string hashCode = "";
		private string walletType = "";
		private string payout = "";
		private int timeOutSec = 30;
		private string language = "en";
		private GameMapping gameMap;
		private ABS.ABS api = null;
		private string lobbyUrl = HttpContext.Current.Request.Url.AbsoluteUri.Replace(HttpContext.Current.Request.Url.PathAndQuery, "/") + "ABS/Lobby";


		public ABSConnector(string PlayerID = "", string CurrencyID = "", string IPAddr = "", GameEnvironment Environment = GameEnvironment.PROD) : base(PlayerID, CurrencyID, IPAddr, Environment)
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
					apiUrl = jObj["ABSApiUrl"].ToString();
					agent = jObj["ABSAgent"].ToString();
					parentId = jObj["ABSParentId"].ToString();
					hashCode = jObj["ABSHashCode"].ToString();
					walletType = jObj["ABSWalletType"].ToString();
					payout = jObj["ABSPayout"].ToString();
					timeOutSec = Convert.ToInt32(jObj["ABSTimeOutSec"].ToString());
					//apiUrl = apiUrl.Substring(0, apiUrl.LastIndexOf('/')); ;
					api = new ABS.ABS(apiUrl, agent, parentId, hashCode, walletType, payout, timeOutSec);
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
					string trackerId = Guid.NewGuid().ToString();
					string language = "en";
					string langId = GetGameLanguage(gameID, language);
					var apiCall = api.Login(loginID, loginPass, currencyID, 0, trackerId, langId);
					InsertLog(gameID, "CreateUser", "request", api.Request, "response", api.Response);

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
				var apiCall = api.Deposit(gameMap.GamePlayerID, gameMap.GamePassword, gameMap.Currency, Convert.ToDouble(Amount), TrackerId, language);
				InsertLog(gameID, "ABSDeposit", "request", api.Request, "response", api.Response);

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
				var apiCall = api.Withdraw(gameMap.GamePlayerID, gameMap.GamePassword, gameMap.Currency, Convert.ToDouble(Amount), TrackerId, language);
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
					string trackerId = Guid.NewGuid().ToString();
					var apiCall = api.GetBalance(gameMap.GamePlayerID, gameMap.GamePassword, gameMap.Currency, trackerId, language);
					InsertLog(gameID, "GetBalance", "request", api.Request, "response", api.Response);

					targetApiUrl = api.Request;
					request = api.Request;
					response = api.Response;

					if (apiCall)
					{
						res.PlayerID = playerID;
						res.Balance = Convert.ToDecimal(api.GetBalanceResponse.Balance);
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
				string trackerId = Guid.NewGuid().ToString();
				var apiCall = api.GetBalance(gameMap.GamePlayerID, gameMap.GamePassword, gameMap.Currency, trackerId, language);
				InsertLog(gameID, "GetBalance", "request", api.Request, "response", api.Response);

				targetApiUrl = api.Request;
				request = api.Request;
				response = api.Response;

				if (apiCall)
				{
					return Convert.ToDecimal(api.GetBalanceResponse.Balance);
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
					InsertLog(gameID, "GetGameUrlABS", "langId", langId, "IsMobile", IsMobile, "gameID", gameID);

					var sessionId = Guid.NewGuid().ToString();
					langId = GetGameLanguage(gameID, langId);
					var apiCall = api.GetBalance(gameMap.GamePlayerID, gameMap.GamePassword, gameMap.Currency, sessionId, langId);
					if (apiCall)
					{

						var apiCallLogin = api.Login(gameMap.GamePlayerID, gameMap.GamePassword, gameMap.Currency, 0, sessionId, langId);

						targetApiUrl = api.Request;
						request = api.Request;
						response = api.Response;

						string url = api.LoginResponse.LoginURL;

						InsertLog(gameID, "GetGameUrlABS", "langId", langId, "IsMobile", IsMobile, "gameID", gameID, "url", url);

						res.PlayerID = playerID;
						res.Url = url;
						res.Game = gameID;
						res.Message = "Success";
						res.Code = 0;
					}
					else
					{
						res.PlayerID = playerID;
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
				_common.LogError("GetGameUrlABS" + " " + DateTime.Now.ToString() + " " + string.Format("{0}", playerID) + " " + ex.Message.ToString(), gameID);

				var err = new Error
				{
					ErrorCode = "999",
					ErrorDesc = ex.Message.ToString()
				};

				InsertLog(gameID, "GetGameUrlABS", "Error", JsonConvert.SerializeObject(err));

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
				var sessionId = new Guid().ToString();
				var apiCall = api.SuspendUser(gameMap.GamePlayerID, sessionId, language);
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

		public ABS.Model.GrabTicketResponse RetrieveTransactionByMinute(int startrec, string langId = "en")
		{
			var sessionId = new Guid().ToString();
			langId = GetGameLanguage(gameID, langId);
			var apiCall = api.GrabTicketRecord(startrec, sessionId, langId);


			targetApiUrl = api.Request;
			request = api.Request;
			response = api.Response;

			if (apiCall)
			{
				InsertLog(gameID, "ABSJob", "request", api.Request, "response", api.Response);

				return api.GrabTicketResponse;
			}
			return null;
		}

		public ABS.Model.HitRecordReportResponse RetrieveHitTransactionByMinute(string drawDate, string langId = "en")
		{
			var sessionId = new Guid().ToString();
			langId = GetGameLanguage(gameID, langId);
			var apiCall = api.HitRecordReport(drawDate, sessionId, langId);

			InsertLog(gameID, "ABSJobHit", "request", api.Request, "response", api.Response);

			targetApiUrl = api.Request;
			request = api.Request;
			response = api.Response;

			if (apiCall)
			{
				return api.HitRecordReportResponse;
			}
			return null;
		}
	}
}