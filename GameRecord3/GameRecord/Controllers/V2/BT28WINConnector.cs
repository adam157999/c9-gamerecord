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
	public class BT28WINConnector : BaseConnector
	{
		private string gameID = "BT28WIN";
		private string apiUrl = "";
		private string betUrl_desktop = "";
		private string betUrl_mobile = "";
		private string apiUser = "";
		private string apiPass = "";
		private string user = "";
		private string pass = "";
		private int timeOutSec = 30;
		private GameMapping gameMap;
		private BT28WIN.BT28WIN api = null;

		public BT28WINConnector(string PlayerID = "", string CurrencyID = "", string IPAddr = "", GameEnvironment Environment = GameEnvironment.PROD) : base(PlayerID, CurrencyID, IPAddr, Environment)
		{
			//gameMap = LoadGameMap(gameID, playerID, currencyID);

			using (BingoConn db = new BingoConn())
			{
				gameMap = db.GameMappings.Where(x => x.Game.ToUpper().Trim() == gameID.ToUpper().Trim() &&
																								x.PlayerID == playerID &&
																								x.Currency == currencyID &&
																								x.IsActive == true).FirstOrDefault();

				var apiConfig = CatchData.GetApiKeys(gameID.ToUpper().Trim(), Environment.ToString(), currencyID.ToUpper().Trim());
				if (string.IsNullOrEmpty(CurrencyID)) apiConfig = CatchData.GetApiKeys(gameID.ToUpper().Trim(), Environment.ToString());


				if (apiConfig != null)
				{
					JObject jObj = JObject.Parse(apiConfig.ApiConfig);
					apiUrl = jObj["BT28WINApiUrl"].ToString();
					betUrl_desktop = jObj["BT28WINBetUrl_Desktop"].ToString();
					betUrl_mobile = jObj["BT28WINBetUrl_Mobile"].ToString();
					apiUser = jObj["BT28WINApiUser"].ToString();
					apiPass = jObj["BT28WINApiPass"].ToString();
					user = jObj["BT28WINUser"].ToString();
					pass = jObj["BT28WINPass"].ToString();
					timeOutSec = Convert.ToInt32(jObj["BT28WINTimeOutSec"].ToString());

					api = new BT28WIN.BT28WIN(apiUrl, apiUser, apiPass, user, pass, timeOutSec);
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
					if (IsBlocked_Process())
					{
						var err = new
						{
							ErrorCode = "999",
							ErrorDesc = "4D close from 7pm to 9pm."
						};

						InsertLog(gameID, "CreatePlayer_Blocked", "Blocked", JsonConvert.SerializeObject(err));
					}

					if (IsBlocked_Peak())
					{
						var err = new
						{
							ErrorCode = "997",
							ErrorDesc = "Cannot Create Player From 5pm to 7pm."
						};

						InsertLog(gameID, "CreatePlayer_Blocked", "Blocked", JsonConvert.SerializeObject(err));
					}

					//if (IsBlockedPlayer(gameID, playerID))
					//{
					//	var err = new
					//	{
					//		ErrorCode = "998",
					//		ErrorDesc = "This player is under agent 4d blocked agent, unable to create."
					//	};

					//	InsertLog(gameID, "CreatePlayer_Blocked", "Blocked", JsonConvert.SerializeObject(err));
					//}

					var apiCheck = api.GetProfile(loginID);
					InsertLog(gameID, "CreatePlayer_Check", "request", api.request, "response", api.response);
					if (apiCheck)
					{
						res.PlayerID = playerID;
						res.GamePlayerID = loginID;
						res.Game = gameID;
						res.Message = "Account already exists";
						res.Code = 2;
					}
					else
					{
						var apiCall = api.CreatePlayer(loginID, loginPass, fullName);
						InsertLog(gameID, "CreatePlayer", "request", api.request, "response", api.response);

						targetApiUrl = api.targetUrl;
						request = api.request;
						response = api.response;

						if (apiCall)
						{
							InsertGameMapping(gameID, loginID, loginPass, playerID);
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
							res.Message = (api.CreatePlayerResponse != null ? api.CreatePlayerResponse.desc : api.errorMessage);
							res.Code = 2;
						}
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
					if (IsBlocked_Process())
					{
						var err = new Error()
						{
							ErrorCode = "999",
							ErrorDesc = "4D close from 7pm to 9pm."
						};

						InsertLog(gameID, "GetProfile", "Error", JsonConvert.SerializeObject(err));
					}

					var apiCall = api.GetProfile(gameMap.GamePlayerID);
					InsertLog(gameID, "GetProfile", "request", api.request, "response", api.response);

					targetApiUrl = api.targetUrl;
					request = api.request;
					response = api.response;

					if (apiCall)
					{
						res.PlayerID = playerID;
						res.Balance = Convert.ToDecimal(api.GetProfileResponse.balance);
						res.Game = gameID;
						res.Message = "Successfully get balance.";
						res.Code = 0;
					}
					else
					{
						res.PlayerID = playerID;
						res.Balance = null;
						res.Game = gameID;
						res.Message = (api.GetProfileResponse != null ? api.GetProfileResponse.desc : api.errorMessage);
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
					//langId = GetGameLanguage(gameID, langId);
					var apiCall = api.BetLogin(gameMap.GamePlayerID, gameMap.GamePassword, true);
					InsertLog(gameID, "GetGameUrl", "request", api.request, "response", api.response);

					targetApiUrl = api.targetUrl;
					request = api.request;
					response = api.response;

					if (apiCall)
					{
						res.PlayerID = playerID;
						if (IsMobile)
						{
							res.Url = string.Format(betUrl_mobile, gameMap.GamePlayerID, api.BetLoginResponse.tokenCode, api.BetLoginResponse.sessionID);
						}
						else
						{
							res.Url = string.Format(betUrl_desktop, gameMap.GamePlayerID, api.BetLoginResponse.tokenCode, api.BetLoginResponse.sessionID);
						}

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
				var apiCall = api.GetProfile(gameMap.GamePlayerID);
				InsertLog(gameID, "GetBalance", "request", api.request, "response", api.response);

				targetApiUrl = api.targetUrl;
				request = api.request;
				response = api.response;

				if (apiCall)
				{
					return Convert.ToDecimal(api.GetProfileResponse.balance);
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
				var apiCall = api.Deposit(gameMap.GamePlayerID, Amount);
				InsertLog(gameID, "Deposit", "request", api.request, "response", api.response);

				targetApiUrl = api.targetUrl;
				request = api.request;
				response = api.response;

				if (apiCall)
				{
					Error = new Error
					{
						ErrorCode = "0",
						ErrorDesc = (api.DepositResponse != null ? api.DepositResponse.desc : api.errorMessage)
					};
					return true;
				}
				else
				{
					Error = new Error
					{
						ErrorCode = "-9999",
						ErrorDesc = (api.DepositResponse != null ? api.DepositResponse.desc : api.errorMessage)
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
				var apiCall = api.Withdraw(gameMap.GamePlayerID, Amount);
				InsertLog(gameID, "Withdraw", "request", api.request, "response", api.response);

				targetApiUrl = api.targetUrl;
				request = api.request;
				response = api.response;

				if (apiCall)
				{
					Error = new Error
					{
						ErrorCode = "0",
						ErrorDesc = (api.WithdrawResponse != null ? api.WithdrawResponse.desc : api.errorMessage)
					};
					return true;
				}
				else
				{
					Error = new Error
					{
						ErrorCode = "-9999",
						ErrorDesc = (api.WithdrawResponse != null ? api.WithdrawResponse.desc : api.errorMessage)
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

		public vm.Get4DPackageVM Get4DPackage()
		{
			var res = new vm.Get4DPackageVM();

			try
			{
				if (gameMap != null)
				{
					var apiCall = api.GetProfile(gameMap.GamePlayerID);
					InsertLog(gameID, "Get4DPackage", "request", api.request, "response", api.response);

					targetApiUrl = api.targetUrl;
					request = api.request;
					response = api.response;

					if (apiCall)
					{
						dynamic result = new System.Dynamic.ExpandoObject();
						result.others = new System.Dynamic.ExpandoObject();
						result.gd = new System.Dynamic.ExpandoObject();

						result.others._B = new System.Dynamic.ExpandoObject();
						result.others._S = new System.Dynamic.ExpandoObject();
						result.others._SA = new System.Dynamic.ExpandoObject();
						result.others._3A = new System.Dynamic.ExpandoObject();
						result.others._3C = new System.Dynamic.ExpandoObject();
						result.others._5D = new System.Dynamic.ExpandoObject();
						result.others._6D = new System.Dynamic.ExpandoObject();

						result.gd._B = new System.Dynamic.ExpandoObject();
						result.gd._S = new System.Dynamic.ExpandoObject();
						result.gd._SA = new System.Dynamic.ExpandoObject();
						result.gd._3A = new System.Dynamic.ExpandoObject();
						result.gd._3C = new System.Dynamic.ExpandoObject();
						result.gd._5D = new System.Dynamic.ExpandoObject();
						result.gd._6D = new System.Dynamic.ExpandoObject();

						result.others._B._1ST = api.GetProfileResponse.prizeBig1;
						result.others._B._2ND = api.GetProfileResponse.prizeBig2;
						result.others._B._3RD = api.GetProfileResponse.prizeBig3;
						result.others._B._SPC = api.GetProfileResponse.prizeBig4;
						result.others._B._CON = api.GetProfileResponse.prizeBig5;

						result.others._S._1ST = api.GetProfileResponse.prizeSmall1;
						result.others._S._2ND = api.GetProfileResponse.prizeSmall2;
						result.others._S._3RD = api.GetProfileResponse.prizeSmall3;
						result.others._S._SPC = null;
						result.others._S._CON = null;

						result.others._SA._1ST = api.GetProfileResponse.prize4a;
						result.others._SA._2ND = null;
						result.others._SA._3RD = null;
						result.others._SA._SPC = null;
						result.others._SA._CON = null;

						result.others._3A._1ST = api.GetProfileResponse.prizeA;
						result.others._3A._2ND = null;
						result.others._3A._3RD = null;
						result.others._3A._SPC = null;
						result.others._3A._CON = null;

						result.others._3C._1ST = api.GetProfileResponse.prizeAbc1;
						result.others._3C._2ND = api.GetProfileResponse.prizeAbc2;
						result.others._3C._3RD = api.GetProfileResponse.prizeAbc3;
						result.others._3C._SPC = null;
						result.others._3C._CON = null;

						result.others._5D._1ST = api.GetProfileResponse.prize5D1;
						result.others._5D._2ND = api.GetProfileResponse.prize5D2;
						result.others._5D._3RD = api.GetProfileResponse.prize5D3;
						result.others._5D._SPC = api.GetProfileResponse.prize5D4;
						result.others._5D._CON = api.GetProfileResponse.prize5D5;

						result.others._6D._1ST = api.GetProfileResponse.prize6D1;
						result.others._6D._2ND = api.GetProfileResponse.prize6D2;
						result.others._6D._3RD = api.GetProfileResponse.prize6D3;
						result.others._6D._SPC = api.GetProfileResponse.prize6D4;
						result.others._6D._CON = api.GetProfileResponse.prize6D5;

						result.gd._B._1ST = api.GetProfileResponse.GDprizeBig1;
						result.gd._B._2ND = api.GetProfileResponse.GDprizeBig2;
						result.gd._B._3RD = api.GetProfileResponse.GDprizeBig3;
						result.gd._B._SPC = api.GetProfileResponse.GDprizeBig4;
						result.gd._B._CON = api.GetProfileResponse.GDprizeBig5;

						result.gd._S._1ST = api.GetProfileResponse.GDprizeSmall1;
						result.gd._S._2ND = api.GetProfileResponse.GDprizeSmall2;
						result.gd._S._3RD = api.GetProfileResponse.GDprizeSmall3;
						result.gd._S._SPC = null;
						result.gd._S._CON = null;

						result.gd._SA._1ST = api.GetProfileResponse.GDprize4a;
						result.gd._SA._2ND = null;
						result.gd._SA._3RD = null;
						result.gd._SA._SPC = null;
						result.gd._SA._CON = null;

						result.gd._3A._1ST = api.GetProfileResponse.GDprizeA;
						result.gd._3A._2ND = null;
						result.gd._3A._3RD = null;
						result.gd._3A._SPC = null;
						result.gd._3A._CON = null;

						result.gd._3C._1ST = api.GetProfileResponse.GDprizeAbc1;
						result.gd._3C._2ND = api.GetProfileResponse.GDprizeAbc2;
						result.gd._3C._3RD = api.GetProfileResponse.GDprizeAbc3;
						result.gd._3C._SPC = null;
						result.gd._3C._CON = null;

						result.gd._5D._1ST = "-";
						result.gd._5D._2ND = "-";
						result.gd._5D._3RD = "-";
						result.gd._5D._SPC = "-";
						result.gd._5D._CON = "-";

						result.gd._6D._1ST = api.GetProfileResponse.GDprize6D1;
						result.gd._6D._2ND = api.GetProfileResponse.GDprize6D2;
						result.gd._6D._3RD = api.GetProfileResponse.GDprize6D3;
						result.gd._6D._SPC = api.GetProfileResponse.GDprize6D4;
						result.gd._6D._CON = api.GetProfileResponse.GDprize6D5;

						res.PlayerID = playerID;
						res.Package = result;
						res.Game = gameID;
						res.Message = "Successfully get package.";
						res.Code = 0;
					}
					else
					{
						res.PlayerID = playerID;
						res.Package = null;
						res.Game = gameID;
						//res.Message = "Get package api error.";
						res.Message = api.GetProfileResponse.desc;
						res.Code = 2;
					}
				}
				else
				{
					res.PlayerID = playerID;
					res.Package = null;
					res.Game = gameID;
					res.Message = "Game account not found.";
					res.Code = 1;
				}
			}
			catch (Exception ex)
			{
				_common.LogError("Get4DPackage" + " " + DateTime.Now.ToString() + " " + string.Format("{0}", playerID) + " " + ex.Message.ToString(), gameID);

				var err = new Error
				{
					ErrorCode = "999",
					ErrorDesc = ex.Message.ToString()
				};

				InsertLog(gameID, "Get4DPackage_Exception", "Error", JsonConvert.SerializeObject(err));

				res.PlayerID = playerID;
				res.Package = null;
				res.Game = gameID;
				res.Message = ex.Message.ToString();
				res.Code = 99;
			}

			return res;
		}

		public BT28WIN.GetBetListByPageResponse GetBetListByPage(DateTime dateFrom, DateTime dateTo, int page = 1, string loginID = "")
		{
			var apiCall = api.GetBetListByPage(dateFrom, dateTo, page, loginID);

			InsertLog(gameID, "BT28WINGameRecord", "request", api.request, "response", api.response);

			targetApiUrl = api.targetUrl;
			request = api.request;
			response = api.response;

			if (apiCall)
			{
				return api.GetBetListByPageResponse;
			}
			return null;
		}

		public BT28WIN.GetDrawResultResponse GetDrawResult(DateTime drawDate)
		{
			var apiCall = api.GetDrawResult(drawDate);

			targetApiUrl = api.targetUrl;
			request = api.request;
			response = api.response;

			if (apiCall)
			{
				return api.GetDrawResultResponse;
			}
			return null;
		}

		public BT28WIN.GetWinningNumberResponse GetWinningNumber(DateTime dateFrom, DateTime dateTo, string loginID = "")
		{
			var apiCall = api.GetWinningNumber(dateFrom, dateTo, loginID);

			targetApiUrl = api.targetUrl;
			request = api.request;
			response = api.response;

			if (apiCall)
			{
				return api.GetWinningNumberResponse;
			}
			return null;
		}
		#endregion

		#region Private Function
		private bool IsBlocked_Process()
		{
			TimeSpan _7pm = new TimeSpan(0, 19, 0, 0, 0);
			TimeSpan _9pm = new TimeSpan(0, 21, 0, 0, 0);
			if (TimeSpan.Compare(DateTime.Now.TimeOfDay, _7pm) == -1 || TimeSpan.Compare(DateTime.Now.TimeOfDay, _9pm) == 1)
				return false;

			return true;
		}

		private bool IsBlocked_Peak()
		{
			TimeSpan _5pm = new TimeSpan(0, 17, 0, 0, 0);
			TimeSpan _7pm = new TimeSpan(0, 19, 0, 0, 0);
			if (TimeSpan.Compare(DateTime.Now.TimeOfDay, _5pm) == -1 || TimeSpan.Compare(DateTime.Now.TimeOfDay, _7pm) == 1)
				return false;

			return true;
		}
		#endregion
	}
}