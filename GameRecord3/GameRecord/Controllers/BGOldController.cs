using System;
using System.Linq;
using System.Web.Mvc;
using System.Net;
using System.IO;
using GameRecord.Models;
using System.Data.Entity.Migrations;
using System.Data.Entity.Validation;
using GameRecord.Controllers.V2;
using System.Collections.Generic;

namespace GameRecord.Controllers
{
	public class BGOldController : Controller
	{
		public static readonly object MyLock = new object();
		private string P_GamePrefix = "BG";

		private string P_FilePrefix = "BG";
		private string P_DTFormat = "yyyy-MM-ddTHH:mm:ss.fffZ";
		Common _common = new Common();
		private BGConnector api = new BGConnector(Environment: BaseConnector.GameEnvironment.PROD);

		public bool UpdJobTime(string startTime, string endTime, string sMainID)
		{
			try
			{
				DateTime dtEnd = Convert.ToDateTime(endTime);

				using (var bingoConn = new BingoConn())
				{
					var xyz = bingoConn.ADMJobDates.Where(s => s.GameProvider == P_GamePrefix).FirstOrDefault();
					if (xyz != null)
					{
						xyz.LastRunTillDate = dtEnd;

						bingoConn.Entry(xyz).State = System.Data.Entity.EntityState.Modified;
						bingoConn.SaveChanges();
					}
				}
				return true;
			}
			catch (Exception ex)
			{
				_common.LogError("BGGame/UpdJobTime" + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), P_FilePrefix);
				return false;
			}
		}

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
						DateTime dtSTime = Convert.ToDateTime(DateTime.Now.ToString("yyyy-MM-dd HH:mm")) - new TimeSpan(0, 12, 0, 0, 0);
						sTime = dtSTime.ToString("yyyy-MM-dd HH:mm:ss");
					}
				}

				return sTime;

			}
			catch (Exception ex)
			{
				_common.LogError("BGGame/GetJobTime" + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), P_FilePrefix);
				return "Error:" + ex.Message.ToString();
			}
		}

		public void BGJob()
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

		public IDictionary<int, List<BG.Model.ReportResult>> GetGameRecordCore(string startTime, string endTime, string sMainID, int pageSize = 0, int pageIndex = 1)
		{
			int count = 0;
			int finalPageIndex = 0;
			Dictionary<int, List<BG.Model.ReportResult>> returnValue = new Dictionary<int, List<BG.Model.ReportResult>> { };
			BG.Model.GetReportResult result = api.GetBrandCompletedGameResultsV2(startTime, endTime, pageIndex, pageSize);
			using (BingoConn db = new BingoConn())
			{
				BGGameMain bgMain = new BGGameMain()
				{
					Id = sMainID,
					Total = null,
					StartTime = Convert.ToDateTime(startTime),
					EndTime = Convert.ToDateTime(endTime),
					PageIndex = result.pageIndex,
					PageSize = result.pageSize,
					CreatedDate = DateTime.Now
				};
				db.BGGameMains.Add(bgMain);
				db.SaveChanges();

				if (result.items.Count > 0)
				{
					foreach (var b in result.items)
					{
						count++;
						string id = count.ToString();
						try
						{
							BGGame bgGame = new BGGame()
							{
								fId = sMainID,
								orderId = b.orderId,
								tranId = b.tranId,
								sn = b.sn,
								uid = b.uid,
								loginId = b.loginId,
								moduleId = b.moduleId,
								moduleName = b.moduleName,
								gameId = b.gameId,
								gameName = b.gameName,
								orderStatus = b.orderStatus,
								bAmount = b.bAmount,
								aAmount = b.aAmount,
								orderFrom = b.orderFrom,
								orderTime = b.orderTime,
								lastUpdateTime = b.lastUpdateTime,
								tableId = b.tableId,
								issueId = b.issueId,
								playId = b.playId,
								playName = b.playName,
								playNameEn = b.playNameEn,
								validBet = b.validBet,
								payment = b.payment,
								CreatedDate = DateTime.Now
							};
							db.BGGames.AddOrUpdate(bgGame);
						}
						catch (DbEntityValidationException e)
						{
							foreach (var eve in e.EntityValidationErrors)
							{
								_common.LogError("BGGame/GetGameRecord" + " " + DateTime.Now.ToString() + " " + string.Format("Entity of type \"{0}\" in state \"{1}\" has the following validation errors:",
												eve.Entry.Entity.GetType().Name, eve.Entry.State), P_FilePrefix);
								foreach (var ve in eve.ValidationErrors)
								{
									_common.LogError("BGGame/GetGameRecord" + " " + DateTime.Now.ToString() + " " + string.Format("- Property: \"{0}\", Error: \"{1}\"",
													ve.PropertyName, ve.ErrorMessage), P_FilePrefix);
								}
							}
						}
						catch (WebException ex)
						{
							var error = new StreamReader(ex.Response.GetResponseStream()).ReadToEnd();
							_common.LogError("Record " + id + " " + DateTime.Now.ToString() + " " + ex.Message.ToString() + " " + error, P_FilePrefix);
						}
						catch (Exception ex)
						{
							_common.LogError("Record " + id + " " + DateTime.Now.ToString() + " " + ex.Message.ToString(), P_FilePrefix);
						}
					}
					db.SaveChanges();
					int paginationResult = result.total % result.items.Count;
					if (paginationResult != 0)
					{
						finalPageIndex = result.pageIndex + 1;
					}
					returnValue.Add(finalPageIndex, result.items);
				}
				return returnValue;
			}
		}

		public bool GetGameRecord()
		{
			string startTime = GetJobTime("Start");
			string endTime = GetJobTime("End");
			string mStartTime = startTime;
			string mEndTime = endTime;
			int limit = 1440; // 24 hours
			int pageSize = 500;
			string sMainID = Guid.NewGuid().ToString();
			try
			{
				//int MinDiff = Convert.ToInt32((Convert.ToDateTime(endTime) - Convert.ToDateTime(startTime)).TotalMinutes);
				int times = 1;
				for (int j = limit; j <= times * limit; j += limit)
				{
					mStartTime = j == limit ? startTime : mEndTime;

					//if ((Convert.ToDateTime(mStartTime).AddMinutes(limit) - Convert.ToDateTime(endTime)).TotalMinutes > 0)
					//    mEndTime = endTime;
					//else
					mEndTime = Convert.ToString(Convert.ToDateTime(mStartTime).AddMinutes(limit).ToString("yyyy-MM-dd HH:mm:ss"));
					IDictionary<int, List<BG.Model.ReportResult>> reportInsertedFlag = GetGameRecordCore(startTime, mEndTime, sMainID, pageSize);
					if (reportInsertedFlag.Count != 0)
					{
						while (reportInsertedFlag.FirstOrDefault().Value.Count > 0)
						{
							sMainID = Guid.NewGuid().ToString();
							//decimal dCnt = Convert.ToDecimal(reportInsertedFlag.FirstOrDefault().Key) / Convert.ToDecimal(pageSize);
							//int iPageIndex = Int32.Parse(Math.Ceiling(dCnt).ToString());
							int iPageIndex = reportInsertedFlag.FirstOrDefault().Key;
							if (iPageIndex > 1)
							{
								reportInsertedFlag.Clear();
								reportInsertedFlag = GetGameRecordCore(startTime, mEndTime, sMainID, pageSize, iPageIndex);
								if (reportInsertedFlag.Count == 0)
								{
									reportInsertedFlag[iPageIndex] = new List<BG.Model.ReportResult>();
								}
							}
							else
							{
								reportInsertedFlag[iPageIndex] = new List<BG.Model.ReportResult>();
							}

							bool isUpdJob = UpdJobTime(startTime, endTime, sMainID);
						}
					}
				}
				//}


				return true;
			}
			catch (DbEntityValidationException e)
			{
				foreach (var eve in e.EntityValidationErrors)
				{
					_common.LogError("BGGame/GetGameRecord" + " " + DateTime.Now.ToString() + " " + string.Format("Entity of type \"{0}\" in state \"{1}\" has the following validation errors:",
									eve.Entry.Entity.GetType().Name, eve.Entry.State), P_FilePrefix);
					foreach (var ve in eve.ValidationErrors)
					{
						_common.LogError("BGGame/GetGameRecord" + " " + DateTime.Now.ToString() + " " + string.Format("- Property: \"{0}\", Error: \"{1}\"",
										ve.PropertyName, ve.ErrorMessage), P_FilePrefix);
					}
				}
				return false;
			}
			catch (WebException ex)
			{
				var responseText = new StreamReader(ex.Response.GetResponseStream()).ReadToEnd();
				_common.LogError("BGGame/GetGameRecord" + " " + DateTime.Now.ToString() + " " + ex.Message.ToString() + " " + responseText, P_FilePrefix);
				return false;
			}
			catch (Exception ex)
			{
				_common.LogError("BGGame/GetGameRecord" + " " + DateTime.Now.ToString() + " " + ex.Message.ToString(), P_FilePrefix);
				return false;
			}
		}
	}
}