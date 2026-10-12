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

namespace GameRecord.Controllers
{
	public class UnknownController : Controller
	{
		private string P_GamePrefix = "AG";
		private string P_LogUrl = "http://gslog.336699bet.com";
		private string P_OperatorCode = "";
		private string P_Secretkey = "";
		private string P_ProviderCode = "BG";
		Common _common = new Common();

		//###################################################################################
		#region Declare 

		public class record
		{
			public string errCode { get; set; }
			public List<data> records { get; set; }
			public string errMsg { get; set; }
		}

		public class data
		{
			public long id { get; set; }
			public long ref_no { get; set; }
			public string site { get; set; }
			public string product { get; set; }
			public string member { get; set; }
			public string gameid { get; set; }
			public string start_time { get; set; }
			public string end_time { get; set; }
			public string match_time { get; set; }
			public string bet_detail { get; set; }
			public decimal turnover { get; set; }
			public decimal bet { get; set; }
			public decimal payout { get; set; }
			public decimal commission { get; set; }
			public decimal p_share { get; set; }
			public decimal p_win { get; set; }
			public int status { get; set; }
		}

		#endregion

		//###################################################################################
		#region Main Function 

		public bool GetGameRecord()
		{
			string startTime = GetJobTime("Start");
			string endTime = GetJobTime("End");
			string sMainID = Guid.NewGuid().ToString();

			try
			{

				string method = "/repullBettingHistoryApiClient.ashx";
				string keyOrDate = "date";
				string sign = MD5Encrypt(P_ProviderCode + P_OperatorCode + keyOrDate + P_Secretkey);
				string param = "?providercode=" + P_ProviderCode;
				param += "&keyOrdate=" + keyOrDate;
				param += "&from=" + startTime;
				param += "&to=" + endTime;
				param += "&operatorcode=" + P_OperatorCode;
				param += "&signature=" + sign;
				string webAddr = P_LogUrl + method + param;

				var httpWebRequest = (HttpWebRequest)WebRequest.Create(webAddr);
				httpWebRequest.ContentType = "application/json";
				httpWebRequest.Method = "GET";
				httpWebRequest.Timeout = 60000;
				httpWebRequest.ReadWriteTimeout = 60000;

				var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
				using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
				{
					var responseText = streamReader.ReadToEnd();
					record r = JsonConvert.DeserializeObject<record>(responseText);

					if (r.errCode == "0")
					{
						using (BingoConn db = new BingoConn())
						{
							AGGameMain agGameMain = new AGGameMain()
							{
								Id = sMainID,
								Total = JsonConvert.SerializeObject(r.records) == "0" ? 0 : r.records.Count,
								Error = r.errMsg,
								StartTime = Convert.ToDateTime(startTime),
								EndTime = Convert.ToDateTime(endTime),
								CreatedDate = DateTime.Now
							};
							db.AGGameMains.Add(agGameMain);
							db.SaveChanges();

							if (JsonConvert.SerializeObject(r.records) != "0")
							{
								foreach (var obj in r.records)
								{
									long betID = obj.id;
									if (db.AGGames.Where(x => x.BetId == betID).FirstOrDefault() == null)
									{
										AGGame agGame = new AGGame()
										{
											Id = Guid.NewGuid().ToString(),
											fId = sMainID,
											BetId = obj.id,
											RefNo = obj.ref_no,
											Site = obj.site,
											Product = obj.product,
											Member = obj.member,
											GameID = obj.gameid,
											StartTime = Convert.ToDateTime(obj.start_time),
											EndTime = Convert.ToDateTime(obj.end_time),
											MatchTime = Convert.ToDateTime(obj.match_time),
											BetDetail = obj.bet_detail,
											Turnover = obj.turnover,
											Bet = obj.bet,
											Payout = obj.payout,
											Commission = obj.commission,
											JackpotBetAmt = obj.p_share,
											JackpotPayout = obj.p_win,
											Status = obj.status,
											StatusDesc = GetStatusDesc(obj.status),
											CreatedDate = DateTime.Now
										};
										db.AGGames.Add(agGame);
									}
								}
								db.SaveChanges();
							}
						}
					}
				}

				bool isUpdJob = UpdJobTime(startTime, endTime, sMainID);

				return true;
			}
			catch (Exception ex)
			{
				_common.LogError("AG/GetGameRecord" + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), "AG");
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
				_common.LogError("AG/UpdJobTime" + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), "AG");
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
						DateTime dtSTime = Convert.ToDateTime(query.LastRunTillDate?.ToString("yyyy-MM-dd HH:mm"));
						sTime = dtSTime.ToString("yyyy-MM-ddTHH:mm:ss");
					}
					else
					{
						DateTime dtSTime = Convert.ToDateTime(DateTime.Now.ToString("yyyy-MM-dd HH:mm")) - new TimeSpan(0, 0, 3, 0, 0);
						sTime = dtSTime.ToString("yyyy-MM-ddTHH:mm:ss");
					}
				}

				return sTime;

			}
			catch (Exception ex)
			{
				_common.LogError("AG/GetJobTime" + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), "AG");
				return "Error:" + ex.Message.ToString();
			}
		}

		public string MD5Encrypt(string text)
		{
			byte[] ba = System.Security.Cryptography.MD5.Create().ComputeHash(System.Text.ASCIIEncoding.ASCII.GetBytes(text));
			string hex = BitConverter.ToString(ba);
			hex = hex.Replace("-", "");

			return hex.ToUpper();
		}

		public string GetStatusDesc(int id)
		{
			switch (id)
			{
				case 1:
					return "Valid";
				case 0:
					return "Running";
				case -1:
					return "Invalid";
				default:
					return "Unknown";
			}
		}

		#endregion

		// GET: AG
		public void AGJob()
		{
			try
			{
				bool isOK = GetGameRecord();
			}
			catch (Exception ex)
			{
				_common.LogError("Start - " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), "AG");
			}
		}
	}
}