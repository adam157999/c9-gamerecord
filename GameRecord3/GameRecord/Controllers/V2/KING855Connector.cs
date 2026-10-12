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
	public class KING855Connector : BaseConnector
	{
		// GET: DGConnector
		private string gameID = "KING855";
		private string apiUrl = "";
		private string gameUrl = "";
		private string apiKey = "";
		private string agentName = "";
		private string random = "";
		private string limitGroup = "";
		private int timeOutSec = 30;
		private GameMapping gameMap;
		private King855.King855 api = null;

		public KING855Connector(string PlayerID = "", string CurrencyID = "", string IPAddr = "", GameEnvironment Environment = GameEnvironment.PROD) : base(PlayerID, CurrencyID, IPAddr, Environment)
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
					apiUrl = jObj["KING855ApiUrl"].ToString();
					apiKey = jObj["KING855ApiKey"].ToString();
					agentName = jObj["KING855AgentName"].ToString();
					random = jObj["KING855Random"].ToString();
					limitGroup = jObj["KING855LimitGroup"].ToString();
					timeOutSec = Convert.ToInt32(jObj["KING855TimeOutSec"].ToString());

					api = new King855.King855(apiUrl, apiKey, agentName, random, limitGroup, timeOutSec);
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
					var apiCheck = api.Signup(loginID, loginPass, currencyID, 0);
					if (apiCheck)
					{
						//var apiCall = api.CreateMember(loginID);
						InsertLog(gameID, "CreateUser", "request", api.Request, "response", api.Response);
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

		public override bool GameDeposit(decimal Amount, string TransactionId, ref Error Error)
		{
			if (gameMap != null)
			{
				if (Amount >= 0)
				{
					var apiCall = api.Transfer(TransactionId, gameMap.GamePlayerID, Double.Parse(Amount.ToString("F")));
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

		public override bool GameWithdraw(decimal Amount, string TransactionId, ref Error Error)
		{
			if (gameMap != null)
			{
				Amount = -Amount;
				if (Amount <= 0)
				{
					var apiCall = api.Transfer(TransactionId, gameMap.GamePlayerID, Double.Parse(Amount.ToString("F")));
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
						ErrorDesc = "Withdraw Only Accept Negative Value"
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
					InsertLog(gameID, "GetBalance", "request", api.Request, "response", api.Response);

					targetApiUrl = api.Request;
					request = api.Request;
					response = api.Response;

					if (apiCall)
					{
						res.PlayerID = playerID;
						res.Balance = Convert.ToDecimal(api.GetBalanceResponse.member.balance);
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
				var apiCall = api.GetBalance(gameMap.GamePlayerID);
				InsertLog(gameID, "GetBalance", "request", api.Request, "response", api.Response);

				targetApiUrl = api.Request;
				request = api.Request;
				response = api.Response;

				if (apiCall)
				{
					return Convert.ToDecimal(api.GetBalanceResponse.member.balance);
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
					string domains = "0";
					if (IsMobile == true)
					{
						domains = "1";
					}
					var sessionId = new Guid().ToString();
					langId = GetGameLanguage(gameID, langId);
					var apiCall = api.Login(langId, domains, gameMap.GamePlayerID, gameMap.GamePassword);
					InsertLog(gameID, "GetGameUrl", "request", api.Request, "response", api.Response);

					targetApiUrl = api.Request;
					request = api.Request;
					response = api.Response;
					if (IsMobile == true)
					{
						gameUrl = api.LoginResponse.list[0] + api.LoginResponse.token + $"&lang={langId}";
					}
					else
					{
						gameUrl = api.LoginResponse.list[1] + api.LoginResponse.token + $"&lang={langId}";
					}

					if (apiCall)
					{
						res.PlayerID = playerID;
						res.Url = gameUrl;
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
				long defaultIntGamePlayerId;
				List<long> gamePlayerListId = new List<long>();
				if (long.TryParse(GamePlayerId, out defaultIntGamePlayerId))
				{
					gamePlayerListId.Add(defaultIntGamePlayerId);
					var apiCall = api.KickUser(gamePlayerListId);
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
						ErrorCode = "997",
						ErrorDesc = "Game Player ID must be numeric"
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

		public King855.Model.GetReportResponse RetrieveTransactionByMinute()
		{
			api = new King855.King855(apiUrl, apiKey, agentName, random, limitGroup, timeOutSec);
			var apiCall = api.GetReport();

			InsertLog(gameID, "KING855GameRecord", "request", api.Request, "response", api.Response);

			targetApiUrl = api.Request;
			request = api.Request;
			response = api.Response;

			if (apiCall)
			{
				return api.GetReportResponse;
			}
			return null;
		}

		public King855.Model.MarkResponse MarkRecord(List<long> list)
		{
			api = new King855.King855(apiUrl, apiKey, agentName, random, limitGroup, timeOutSec);
			var apiCall = api.Mark(list);

			//InsertLog(gameID, "KING855MarkRecordRecord", "request", api.Request, "response", api.Response);

			targetApiUrl = api.Request;
			request = api.Request;
			response = api.Response;

			if (apiCall)
			{
				return api.MarkResponse;
			}
			return null;
		}

        public King855.Model.UpdateLimitResponse UpdateLimit(string GamePlayerId)
        {
			api = new King855.King855(apiUrl, apiKey, agentName, random, limitGroup, timeOutSec);
			var apiCall = api.UpdateLimit(GamePlayerId);
			InsertLog(gameID, "UpdateLimit", "request", api.Request, "response", api.Response);

			targetApiUrl = api.TargetApiUrl;
			request = api.Request;
			response = api.Response;

			if (apiCall)
			{
				return api.UpdateLimitResponse;
			}
			else
			{
				return api.UpdateLimitResponse;
			}

        }
    }
}