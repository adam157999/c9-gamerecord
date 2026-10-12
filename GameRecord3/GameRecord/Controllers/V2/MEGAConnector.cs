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
using System.Data;
using vm = GameRecord.Controllers.GameAPIController;
using System.Data.SqlClient;

namespace GameRecord.Controllers
{
	public class MEGAConnector : BaseConnector
	{
		private string gameID = "MEGA";
		private string apiUrl = "";
		private string sn = "";
		private string agentLoginId = "";
		private string secretCode = "";
		private string jsonrpc = "";
		private int timeOutSec = 30;
		private GameMapping gameMap;
		private MEGA.MEGA api = null;

		public MEGAConnector(string PlayerID = "", string CurrencyID = "", string IPAddr = "", GameEnvironment Environment = GameEnvironment.PROD) : base(PlayerID, CurrencyID, IPAddr, Environment)
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
					apiUrl = jObj["MEGAApiUrl"].ToString();
					sn = jObj["MEGAsn"].ToString();
					agentLoginId = jObj["MEGAAgentLoginId"].ToString();
					secretCode = jObj["MEGASecretCode"].ToString();
					jsonrpc = jObj["MEGAjsonrpc"].ToString();
					timeOutSec = Convert.ToInt32(jObj["MEGATimeOutSec"].ToString());

					api = new MEGA.MEGA(apiUrl, sn, agentLoginId, secretCode, jsonrpc, timeOutSec);
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
					var apiCall = api.CreateUser(mcUID);
					InsertLog(gameID, "CreateUser", "request", api.request, "response", api.response);

					targetApiUrl = api.targetUrl;
					request = api.request;
					response = api.response;

					if (apiCall)
					{
						string passwords = _common.RandomString(6);
						InsertGameMapping(gameID, api.CreateMemberResponse.result.loginId, passwords, mcUID);
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
				long? bizId = 0;
				try
				{
					long s;
					long.TryParse(TrackerId, out s);
					bizId = s;
				}
				catch
				{
					bizId = null;
				}
				var apiCall = api.Deposit(gameMap.GamePlayerID, Amount, bizId);
				InsertLog(gameID, "Deposit", "url", api.request, "response", api.response, "request", api.parameter);

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
				long? bizId = 0;
				try
				{
					long s;
					long.TryParse(TrackerId, out s);
					bizId = s;
				}
				catch
				{
					bizId = null;
				}
				var apiCall = api.Withdraw(gameMap.GamePlayerID, Amount, bizId);
				InsertLog(gameID, "Withdraw", "url", api.request, "response", api.response, "request", api.parameter);

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
					var apiCall = api.GetBalance(gameMap.GamePlayerID);
					InsertLog(gameID, "GetBalance", "request", api.request, "response", api.response);

					targetApiUrl = api.targetUrl;
					request = api.request;
					response = api.response;

					if (apiCall)
					{
						res.PlayerID = playerID;
						res.Balance = api.GetBalanceResponse.result.Value;
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

				targetApiUrl = api.targetUrl;
				request = api.request;
				response = api.response;

				if (apiCall)
				{
					return api.GetBalanceResponse.result.Value;
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
					var apiCall = api.AppDownload();
					InsertLog(gameID, "GetGameUrl", "request", api.request, "response", api.response);

					targetApiUrl = api.request;
					request = api.parameter;
					response = api.response;

					if (apiCall)
					{
						string deeplink = null;
						string package = "com.mega888.Release";

						res.PlayerID = playerID;
						res.Url = api.AppDownloadResponse.result;
						res.Game = gameID;
						res.Message = "Success";
						res.Code = 0;
						res.ApkInfo = new vm.GameApkVM()
						{
							Android = new vm.GameApkInfoVM()
							{
								DownloadLink = Convert.ToString(api.AppDownloadResponse.result),
								Package = package,
								Activity = null,
								DeepLink = deeplink,
								GamePlayerID = gameMap.GamePlayerID,
								Passwords = gameMap.GamePassword
							},
							iOS = new vm.GameApkInfoVM()
							{
								DownloadLink = Convert.ToString(api.AppDownloadResponse.result),
								Package = null,
								Activity = null,
								DeepLink = "lobbymegarelease://lobbymegarelease",
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
			if (gameMap != null)
			{
				long? bizId;
				try
				{
					long s;
					long.TryParse(TrackerId, out s);
					bizId = s;
				}
				catch
				{
					bizId = null;
				}
				var apiCall = api.Deposit(gameMap.GamePlayerID, Amount, bizId);
				InsertLog(gameID, "Deposit", "request", api.request, "response", api.response);

				targetApiUrl = api.targetUrl;
				request = api.request;
				response = api.response;

				if (apiCall)
				{
					return api.response;
				}
				else
				{
					Error = new Error
					{
						ErrorCode = "-9999",
						ErrorDesc = api.errorMessage
					};
					return "";
				}
			}
			else
			{
				Error = new Error
				{
					ErrorCode = "-9999",
					ErrorDesc = "Retrieve Success"
				};
				return "";
			}
		}

		public override string RetryWithdraw(decimal Amount, string TrackerId, ref Error Error)
		{
			if (gameMap != null)
			{
				long? bizId;
				try
				{
					long s;
					long.TryParse(TrackerId, out s);
					bizId = s;
				}
				catch
				{
					bizId = null;
				}
				var apiCall = api.Withdraw(gameMap.GamePlayerID, Amount, bizId);
				InsertLog(gameID, "Withdraw", "request", api.request, "response", api.response);

				targetApiUrl = api.targetUrl;
				request = api.request;
				response = api.response;

				if (apiCall)
				{
					return api.response;
				}
				else
				{
					Error = new Error
					{
						ErrorCode = "-9999",
						ErrorDesc = api.errorMessage
					};
					return "";
				}
			}
			else
			{
				Error = new Error
				{
					ErrorCode = "-9999",
					ErrorDesc = "Retrieve Success"
				};
				return "";
			}
		}

		public MEGA.PlayerTotalReportResponse PlayerTotalReport(string type, DateTime startTime, DateTime endTime)
		{
			var apiCall = api.PlayerTotalReport(type, startTime, endTime);
			InsertLog(gameID, "PlayerTotalReport", "request", api.request, "response", api.response);
			targetApiUrl = api.targetUrl;
			request = api.request;
			response = api.response;

			if (apiCall)
			{
				return api.PlayerTotalReportResponse;
			}
			return null;
		}

		public MEGA.PlayerGameLogResponse PlayerGameLog(string userId, DateTime startTime, DateTime endTime)
		{
			var apiCall = api.PlayerGameLog(userId, startTime, endTime);

			targetApiUrl = api.targetUrl;
			request = api.request;
			response = api.response;

			if (apiCall)
			{
				return api.PlayerGameLogResponse;
			}
			return null;
		}

		public MEGA.GameRecordQueryResponse GameRecordQuery(string userId, DateTime startTime, DateTime endTime, int pageIndex, int pageSize)
		{
			var apiCall = api.GameRecordQuery(userId, startTime, endTime, pageIndex, pageSize);

			targetApiUrl = api.targetUrl;
			request = api.request;
			response = api.response;
			InsertLog(gameID, "GameRecordQuery", "request", api.request, "response", api.response, "userId", userId, "startTime", startTime, "endTime", endTime);

			if (apiCall)
			{
				return api.GameRecordQueryResponse;
			}
			return null;
		}

		public bool ValidateCallbackSn(string SN)
		{
			if (SN == sn)
				return true;

			return false;
		}

		public string GetDigest(string random, string sn, string loginId)
		{
			return _common.MD5Hashing(Convert.ToString(random + sn + loginId + secretCode));
		}

		public DataTable GetUserGameInfo(string gamePlayerID, string password)
		{
			try
			{
				using (SqlConnection con = new SqlConnection(BingoConn))
				{
					con.Open();
					SqlCommand cmd = new SqlCommand("sp_GameMapping_Get", con);
					cmd.CommandType = CommandType.StoredProcedure;
					cmd.Parameters.Add(new SqlParameter("@GamePlayerID", gamePlayerID));
					cmd.Parameters.Add(new SqlParameter("@Game", gameID));
					cmd.Parameters.Add(new SqlParameter("@GamePassword", password));
					DataTable table = new DataTable();
					SqlDataAdapter da = new SqlDataAdapter(cmd);
					da.Fill(table);

					return table;
				}
			}
			catch (Exception ex)
			{
				return new DataTable();
			}
		}
	}
}