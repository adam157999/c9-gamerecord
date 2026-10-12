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
	public class Evo888Controller : Controller
	{
		public static readonly object MyLock = new object();
		private string P_GamePrefix = "Evo888";
		private string P_ApiUrl = "https://api.evo888.io/api";
		private string P_Authcode = "EFA185D97BA814BD";
		private string P_Secretkey = "1621342DF609B11775E27515BABF4EEC";
		private int P_TimeoutSec = 1200000;
		private bool IsError = false;
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
			int limit = 20; //24 hours is max on evo888 side but we set to 30 mins to prevent more than 1000 records
			//string startTime = "2021-03-17 12:00:00";
			//string endTime = "2021-03-18 12:00:00";

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
					//_common.LogError(responseText, "Evo888");

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
									//if (Convert.ToDecimal(obj.Turnover) > 0)
									//{
									int[] gameTypeArr = { 0 };
									foreach (var gt in gameTypeArr)
									{
										string pStartTime = Convert.ToDateTime(startTime).AddMinutes(-30).ToString("yyyy-MM-dd HH:mm:ss"); //give half hour buffer for evo to finish the settlement
										string pEndTime = Convert.ToDateTime(endTime).ToString("yyyy-MM-dd HH:mm:ss");
										string mStartTime = pStartTime;
										string mEndTime = pEndTime;
										int MinDiff = Convert.ToInt32((Convert.ToDateTime(pEndTime) - Convert.ToDateTime(pStartTime)).TotalMinutes);
										int times = (MinDiff / limit) + 1;
										for (int j = limit; j <= times * limit; j += limit)
										{
											mStartTime = j == limit ? pStartTime : mEndTime;

											if ((Convert.ToDateTime(mStartTime).AddMinutes(limit) - Convert.ToDateTime(pEndTime)).TotalMinutes > 0)
												mEndTime = pEndTime;
											else
												mEndTime = Convert.ToString(Convert.ToDateTime(mStartTime).AddMinutes(limit));

											//_common.LogError(Convert.ToString(obj.UserName) + " " + mStartTime + " " + mEndTime + " " + gt.ToString(), "Evo888");

											GetGameRecord(Convert.ToString(obj.UserName), mStartTime, mEndTime, sMainID, gt);
										}
									}
									//}
								}

								if (Convert.ToString(result.success) == "true" && Convert.ToInt16(result.code) == 0 && !IsError)
								{
									bool isUpdJob = UpdJobTime(startTime, endTime, sMainID);
								}
							}
						}
						else
						{
							IsError = true;
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

		public bool GetGameRecord(string username, string sdate, string edate, string sMainID, int gameType)
		{
			try
			{
				decimal total = 0;
				int count = 0;
				string action = "getUserGameLog";
				long time = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
				string sign = MD5Encrypt((P_Authcode + username + time + P_Secretkey).ToLower());
				string param = string.Format("?action={0}&sdate={1}&edate={2}&time={3}&username={4}&gametype={5}&authcode={6}&sign={7}",
					action,
					HttpUtility.UrlEncode(Convert.ToDateTime(sdate).ToString("yyyy-MM-dd HH:mm:ss")),
					HttpUtility.UrlEncode(Convert.ToDateTime(edate).ToString("yyyy-MM-dd HH:mm:ss")),
					time,
					username,
					gameType,
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
					//_common.LogError(responseText, "Evo888");

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
											count++;
											total += Convert.ToDecimal(obj.Bet);
										}
									}
									catch (Exception ex)
									{
										_common.LogError("Record " + obj.Id + " " + DateTime.Now.ToString() + " " + ex.Message.ToString(), "Evo888");
									}
								}
								db.SaveChanges();
								//_common.LogError(username + " " + sdate + " " + edate + " " + gameType.ToString() + " " + total.ToString() + " " + count.ToString(), "Evo888");
							}
						}
						else
						{
							IsError = true;
						}
					}
				}
				return true;
			}
			catch (Exception ex)
			{
				_common.LogError("Evo888/GetGameRecord" + " " + DateTime.Now.ToString() + " " + username + " " + sdate + " " + edate + " " + gameType.ToString() + " " + ex.Message.ToString(), "Evo888");
				return false;
			}
		}

		//public bool GetGameRecordTest(string username, string sdate, string edate, string sMainID, int gameType)
		//{
		//	try
		//	{
		//		//decimal total = 0;
		//		//int count = 0;
		//		string action = "getUserGameLog";
		//		long time = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
		//		string sign = MD5Encrypt((P_Authcode + username + time + P_Secretkey).ToLower());
		//		string param = string.Format("?action={0}&sdate={1}&edate={2}&time={3}&username={4}&gametype={5}&authcode={6}&sign={7}",
		//			action,
		//			HttpUtility.UrlEncode(Convert.ToDateTime(sdate).ToString("yyyy-MM-dd HH:mm:ss")),
		//			HttpUtility.UrlEncode(Convert.ToDateTime(edate).ToString("yyyy-MM-dd HH:mm:ss")),
		//			time,
		//			username,
		//			gameType,
		//			P_Authcode,
		//			sign);
		//		string webAddr = P_ApiUrl + param;

		//		var httpWebRequest = (HttpWebRequest)WebRequest.Create(webAddr);
		//		httpWebRequest.ContentType = "application/json";
		//		httpWebRequest.Method = "GET";
		//		httpWebRequest.Timeout = P_TimeoutSec;
		//		httpWebRequest.ReadWriteTimeout = P_TimeoutSec;

		//		var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
		//		using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
		//		{
		//			var responseText = streamReader.ReadToEnd();
		//			//_common.LogError(responseText, "Evo888");

		//			dynamic result = JObject.Parse(responseText);

		//			using (BingoConn db = new BingoConn())
		//			{
		//				sMainID = Guid.NewGuid().ToString();
		//				Evo888GameMain evoGameMain = new Evo888GameMain()
		//				{
		//					Id = sMainID,
		//					Code = result.code,
		//					Msg = result.msg,
		//					StartTime = Convert.ToDateTime(sdate),
		//					EndTime = Convert.ToDateTime(edate),
		//					CreatedDate = DateTime.Now
		//				};
		//				db.Evo888GameMain.Add(evoGameMain);
		//				db.SaveChanges();

		//				if (Convert.ToString(result.success) == "true" && Convert.ToInt16(result.code) == 0)
		//				{
		//					if (result.results.gameLog != null)
		//					{
		//						foreach (var obj in result.results.gameLog)
		//						{
		//							try
		//							{
		//								if (Convert.ToString(obj.GameName).Trim().ToUpper() != "SETSCORE")
		//								{
		//									Evo888Game evoGame = new Evo888Game()
		//									{
		//										fId = sMainID,
		//										Id = obj.Id,
		//										UserName = username,
		//										GameName = obj.GameName,
		//										TableID = obj.TableID,
		//										Bet = Convert.ToDecimal(obj.Bet),
		//										Win = Convert.ToDecimal(obj.Win),
		//										BeginMoney = Convert.ToDecimal(obj.BeginMoney),
		//										EndMoney = Convert.ToDecimal(obj.EndMoney),
		//										DateTime = Convert.ToDateTime(obj.DateTime),
		//									};

		//									string Id = Convert.ToString(obj.Id);
		//									var record = db.Evo888Game.Where(x => x.Id == Id).FirstOrDefault();
		//									if (record == null)
		//									{
		//										evoGame.CreatedDate = DateTime.Now;
		//									}
		//									else
		//									{
		//										evoGame.CreatedDate = record.CreatedDate;
		//										evoGame.ModifiedDate = DateTime.Now;
		//									}
		//									db.Evo888Game.AddOrUpdate(evoGame);
		//									//count++;
		//									//total += Convert.ToDecimal(obj.Bet);
		//								}
		//							}
		//							catch (Exception ex)
		//							{
		//								_common.LogError("Record " + obj.Id + " " + DateTime.Now.ToString() + " " + ex.Message.ToString(), "Evo888");
		//							}
		//						}
		//						db.SaveChanges();
		//						//_common.LogError(username + " " + sdate + " " + edate + " " + gameType.ToString() + " " + total.ToString() + " " + count.ToString(), "Evo888");


		//					}
		//				}
		//				else
		//				{
		//					IsError = true;
		//				}

		//			}
		//		}
		//		return true;
		//	}
		//	catch (Exception ex)
		//	{
		//		_common.LogError("Evo888/GetGameRecord" + " " + DateTime.Now.ToString() + " " + ex.Message.ToString(), "Evo888");
		//		return false;
		//	}
		//}


		public void Evo888Job(string sdate = "", string edate = "")
		{
			try
			{
				lock (MyLock)
				{
					bool isOK = GetPlayerList();
					//bool isOK = GetGameRecordTest("01584467585", "2021-02-16 00:00:00", "2021-02-16 06:00:00", "asasas", 0);
					//bool isOK = GetGameRecordTest("01584467585", sdate, edate, "asasas", 0);

				}
			}
			catch (Exception ex)
			{
				_common.LogError("Start - " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), "Evo888");
			}
		}
	}
}