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
using System.Data.Entity.Migrations;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using System.Configuration;
using System.Data.Entity.Validation;
using Newtonsoft.Json.Linq;

namespace GameRecord.Controllers
{
	public class BGAngpaoController : Controller
	{
		public static readonly object MyLock = new object();
		private string P_ApiUrl = "http://n1api.linirn.com/open-cloud/api/";
		private string P_ApiUrl1 = "http://n1api.trarot.com/open-cloud/api/"; //use this is first one got problem
		private string P_SecretKey = "4AB268FD607445B98C0ABB5110403237";
		//private string P_AgentId = "fa08btagent";
		//private string P_AgentPwd = "0524Bt0524";
		private string P_sn = "bj09";
		private string P_GamePrefix = "BGAngpao";

		Common _common = new Common();

		//##########################################################################################################################
		#region Function

		//Encrypt the data - Key to lower, Concat random+sn+secretKey 
		public string digestMD5Key(string random)
		{
			string text = random + P_sn + P_SecretKey;

			byte[] ba = System.Security.Cryptography.MD5.Create().ComputeHash(System.Text.ASCIIEncoding.ASCII.GetBytes(text));
			string hex = BitConverter.ToString(ba);
			hex = hex.Replace("-", "");

			return hex;
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
						DateTime dtSTime = Convert.ToDateTime(query.JobStart?.ToString("yyyy-MM-dd HH:mm"));
						sTime = dtSTime.ToString("yyyy-MM-dd HH:mm:ss");
					}
					else
					{
						DateTime dtSTime = Convert.ToDateTime(query.JobEnd?.ToString("yyyy-MM-dd HH:mm"));
						sTime = dtSTime.ToString("yyyy-MM-dd HH:mm:ss");
					}

					//if (action == "Start")
					//{
					//	DateTime dtSTime = Convert.ToDateTime(query.LastRunTillDate);
					//	sTime = dtSTime.ToString("yyyy-MM-dd HH:mm:ss");
					//}
					//else
					//{
					//	DateTime dtSTime = DateTime.Now - new TimeSpan(0, 12, 3, 0);
					//	sTime = dtSTime.ToString("yyyy-MM-dd HH:mm:ss");
					//}
				}
				return sTime;
			}
			catch (Exception ex)
			{
				_common.LogError("BG/GetJobTime" + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), "BG");
				return "Error:" + ex.Message.ToString();
			}
		}

		//public string GetJobTime(string action)
		//{
		//    try
		//    {
		//        string sTime = "";
		//        using (var bingoConn = new BingoConn())
		//        {
		//            var query = bingoConn.ADMJobDates
		//                                .Where(s => s.GameProvider == P_GamePrefix)
		//                                .FirstOrDefault();

		//            if (action == "Start")
		//            {
		//                DateTime dtSTime = Convert.ToDateTime(query.JobStart);
		//                sTime = dtSTime.ToString("yyyy-MM-dd HH:mm:ss");
		//            }
		//            else
		//            {
		//                DateTime dtSTime = Convert.ToDateTime(query.JobEnd);
		//                sTime = dtSTime.ToString("yyyy-MM-dd HH:mm:ss");
		//            }
		//        }
		//        return sTime;
		//    }
		//    catch (Exception ex)
		//    {
		//        _common.LogError("BG/GetJobTime" + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), "BG");
		//        return "Error:" + ex.Message.ToString();
		//    }
		//}

		#endregion

		//##########################################################################################################################
		#region  Declaration

		public string ErrorMessage
		{
			get
			{
				return p_ErrorMessage;
			}
		}
		private string p_ErrorMessage;

		public class GameLogVM
		{
			public GameLogResultVM result { get; set; }
			public string id { get; set; }
			public string error { get; set; }
			public string jsonrpc { get; set; }
		}

		public class GameLogResultVM
		{
			public List<GameLogItemVM> items { get; set; }
			public int total { get; set; }
			public int pageIndex { get; set; }
			public int pageSize { get; set; }
			public string etag { get; set; }
		}

		public class GameLogItemVM
		{
			public long orderId { get; set; }
			//public long tranId { get; set; }
			public string sn { get; set; }
			public long uid { get; set; }
			public string loginId { get; set; }
			public int moduleId { get; set; }
			public string moduleName { get; set; }
			public int gameId { get; set; }
			public string gameName { get; set; }
			public int orderStatus { get; set; }
			public float bAmount { get; set; }
			public float aAmount { get; set; }
			public int orderFrom { get; set; }
			public DateTime orderTime { get; set; }
			public DateTime lastUpdateTime { get; set; }
			public string playId { get; set; }
			public string fromIp { get; set; }
			public string issueId { get; set; }
			public string playName { get; set; }
			public string playNameEn { get; set; }
			public float validBet { get; set; }
			public float payment { get; set; }
			public string betContent { get; set; }
		}

		#endregion

		//###################################################################################
		#region Main function 

		//@@@@@@@@@@@@@@@@@@@@@@@@@@@
		public bool GetGameRecord()
		{
			try
			{
				string startTime = GetJobTime("Start");
				string endTime = GetJobTime("End");

				string method = "open.red.packet.exchange";
				string webAddr = P_ApiUrl + "/" + method;
				string random = "BG" + Guid.NewGuid().ToString();
				string sign = digestMD5Key(random);
				string srpc = "2.0";
				int pageSize = 500;

				var httpWebRequest = (HttpWebRequest)WebRequest.Create(webAddr);
				httpWebRequest.ContentType = "application/json";
				httpWebRequest.Method = "POST";
				httpWebRequest.Timeout = 60000;
				httpWebRequest.ReadWriteTimeout = 60000;

				string json = "";
				using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
				{
					json = "{ \"id\" : \"" + random +
								"\", \"method\" : \"" + method +
							"\" , \"params\" : { \"random\" : \"" + random + "\" , \"sign\" : \"" + sign + "\" , \"sn\" : \"" + P_sn + "\" , \"startTime\" : \"" + startTime + "\", \"endTime\" : \"" + endTime + "\", \"pageSize\" : \"" + pageSize + "\" }" +
							", \"jsonrpc\" : \"" + srpc + "\"} ";


					streamWriter.Write(json);
					streamWriter.Flush();
				}

				var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
				using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
				{
					var responseText = streamReader.ReadToEnd();

					dynamic result = JObject.Parse(responseText);
					string sMainID = Guid.NewGuid().ToString();

					using (BingoConn db = new BingoConn())
					{
						//Insert Result table
						foreach (var p in result.result.items)
						{
							try
							{
								string pId = p.pid;
								var bggame = db.BGAngpaos.Where(x => x.pid == pId).FirstOrDefault();
								if (bggame == null)
								{
									var bGAngpao = new BGAngpao()
									{
										pid = p.pid,
										sn = p.sn,
										uid = p.uid,
										time = Convert.ToDateTime(p.time),
										account = p.account,
										balance = Convert.ToDecimal(p.balance),
										amount = Convert.ToDecimal(p.amount),
										CreatedDate = DateTime.Now
									};
									db.BGAngpaos.AddOrUpdate(bGAngpao);
								}
							}
							catch (Exception ex)
							{
								_common.LogError("Record " + p.pid + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), "BG");
							}
						}

						db.SaveChanges();
					}

					//If record more than 500, need to get the record from Page 2 and above
					decimal dCnt = Convert.ToDecimal(result.result.total) / Convert.ToDecimal(500);
					int iPageIndex = Int32.Parse(Math.Ceiling(dCnt).ToString());

					if (iPageIndex > 1)
					{
						for (int x = 2; x <= iPageIndex; x++)
						{
							bool IsOK = GetGameRecordExtend(x);
						}
					}

					bool isUpdJob = UpdJobTime(startTime, endTime, sMainID);

					return true;
				}
			}
			catch (DbEntityValidationException e)
			{
				foreach (var eve in e.EntityValidationErrors)
				{
					_common.LogError("BG/GetGameRecord" + " " + DateTime.Now.ToString() + " " + string.Format("Entity of type \"{0}\" in state \"{1}\" has the following validation errors:",
							eve.Entry.Entity.GetType().Name, eve.Entry.State), "BG");
					foreach (var ve in eve.ValidationErrors)
					{
						_common.LogError("BG/GetGameRecord" + " " + DateTime.Now.ToString() + " " + string.Format("- Property: \"{0}\", Error: \"{1}\"",
								ve.PropertyName, ve.ErrorMessage), "BG");
					}
				}
				return false;
			}
			catch (Exception ex)
			{
				_common.LogError("BG/GetGameRecord" + " " + DateTime.Now.ToString() + " " + ex.Message.ToString(), "BG");
				return false;
			}
			//return "OK";
		}

		//@@@@@@@@@@@@@@@@@@@@@@@@@@@
		public bool GetGameRecordExtend(int pageIndex)
		{
			string startTime = GetJobTime("Start");
			string endTime = GetJobTime("End");
			string sMainID = Guid.NewGuid().ToString();

			try
			{
				string method = "open.red.packet.exchange";
				string webAddr = P_ApiUrl + "/" + method;
				string random = "BG" + Guid.NewGuid().ToString();
				string sign = digestMD5Key(random);
				string srpc = "2.0";
				int pageSize = 500;

				var httpWebRequest = (HttpWebRequest)WebRequest.Create(webAddr);
				httpWebRequest.ContentType = "application/json";
				httpWebRequest.Method = "POST";
				httpWebRequest.Timeout = 60000;
				httpWebRequest.ReadWriteTimeout = 60000;

				using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
				{
					string json = "{ \"id\" : \"" + random +
													"\", \"method\" : \"" + method +
													"\" , \"params\" : { \"random\" : \"" + random + "\" , \"sign\" : \"" + sign + "\" , \"sn\" : \"" + P_sn + "\" , \"startTime\" : \"" + startTime + "\", \"endTime\" : \"" + endTime + "\", \"pageSize\" : \"" + pageSize + "\", \"pageIndex\" : \"" + pageIndex + "\" }" +
													", \"jsonrpc\" : \"" + srpc + "\"} ";

					streamWriter.Write(json);
					streamWriter.Flush();
				}
				var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
				using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
				{
					var responseText = streamReader.ReadToEnd();

					dynamic result = JObject.Parse(responseText);

					using (BingoConn db = new BingoConn())
					{
						foreach (var p in result.result.items)
						{
							try
							{
								string pId = p.pid;
								var bggame = db.BGAngpaos.Where(x => x.pid == pId).FirstOrDefault();
								if (bggame == null)
								{
									var bGAngpao = new BGAngpao()
									{
										pid = p.pid,
										sn = p.sn,
										uid = p.uid,
										time = Convert.ToDateTime(p.time),
										account = p.account,
										balance = Convert.ToDecimal(p.balance),
										amount = Convert.ToDecimal(p.amount),
										CreatedDate = DateTime.Now
									};
									db.BGAngpaos.AddOrUpdate(bGAngpao);
								}
							}
							catch (Exception ex)
							{
								_common.LogError("Record " + p.pid + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), "BG");
							}
						}

						db.SaveChanges();
					}

					return true;
				}
			}
			catch (DbEntityValidationException e)
			{
				foreach (var eve in e.EntityValidationErrors)
				{
					_common.LogError("BG/GetGameRecordExtend" + " " + DateTime.Now.ToString() + "  " + string.Format("Entity of type \"{0}\" in state \"{1}\" has the following validation errors:",
							eve.Entry.Entity.GetType().Name, eve.Entry.State), "BG");
					foreach (var ve in eve.ValidationErrors)
					{
						_common.LogError("BG/GetGameRecordExtend" + " " + DateTime.Now.ToString() + "  " + string.Format("- Property: \"{0}\", Error: \"{1}\"",
								ve.PropertyName, ve.ErrorMessage), "BG");
					}
				}
				return false;
			}
			catch (Exception ex)
			{
				_common.LogError("BG/GetGameRecordExtend" + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), "BG");
				return false;
			}
		}

		//@@@@@@@@@@@@@@@@@@@@@@@@@@@
		public bool UpdJobTime(string startTime, string endTime, string fId)
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
				_common.LogError("BG/UpdJobTime" + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), "BG");
				return false;
			}
		}

		#endregion

		// GET: BG
		public string BGJob()
		{
			try
			{
				lock (MyLock)
				{
					//_common.LogError("BGJob Start - " + DateTime.Now.ToString(), "BG");

					bool IsAllRecord = GetGameRecord();

					//_common.LogError("BGJob Done - " + DateTime.Now.ToString(), "BG");
					return "Done";
				}
			}
			catch (Exception ex)
			{
				_common.LogError("Start - " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), "BG");
				return "Err: " + ex.Message.ToString();
			}
		}


	}
}