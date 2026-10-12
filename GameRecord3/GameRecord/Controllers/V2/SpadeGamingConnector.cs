using GameRecord.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data;
using vm = GameRecord.Controllers.GameAPIController;

namespace GameRecord.Controllers
{
	public class SpadeGamingConnector : BaseConnector
	{
		// GET: SpadeGamingConnector
		private string gameID = "SPADEGAMING";
		private string apiUrl = "";
		private string gameUrl = "";
		private string merchantCode = "";
		private string currency = "";
		private int timeOutSec = 30;
		private GameMapping gameMap;
		private SpadeGaming.SpadeGaming api = null;
		Common _common = new Common();

		public SpadeGamingConnector(string PlayerID = "", string CurrencyID = "", string IPAddr = "", GameEnvironment Environment = GameEnvironment.PROD) : base(PlayerID, CurrencyID, IPAddr, Environment)
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
					apiUrl = jObj["SpadeGamingApiUrl"].ToString();
					gameUrl = jObj["SpadeGamingGameUrl"].ToString();
					merchantCode = jObj["SpadeGamingMerchantCode"].ToString();
					currency = jObj["SpadeGamingCurrency"].ToString();
					timeOutSec = Convert.ToInt32(jObj["SpadeGamingTimeOutSec"].ToString());
					api = new SpadeGaming.SpadeGaming(apiUrl, merchantCode, currency, timeOutSec);
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
					decimal amt = Convert.ToDecimal(0.01);
					var apiCheck = api.Deposit(loginID, amt);

					targetApiUrl = api.TargetApiUrl;
					request = api.Request;
					response = api.Response;
					string error = api.ErrorMessage;


					if (apiCheck)
					{
						//var apiCall = api.CreateMember(loginID);
						InsertGameMapping(gameID, loginID, loginPass, playerID);
						gameMap = LoadGameMap(gameID, playerID, currencyID);
						var apiCheck1 = api.Withdraw(loginID, amt);

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
				var apiCall = api.Deposit(gameMap.GamePlayerID, Amount);
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

		public override bool GameWithdraw(decimal Amount, string RequestID, ref Error Error)
		{
			if (gameMap != null)
			{
				var apiCall = api.Withdraw(gameMap.GamePlayerID, Amount);
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
					var apiCall = api.GetAcctInfo(gameMap.GamePlayerID);
					InsertLog(gameID, "GetBalance", "request", api.Request, "response", api.Response);

					targetApiUrl = api.Request;
					request = api.Request;
					response = api.Response;

					if (apiCall)
					{
						if (api.GetAcctInfoResponse.list.Count > 0)
						{
							res.PlayerID = playerID;
							res.Balance = api.GetAcctInfoResponse.list[0].balance;
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
				var apiCall = api.GetAcctInfo(gameMap.GamePlayerID);
				InsertLog(gameID, "GetBalance", "request", api.Request, "response", api.Response);

				targetApiUrl = api.Request;
				request = api.Request;
				response = api.Response;

				if (apiCall)
				{
					if (api.GetAcctInfoResponse.list.Count > 0)
					{
						return Convert.ToDecimal(api.GetAcctInfoResponse.list[0].balance);
					}
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

		public override vm.GetGameUrlVM GetGameUrl(string langId, bool IsMobile = false)
		{
			var res = new vm.GetGameUrlVM();

			try
			{
				if (gameMap != null)
				{
					langId = GetGameLanguage(gameID, langId);
					//string token = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
					string token = GetToken().ToLower();

					InsertSpadeGamingGamingToken(gameMap.PlayerID, gameMap.GamePlayerID, token);
					InsertLog(gameID, "GetGameUrl", "request", api.Request, "response", api.Response);

					targetApiUrl = api.Request;
					request = api.Request;
					response = api.Response;

					res.PlayerID = playerID;
					res.Url = string.Format(gameUrl, merchantCode, gameMap.GamePlayerID, langId, HttpUtility.UrlEncode(token));
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
				var apiCall = api.KickAcct(gameMap.GamePlayerID);
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

		public SpadeGaming.Model.GetBetHistoryResponse GetBetHistory(string dateFrom, string dateTo, int page)
		{
			api = new SpadeGaming.SpadeGaming(apiUrl, merchantCode, currency, timeOutSec);

			var apiCall = api.GetBetHistory(Convert.ToDateTime(dateFrom), Convert.ToDateTime(dateTo), page);

			targetApiUrl = api.Request;
			request = api.Request;
			response = api.Response;

			InsertLog(gameID, "GetBetHistory", "request", api.Request, "response", api.Response);

			if (apiCall)
			{
				return api.GetBetHistoryResponse;
			}
			return null;
		}

		public void InsertSpadeGamingGamingToken(string playerID, string gamePlayerID, string token)
		{
			try
			{
				using (SqlConnection con = new SqlConnection(BingoConn))
				{
					con.Open();
					SqlCommand cmd = new SqlCommand("sp_SpadeGamingGameToken_Insert", con);
					cmd.CommandType = CommandType.StoredProcedure;
					cmd.Parameters.Add(new SqlParameter("@PlayerID", playerID));
					cmd.Parameters.Add(new SqlParameter("@GamePlayerID", gamePlayerID));
					cmd.Parameters.Add(new SqlParameter("@Token", token));

					cmd.ExecuteNonQuery();
				}
			}
			catch (Exception ex)
			{
				throw;
			}
		}

		public bool ValidateCallbackMerchantCode(string code)
		{
			if (merchantCode == code)
				return true;

			return false;
		}

		public string GetCurrency()
		{
			return currency;
		}

		public string GetToken()
		{
			Random random = new Random();
			string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
			return new string(Enumerable.Repeat(chars, 20)
					.Select(s => s[random.Next(s.Length)]).ToArray());
		}
	}
}