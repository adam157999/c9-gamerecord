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
using NextSpin.Model;
using System.Globalization;

namespace GameRecord.Controllers
{
	public class NextSpinController : Controller
	{
		public static readonly object MyLock = new object();
		private string P_GamePrefix = "NEXTSPIN";

		private string P_FilePrefix = "NEXTSPIN";
		private string Currency = "MYR";
		private string P_DTFormat = "yyyyMMddTHHmmss";
		Common _common = new Common();
		private NextSpinConnector api = new NextSpinConnector(Environment: BaseConnector.GameEnvironment.PROD);

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
				_common.LogError("NextSpin/UpdJobTime" + " " + DateTime.Now.ToString() + "  " + ex.InnerException.InnerException.ToString(), P_FilePrefix);
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
				_common.LogError("NextSpin/GetJobTime" + " " + DateTime.Now.ToString() + "  " + ex.InnerException.InnerException.ToString(), P_FilePrefix);
				return "Error:" + ex.InnerException.InnerException.ToString();
			}
		}

		public void NextSpinJob()
		{
			try
			{
				lock (MyLock)
				{
					api = new NextSpinConnector(P_GamePrefix, Environment: GameEnvironment.PROD);
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

				int limit = 120; // 2 hours
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

					NextSpin.Model.GetBetHistoryResponse result = api.GetBetHistory(mStartTime, mEndTime, pageIndex);
					using (BingoConn db = new BingoConn())
					{
						string sMainID = Guid.NewGuid().ToString();
						NextSpinGameMain NextSpinMain = new NextSpinGameMain()
						{
							Id = sMainID,
							Total = 0,
							StartTime = Convert.ToDateTime(mStartTime),
							EndTime = Convert.ToDateTime(mEndTime),
							CreatedDate = DateTime.Now
						};
						db.NextSpinGameMains.Add(NextSpinMain);
						db.SaveChanges();

						if (result != null)
						{
							if (result.resultCount > 0)
							{
								foreach (var obj in result.list)
								{
									string ticketId = Convert.ToString(obj.ticketId);

									try
									{
										NextSpinGame nextSpinGame = new NextSpinGame()
										{
											fId = sMainID,
											ticketId = obj.ticketId,
											acctId = obj.acctId,
											ticketTime = DateTime.ParseExact(Convert.ToString(obj.ticketTime), P_DTFormat, CultureInfo.InvariantCulture),
											categoryId = obj.categoryId,
											gameCode = obj.gameCode,
											currency = obj.currency,
											betAmount = obj.betAmount,
											result = obj.result,
											winLoss = obj.winLoss,
											jackpotAmount = obj.jackpotAmount,
											betIp = obj.betIp,
											luckyDrawId = obj.luckyDrawId,
											completed = obj.completed,
											roundId = obj.roundId,
											sequence = obj.sequence,
											channel = obj.channel,
											balance = obj.balance,
											jpWin = obj.jpWin
										};

										var record = db.NextSpinGames.Where(x => x.ticketId == ticketId).FirstOrDefault();

										if (record == null)
										{
											nextSpinGame.CreatedDate = DateTime.Now;
										}
										else
										{
											nextSpinGame.CreatedDate = record.CreatedDate;
											nextSpinGame.ModifiedDate = DateTime.Now;
										}
										db.NextSpinGames.AddOrUpdate(nextSpinGame);
									}
									catch (DbEntityValidationException e)
									{
										foreach (var eve in e.EntityValidationErrors)
										{
											_common.LogError("NextSpin/GetGameRecord" + " " + DateTime.Now.ToString() + " " + string.Format("Entity of type \"{0}\" in state \"{1}\" has the following validation errors:",
																											eve.Entry.Entity.GetType().Name, eve.Entry.State), P_FilePrefix);
											foreach (var ve in eve.ValidationErrors)
											{
												_common.LogError("NextSpin/GetGameRecord" + " " + DateTime.Now.ToString() + " " + string.Format("- Property: \"{0}\", Error: \"{1}\"",
																												ve.PropertyName, ve.ErrorMessage), P_FilePrefix);
											}
										}
										throw;
									}
									catch (WebException ex)
									{
										var responseText = new StreamReader(ex.Response.GetResponseStream()).ReadToEnd();
										_common.LogError("NextSpin/GetGameRecord" + " " + DateTime.Now.ToString() + " " + ex.Message.ToString() + " " + responseText, P_FilePrefix);
										throw;
									}
									catch (Exception ex)
									{
										_common.LogError("NextSpin/GetGameRecord" + " " + DateTime.Now.ToString() + " " + ex.Message.ToString(), P_FilePrefix);
										throw;
									}
								}
								db.SaveChanges();

								if (Convert.ToInt16(result.pageCount) > 1)
								{
									for (int x = 2; x <= Convert.ToInt16(result.pageCount); x++)
									{
										bool IsOK = GetGameRecordExtend(x, mStartTime, mEndTime, sMainID);
									}
								}

							}

							if (result.code == 0)
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
					_common.LogError("NextSpin/GetGameRecord" + " " + DateTime.Now.ToString() + " " + string.Format("Entity of type \"{0}\" in state \"{1}\" has the following validation errors:",
													eve.Entry.Entity.GetType().Name, eve.Entry.State), P_FilePrefix);
					foreach (var ve in eve.ValidationErrors)
					{
						_common.LogError("NextSpin/GetGameRecord" + " " + DateTime.Now.ToString() + " " + string.Format("- Property: \"{0}\", Error: \"{1}\"",
														ve.PropertyName, ve.ErrorMessage), P_FilePrefix);
					}
				}
				return false;
			}
			catch (WebException ex)
			{
				var responseText = new StreamReader(ex.Response.GetResponseStream()).ReadToEnd();
				_common.LogError("NextSpin/GetGameRecord" + " " + DateTime.Now.ToString() + " " + ex.Message.ToString() + " " + responseText, P_FilePrefix);
				return false;
			}
			catch (Exception ex)
			{
				_common.LogError("NextSpin/GetGameRecord" + " " + DateTime.Now.ToString() + " " + ex.Message.ToString(), P_FilePrefix);
				return false;
			}

		}

		public bool GetGameRecordExtend(int pageIndex, string startTime, string endTime, string sMainID)
		{
			try
			{
				NextSpin.Model.GetBetHistoryResponse result = api.GetBetHistory(startTime, endTime, pageIndex);

				using (BingoConn db = new BingoConn())
				{
					if (result != null)
					{
						if (result.resultCount > 0)
						{
							foreach (var obj in result.list)
							{
								string ticketId = Convert.ToString(obj.ticketId);

								try
								{
									NextSpinGame nextSpinGame = new NextSpinGame()
									{
										fId = sMainID,
										ticketId = obj.ticketId,
										acctId = obj.acctId,
										ticketTime = DateTime.ParseExact(Convert.ToString(obj.ticketTime), P_DTFormat, CultureInfo.InvariantCulture),
										categoryId = obj.categoryId,
										gameCode = obj.gameCode,
										currency = obj.currency,
										betAmount = obj.betAmount,
										result = obj.result,
										winLoss = obj.winLoss,
										jackpotAmount = obj.jackpotAmount,
										betIp = obj.betIp,
										luckyDrawId = obj.luckyDrawId,
										completed = obj.completed,
										roundId = obj.roundId,
										sequence = obj.sequence,
										channel = obj.channel,
										balance = obj.balance,
										jpWin = obj.jpWin
									};

									var record = db.NextSpinGames.Where(x => x.ticketId == ticketId).FirstOrDefault();

									if (record == null)
									{
										nextSpinGame.CreatedDate = DateTime.Now;
									}
									else
									{
										nextSpinGame.CreatedDate = record.CreatedDate;
										nextSpinGame.ModifiedDate = DateTime.Now;
									}
									db.NextSpinGames.AddOrUpdate(nextSpinGame);
								}
								catch (DbEntityValidationException e)
								{
									foreach (var eve in e.EntityValidationErrors)
									{
										_common.LogError("NextSpin/GetGameRecord" + " " + DateTime.Now.ToString() + " " + string.Format("Entity of type \"{0}\" in state \"{1}\" has the following validation errors:",
																										eve.Entry.Entity.GetType().Name, eve.Entry.State), P_FilePrefix);
										foreach (var ve in eve.ValidationErrors)
										{
											_common.LogError("NextSpin/GetGameRecord" + " " + DateTime.Now.ToString() + " " + string.Format("- Property: \"{0}\", Error: \"{1}\"",
																											ve.PropertyName, ve.ErrorMessage), P_FilePrefix);
										}
									}
									throw;
								}
								catch (WebException ex)
								{
									var responseText = new StreamReader(ex.Response.GetResponseStream()).ReadToEnd();
									_common.LogError("NextSpin/GetGameRecord" + " " + DateTime.Now.ToString() + " " + ex.InnerException.InnerException.ToString() + " " + responseText, P_FilePrefix);
									throw;
								}
								catch (Exception ex)
								{
									_common.LogError("NextSpin/GetGameRecord" + " " + DateTime.Now.ToString() + " " + ex.InnerException.InnerException.ToString(), P_FilePrefix);
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
					_common.LogError("NextSpin/GetGameRecord" + " " + DateTime.Now.ToString() + " " + string.Format("Entity of type \"{0}\" in state \"{1}\" has the following validation errors:",
													eve.Entry.Entity.GetType().Name, eve.Entry.State), P_FilePrefix);
					foreach (var ve in eve.ValidationErrors)
					{
						_common.LogError("NextSpin/GetGameRecord" + " " + DateTime.Now.ToString() + " " + string.Format("- Property: \"{0}\", Error: \"{1}\"",
														ve.PropertyName, ve.ErrorMessage), P_FilePrefix);
					}
				}
				return false;
			}
			catch (WebException ex)
			{
				var responseText = new StreamReader(ex.Response.GetResponseStream()).ReadToEnd();
				_common.LogError("NextSpin/GetGameRecord" + " " + DateTime.Now.ToString() + " " + ex.InnerException.InnerException.ToString() + " " + responseText, P_FilePrefix);
				return false;
			}
			catch (Exception ex)
			{
				_common.LogError("NextSpin/GetGameRecord" + " " + DateTime.Now.ToString() + " " + ex.InnerException.InnerException.ToString(), P_FilePrefix);
				return false;
			}
		}
	}
}