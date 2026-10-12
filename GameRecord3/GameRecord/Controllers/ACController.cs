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
using System.Security.Cryptography.X509Certificates;
using Newtonsoft.Json.Linq;

namespace GameRecord.Controllers
{
	public class ACController : Controller
	{
		public static readonly object MyLock = new object();
		private string P_GamePrefix = "AC";
		//private string P_ApiUrl = "http://47.115.24.233:8033/ws/service.ashx";
		//private string P_AgentName = "MLTeoh";
		//private string P_AgentPassword = "Ab123456";

		private string P_ApiUrl = "http://allcity.vip:8033/ws/service.ashx";
		private string P_AgentName = "MLTeoh";
		private string P_AgentPassword = "MCallcity1001admin";
		//private string P_AgentPassword = "Tuh864oy";
		private int P_TimeoutSec = 300000;
		Common _common = new Common();

		//###################################################################################
		#region Declare 

		public class GameRecordResponse
		{
			public List<GameRecordData> msg { get; set; }
			public int code { get; set; }
		}

		public class GameRecordData
		{
			public string account { get; set; }
			public string gameid { get; set; }
			public decimal balancebefore { get; set; }
			public decimal totalplayed { get; set; }
			public decimal totalwin { get; set; }
			public decimal balancenow { get; set; }
			public string reason { get; set; }
			public DateTime inserttime { get; set; }
		}

		#endregion

		public bool UpdJobTime(string startTime, string endTime, string sMainID)
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

						bingoConn.Entry(xyz).State = System.Data.Entity.EntityState.Modified;
						bingoConn.SaveChanges();
					}
				}
				return true;
			}
			catch (Exception ex)
			{
				_common.LogError("AC/UpdJobTime" + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), "AC");
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
						DateTime dtSTime = Convert.ToDateTime(DateTime.Now.ToString("yyyy-MM-dd HH:mm")) - new TimeSpan(0, 0, 3, 0, 0);
						sTime = dtSTime.ToString("yyyy-MM-dd HH:mm:ss");
					}
				}

				return sTime;

			}
			catch (Exception ex)
			{
				_common.LogError("AC/GetJobTime" + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), "AC");
				return "Error:" + ex.Message.ToString();
			}
		}

		public string MD5Encrypt(string text)
		{
			byte[] ba = System.Security.Cryptography.MD5.Create().ComputeHash(System.Text.ASCIIEncoding.ASCII.GetBytes(text));
			string hex = BitConverter.ToString(ba).ToLower();
			hex = hex.Replace("-", "");

			return hex;
		}

		public bool GetGameRecord()
		{
			string startTime = GetJobTime("Start");
			string endTime = GetJobTime("End");
			string mStartTime = startTime;
			string mEndTime = endTime;
			int limit = 60;

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
					{
						mEndTime = Convert.ToString(Convert.ToDateTime(mStartTime).AddMinutes(limit));
					}

					string action = "getgamelog";
					long time = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
					string sign = MD5Encrypt((P_AgentName.ToLower() + time.ToString() + MD5Encrypt(P_AgentPassword)).ToLower());
					string param = string.Format("?action={0}&fromdate={1}&todate={2}&agentname={3}&time={4}&sign={5}",
						action,
						Convert.ToDateTime(mStartTime).ToString("yyyy-MM-dd HH:mm:ss").Replace(" ", "%20"),
						Convert.ToDateTime(mEndTime).ToString("yyyy-MM-dd HH:mm:ss").Replace(" ", "%20"),
						P_AgentName,
						time,
						sign);
					string webAddr = P_ApiUrl + param;

					var httpWebRequest = (HttpWebRequest)WebRequest.Create(webAddr);
					httpWebRequest.ContentType = "application/json";
					httpWebRequest.Method = "GET";
					httpWebRequest.Timeout = P_TimeoutSec;
					httpWebRequest.ReadWriteTimeout = P_TimeoutSec;

					var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
					using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
					{
						var responseText = streamReader.ReadToEnd();
						dynamic result = JObject.Parse(responseText);

						using (BingoConn db = new BingoConn())
						{
							string sMainID = Guid.NewGuid().ToString();
							ACGameMain acGameMain = new ACGameMain()
							{
								Id = sMainID,
								Total = (result.msg != null && result.code == 200) ? result.msg.Count : 0,
								Code = result.code,
								StartTime = Convert.ToDateTime(mStartTime),
								EndTime = Convert.ToDateTime(mEndTime),
								CreatedDate = DateTime.Now
							};
							db.ACGameMains.Add(acGameMain);
							db.SaveChanges();

							if (result.code == 200)
							{
								foreach (var obj in result.msg)
								{
									string id = Convert.ToString(obj.id);
									try
									{
										if (db.ACGames.Where(x => x.id == id).FirstOrDefault() == null)
										{
											ACGame acGame = new ACGame()
											{
												id = obj.id,
												fId = sMainID,
												AgentID = obj.AgentID,
												GameCode = obj.GameCode,
												Account = obj.Account,
												UserID = obj.UserID,
												OpenType = obj.OpenType,
												TotalPlay = Convert.ToDecimal(obj.TotalPlay) / 100,
												TotalWin = Convert.ToDecimal(obj.TotalWin) / 100,
												Credit = Convert.ToDecimal(obj.Credit) / 100,
												time = obj.time,
												CreatedDate = DateTime.Now
											};
											db.ACGames.Add(acGame);
										}
									}
									catch (Exception ex)
									{
										_common.LogError("Record " + id + " " + DateTime.Now.ToString() + " " + ex.Message.ToString(), "AC");
									}
								}
								db.SaveChanges();
							}

							if (result.code == 200)
							{
								bool isUpdJob = UpdJobTime(mStartTime, mEndTime, sMainID);
							}
						}
					}
				}
				return true;
			}
			catch (Exception ex)
			{
				_common.LogError("AC/GetGameRecord" + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), "AC");
				return false;
			}
		}

		public void ACJob()
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
				_common.LogError("Start - " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), "AC");
			}
		}
	}
}