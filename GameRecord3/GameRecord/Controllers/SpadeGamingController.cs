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
using SpadeGaming.Model;
using System.Globalization;

namespace GameRecord.Controllers
{
	public class SpadeGamingController : Controller
	{
		public static readonly object MyLock = new object();
		private string P_GamePrefix = "SPADEGAMING";

		private string P_FilePrefix = "SPADEGAMING";
		private string P_DTFormat = "yyyyMMddTHHmmss";
		Common _common = new Common();
		private SpadeGamingConnector api = new SpadeGamingConnector(Environment: BaseConnector.GameEnvironment.PROD);

		public bool UpdJobTime(string startTime, string endTime)
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

						bingoConn.SaveChanges();
					}
				}
				return true;
			}
			catch (Exception ex)
			{
				_common.LogError("SpadeGaming/UpdJobTime" + " " + DateTime.Now.ToString() + "  " + ex.InnerException.InnerException.ToString(), P_FilePrefix);
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
				_common.LogError("SpadeGaming/GetJobTime" + " " + DateTime.Now.ToString() + "  " + ex.InnerException.InnerException.ToString(), P_FilePrefix);
				return "Error:" + ex.InnerException.InnerException.ToString();
			}
		}

		public void SpadeGamingJob()
		{
			try
			{
				lock (MyLock)
				{
					api = new SpadeGamingConnector(P_GamePrefix, Environment: GameEnvironment.PROD);
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

					SpadeGaming.Model.GetBetHistoryResponse result = api.GetBetHistory(mStartTime, mEndTime, pageIndex);
					using (BingoConn db = new BingoConn())
					{
						string sMainID = Guid.NewGuid().ToString();
						SpadeGamingGameMain SpadeMain = new SpadeGamingGameMain()
						{
							Id = sMainID,
							Total = 0,
							StartTime = Convert.ToDateTime(mStartTime),
							EndTime = Convert.ToDateTime(mEndTime),
							CreatedDate = DateTime.Now
						};
						db.SpadeGamingGameMains.Add(SpadeMain);
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
										SpadeGamingGame spadeGame = new SpadeGamingGame()
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

										var record = db.SpadeGamingGames.Where(x => x.ticketId == ticketId).FirstOrDefault();

										if (record == null)
										{
											spadeGame.CreatedDate = DateTime.Now;
										}
										else
										{
											spadeGame.CreatedDate = record.CreatedDate;
											spadeGame.ModifiedDate = DateTime.Now;
										}
										db.SpadeGamingGames.AddOrUpdate(spadeGame);
									}
									catch (DbEntityValidationException e)
									{
										foreach (var eve in e.EntityValidationErrors)
										{
											_common.LogError("SpadeGaming/GetGameRecord" + " " + DateTime.Now.ToString() + " " + string.Format("Entity of type \"{0}\" in state \"{1}\" has the following validation errors:",
																											eve.Entry.Entity.GetType().Name, eve.Entry.State), P_FilePrefix);
											foreach (var ve in eve.ValidationErrors)
											{
												_common.LogError("SpadeGaming/GetGameRecord" + " " + DateTime.Now.ToString() + " " + string.Format("- Property: \"{0}\", Error: \"{1}\"",
																												ve.PropertyName, ve.ErrorMessage), P_FilePrefix);
											}
										}
										throw;
									}
									catch (WebException ex)
									{
										var responseText = new StreamReader(ex.Response.GetResponseStream()).ReadToEnd();
										_common.LogError("SpadeGaming/GetGameRecord" + " " + DateTime.Now.ToString() + " " + ex.InnerException.InnerException.ToString() + " " + responseText, P_FilePrefix);
										throw;
									}
									catch (Exception ex)
									{
										_common.LogError("SpadeGaming/GetGameRecord" + " " + DateTime.Now.ToString() + " " + ex.InnerException.InnerException.ToString(), P_FilePrefix);
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
								bool isUpdJob = UpdJobTime(mStartTime, mEndTime);
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
					_common.LogError("SpadeGaming/GetGameRecord" + " " + DateTime.Now.ToString() + " " + string.Format("Entity of type \"{0}\" in state \"{1}\" has the following validation errors:",
													eve.Entry.Entity.GetType().Name, eve.Entry.State), P_FilePrefix);
					foreach (var ve in eve.ValidationErrors)
					{
						_common.LogError("SpadeGaming/GetGameRecord" + " " + DateTime.Now.ToString() + " " + string.Format("- Property: \"{0}\", Error: \"{1}\"",
														ve.PropertyName, ve.ErrorMessage), P_FilePrefix);
					}
				}
				return false;
			}
			catch (WebException ex)
			{
				var responseText = new StreamReader(ex.Response.GetResponseStream()).ReadToEnd();
				_common.LogError("SpadeGaming/GetGameRecord" + " " + DateTime.Now.ToString() + " " + ex.InnerException.InnerException.ToString() + " " + responseText, P_FilePrefix);
				return false;
			}
			catch (Exception ex)
			{
				_common.LogError("SpadeGaming/GetGameRecord" + " " + DateTime.Now.ToString() + " " + ex.InnerException.InnerException.ToString(), P_FilePrefix);
				return false;
			}

		}

		public bool GetGameRecordExtend(int pageIndex, string startTime, string endTime, string sMainID)
		{
			try
			{
				SpadeGaming.Model.GetBetHistoryResponse result = api.GetBetHistory(startTime, endTime, pageIndex);

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
									SpadeGamingGame spadeGame = new SpadeGamingGame()
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

									var record = db.SpadeGamingGames.Where(x => x.ticketId == ticketId).FirstOrDefault();

									if (record == null)
									{
										spadeGame.CreatedDate = DateTime.Now;
									}
									else
									{
										spadeGame.CreatedDate = record.CreatedDate;
										spadeGame.ModifiedDate = DateTime.Now;
									}
									db.SpadeGamingGames.AddOrUpdate(spadeGame);
								}
								catch (DbEntityValidationException e)
								{
									foreach (var eve in e.EntityValidationErrors)
									{
										_common.LogError("SpadeGaming/GetGameRecord" + " " + DateTime.Now.ToString() + " " + string.Format("Entity of type \"{0}\" in state \"{1}\" has the following validation errors:",
																										eve.Entry.Entity.GetType().Name, eve.Entry.State), P_FilePrefix);
										foreach (var ve in eve.ValidationErrors)
										{
											_common.LogError("SpadeGaming/GetGameRecord" + " " + DateTime.Now.ToString() + " " + string.Format("- Property: \"{0}\", Error: \"{1}\"",
																											ve.PropertyName, ve.ErrorMessage), P_FilePrefix);
										}
									}
									throw;
								}
								catch (WebException ex)
								{
									var responseText = new StreamReader(ex.Response.GetResponseStream()).ReadToEnd();
									_common.LogError("SpadeGaming/GetGameRecord" + " " + DateTime.Now.ToString() + " " + ex.InnerException.InnerException.ToString() + " " + responseText, P_FilePrefix);
									throw;
								}
								catch (Exception ex)
								{
									_common.LogError("SpadeGaming/GetGameRecord" + " " + DateTime.Now.ToString() + " " + ex.InnerException.InnerException.ToString(), P_FilePrefix);
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
					_common.LogError("SpadeGaming/GetGameRecord" + " " + DateTime.Now.ToString() + " " + string.Format("Entity of type \"{0}\" in state \"{1}\" has the following validation errors:",
													eve.Entry.Entity.GetType().Name, eve.Entry.State), P_FilePrefix);
					foreach (var ve in eve.ValidationErrors)
					{
						_common.LogError("SpadeGaming/GetGameRecord" + " " + DateTime.Now.ToString() + " " + string.Format("- Property: \"{0}\", Error: \"{1}\"",
														ve.PropertyName, ve.ErrorMessage), P_FilePrefix);
					}
				}
				return false;
			}
			catch (WebException ex)
			{
				var responseText = new StreamReader(ex.Response.GetResponseStream()).ReadToEnd();
				_common.LogError("SpadeGaming/GetGameRecord" + " " + DateTime.Now.ToString() + " " + ex.InnerException.InnerException.ToString() + " " + responseText, P_FilePrefix);
				return false;
			}
			catch (Exception ex)
			{
				_common.LogError("SpadeGaming/GetGameRecord" + " " + DateTime.Now.ToString() + " " + ex.InnerException.InnerException.ToString(), P_FilePrefix);
				return false;
			}
		}
	}
}