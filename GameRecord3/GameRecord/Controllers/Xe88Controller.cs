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
	using System.Globalization;

namespace GameRecord.Controllers
{
	public class Xe88Controller : Controller
	{
		public static readonly object MyLock = new object();
		//private string P_ApiUrl = "https://api.fatchoy888.com";
		private string P_ApiUrl = "https://api.multi888.com";
		private string P_AgentID = "MCityAgent";
		private string P_SignatureKey = "fb103665-cb1f-4233-bcc2-4cf9244833b5";
		private int P_TimeoutSec = 120000;
		private string P_FilePrefix = "XE88";
		private string P_GamePrefix = "Xe88";
		private int P_PageSize = 500;
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
				_common.LogError("Xe88/UpdJobTime" + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), P_FilePrefix);
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
				_common.LogError("Spade/GetJobTime" + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), "Spade");
				return "Error:" + ex.Message.ToString();
			}
		}

		public string CreateSign(string message, string secret)
		{
			secret = secret ?? "";
			var encoding = new System.Text.ASCIIEncoding();
			byte[] keyByte = encoding.GetBytes(secret);
			byte[] messageBytes = encoding.GetBytes(message);
			using (var hmacsha256 = new HMACSHA256(keyByte))
			{
				byte[] hashmessage = hmacsha256.ComputeHash(messageBytes);
				return Convert.ToBase64String(hashmessage);
			}
		}

		public bool GetGameRecord()
		{
			string sMainID = Guid.NewGuid().ToString();
			string startTime = GetJobTime("Start");
			string endTime = GetJobTime("End");
			if (Convert.ToDateTime(startTime).ToString("yyyy-MM-dd") != Convert.ToDateTime(endTime).ToString("yyyy-MM-dd"))
			{
				if (Convert.ToDateTime(startTime).ToString("HH:mm:ss.fff") == "23:59:59.999")
					startTime = Convert.ToDateTime(startTime).AddDays(1).ToString("yyyy-MM-dd") + " 00:00:00.000";
				else
					endTime = Convert.ToDateTime(startTime).ToString("yyyy-MM-dd") + " 23:59:59.999";
			}

			try
			{
				dynamic request = new ExpandoObject();
				request.agentid = P_AgentID;
				request.date = Convert.ToDateTime(startTime).ToString("yyyy-MM-dd");
				request.starttime = Convert.ToDateTime(startTime).ToString("HH:mm:ss");
				request.endtime = Convert.ToDateTime(endTime).ToString("HH:mm:ss");
				request.page = 1;
				request.perpage = P_PageSize;

				string requestStr = JsonConvert.SerializeObject(request);
				string hashKey = CreateSign(requestStr, P_SignatureKey);

				string webAddr = $"{P_ApiUrl}/customreport/playergamelog";
				var httpWebRequest = (HttpWebRequest)WebRequest.Create(webAddr);

				httpWebRequest.ContentType = "application/json";
				httpWebRequest.Method = "POST";
				httpWebRequest.Timeout = P_TimeoutSec;
				httpWebRequest.ReadWriteTimeout = P_TimeoutSec;
				httpWebRequest.Headers.Add("hashkey", hashKey);

				using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
				{
					streamWriter.Write(requestStr);
					streamWriter.Flush();
				}

				var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
				using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
				{
					var responseText = streamReader.ReadToEnd();
					dynamic result = JObject.Parse(responseText);

					using (BingoConn db = new BingoConn())
					{
						Xe88GameMain xe88GameMain = new Xe88GameMain()
						{
							Id = sMainID,
							Total = result.code == 0 ? Convert.ToInt32(result.pagination.totalcount) : 0,
							Code = result.code,
							Msg = result.message,
							StartTime = Convert.ToDateTime(startTime),
							EndTime = Convert.ToDateTime(endTime),
							CreatedDate = DateTime.Now
						};
						db.Xe88GameMain.Add(xe88GameMain);
						db.SaveChanges();

						if (result.code == 0)
						{
							if (Convert.ToInt32(result.pagination.totalcount) > 0)
							{
								foreach (var obj in result.result)
								{
									if (Convert.ToString(obj.gamename).Trim().ToUpper() != "SETSCORE")
									{
										string id = Convert.ToString(obj.id);

										try
										{
											Xe88Game xe88Game = new Xe88Game()
											{
												fId = sMainID,
												id = obj.id,
												username = obj.username,
												gameid = obj.gameid,
												gamename = obj.gamename,
												bet = obj.bet != null ? Convert.ToDecimal(obj.bet) : null,
												win = obj.win != null ? Convert.ToDecimal(obj.win) : null,
												beginmoney = obj.beginmoney != null ? Convert.ToDecimal(obj.beginmoney) : null,
												endmoney = obj.endmoney != null ? Convert.ToDecimal(obj.endmoney) : null,
												gamelog = obj.gamelog,
												logtime = obj.logtime != null ? Convert.ToDateTime(obj.logtime) : null
											};

											var record = db.Xe88Game.Where(x => x.id == id).FirstOrDefault();

											if (record == null)
											{
												xe88Game.CreatedDate = DateTime.Now;
											}
											else
											{
												xe88Game.CreatedDate = record.CreatedDate;
												xe88Game.ModifiedDate = DateTime.Now;
											}
											db.Xe88Game.AddOrUpdate(xe88Game);
										}
										catch (Exception ex)
										{
											_common.LogError("Record " + id + " " + DateTime.Now.ToString() + " " + ex.Message.ToString(), P_FilePrefix);
										}
									}
								}
								db.SaveChanges();

								if (Convert.ToInt32(result.pagination.totalpages) > 1)
								{
									for (int x = 2; x <= Convert.ToInt32(result.pagination.totalpages); x++)
									{
										bool IsOK = GetGameRecordExtend(x, startTime, endTime, sMainID);
									}
								}
							}
						}

						if (result.code == 0)
						{
							bool isUpdJob = UpdJobTime(startTime, endTime, sMainID);
						}
					}
				}

				return true;
			}
			catch (Exception ex)
			{
				_common.LogError("Xe88/GetGameRecord" + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), P_FilePrefix);
				return false;
			}
		}

		public bool GetGameRecordExtend(int pageIndex, string startTime, string endTime, string sMainID)
		{
			try
			{
				dynamic request = new ExpandoObject();
				request.agentid = P_AgentID;
				request.date = Convert.ToDateTime(startTime).ToString("yyyy-MM-dd");
				request.starttime = Convert.ToDateTime(startTime).ToString("HH:mm:ss");
				request.endtime = Convert.ToDateTime(endTime).ToString("HH:mm:ss");
				request.page = pageIndex;
				request.perpage = P_PageSize;

				string requestStr = JsonConvert.SerializeObject(request);
				string hashKey = CreateSign(requestStr, P_SignatureKey);

				string webAddr = $"{P_ApiUrl}/customreport/playergamelog";
				var httpWebRequest = (HttpWebRequest)WebRequest.Create(webAddr);

				httpWebRequest.ContentType = "application/json";
				httpWebRequest.Method = "POST";
				httpWebRequest.Timeout = P_TimeoutSec;
				httpWebRequest.ReadWriteTimeout = P_TimeoutSec;
				httpWebRequest.Headers.Add("hashkey", hashKey);

				using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
				{
					streamWriter.Write(requestStr);
					streamWriter.Flush();
				}

				var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
				using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
				{
					var responseText = streamReader.ReadToEnd();
					dynamic result = JObject.Parse(responseText);

					using (BingoConn db = new BingoConn())
					{
						if (result.code == 0)
						{
							if (Convert.ToInt32(result.pagination.totalcount) > 0)
							{
								foreach (var obj in result.result)
								{
									if (Convert.ToString(obj.gamename).Trim().ToUpper() != "SETSCORE")
									{
										string id = Convert.ToString(obj.id);

										try
										{
											Xe88Game xe88Game = new Xe88Game()
											{
												fId = sMainID,
												id = obj.id,
												username = obj.username,
												gameid = obj.gameid,
												gamename = obj.gamename,
												bet = obj.bet != null ? Convert.ToDecimal(obj.bet) : null,
												win = obj.win != null ? Convert.ToDecimal(obj.win) : null,
												beginmoney = obj.beginmoney != null ? Convert.ToDecimal(obj.beginmoney) : null,
												endmoney = obj.endmoney != null ? Convert.ToDecimal(obj.endmoney) : null,
												gamelog = obj.gamelog,
												logtime = obj.logtime != null ? Convert.ToDateTime(obj.logtime) : null
											};

											var record = db.Xe88Game.Where(x => x.id == id).FirstOrDefault();

											if (record == null)
											{
												xe88Game.CreatedDate = DateTime.Now;
											}
											else
											{
												xe88Game.CreatedDate = record.CreatedDate;
												xe88Game.ModifiedDate = DateTime.Now;
											}
											db.Xe88Game.AddOrUpdate(xe88Game);
										}
										catch (Exception ex)
										{
											_common.LogError("Record " + id + " " + DateTime.Now.ToString() + " " + ex.Message.ToString(), P_FilePrefix);
										}
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
				_common.LogError("Xe88/GetGameRecordExtend" + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), P_FilePrefix);
				throw;
			}
		}

		public void Xe88Job()
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
	}
}