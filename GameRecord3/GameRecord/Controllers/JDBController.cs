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
using System.Threading;

namespace GameRecord.Controllers
{
	public class JDBController : Controller
	{
		public static readonly object MyLock = new object();
		private string P_GamePrefix = "JDB";

		private string P_FilePrefix = "JDB";
		private string P_DTFormat = "yyyy-MM-ddTHH:mm:ss.fffZ";
		Common _common = new Common();
		private JDBConnector api = new JDBConnector(Environment: BaseConnector.GameEnvironment.PROD);

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
				_common.LogError("JDB/UpdJobTime" + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), P_FilePrefix);
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
						DateTime dtSTime = Convert.ToDateTime(query.LastRunTillDate?.ToString("yyyy-MM-dd HH:mm")) - new TimeSpan(0, 0, 18, 0, 0);
						sTime = dtSTime.ToString("yyyy-MM-dd HH:mm:ss");
					}
					else
					{
						DateTime dtSTime = Convert.ToDateTime(DateTime.Now.ToString("yyyy-MM-dd HH:mm")) - new TimeSpan(0, 0, 3, 0, 0);
						sTime = dtSTime.ToString("yyyy-MM-dd HH:mm:ss");
					}
				}

				return sTime;

			}
			catch (Exception ex)
			{
				_common.LogError("JDB/GetJobTime" + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), P_FilePrefix);
				return "Error:" + ex.Message.ToString();
			}
		}

		public void JDBJob()
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
			int limit = 15;
			int count = 0;
			try
			{
				int MinDiff = Convert.ToInt32((Convert.ToDateTime(endTime) - Convert.ToDateTime(startTime)).TotalMinutes);
				int times = (MinDiff / limit) + 1;
				for (int j = limit; j <= times * limit; j += limit)
				{
					mStartTime = j == limit ? startTime : mEndTime;

					if ((Convert.ToDateTime(mStartTime).AddMinutes(limit) - Convert.ToDateTime(endTime)).TotalMinutes >= 0)
						mEndTime = endTime;
					else
						mEndTime = Convert.ToString(Convert.ToDateTime(mStartTime).AddMinutes(limit));

					JDB.Model.GetDetailReportResponse result = api.GetDetailReport(mStartTime, mEndTime);

					using (BingoConn db = new BingoConn())
					{
						string sMainID = Guid.NewGuid().ToString();
						JDBGameMain jdbMain = new JDBGameMain()
						{
							Id = sMainID,
							Total = result != null ? result.data.Count : 0,
							StartTime = Convert.ToDateTime(startTime),
							EndTime = Convert.ToDateTime(endTime),
							CreatedDate = DateTime.Now
						};
						db.JDBGameMains.Add(jdbMain);
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
											decimal stake = GetJDBTurnover(b.bet, b.validBet, b.gambleBet, b.gType, b.hasGamble);
											JDBGame jdbGame = new JDBGame()
											{
												fId = sMainID,
												seqNo = b.seqNo,
												roundSeqNo = b.roundSeqNo,
												playerId = b.playerId,
												gType = b.gType,
												mtype = b.mtype,
												gameDate = DateTime.ParseExact(b.gameDate, "dd-MM-yyyy HH:mm:ss", CultureInfo.InvariantCulture),
												roomType = b.roomType,
												bet = b.bet,
												gambleBet = b.gambleBet,
												validBet = b.validBet,
												rollover = _common.GetSlotRollover(Math.Abs(stake), b.win - Math.Abs(stake)),
												win = b.win,
												tax = b.tax,
												total = b.total,
												currency = b.currency,
												jackpot = b.jackpot,
												jackpotContribute = b.jackpotContribute,
												denom = b.denom,
												beforeBalance = b.beforeBalance,
												afterBalance = b.afterBalance,
												lastModifyTime = DateTime.ParseExact(b.lastModifyTime, "dd-MM-yyyy HH:mm:ss", CultureInfo.InvariantCulture),
												playerIp = b.playerIp,
												clientType = b.clientType,
												hasFreegame = b.hasFreegame,
												hasGamble = b.hasGamble,
												hasBonusGame = b.hasBonusGame,
												systemTakeWin = b.systemTakeWin,
												//CreatedDate = DateTime.Now
											};
											var record = db.JDBGames.Where(x => x.seqNo == b.seqNo).FirstOrDefault();

											if (record == null)
											{
												jdbGame.CreatedDate = DateTime.Now;
											}
											else
											{
												jdbGame.CreatedDate = record.CreatedDate;
												jdbGame.ModifiedDate = DateTime.Now;
											}
											db.JDBGames.AddOrUpdate(jdbGame);
										}
										catch (DbEntityValidationException e)
										{
											foreach (var eve in e.EntityValidationErrors)
											{
												_common.LogError("JDB/GetGameRecord" + " " + DateTime.Now.ToString() + " " + string.Format("Entity of type \"{0}\" in state \"{1}\" has the following validation errors:",
																eve.Entry.Entity.GetType().Name, eve.Entry.State), P_FilePrefix);
												foreach (var ve in eve.ValidationErrors)
												{
													_common.LogError("JDB/GetGameRecord" + " " + DateTime.Now.ToString() + " " + string.Format("- Property: \"{0}\", Error: \"{1}\"",
																	ve.PropertyName, ve.ErrorMessage), P_FilePrefix);
												}
											}
										}
										catch (WebException ex)
										{
											var error = new StreamReader(ex.Response.GetResponseStream()).ReadToEnd();
											_common.LogError("Record " + b.seqNo + " " + DateTime.Now.ToString() + " " + ex.Message.ToString() + " " + error, P_FilePrefix);
										}
										catch (Exception ex)
										{
											_common.LogError("Record " + b.seqNo + " " + DateTime.Now.ToString() + " " + ex.Message.ToString(), P_FilePrefix);
										}
									}
									db.SaveChanges();

								}
								else
								{
									Thread.Sleep(5000);
								}
							}
						}

						if (result.status == "0000")
						{
							bool isUpdJob = UpdJobTime(mStartTime, mEndTime, sMainID);
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
					_common.LogError("JDB/GetGameRecord" + " " + DateTime.Now.ToString() + " " + string.Format("Entity of type \"{0}\" in state \"{1}\" has the following validation errors:",
									eve.Entry.Entity.GetType().Name, eve.Entry.State), P_FilePrefix);
					foreach (var ve in eve.ValidationErrors)
					{
						_common.LogError("JDB/GetGameRecord" + " " + DateTime.Now.ToString() + " " + string.Format("- Property: \"{0}\", Error: \"{1}\"",
										ve.PropertyName, ve.ErrorMessage), P_FilePrefix);
					}
				}
				return false;
			}
			catch (WebException ex)
			{
				var responseText = new StreamReader(ex.Response.GetResponseStream()).ReadToEnd();
				_common.LogError("JDB/GetGameRecord" + " " + DateTime.Now.ToString() + " " + ex.Message.ToString() + " " + responseText, P_FilePrefix);
				return false;
			}
			catch (Exception ex)
			{
				_common.LogError("JDB/GetGameRecord" + " " + DateTime.Now.ToString() + " " + ex.Message.ToString(), P_FilePrefix);
				return false;
			}
		}

		public decimal GetJDBTurnover(decimal bet, decimal? validBet, decimal? gambleBet, int gType, int? hasGamble)
		{
			if (gType == 18)
				return (validBet != null ? Math.Abs((decimal)validBet) : Math.Abs(bet));

			if (hasGamble == 1)
				return (gambleBet != null ? Math.Abs((decimal)gambleBet) : Math.Abs(bet));

			return Math.Abs(bet);
		}
	}
}