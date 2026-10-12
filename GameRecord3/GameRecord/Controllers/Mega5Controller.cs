using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Net;
using System.Net.Http;
using System.IO;
using Newtonsoft.Json;
using System.Text;
using System.Security.Cryptography;
using System.Data.Entity.Migrations;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using System.Configuration;
using System.Data.Entity.Validation;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium;
using Newtonsoft.Json.Linq;
using System.Dynamic;
using OpenQA.Selenium.Firefox;
using System.Diagnostics;
using GameRecord.Models;

namespace GameRecord.Controllers
{
	public class Mega5Controller : Controller
	{
		public static readonly object MyLock = new object();
		//private string P_ApiUrl = "http://mgt3.36ozhushou.com/mega-cloud/api/";
		//private string P_SecretKey = "KOI9iMnTl3H72Nr1hqfza6JyC1o=";
		//private string P_AgentId = "Mega1-1303";
		//private string P_AgentPwd = "0524Bt0524";
		//private string P_sn = "ld00";
		private string P_GamePrefix = "MEGA5";
		private string P_FilePrefix = "MEGA5";
		private int P_TimeoutSec = 120000; //2 mins
		Common _common = new Common();
		bool isNextPage = false;
		bool isError = false;
		int page = 1;
		private MEGAConnector api = new MEGAConnector(Environment: BaseConnector.GameEnvironment.PROD);

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
				_common.LogError("Mega/UpdJobTime" + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), P_FilePrefix);
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
						DateTime dtSTime = Convert.ToDateTime(query.LastRunTillDate?.ToString("yyyy-MM-dd HH:mm"));
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
				_common.LogError("Mega/GetJobTime" + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), P_FilePrefix);
				return "Error:" + ex.Message.ToString();
			}
		}

		public bool GetPlayerList()
		{
			string startTime = GetJobTime("Start");
			string endTime = GetJobTime("End");
			//string startDate = startTime;
			string startDate = Convert.ToDateTime(startTime).AddMinutes(-30).ToString("yyyy-MM-dd HH:mm:ss");
			string endDate = endTime;
			TimeSpan _1200AM = new TimeSpan(0, 0, 0, 0, 0);
			TimeSpan _1215AM = new TimeSpan(0, 0, 15, 0, 0);
			int limit = 30;


			startDate = Convert.ToDateTime(startDate).ToString("yyyy-MM-dd") + " 00:00:00";
			endDate = Convert.ToDateTime(startDate).ToString("yyyy-MM-dd") + " 23:59:59";

			try
			{
				MEGA.PlayerTotalReportResponse result = api.PlayerTotalReport("1", DateTime.Parse(startDate), DateTime.Parse(endDate));

				using (BingoConn db = new BingoConn())
				{
					string sMainID = Guid.NewGuid().ToString();
					MegaGameMain megaGameMain = new MegaGameMain()
					{
						Id = sMainID,
						Code = null,
						Msg = result.error != null ? result.error.message : null,
						StartTime = Convert.ToDateTime(startTime),
						EndTime = Convert.ToDateTime(endTime),
						CreatedDate = DateTime.Now
					};
					db.MegaGameMains.Add(megaGameMain);
					db.SaveChanges();

					if (result.error == null)
					{
						if (result.result != null)
						{
							foreach (var p in result.result)
							{
								string pStartTime = Convert.ToDateTime(startTime).AddMinutes(-30).ToString("yyyy-MM-dd HH:mm:ss");
								string pEndTime = Convert.ToDateTime(endTime).ToString("yyyy-MM-dd HH:mm:ss");
								string mStartTime = pStartTime;
								string mEndTime = pEndTime;

								while (DateTime.Compare(Convert.ToDateTime(pEndTime), Convert.ToDateTime(mStartTime)) > 0)
								{
									if (Convert.ToDateTime(mStartTime).ToString("yyyy-MM-dd") != Convert.ToDateTime(mEndTime).ToString("yyyy-MM-dd"))
									{
										mEndTime = Convert.ToDateTime(mStartTime).ToString("yyyy-MM-dd") + " 23:59:59";
									}
									else
									{
										if (Convert.ToDateTime(mStartTime).ToString("HH:mm:ss") == "23:59:59")
											mStartTime = Convert.ToDateTime(mStartTime).AddDays(1).ToString("yyyy-MM-dd") + " 00:00:00";
										else
											mStartTime = Convert.ToDateTime(mStartTime).ToString("yyyy-MM-dd") + " 00:00:00";

										if ((Convert.ToDateTime(Convert.ToDateTime(mStartTime).ToString("yyyy-MM-dd 23:59:59")) - Convert.ToDateTime(pEndTime)).TotalMinutes > 0)
											mEndTime = pEndTime;
										else
											mEndTime = Convert.ToDateTime(mStartTime).ToString("yyyy-MM-dd 23:59:59");
									}

									GetGameRecord(Convert.ToString(p.loginId), mStartTime, mEndTime, sMainID);

									mStartTime = mEndTime;
								}
							}

							if (!isError)
							{
								//_common.LogError(startTime + " " + endTime, P_FilePrefix);
								bool isUpdJob = UpdJobTime(startTime, endTime, sMainID);
							}
						}
					}
				}

				return true;
			}
			catch (Exception ex)
			{
				_common.LogError("Mega/GetPlayerList" + " " + DateTime.Now.ToString() + "  " + ex.InnerException.InnerException.ToString(), P_FilePrefix);
				isError = true;
				return false;
			}
		}

		public bool GetGameRecord(string loginId, string startTime, string endTime, string sMainID)
		{
			int pageSize = 2000;
			int pageIndex = 1;

			try
			{
				using (BingoConn db = new BingoConn())
				{
					MEGA.GameRecordQueryResponse result = api.GameRecordQuery(loginId, DateTime.Parse(startTime), DateTime.Parse(endTime), pageIndex, pageSize);

					//_common.LogError(JsonConvert.SerializeObject(result), P_FilePrefix);

					if (result.error == null)
					{
						if (result.result != null)
						{
							foreach (var b in result.result.items)
							{
								try
								{
									if (!Convert.ToString(b.tableId).ToUpper().StartsWith("SET SCORE"))
									{
										MegaGame megaGame = new MegaGame()
										{
											fId = sMainID,
											Id = b.id,
											UserName = loginId,
											GameId = b.gameId,
											GameName = b.gameName,
											RoundNo = b.roundNo,
											TableID = b.tableId,
											ClassID = b.classId,
											LogDataType = b.logDataType,
											MarkID = b.markId,
											Bet = b.bet,
											Win = b.win,
											Rollover = _common.GetSlotRollover(Math.Abs(b.bet), b.win - Math.Abs(b.bet)),
											BeginBalance = b.beginBalance,
											EndBalance = b.endBalance,
											LineNum = b.lineNum,
											CreateTime = b.createTime,
											//CreatedDate = DateTime.Now
										};
										var record = db.MegaGames.Where(x => x.UserName == loginId && x.CreateTime == megaGame.CreateTime && x.Bet == megaGame.Bet
																										&& x.Win == megaGame.Win && x.BeginBalance == megaGame.BeginBalance && x.EndBalance == megaGame.EndBalance).FirstOrDefault();
										//var record = db.MegaGames.Where(x => x.Id == megaGame.Id && x.UserName == loginId && x.CreateTime == megaGame.CreateTime && x.Bet == megaGame.Bet && x.Win == megaGame.Win).FirstOrDefault();
										if (record == null)
										{
											megaGame.CreatedDate = DateTime.Now;
										}
										else
										{
											megaGame.CreatedDate = record.CreatedDate;
											megaGame.ModifiedDate = DateTime.Now;
										}
										db.MegaGames.AddOrUpdate(megaGame);
									}
								}
								catch (DbEntityValidationException e)
								{
									foreach (var eve in e.EntityValidationErrors)
									{
										_common.LogError("MEGA/GetGameRecord" + " " + DateTime.Now.ToString() + " " + string.Format("Entity of type \"{0}\" in state \"{1}\" has the following validation errors:",
																		eve.Entry.Entity.GetType().Name, eve.Entry.State), P_FilePrefix);
										foreach (var ve in eve.ValidationErrors)
										{
											_common.LogError("MEGA/GetGameRecord" + " " + DateTime.Now.ToString() + " " + string.Format("- Property: \"{0}\", Error: \"{1}\"",
																			ve.PropertyName, ve.ErrorMessage), P_FilePrefix);
										}
									}
									throw;
								}
								catch (WebException ex)
								{
									var responseText = new StreamReader(ex.Response.GetResponseStream()).ReadToEnd();
									_common.LogError("MEGA/GetGameRecord" + " " + DateTime.Now.ToString() + " " + ex.InnerException.InnerException.ToString() + " " + responseText, P_FilePrefix);
									throw;
								}
								catch (Exception ex)
								{
									_common.LogError("MEGA/GetGameRecord" + " " + DateTime.Now.ToString() + " " + ex.InnerException.InnerException.ToString(), P_FilePrefix);
									throw;
								}
							}
							db.SaveChanges();

							if (Convert.ToInt32(result.result.totalPage) > 1)
							{
								for (int x = 2; x <= Convert.ToInt32(result.result.totalPage); x++)
								{
									bool IsOK = GetGameRecordExtend(loginId, startTime, endTime, sMainID, x, pageSize);
								}
							}
						}
					}

					return true;
				}
			}
			catch (Exception ex)
			{
				_common.LogError("Mega/GetGameRecord" + " " + DateTime.Now.ToString() + "  " + ex.InnerException.InnerException.ToString(), P_FilePrefix);
				isError = true;
				return false;
			}
		}

		public bool GetGameRecordExtend(string loginId, string startTime, string endTime, string sMainID, int pageIndex, int pageSize)
		{
			try
			{
				using (BingoConn db = new BingoConn())
				{
					MEGA.GameRecordQueryResponse result = api.GameRecordQuery(loginId, DateTime.Parse(startTime), DateTime.Parse(endTime), pageIndex, pageSize);

					//_common.LogError(JsonConvert.SerializeObject(result), P_FilePrefix);

					if (result.error == null)
					{
						if (result.result != null)
						{
							foreach (var b in result.result.items)
							{
								try
								{
									if (!Convert.ToString(b.tableId).ToUpper().StartsWith("SET SCORE"))
									{
										MegaGame megaGame = new MegaGame()
										{
											fId = sMainID,
											Id = b.id,
											UserName = loginId,
											GameId = b.gameId,
											GameName = b.gameName,
											RoundNo = b.roundNo,
											TableID = b.tableId,
											ClassID = b.classId,
											LogDataType = b.logDataType,
											MarkID = b.markId,
											Bet = b.bet,
											Win = b.win,
											Rollover = _common.GetSlotRollover(Math.Abs(b.bet), b.win - Math.Abs(b.bet)),
											BeginBalance = b.beginBalance,
											EndBalance = b.endBalance,
											LineNum = b.lineNum,
											CreateTime = b.createTime,
											//CreatedDate = DateTime.Now
										};
										var record = db.MegaGames.Where(x => x.UserName == loginId && x.CreateTime == megaGame.CreateTime && x.Bet == megaGame.Bet
																											&& x.Win == megaGame.Win && x.BeginBalance == megaGame.BeginBalance && x.EndBalance == megaGame.EndBalance).FirstOrDefault();
										//var record = db.MegaGames.Where(x => x.Id == megaGame.Id && x.UserName == loginId && x.CreateTime == megaGame.CreateTime && x.Bet == megaGame.Bet && x.Win == megaGame.Win).FirstOrDefault();
										if (record == null)
										{
											megaGame.CreatedDate = DateTime.Now;
										}
										else
										{
											megaGame.CreatedDate = record.CreatedDate;
											megaGame.ModifiedDate = DateTime.Now;
										}
										db.MegaGames.AddOrUpdate(megaGame);
									}
								}
								catch (DbEntityValidationException e)
								{
									foreach (var eve in e.EntityValidationErrors)
									{
										_common.LogError("MEGA/GetGameRecord" + " " + DateTime.Now.ToString() + " " + string.Format("Entity of type \"{0}\" in state \"{1}\" has the following validation errors:",
																		eve.Entry.Entity.GetType().Name, eve.Entry.State), P_FilePrefix);
										foreach (var ve in eve.ValidationErrors)
										{
											_common.LogError("MEGA/GetGameRecord" + " " + DateTime.Now.ToString() + " " + string.Format("- Property: \"{0}\", Error: \"{1}\"",
																			ve.PropertyName, ve.ErrorMessage), P_FilePrefix);
										}
									}
									throw;
								}
								catch (WebException ex)
								{
									var responseText = new StreamReader(ex.Response.GetResponseStream()).ReadToEnd();
									_common.LogError("MEGA/GetGameRecord" + " " + DateTime.Now.ToString() + " " + ex.InnerException.InnerException.ToString() + " " + responseText, P_FilePrefix);
									throw;
								}
								catch (Exception ex)
								{
									_common.LogError("MEGA/GetGameRecordExtend" + " " + DateTime.Now.ToString() + " " + ex.InnerException.InnerException.ToString(), P_FilePrefix);
									throw;
								}
							}
							db.SaveChanges();
						}
					}

					return true;
				}

			}
			catch (Exception ex)
			{
				_common.LogError("Mega/GetGameRecordExtend" + " " + DateTime.Now.ToString() + "  " + ex.InnerException.InnerException.ToString(), P_FilePrefix);
				isError = true;
				return false;
			}
		}

		public void MegaJob()
		{
			try
			{
				using (BingoConn db = new BingoConn())
				{
					var list = db.MEGAPlayerReruns.Where(x => x.Job == P_GamePrefix && x.Finish == false && x.IsRunning == false).Take(3).ToList();

					list.ForEach(q =>
					{
						q.IsRunning = true;
					});

					string sMainID = Guid.NewGuid().ToString();
					MegaGameMain megaGameMain = new MegaGameMain()
					{
						Id = sMainID,
						Code = null,
						Msg = null,
						StartTime = null,
						EndTime = null,
						CreatedDate = DateTime.Now
					};
					db.MegaGameMains.Add(megaGameMain);
					db.SaveChanges();
					foreach (var i in list)
					{
						GetGameRecord(i.PlayerID.Trim(), Convert.ToDateTime(i.StartDt).ToString("yyyy-MM-dd HH:mm:ss"), Convert.ToDateTime(i.EndDt).ToString("yyyy-MM-dd HH:mm:ss"), sMainID);
						i.IsRunning = false;
						i.Finish = true;
						db.SaveChanges();
					}
				}
			}
			catch (Exception ex)
			{
				_common.LogError("Start - " + DateTime.Now.ToString() + "  " + ex.InnerException.InnerException.ToString(), P_FilePrefix);
			}
		}

	}
}