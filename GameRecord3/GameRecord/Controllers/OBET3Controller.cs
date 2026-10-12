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
using System.Threading;

namespace GameRecord.Controllers
{
	public class OBET3Controller : Controller
	{
		public static readonly object MyLock = new object();
		private string P_GamePrefix = "OBET3";

		private string P_FilePrefix = "OBET3";
		private string P_DTFormat = "yyyy-MM-ddTHH:mm:ss.fffZ";
		Common _common = new Common();
		private OBETConnector api = new OBETConnector(Environment: BaseConnector.GameEnvironment.PROD);

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
				_common.LogError("OBETGame/UpdJobTime" + " " + DateTime.Now.ToString() + "  " + ex.InnerException.InnerException.ToString(), P_FilePrefix);
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
						DateTime dtSTime = Convert.ToDateTime(DateTime.Now.ToString("yyyy-MM-dd HH:mm"));
						sTime = dtSTime.ToString("yyyy-MM-dd HH:mm:ss");
					}
				}

				return sTime;

			}
			catch (Exception ex)
			{
				_common.LogError("OBETGame/GetJobTime" + " " + DateTime.Now.ToString() + "  " + ex.InnerException.InnerException.ToString(), P_FilePrefix);
				return "Error:" + ex.InnerException.InnerException.ToString();
			}
		}

		public void OBETJob()
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
				_common.LogError("Start - " + DateTime.Now.ToString() + "  " + ex.InnerException.InnerException.ToString(), P_FilePrefix);
			}
		}

		public bool GetGameRecord()
		{
			string startTime = GetJobTime("Start");
			string endTime = GetJobTime("End");
			string mStartTime = startTime;
			string mEndTime = endTime;
			int limit = 15; // 24 hours
			//int timeRangeLimit = 15;
			int count = 0;
			string langId = "en";
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

					List<Obet.model.ReportResult> result = api.GetBrandCompletedGameResultsV2(langId, Convert.ToDateTime(mStartTime), Convert.ToDateTime(mEndTime));

					using (BingoConn db = new BingoConn())
					{
						string sMainID = Guid.NewGuid().ToString();
						ObetGameMain obetGameMain = new ObetGameMain
						{
							Id = sMainID,
							Total = null,
							StartTime = Convert.ToDateTime(mStartTime),
							EndTime = Convert.ToDateTime(mEndTime),
							CreatedDate = DateTime.Now
						};
						db.ObetGameMains.Add(obetGameMain);
						db.SaveChanges();

						if (result != null)
						{
							if (result.Count > 0)
							{
								foreach (var b in result)
								{
									try
									{
										ObetGame obetGame = new ObetGame()
										{
											fId = sMainID,
											id = b.id,
											sDate = b.sDate,
											tDate = b.tDate,
											kDate = b.kDate,
											aDate = b.aDate,
											pDate = b.pDate,
											user = b.user,
											sport = b.sport,
											league = b.league,
											home = b.home,
											away = b.away,
											sport_en = b.sport_en,
											league_en = b.league_en,
											home_en = b.home_en,
											away_en = b.away_en,
											live = b.live,
											homeScore = b.homeScore,
											awayScore = b.awayScore,
											status = b.status,
											ft = b.ft,
											oddsType = b.oddsType,
											game = b.game,
											info = b.info,
											odds = b.odds,
											side = b.side,
											side_en = b.side_en,
											bamt = b.bamt,
											wamt = b.wamt,
											tresult = b.tresult,
											fhscore = b.fhscore,
											ftscore = b.ftscore,
											fg = b.fg,
											lg = b.lg,
											ip = b.ip,
											isMobile = b.isMobile,
											commission = b.commission,
											ModifiedDate = DateTime.Now,
											CreatedDate = DateTime.Now
										};
										db.ObetGames.AddOrUpdate(obetGame);
									}
									catch (DbEntityValidationException e)
									{
										foreach (var eve in e.EntityValidationErrors)
										{
											_common.LogError("OBETGame/GetGameRecord" + " " + DateTime.Now.ToString() + " " + string.Format("Entity of type \"{0}\" in state \"{1}\" has the following validation errors:",
															eve.Entry.Entity.GetType().Name, eve.Entry.State), P_FilePrefix);
											foreach (var ve in eve.ValidationErrors)
											{
												_common.LogError("OBETGame/GetGameRecord" + " " + DateTime.Now.ToString() + " " + string.Format("- Property: \"{0}\", Error: \"{1}\"",
																ve.PropertyName, ve.ErrorMessage), P_FilePrefix);
											}
										}
									}
									catch (WebException ex)
									{
										var error = new StreamReader(ex.Response.GetResponseStream()).ReadToEnd();
										_common.LogError("Record " + b.id + " " + DateTime.Now.ToString() + " " + ex.InnerException.InnerException.ToString() + " " + error, P_FilePrefix);
									}
									catch (Exception ex)
									{
										_common.LogError("Record " + b.id + " " + DateTime.Now.ToString() + " " + ex.InnerException.InnerException.ToString(), P_FilePrefix);
									}
								}
								db.SaveChanges();
							}
						}

						bool isUpdJob = UpdJobTime(mStartTime, mEndTime, sMainID);
					}

					Thread.Sleep(30000);
				}


				return true;
			}
			catch (DbEntityValidationException e)
			{
				foreach (var eve in e.EntityValidationErrors)
				{
					_common.LogError("OBETGame/GetGameRecord" + " " + DateTime.Now.ToString() + " " + string.Format("Entity of type \"{0}\" in state \"{1}\" has the following validation errors:",
									eve.Entry.Entity.GetType().Name, eve.Entry.State), P_FilePrefix);
					foreach (var ve in eve.ValidationErrors)
					{
						_common.LogError("OBETGame/GetGameRecord" + " " + DateTime.Now.ToString() + " " + string.Format("- Property: \"{0}\", Error: \"{1}\"",
										ve.PropertyName, ve.ErrorMessage), P_FilePrefix);
					}
				}
				return false;
			}
			catch (WebException ex)
			{
				var responseText = new StreamReader(ex.Response.GetResponseStream()).ReadToEnd();
				_common.LogError("OBETGame/GetGameRecord" + " " + DateTime.Now.ToString() + " " + ex.InnerException.InnerException.ToString() + " " + responseText, P_FilePrefix);
				return false;
			}
			catch (Exception ex)
			{
				_common.LogError("OBETGame/GetGameRecord" + " " + DateTime.Now.ToString() + " " + ex.InnerException.InnerException.ToString(), P_FilePrefix);
				return false;
			}
		}
	}
}