using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Net;
using System.Net.Http;
using System.IO;
using Newtonsoft.Json;
using System.Text;
using System.Security.Cryptography;
using GameRecord.Models;
using System.Data;
using System.Data.SqlClient;

namespace GameRecord.Controllers
{
	public class KISController_Old : Controller
	{
		//1. Need to have a list of player, then loop to call GetGameRecord. No function to get all player record
		//2. Now interval to get record is 5 min, defaul to one page, 1000 records. If necessary, need to check total > 1000, then doing loop


		private string P_ApiUrl = "http://api.918kiss.com:9991";
		private string P_ApiUrl2 = "http://api.918kiss.com:9919";
		private string P_Agent = "MPI-HK3";
		private string P_SecretKey = "u9EDh9SKm8A8Wdv4c553";
		private string P_AuthCode = "DNAtnpDjHwYJGfTkeVRc";
		private string P_AgentPwd = "Caring1976";
		private int P_TimeOutSec = 60000;
		private int RetryTimes = 2;
		private string P_GamePrefix = "KIS";
		Common _common = new Common();

		//###################################################################################
		#region Declaration

		public string ReturnJSON
		{
			get
			{
				return P_ReturnJSON;
			}
		}
		private string P_ReturnJSON;

		public string ErrorMessage
		{
			get
			{
				return p_ErrorMessage;
			}
		}
		private string p_ErrorMessage;

		public string TargetApiUrl
		{
			get
			{
				return p_TargetApiUrl;
			}
		}
		private string p_TargetApiUrl;

		public string Data
		{
			get
			{
				return p_Data;
			}
		}
		private string p_Data;

		public string Request
		{
			get
			{
				return p_Request;
			}
		}
		private string p_Request;

		public class GameLogVM
		{
			public List<GameLogResultVM> results { get; set; }
			public bool success { get; set; }
			public string currMoney { get; set; }
			public string msg { get; set; }
			public int total { get; set; }
		}

		public class GameLogResultVM
		{
			public int Rownum { get; set; }
			public string BeginBlance { get; set; }
			public string CreateTime { get; set; }
			public string EndBlance { get; set; }
			public string GameName { get; set; }
			public string win { get; set; }
			public string bet { get; set; }
			public int ClassId { get; set; }
			public int GameID { get; set; }
			public int LineNum { get; set; }
			public int RoundNo { get; set; }
			public int TableID { get; set; }
			public long betID { get; set; }
			public string uuid { get; set; }
		}


		#endregion

		//###################################################################################
		#region GET function
		private string SendGETRequest(string requestURL, int timeoutSec, int retryTimes, int retryingTimes = 0)
		{
			StreamReader reader = null;

			try
			{
				// Create web request
				HttpWebRequest httpWebRequest;
				httpWebRequest = (HttpWebRequest)WebRequest.Create(requestURL);
				httpWebRequest.Method = "GET";
				httpWebRequest.Timeout = timeoutSec * 1000;
				httpWebRequest.ReadWriteTimeout = timeoutSec * 1000;

				// Make the request and get the response
				HttpWebResponse response = (HttpWebResponse)httpWebRequest.GetResponse();

				if (response != null && response.GetResponseStream() != null)
				{
					reader = new StreamReader(response.GetResponseStream(), System.Text.Encoding.UTF8, true);
					string dataStr = reader.ReadToEnd();

					return dataStr;
				}
				else if (retryingTimes < retryTimes)
					return SendGETRequest(requestURL, timeoutSec, retryTimes, retryingTimes + 1);
				else
					throw new Exception("Service Time Out");
			}
			catch (Exception e)
			{
				if (retryingTimes < retryTimes)
					return SendGETRequest(requestURL, timeoutSec, retryTimes, retryingTimes + 1);
				else
					throw e;
			}
			finally
			{
				if (reader != null)
					reader.Close();
			}
		}

		private bool ConvertJsonToClassObject(string JsonObj, Object TargetClass)
		{
			try
			{
				TargetClass = JsonConvert.DeserializeObject(JsonObj, TargetClass.GetType());
				return true;
			}
			catch (Exception ex)
			{
				p_ErrorMessage = ex.Message;
				return false;
			}
		}
		#endregion

		//###################################################################################
		#region API function 

		public bool GetGameRecord(string username, string UID, string sStartTime, string sEndTime)
		{
			try
			{
				DateTime spoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
				string time = (System.Convert.ToInt64((DateTime.UtcNow - spoch).TotalMilliseconds)).ToString();

				string strParam = "";
				strParam += "pageIndex=1";
				strParam += "&pageSize=500";
				strParam += "&userName=" + username;
				strParam += "&sDate=" + sStartTime;
				strParam += "&eDate=" + sEndTime;
				strParam += "&time=" + time;
				strParam += "&authcode=" + P_AuthCode;
				strParam += "&sign=" + GetSign(username, time);

				string paramStr = strParam;
				p_Request = paramStr;
				p_TargetApiUrl = P_ApiUrl2 + "/ashx/GameLog.ashx?" + paramStr;

				string strJson = SendGETRequest(p_TargetApiUrl, P_TimeOutSec, RetryTimes);
				strJson = strJson.Replace("id", "betID");
				strJson = strJson.Replace("uubetID", "uuid");

				P_ReturnJSON = strJson;

				GameLogVM result = JsonConvert.DeserializeObject<GameLogVM>(P_ReturnJSON);

				using (BingoConn dbConn = new BingoConn())
				{
					var kissgames = dbConn.KISGames.ToList();

					//Insert Main Table
					var kISGameMain = new KISGameMain
					{
						Id = Guid.NewGuid().ToString(),
						currMoney = result.currMoney,
						msg = result.msg,
						success = result.success,
						//total = result.total,
						startTime = Convert.ToDateTime(sStartTime),
						endTime = Convert.ToDateTime(sEndTime),
						CreatedDate = DateTime.Now
					};
					dbConn.KISGameMains.Add(kISGameMain);

					//Insert Result table
					foreach (var p in result.results)
					{
						if (!kissgames.Any(x => x.uuid == p.uuid))
						{
							var kISGame = new KISGame
							{
								Id = Guid.NewGuid().ToString(),
								fId = kISGameMain.Id,
								username = username,
								UID = UID,
								BeginBalance = p.BeginBlance,
								EndBalance = p.EndBlance,
								GameID = p.GameID,
								GameName = p.GameName,
								ClassID = p.ClassId,
								tableID = p.TableID,
								LineNum = p.LineNum,
								RoundNo = p.RoundNo,
								CreateTime = Convert.ToDateTime(p.CreateTime),
								Rownum = p.Rownum,
								betID = p.betID,
								Win = p.win,
								bet = p.bet,
								uuid = p.uuid,
								CreateDate = DateTime.Now
							};
							kISGameMain.total++;
							dbConn.KISGames.Add(kISGame);
						}
					}

					//if (kISGameMain.total > 0)
						dbConn.SaveChanges();
				}

				//If record more than 1 page, need to get the record from Page 2 and above
				decimal dCnt = Convert.ToDecimal(result.total) / Convert.ToDecimal(500);
				int iPageIndex = Int32.Parse(Math.Ceiling(dCnt).ToString());

				if (iPageIndex > 1)
				{
					for (int x = 2; x <= iPageIndex; x++)
					{
						bool IsOK = GetGameRecordByPage(x, username, UID, sStartTime, sEndTime);
					}
				}

				return true;
			}
			catch (Exception ex)
			{
				_common.LogError("K918/GetGameRecord" + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), "K918");
				return false;
			}
		}

		public bool GetGameRecordByPage(int iPage, string username, string UID, string sStartTime, string sEndTime)
		{
			try
			{
				DateTime spoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
				string time = (System.Convert.ToInt64((DateTime.UtcNow - spoch).TotalMilliseconds)).ToString();

				string strParam = "";
				strParam += "pageIndex=" + iPage.ToString();
				strParam += "&pageSize=500";
				strParam += "&userName=" + username;
				strParam += "&sDate=" + sStartTime;
				strParam += "&eDate=" + sEndTime;
				strParam += "&time=" + time;
				strParam += "&authcode=" + P_AuthCode;
				strParam += "&sign=" + GetSign(username, time);

				string paramStr = strParam;
				p_Request = paramStr;
				p_TargetApiUrl = P_ApiUrl2 + "/ashx/GameLog.ashx?" + paramStr;

				string strJson = SendGETRequest(p_TargetApiUrl, P_TimeOutSec, RetryTimes);
				strJson = strJson.Replace("id", "betID");
				strJson = strJson.Replace("uubetID", "uuid");
				P_ReturnJSON = strJson;

				GameLogVM result = JsonConvert.DeserializeObject<GameLogVM>(P_ReturnJSON);

				using (BingoConn dbConn = new BingoConn())
				{
					string sMainID = Guid.NewGuid().ToString();

					//Insetrt Main Table
					KISGameMain kISGameMain = new KISGameMain()
					{
						Id = sMainID,
						currMoney = result.currMoney,
						msg = result.msg,
						success = result.success,
						total = result.total,
						startTime = Convert.ToDateTime(sStartTime),
						endTime = Convert.ToDateTime(sEndTime),
						CreatedDate = DateTime.Now
					};
					dbConn.KISGameMains.Add(kISGameMain);
					dbConn.SaveChanges();

					//Insert Result table
					foreach (var p in result.results)
					{
						if (!dbConn.KISGames.Any(x => x.uuid == p.uuid))
						{
							KISGame kISGame = new KISGame()
							{
								Id = Guid.NewGuid().ToString(),
								fId = kISGameMain.Id,
								username = username,
								UID = UID,
								BeginBalance = p.BeginBlance,
								EndBalance = p.EndBlance,
								GameID = p.GameID,
								GameName = p.GameName,
								ClassID = p.ClassId,
								tableID = p.TableID,
								LineNum = p.LineNum,
								RoundNo = p.RoundNo,
								CreateTime = Convert.ToDateTime(p.CreateTime),
								Rownum = p.Rownum,
								betID = p.betID,
								Win = p.win,
								bet = p.bet,
								uuid = p.uuid,
								CreateDate = DateTime.Now
							};
							dbConn.KISGames.Add(kISGame);
						}
					}

					dbConn.SaveChanges();
				}

				return true;
			}
			catch (Exception ex)
			{
				//_common.LogError("K918/GetGameecordPage" + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), "K918");
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
					var query = bingoConn.ADMJobDates
															.Where(s => s.GameProvider == P_GamePrefix)
															.FirstOrDefault();

					if (action == "Start")
					{
						DateTime dtSTime = Convert.ToDateTime(query.JobStart);
						sTime = dtSTime.ToString("yyyy-MM-dd HH:mm:ss");
					}
					else
					{
						DateTime dtSTime = Convert.ToDateTime(query.JobEnd);
						sTime = dtSTime.ToString("yyyy-MM-dd HH:mm:ss");
					}
				}

				return sTime;

			}
			catch (Exception ex)
			{
				//_common.LogError("K918/GetJobTime" + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), "K918");
				return "Error:" + ex.Message.ToString();
			}
		}

		public bool UpdJobTime(string startTime, string endTime)
		{
			try
			{
				DateTime dtStart = Convert.ToDateTime(startTime);
				DateTime dtEnd = Convert.ToDateTime(endTime);

				DateTime dtStartAdd = dtStart.AddMinutes(5);
				DateTime dtEndAdd = dtEnd.AddMinutes(5);

				using (var bingoConn = new BingoConn())
				{
					var xyz = bingoConn.ADMJobDates.Where(s => s.GameProvider == P_GamePrefix).FirstOrDefault();
					if (xyz != null)
					{
						xyz.JobStart = dtStartAdd;
						xyz.JobEnd = dtEndAdd;

						bingoConn.Entry(xyz).State = System.Data.Entity.EntityState.Modified;
						bingoConn.SaveChanges();
					}
				}
				return true;
			}
			catch (Exception ex)
			{
				//_common.LogError("K918/UpdJobTime" + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), "K918");
				return false;
			}
		}

		public string MD5Encrypt(string value)
		{
			byte[] ba = System.Security.Cryptography.MD5.Create().ComputeHash(System.Text.ASCIIEncoding.ASCII.GetBytes(value));
			string hex = BitConverter.ToString(ba);
			hex = hex.Replace("-", "");
			string res = hex.ToLower();

			return res;
		}

		public string GetSign(string UID, string time)
		{
			string sValue = P_AuthCode + UID + time + P_SecretKey;
			string sign = "";
			sign = MD5Encrypt(sValue.ToLower());

			return sign.ToUpper();
		}

		public bool GetLognRun()
		{
			try
			{
				using (var bingoConn = new BingoConn())
				{
					// 1. Still Playing
					var query = bingoConn.KISScoreLogs
													.Where(s => s.Flag == "A")
													.ToList();

					if (query != null)
					{
						foreach (var x in query)
						{
							DateTime dtSTime = Convert.ToDateTime(x.FromDate);

							string sTime = dtSTime.ToString("yyyy-MM-dd hh:mm:ss");
							string eTime = DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss");
							string sTodayAM = DateTime.Now.ToString("yyyy-MM-dd ") + "00:00:00";
							string sTodayPM = DateTime.Now.ToString("yyyy-MM-dd ") + "12:00:00";
							string sTodayEnd = DateTime.Now.ToString("yyyy-MM-dd ") + "23:59:59";
							DateTime dtTodayAM = Convert.ToDateTime(sTodayAM);
							DateTime dtTodayPM = Convert.ToDateTime(sTodayPM);

							DateTime dtYest = DateTime.Today.AddDays(-1);
							string sYestPM = dtYest.ToString("yyyy-MM-dd ") + "12:00:00";

							if (dtSTime.Date == DateTime.Today)
							{
								if (dtSTime <= dtTodayPM)
								{
									GetGameRecord(x.Username, x.UID, sTodayAM, sTodayPM);
								}
								else
								{
									GetGameRecord(x.Username, x.UID, sTodayPM, sTodayEnd);
								}
							}
							else
							{
								if (dtSTime.Date < DateTime.Today)
								{
									GetGameRecord(x.Username, x.UID, sYestPM, sTodayAM);
									GetGameRecord(x.Username, x.UID, sTodayAM, sTodayPM);
								}
							}
						}
					}

					// 2. Completed Playing
					var query1 = bingoConn.KISScoreLogs
													.Where(s => s.Flag == "B")
													.ToList();

					if (query1 != null)
					{
						foreach (var y in query1)
						{
							DateTime dtSTime = Convert.ToDateTime(y.FromDate);
							string sTime = dtSTime.ToString("yyyy-MM-dd hh:mm:ss");

							DateTime dtETime = Convert.ToDateTime(y.ToDate);
							string eTime = dtETime.ToString("yyyy-MM-dd hh:mm:ss");

							string sTodayAM = DateTime.Now.ToString("yyyy-MM-dd ") + "00:00:00";
							string sTodayPM = DateTime.Now.ToString("yyyy-MM-dd ") + "12:00:00";
							string sTodayEnd = DateTime.Now.ToString("yyyy-MM-dd ") + "23:59:59";
							DateTime dtTodayAM = Convert.ToDateTime(sTodayAM);
							DateTime dtTodayPM = Convert.ToDateTime(sTodayPM);

							DateTime dtYest = DateTime.Today.AddDays(-1);
							string sYestPM = dtYest.ToString("yyyy-MM-dd ") + "12:00:00";

							if (dtSTime.Date == dtETime.Date)
							{
								if (dtSTime <= dtTodayPM)
								{
									GetGameRecord(y.Username, y.UID, sTodayAM, sTodayPM);
								}
								else
								{
									GetGameRecord(y.Username, y.UID, sTodayPM, sTodayEnd);
								}
							}
							else
							{
								GetGameRecord(y.Username, y.UID, sYestPM, sTodayAM);
								GetGameRecord(y.Username, y.UID, sTodayAM, sTodayPM);
							}

							y.Flag = "C";
						}
						bingoConn.SaveChanges();
					}

					// 2. Adhoc Request
					//var queryA = bingoConn.KISScoreLogs
					//                .Where(s => s.Flag == "R")
					//                .ToList();

					//if (queryA != null)
					//{
					//    foreach (var z in queryA)
					//    {
					//        DateTime dtSTime = Convert.ToDateTime(z.FromDate);
					//        string sTime = dtSTime.ToString("yyyy-MM-dd HH:mm:ss");

					//        DateTime dtETime = Convert.ToDateTime(z.ToDate);
					//        string eTime = dtETime.ToString("yyyy-MM-dd HH:mm:ss");

					//        GetGameRecord(z.Username, z.UID, sTime, eTime);

					//        z.Flag = "RD";
					//    }
					//    bingoConn.SaveChanges();
					//}



				}

				return true;
			}
			catch (Exception ex)
			{
				return false;
			}


		}

		#endregion




		// GET: KIS
		public void KISJob()
		{
			try
			{
				bool isOK = GetLognRun();
			}
			catch (Exception ex)
			{
				//_common.LogError("Start - " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), "IG");
			}
		}
	}
}