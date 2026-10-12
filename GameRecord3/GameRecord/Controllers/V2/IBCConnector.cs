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
	public class IBCConnector : BaseConnector
	{
		private string gameID = "IBC";
		private string apiUrl = "";
		private string vendorId = "";
		private string operatorId = "";
		private string prefix = "";
		private string suffix = "";
		private decimal minTransfer = 1;
		private decimal maxTransfer = 100000;
		private int accountMaxLength = 30;
		private int timeOutSec = 30;
		private GameMapping gameMap;
		private IBC.IBC api = null;

		public IBCConnector(string PlayerID = "", string CurrencyID = "", string IPAddr = "", GameEnvironment Environment = GameEnvironment.PROD) : base(PlayerID, CurrencyID, IPAddr, Environment)
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
					apiUrl = jObj["IBCApiUrl"].ToString();
					vendorId = jObj["IBCVendorId"].ToString();
					operatorId = jObj["IBCOperatorId"].ToString();
					prefix = jObj["IBCPrefix"].ToString();
					suffix = jObj["IBCSuffix"].ToString();
					minTransfer = Convert.ToDecimal(jObj["IBCMinTransfer"].ToString());
					maxTransfer = Convert.ToDecimal(jObj["IBCMaxTransfer"].ToString());
					timeOutSec = Convert.ToInt32(jObj["IBCTimeOutSec"].ToString());

					api = new IBC.IBC(apiUrl, vendorId, operatorId, timeOutSec);
				}
			}
		}

		public override vm.CheckAndCreateAccountVM CreatePlayer(string loginID, string loginPass, string fullName, string mcUID)
		{
			var res = new vm.CheckAndCreateAccountVM();

			try
			{

				if (loginID.Length > accountMaxLength)
				{
					res.PlayerID = playerID;
					res.GamePlayerID = mcUID;
					res.Game = gameID;
					res.Message = "Username length cannot more than " + accountMaxLength.ToString();
					res.Code = 99;
				}

				if (res.Code == 0)
				{
					if (gameMap == null)
					{
						loginID = prefix + loginID.Trim() + suffix;
						var apiCall = api.CreateMember(loginID, minTransfer, maxTransfer);
						InsertLog(gameID, "CreateMember", "request", api.request, "response", api.response);

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
			}
			catch (Exception ex)
			{
				_common.LogError("CreateMember" + " " + DateTime.Now.ToString() + " " + string.Format("{0}", playerID) + " " + ex.Message.ToString(), gameID);

				var err = new
				{
					ErrorCode = "999",
					ErrorDesc = ex.Message.ToString()
				};

				InsertLog(gameID, "CreateMember_Exception", "Error", JsonConvert.SerializeObject(err));

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
				var apiCall = api.Withdraw(gameMap.GamePlayerID, Amount, TrackerId);
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
						res.Balance = api.GetBalanceResponse.Data[0].error_code == 6 ? 0 : api.GetBalanceResponse.Data.Where(x => x.vendor_member_id == gameMap.GamePlayerID).Select(x => x.balance).FirstOrDefault();
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
					if (api.GetBalanceResponse.Data[0].error_code == 6)
						return 0;
					else
						return api.GetBalanceResponse.Data.Where(x => x.vendor_member_id == gameMap.GamePlayerID).Select(x => x.balance).FirstOrDefault();
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
					var sessionId = new Guid().ToString();
					langId = GetGameLanguage(gameID, langId);
					var apiCall = api.LaunchGame(gameMap.GamePlayerID, langId);
					InsertLog(gameID, "LaunchGame", "request", api.request, "response", api.response);

					targetApiUrl = api.targetUrl;
					request = api.request;
					response = api.response;

					if (apiCall)
					{
						res.PlayerID = playerID;
						res.Url = string.Format("{0}&lang={1}", api.LaunchGameResponse.Data, langId);
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
				_common.LogError("LaunchGame" + " " + DateTime.Now.ToString() + " " + string.Format("{0}", playerID) + " " + ex.Message.ToString(), gameID);

				var err = new Error
				{
					ErrorCode = "999",
					ErrorDesc = ex.Message.ToString()
				};

				InsertLog(gameID, "LaunchGame_Exception", "Error", JsonConvert.SerializeObject(err));

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
				var apiCall = api.Deposit(gameMap.GamePlayerID, Amount, TrackerId);
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
				var apiCall = api.Withdraw(gameMap.GamePlayerID, Amount, TrackerId);
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

		public IBC.GetBetDetailResponse GetBetDetail(string versionKey)
		{
			api = new IBC.IBC(apiUrl, vendorId, operatorId, timeOutSec);

			var apiCall = api.GetBetDetail(versionKey);
			InsertLog(gameID, "GetBetDetail", "targetUrl", api.targetUrl, "request", api.request, "response", api.response);

			if (apiCall)
				return api.GetBetDetailResponse;

			return null;
		}

		public IBC.GetVersionkeyByDateResponse GetVersionkeyByDate(string winloseDate)
		{
			api = new IBC.IBC(apiUrl, vendorId, operatorId, timeOutSec);

			var apiCall = api.GetVersionkeyByDate(winloseDate);

			InsertLog(gameID, "GetVersionkeyByDate", "request", api.request, "response", api.response);

			targetApiUrl = api.targetUrl;
			request = api.request;
			response = api.response;

			if (apiCall)
			{
				return api.GetVersionkeyByDateResponse;
			}
			return null;
		}
	}
}