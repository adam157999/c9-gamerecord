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
using System.Data.Entity.Migrations;

namespace GameRecord.Controllers
{
	public class GDController : Controller
	{
		private string P_ApiUrl = "http://wsgd.gdsecure88.com/MerchantAPI/ewallet.php";
		private string P_ApiUrlRpt = "http://wsgdreport.gdsecure88.com/MerchantAPI/report.php";
		private string P_SecretKey = "3c696990-11f2-469c-a159-a6eda9d0feb1"; //"9c6ab240-e0be-490a-93c4-e4f4fb4259c3"; //
		private string P_MerchantID = "BINGOTOWN";
		private string P_GamePrefix = "GD";
		Common _common = new Common();

		//###################################################################################
		#region Declare 

		public class Data
		{
			public string No { get; set; }
			public string BetID { get; set; }
			public string BetTime { get; set; }
			public string ProductID { get; set; }
			public string BetAmount { get; set; }
			public string WinLoss { get; set; }
		}

		#endregion

		//###################################################################################
		#region Main Function 

		public bool GetGameRecord()
		{
			//satrtTime = "MM/DD/YYYY HH:MM:SS"
			string startTime = GetJobTime("Start");
			string endTime = GetJobTime("End");
			string sMainID = Guid.NewGuid().ToString();

			//satrtTime = "dd/MM/YYYY HH:MM:SS"
			string startTimeSQL = ChangeDateFormatCustom(startTime);
			string endTimeSQL = ChangeDateFormatCustom(endTime);

			try
			{
				string method = "cGetBetHistory";
				string random = RandomString(5);
				string time = DateTime.Now.ToString("yyMMddHHmmss");
				string MessageID = "H" + time + random;

				var httpWebRequest = (HttpWebRequest)WebRequest.Create(P_ApiUrl);
				httpWebRequest.ContentType = "application/xml";
				httpWebRequest.Method = "POST";
				httpWebRequest.Timeout = 60000;
				httpWebRequest.ReadWriteTimeout = 60000;

				using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
				{
					string xml = "<?xml version=\"1.0\"?>\r\n" +
											 "<Request>\r\n  " +
											 "  <Header>\r\n    " +
											 "      <Method>" + method + "</Method>\r\n    " +
											 "      <MerchantID>" + P_MerchantID + "</MerchantID>\r\n    " +
											 "      <MessageID>" + MessageID + "</MessageID>\r\n  " +
											 "  </Header>\r\n  " +
											 "  <Param>\r\n    " +
											 "      <FromTime>" + startTime + "</FromTime>\r\n    " +
											 "      <ToTime>" + endTime + "</ToTime>\r\n    " +
											 "      <Index>0</Index>\r\n    " +
											 //"      <UserID>" + UID + "</UserID>\r\n    " +
											 "      <SearchByBalanceTime>1</SearchByBalanceTime>\r\n    " +
											 "  </Param>\r\n" +
											 "</Request>";


					streamWriter.Write(xml);
					streamWriter.Flush();
				}
				var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
				using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
				{
					var responseText = streamReader.ReadToEnd();

					XmlDocument xmlDoc = new XmlDocument();
					xmlDoc.LoadXml(responseText);

					string rErrorCode = xmlDoc.SelectSingleNode("Reply/Header/ErrorCode").InnerText;
					string rTotalRecord = xmlDoc.SelectSingleNode("Reply/Param/TotalRecord").InnerText;
					string rErrorDesc = xmlDoc.SelectSingleNode("Reply/Param/ErrorDesc").InnerText;

					if (rTotalRecord == "")
					{
						rTotalRecord = "0";
					}

					XmlNodeList xnList = xmlDoc.SelectNodes("/Reply/Param/BetInfo");

					using (BingoConn db = new BingoConn())
					{
						//Insert Main table
						GDGameMain gDGameMain = new GDGameMain()
						{
							Id = sMainID,
							ErrorCode = Convert.ToInt32(rErrorCode),
							ErrorDesc = rErrorDesc,
							MessageID = MessageID,
							TotalRecord = Convert.ToInt32(rTotalRecord),
							startTime = Convert.ToDateTime(startTimeSQL),
							endTime = Convert.ToDateTime(endTimeSQL),
							CreateDate = DateTime.Now
						};
						db.GDGameMains.Add(gDGameMain);
						db.SaveChanges();

						//Insert Result table
						foreach (XmlNode xn in xnList)
						{
							string uuid = Guid.NewGuid().ToString();

							XmlNode oNode = xn.SelectSingleNode(".//BetArrays");
							XmlNodeList dxnList = oNode.SelectNodes("Bet");

							foreach (XmlNode dxn in dxnList)
							{
								bool isExist = IsBetIDExist(xn["BetID"].InnerText);
								if (isExist == false)
								{
									string sRtnBetTime = xn["BetTime"].InnerText; // (Convert.ToDateTime(xn["BetTime"].InnerText)).ToString("dd/MM/yyyy HH:mm:ss");
									string sRtnBalanceTime = xn["BalanceTime"].InnerText; // (Convert.ToDateTime(xn["BalanceTime"].InnerText)).ToString("dd/MM/yyyy HH:mm:ss");

									sRtnBetTime = ChangeDateFormatCustom(sRtnBetTime);
									sRtnBalanceTime = ChangeDateFormatCustom(sRtnBalanceTime);

									GDGame gDGame = new GDGame()
									{
										Id = uuid,
										fId = sMainID,
										UserID = xn["UserID"].InnerText,
										Currency = xn["Currency"].InnerText,
										ProductID = xn["ProductID"].InnerText,
										ClientType = xn["ClientType"].InnerText,
										GameInterface = xn["GameInterface"].InnerText,
										BetID = xn["BetID"].InnerText,
										BetType = xn["BetType"].InnerText,
										BetAmount = Convert.ToDouble(xn["BetAmount"].InnerText),
										WinLoss = Convert.ToDouble(xn["WinLoss"].InnerText),
										BetResult = xn["BetResult"].InnerText,
										GameID = dxn["GameID"].InnerText,
										SubBetType = dxn["SubBetType"].InnerText,
										GameResult = dxn["GameResult"].InnerText,
										WinningBet = dxn["WinningBet"].InnerText,
										TableID = dxn["TableID"].InnerText,
										DealerID = dxn["DealerID"].InnerText,
										Total = rTotalRecord,
										CreatedDate = DateTime.Now
									};

									gDGame.BetTime = Convert.ToDateTime(sRtnBetTime);
									gDGame.BalanceTime = Convert.ToDateTime(sRtnBalanceTime);

									db.GDGames.AddOrUpdate(gDGame);
								}
							}
							db.SaveChanges();
						}
					}
					//If record more than 500, need to get the record from Page 2 and above
					decimal dCnt = Convert.ToDecimal(rTotalRecord) / Convert.ToDecimal(500);
					int iPageIndex = Int32.Parse(Math.Ceiling(dCnt).ToString());

					if (iPageIndex > 1)
					{
						for (int x = 2; x <= iPageIndex; x++)
						{
							bool IsOK = GetGameRecordExtend(500 + x - 1);
						}
					}

					bool isUpdJob = UpdJobTime(startTimeSQL, endTimeSQL, sMainID);

					return true;
				}
			}
			catch (Exception ex)
			{
				_common.LogError("GD/GetGameRecord" + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), "GD");
				return false;
			}
		}

		public bool GetGameRecordExtend(int pageIndex)
		{
			//satrtTime = "MM/DD/YYYY HH:MM:SS"
			string startTime = GetJobTime("Start");
			string endTime = GetJobTime("End");
			string sMainID = Guid.NewGuid().ToString();

			//satrtTime = "dd/MM/YYYY HH:MM:SS"
			string startTimeSQL = ChangeDateFormatCustom(startTime);
			string endTimeSQL = ChangeDateFormatCustom(endTime);

			try
			{
				string method = "cGetBetHistory";
				string random = RandomString(5);
				string time = DateTime.Now.ToString("yyMMddHHmmss");
				string MessageID = "H" + time + random;

				var httpWebRequest = (HttpWebRequest)WebRequest.Create(P_ApiUrl);
				httpWebRequest.ContentType = "application/xml";
				httpWebRequest.Method = "POST";
				httpWebRequest.Timeout = 60000;
				httpWebRequest.ReadWriteTimeout = 60000;

				using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
				{
					string xml = "<?xml version=\"1.0\"?>\r\n" +
											 "<Request>\r\n  " +
											 "  <Header>\r\n    " +
											 "      <Method>" + method + "</Method>\r\n    " +
											 "      <MerchantID>" + P_MerchantID + "</MerchantID>\r\n    " +
											 "      <MessageID>" + MessageID + "</MessageID>\r\n  " +
											 "  </Header>\r\n  " +
											 "  <Param>\r\n    " +
											 "      <FromTime>" + startTime + "</FromTime>\r\n    " +
											 "      <ToTime>" + endTime + "</ToTime>\r\n    " +
											 "      <Index>" + pageIndex + "</Index>\r\n    " +
											 //"      <UserID>" + UID + "</UserID>\r\n    " +
											 "      <SearchByBalanceTime>1</SearchByBalanceTime>\r\n    " +
											 "  </Param>\r\n" +
											 "</Request>";


					streamWriter.Write(xml);
					streamWriter.Flush();
				}
				var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
				using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
				{
					var responseText = streamReader.ReadToEnd();

					XmlDocument xmlDoc = new XmlDocument();
					xmlDoc.LoadXml(responseText);

					string rErrorCode = xmlDoc.SelectSingleNode("Reply/Header/ErrorCode").InnerText;
					string rTotalRecord = xmlDoc.SelectSingleNode("Reply/Param/TotalRecord").InnerText;
					string rErrorDesc = xmlDoc.SelectSingleNode("Reply/Param/ErrorDesc").InnerText;

					//List<Data> result = new List<Data>();

					XmlNodeList xnList = xmlDoc.SelectNodes("/Reply/Param/BetInfo");

					using (BingoConn db = new BingoConn())
					{
						//Insert Main table
						GDGameMain gDGameMain = new GDGameMain()
						{
							Id = sMainID,
							ErrorCode = Convert.ToInt32(rErrorCode),
							ErrorDesc = rErrorDesc,
							MessageID = MessageID,
							TotalRecord = Convert.ToInt32(rTotalRecord),
							startTime = Convert.ToDateTime(startTimeSQL),
							endTime = Convert.ToDateTime(endTimeSQL),
							CreateDate = DateTime.Now
						};
						db.GDGameMains.Add(gDGameMain);
						db.SaveChanges();

						//Insert Result table
						foreach (XmlNode xn in xnList)
						{
							XmlNode oNode = xn.SelectSingleNode(".//BetArrays");
							XmlNodeList dxnList = oNode.SelectNodes("Bet");

							foreach (XmlNode dxn in dxnList)
							{
								bool isExist = IsBetIDExist(xn["BetID"].InnerText);
								if (isExist == false)
								{
									GDGame gDGame = new GDGame()
									{
										Id = Guid.NewGuid().ToString(),
										fId = sMainID,
										UserID = xn["UserID"].InnerText,
										Currency = xn["Currency"].InnerText,
										BetTime = Convert.ToDateTime(xn["BetTime"].InnerText),
										BalanceTime = Convert.ToDateTime(xn["BalanceTime"].InnerText),
										ProductID = xn["ProductID"].InnerText,
										ClientType = xn["ClientType"].InnerText,
										GameInterface = xn["GameInterface"].InnerText,
										BetID = xn["BetID"].InnerText,
										BetType = xn["BetType"].InnerText,
										BetAmount = Convert.ToDouble(xn["BetAmount"].InnerText),
										WinLoss = Convert.ToDouble(xn["WinLoss"].InnerText),
										BetResult = xn["BetResult"].InnerText,
										GameID = xn["BetArrays/Bet/GameID"].InnerText,
										SubBetType = dxn["SubBetType"].InnerText,
										GameResult = dxn["GameResult"].InnerText,
										WinningBet = dxn["WinningBet"].InnerText,
										TableID = dxn["TableID"].InnerText,
										DealerID = dxn["DealerID"].InnerText,
										Total = rTotalRecord,
										CreatedDate = DateTime.Now
									};
									db.GDGames.AddOrUpdate(gDGame);

								}
							}
							db.SaveChanges();
						}
					}
					return true;
				}
			}
			catch (Exception ex)
			{
				_common.LogError("GD/GetGameRecordExtend" + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), "GD");
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
				_common.LogError("GD/UpdJobTime" + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), "GD");
				return false;
			}
		}


		#endregion

		//###################################################################################
		#region Function 

		public string GetJobTime(string action)
		{
			//Format "MM/DD/YYYY HH:MM:SS"
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
						DateTime dtSTime = Convert.ToDateTime(query.LastRunTillDate?.ToString("yyyy-MM-dd HH:mm"));
						sTime = dtSTime.ToString("MM/dd/yyyy HH:mm:ss");
					}
					else
					{
						DateTime dtSTime = Convert.ToDateTime(DateTime.Now.ToString("yyyy-MM-dd HH:mm")) - new TimeSpan(0, 0, 3, 0, 0);
						sTime = dtSTime.ToString("MM/dd/yyyy HH:mm:ss");
					}

					//if (action == "Start")
					//{
					//	DateTime dtSTime = Convert.ToDateTime(query.LastRunTillDate);
					//	sTime = dtSTime.ToString("MM/dd/yyyy HH:mm:ss");
					//}
					//else
					//{
					//	DateTime dtSTime = DateTime.Now - new TimeSpan(0, 0, 3, 0);
					//	sTime = dtSTime.ToString("MM/dd/yyyy HH:mm:ss");
					//}
				}

				return sTime;

			}
			catch (Exception ex)
			{
				_common.LogError("GD/GetJobTime" + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), "GD");
				return "Error:" + ex.Message.ToString();
			}
		}

		public string GetJobTimeSave(string action)
		{
			//Format "MM/DD/YYYY HH:MM:SS"
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
						DateTime dtSTime = Convert.ToDateTime(query.LastRunTillDate);
						sTime = dtSTime.ToString("dd/MM/yyyy HH:mm:ss");
					}
					else
					{
						DateTime dtSTime = DateTime.Now - new TimeSpan(0, 0, 3, 0);
						sTime = dtSTime.ToString("dd/MM/yyyy HH:mm:ss");
					}
				}

				return sTime;

			}
			catch (Exception ex)
			{
				_common.LogError("GD/GetJobTimeSave" + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), "GD");
				return "Error:" + ex.Message.ToString();
			}
		}

		private static Random random = new Random();

		public static string RandomString(int length)
		{
			const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
			return new string(Enumerable.Repeat(chars, length)
				.Select(s => s[random.Next(s.Length)]).ToArray());
		}

		public bool IsBetIDExist(string sBetID)
		{
			try
			{
				string sRefNo = "";

				using (var bingoConn = new BingoConn())
				{
					var query = bingoConn.GDGames
															.Where(s => s.BetID == sBetID)
															.FirstOrDefault();
					if (query != null)
					{
						sRefNo = query.BetID.ToString();
					}
				}

				if (sRefNo == "")
				{
					return false;
				}
				else
				{
					return true;
				}

			}
			catch (Exception ex)
			{
				_common.LogError("GD/IsBetIDExist" + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), "GD");
				return false;
			}
		}
		#endregion

		// GET: GD
		public void GDJob()
		{
			try
			{
				bool isOK = GetGameRecord();
			}
			catch (Exception ex)
			{
				_common.LogError("Start - " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), "GD");
			}
		}


		public string ChangeDateFormatCustom(string Source)
		{
			try
			{
				string sResult = Source;

				var sMonth = Convert.ToInt32(sResult.Split(' ')[0].Split('/')[0]);
				var sDay = Convert.ToInt32(sResult.Split(' ')[0].Split('/')[1]);
				var sYear = Convert.ToInt32(sResult.Split(' ')[0].Split('/')[2]);
				sResult = sDay + "/" + sMonth + "/" + sYear + " " + sResult.Split(' ')[1];

				return sResult;
			}
			catch
			{
				return Source;
			}
		}
	}
}