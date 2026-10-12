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
	public class Menang88Controller : Controller
	{
		public static readonly object MyLock = new object();
		//private string P_ApiUrl = "https://menang88.live/api/";
		private string P_ApiUrl = "https://556699.co/api/";
		private string P_ApiToken = "53rA5rpcf2I8KEzUeRydSQ%3d%3d";
		private string P_FilePrefix = "MENANG88";
		private int P_TimeoutSec = 120000;
		private string P_GamePrefix = "Menang88";
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
				_common.LogError("Menang88/UpdJobTime" + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), P_FilePrefix);
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
				_common.LogError("Menang88/GetJobTime" + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), P_FilePrefix);
				return "Error:" + ex.Message.ToString();
			}
		}

		public void GetDrawDateList()
		{
			try
			{
				string method = "DrawDateList.aspx";
				string param = $"?token={P_ApiToken}";
				string webAddr = P_ApiUrl + method + param;
				var httpWebRequest = (HttpWebRequest)WebRequest.Create(webAddr);
				httpWebRequest.ContentType = "application/json";
				httpWebRequest.Method = "POST";
				httpWebRequest.Timeout = P_TimeoutSec;
				httpWebRequest.ReadWriteTimeout = P_TimeoutSec;
				httpWebRequest.ContentType = "application/json";
				httpWebRequest.ContentLength = 0;

				var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
				using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
				{
					var responseText = streamReader.ReadToEnd();

					dynamic result = JObject.Parse(responseText);

					if (Convert.ToString(result.status) == "1")
					{
						if (result.betdates != null)
						{
							using (BingoConn db = new BingoConn())
							{
								foreach (var obj in result.betdates)
								{
									DateTime drawDate = Convert.ToDateTime(obj.drawdate);

									Menang88GameDrawDate m = new Menang88GameDrawDate()
									{
										drawno = obj.drawno,
										drawdate = obj.drawdate != null ? Convert.ToDateTime(obj.drawdate) : null,
										tekateki = obj.tekateki,
										totalno = obj.totalno,
										maxpayout = obj.maxpayout != null ? Convert.ToDecimal(obj.maxpayout) : null,
										IsFinish = false,
									};

									var record = db.Menang88GameDrawDate.Where(x => x.drawdate == drawDate).FirstOrDefault();

									if (record == null)
									{
										m.CreatedDate = DateTime.Now;
									}
									else
									{
										m.CreatedDate = record.CreatedDate;
										m.ModifiedDate = DateTime.Now;
									}
									db.Menang88GameDrawDate.AddOrUpdate(m);
								}
								db.SaveChanges();
							}
						}
					}
				}
			}
			catch (Exception ex)
			{
				_common.LogError("GetDrawDateList" + " " + DateTime.Now.ToString() + " " + ex.Message.ToString(), P_FilePrefix);
			}
		}

		public void CheckResultOut()
		{
			try
			{
				using (BingoConn db = new BingoConn())
				{
					var pendingDraw = db.Menang88GameDrawDate.Where(x => x.IsFinish != true).ToList();
					foreach (var p in pendingDraw)
					{
						string method = "Results.aspx";
						string param = $"?token={P_ApiToken}&dte={Convert.ToDateTime(p.drawdate).ToString("yyyy-MM-dd HH:mm:ss")}";
						string webAddr = P_ApiUrl + method + param;
						var httpWebRequest = (HttpWebRequest)WebRequest.Create(webAddr);
						httpWebRequest.ContentType = "application/json";
						httpWebRequest.Method = "POST";
						httpWebRequest.Timeout = P_TimeoutSec;
						httpWebRequest.ReadWriteTimeout = P_TimeoutSec;
						httpWebRequest.ContentType = "application/json";
						httpWebRequest.ContentLength = 0;

						var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
						using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
						{
							var responseText = streamReader.ReadToEnd();

							dynamic result = JObject.Parse(responseText);

							if (Convert.ToString(result.status) == "1")
							{
								if (result.betresults != null)
								{
									if (result.betresults.Count > 0)
									{
										p.IsFinish = true;
										db.SaveChanges();
									}
								}
							}
						}
					}
				}
			}
			catch (Exception ex)
			{
				_common.LogError("CheckResultOut" + " " + DateTime.Now.ToString() + " " + ex.Message.ToString(), P_FilePrefix);
			}
		}

		public bool GetGameRecord()
		{
			string startTime = GetJobTime("Start");
			string endTime = GetJobTime("End");
			string sMainID = Guid.NewGuid().ToString();

			try
			{
				using (BingoConn db = new BingoConn())
				{
					Menang88GameMain gameMain = new Menang88GameMain()
					{
						Id = sMainID,
						Total = null,
						StartTime = Convert.ToDateTime(startTime),
						EndTime = Convert.ToDateTime(endTime),
						CreatedDate = DateTime.Now
					};
					db.Menang88GameMain.Add(gameMain);
					db.SaveChanges();

					GetDrawDateList();
					var pendingDraw = db.Menang88GameDrawDate.Where(x => x.IsFinish != true).ToList();
					foreach (var p in pendingDraw)
					{
						string method = "BetListMax.aspx";
						string param = $"?token={P_ApiToken}&dte={Convert.ToDateTime(p.drawdate).ToString("yyyy-MM-dd HH:mm:ss")}&pge=0";
						string webAddr = P_ApiUrl + method + param;
						var httpWebRequest = (HttpWebRequest)WebRequest.Create(webAddr);
						httpWebRequest.ContentType = "application/json";
						httpWebRequest.Method = "POST";
						httpWebRequest.Timeout = P_TimeoutSec;
						httpWebRequest.ReadWriteTimeout = P_TimeoutSec;
						httpWebRequest.ContentType = "application/json";
						httpWebRequest.ContentLength = 0;

						var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
						using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
						{
							var responseText = streamReader.ReadToEnd();

							dynamic result = JObject.Parse(responseText);

							if (Convert.ToString(result.status) == "1")
							{
								if (result.betdatas != null)
								{
									foreach (var b in result.betdatas)
									{
										string betID = Convert.ToString(b.betid);

										Menang88Game menangGame = new Menang88Game()
										{
											fId = sMainID,
											resitid = b.resitid,
											betid = b.betid,
											ip = b.ip,
											account = b.account,
											username = b.username,
											logdate = b.logdate != null ? Convert.ToDateTime(b.logdate) : null,
											betnumber = b.betnumber,
											bettype = b.bettype,
											stake = b.stake != null ? Convert.ToDecimal(b.stake) : null,
											payout = b.payout != null ? Convert.ToDecimal(b.payout) : null,
											status = b.status,
											strike = b.strike != null ? Convert.ToDecimal(b.strike) : null,
											drawdate = b.drawdate != null ? Convert.ToDateTime(b.drawdate) : null
										};

										var record = db.Menang88Game.Where(x => x.betid == betID).FirstOrDefault();

										if (record == null)
										{
											menangGame.CreatedDate = DateTime.Now;
										}
										else
										{
											menangGame.CreatedDate = record.CreatedDate;
											menangGame.ModifiedDate = DateTime.Now;
										}
										db.Menang88Game.AddOrUpdate(menangGame);
									}
									db.SaveChanges();
								}

								if (result.totalpages != null)
								{
									if (result.totalpages > 1)
									{
										int totalPages = Convert.ToInt16(result.totalpages);
										for (int x = 1; x <= totalPages; x++)
										{
											bool IsOK = GetGameRecordExtend(x, p.drawdate, sMainID);
										}
									}
								}
							}
						}
					}

					CheckResultOut();
				}

				bool isUpdJob = UpdJobTime(startTime, endTime, sMainID);

				return true;
			}
			catch (DbEntityValidationException e)
			{
				_common.LogError("Menang88/GetGameRecord" + " " + DateTime.Now.ToString() + "  " + e.Message.ToString(), P_FilePrefix);

				foreach (var eve in e.EntityValidationErrors)
				{
					Console.WriteLine("Entity of type \"{0}\" in state \"{1}\" has the following validation errors:",
													eve.Entry.Entity.GetType().Name, eve.Entry.State);
					foreach (var ve in eve.ValidationErrors)
					{
						Console.WriteLine("- Property: \"{0}\", Error: \"{1}\"",
														ve.PropertyName, ve.ErrorMessage);
					}
				}
				return false;
			}
			catch (Exception ex)
			{
				_common.LogError("Menang88/GetGameRecord" + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), P_FilePrefix);
				return false;
			}
		}

		public bool GetGameRecordExtend(int page, DateTime drawDate, string sMainID)
		{
			try
			{
				string method = "BetListMax.aspx";
				string param = $"?token={P_ApiToken}&dte={drawDate.ToString("yyyy-MM-dd HH:mm:ss")}&pge=" + page.ToString();
				string webAddr = P_ApiUrl + method + param;
				var httpWebRequest = (HttpWebRequest)WebRequest.Create(webAddr);
				httpWebRequest.ContentType = "application/json";
				httpWebRequest.Method = "POST";
				httpWebRequest.Timeout = P_TimeoutSec;
				httpWebRequest.ReadWriteTimeout = P_TimeoutSec;
				httpWebRequest.ContentType = "application/json";
				httpWebRequest.ContentLength = 0;

				var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
				using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
				{
					var responseText = streamReader.ReadToEnd();

					dynamic result = JObject.Parse(responseText);

					if (Convert.ToString(result.status == "1"))
					{
						if (result.betdatas != null)
						{
							using (BingoConn db = new BingoConn())
							{
								foreach (var b in result.betdatas)
								{

									string betID = Convert.ToString(b.betid);

									Menang88Game menangGame = new Menang88Game()
									{
										fId = sMainID,
										resitid = b.resitid,
										betid = b.betid,
										ip = b.ip,
										account = b.account,
										username = b.username,
										logdate = b.logdate != null ? Convert.ToDateTime(b.logdate) : null,
										betnumber = b.betnumber,
										bettype = b.bettype,
										stake = b.stake != null ? Convert.ToDecimal(b.stake) : null,
										payout = b.payout != null ? Convert.ToDecimal(b.payout) : null,
										status = b.status,
										strike = b.strike != null ? Convert.ToDecimal(b.strike) : null,
										drawdate = b.drawdate != null ? Convert.ToDateTime(b.drawdate) : null
									};

									var record = db.Menang88Game.Where(x => x.betid == betID).FirstOrDefault();

									if (record == null)
									{
										menangGame.CreatedDate = DateTime.Now;
									}
									else
									{
										menangGame.CreatedDate = record.CreatedDate;
										menangGame.ModifiedDate = DateTime.Now;
									}
									db.Menang88Game.AddOrUpdate(menangGame);
								}
								db.SaveChanges();
							}
						}
					}

					return true;
				}
			}
			catch (DbEntityValidationException e)
			{
				_common.LogError("Menang88/GetGameRecord" + " " + DateTime.Now.ToString() + "  " + e.Message.ToString(), P_FilePrefix);

				foreach (var eve in e.EntityValidationErrors)
				{
					Console.WriteLine("Entity of type \"{0}\" in state \"{1}\" has the following validation errors:",
													eve.Entry.Entity.GetType().Name, eve.Entry.State);
					foreach (var ve in eve.ValidationErrors)
					{
						Console.WriteLine("- Property: \"{0}\", Error: \"{1}\"",
														ve.PropertyName, ve.ErrorMessage);
					}
				}
				return false;
			}
			catch (Exception ex)
			{
				_common.LogError("Menang88/GetGameRecordExtend" + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), P_FilePrefix);
				return false;
			}
		}

		public void Menang88Job()
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
				_common.LogError("Start - " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), "BT28Win");
			}
		}
	}
}