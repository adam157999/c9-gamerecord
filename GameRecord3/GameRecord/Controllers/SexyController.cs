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
using System.Text;
using GameRecord.Models;
using RestSharp;
using System.Dynamic;
using Newtonsoft.Json.Linq;
using System.Data.Entity.Migrations;
using System.Globalization;
using System.Threading;
using System.Data.Entity.Validation;

namespace GameRecord.Controllers
{
	public class SexyController : Controller
	{
		//staging
		//public static readonly object MyLock = new object();
		//private string P_GamePrefix = "SexyGaming";
		//private string P_ApiUrl = "https://api.mm-api.com/";
		//private string P_AgentName = "MCM";
		//private string P_GameRecordUser = "ERICTEST";
		//private string P_ApiKey = "f1951a49b49c0e5f561d01b9a6cf565d0496dbb9fb5501c8554541894b7a2ca3";
		//private int P_PageSize = 1000;
		//private int P_PageIndex = 1;
		//private int P_TimeoutSec = 60000;
		//private bool IsNextPage = false;

		//production Serene
		//public static readonly object MyLock = new object();
		//private string P_GamePrefix = "SexyGaming";
		//private string P_ApiUrl = "https://fetch.onlinegames22.com";
		//private string P_AgentName = "MCM";
		//private string P_GameRecordUser = "ERICTEST";
		//private string P_ApiKey = "821814a6d7c534055a93819f5999f527707d705a3d8a95bb1d9f51b68558c702";
		//private int P_PageSize = 1000;
		//private int P_PageIndex = 1;
		//private int P_TimeoutSec = 60000;
		//private bool IsNextPage = false;

		//production A1
		public static readonly object MyLock = new object();
		private string P_GamePrefix = "SEXYGAMING";
		private string P_FilePrefix = "SEXYGAMING";
		private string P_ApiUrl = "https://tttint.onlinegames22.com";
		private string P_AgentName = "anne";
		private string P_GameRecordUser = "c5i";
		private string P_ApiKey = "2jb1udWtdgOuVNTvrb3";
		private int P_PageSize = 1000;
		private int P_PageIndex = 1;
		private int P_TimeoutSec = 60000;
		private bool IsNextPage = false;

		Common _common = new Common();
		private AESexyConnector api = new AESexyConnector(Environment: BaseConnector.GameEnvironment.PROD);


		//###################################################################################
		#region Main Function 

		public bool GetGameRecord()
		{
			string startTime = GetJobTime("Start");
			string endTime = GetJobTime("End");
			string mStartTime = startTime;
			string mEndTime = endTime;
			int limit = 60; // 1 hours

			try
			{
				int MinDiff = Convert.ToInt32((Convert.ToDateTime(endTime) - Convert.ToDateTime(startTime)).TotalMinutes);
				int times = (MinDiff / limit) + 1;
				for (int j = limit; j <= times * limit; j += limit)
				{
					mStartTime = j == limit ? startTime : mEndTime;

					if ((Convert.ToDateTime(mStartTime).AddMinutes(limit) - Convert.ToDateTime(endTime)).TotalMinutes > 0)
						mEndTime = endTime;
					else
						mEndTime = Convert.ToString(Convert.ToDateTime(mStartTime).AddMinutes(limit));

					AESexy.getTransactionByUpdateDateResponse result1 = api.getTransactionByUpdateDate(Convert.ToDateTime(mStartTime));
					AESexy.getSummaryByTxTimeHourResponse result2 = api.getSummaryByTxTimeHour(Convert.ToDateTime(mStartTime).AddHours(-2), Convert.ToDateTime(mEndTime).AddHours(-1));

					AESexy.getTransactionByTxTimeResponse result = api.getTransactionByTxTime(Convert.ToDateTime(mStartTime), Convert.ToDateTime(mEndTime));

					using (BingoConn db = new BingoConn())
					{
						string sMainID = Guid.NewGuid().ToString();
						SexyGameMain sexyGameMain = new SexyGameMain()
						{
							Id = sMainID,
							Total = result.status == "0000" && result.transactions != null ? result.transactions.Count() : 0,
							Code = Convert.ToInt32(result.status),
							Msg = result.desc,
							StartTime = Convert.ToDateTime(startTime),
							EndTime = Convert.ToDateTime(endTime),
							CreatedDate = DateTime.Now
						};
						db.SexyGameMains.Add(sexyGameMain);
						db.SaveChanges();

						if (result.status == "0000")
						{
							if (result.transactions != null)
							{
								if (result.transactions.Count() > 0)
								{
									foreach (var obj in result.transactions)
									{
										string BetId = obj.platform + obj.platformTxId;

										try
										{
											SexyBetDetail sexyGame = new SexyBetDetail()
											{
												fId = sMainID,
												betId = BetId,
												gameType = obj.gameType,
												winAmount = obj.winAmount,
												settleStatus = obj.settleStatus,
												realBetAmount = obj.realBetAmount,
												realWinAmount = obj.realWinAmount,
												txTime = !string.IsNullOrEmpty(obj.txTime) ? DateTime.ParseExact(obj.txTime, "yyyy-MM-ddTHH:mm:sszzz", CultureInfo.InvariantCulture) : (DateTime?)null,
												updateTime = !string.IsNullOrEmpty(obj.updateTime) ? DateTime.ParseExact(obj.updateTime, "yyyy-MM-ddTHH:mm:sszzz", CultureInfo.InvariantCulture) : (DateTime?)null,
												userId = obj.userId,
												betType = obj.betType,
												platform = obj.platform,
												txStatus = obj.txStatus,
												betAmount = obj.betAmount,
												gameName = obj.gameName,
												platformTxId = obj.platformTxId,
												betTime = !string.IsNullOrEmpty(obj.betTime) ? DateTime.ParseExact(obj.betTime, "yyyy-MM-ddTHH:mm:sszzz", CultureInfo.InvariantCulture) : (DateTime?)null,
												gameCode = obj.gameCode,
												currency = obj.currency,
												jackpotBetAmount = obj.jackpotBetAmount,
												jackpotWinAmount = obj.jackpotWinAmount,
												turnover = obj.turnover,
												roundId = obj.roundId,
												gameInfo = obj.gameInfo
											};

											var record = db.SexyBetDetails.Where(x => x.betId == BetId).FirstOrDefault();

											if (record == null)
											{
												sexyGame.CreatedDt = DateTime.Now;
											}
											else
											{
												sexyGame.CreatedDt = record.CreatedDt;
												sexyGame.ModifiedDt = DateTime.Now;
											}

											db.SexyBetDetails.AddOrUpdate(sexyGame);
										}
										catch (Exception ex)
										{
											_common.LogError("Record " + BetId + " " + DateTime.Now.ToString() + " " + ex.Message.ToString(), P_FilePrefix);
										}
									}
									db.SaveChanges();
								}
							}
						}

						if (result.status == "0000")
						{
							bool isUpdJob = UpdJobTime(mStartTime, mEndTime, sMainID);
						}

						Thread.Sleep(30000); //sleep for 30 seconds

					}
				}

				return true;
			}
			catch (DbEntityValidationException e)
			{
				foreach (var eve in e.EntityValidationErrors)
				{
					_common.LogError("Sexy/GetGameRecord" + " " + DateTime.Now.ToString() + " " + string.Format("Entity of type \"{0}\" in state \"{1}\" has the following validation errors:",
									eve.Entry.Entity.GetType().Name, eve.Entry.State), P_FilePrefix);
					foreach (var ve in eve.ValidationErrors)
					{
						_common.LogError("Sexy/GetGameRecord" + " " + DateTime.Now.ToString() + " " + string.Format("- Property: \"{0}\", Error: \"{1}\"",
										ve.PropertyName, ve.ErrorMessage), P_FilePrefix);
					}
				}
				return false;
			}
			catch (WebException ex)
			{
				var responseText = new StreamReader(ex.Response.GetResponseStream()).ReadToEnd();
				_common.LogError("Sexy/GetGameRecord" + " " + DateTime.Now.ToString() + " " + ex.InnerException.ToString() + " " + responseText, P_FilePrefix);
				return false;
			}
			catch (Exception ex)
			{
				_common.LogError("Sexy/GetGameRecord" + " " + DateTime.Now.ToString() + "  " + ex.InnerException.ToString(), P_FilePrefix);
				return false;
			}
		}

		public bool GetGameRecordExtend(string startTime, string endTime, string sMainID)
		{
			try
			{
				string method = "data/BetListDetailsV3";
				string webAddr = P_ApiUrl + method;

				var httpWebRequest = (HttpWebRequest)WebRequest.Create(webAddr);
				httpWebRequest.ContentType = "application/json";
				httpWebRequest.Method = "POST";
				httpWebRequest.Timeout = P_TimeoutSec;
				httpWebRequest.ReadWriteTimeout = P_TimeoutSec;
				//httpWebRequest.Headers.Add("Authorization", "Bearer " + GetToken());

				string requestStr = "";
				using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
				{
					dynamic request = new ExpandoObject();
					request.fromDate = startTime;
					request.toDate = endTime;
					request.pageIndex = P_PageIndex;
					request.pageSize = P_PageSize;
					request.AgentName = P_AgentName;
					request.APIKey = P_ApiKey;
					request.providerId = 6;
					request.TimeSpecific = true;

					requestStr = JsonConvert.SerializeObject(request);

					streamWriter.Write(requestStr);
					streamWriter.Flush();
				}

				var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
				using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
				{
					var responseText = streamReader.ReadToEnd();
					dynamic result = JObject.Parse(responseText);

					using (BingoConn db = new BingoConn())
					{
						if (result.error == 0)
						{
							if (result.data != null)
							{
								if (result.data.Count > 0)
								{
									foreach (var obj in result.data.Entities)
									{
										string transGuid = Convert.ToString(obj.TransGuid);

										try
										{
											SexyGame sexyGame = new SexyGame()
											{
												fId = sMainID,
												TransGuid = obj.TransGuid,
												UserName = obj.UserName,
												ActualStake = obj.ActualStake != null ? Convert.ToDecimal(obj.ActualStake) : null,
												TurnOver = obj.TurnOver != null ? Convert.ToDecimal(obj.TurnOver) : null,
												WinLost = obj.WinLost != null ? Convert.ToDecimal(obj.WinLost) : null,
												Profit = obj.Profit != null ? Convert.ToDecimal(obj.Profit) : null,
												StartAmount = obj.StartAmount != null ? Convert.ToDecimal(obj.StartAmount) : null,
												WinLoseDate = obj.WinLoseDate != null ? Convert.ToDateTime(obj.WinLoseDate) : null,
												BetDate = obj.BetDate != null ? Convert.ToDateTime(obj.BetDate) : null,
												GameType = obj.GameType,
												GameId = obj.GameId,
												ProductType = obj.ProductType,
												TableName = obj.TableName,
												Status = obj.Status,
												ProviderStatus = obj.ProviderStatus,
												ProviderId = obj.ProviderId,
												Provider = obj.Provider,
												Result = obj.Result,
												PlayTypeName = obj.PlayTypeName,
												SubBets = obj.SubBets,
												BetChoice = obj.BetChoice,
												Ip = obj.Ip
											};

											var record = db.SexyGames.Where(x => x.TransGuid == transGuid).FirstOrDefault();

											if (record == null)
											{
												sexyGame.CreatedDate = DateTime.Now;
											}
											else
											{
												sexyGame.CreatedDate = record.CreatedDate;
												sexyGame.ModifiedDate = DateTime.Now;
											}
											db.SexyGames.AddOrUpdate(sexyGame);
										}
										catch (Exception ex)
										{
											_common.LogError("Record " + transGuid + " " + DateTime.Now.ToString() + " " + ex.Message.ToString(), P_FilePrefix);
										}
									}
									db.SaveChanges();
								}

								if (Convert.ToInt16(result.data.Count) < P_PageSize)
									IsNextPage = false;
								else
									IsNextPage = true;
							}
							else
							{
								IsNextPage = false;
							}
						}
						else
						{
							IsNextPage = false;
						}
					}
				}

				return true;
			}
			catch (DbEntityValidationException e)
			{
				foreach (var eve in e.EntityValidationErrors)
				{
					_common.LogError("Sexy/GetGameRecord" + " " + DateTime.Now.ToString() + " " + string.Format("Entity of type \"{0}\" in state \"{1}\" has the following validation errors:",
									eve.Entry.Entity.GetType().Name, eve.Entry.State), P_FilePrefix);
					foreach (var ve in eve.ValidationErrors)
					{
						_common.LogError("Sexy/GetGameRecord" + " " + DateTime.Now.ToString() + " " + string.Format("- Property: \"{0}\", Error: \"{1}\"",
										ve.PropertyName, ve.ErrorMessage), P_FilePrefix);
					}
				}
				return false;
			}
			catch (WebException ex)
			{
				var responseText = new StreamReader(ex.Response.GetResponseStream()).ReadToEnd();
				_common.LogError("Sexy/GetGameRecord" + " " + DateTime.Now.ToString() + " " + ex.InnerException.ToString() + " " + responseText, P_FilePrefix);
				return false;
			}
			catch (Exception ex)
			{
				_common.LogError("Sexy/GetGameRecord" + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), P_FilePrefix);
				return false;
			}
		}

		public bool UpdJobTime(string startTime, string endTime, string sMainID)
		{
			try
			{
				DateTime dtEnd = Convert.ToDateTime(endTime);

				_common.UpdateADMJobDate(P_GamePrefix, dtEnd, "");

				//using (var bingoConn = new BingoConn())
				//{
				//	var xyz = bingoConn.ADMJobDates.Where(s => s.GameProvider == P_GamePrefix).FirstOrDefault();
				//	if (xyz != null)
				//	{
				//		xyz.LastRunTillDate = dtEnd;

				//		//bingoConn.Entry(xyz).State = System.Data.Entity.EntityState.Modified;
				//		bingoConn.SaveChanges();
				//	}
				//}
				return true;
			}
			catch (Exception ex)
			{
				_common.LogError("Sexy/UpdJobTime" + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), P_FilePrefix);
				return false;
			}
		}

		public string GetToken()
		{
			lock (MyLock)
			{
				try
				{
					string method = "account/login";
					string webAddr = P_ApiUrl + method;
					var httpWebRequest = (HttpWebRequest)WebRequest.Create(webAddr);
					httpWebRequest.ContentType = "application/json";
					httpWebRequest.Method = "POST";
					httpWebRequest.Timeout = P_TimeoutSec;
					httpWebRequest.ReadWriteTimeout = P_TimeoutSec;

					string requestStr = "";
					using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
					{
						dynamic request = new ExpandoObject();
						request.ApiKey = P_ApiKey;
						request.UserName = P_GameRecordUser;
						request.AgentName = P_AgentName;

						requestStr = JsonConvert.SerializeObject(request);

						streamWriter.Write(requestStr);
						streamWriter.Flush();
					}

					var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
					using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
					{
						var responseText = streamReader.ReadToEnd();

						dynamic result = JObject.Parse(responseText);

						if (Convert.ToInt16(result.error) == 0)
						{
							return Convert.ToString(result.data);
						}
					}

					return "";
				}
				catch (Exception ex)
				{
					_common.LogError("Sexy/GetToken" + " " + DateTime.Now.ToString() + " " + string.Format("{0}", P_GameRecordUser) + " " + ex.Message.ToString(), P_FilePrefix);
					return "";
				}
			}
		}

		#endregion

		//###################################################################################
		#region Function 

		public string GetJobTime(string action)
		{
			try
			{
				string sTime = "";

				using (var bingoConn = new BingoConn())
				{
					var query = bingoConn.ADMJobDates.Where(s => s.GameProvider == P_GamePrefix).FirstOrDefault();

					if (action == "Start")
					{
						DateTime dtSTime = Convert.ToDateTime(query.LastRunTillDate?.ToString("yyyy-MM-dd HH:mm")) - new TimeSpan(0, 0, 15, 0, 0);
						sTime = dtSTime.ToString("yyyy-MM-dd HH:mm:ss");
					}
					else
					{
						DateTime dtSTime = Convert.ToDateTime(DateTime.Now.ToString("yyyy-MM-dd HH:mm"));
						sTime = dtSTime.ToString("yyyy-MM-dd HH:mm:ss");
					}
				}

				return sTime;

			}
			catch (Exception ex)
			{
				_common.LogError("Sexy/GetJobTime" + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), P_FilePrefix);
				return "Error:" + ex.Message.ToString();
			}
		}

		#endregion

		// GET: Sexy
		public void SexyJob()
		{
			try
			{
				lock (MyLock)
				{
					bool isOK = GetGameRecord();
				}
			}
			catch (Exception ex)
			{
				_common.LogError("Start - " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), P_FilePrefix);
			}
		}

	}
}