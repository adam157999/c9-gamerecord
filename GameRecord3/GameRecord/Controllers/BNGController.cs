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
using GameRecord.Gamess.BNG;

namespace GameRecord.Controllers
{
	public class BNGController : Controller
	{
		public static readonly object MyLock = new object();
		private string P_GamePrefix = "BNG";
		private string P_DTFormat = "yyyy-MM-ddTHH:mm:ss.fffZ";
		Common _common = new Common();
		private BNGConnector api = new BNGConnector(Environment: BaseConnector.GameEnvironment.PROD);

		public bool UpdJobTime(string startTime, string endTime, string sMainID)
		{
			try
			{
				DateTime dtEnd = Convert.ToDateTime(endTime);
				_common.UpdateADMJobDate(P_GamePrefix, dtEnd, "");

				return true;
			}
			catch (Exception ex)
			{
				_common.LogError("BNGGame/UpdJobTime: " + ex.Message.ToString(), P_GamePrefix);
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
						DateTime dtSTime = Convert.ToDateTime(DateTime.Now.ToString("yyyy-MM-dd HH:mm")) - new TimeSpan(0, 8, 5, 0, 0);
						sTime = dtSTime.ToString("yyyy-MM-dd HH:mm:ss");
					}
				}

				return sTime;
			}
			catch (Exception ex)
			{
				_common.LogError("BNGGame/GetJobTime: " + ex.Message.ToString(), P_GamePrefix);
				return "Error:" + ex.Message.ToString();
			}
		}

		public void BNGJob()
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
				_common.LogError("BNGJob: " + ex.Message.ToString(), P_GamePrefix);
			}
		}

		public bool GetGameRecord()
		{
			string startTime = GetJobTime("Start");
			string endTime = GetJobTime("End");
			string mStartTime = startTime;
			string mEndTime = endTime;
			int limit = 30; // 24 hours
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

					transactionListResponse result = api.RetrieveTransactionByMinute(mStartTime, mEndTime);

					using (BingoConn db = new BingoConn())
					{
						string sMainID = Guid.NewGuid().ToString();
						BNGGameMain BNGMain = new BNGGameMain()
						{
							Id = sMainID,
							Total = null,
							StartTime = Convert.ToDateTime(startTime),
							EndTime = Convert.ToDateTime(endTime),
							CreatedDate = DateTime.Now
						};
						db.BNGGameMains.Add(BNGMain);
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
											BNGGame BNGGame = new BNGGame()
											{
												fId = sMainID,
												transaction_id = b.transaction_id,
												original_transaction_id = b.original_transaction_id,
												type = b.type,
												player_id = b.player_id,
												brand = b.brand,
												currency = b.currency,
												mode = b.mode,
												is_test = b.is_test,
												platform = b.platform,
												tag = b.tag,
												round_id = b.round_id,
												round_started = b.round_started,
												round_finished = b.round_finished,
												bet = Convert.ToDecimal(b.bet),
												win = Convert.ToDecimal(b.win),
												balance_after = Convert.ToDecimal(b.balance_after),
												auto_paid = b.auto_paid,
												provider_id = b.provider_id,
												provider_name = b.provider_name,
												game_id = b.game_id,
												game_name = b.game_name,
												status = b.status,
												manual_confirmed = b.manual_confirmed,
												c_at = Convert.ToDateTime(b.c_at),
												bonus_id = b.bonus_id,
												campaign = b.campaign,
												bonus_source = b.bonus_source,
												ext_bonus_id = b.ext_bonus_id,
												bonus_event = b.bonus_event,
												bonus_type = b.bonus_type,
												CreatedDate = DateTime.Now
											};
											db.BNGGames.AddOrUpdate(BNGGame);
										}
										catch (DbEntityValidationException e)
										{
											foreach (var eve in e.EntityValidationErrors)
											{
												_common.LogError("BNG/GetGameRecord: " + string.Format("Entity of type \"{0}\" in state \"{1}\" has the following validation errors:", eve.Entry.Entity.GetType().Name, eve.Entry.State), P_GamePrefix);
												foreach (var ve in eve.ValidationErrors)
												{
													_common.LogError("BNG/GetGameRecord: " + string.Format("- Property: \"{0}\", Error: \"{1}\"", ve.PropertyName, ve.ErrorMessage), P_GamePrefix);
												}
											}
										}
										catch (WebException ex)
										{
											var error = new StreamReader(ex.Response.GetResponseStream()).ReadToEnd();
											_common.LogError("Record " + b.transaction_id + " " + DateTime.Now.ToString() + " " + ex.Message.ToString() + " " + error, P_GamePrefix);
										}
										catch (Exception ex)
										{
											_common.LogError("Record " + b.transaction_id + " " + DateTime.Now.ToString() + " " + ex.Message.ToString(), P_GamePrefix);
										}
									}
									db.SaveChanges();
								}
							}

							bool isUpdJob = UpdJobTime(mStartTime, mEndTime, sMainID);
						}
					}
				}
				return true;
			}
			catch (Exception ex)
			{
				_common.LogError("BNGGame/GetGameRecord: " + ex.Message.ToString(), P_GamePrefix);
				return false;
			}
		}

		//new
		//http://localhost:51148/BNG/BNGJobNew
		public string BNGJobNew()
		{
			string startTime = GetJobTime("Start");
			string endTime = GetJobTime("End");
			string mStartTime = startTime;
			string mEndTime = endTime;
			int limit = 30; // 24 hours
			int iProcessed = 0;

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

					transactionListResponse result = api.RetrieveTransactionByMinute(mStartTime, mEndTime);

					using (BingoConn db = new BingoConn())
					{
						string sMainID = Guid.NewGuid().ToString();
						string s = "insert into BNGGameMain (Id,StartTime,EndTime,CreatedDate) ";
						s += "values ('" + sMainID + "','" + startTime + "','" + endTime + "',getdate());";
						_common.Sql_RunAndCommit(s);

						if (result != null)
						{
							if (result.items != null && result.items.Count > 0)
							{
								foreach (var b in result.items)
								{
									try
									{
										string s2 = "insert into BNGGame (fId,transaction_id,original_transaction_id,[type],player_id,brand,currency,mode,is_test,[platform],tag,round_id,round_started,round_finished,bet,win,balance_after,auto_paid,provider_id,provider_name,game_id,game_name,[status],manual_confirmed,c_at,bonus_id,campaign,bonus_source,ext_bonus_id,bonus_event,bonus_type,CreatedDate) ";
										s2 += "select * from (values ('" + sMainID + "','" + b.transaction_id + "'," + _common.F1(b.original_transaction_id) + "," + _common.F1(b.type) + "," + _common.F1(b.player_id) + "," + _common.F1(b.brand) + "," + _common.F1(b.currency) + "," + _common.F1(b.mode) + "," + (b.is_test ? "1" : "0") + "," + _common.F1(b.platform) + "," + _common.F1(b.tag) + "," + b.round_id + "," + (b.round_started ? "1" : "0") + "," + (b.round_finished ? "1" : "0") + "," + b.bet + "," + b.win + "," + b.balance_after + "," + (b.auto_paid ? "1" : "0") + "," + _common.F1(b.provider_id) + "," + _common.F1(b.provider_name) + "," + _common.F1(b.game_id) + "," + _common.F1(b.game_name) + "," + _common.F1(b.status) + "," + (b.manual_confirmed ? "1" : "0") + ",'" + Convert.ToDateTime(b.c_at).ToString("yyyy-MM-dd HH:mm:ss") + "'," + b.bonus_id + "," + _common.F1(b.campaign) + "," + _common.F1(b.bonus_source) + "," + _common.F1(b.ext_bonus_id) + "," + _common.F1(b.bonus_event) + "," + _common.F1(b.bonus_type) + ",getdate())) ";
										s2 += "as a(fId,transaction_id,original_transaction_id,[type],player_id,brand,currency,mode,is_test,[platform],tag,round_id,round_started,round_finished,bet,win,balance_after,auto_paid,provider_id,provider_name,game_id,game_name,[status],manual_confirmed,c_at,bonus_id,campaign,bonus_source,ext_bonus_id,bonus_event,bonus_type,CreatedDate) ";
										s2 += "where not exists (select * from BNGGame b with (updlock) where b.transaction_id=a.transaction_id);";
										_common.Sql_RunAndCommit(s2);
										iProcessed++;
									}
									catch (Exception ex)
									{
										_common.LogError("Record: " + b.transaction_id + " " + ex.Message.ToString(), P_GamePrefix);
									}
								}
								db.SaveChanges();
							}

							bool isUpdJob = UpdJobTime(mStartTime, mEndTime, sMainID);
						}
					}
				}

				return "BNGJobNew has processed " + iProcessed + " record(s) from " + startTime + " until " + endTime;
			}
			catch (Exception ex)
			{
				_common.LogError("Error at BNG/BNGJobNew: " + ex.Message.ToString(), P_GamePrefix);
				return "Error at BNG/BNGJobNew: " + ex.Message.ToString();
			}
		}
	}
}