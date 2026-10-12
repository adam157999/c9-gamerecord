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
using RestSharp;
using System.Dynamic;
using Newtonsoft.Json.Linq;
using System.Data.Entity.Migrations;

namespace GameRecord.Controllers
{
	public class CQ9Controller : Controller
	{
		public static readonly object MyLock = new object();
		private string P_GamePrefix = "CQ9";
		private string P_ApiUrl = "https://api.cqgame.games";
		private string P_Token = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJ1c2VyaWQiOiI2MTY3ZjJlNzk0ZDI0ZDAwMDFmOTdmYWQiLCJhY2NvdW50IjoiTUNJVFkiLCJvd25lciI6IjVkMWFmYTI0ZDhiYjg4MDAwMThiMTExMSIsInBhcmVudCI6IjVkMWFmYTI0ZDhiYjg4MDAwMThiMTExMSIsImN1cnJlbmN5IjoiTVlSIiwianRpIjoiMjQ3MDUwNDA5IiwiaWF0IjoxNjM0MjAyMzQzLCJpc3MiOiJDeXByZXNzIiwic3ViIjoiU1NUb2tlbiJ9.kmAi8PctmDUg0Ln2ThAHqpNcklPFmUmqwsfgOnqFWjI";
		private string P_SGTimeZone = "Singapore Standard Time";
		private string P_ESTimeZone = "Eastern Standard Time";
		private int P_TimeoutSec = 60000;
		private int P_PageSize = 500;
		Common _common = new Common();
		private CQ9Connector api = new CQ9Connector(Environment: BaseConnector.GameEnvironment.PROD);

		//###################################################################################
		#region Main Function 

		public bool GetGameRecord()
		{
			string startTime = GetJobTime("Start");
			string endTime = GetJobTime("End");
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

					CQ9.GetBetHistoryResponse resp = api.GetBetHistory(Convert.ToDateTime(mStartTime), Convert.ToDateTime(mEndTime), 1, P_PageSize);

					using (BingoConn db = new BingoConn())
					{
						string sMainID = Guid.NewGuid().ToString();
						CQ9GameMain cq9GameMain = new CQ9GameMain()
						{
							Id = sMainID,
							Total = resp != null && resp.status.code == "0" ? resp.data.Data.Count() : 0,
							Code = int.Parse(resp.status.code),
							StartTime = Convert.ToDateTime(startTime),
							EndTime = Convert.ToDateTime(endTime),
							CreatedDate = DateTime.Now
						};
						db.CQ9GameMain.Add(cq9GameMain);
						db.SaveChanges();

						if (resp != null && resp.status.code == "0")
						{
							foreach (CQ9.Datum obj in resp.data.Data)
							{
								string round = Convert.ToString(obj.round);

								try
								{
									CQ9Game cq9Game = new CQ9Game()
									{
										gamehall = obj.gamehall,
										gametype = obj.gametype,
										gameplat = obj.gameplat,
										gamecode = obj.gamecode,
										account = obj.account,
										round = obj.round,
										balance = obj.balance,
										win = obj.win,
										bet = obj.bet,
										validbet = obj.validbet,
										jackpot = obj.jackpot,
										jackpotcontribution = obj.jackpotcontribution != null ? JsonConvert.SerializeObject(obj.jackpotcontribution) : null,
										jackpottype = obj.jackpottype,
										status = obj.status,
										endroundtime = obj.endroundtime,
										createtime = obj.createtime,
										bettime = obj.bettime,
										detail = obj.detail != null ? JsonConvert.SerializeObject(obj.detail) : null,
										singlerowbet = obj.singlerowbet,
										gamerole = obj.gamerole != null ? JsonConvert.SerializeObject(obj.gamerole) : null,
										bankertype = obj.bankertype != null ? JsonConvert.SerializeObject(obj.bankertype) : null,
										rake = obj.rake.ToString(),
										roomfee = obj.roomfee.ToString(),
										tabletype = obj.tabletype != null ? JsonConvert.SerializeObject(obj.tabletype) : null,
										tableid = obj.tableid != null ? JsonConvert.SerializeObject(obj.tableid) : null,
										roundnumber = obj.roundnumber != null ? JsonConvert.SerializeObject(obj.roundnumber) : null,
										bettype = obj.bettype != null ? JsonConvert.SerializeObject(obj.bettype) : null,
										gameresult = obj.gameresult != null ? JsonConvert.SerializeObject(obj.gameresult) : null
									};

									var record = db.CQ9Game.Where(x => x.round == round).FirstOrDefault();

									if (record == null)
									{
										cq9Game.CreatedDate = DateTime.Now;
									}
									else
									{
										cq9Game.CreatedDate = record.CreatedDate;
										cq9Game.ModifiedDate = DateTime.Now;
									}
									db.CQ9Game.AddOrUpdate(cq9Game);
								}
								catch (Exception ex)
								{
									_common.LogError("Record " + round + " " + DateTime.Now.ToString() + " " + ex.Message.ToString(), "CQ9");
								}
							}
							db.SaveChanges();

							decimal dCnt = Convert.ToDecimal(resp.data.TotalSize) / Convert.ToDecimal(P_PageSize);
							int iPageIndex = Int32.Parse(Math.Ceiling(dCnt).ToString());

							if (iPageIndex > 1)
							{
								for (int x = 2; x <= iPageIndex; x++)
								{
									bool IsOK = GetGameRecordExtend(x, mStartTime, mEndTime);
								}
							}
						}

						if (Convert.ToString(resp.status.code) == "0" || Convert.ToString(resp.status.code) == "8")
						{
							bool isUpdJob = UpdJobTime(mStartTime, mEndTime);
						}
					}
				}

				return true;
			}
			catch (Exception ex)
			{
				_common.LogError("CQ9/GetGameRecord" + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), "CQ9");
				return false;
			}
		}

		public bool GetGameRecordExtend(int pageIndex, string startTime, string endTime)
		{
			CQ9.GetBetHistoryResponse resp = api.GetBetHistory(Convert.ToDateTime(startTime), Convert.ToDateTime(endTime), pageIndex, P_PageSize);

			using (BingoConn db = new BingoConn())
			{
				if (resp != null && resp.status.code == "0")
				{
					foreach (CQ9.Datum obj in resp.data.Data)
					{
						string round = Convert.ToString(obj.round);

						try
						{
							CQ9Game cq9Game = new CQ9Game()
							{
								gamehall = obj.gamehall,
								gametype = obj.gametype,
								gameplat = obj.gameplat,
								gamecode = obj.gamecode,
								account = obj.account,
								round = obj.round,
								balance = obj.balance,
								win = obj.win,
								bet = obj.bet,
								validbet = obj.validbet,
								jackpot = obj.jackpot,
								jackpotcontribution = obj.jackpotcontribution != null ? JsonConvert.SerializeObject(obj.jackpotcontribution) : null,
								jackpottype = obj.jackpottype,
								status = obj.status,
								endroundtime = obj.endroundtime,
								createtime = obj.createtime,
								bettime = obj.bettime,
								detail = obj.detail != null ? JsonConvert.SerializeObject(obj.detail) : null,
								singlerowbet = obj.singlerowbet,
								gamerole = obj.gamerole != null ? JsonConvert.SerializeObject(obj.gamerole) : null,
								bankertype = obj.bankertype != null ? JsonConvert.SerializeObject(obj.bankertype) : null,
								rake = obj.rake.ToString(),
								roomfee = obj.roomfee.ToString(),
								tabletype = obj.tabletype != null ? JsonConvert.SerializeObject(obj.tabletype) : null,
								tableid = obj.tableid != null ? JsonConvert.SerializeObject(obj.tableid) : null,
								roundnumber = obj.roundnumber != null ? JsonConvert.SerializeObject(obj.roundnumber) : null,
								bettype = obj.bettype != null ? JsonConvert.SerializeObject(obj.bettype) : null,
								gameresult = obj.gameresult != null ? JsonConvert.SerializeObject(obj.gameresult) : null
							};

							var record = db.CQ9Game.Where(x => x.round == round).FirstOrDefault();

							if (record == null)
							{
								cq9Game.CreatedDate = DateTime.Now;
							}
							else
							{
								cq9Game.CreatedDate = record.CreatedDate;
								cq9Game.ModifiedDate = DateTime.Now;
							}
							db.CQ9Game.AddOrUpdate(cq9Game);
						}
						catch (Exception ex)
						{
							_common.LogError("Record " + round + " " + DateTime.Now.ToString() + " " + ex.Message.ToString(), "CQ9");
						}
					}
					db.SaveChanges();
				}
			}

			return true;
		}

		public string ConvertDateTimeToLocal(string datetime)
		{
			try
			{
				DateTime temp = Convert.ToDateTime(datetime);
				var dt = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(new DateTimeOffset(temp), P_SGTimeZone);
				return dt.ToString("yyyy-MM-dd HH:mm:ss.fff");
			}
			catch
			{
				return null;
			}
		}

		public string ConvertDateTimeToEastern(string datetime)
		{
			try
			{
				DateTime temp = Convert.ToDateTime(datetime);
				return temp.AddHours(-12).ToString("yyyy-MM-dd'T'HH:mm:ss") + "-04:00";
				//var dt = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(new DateTimeOffset(temp), P_ESTimeZone);
				//return dt.ToString("yyyy-MM-dd'T'HH:mm:sszzz");
			}
			catch
			{
				return null;
			}
		}

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
				_common.LogError("CQ9/UpdJobTime" + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), "CQ9");
				return false;
			}
		}


		#endregion

		//###################################################################################
		#region Function 

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
						DateTime dtSTime = Convert.ToDateTime(DateTime.Now.ToString("yyyy-MM-dd HH:mm")) - new TimeSpan(0, 12, 0, 0, 0);
						sTime = dtSTime.ToString("yyyy-MM-dd HH:mm:ss");
					}
				}

				return sTime;

			}
			catch (Exception ex)
			{
				_common.LogError("CQ9/GetJobTime" + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), "CQ9");
				return "Error:" + ex.Message.ToString();
			}
		}

		#endregion

		// GET: CQ9
		public void CQ9Job()
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
				_common.LogError("Start - " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), "CQ9");
			}
		}
	}
}