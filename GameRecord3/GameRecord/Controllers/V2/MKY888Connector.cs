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
	public class MKY888Connector : BaseConnector
	{
		private string gameID = "MKY888";
		private string apiUrl = "";
		private string gameUrl = "";
		private string appId = "";
		private string secretKey = "";
		private int timeOutSec = 30;
		private GameMapping gameMap;
		private MKY888.MKY888 api = null;
		private string lobbyUrl = HttpContext.Current.Request.Url.AbsoluteUri.Replace(HttpContext.Current.Request.Url.PathAndQuery, "/") + "MKY888/Lobby";


		public MKY888Connector(string PlayerID = "", string CurrencyID = "", string IPAddr = "", GameEnvironment Environment = GameEnvironment.PROD) : base(PlayerID, CurrencyID, IPAddr, Environment)
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
					apiUrl = jObj["MKY888ApiUrl"].ToString();
					gameUrl = jObj["MKY888GameUrl"].ToString();
					appId = jObj["MKY888AppId"].ToString();
					secretKey = jObj["MKY888SecretKey"].ToString();
					timeOutSec = Convert.ToInt32(jObj["MKY888TimeOutSec"].ToString());

					api = new MKY888.MKY888(apiUrl, appId, secretKey, timeOutSec);
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
					var apiCheck = api.GetCredit(loginID);
					if (apiCheck)
					{
						var apiCall = api.CreateUser(loginID);
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
				var apiCall = api.Deposit(gameMap.GamePlayerID, Amount, TrackerId);
				InsertLog(gameID, "MKY888Deposit", "request", api.Request, "response", api.Response);

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
				var apiCall = api.Withdraw(gameMap.GamePlayerID, Amount, TrackerId);
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
					var apiCall = api.GetCredit(gameMap.GamePlayerID);
					InsertLog(gameID, "GetBalance", "request", api.Request, "response", api.Response);

					targetApiUrl = api.Request;
					request = api.Request;
					response = api.Response;

					if (apiCall)
					{
						res.PlayerID = playerID;
						res.Balance = api.GetCreditResponse.Credit;
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
				var apiCall = api.GetCredit(gameMap.GamePlayerID);
				InsertLog(gameID, "GetBalance", "request", api.Request, "response", api.Response);

				targetApiUrl = api.Request;
				request = api.Request;
				response = api.Response;

				if (apiCall)
				{
					return Convert.ToDecimal(api.GetCreditResponse.Credit);
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
					InsertLog(gameID, "GetGameUrlMKY888", "langId", langId, "IsMobile", IsMobile, "gameID", gameID);

					var sessionId = new Guid().ToString();
					langId = GetGameLanguage(gameID, langId);
					//var apiCall = api.PlayGame(gameMap.GamePlayerID);

					//targetApiUrl = api.Request;
					//request = api.Request;
					//response = api.Response;

					string url = string.Format("{0}?username={1}&langid={2}", lobbyUrl, gameMap.GamePlayerID, langId);

					InsertLog(gameID, "GetGameUrlMKY888", "langId", langId, "IsMobile", IsMobile, "gameID", gameID, "url", url);

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
				_common.LogError("GetGameUrlMKY888" + " " + DateTime.Now.ToString() + " " + string.Format("{0}", playerID) + " " + ex.Message.ToString(), gameID);

				var err = new Error
				{
					ErrorCode = "999",
					ErrorDesc = ex.Message.ToString()
				};

				InsertLog(gameID, "GetGameUrlMKY888", "Error", JsonConvert.SerializeObject(err));

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

		public MKY888.Models.RetrieveTransactionByMinuteResponse RetrieveTransactionByMinute(DateTime dateFrom, DateTime dateTo, string nextId)
		{
			var apiCall = api.RetrieveTransactionByMinute(dateFrom, dateTo, nextId);
			InsertLog(gameID, "MKY888Job", "request", api.Request, "response", api.Response);

			if (apiCall)
				return api.RetrieveTransactionByMinuteResponse;

			return null;
		}

		public MKY888.Models.ListGamesResponse GetGamesList()
		{
			var apiCall = api.ListGames();

			targetApiUrl = api.Request;
			request = api.Request;
			response = api.Response;

			if (apiCall)
			{
				return api.ListGamesResponse;
			}
			return null;
		}

		//lobby
		public vm.GetBalanceVM GetBalanceLobby(string username)
		{
			var res = new vm.GetBalanceVM();

			try
			{
				if (!string.IsNullOrEmpty(username))
				{
					var apiCall = api.GetCredit(username);
					InsertLog(gameID, "GetBalance", "request", api.Request, "response", api.Response);

					targetApiUrl = api.Request;
					request = api.Request;
					response = api.Response;

					if (apiCall)
					{
						res.PlayerID = playerID;
						res.Balance = api.GetCreditResponse.Credit;
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

		public vm.GetGameUrlVM GetSlotGameUrl(string username, string langId, string gameCode)
		{
			var res = new vm.GetGameUrlVM();

			try
			{
				if (!string.IsNullOrEmpty(username))
				{
					var sessionId = new Guid().ToString();
					//langId = GetGameLanguage(gameID, langId);
					string lobby = HttpUtility.UrlEncode(string.Format("{0}?username={1}&langid={2}", lobbyUrl, username, langId));
					var apiCall = api.PlayGame(username);
					InsertLog(gameID, "GetSlotGameUrl", "request", api.Request, "response", api.Response);

					targetApiUrl = api.Request;
					request = api.Request;
					response = api.Response;

					if (apiCall)
					{
						res.PlayerID = playerID;
						res.Url = string.Format(gameUrl, api.PlayGameResponse.Token, gameCode, lobby, true, langId);
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
				_common.LogError("GetSlotGameUrl" + " " + DateTime.Now.ToString() + " " + string.Format("{0}", playerID) + " " + ex.Message.ToString(), gameID);

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
	}
}