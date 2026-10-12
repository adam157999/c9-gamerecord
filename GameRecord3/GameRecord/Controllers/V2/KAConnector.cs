using GameRecord.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using vm = GameRecord.Controllers.GameAPIController;

namespace GameRecord.Controllers.V2
{
	public class KAConnector : BaseConnector
	{
		// GET: HabaneroConnector
		private int gameUniqueID = 0;
		private string gameID = "KA";
		private string apiUrl = "";
		private string gameUrl = "";
		private string secretKey = "";
		private string accessKey = "";
		private string partnerName = "";
		private string operatorName = "";
		private int timeOutSec = 30;
		private int currencyUnit = 100;
		private GameMapping gameMap;
		private KA.DLL.KA api = null;

		public KAConnector(string PlayerID = "", string CurrencyID = "", string IPAddr = "", GameEnvironment Environment = GameEnvironment.PROD) : base(PlayerID, CurrencyID, IPAddr, Environment)
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
					apiUrl = jObj["KAApiUrl"].ToString();
					gameUrl = jObj["KAGameUrl"].ToString();
					accessKey = jObj["KAAccessKey"].ToString();
					secretKey = jObj["KASecretKey"].ToString();
					partnerName = jObj["KAPartnerName"].ToString();
					operatorName = jObj["KAOperatorName"].ToString();
					timeOutSec = Convert.ToInt32(jObj["KATimeOutSec"].ToString());
					api = new KA.DLL.KA(apiUrl, accessKey, secretKey, partnerName, timeOutSec);
				}
			}
		}

		public override vm.CheckAndCreateAccountVM CreatePlayer(string loginID, string loginPass, string fullName, string mcUID)
		{
			// No create player in KA
			var res = new vm.CheckAndCreateAccountVM();
			InsertGameMapping(gameID, loginID, loginPass, mcUID);
			gameMap = LoadGameMap(gameID, playerID, currencyID);
			res.PlayerID = playerID;
			res.GamePlayerID = loginID;
			res.Game = gameID;
			res.Message = "Retrieve Success";
			res.Code = 0;

			return res;
		}

		public override bool GameDeposit(decimal Amount, string RequestID, ref Error Error)
		{
			if (gameMap != null)
			{
				KA.Model.BaseDetachedWalletRequest requestBody = new KA.Model.BaseDetachedWalletRequest();
				requestBody.currency = currencyID;
				requestBody.operatorName = operatorName;
				requestBody.partnerName = partnerName;
				requestBody.username = gameMap.GamePlayerID;
				var apiCall = api.Deposit(Convert.ToInt64(Amount * currencyUnit), RequestID, requestBody);
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
				KA.Model.BaseDetachedWalletRequest requestBody = new KA.Model.BaseDetachedWalletRequest();
				requestBody.currency = currencyID;
				requestBody.operatorName = operatorName;
				requestBody.partnerName = partnerName;
				requestBody.username = gameMap.GamePlayerID;
				var apiCall = api.Withdraw(Convert.ToInt64(Amount * currencyUnit), RequestID, requestBody);
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
					//bool online = true;
					KA.Model.BaseDetachedWalletRequest requestBody = new KA.Model.BaseDetachedWalletRequest();
					requestBody.currency = currencyID;
					requestBody.operatorName = operatorName;
					requestBody.partnerName = partnerName;
					requestBody.username = gameMap.GamePlayerID;
					var apiCall = api.Balance(requestBody);

					InsertLog(gameID, "GetBalance", "request", api.Request, "response", api.Response, "url", api.TargetApiUrl);

					if (api.BalanceResponse.statusCode == 4)
					{
						apiCall = CreateNewWallet(requestBody);
					}
					InsertLog(gameID, "GetBalance", "request", api.Request, "response", api.Response);

					targetApiUrl = api.Request;
					request = api.Request;
					response = api.Response;

					if (apiCall)
					{
						res.PlayerID = playerID;
						res.Balance = (decimal)api.DeatachedWalletBalanceResponse.balance / (decimal)currencyUnit;
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
				//bool online = true;
				KA.Model.BaseDetachedWalletRequest requestBody = new KA.Model.BaseDetachedWalletRequest();
				requestBody.currency = currencyID;
				requestBody.operatorName = operatorName;
				requestBody.partnerName = partnerName;
				requestBody.username = gameMap.GamePlayerID;
				var apiCall = api.Balance(requestBody);
				InsertLog(gameID, "GetBalance", "request", api.Request, "response", api.Response, "url", api.TargetApiUrl);
				if (api.DeatachedWalletBalanceResponse.statusCode == 4)
				{
					apiCall = CreateNewWallet(requestBody);
				}
				InsertLog(gameID, "GetBalance", "request", api.Request, "response", api.Response);

				targetApiUrl = api.Request;
				request = api.Request;
				response = api.Response;

				if (apiCall)
				{
					return Convert.ToDecimal(api.DeatachedWalletBalanceResponse.balance) / (decimal)currencyUnit;
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

		public override vm.GetGameUrlVM GetGameUrl(string langId, bool app = false)
		{
			var res = new vm.GetGameUrlVM();

			try
			{
				if (gameMap != null)
				{
					var sessionId = new Guid().ToString();
					langId = GetGameLanguage(gameID, langId);
					var apiCall = api.CreateToken(currencyID, gameMap.GamePlayerID);
					InsertLog(gameID, "GetGameUrl", "request", api.Request, "response", api.Response);
					targetApiUrl = api.Request;
					request = api.Request;
					response = api.Response;

					res.PlayerID = playerID;

					if (apiCall)
					{
						res.PlayerID = playerID;
						res.Url = gameUrl + $"?tp=rectangular&loc={langId}&nw=0&p={partnerName}&ak={accessKey}&u={gameMap.GamePlayerID}" +
								$"&cr={currencyID}&t={api.CreateTokenResponse.launchToken}";
						//res.Url = gameUrl + $"?g={gameName}&p={partnerName}&u={playerID}&t={api.CreateTokenResponse.launchToken}" +
						//    $"&ak={accessKey}&loc={langId}&cr={currencyID}" +
						//    $"&o={operatorName}&tl=KAGA88";
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

		public KA.Model.GameReportResponse RetrieveTransactionByTime(DateTime dateFrom, DateTime dateTo, int timeOffset, long offset, long numberOfRecord)
		{
			api = new KA.DLL.KA(apiUrl, accessKey, secretKey, partnerName, timeOutSec);

			var apiCall = api.GameReport(currencyID, dateFrom.ToString("yyyy-MM-dd HH:mm:ss"), dateTo.ToString("yyyy-MM-dd HH:mm:ss"), timeOffset, offset, numberOfRecord);

			//InsertLog(gameID, "KAGameRecord", "request", api.Request, "response", api.Response);

			targetApiUrl = api.Request;
			request = api.Request;
			response = api.Response;

			if (apiCall)
			{
				return api.GameReportResponse;
			}
			return null;
		}

		private bool CreateNewWallet(KA.Model.BaseDetachedWalletRequest requestBody)
		{
			string RequestID = Guid.NewGuid().ToString();
			var apiCallDeposite = api.Deposit(Convert.ToInt64(0 * currencyUnit), RequestID, requestBody);
			return apiCallDeposite;
		}
	}
}