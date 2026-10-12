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
using GameRecord.Gamess.EvoGaming;

namespace GameRecord.Controllers
{
	public class EVOGMConnector : BaseConnector
	{
		private string gameID = "EVOLUTIONGAMING";
		private string hostName = "";
		private string reportHost = "";
		private string merchantId = "";
		private string casinoKey = "";
		private string apiToken = "";
		private string country = "";
		private string brandId = "";
		private string skinId = "1";
		private string gameCategory = "baccarat";
		private string gameInterface = "view1";
		private string vipGroupAction = "assign";
		private string vipGroupId = "";
		private JObject urls;
		private int timeOutSec = 30;
		private GameMapping gameMap;
		//private EvoGaming.EVOGM api = null;
		private EVOGM api = null;

		public EVOGMConnector(string PlayerID = "", string CurrencyID = "", string IPAddr = "", GameEnvironment Environment = GameEnvironment.PROD) : base(PlayerID, CurrencyID, IPAddr, Environment)
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
					hostName = jObj["EVOGMHostName"].ToString();
					reportHost = jObj["EVOGMReportHost"].ToString();
					merchantId = jObj["EVOGMMerchantId"].ToString();
					casinoKey = jObj["EVOGMCasinoKey"].ToString();
					apiToken = jObj["EVOGMApiToken"].ToString();
					country = jObj["EVOGMCountry"].ToString();
					brandId = jObj["EVOGMBrandId"].ToString();
					skinId = jObj["EVOGMSkinId"].ToString();
					gameCategory = jObj["EVOGMGameCategory"].ToString();
					gameInterface = jObj["EVOGMGameInterface"].ToString();
					vipGroupAction = jObj["EVOGMVIPAction"].ToString();
					vipGroupId = jObj["EVOGMVIPId"].ToString();
					urls = JObject.Parse(jObj["EVOGMUrls"].ToString());
					timeOutSec = Convert.ToInt32(jObj["EVOGMTimeOutSec"].ToString());

					//api = new EvoGaming.EVOGM(hostName, merchantId, casinoKey, apiToken, timeOutSec);
					api = new EVOGM(hostName, merchantId, casinoKey, apiToken, timeOutSec);
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
					if (!string.IsNullOrEmpty(ipAddr)) ipAddr = ipAddr.Split(':')[0];

					var apiCall = api.UserAuth(loginID, loginID, loginID, country, "en", currencyID, sessionId, ipAddr, brandId, skinId, gameCategory, gameInterface, urls);
					InsertLog(gameID, "UserAuth", "request", api.request, "response", api.response);

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
				_common.LogError("UserAuth" + " " + DateTime.Now.ToString() + " " + string.Format("{0}", playerID) + " " + ex.Message.ToString(), gameID);

				var err = new
				{
					ErrorCode = "999",
					ErrorDesc = ex.Message.ToString()
				};

				InsertLog(gameID, "UserAuth_Exception", "Error", JsonConvert.SerializeObject(err));

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
				InsertLog(gameID, "Deposit", "request", api.request, "response", api.response);

				targetApiUrl = api.targetApiUrl;
				request = api.request;
				response = api.response;

				if (apiCall)
				{
					Error = new Error
					{
						ErrorCode = "0",
						ErrorDesc = ""
					};
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
				var apiCall = api.Withdraw(gameMap.GamePlayerID, Amount, TrackerId);
				InsertLog(gameID, "Withdraw", "request", api.request, "response", api.response);

				targetApiUrl = api.targetApiUrl;
				request = api.request;
				response = api.response;

				if (apiCall)
				{
					Error = new Error
					{
						ErrorCode = "0",
						ErrorDesc = ""
					};
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
						res.Balance = api.GetBalanceSuccessResponse.tbalance;
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
				InsertLog(gameID, "GetGameBalance", "request", api.request, "response", api.response);


				targetApiUrl = api.targetApiUrl;
				request = api.request;
				response = api.response;

				if (apiCall)
				{
					return Convert.ToDecimal(api.GetBalanceSuccessResponse.tbalance);
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
				//_common.LogError($"GetGameUrlEric:FAILED" + string.Format("{0}|{1}|{2}|{3}|{4}|{5}|{6}|{7}|{8}|{9}|{10}|{11}", gameMap.GamePlayerID, playerID, playerID, country, langId, currencyID, "", ipAddr, brandId, skinId, langId, ipAddr), FilePrefix);

				if (gameMap != null)
				{
					var sessionId = new Guid().ToString();
					langId = GetGameLanguage(gameID, langId);
					if (!string.IsNullOrEmpty(ipAddr)) ipAddr = ipAddr.Split(':')[0];


					var apiCall = api.UserAuth(gameMap.GamePlayerID, playerID, playerID, country, langId, currencyID, sessionId, ipAddr, brandId, skinId, gameCategory, gameInterface, urls, IsMobile, true, vipGroupId, vipGroupAction);
					InsertLog(gameID, "UserAuth", "request", api.request, "response", api.response);

					targetApiUrl = api.targetApiUrl;
					request = api.request;
					response = api.response;

					if (apiCall)
					{
						res.PlayerID = playerID;
						res.Url = api.UserAuthSuccessResponse.entry;
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
				_common.LogError("UserAuth" + " " + DateTime.Now.ToString() + " " + string.Format("{0}", playerID) + " " + ex.Message.ToString(), gameID);

				var err = new Error
				{
					ErrorCode = "999",
					ErrorDesc = ex.Message.ToString()
				};

				InsertLog(gameID, "UserAuth_Exception", "Error", JsonConvert.SerializeObject(err));

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

		public GameRecord.Gamess.EvoGaming.CasinoStreamGamesDetail.CasinoStreamGamesDetailResponse GetBetHistory(DateTime dateFrom, DateTime dateTo)
		{
			api = new EVOGM(reportHost, merchantId, casinoKey, apiToken, timeOutSec);

			var apiCall = api.CasinoStreamGamesDetail(dateFrom.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"), dateTo.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"));

			targetApiUrl = api.targetApiUrl;
			request = api.request;
			response = api.response;

			if (apiCall)
			{
				return api.CasinoStreamGamesDetailResponse;
			}
			return null;
		}
	}
}