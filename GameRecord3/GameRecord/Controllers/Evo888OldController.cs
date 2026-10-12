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

namespace GameRecord.Controllers
{
	public class Evo888OldController : Controller
	{
		public static readonly object MyLock = new object();
		private string P_GamePrefix = "Evo888";
		private string P_ApiUrl = "https://api.evo888.io/api";
		private string P_Authcode = "EFA185D97BA814BD";
		private string P_Secretkey = "1621342DF609B11775E27515BABF4EEC";
		private int P_TimeoutSec = 1200000;
		Common _common = new Common();

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
				_common.LogError("Evo888/UpdJobTime" + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), "Evo888");
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
				_common.LogError("Evo888/GetJobTime" + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), "Evo888");
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

		public bool GetPlayerList()
		{
			string startTime = GetJobTime("Start");
			string endTime = GetJobTime("End");
			//string startTime = "2021-02-11 12:00:00";
			//string endTime = "2021-02-12 12:00:00";

			try
			{
				string startDate = Convert.ToDateTime(startTime).AddHours(-1).ToString("yyyy-MM-dd"); //give 1 hour buffer to get the previous day data
				string endDate = Convert.ToDateTime(endTime).ToString("yyyy-MM-dd");
				string action = "getTotalReport";
				long time = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
				string sign = MD5Encrypt((P_Authcode + time + P_Secretkey).ToLower());
				string param = string.Format("?action={0}&sdate={1}&edate={2}&time={3}&type={4}&authcode={5}&sign={6}",
					action,
					startDate,
					endDate,
					time,
					0,
					P_Authcode,
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
						Evo888GameMain evoGameMain = new Evo888GameMain()
						{
							Id = sMainID,
							Code = result.code,
							Msg = result.msg,
							StartTime = Convert.ToDateTime(startTime),
							EndTime = Convert.ToDateTime(endTime),
							CreatedDate = DateTime.Now
						};
						db.Evo888GameMain.Add(evoGameMain);
						db.SaveChanges();

						if (Convert.ToString(result.success) == "true" && Convert.ToInt16(result.code) == 0)
						{
							if (result.results.report != null)
							{
								foreach (var obj in result.results.report)
								{
									if (Convert.ToDecimal(obj.Turnover) > 0)
									{
										int DayDiff = (Convert.ToDateTime(endDate).Date - Convert.ToDateTime(startDate).Date).Days;

										if (DayDiff > 0)
										{
											for (int i = 0; i <= DayDiff; i++)
											{
												string pDate = Convert.ToDateTime(startDate).AddDays(i).ToString("yyyy-MM-dd");
												GetGameRecord(Convert.ToString(obj.UserName), pDate, sMainID);
											}
										}
										else
										{
											GetGameRecord(Convert.ToString(obj.UserName), Convert.ToDateTime(startDate).ToString("yyyy-MM-dd"), sMainID);
										}
									}
								}

								if (Convert.ToString(result.success) == "true" && Convert.ToInt16(result.code) == 0)
								{
									bool isUpdJob = UpdJobTime(startTime, endTime, sMainID);
								}
							}
						}
					}
				}
				return true;
			}
			catch (Exception ex)
			{
				_common.LogError("Evo888/GetPlayerList" + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), "Evo888");
				return false;
			}
		}

		public bool GetGameRecord(string username, string date, string sMainID)
		{
			try
			{
				string action = "getUserGameLog";
				long time = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
				string sign = MD5Encrypt((P_Authcode + username + time + P_Secretkey).ToLower());
				string param = string.Format("?action={0}&date={1}&time={2}&type={3}&username={4}&authcode={5}&sign={6}",
					action,
					date,
					time,
					0,
					username,
					P_Authcode,
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
						if (Convert.ToString(result.success) == "true" && Convert.ToInt16(result.code) == 0)
						{
							if (result.results.gameLog != null)
							{
								foreach (var obj in result.results.gameLog)
								{
									try
									{
										if (Convert.ToString(obj.GameName).Trim().ToUpper() != "SETSCORE")
										{
											Evo888Game evoGame = new Evo888Game()
											{
												fId = sMainID,
												Id = obj.Id,
												UserName = username,
												GameName = obj.GameName,
												TableID = obj.TableID,
												Bet = Convert.ToDecimal(obj.Bet),
												Win = Convert.ToDecimal(obj.Win),
												BeginMoney = Convert.ToDecimal(obj.BeginMoney),
												EndMoney = Convert.ToDecimal(obj.EndMoney),
												DateTime = Convert.ToDateTime(obj.DateTime),
											};

											string Id = Convert.ToString(obj.Id);
											var record = db.Evo888Game.Where(x => x.Id == Id).FirstOrDefault();
											if (record == null)
											{
												evoGame.CreatedDate = DateTime.Now;
											}
											else
											{
												evoGame.CreatedDate = record.CreatedDate;
												evoGame.ModifiedDate = DateTime.Now;
											}
											db.Evo888Game.AddOrUpdate(evoGame);
										}
									}
									catch (Exception ex)
									{
										_common.LogError("Record " + obj.Id + " " + DateTime.Now.ToString() + " " + ex.Message.ToString(), "Evo888");
									}
								}
								db.SaveChanges();
							}
						}
					}
				}
				return true;
			}
			catch (Exception ex)
			{
				_common.LogError("Evo888/GetGameRecord" + " " + DateTime.Now.ToString() + " " + ex.Message.ToString(), "Evo888");
				return false;
			}
		}

		public void Evo888Job()
		{
			try
			{
				lock (MyLock)
				{
					bool isOK = GetPlayerList();
				}
			}
			catch (Exception ex)
			{
				_common.LogError("Start - " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), "Evo888");
			}
		}
	}
}