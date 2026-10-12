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
using System.Data.SqlClient;
using System.Data;
using System.Configuration;

namespace GameRecord.Controllers
{
	public class VPowerTestController : Controller
	{
		//public static readonly object MyLock = new object();
		//private string P_GamePrefix = "VPower";
		//private string P_ApiUrl = "https://www.stargame88.xyz/open_api?";
		//private string P_SecretKey = "K85FYkLoJYjulainuZU1ghx3jd39";
		//private string P_AppId = "F00CNcapRVkdxtty4d4";

		public static readonly object MyLock = new object();
		private string P_GamePrefix = "VPower";
		//private string P_ApiUrl = "https://www.stargame88.xyz/open_api?";
		private string P_ApiUrl = "https://www.stargame18.com/open_api?";
		private string P_SecretKey = "J00CZNfYVWpXgbjWnojufvn4d4";
		private string P_AppId = "G85FkVQXNZoulld39";
		private int P_TimeoutSec = 60000;
		Common _common = new Common();



		//###################################################################################
		#region Main Function 

		public bool GetGameRecord(string acc)
		{
			//string startTime = GetJobTime("Start");
			//string endTime = GetJobTime("End");
			string startTime = "2021-01-19 00:00:00";
			string endTime = "2021-01-19 12:00:00";

			try
			{
				string sMainID = Guid.NewGuid().ToString();
				int limit = 30;
				string action = "glog";
				long time = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
				string rawParam = string.Format("Act={0}&GameId={1}&Username={2}&BeginTime={3}&EndTime={4}&PageIndex={5}&Timestamp={6}&AppId={7}", 
					action, 
					"",
					acc,
					startTime,
					endTime,
					1,
					time,
					P_AppId);
				string signature = hash_hmac(rawParam);
				string param = string.Format("Act={0}&GameId={1}&Username={2}&BeginTime={3}&EndTime={4}&PageIndex={5}&Timestamp={6}&AppId={7}&Signature={8}",
					action,
					"",
					acc,
					startTime,
					endTime,
					1,
					time,
					P_AppId,
					signature);
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
						VPowerGameMain vpowerGameMain = new VPowerGameMain()
						{
							Id = sMainID,
							Total = result.Total != null ? result.Total : 0,
							Code = result.ErrorCode,
							StartTime = Convert.ToDateTime(startTime),
							EndTime = Convert.ToDateTime(endTime),
							CreatedDate = DateTime.Now
						};
						db.VPowerGameMains.Add(vpowerGameMain);
						db.SaveChanges();

						if (result.ErrorCode == 0)
						{
							if (result.Data.Count > 0)
							{
								foreach (var obj in result.Data)
								{
									string uniqleID = Convert.ToString(obj.uniqleid);

									try
									{
										VPowerGame vpowerGame = new VPowerGame()
										{
											fId = sMainID,
											uniqleid = obj.uniqleid,
											uid = obj.uniqueid,
											username = obj.username,
											gameid = obj.gameid,
											gameocode = obj.gameocode,
											tableid = obj.tableid,
											pay = obj.pay != null ? Convert.ToDecimal(obj.pay) : null,
											win = obj.win != null ? Convert.ToDecimal(obj.win) : null,
											startscore = obj.startscore != null ? Convert.ToDecimal(obj.startscore) : null,
											finalscore = obj.finalscore != null ? Convert.ToDecimal(obj.finalscore) : null,
											time = obj.time != null ? DateTimeOffset.FromUnixTimeSeconds(Convert.ToInt64(obj.time)).LocalDateTime : null
										};

										var record = db.VPowerGames.Where(x => x.uniqleid == uniqleID).FirstOrDefault();

										if (record == null)
										{
											vpowerGame.CreatedDate = DateTime.Now;
										}
										else
										{
											vpowerGame.CreatedDate = record.CreatedDate;
											vpowerGame.ModifiedDate = DateTime.Now;
										}
										db.VPowerGames.AddOrUpdate(vpowerGame);
									}
									catch (Exception ex)
									{
										_common.LogError("Record " + uniqleID + " " + DateTime.Now.ToString() + " " + ex.Message.ToString(), "VPower");
									}
								}
								db.SaveChanges();

								decimal dCnt = Convert.ToDecimal(result.Total) / Convert.ToDecimal(limit);
								int iPageIndex = Int32.Parse(Math.Ceiling(dCnt).ToString());

								if (iPageIndex > 1)
								{
									for (int x = 2; x <= iPageIndex; x++)
									{
										bool IsOK = GetGameRecordExtend(x, startTime, endTime, sMainID, acc);
									}
								}
							}
						}

						if (result.ErrorCode == 0)
						{
							bool isUpdJob = UpdJobTime(startTime, endTime, sMainID);
						}
					}
				}


				return true;
			}
			catch (Exception ex)
			{
				_common.LogError("VPower/GetGameRecord" + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), "VPower");
				return false;
			}
		}

		public bool GetGameRecordExtend(int pageIndex, string startTime, string endTime, string sMainID, string acc)
		{
			string action = "glog";
			long time = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
			string rawParam = string.Format("Act={0}&GameId={1}&Username={2}&BeginTime={3}&EndTime={4}&PageIndex={5}&Timestamp={6}&AppId={7}",
				action,
				"",
				acc,
				startTime,
				endTime,
				pageIndex,
				time,
				P_AppId);
			string signature = hash_hmac(rawParam);
			string param = string.Format("Act={0}&GameId={1}&Username={2}&BeginTime={3}&EndTime={4}&PageIndex={5}&Timestamp={6}&AppId={7}&Signature={8}",
				action,
				"",
				acc,
				startTime,
				endTime,
				pageIndex,
				time,
				P_AppId,
				signature);
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
					if (result.ErrorCode == 0)
					{
						if (result.Data.Count > 0)
						{
							foreach (var obj in result.Data)
							{
								string uniqleID = Convert.ToString(obj.uniqueid);

								try
								{
									VPowerGame vpowerGame = new VPowerGame()
									{
										fId = sMainID,
										uniqleid = obj.uniqleid,
										uid = obj.uniqueid,
										username = obj.username,
										gameid = obj.gameid,
										gameocode = obj.gameocode,
										tableid = obj.tableid,
										pay = obj.pay != null ? Convert.ToDecimal(obj.pay) : null,
										win = obj.win != null ? Convert.ToDecimal(obj.win) : null,
										startscore = obj.startscore != null ? Convert.ToDecimal(obj.startscore) : null,
										finalscore = obj.finalscore != null ? Convert.ToDecimal(obj.finalscore) : null,
										time = obj.time != null ? DateTimeOffset.FromUnixTimeSeconds(Convert.ToInt64(obj.time)).LocalDateTime : null
									};

									var record = db.VPowerGames.Where(x => x.uniqleid == uniqleID).FirstOrDefault();

									if (record == null)
									{
										vpowerGame.CreatedDate = DateTime.Now;
									}
									else
									{
										vpowerGame.CreatedDate = record.CreatedDate;
										vpowerGame.ModifiedDate = DateTime.Now;
									}
									db.VPowerGames.AddOrUpdate(vpowerGame);
								}
								catch (Exception ex)
								{
									_common.LogError("Record " + uniqleID + " " + DateTime.Now.ToString() + " " + ex.Message.ToString(), "VPower");
								}
							}
							db.SaveChanges();
						}
					}
				}
			}

			return true;
		}

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
				_common.LogError("VPower/UpdJobTime" + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), "VPower");
				return false;
			}
		}

		public string hash_hmac(string signatureString)
		{
			using (var signature = new HMACSHA1(Encoding.UTF8.GetBytes(P_SecretKey)))
			{
				var bytes = Encoding.UTF8.GetBytes(signatureString);
				var hash = signature.ComputeHash(bytes);
				return HttpUtility.UrlEncode(Convert.ToBase64String(hash));
			}
		}

		public DateTime UnixTimeStampToDateTime(double unixTimeStamp)
		{
			// Unix timestamp is seconds past epoch
			System.DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
			dtDateTime = dtDateTime.AddSeconds(unixTimeStamp).ToLocalTime();
			return dtDateTime;
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
				_common.LogError("VPower/GetJobTime" + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), "VPower");
				return "Error:" + ex.Message.ToString();
			}
		}

		#endregion

		public void Update_PlayerTask_NearestAgent(string PlayerId)
		{
			using (SqlConnection con_tw = new SqlConnection(ConfigurationManager.ConnectionStrings["TaiwanConn"].ConnectionString))
			using (SqlConnection con_game = new SqlConnection(ConfigurationManager.ConnectionStrings["BingoConn1"].ConnectionString))
			{
				con_tw.Open();
				con_game.Open();

				string sNearestUpline = "";

				string sql1 = "select * from dbo.GetNearestAgentByPlayer('" + PlayerId + "')";
				using (SqlDataAdapter adapter = new SqlDataAdapter(sql1, con_game))
				{
					DataTable table = new DataTable();
					adapter.Fill(table);
					if (table.Rows.Count > 0)
						sNearestUpline = table.Rows[0]["AccountID"].ToString();
				}

				using (SqlTransaction tran = con_tw.BeginTransaction())
				{
					string sql = "update PLAYER_TASK set [Near Agent]='" + sNearestUpline + "' where KEY1='" + PlayerId + "'";
					SqlCommand cmd = new SqlCommand(sql, con_tw, tran);
					int iRow = cmd.ExecuteNonQuery();
					if (iRow == 1) tran.Commit();
				}
			}
		}

		// GET: VPower
		public void VPowerJob(string acc)
		{
			try
			{
				lock (MyLock)
				{
					bool isOK = GetGameRecord(acc);
				}
			}
			catch (Exception ex)
			{
				_common.LogError("Start - " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), "VPower");
			}
		}
	}
}