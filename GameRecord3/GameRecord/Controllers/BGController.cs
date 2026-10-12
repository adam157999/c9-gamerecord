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
	public class BGController : Controller
	{
		public static readonly object MyLock = new object();
		private string P_GamePrefix = "BG";

		private string P_FilePrefix = "BG";
		private string P_NamePrefix = "BGC";
		private string P_DTFormat = "yyyy-MM-ddTHH:mm:ss.fffZ";
		Common _common = new Common();
		private BGConnector api = new BGConnector(Environment: BaseConnector.GameEnvironment.PROD);

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
						DateTime dtSTime = Convert.ToDateTime(DateTime.Now.ToString("yyyy-MM-dd HH:mm")) - new TimeSpan(0, 12, 5, 0, 0);
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

		public bool GetGameRecord()
		{
			string startTime = GetJobTime("Start");
			string endTime = GetJobTime("End");
			string mStartTime = startTime;
			string mEndTime = endTime;
			int limit = 1440; // 24 hours
			int pageSize = 500;
			int pageIndex = 1;

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
						mEndTime = Convert.ToDateTime(mStartTime).AddMinutes(limit).ToString("yyyy-MM-dd HH:mm:ss");

					BG.Model.GetReportResult result = api.GetBrandCompletedGameResultsV2(mStartTime, mEndTime, pageIndex, pageSize);

					using (BingoConn db = new BingoConn())
					{
						string sMainID = Guid.NewGuid().ToString();
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

						if (result != null)
						{
							if (result.items != null)
							{
								if (result.items.Count > 0)
								{
									foreach (var b in result.items)
									{
										try
										{
											if (b.loginId.ToUpper().Trim().StartsWith(P_NamePrefix))
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
										}
										catch (DbEntityValidationException e)
										{
											foreach (var eve in e.EntityValidationErrors)
											{
												_common.LogError("BG/GetGameRecord" + " " + DateTime.Now.ToString() + " " + string.Format("Entity of type \"{0}\" in state \"{1}\" has the following validation errors:",
																eve.Entry.Entity.GetType().Name, eve.Entry.State), P_FilePrefix);
												foreach (var ve in eve.ValidationErrors)
												{
													_common.LogError("BG/GetGameRecord" + " " + DateTime.Now.ToString() + " " + string.Format("- Property: \"{0}\", Error: \"{1}\"",
																	ve.PropertyName, ve.ErrorMessage), P_FilePrefix);
												}
											}
										}
										catch (WebException ex)
										{
											var error = new StreamReader(ex.Response.GetResponseStream()).ReadToEnd();
											_common.LogError("Record " + b.orderId + " " + DateTime.Now.ToString() + " " + ex.Message.ToString() + " " + error, P_FilePrefix);
										}
										catch (Exception ex)
										{
											_common.LogError("Record " + b.orderId + " " + DateTime.Now.ToString() + " " + ex.Message.ToString(), P_FilePrefix);
										}
									}
									db.SaveChanges();

									decimal dCnt = Convert.ToDecimal(result.total) / Convert.ToDecimal(pageSize);
									int iPageIndex = Int32.Parse(Math.Ceiling(dCnt).ToString());

									if (iPageIndex > 1)
									{
										for (int x = 2; x <= iPageIndex; x++)
										{
											bool IsOK = GetGameRecordExtend(mStartTime, mEndTime, x, pageSize, sMainID);
										}
									}
								}
							}

							bool isUpdJob = UpdJobTime(mStartTime, mEndTime, sMainID);
						}
					}
				}
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

		public bool GetGameRecordExtend(string startTime, string endTime, int pageIndex, int pageSize, string sMainID)
		{
			try
			{
				BG.Model.GetReportResult result = api.GetBrandCompletedGameResultsV2(startTime, endTime, pageIndex, pageSize);

				using (BingoConn db = new BingoConn())
				{
					if (result != null)
					{
						if (result.items != null)
						{
							if (result.items.Count > 0)
							{
								foreach (var b in result.items)
								{
									try
									{
										if (b.loginId.ToUpper().Trim().StartsWith(P_NamePrefix))
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
									}
									catch (DbEntityValidationException e)
									{
										foreach (var eve in e.EntityValidationErrors)
										{
											_common.LogError("BG/GetGameRecord" + " " + DateTime.Now.ToString() + " " + string.Format("Entity of type \"{0}\" in state \"{1}\" has the following validation errors:",
															eve.Entry.Entity.GetType().Name, eve.Entry.State), P_FilePrefix);
											foreach (var ve in eve.ValidationErrors)
											{
												_common.LogError("BG/GetGameRecord" + " " + DateTime.Now.ToString() + " " + string.Format("- Property: \"{0}\", Error: \"{1}\"",
																ve.PropertyName, ve.ErrorMessage), P_FilePrefix);
											}
										}
									}
									catch (WebException ex)
									{
										var error = new StreamReader(ex.Response.GetResponseStream()).ReadToEnd();
										_common.LogError("Record " + b.orderId + " " + DateTime.Now.ToString() + " " + ex.Message.ToString() + " " + error, P_FilePrefix);
									}
									catch (Exception ex)
									{
										_common.LogError("Record " + b.orderId + " " + DateTime.Now.ToString() + " " + ex.Message.ToString(), P_FilePrefix);
									}
								}
								db.SaveChanges();
							}
						}
					}
				}
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