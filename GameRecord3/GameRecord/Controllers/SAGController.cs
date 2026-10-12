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

namespace GameRecord.Controllers
{
	public class SAGController : Controller
	{
		public static readonly object MyLock = new object();
		private string P_GamePrefix = "SAG";

		private string P_FilePrefix = "SAG";
		private string P_DTFormat = "yyyy-MM-ddTHH:mm:ss.fffZ";
		Common _common = new Common();
		private SAGConnector api = new SAGConnector(Environment: BaseConnector.GameEnvironment.PROD);

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
				_common.LogError("SAG/UpdJobTime" + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), P_FilePrefix);
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
						DateTime dtSTime = Convert.ToDateTime(DateTime.Now.ToString("yyyy-MM-dd HH:mm")) - new TimeSpan(0, 8, 15, 0, 0);
						sTime = dtSTime.ToString("yyyy-MM-dd HH:mm:ss");
					}
				}

				return sTime;

			}
			catch (Exception ex)
			{
				_common.LogError("SAG/GetJobTime" + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), P_FilePrefix);
				return "Error:" + ex.Message.ToString();
			}
		}

		public void SAGJob()
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
			string sMainID = Guid.NewGuid().ToString();
			string mStartTime = startTime;
			string mEndTime = endTime;
			int limit = 1440; // 24 hours

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

					SAG.Models.GetAllBetDetailsForTimeIntervalDVResponse result = api.GetBetHistory(Convert.ToDateTime(startTime), Convert.ToDateTime(endTime));

					using (BingoConn db = new BingoConn())
					{
						SAMain saMain = new SAMain()
						{
							Id = sMainID,
							Total = null,
							StartTime = Convert.ToDateTime(startTime),
							EndTime = Convert.ToDateTime(endTime),
							CreatedDate = DateTime.Now
						};
						db.SAMains.Add(saMain);
						db.SaveChanges();

						if (result.BetDetailList.Count > 0)
						{
							foreach (var b in result.BetDetailList)
							{
								string id = b.GameID;
								try
								{
									SAGame saGame = new SAGame()
									{
										fId = sMainID,
										BetTime = b.BetTime,
										PayoutTime = b.PayoutTime,
										Username = b.Username,
										HostID = b.HostID,
										Detail = b.Detail,
										GameID = b.GameID,
										Round = b.Round,
										Set = b.Set,
										BetID = b.BetID,
										BetAmount = b.BetAmount,
										Rolling = b.Rolling,
										ResultAmount = b.ResultAmount,
										Balance = b.Balance,
										GameType = b.GameType,
										BetType = b.BetType,
										BetSource = b.BetSource,
										State = b.State,
										CreatedDate = DateTime.Now
									};
									db.SAGames.AddOrUpdate(saGame);
								}
								catch (DbEntityValidationException e)
								{
									foreach (var eve in e.EntityValidationErrors)
									{
										_common.LogError("SAG/GetGameRecord" + " " + DateTime.Now.ToString() + " " + string.Format("Entity of type \"{0}\" in state \"{1}\" has the following validation errors:",
														eve.Entry.Entity.GetType().Name, eve.Entry.State), P_FilePrefix);
										foreach (var ve in eve.ValidationErrors)
										{
											_common.LogError("SAG/GetGameRecord" + " " + DateTime.Now.ToString() + " " + string.Format("- Property: \"{0}\", Error: \"{1}\"",
															ve.PropertyName, ve.ErrorMessage), P_FilePrefix);
										}
									}
								}
								catch (WebException ex)
								{
									var error = new StreamReader(ex.Response.GetResponseStream()).ReadToEnd();
									_common.LogError("Record " + id + " " + DateTime.Now.ToString() + " " + ex.Message.ToString() + " " + error, P_FilePrefix);
								}
								catch (Exception ex)
								{
									_common.LogError("Record " + id + " " + DateTime.Now.ToString() + " " + ex.Message.ToString(), P_FilePrefix);
								}
							}
							db.SaveChanges();

						}

						bool isUpdJob = UpdJobTime(startTime, endTime, sMainID);
					}
				}
				//}


				return true;
			}
			catch (DbEntityValidationException e)
			{
				foreach (var eve in e.EntityValidationErrors)
				{
					_common.LogError("SAG/GetGameRecord" + " " + DateTime.Now.ToString() + " " + string.Format("Entity of type \"{0}\" in state \"{1}\" has the following validation errors:",
									eve.Entry.Entity.GetType().Name, eve.Entry.State), P_FilePrefix);
					foreach (var ve in eve.ValidationErrors)
					{
						_common.LogError("SAG/GetGameRecord" + " " + DateTime.Now.ToString() + " " + string.Format("- Property: \"{0}\", Error: \"{1}\"",
										ve.PropertyName, ve.ErrorMessage), P_FilePrefix);
					}
				}
				return false;
			}
			catch (WebException ex)
			{
				var responseText = new StreamReader(ex.Response.GetResponseStream()).ReadToEnd();
				_common.LogError("SAG/GetGameRecord" + " " + DateTime.Now.ToString() + " " + ex.Message.ToString() + " " + responseText, P_FilePrefix);
				return false;
			}
			catch (Exception ex)
			{
				_common.LogError("SAG/GetGameRecord" + " " + DateTime.Now.ToString() + " " + ex.Message.ToString(), P_FilePrefix);
				return false;
			}
		}
	}
}