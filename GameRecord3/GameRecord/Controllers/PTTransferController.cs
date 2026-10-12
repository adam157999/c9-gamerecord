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
	public class PTTransferController : Controller
	{
		public static readonly object MyLock = new object();
		private string P_GamePrefix = "PTTransfer";
		private string P_ApiUrl = "https://kioskpublicapi.luckydragon88.com/";
		private string P_EntityKey = "ee1976197b52c2fa60e05e1d489810c3de3dc6ab184508f97cbabe54b21dc39838dd0ce3f8f1377f9fba3ac0441d1988a7e51d2f43fcfd192376b476f154635d";
		private string P_CertPass = "vrinkiv4HfXj7flP";
		private string P_CertPath = System.Web.HttpContext.Current.Server.MapPath(@"\Keys\MYRWFF88.p12");
		//private string P_CertPath = System.Web.HttpContext.Current.Server.MapPath(@"\Keys\CNY_UAT_FB88.p12");
		//private string P_CertPath = "C:\\inetpub\\wwwroot\\GameRecord Publish\\Keys\\CNY_UAT_FB88.p12";
		Common _common = new Common();

		//###################################################################################
		#region Declare 

		public class GameResponse
		{
			public Pagination pagination { get; set; }
			public List<Result> result { get; set; }
			public string error { get; set; }
			public int errorcode { get; set; }
		}

		public class Pagination
		{
			public int currentPage { get; set; }
			public int totalPages { get; set; }
			public int itemsPerPage { get; set; }
			public int totalCount { get; set; }
		}

		public class Result
		{
			public string PLAYERNAME { get; set; }
			public string WINDOWCODE { get; set; }
			public string GAMEID { get; set; }
			public string GAMECODE { get; set; }
			public string GAMETYPE { get; set; }
			public string GAMENAME { get; set; }
			public string SESSIONID { get; set; }
			public decimal BET { get; set; }
			public decimal WIN { get; set; }
			public decimal PROGRESSIVEBET { get; set; }
			public decimal PROGRESSIVEWIN { get; set; }
			public decimal BALANCE { get; set; }
			public decimal CURRENTBET { get; set; }
			public DateTime? GAMEDATE { get; set; }
			public string LIVENETWORK { get; set; }
			public string RNUM { get; set; }
		}

		#endregion

		private static bool CertificateValidationCallBack(object sender, X509Certificate certificate, X509Chain chain, System.Net.Security.SslPolicyErrors sslPolicyErrors)
		{
			// If the certificate is a valid, signed certificate, return true.
			if (sslPolicyErrors == System.Net.Security.SslPolicyErrors.None)
			{
				return true;
			}

			// If there are errors in the certificate chain, look at each error to determine the cause.
			if ((sslPolicyErrors & System.Net.Security.SslPolicyErrors.RemoteCertificateChainErrors) != 0)
			{
				if (chain != null && chain.ChainStatus != null)
				{
					foreach (X509ChainStatus status in chain.ChainStatus)
					{
						if ((certificate.Subject == certificate.Issuer) && (status.Status == X509ChainStatusFlags.UntrustedRoot))
						{
							// Self-signed certificates with an untrusted root are valid. 
							continue;
						}
						else
						{
							if (status.Status != X509ChainStatusFlags.NoError)
							{
								// If there are any other errors in the certificate chain, the certificate is invalid,
								// so the method returns false.
								return false;
							}
						}
					}
				}

				return true;
			}
			else
			{
				// In all other cases, return false.
				return false;
			}
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
				_common.LogError("PT/UpdJobTime" + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), "PT");
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
						DateTime dtSTime = Convert.ToDateTime(query.JobStart?.ToString("yyyy-MM-dd HH:mm"));
						sTime = dtSTime.ToString("yyyy-MM-dd HH:mm:ss");
					}
					else
					{
						DateTime dtSTime = Convert.ToDateTime(query.JobEnd?.ToString("yyyy-MM-dd HH:mm"));
						sTime = dtSTime.ToString("yyyy-MM-dd HH:mm:ss");
					}
				}

				return sTime;

			}
			catch (Exception ex)
			{
				_common.LogError("PT/GetJobTime" + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), "PT");
				return "Error:" + ex.Message.ToString();
			}
		}

		public bool GetGameRecord()
		{
			string startTime = GetJobTime("Start");
			string endTime = GetJobTime("End");
			int currentPage = 1;
			int pageSize = 5000;
			string mStartTime = startTime;
			string mEndTime = endTime;
			int limit = 30;

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

					string param = string.Format("/customreport/getdata/reportname/PlayerTransactions/startdate/{0}/enddate/{1}/page/{2}/perPage/{3}",
					Convert.ToDateTime(mStartTime).ToString("yyyy-MM-dd HH:mm:ss").Replace(" ", "%20"),
					Convert.ToDateTime(mEndTime).ToString("yyyy-MM-dd HH:mm:ss").Replace(" ", "%20"),
					Convert.ToString(currentPage),
					Convert.ToString(pageSize));
					string webAddr = P_ApiUrl + param;

					HttpWebRequest Request = (HttpWebRequest)WebRequest.Create(webAddr);

					Request.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8";
					Request.Headers.Add("Cache-Control", "max-age=0");
					Request.KeepAlive = true;
					Request.Headers.Add("Keep-Alive", "timeout=5, max=100");
					Request.Headers.Add("Accept-Charset", "ISO-8859-1,utf-8;q=0.7,*;q=0.3");
					Request.Headers.Add("Accept-Language", "es-ES,es;q=0.8");
					Request.Headers.Add("Pragma", "");
					Request.Headers.Add("X_ENTITY_KEY", P_EntityKey);
					Request.Method = "POST";

					Request.ContentType = "application/x-www-form-urlencoded";
					Request.ClientCertificates.Add(new X509Certificate2(P_CertPath, P_CertPass, X509KeyStorageFlags.MachineKeySet));
					ServicePointManager.ServerCertificateValidationCallback = CertificateValidationCallBack;
					ServicePointManager.Expect100Continue = true;
					ServicePointManager.DefaultConnectionLimit = 9999;
					ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12 | SecurityProtocolType.Ssl3;

					var httpResponse = (HttpWebResponse)Request.GetResponse();
					using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
					{
						var responseText = streamReader.ReadToEnd();

						_common.LogError(responseText, "PT");

						dynamic res = JObject.Parse(responseText);
						//GameResponse res = JsonConvert.DeserializeObject<GameResponse>(responseText);

						using (BingoConn db = new BingoConn())
						{
							string sMainID = Guid.NewGuid().ToString();
							if (res.result != null && res.pagination != null)
							{
								foreach (var obj in res.result)
								{
									string id = Convert.ToString(obj.PLAYTECHTRANSACTIONID);
									try
									{
										if (db.PTTransfers.Where(x => x.PLAYTECHTRANSACTIONID == id).FirstOrDefault() == null)
										{
											PTTransfer ptGame = new PTTransfer()
											{
												KIOSKTRANSACTIONID = obj.KIOSKTRANSACTIONID,
												PLAYTECHTRANSACTIONID = obj.PLAYTECHTRANSACTIONID,
												EXTERNALTRANSACTIONID = obj.EXTERNALTRANSACTIONID,
												ENTITYNAME = obj.ENTITYNAME,
												KIOSKNAME = obj.KIOSKNAME,
												ADMINNAME = obj.ADMINNAME,
												PLAYERNAME = obj.PLAYERNAME,
												BRAND = obj.BRAND,
												TRANSACTIONTIME = obj.TRANSACTIONTIME,
												AMOUNT = obj.AMOUNT,
												TYPE = obj.TYPE,
												STATUS = obj.STATUS,
												REMOTEIP = obj.REMOTEIP,
												CreatedDate = DateTime.Now
											};
											db.PTTransfers.Add(ptGame);
										}
									}
									catch (Exception ex)
									{
										_common.LogError("Record " + id + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), "PT");
									}
								}
								db.SaveChanges();

								int totalPages = Convert.ToInt32(res.pagination.totalPages);
								int currentPg = Convert.ToInt32(res.pagination.currentPage);
								//record more than limit
								if (currentPg <= totalPages)
								{
									for (int i = 1; i <= totalPages; i++)
									{
										GetGameRecordExtend(currentPg, pageSize, mStartTime, mEndTime, sMainID);
									}
								}
							}

							//if (res.errorcode.ToString() == "0")
							//{
							//	bool isUpdJob = UpdJobTime(mStartTime, mEndTime, sMainID);
							//}
						}
					}
				}

				return true;
			}
			catch (Exception ex)
			{
				_common.LogError("PT/GetGameRecord" + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), "PT");
				return false;
			}
		}

		public bool GetGameRecordExtend(int currentPage, int pageSize, string startTime, string endTime, string sMainID)
		{

			string param = string.Format("/customreport/getdata/reportname/PlayerTransactions/startdate/{0}/enddate/{1}/page/{2}/perPage/{3}",
					Convert.ToDateTime(startTime).ToString("yyyy-MM-dd HH:mm:ss").Replace(" ", "%20"),
					Convert.ToDateTime(endTime).ToString("yyyy-MM-dd HH:mm:ss").Replace(" ", "%20"),
					Convert.ToString(currentPage),
					Convert.ToString(pageSize));
			string webAddr = P_ApiUrl + param;

			HttpWebRequest Request = (HttpWebRequest)WebRequest.Create(webAddr);

			Request.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8";
			Request.Headers.Add("Cache-Control", "max-age=0");
			Request.KeepAlive = true;
			Request.Headers.Add("Keep-Alive", "timeout=5, max=100");
			Request.Headers.Add("Accept-Charset", "ISO-8859-1,utf-8;q=0.7,*;q=0.3");
			Request.Headers.Add("Accept-Language", "es-ES,es;q=0.8");
			Request.Headers.Add("Pragma", "");
			Request.Headers.Add("X_ENTITY_KEY", P_EntityKey);
			Request.Method = "POST";

			Request.ContentType = "application/x-www-form-urlencoded";
			Request.ClientCertificates.Add(new X509Certificate2(P_CertPath, P_CertPass, X509KeyStorageFlags.MachineKeySet));
			ServicePointManager.ServerCertificateValidationCallback = CertificateValidationCallBack;
			ServicePointManager.Expect100Continue = true;
			ServicePointManager.DefaultConnectionLimit = 9999;
			ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12 | SecurityProtocolType.Ssl3;

			var httpResponse = (HttpWebResponse)Request.GetResponse();
			using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
			{
				var responseText = streamReader.ReadToEnd();
				dynamic res = JObject.Parse(responseText);

				using (BingoConn db = new BingoConn())
				{
					if (res.result != null && res.pagination != null)
					{
						foreach (var obj in res.result)
						{
							string id = Convert.ToString(obj.PLAYTECHTRANSACTIONID);
							try
							{
								if (db.PTTransfers.Where(x => x.PLAYTECHTRANSACTIONID == id).FirstOrDefault() == null)
								{
									PTTransfer ptGame = new PTTransfer()
									{
										KIOSKTRANSACTIONID = obj.KIOSKTRANSACTIONID,
										PLAYTECHTRANSACTIONID = obj.PLAYTECHTRANSACTIONID,
										EXTERNALTRANSACTIONID = obj.EXTERNALTRANSACTIONID,
										ENTITYNAME = obj.ENTITYNAME,
										KIOSKNAME = obj.KIOSKNAME,
										ADMINNAME = obj.ADMINNAME,
										PLAYERNAME = obj.PLAYERNAME,
										BRAND = obj.BRAND,
										TRANSACTIONTIME = obj.TRANSACTIONTIME,
										AMOUNT = obj.AMOUNT,
										TYPE = obj.TYPE,
										STATUS = obj.STATUS,
										REMOTEIP = obj.REMOTEIP,
										CreatedDate = DateTime.Now
									};
									db.PTTransfers.Add(ptGame);
								}
							}
							catch (Exception ex)
							{
								_common.LogError("Record " + id + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), "PT");
							}
						}
						db.SaveChanges();
					}
				}
			}
			return true;
		}

		public void PTJob()
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
				_common.LogError("Start - " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), "PT");
			}
		}
	}

	//public class TrustAllCertificatePolicy : ICertificatePolicy
	//{
	//	public TrustAllCertificatePolicy()
	//	{ }
	//	public bool CheckValidationResult(ServicePoint sp, System.Security.Cryptography.X509Certificates.X509Certificate cert, WebRequest req, int problem)
	//	{
	//		return true;
	//	}
	//}
}