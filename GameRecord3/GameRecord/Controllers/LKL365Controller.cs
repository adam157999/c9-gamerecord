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
	public class LKL365Controller : Controller
	{
		public static readonly object MyLock = new object();
		private string P_GamePrefix = "LKL365";

		private string P_FilePrefix = "LKL365";
		private string P_DTFormat = "yyyy-MM-ddTHH:mm:ss.fffZ";
		Common _common = new Common();
		private LKL365Connector api = null;
		List<string> idArr = new List<string>();

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
				_common.LogError("LKL365/UpdJobTime" + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), P_FilePrefix);
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
				_common.LogError("LKL365/GetJobTime" + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), P_FilePrefix);
				return "Error:" + ex.Message.ToString();
			}
		}

		public void LKL365Job(string From = "", string To = "")
		{
			try
			{
				lock (MyLock)
				{
					//if (!string.IsNullOrEmpty(From) && !string.IsNullOrEmpty(To))
					//	_common.LogError("LKL365/GetGameRecord" + " " + DateTime.Now.ToString() + " " + From + " " + To, P_FilePrefix);
					api = new LKL365Connector(P_GamePrefix, Environment: GameEnvironment.PROD);
					bool isOK = GetGameRecord(From, To);

				}
			}
			catch (DbEntityValidationException e)
			{
				foreach (var eve in e.EntityValidationErrors)
				{
					_common.LogError("LKL365Job" + " " + DateTime.Now.ToString() + " " + string.Format("Entity of type \"{0}\" in state \"{1}\" has the following validation errors:",
									eve.Entry.Entity.GetType().Name, eve.Entry.State), P_FilePrefix);
					foreach (var ve in eve.ValidationErrors)
					{
						_common.LogError("LKL365Job" + " " + DateTime.Now.ToString() + " " + string.Format("- Property: \"{0}\", Error: \"{1}\"",
										ve.PropertyName, ve.ErrorMessage), P_FilePrefix);
					}
				}
			}
			catch (WebException ex)
			{
				var responseText = new StreamReader(ex.Response.GetResponseStream()).ReadToEnd();
				_common.LogError("LKL365Job" + " " + DateTime.Now.ToString() + " " + ex.InnerException.InnerException.ToString() + " " + responseText, P_FilePrefix);
			}
			catch (Exception ex)
			{
				_common.LogError("LKL365Job - " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), P_FilePrefix);
			}
		}

		public bool GetGameRecord(string From, string To)
		{
			try
			{
				string startTime = (!string.IsNullOrEmpty(From) ? From : GetJobTime("Start"));
				string endTime = (!string.IsNullOrEmpty(To) ? To : GetJobTime("End"));
				string mStartTime = startTime;
				string mEndTime = endTime;

				//int limit = 120; // 2 hours
				int limit = 30; // 30 mins
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
						LKL365GameMain lkl365Main = new LKL365GameMain()
						{
							Id = sMainID,
							Total = result.TotalRows,
							PageIndex = result.CurrentPage,
							PageSize = pageSize,
							StartTime = Convert.ToDateTime(startTime),
							EndTime = Convert.ToDateTime(endTime),
							CreatedDate = DateTime.Now
						};
						db.LKL365GameMain.Add(lkl365Main);
						db.SaveChanges();

						if (result != null)
						{
							if (result.Item.Count > 0)
							{
								foreach (var b in result.Item)
								{
									try
									{
										LKL365Game lkl365Game = new LKL365Game()
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
										var record = db.LKL365Game.Where(x => x.OrderId == b.OrderId).FirstOrDefault();

										if (record == null)
										{
											lkl365Game.CreatedDate = DateTime.Now;
										}
										else
										{
											lkl365Game.CreatedDate = record.CreatedDate;
											lkl365Game.ModifiedDate = DateTime.Now;
										}
										if (!idArr.Contains(Convert.ToString(b.OrderId)))
										{
											db.LKL365Game.AddOrUpdate(lkl365Game);
											idArr.Add(Convert.ToString(b.OrderId));
										}
									}
									catch (DbEntityValidationException e)
									{
										foreach (var eve in e.EntityValidationErrors)
										{
											_common.LogError("LKL365/GetGameRecord" + " " + DateTime.Now.ToString() + " " + string.Format("Entity of type \"{0}\" in state \"{1}\" has the following validation errors:",
																			eve.Entry.Entity.GetType().Name, eve.Entry.State), P_FilePrefix);
											foreach (var ve in eve.ValidationErrors)
											{
												_common.LogError("LKL365/GetGameRecord" + " " + DateTime.Now.ToString() + " " + string.Format("- Property: \"{0}\", Error: \"{1}\"",
																				ve.PropertyName, ve.ErrorMessage), P_FilePrefix);
											}
										}
										throw;
									}
									catch (WebException ex)
									{
										var responseText = new StreamReader(ex.Response.GetResponseStream()).ReadToEnd();
										_common.LogError("LKL365/GetGameRecord" + " " + DateTime.Now.ToString() + " " + ex.InnerException.InnerException.ToString() + " " + responseText, P_FilePrefix);
										throw;
									}
									catch (Exception ex)
									{
										_common.LogError("LKL365/GetGameRecord" + " " + DateTime.Now.ToString() + " " + ex.InnerException.InnerException.ToString(), P_FilePrefix);
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

					idArr.Clear();
				}

				return true;
			}
			catch (DbEntityValidationException e)
			{
				foreach (var eve in e.EntityValidationErrors)
				{
					_common.LogError("LKL365/GetGameRecord" + " " + DateTime.Now.ToString() + " " + string.Format("Entity of type \"{0}\" in state \"{1}\" has the following validation errors:",
									eve.Entry.Entity.GetType().Name, eve.Entry.State), P_FilePrefix);
					foreach (var ve in eve.ValidationErrors)
					{
						_common.LogError("LKL365/GetGameRecord" + " " + DateTime.Now.ToString() + " " + string.Format("- Property: \"{0}\", Error: \"{1}\"",
										ve.PropertyName, ve.ErrorMessage), P_FilePrefix);
					}
				}
				return false;
			}
			catch (WebException ex)
			{
				var responseText = new StreamReader(ex.Response.GetResponseStream()).ReadToEnd();
				_common.LogError("LKL365/GetGameRecord" + " " + DateTime.Now.ToString() + " " + ex.InnerException.InnerException.ToString() + " " + responseText, P_FilePrefix);
				return false;
			}
			catch (Exception ex)
			{
				_common.LogError("LKL365/GetGameRecord" + " " + DateTime.Now.ToString() + " " + ex.InnerException.InnerException.ToString(), P_FilePrefix);
				return false;
			}
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
									LKL365Game lkl365Game = new LKL365Game()
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
									var record = db.LKL365Game.Where(x => x.OrderId == b.OrderId).FirstOrDefault();

									if (record == null)
									{
										lkl365Game.CreatedDate = DateTime.Now;
									}
									else
									{
										lkl365Game.CreatedDate = record.CreatedDate;
										lkl365Game.ModifiedDate = DateTime.Now;
									}
									if (!idArr.Contains(Convert.ToString(b.OrderId)))
									{
										db.LKL365Game.AddOrUpdate(lkl365Game);
										idArr.Add(Convert.ToString(b.OrderId));
									}
								}
								catch (DbEntityValidationException e)
								{
									foreach (var eve in e.EntityValidationErrors)
									{
										_common.LogError("LKL365/GetGameRecord" + " " + DateTime.Now.ToString() + " " + string.Format("Entity of type \"{0}\" in state \"{1}\" has the following validation errors:",
																		eve.Entry.Entity.GetType().Name, eve.Entry.State), P_FilePrefix);
										foreach (var ve in eve.ValidationErrors)
										{
											_common.LogError("LKL365/GetGameRecord" + " " + DateTime.Now.ToString() + " " + string.Format("- Property: \"{0}\", Error: \"{1}\"",
																			ve.PropertyName, ve.ErrorMessage), P_FilePrefix);
										}
									}
									throw;
								}
								catch (WebException ex)
								{
									var responseText = new StreamReader(ex.Response.GetResponseStream()).ReadToEnd();
									_common.LogError("LKL365/GetGameRecord" + " " + DateTime.Now.ToString() + " " + ex.InnerException.InnerException.ToString() + " " + responseText, P_FilePrefix);
									throw;
								}
								catch (Exception ex)
								{
									_common.LogError("LKL365/GetGameRecord" + " " + DateTime.Now.ToString() + " " + ex.InnerException.InnerException.ToString(), P_FilePrefix);
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
					_common.LogError("LKL365/GetGameRecord" + " " + DateTime.Now.ToString() + " " + string.Format("Entity of type \"{0}\" in state \"{1}\" has the following validation errors:",
									eve.Entry.Entity.GetType().Name, eve.Entry.State), P_FilePrefix);
					foreach (var ve in eve.ValidationErrors)
					{
						_common.LogError("LKL365/GetGameRecord" + " " + DateTime.Now.ToString() + " " + string.Format("- Property: \"{0}\", Error: \"{1}\"",
										ve.PropertyName, ve.ErrorMessage), P_FilePrefix);
					}
				}
				return false;
			}
			catch (WebException ex)
			{
				var responseText = new StreamReader(ex.Response.GetResponseStream()).ReadToEnd();
				_common.LogError("LKL365/GetGameRecord" + " " + DateTime.Now.ToString() + " " + ex.InnerException.InnerException.ToString() + " " + responseText, P_FilePrefix);
				return false;
			}
			catch (Exception ex)
			{
				_common.LogError("LKL365/GetGameRecord" + " " + DateTime.Now.ToString() + " " + ex.InnerException.InnerException.ToString(), P_FilePrefix);
				return false;
			}
		}
	}
}