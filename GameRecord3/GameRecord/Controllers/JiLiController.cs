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
using Newtonsoft.Json.Linq;
using System.Data.Entity.Migrations;
using System.Data.Entity.Validation;
using System.Globalization;
using static GameRecord.Controllers.BaseConnector;

namespace GameRecord.Controllers
{
	public class JiLiController : Controller
	{
		public static readonly object MyLock = new object();
		private string P_GamePrefix = "JiLi";

		private string P_FilePrefix = "JiLi";
		private string P_DTFormat = "yyyy-MM-ddTHH:mm:ss.fffZ";
		Common _common = new Common();
		private JiLiConnector api = new JiLiConnector(Environment: BaseConnector.GameEnvironment.PROD);
		public JiLiController()
		{
			api = new JiLiConnector(Environment: GameEnvironment.PROD);
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
				_common.LogError("JiLi/UpdJobTime" + " " + DateTime.Now.ToString() + "  " + ex.InnerException.InnerException.ToString(), P_FilePrefix);
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
				_common.LogError("JiLi/GetJobTime" + " " + DateTime.Now.ToString() + "  " + ex.InnerException.InnerException.ToString(), P_FilePrefix);
				return "Error:" + ex.InnerException.InnerException.ToString();
			}
		}

		public void JiLiJob()
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
			int limit = 60; // 60 minutes
			int pageLimit = 15000;
			int count = 0;
			int page = 1;
			bool filterAgent = true;
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

					JiLi.Model.GetBetRecordByTimeResponse result = api.GetBetRecordByTime(DateTime.Parse(mStartTime), DateTime.Parse(mEndTime), page, pageLimit, filterAgent);

					using (BingoConn db = new BingoConn())
					{
						string sMainID = Guid.NewGuid().ToString();
						JiLiGameMain jiliGameMain = new JiLiGameMain()
						{
							Id = sMainID,
							Total = null,
							StartTime = Convert.ToDateTime(startTime),
							EndTime = Convert.ToDateTime(endTime),
							CreatedDate = DateTime.Now
						};
						db.JiLiGameMains.Add(jiliGameMain);
						db.SaveChanges();

						if (result.ErrorCode == 0)
						{
							if (result.Data.Result.Count > 0)
							{
								foreach (var b in result.Data.Result)
								{
									try
									{
										JiLiGame jiliGame = new JiLiGame()
										{
											fId = sMainID,
											Account = b.Account,
											WagersId = b.WagersId,
											GameId = b.GameId,
											WagersTime = b.WagersTime,
											BetAmount = b.BetAmount,
											Rollover = _common.GetSlotRollover(Math.Abs(b.BetAmount), b.PayoffAmount - Math.Abs(b.BetAmount)),
											PayoffTime = b.PayoffTime,
											PayoffAmount = b.PayoffAmount,
											Status = b.Status,
											SettlementTime = b.SettlementTime,
											GameCategoryId = b.GameCategoryId,
											Type = b.Type,
											CreatedDate = DateTime.Now
										};
										db.JiLiGames.AddOrUpdate(jiliGame);
									}
									catch (DbEntityValidationException e)
									{
										foreach (var eve in e.EntityValidationErrors)
										{
											_common.LogError("JiLi/GetGameRecord" + " " + DateTime.Now.ToString() + " " + string.Format("Entity of type \"{0}\" in state \"{1}\" has the following validation errors:",
															eve.Entry.Entity.GetType().Name, eve.Entry.State), P_FilePrefix);
											foreach (var ve in eve.ValidationErrors)
											{
												_common.LogError("JiLi/GetGameRecord" + " " + DateTime.Now.ToString() + " " + string.Format("- Property: \"{0}\", Error: \"{1}\"",
																ve.PropertyName, ve.ErrorMessage), P_FilePrefix);
											}
										}
									}
									catch (WebException ex)
									{
										var error = new StreamReader(ex.Response.GetResponseStream()).ReadToEnd();
										_common.LogError("Record " + b.WagersId + " " + DateTime.Now.ToString() + " " + ex.InnerException.InnerException.ToString() + " " + error, P_FilePrefix);
									}
									catch (Exception ex)
									{
										_common.LogError("Record " + b.WagersId + " " + DateTime.Now.ToString() + " " + ex.InnerException.InnerException.ToString(), P_FilePrefix);
									}
								}
								db.SaveChanges();

								if (Convert.ToInt32(result.Data.Pagination.TotalPages) > 1)
								{
									for (int x = 2; x <= Convert.ToInt32(result.Data.Pagination.TotalPages); x++)
									{
										bool IsOK = GetGameRecordExtend(x, pageLimit, mStartTime, mEndTime, sMainID);
									}
								}
							}
						}

						if (result.ErrorCode == 0)
						{
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
					_common.LogError("JiLi/GetGameRecord" + " " + DateTime.Now.ToString() + " " + string.Format("Entity of type \"{0}\" in state \"{1}\" has the following validation errors:",
									eve.Entry.Entity.GetType().Name, eve.Entry.State), P_FilePrefix);
					foreach (var ve in eve.ValidationErrors)
					{
						_common.LogError("JiLi/GetGameRecord" + " " + DateTime.Now.ToString() + " " + string.Format("- Property: \"{0}\", Error: \"{1}\"",
										ve.PropertyName, ve.ErrorMessage), P_FilePrefix);
					}
				}
				return false;
			}
			catch (WebException ex)
			{
				var responseText = new StreamReader(ex.Response.GetResponseStream()).ReadToEnd();
				_common.LogError("JiLi/GetGameRecord" + " " + DateTime.Now.ToString() + " " + ex.InnerException.InnerException.ToString() + " " + responseText, P_FilePrefix);
				return false;
			}
			catch (Exception ex)
			{
				_common.LogError("JiLi/GetGameRecord" + " " + DateTime.Now.ToString() + " " + ex.InnerException.InnerException.ToString(), P_FilePrefix);
				return false;
			}
		}

		public bool GetGameRecordExtend(int pageIndex, int pageLimit, string startTime, string endTime, string sMainID)
		{
			try
			{
				JiLi.Model.GetBetRecordByTimeResponse result = api.GetBetRecordByTime(DateTime.Parse(startTime), DateTime.Parse(endTime), pageIndex, pageLimit, true);

				using (BingoConn db = new BingoConn())
				{
					if (result.ErrorCode == 0)
					{
						if (result.Data.Result.Count > 0)
						{
							foreach (var b in result.Data.Result)
							{
								try
								{
									JiLiGame jiliGame = new JiLiGame()
									{
										fId = sMainID,
										Account = b.Account,
										WagersId = b.WagersId,
										GameId = b.GameId,
										WagersTime = b.WagersTime,
										BetAmount = b.BetAmount,
										Rollover = _common.GetSlotRollover(Math.Abs(b.BetAmount), b.PayoffAmount - Math.Abs(b.BetAmount)),
										PayoffTime = b.PayoffTime,
										PayoffAmount = b.PayoffAmount,
										Status = b.Status,
										SettlementTime = b.SettlementTime,
										GameCategoryId = b.GameCategoryId,
										Type = b.Type,
										CreatedDate = DateTime.Now
									};
									db.JiLiGames.AddOrUpdate(jiliGame);
								}
								catch (DbEntityValidationException e)
								{
									foreach (var eve in e.EntityValidationErrors)
									{
										_common.LogError("JiLi/GetGameRecord" + " " + DateTime.Now.ToString() + " " + string.Format("Entity of type \"{0}\" in state \"{1}\" has the following validation errors:",
														eve.Entry.Entity.GetType().Name, eve.Entry.State), P_FilePrefix);
										foreach (var ve in eve.ValidationErrors)
										{
											_common.LogError("JiLi/GetGameRecord" + " " + DateTime.Now.ToString() + " " + string.Format("- Property: \"{0}\", Error: \"{1}\"",
															ve.PropertyName, ve.ErrorMessage), P_FilePrefix);
										}
									}
								}
								catch (WebException ex)
								{
									var error = new StreamReader(ex.Response.GetResponseStream()).ReadToEnd();
									_common.LogError("Record " + b.WagersId + " " + DateTime.Now.ToString() + " " + ex.InnerException.InnerException.ToString() + " " + error, P_FilePrefix);
								}
								catch (Exception ex)
								{
									_common.LogError("Record " + b.WagersId + " " + DateTime.Now.ToString() + " " + ex.InnerException.InnerException.ToString(), P_FilePrefix);
								}
							}
							db.SaveChanges();
						}
					}
				}

				return true;
			}
			catch (DbEntityValidationException e)
			{
				foreach (var eve in e.EntityValidationErrors)
				{
					_common.LogError("JiLi/GetGameRecord" + " " + DateTime.Now.ToString() + " " + string.Format("Entity of type \"{0}\" in state \"{1}\" has the following validation errors:",
									eve.Entry.Entity.GetType().Name, eve.Entry.State), P_FilePrefix);
					foreach (var ve in eve.ValidationErrors)
					{
						_common.LogError("JiLi/GetGameRecord" + " " + DateTime.Now.ToString() + " " + string.Format("- Property: \"{0}\", Error: \"{1}\"",
										ve.PropertyName, ve.ErrorMessage), P_FilePrefix);
					}
				}
				return false;
			}
			catch (WebException ex)
			{
				var responseText = new StreamReader(ex.Response.GetResponseStream()).ReadToEnd();
				_common.LogError("JiLi/GetGameRecord" + " " + DateTime.Now.ToString() + " " + ex.InnerException.InnerException.ToString() + " " + responseText, P_FilePrefix);
				return false;
			}
			catch (Exception ex)
			{
				_common.LogError("JiLi/GetGameRecord" + " " + DateTime.Now.ToString() + " " + ex.InnerException.InnerException.ToString(), P_FilePrefix);
				return false;
			}
		}
	}
}