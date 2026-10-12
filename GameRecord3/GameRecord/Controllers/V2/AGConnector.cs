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
	public class AGConnector : BaseConnector
	{
		private string gameID = "AG";
		private string serviceUrl = "";
		private string forwardGameUrl = "";
		private string cAgent = "";
		private string desKey = "";
		private string md5Key = "";
		private string password = "";
		private string ftpUrl = "";
		private string ftpFolder = "";
		private string ftpUserName = "";
		private string betLimit = "";
		private string ftpPassword = "";
		private int timeOutSec = 30;
		private GameMapping gameMap;
		private AsiaGaming.AG api = null;

		public AGConnector(string PlayerID = "", string CurrencyID = "", string IPAddr = "", GameEnvironment Environment = GameEnvironment.PROD) : base(PlayerID, CurrencyID, IPAddr, Environment)
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
					serviceUrl = jObj["AGServiceUrl"].ToString();
					forwardGameUrl = jObj["AGForwardGameUrl"].ToString();
					cAgent = jObj["AGCAgent"].ToString();
					desKey = jObj["AGDesKey"].ToString();
					md5Key = jObj["AGMd5Key"].ToString();
					password = jObj["AGPassword"].ToString();
					ftpUrl = jObj["AGFtpUrl"].ToString();
					ftpFolder = jObj["AGFtpFolder"].ToString();
					ftpUserName = jObj["AGFtpUserName"].ToString();
					ftpPassword = jObj["AGFtpPassword"].ToString();
					betLimit = jObj["AGBetRange"].ToString();
					timeOutSec = Convert.ToInt32(jObj["AGTimeOutSec"].ToString());

					api = new AsiaGaming.AG(serviceUrl, cAgent, desKey, md5Key, password, betLimit, timeOutSec);
				}
			}
		}

		#region Main Function 
		public override vm.CheckAndCreateAccountVM CreatePlayer(string loginID, string loginPass, string fullName, string mcUID)
		{
			var res = new vm.CheckAndCreateAccountVM();

			try
			{
				if (gameMap == null)
				{
					var apiCall = api.CheckOrCreateAccount(loginID, currencyID);
					InsertLog(gameID, "CreatePlayer", "request", api.request, "response", api.response);

					targetApiUrl = api.targetUrl;
					request = api.request;
					response = api.response;

					if (apiCall)
					{
						InsertGameMapping(gameID, loginID, loginPass, mcUID);
						gameMap = LoadGameMap(gameID, playerID, currencyID);

						res.PlayerID = playerID;
						res.GamePlayerID = loginID;
						res.Game = gameID;
						res.Message = api.CheckOrCreateAccountResponse.msg;
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
				_common.LogError("CreatePlayer" + " " + DateTime.Now.ToString() + " " + string.Format("{0}", playerID) + " " + ex.Message.ToString(), gameID);

				var err = new
				{
					ErrorCode = "999",
					ErrorDesc = ex.Message.ToString()
				};

				InsertLog(gameID, "CreatePlayer_Exception", "Error", JsonConvert.SerializeObject(err));

				res.PlayerID = playerID;
				res.GamePlayerID = loginID;
				res.Game = gameID;
				res.Message = ex.Message.ToString();
				res.Code = 99;
			}

			return res;
		}
		public override vm.GetBalanceVM GetBalance()
		{
			var res = new vm.GetBalanceVM();

			try
			{
				if (gameMap != null)
				{
					var apiCall = api.GetBalance(gameMap.GamePlayerID, currencyID);
					InsertLog(gameID, "GetBalance", "request", api.request, "response", api.response);

					targetApiUrl = api.targetUrl;
					request = api.request;
					response = api.response;

					if (apiCall)
					{
						res.PlayerID = playerID;
						res.Balance = Convert.ToDecimal(api.GetBalanceResponse.info);
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
				_common.LogError("GetProfile" + " " + DateTime.Now.ToString() + " " + string.Format("{0}", playerID) + " " + ex.Message.ToString(), gameID);

				var err = new Error
				{
					ErrorCode = "999",
					ErrorDesc = ex.Message.ToString()
				};

				InsertLog(gameID, "GetProfile_Exception", "Error", JsonConvert.SerializeObject(err));

				res.PlayerID = playerID;
				res.Balance = null;
				res.Game = gameID;
				res.Message = ex.Message.ToString();
				res.Code = 99;
			}

			return res;
		}
		public override vm.GetGameUrlVM GetGameUrl(string langId, bool IsMobile = false)
		{
			var res = new vm.GetGameUrlVM();

			try
			{
				if (gameMap != null)
				{
					langId = GetGameLanguage(gameID, langId);
					var apiCall = api.ForwardGame(forwardGameUrl, gameMap.GamePlayerID, currencyID, langId);
					//string param = string.Format("/ForwardGame?loginname={0}&currency={1}&lang={2}", gameMap.GamePlayerID, currencyID, langId);
					string url = api.ForwardGameResponse.url;
					InsertLog(gameID, "GetGameUrl", "request", url, "response", "");

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
				_common.LogError("Bet" + " " + DateTime.Now.ToString() + " " + string.Format("{0}", playerID) + " " + ex.Message.ToString(), gameID);

				var err = new Error
				{
					ErrorCode = "999",
					ErrorDesc = ex.Message.ToString()
				};

				InsertLog(gameID, "Bet_Exception", "Error", JsonConvert.SerializeObject(err));

				res.PlayerID = playerID;
				res.Url = null;
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
				var apiCall = api.GetBalance(gameMap.GamePlayerID, currencyID);
				InsertLog(gameID, "GetBalance", "request", api.request, "response", api.response);

				targetApiUrl = api.targetUrl;
				request = api.request;
				response = api.response;

				if (apiCall)
				{
					return Convert.ToDecimal(api.GetBalanceResponse.info);
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

		public override bool GameDeposit(decimal Amount, string TrackerId, ref Error Error)
		{
			if (gameMap != null)
			{
				var apiCall = api.Deposit(gameMap.GamePlayerID, Amount, currencyID);
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
		public override bool KickPlayer(string GamePlayerId, ref Error Error)
		{
			throw new NotImplementedException();
		}

		public override string RetryDeposit(decimal Amount, string TrackerId, ref Error Error)
		{
			throw new NotImplementedException();
		}

		public override bool GameWithdraw(decimal Amount, string TrackerId, ref Error Error)
		{
			if (gameMap != null)
			{
				var apiCall = api.Withdraw(gameMap.GamePlayerID, Amount, currencyID);
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

		public override string RetryWithdraw(decimal Amount, string TrackerId, ref Error Error)
		{
			throw new NotImplementedException();
		}

		public AsiaGaming.FtpListDirectoryResponse FtpListDirectory(string fileDate)
		{
			var apiCall = api.GetFtpDirectories(ftpUrl, ftpFolder, fileDate, ftpUserName, ftpPassword);
			InsertLog(gameID, "FtpListDirectory", "request", api.request, "response", api.response, "error", api.errorMessage);

			if (apiCall)
			{
				return api.FtpListDirectoryResponse;
			}
			return null;
		}

		public AsiaGaming.FtpDownloadResponse FtpDownload(string fileDate, string fileName)
		{
			var apiCall = api.GetFtpFiles(ftpUrl, ftpFolder, fileDate, fileName, ftpUserName, ftpPassword);
			InsertLog(gameID, "FtpDownload", "request", api.request, "response", api.response, "error", api.errorMessage);

			targetApiUrl = api.targetUrl;
			request = api.request;
			response = api.response;

			if (apiCall)
			{
				return api.FtpDownloadResponse;
			}
			return null;
		}
		#endregion

		#region Private Function

		#endregion
	}
}