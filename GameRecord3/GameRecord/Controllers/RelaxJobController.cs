using System;
using System.Linq;
using System.Web.Mvc;
using System.Net;
using System.IO;
using GameRecord.Models;
using System.Data.Entity.Migrations;
using System.Data.Entity.Validation;
using static GameRecord.Controllers.BaseConnector;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Dynamic;
using System.Web.Script.Serialization;
using System.Web;
using GameRecord.Gamess.Relax;

namespace GameRecord.Controllers
{
	public class RelaxJobController : Controller
	{
		public static readonly object MyLock = new object();
		private string P_GamePrefix = "RELAX";

		private string P_FilePrefix = "RELAX";
		private string Currency = "MYR";
		private string P_DTFormat = "yyyy-MM-ddTHH:mm:ss.fffZ";
		Common _common = new Common();
		private RelaxConnector api = new RelaxConnector(Environment: BaseConnector.GameEnvironment.PROD);

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

				//		bingoConn.Entry(xyz).State = System.Data.Entity.EntityState.Modified;
				//		bingoConn.SaveChanges();
				//	}
				//}
				return true;
			}
			catch (Exception ex)
			{
				_common.LogError("Relax/UpdJobTime" + " " + DateTime.Now.ToString() + "  " + ex.InnerException.InnerException.ToString(), P_FilePrefix);
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
						DateTime dtSTime = Convert.ToDateTime(DateTime.Now.ToString("yyyy-MM-dd HH:mm")) - new TimeSpan(0, 8, 0, 0, 0);
						sTime = dtSTime.ToString("yyyy-MM-dd HH:mm:ss");
					}
				}

				return sTime;

			}
			catch (Exception ex)
			{
				_common.LogError("Relax/GetJobTime" + " " + DateTime.Now.ToString() + "  " + ex.InnerException.InnerException.ToString(), P_FilePrefix);
				return "Error:" + ex.InnerException.InnerException.ToString();
			}
		}

		//using
		public void RelaxJob()
		{
			try
			{
				lock (MyLock)
				{
					api = new RelaxConnector(P_GamePrefix, Environment: GameEnvironment.PROD);
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
			try
			{
				string startTime = GetJobTime("Start");
				string endTime = GetJobTime("End");
				string mStartTime = startTime;
				string mEndTime = endTime;

				int limit = 120; // 24 hours
				int pageSize = 5000;
				int pageIndex = 1;

				int MinDiff = Convert.ToInt32((Convert.ToDateTime(endTime) - Convert.ToDateTime(startTime)).TotalMinutes);
				int times = (MinDiff / limit) + 1;
				for (int j = limit; j <= times * limit; j += limit)
				{
					mStartTime = j == limit ? startTime : mEndTime;

					if ((Convert.ToDateTime(mStartTime).AddMinutes(limit) - Convert.ToDateTime(endTime)).TotalMinutes > 0)
						mEndTime = endTime;
					else
						mEndTime = Convert.ToDateTime(mStartTime).AddMinutes(limit).ToString("yyyy-MM-dd HH:mm:ss");

					ReportResponse result = api.RetrieveTransactionByMinute(mStartTime, mEndTime, Currency, pageIndex, pageSize);
					using (BingoConn db = new BingoConn())
					{
						string sMainID = Guid.NewGuid().ToString();
						RelaxGameMain RelaxMain = new RelaxGameMain()
						{
							Id = sMainID,
							Total = 0,
							PageIndex = 0,
							PageSize = pageSize,
							StartTime = Convert.ToDateTime(mStartTime),
							EndTime = Convert.ToDateTime(mEndTime),
							CreatedDate = DateTime.Now
						};
						db.RelaxGameMains.Add(RelaxMain);
						db.SaveChanges();

						if (result != null)
						{
							if (result.data != null)
							{
								if (result.data.Count > 0)
								{
									foreach (var b in result.data)
									{
										try
										{
											var record = db.RelaxGames.Where(x => x.wager_id == b.wager_id && x.wager_type == b.wager_type).FirstOrDefault();
											if (record == null)
											{
												RelaxGame RelaxGame = new RelaxGame()
												{
													fId = sMainID,
													brand_uid = b.brand_uid,
													currency = b.currency,
													wager_type = b.wager_type,
													amount = b.amount,
													before_amount = b.before_amount,
													after_amount = b.after_amount,
													game_id = b.game_id,
													game_name = b.game_name,
													round_id = b.round_id,
													wager_id = b.wager_id,
													jackpot_contribution = b.jackpot_contribution,
													jackpot_win = b.jackpot_win,
													description = b.description,
													game_result = b.game_result,
													tip = b.tip,
													create_time = Convert.ToDateTime(b.create_time),
													CreatedDate = DateTime.Now
												};
												db.RelaxGames.Add(RelaxGame);
											}

											//var record = db.RelaxGames.Where(x => x.wager_id == b.wager_id && x.wager_type == b.wager_type).FirstOrDefault();

											//if (record == null)
											//{
											//	RelaxGame.CreatedDate = DateTime.Now;
											//}
											//else
											//{
											//	RelaxGame.CreatedDate = record.CreatedDate;
											//	RelaxGame.ModifiedDate = DateTime.Now;
											//}
										}
										catch (DbEntityValidationException e)
										{
											foreach (var eve in e.EntityValidationErrors)
											{
												_common.LogError("Relax/GetGameRecord" + " " + DateTime.Now.ToString() + " " + string.Format("Entity of type \"{0}\" in state \"{1}\" has the following validation errors:",
																												eve.Entry.Entity.GetType().Name, eve.Entry.State), P_FilePrefix);
												foreach (var ve in eve.ValidationErrors)
												{
													_common.LogError("Relax/GetGameRecord" + " " + DateTime.Now.ToString() + " " + string.Format("- Property: \"{0}\", Error: \"{1}\"",
																													ve.PropertyName, ve.ErrorMessage), P_FilePrefix);
												}
											}
											throw;
										}
										catch (WebException ex)
										{
											var responseText = new StreamReader(ex.Response.GetResponseStream()).ReadToEnd();
											_common.LogError("Relax/GetGameRecord" + " " + DateTime.Now.ToString() + " " + ex.InnerException.InnerException.ToString() + " " + responseText, P_FilePrefix);
											throw;
										}
										catch (Exception ex)
										{
											_common.LogError("Relax/GetGameRecord" + " " + DateTime.Now.ToString() + " " + ex.InnerException.InnerException.ToString(), P_FilePrefix);
											throw;
										}
									}
									db.SaveChanges();

									decimal dCnt = Convert.ToDecimal(result.page.total_count) / Convert.ToDecimal(pageSize);
									int iPageIndex = Int32.Parse(Math.Ceiling(dCnt).ToString());

									if (iPageIndex > 1)
									{
										for (int x = 2; x <= iPageIndex; x++)
										{
											bool IsOK = GetGameRecordExtend(x, pageSize, mStartTime, mEndTime, Currency, sMainID);
										}
									}
								}
							}

							if (result.code == "5042" || result.code == "1000")
							{
								bool isUpdJob = UpdJobTime(mStartTime, mEndTime, sMainID);
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
					_common.LogError("Relax/GetGameRecord" + " " + DateTime.Now.ToString() + " " + string.Format("Entity of type \"{0}\" in state \"{1}\" has the following validation errors:",
													eve.Entry.Entity.GetType().Name, eve.Entry.State), P_FilePrefix);
					foreach (var ve in eve.ValidationErrors)
					{
						_common.LogError("Relax/GetGameRecord" + " " + DateTime.Now.ToString() + " " + string.Format("- Property: \"{0}\", Error: \"{1}\"",
														ve.PropertyName, ve.ErrorMessage), P_FilePrefix);
					}
				}
				return false;
			}
			catch (WebException ex)
			{
				var responseText = new StreamReader(ex.Response.GetResponseStream()).ReadToEnd();
				_common.LogError("Relax/GetGameRecord" + " " + DateTime.Now.ToString() + " " + ex.InnerException.InnerException.ToString() + " " + responseText, P_FilePrefix);
				return false;
			}
			catch (Exception ex)
			{
				_common.LogError("Relax/GetGameRecord" + " " + DateTime.Now.ToString() + " " + ex.InnerException.InnerException.ToString(), P_FilePrefix);
				return false;
			}

		}

		public bool GetGameRecordExtend(int pageIndex, int pageSize, string startTime, string endTime, string currency, string sMainID)
		{
			try
			{
				ReportResponse result = api.RetrieveTransactionByMinute(startTime, endTime, currency, pageIndex, pageSize);

				using (BingoConn db = new BingoConn())
				{
					if (result != null)
					{
						if (result.data.Count > 0)
						{
							foreach (var b in result.data)
							{
								try
								{
									var record = db.RelaxGames.Where(x => x.wager_id == b.wager_id && x.wager_type == b.wager_type).FirstOrDefault();

									if (record == null)
									{
										RelaxGame RelaxGame = new RelaxGame()
										{
											fId = sMainID,
											brand_uid = b.brand_uid,
											currency = b.currency,
											wager_type = b.wager_type,
											amount = b.amount,
											before_amount = b.before_amount,
											after_amount = b.after_amount,
											game_id = b.game_id,
											game_name = b.game_name,
											round_id = b.round_id,
											wager_id = b.wager_id,
											jackpot_contribution = b.jackpot_contribution,
											jackpot_win = b.jackpot_win,
											description = b.description,
											game_result = b.game_result,
											tip = b.tip,
											create_time = Convert.ToDateTime(b.create_time),
											CreatedDate = DateTime.Now
										};
										db.RelaxGames.Add(RelaxGame);
									}

									//if (record == null)
									//{
									//	RelaxGame.CreatedDate = DateTime.Now;
									//}
									//else
									//{
									//	RelaxGame.CreatedDate = record.CreatedDate;
									//	RelaxGame.ModifiedDate = DateTime.Now;
									//}
								}
								catch (DbEntityValidationException e)
								{
									foreach (var eve in e.EntityValidationErrors)
									{
										_common.LogError("Relax/GetGameRecord" + " " + DateTime.Now.ToString() + " " + string.Format("Entity of type \"{0}\" in state \"{1}\" has the following validation errors:",
																										eve.Entry.Entity.GetType().Name, eve.Entry.State), P_FilePrefix);
										foreach (var ve in eve.ValidationErrors)
										{
											_common.LogError("Relax/GetGameRecord" + " " + DateTime.Now.ToString() + " " + string.Format("- Property: \"{0}\", Error: \"{1}\"",
																											ve.PropertyName, ve.ErrorMessage), P_FilePrefix);
										}
									}
									throw;
								}
								catch (WebException ex)
								{
									var responseText = new StreamReader(ex.Response.GetResponseStream()).ReadToEnd();
									_common.LogError("Relax/GetGameRecord" + " " + DateTime.Now.ToString() + " " + ex.InnerException.InnerException.ToString() + " " + responseText, P_FilePrefix);
									throw;
								}
								catch (Exception ex)
								{
									_common.LogError("Relax/GetGameRecord" + " " + DateTime.Now.ToString() + " " + ex.InnerException.InnerException.ToString(), P_FilePrefix);
									throw;
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
					_common.LogError("Relax/GetGameRecord" + " " + DateTime.Now.ToString() + " " + string.Format("Entity of type \"{0}\" in state \"{1}\" has the following validation errors:",
													eve.Entry.Entity.GetType().Name, eve.Entry.State), P_FilePrefix);
					foreach (var ve in eve.ValidationErrors)
					{
						_common.LogError("Relax/GetGameRecord" + " " + DateTime.Now.ToString() + " " + string.Format("- Property: \"{0}\", Error: \"{1}\"",
														ve.PropertyName, ve.ErrorMessage), P_FilePrefix);
					}
				}
				return false;
			}
			catch (WebException ex)
			{
				var responseText = new StreamReader(ex.Response.GetResponseStream()).ReadToEnd();
				_common.LogError("Relax/GetGameRecord" + " " + DateTime.Now.ToString() + " " + ex.InnerException.InnerException.ToString() + " " + responseText, P_FilePrefix);
				return false;
			}
			catch (Exception ex)
			{
				_common.LogError("Relax/GetGameRecord" + " " + DateTime.Now.ToString() + " " + ex.InnerException.InnerException.ToString(), P_FilePrefix);
				return false;
			}
		}
	}
}