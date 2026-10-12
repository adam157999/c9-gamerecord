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
	public class LionKingController : Controller
	{
		public static readonly object MyLock = new object();
		private string P_GamePrefix = "LIONKING";

		private string P_FilePrefix = "LIONKING";
		private string P_DTFormat = "yyyy-MM-ddTHH:mm:ss.fffZ";
		Common _common = new Common();
		private LionKingConnector api = null;

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
				_common.LogError("LionKing/UpdJobTime" + " " + DateTime.Now.ToString() + "  " + ex.InnerException.InnerException.ToString(), P_FilePrefix);
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
						DateTime dtSTime = Convert.ToDateTime(DateTime.Now.ToString("yyyy-MM-dd HH:mm")) - new TimeSpan(0, 0, 0, 0, 0);
						sTime = dtSTime.ToString("yyyy-MM-dd HH:mm:ss");
					}
				}

				return sTime;

			}
			catch (Exception ex)
			{
				_common.LogError("LionKing/GetJobTime" + " " + DateTime.Now.ToString() + "  " + ex.InnerException.InnerException.ToString(), P_FilePrefix);
				return "Error:" + ex.InnerException.InnerException.ToString();
			}
		}

		public void LionKingJob(string From = "", string To = "")
		{
			try
			{
				lock (MyLock)
				{
					//if (!string.IsNullOrEmpty(From) && !string.IsNullOrEmpty(To))
					//	_common.LogError("LionKing/GetGameRecord" + " " + DateTime.Now.ToString() + " " + From + " " + To, P_FilePrefix);
					api = new LionKingConnector(P_GamePrefix, Environment: GameEnvironment.PROD);
					bool isOK = GetGameRecord(From, To);

				}
			}
			catch (Exception ex)
			{
				_common.LogError("Start - " + DateTime.Now.ToString() + "  " + ex.InnerException.InnerException.ToString(), P_FilePrefix);
			}
		}

		public bool GetGameRecord(string From, string To)
		{
			string startTime = (!string.IsNullOrEmpty(From) ? From : GetJobTime("Start"));
			string endTime = (!string.IsNullOrEmpty(To) ? To : GetJobTime("End"));
			string mStartTime = startTime;
			string mEndTime = endTime;

			int limit = 120; // 24 hours
			int pageSize = 2000;
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

				LK_L365.Model.GetGameRecordByTimeResponseObject result = api.GetBrandCompletedGameResultsV2(mStartTime, mEndTime, pageIndex, pageSize);

				using (BingoConn db = new BingoConn())
				{
					string sMainID = Guid.NewGuid().ToString();
					LionKingGameMain LionKingMain = new LionKingGameMain()
					{
						Id = sMainID,
						Total = result.TotalRows,
						PageIndex = result.CurrentPage,
						PageSize = pageSize,
						StartTime = Convert.ToDateTime(startTime),
						EndTime = Convert.ToDateTime(endTime),
						CreatedDate = DateTime.Now
					};
					db.LionKingGameMains.Add(LionKingMain);
					db.SaveChanges();

					if (result != null)
					{
						if (result.Item.Count > 0)
						{
							foreach (var b in result.Item)
							{
								try
								{
									LionKingGame LionKingGame = new LionKingGame()
									{
										fId = sMainID,
										LoginId = b.LoginId,
										TotalBet = b.TotalBet,
										TotalWin = b.TotalWin,
										GameName = b.GameName,
										BetDetail = b.BetDetail,
										WinDetail = b.WinDetail,
										OrderId = b.OrderId,
										ActionDate = b.ActionDate,
										CreationDate = b.CreationDate,
										ValidCommission = b.ValidCommission,
										ValidBet = b.ValidBet,
										Rollover = _common.GetSlotRollover(Math.Abs(b.ValidBet), b.ValidWin - Math.Abs(b.ValidBet)),
										ValidWin = b.ValidWin,
										MJPComm = b.MJPComm,
										MJPWin = b.MJPWin,
										//CreatedDate = DateTime.Now
									};
									var record = db.LionKingGames.Where(x => x.OrderId == b.OrderId).FirstOrDefault();

									if (record == null)
									{
										LionKingGame.CreatedDate = DateTime.Now;
									}
									else
									{
										LionKingGame.CreatedDate = record.CreatedDate;
										LionKingGame.ModifiedDate = DateTime.Now;
									}
									db.LionKingGames.AddOrUpdate(LionKingGame);
								}
								catch (DbEntityValidationException e)
								{
									foreach (var eve in e.EntityValidationErrors)
									{
										_common.LogError("LionKing/GetGameRecord" + " " + DateTime.Now.ToString() + " " + string.Format("Entity of type \"{0}\" in state \"{1}\" has the following validation errors:",
																		eve.Entry.Entity.GetType().Name, eve.Entry.State), P_FilePrefix);
										foreach (var ve in eve.ValidationErrors)
										{
											_common.LogError("LionKing/GetGameRecord" + " " + DateTime.Now.ToString() + " " + string.Format("- Property: \"{0}\", Error: \"{1}\"",
																			ve.PropertyName, ve.ErrorMessage), P_FilePrefix);
										}
									}
									throw;
								}
								catch (WebException ex)
								{
									var responseText = new StreamReader(ex.Response.GetResponseStream()).ReadToEnd();
									_common.LogError("LionKing/GetGameRecord" + " " + DateTime.Now.ToString() + " " + ex.InnerException.InnerException.ToString() + " " + responseText, P_FilePrefix);
									throw;
								}
								catch (Exception ex)
								{
									_common.LogError("LionKing/GetGameRecord" + " " + DateTime.Now.ToString() + " " + ex.InnerException.InnerException.ToString(), P_FilePrefix);
									throw;
								}
							}
							db.SaveChanges();
							if (Convert.ToInt32(result.TotalPage) > 1)
							{
								for (int x = 2; x <= Convert.ToInt32(result.TotalPage); x++)
								{
									bool IsOK = GetGameRecordExtend(x, pageSize, mStartTime, mEndTime, sMainID);
								}
							}
						}
					}

					if (result != null)
					{
						if (string.IsNullOrEmpty(From) && string.IsNullOrEmpty(To))
						{
							bool isUpdJob = UpdJobTime(mStartTime, mEndTime, sMainID);
						}
					}

				}
			}


			return true;
		}

		public bool GetGameRecordExtend(int pageIndex, int pageSize, string startTime, string endTime, string sMainID)
		{
			try
			{
				LK_L365.Model.GetGameRecordByTimeResponseObject result = api.GetBrandCompletedGameResultsV2(startTime, endTime, pageIndex, pageSize);

				using (BingoConn db = new BingoConn())
				{
					if (result != null)
					{
						if (result.Item.Count > 0)
						{
							foreach (var b in result.Item)
							{
								try
								{
									LionKingGame LionKingGame = new LionKingGame()
									{
										fId = sMainID,
										LoginId = b.LoginId,
										TotalBet = b.TotalBet,
										TotalWin = b.TotalWin,
										GameName = b.GameName,
										BetDetail = b.BetDetail,
										WinDetail = b.WinDetail,
										OrderId = b.OrderId,
										ActionDate = b.ActionDate,
										CreationDate = b.CreationDate,
										ValidCommission = b.ValidCommission,
										ValidBet = b.ValidBet,
										Rollover = _common.GetSlotRollover(Math.Abs(b.ValidBet), b.ValidWin - Math.Abs(b.ValidBet)),
										ValidWin = b.ValidWin,
										MJPComm = b.MJPComm,
										MJPWin = b.MJPWin,
										CreatedDate = DateTime.Now
									};
									var record = db.LionKingGames.Where(x => x.OrderId == b.OrderId).FirstOrDefault();

									if (record == null)
									{
										LionKingGame.CreatedDate = DateTime.Now;
									}
									else
									{
										LionKingGame.CreatedDate = record.CreatedDate;
										LionKingGame.ModifiedDate = DateTime.Now;
									}
									db.LionKingGames.AddOrUpdate(LionKingGame);
								}
								catch (DbEntityValidationException e)
								{
									foreach (var eve in e.EntityValidationErrors)
									{
										_common.LogError("LionKing/GetGameRecord" + " " + DateTime.Now.ToString() + " " + string.Format("Entity of type \"{0}\" in state \"{1}\" has the following validation errors:",
																		eve.Entry.Entity.GetType().Name, eve.Entry.State), P_FilePrefix);
										foreach (var ve in eve.ValidationErrors)
										{
											_common.LogError("LionKing/GetGameRecord" + " " + DateTime.Now.ToString() + " " + string.Format("- Property: \"{0}\", Error: \"{1}\"",
																			ve.PropertyName, ve.ErrorMessage), P_FilePrefix);
										}
									}
									throw;
								}
								catch (WebException ex)
								{
									var responseText = new StreamReader(ex.Response.GetResponseStream()).ReadToEnd();
									_common.LogError("LionKing/GetGameRecord" + " " + DateTime.Now.ToString() + " " + ex.InnerException.InnerException.ToString() + " " + responseText, P_FilePrefix);
									throw;
								}
								catch (Exception ex)
								{
									_common.LogError("LionKing/GetGameRecord" + " " + DateTime.Now.ToString() + " " + ex.InnerException.InnerException.ToString(), P_FilePrefix);
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
					_common.LogError("LionKing/GetGameRecord" + " " + DateTime.Now.ToString() + " " + string.Format("Entity of type \"{0}\" in state \"{1}\" has the following validation errors:",
									eve.Entry.Entity.GetType().Name, eve.Entry.State), P_FilePrefix);
					foreach (var ve in eve.ValidationErrors)
					{
						_common.LogError("LionKing/GetGameRecord" + " " + DateTime.Now.ToString() + " " + string.Format("- Property: \"{0}\", Error: \"{1}\"",
										ve.PropertyName, ve.ErrorMessage), P_FilePrefix);
					}
				}
				return false;
			}
			catch (WebException ex)
			{
				var responseText = new StreamReader(ex.Response.GetResponseStream()).ReadToEnd();
				_common.LogError("LionKing/GetGameRecord" + " " + DateTime.Now.ToString() + " " + ex.InnerException.InnerException.ToString() + " " + responseText, P_FilePrefix);
				return false;
			}
			catch (Exception ex)
			{
				_common.LogError("LionKing/GetGameRecord" + " " + DateTime.Now.ToString() + " " + ex.InnerException.InnerException.ToString(), P_FilePrefix);
				return false;
			}
		}
	}
}