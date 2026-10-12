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

namespace GameRecord.Controllers
{
	public class BKAController : Controller
	{
		public static readonly object MyLock = new object();
		private string P_GamePrefix = "BKA";
		Common _common = new Common();

		//###################################################################################
		#region Declare 

		public class GetGameRec
		{
			public int Code { get; set; }
			public string Message { get; set; }
			public List<GetGameRecData> Datas { get; set; }
		}

		public class GetGameRecData
		{
			public int GameID { get; set; }
			public List<GetGameRecRecord> Records { get; set; }
		}

		public class GetGameRecRecord
		{
			public string Title { get; set; }
			public string SerialNo { get; set; }
			public DateTime? DateTime { get; set; }
			public string Account { get; set; }
			public decimal Amount { get; set; }
			public decimal WinLoss { get; set; }
		}

		#endregion

		//###################################################################################
		#region Main Function 

		public bool GetGameRecord()
		{
			string startTime = GetJobTime("Start");
			string endTime = GetJobTime("End");
			//string startTime = "2020-09-17 00:00:00";
			//string endTime = "2020-09-17 12:00:00";
			string sMainID = Guid.NewGuid().ToString();

			try
			{
				var client = new RestClient("http://180.215.206.36:4613/Cash/CashAPI.asmx?op=GetGameRec");
				client.Timeout = -1;
				var request = new RestRequest(Method.POST);
				request.AddHeader("Content-Type", "text/xml");
				request.AddParameter("text/xml", "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n"
						+ "<soap:Envelope xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" xmlns:soap=\"http://schemas.xmlsoap.org/soap/envelope/\">\r\n  "
						+ "<soap:Body>\r\n    "
						+ "<GetGameRec xmlns=\"http://tempuri.org/\">\r\n      "
						+ "<CompanyID>BingoTown</CompanyID>\r\n      "
						+ "<GameID>3</GameID>\r\n      "
						+ "<TimeStart>" + startTime + "</TimeStart>\r\n      "
						+ "<TimeEnd>" + endTime + "</TimeEnd>\r\n    "
						+ "</GetGameRec>\r\n  "
						+ "</soap:Body>\r\n"
						+ "</soap:Envelope>", ParameterType.RequestBody);
				IRestResponse response = client.Execute(request);

				XmlDocument xmlDoc1 = new XmlDocument();
				xmlDoc1.LoadXml(response.Content);
				string sInnerText = xmlDoc1.InnerText;
				var result = JsonConvert.DeserializeObject<GetGameRec>(sInnerText);

				using (BingoConn db = new BingoConn())
				{
					BKAGameMain bkaGameMain = new BKAGameMain()
					{
						Id = sMainID,
						Total = result.Datas.Count > 0 ? result.Datas[0].Records.Count : 0,
						Error = result.Message,
						StartTime = Convert.ToDateTime(startTime),
						EndTime = Convert.ToDateTime(endTime),
						CreatedDate = DateTime.Now
					};
					db.BKAGameMains.Add(bkaGameMain);
					db.SaveChanges();

					if (result.Code == 1)
					{
						if (result.Datas.Count > 0)
						{
							foreach (var d in result.Datas)
							{
								if (d.Records.Count > 0)
								{
									foreach (var r in d.Records)
									{
										try
										{
											if (db.BKAGames.Where(x => x.SerialNo == r.SerialNo).FirstOrDefault() == null)
											{
												BKAGame bkaGame = new BKAGame()
												{
													fId = sMainID,
													Title = r.Title,
													SerialNo = r.SerialNo,
													DateTime = r.DateTime,
													Account = r.Account,
													Amount = r.Amount,
													WinLoss = r.WinLoss,
													CreatedDate = DateTime.Now
												};

												db.BKAGames.Add(bkaGame);
											}
										}
										catch(Exception ex)
										{
											_common.LogError("Record " + r.SerialNo + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), "BKA");
										}
									}
									db.SaveChanges();
								}
							}
						}

						bool isUpdJob = UpdJobTime(startTime, endTime, sMainID);
					}
				}

				return true;
			}
			catch (Exception ex)
			{
				_common.LogError("BKA/GetGameRecord" + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), "BKA");
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
				_common.LogError("BKA/UpdJobTime" + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), "BKA");
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
				_common.LogError("BKA/GetJobTime" + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), "BKA");
				return "Error:" + ex.Message.ToString();
			}
		}

		#endregion

		// GET: BKA
		public void BKAJob()
		{
			try
			{
				lock(MyLock)
				{
					bool isOK = GetGameRecord();
				}
			}
			catch (Exception ex)
			{
				_common.LogError("Start - " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), "BKA");
			}
		}
	}
}