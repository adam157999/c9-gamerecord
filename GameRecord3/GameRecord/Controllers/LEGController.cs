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
	public class LEGController : Controller
	{
		public static readonly object MyLock = new object();
		private string P_GamePrefix = "LEG";

		private string P_FilePrefix = "LEG";
		private string P_DTFormat = "yyyy-MM-ddTHH:mm:ss.fffZ";
		Common _common = new Common();
		private LEGConnector api = new LEGConnector(Environment: BaseConnector.GameEnvironment.PROD);

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
				_common.LogError("LEG/UpdJobTime" + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), P_FilePrefix);
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
				_common.LogError("LEG/GetJobTime" + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), P_FilePrefix);
				return "Error:" + ex.Message.ToString();
			}
		}

		public void LEGJob()
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
			//string startTime = "2023-04-07 12:00:00";
			string endTime = GetJobTime("End");

			string mStartTime = startTime;
			string mEndTime = endTime;
			int limit = 40; // 24 hours
			int count = 0;
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

					LEG.Model.ReportResponseObject result = api.GetBrandCompletedGameResultsV2(mStartTime, mEndTime);

					using (BingoConn db = new BingoConn())
					{
						string sMainID = Guid.NewGuid().ToString();
						LEGGameMain legMain = new LEGGameMain()
						{
							Id = sMainID,
							Total = result != null ? result.count : 0,
							StartTime = Convert.ToDateTime(mStartTime),
							EndTime = Convert.ToDateTime(mEndTime),
							CreatedDate = DateTime.Now
						};
						db.LEGGameMains.Add(legMain);
						db.SaveChanges();

						if (result != null)
						{
							if (result.code == 0)
							{
								if (result.list != null)
								{
									for (int i = 0; i < Convert.ToInt32(result.count); i++)
									{
										string gameid = Convert.ToString(result.list.GameID[i]);
										try
										{
											LEGGame legGame = new LEGGame()
											{
												fId = sMainID,
												GameID = result.list.GameID[i],
												Accounts = result.list.Accounts[i],
												ServerID = result.list.ServerID[i],
												KindID = result.list.KindID[i],
												TableID = result.list.TableID[i],
												ChairID = result.list.ChairID[i],
												UserCount = result.list.UserCount[i],
												CardValue = result.list.CardValue[i],
												CellScore = result.list.CellScore[i],
												Rollover = _common.GetSlotRollover(Math.Abs(result.list.CellScore[i]), result.list.Profit[i]),
												AllBet = result.list.AllBet[i],
												Profit = result.list.Profit[i],
												Revenue = result.list.Revenue[i],
												GameStartTime = result.list.GameStartTime[i],
												GameEndTime = result.list.GameEndTime[i],
												ChannelID = result.list.ChannelID[i],
												LineCode = result.list.LineCode[i]
											};

											var record = db.LEGGames.Where(x => x.GameID == gameid).FirstOrDefault();
											if (record == null)
											{
												legGame.CreatedDate = DateTime.Now;
											}
											else
											{
												legGame.CreatedDate = record.CreatedDate;
												legGame.ModifiedDate = DateTime.Now;
											}
											db.LEGGames.AddOrUpdate(legGame);
										}
										catch (Exception ex)
										{
											_common.LogError("Record " + gameid + " " + DateTime.Now.ToString() + " " + ex.Message.ToString(), P_FilePrefix);
										}
									}
									db.SaveChanges();
								}
							}

							if (result.code == 0 || result.code == 16)
							{
								bool isUpdJob = UpdJobTime(mStartTime, mEndTime, sMainID);
							}
						}
					}

					Thread.Sleep(15000); //sleep for 15 seconds
				}

				return true;
			}
			catch (DbEntityValidationException e)
			{
				foreach (var eve in e.EntityValidationErrors)
				{
					_common.LogError("LEG/GetGameRecord" + " " + DateTime.Now.ToString() + " " + string.Format("Entity of type \"{0}\" in state \"{1}\" has the following validation errors:",
									eve.Entry.Entity.GetType().Name, eve.Entry.State), P_FilePrefix);
					foreach (var ve in eve.ValidationErrors)
					{
						_common.LogError("LEG/GetGameRecord" + " " + DateTime.Now.ToString() + " " + string.Format("- Property: \"{0}\", Error: \"{1}\"",
										ve.PropertyName, ve.ErrorMessage), P_FilePrefix);
					}
				}
				return false;
			}
			catch (WebException ex)
			{
				var responseText = new StreamReader(ex.Response.GetResponseStream()).ReadToEnd();
				_common.LogError("LEG/GetGameRecord" + " " + DateTime.Now.ToString() + " " + ex.Message.ToString() + " " + responseText, P_FilePrefix);
				return false;
			}
			catch (Exception ex)
			{
				_common.LogError("LEG/GetGameRecord" + " " + DateTime.Now.ToString() + " " + ex.Message.ToString(), P_FilePrefix);
				return false;
			}
		}
	}
}