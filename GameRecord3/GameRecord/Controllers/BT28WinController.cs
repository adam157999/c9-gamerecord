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
using System.Data.Entity.Validation;
using static GameRecord.Controllers.BaseConnector;

namespace GameRecord.Controllers
{
	public class BT28WinController : Controller
	{
		public static readonly object MyLock = new object();
		private string P_ApiUrl = "http://api.28win.com";
		private string P_GamePrefix = "BT28WIN";
		private string apiUser = "bingo666";
		private string apiPass = "bingo666@win2020";
		private string user = "bingo666";
		private string pass = "778899";
		private string drawDate = DateTime.Now.ToString("yyyy-MM-dd");
		//private string drawDate = "2023-06-26";
		private bool IsNextPage = false;
		private int P_PageIndex = 1;
		Common _common = new Common();
		private BT28WINConnector api;
		public string BingoConn = System.Configuration.ConfigurationManager.ConnectionStrings["BingoConn"].ToString();

		public BT28WinController(GameEnvironment Environment = GameEnvironment.PROD)
		{
			api = new BT28WINConnector(Environment: Environment);
		}

		public BT28WinController()
		{
			api = new BT28WINConnector(Environment: GameEnvironment.STAG);
		}

		//public class BetListData
		//{
		//	public string betID { get; set; }
		//	public string loginID { get; set; }
		//	public string dateTime { get; set; }
		//	public string drawType { get; set; }
		//	public string drawDate { get; set; }
		//	public string betNumber { get; set; }
		//	public string betPosition { get; set; }
		//	public string bet { get; set; }
		//}

		//###################################################################################
		#region Main Function 
		public bool GetGameRecord()
		{

			string startTime = GetJobTime("Start");
			string endTime = GetJobTime("End");
			string sMainID = Guid.NewGuid().ToString();
			P_PageIndex = 1;

			try
			{
				for (var i = 0; i < 7; i++)
				{
					string dateFrom = DateTime.Now.AddDays(i).ToString("yyyy-MM-dd");
					string dateTo = DateTime.Now.AddDays(i).ToString("yyyy-MM-dd");
					//string dateFrom = "2023-06-26";
					//string dateTo = "2023-06-26";

					BT28WIN.GetBetListByPageResponse resp = api.GetBetListByPage(Convert.ToDateTime(dateFrom), Convert.ToDateTime(dateTo), P_PageIndex);

					using (BingoConn db = new BingoConn())
					{
						if (resp != null && resp.errorCode == 0)
						{
							//Insert Main table
							BT28WinGameMain BT28WinGameMain = new BT28WinGameMain()
							{
								Id = sMainID,
								Total = resp != null && resp.errorCode == 0 ? resp.betData.Count() : 0,
								StartTime = Convert.ToDateTime(startTime),
								EndTime = Convert.ToDateTime(endTime),
								CreatedDate = DateTime.Now
							};
							db.BT28WinGameMain.Add(BT28WinGameMain);

							//Insert Result table
							foreach (BT28WIN.BetData d in resp.betData)
							{
								//string betID = xn["betID"].InnerText.ToString();
								try
								{
									var query = db.BT28WinGame.Where(s => s.BetID == d.betID).FirstOrDefault();

									if (query == null)
									{
										BT28WinGame bT28WinGame = new BT28WinGame()
										{
											fId = sMainID,
											BetID = d.betID,
											LoginID = d.loginID,
											OrderDateTime = Convert.ToDateTime(d.dateTime),
											DrawType = Convert.ToInt32(d.drawType),
											Provider = GetProviderName(Convert.ToInt32(d.drawType)),
											DrawDate = Convert.ToDateTime(d.drawDate),
											BetNumber = d.betNumber,
											BetPosition = d.betPosition,
											BetAmount = Convert.ToDecimal(d.bet),
											WinLoss = 0,
											Status = "P",
											CreatedDate = DateTime.Now
										};
										db.BT28WinGame.Add(bT28WinGame);
									}
								}
								catch (Exception ex)
								{
									_common.LogError("Record " + d.betID + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), "BT28Win");
								}
							}
							db.SaveChanges();

							if (resp.betData.Count() > 0)
								IsNextPage = true;
							else
								IsNextPage = false;

							while (IsNextPage == true)
							{
								P_PageIndex += 1;
								GetGameRecordExtend(sMainID, dateFrom, dateTo);
							}
						}

						var dd = Convert.ToDateTime(drawDate);
						var result = db.BT28WinGameResult.Where(s => s.DrawDate == dd).FirstOrDefault();
						if (result == null)
						{
							if (IsTodayDrawResultOut())
							{
								updatePendingRecord();
							}
						}
					}
				}


				bool isUpdJob = UpdJobTime(startTime, endTime, sMainID);

				return true;
			}
			catch (DbEntityValidationException e)
			{
				_common.LogError("BT28Win/GetGameRecord" + " " + DateTime.Now.ToString() + "  " + e.Message.ToString(), "BT28Win");

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
				throw;
			}

			catch (Exception ex)
			{
				_common.LogError("BT28Win/GetGameRecord" + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), "BT28Win");
				return false;
			}
		}

		public bool GetGameRecordExtend(string mainID, string dateFrom, string dateTo)
		{
			try
			{
				BT28WIN.GetBetListByPageResponse resp = api.GetBetListByPage(Convert.ToDateTime(dateFrom), Convert.ToDateTime(dateTo), P_PageIndex);

				using (BingoConn db = new BingoConn())
				{
					if (resp != null && resp.errorCode == 0)
					{
						//Insert Result table
						foreach (BT28WIN.BetData d in resp.betData)
						{
							//string betID = xn["betID"].InnerText.ToString();
							try
							{
								var query = db.BT28WinGame.Where(s => s.BetID == d.betID).FirstOrDefault();

								if (query == null)
								{
									BT28WinGame bT28WinGame = new BT28WinGame()
									{
										fId = mainID,
										BetID = d.betID,
										LoginID = d.loginID,
										OrderDateTime = Convert.ToDateTime(d.dateTime),
										DrawType = Convert.ToInt32(d.drawType),
										Provider = GetProviderName(Convert.ToInt32(d.drawType)),
										DrawDate = Convert.ToDateTime(d.drawDate),
										BetNumber = d.betNumber,
										BetPosition = d.betPosition,
										BetAmount = Convert.ToDecimal(d.bet),
										WinLoss = 0,
										Status = "P",
										CreatedDate = DateTime.Now
									};
									db.BT28WinGame.Add(bT28WinGame);
								}
							}
							catch (Exception ex)
							{
								_common.LogError("Record " + d.betID + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), "BT28Win");
							}
						}
						db.SaveChanges();

						if (resp.betData.Count() > 0)
							IsNextPage = true;
						else
							IsNextPage = false;
					}
					else
					{
						IsNextPage = false;
					}
				}

				return true;

				//string method = "/betlistPage.aspx";
				//string param = string.Format("?apiUser={0}&apiPass={1}&user={2}&pass={3}&dateFrom={4}&dateTo={5}&page={6}", apiUser, apiPass, user, pass, dateFrom, dateTo, P_PageIndex);
				//string apiurl = P_ApiUrl + method + param;

				//var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
				//httpWebRequest.ContentType = "text/xml";
				//httpWebRequest.Method = "GET";
				//httpWebRequest.Timeout = 1200000;
				//httpWebRequest.ReadWriteTimeout = 1200000;

				//var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
				//using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
				//{
				//    var responseText = streamReader.ReadToEnd();

				//    XmlDocument xmlDoc = new XmlDocument();
				//    xmlDoc.LoadXml(responseText);

				//    string rErrorCode = xmlDoc.SelectSingleNode("response/errorCode").InnerText;

				//    XmlNodeList xnList = xmlDoc.SelectNodes("response/betData");

				//    using (BingoConn db = new BingoConn())
				//    {
				//        if (rErrorCode == "0")
				//        {
				//            //Insert Result table
				//            foreach (XmlNode xn in xnList)
				//            {
				//                string betID = xn["betID"].InnerText.ToString();
				//                try
				//                {
				//                    var query = db.BT28WinGame.Where(s => s.BetID == betID).FirstOrDefault();

				//                    if (query == null)
				//                    {
				//                        BT28WinGame bT28WinGame = new BT28WinGame()
				//                        {
				//                            fId = mainID,
				//                            BetID = xn["betID"].InnerText,
				//                            LoginID = xn["loginID"].InnerText,
				//                            OrderDateTime = Convert.ToDateTime(xn["dateTime"].InnerText),
				//                            DrawType = Convert.ToInt32(xn["drawType"].InnerText),
				//                            Provider = GetProviderName(Convert.ToInt32(xn["drawType"].InnerText)),
				//                            DrawDate = Convert.ToDateTime(xn["drawDate"].InnerText),
				//                            BetNumber = xn["betNumber"].InnerText,
				//                            BetPosition = xn["betPosition"].InnerText,
				//                            BetAmount = Convert.ToDecimal(xn["bet"].InnerText),
				//                            WinLoss = 0,
				//                            Status = "P",
				//                            CreatedDate = DateTime.Now
				//                        };
				//                        db.BT28WinGame.Add(bT28WinGame);
				//                    }
				//                }
				//                catch (Exception ex)
				//                {
				//                    _common.LogError("Record " + betID + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), "BT28Win");
				//                }
				//            }
				//            db.SaveChanges();

				//            if (xnList.Count > 0)
				//                IsNextPage = true;
				//            else
				//                IsNextPage = false;
				//        }
				//        else
				//        {
				//            IsNextPage = false;
				//        }
				//    }

				//    return true;
				//}
			}
			catch (DbEntityValidationException e)
			{
				_common.LogError("BT28Win/GetGameRecordExtend" + " " + DateTime.Now.ToString() + "  " + e.Message.ToString(), "BT28Win");

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
				throw;
			}
			catch (Exception ex)
			{
				_common.LogError("BT28Win/GetGameRecordExtend" + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), "BT28Win");
				return false;
			}
		}

		//public bool GetResult()
		//{
		//	string startTime = GetJobTime("Start");
		//	string endTime = GetJobTime("End");
		//	string sMainID = Guid.NewGuid().ToString();

		//	try
		//	{
		//		using (BingoConn db = new BingoConn())
		//		{
		//			var dd = Convert.ToDateTime(drawDate);
		//			var result = db.BT28WinGameResult.Where(s => s.DrawDate == dd).FirstOrDefault();
		//			if (result == null)
		//			{
		//				if (IsTodayDrawResultOut())
		//				{
		//					updatePendingRecord();
		//				}
		//			}
		//		}

		//		bool isUpdJob = UpdJobTime(startTime, endTime, sMainID);

		//		return true;
		//	}
		//	catch (DbEntityValidationException e)
		//	{
		//		_common.LogError("BT28Win/GetGameRecord" + " " + DateTime.Now.ToString() + "  " + e.Message.ToString(), "BT28Win");

		//		foreach (var eve in e.EntityValidationErrors)
		//		{
		//			Console.WriteLine("Entity of type \"{0}\" in state \"{1}\" has the following validation errors:",
		//											eve.Entry.Entity.GetType().Name, eve.Entry.State);
		//			foreach (var ve in eve.ValidationErrors)
		//			{
		//				Console.WriteLine("- Property: \"{0}\", Error: \"{1}\"",
		//												ve.PropertyName, ve.ErrorMessage);
		//			}
		//		}
		//		throw;
		//	}
		//	catch (Exception ex)
		//	{
		//		_common.LogError("BT28Win/GetGameRecord" + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), "BT28Win");
		//		return false;
		//	}
		//}

		public bool checkProviderResultOut(int provider)
		{
			try
			{
				if (!provider.Equals(null))
				{
					int totoCnt = 0;
					int gdCnt = 0;

					BT28WIN.GetDrawResultResponse resp = api.GetDrawResult(Convert.ToDateTime(drawDate));

					if (resp != null && resp.errorCode == 0)
					{
						foreach (BT28WIN.DrawResult d in resp.drawResult)
						{
							int drawType = Convert.ToInt32(d.drawType);
							if (drawType == provider)
							{
								if (provider == 3)
								{
									if (Convert.ToString(d.betType) == "4D")
										totoCnt += 1;
									if (Convert.ToString(d.betType) == "5D")
										totoCnt += 1;
									if (Convert.ToString(d.betType) == "6D")
										totoCnt += 1;
								}
								else if (provider == 8)
								{
									if (Convert.ToString(d.betType) == "4D")
										gdCnt += 1;
									if (Convert.ToString(d.betType) == "6D")
										gdCnt += 1;
								}
								else
									return true;
							}
						}

						if (provider == 3 && totoCnt == 3)
							return true;
						if (provider == 8 && gdCnt == 2)
							return true;
					}
				}

				return false;
			}
			catch (Exception ex)
			{
				_common.LogError("BT28Win/checkProviderResultOut" + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), "BT28Win");
				return false;
			}
		}

		public void checkAndInsertDrawResult()
		{
			try
			{
				var dd = Convert.ToDateTime(drawDate);
				using (BingoConn db = new BingoConn())
				{
					var query1 = db.BT28WinGameResult.Where(s => s.DrawDate == dd).FirstOrDefault();
					if (query1 == null)
					{
						BT28WinGameResult bT28WinGameResult = new BT28WinGameResult()
						{
							DrawDate = dd,
							CreatedDate = DateTime.Now
						};
						db.BT28WinGameResult.Add(bT28WinGameResult);
						db.SaveChanges();
					}
				}
			}
			catch (Exception ex)
			{
				_common.LogError("BT28Win/checkAndInsertDrawResult" + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), "BT28Win");
			}
		}

		public void updatePendingRecord()
		{
			try
			{
				using (BingoConn db = new BingoConn())
				{
					var dd = Convert.ToDateTime(drawDate);
					var providerList = db.BT28WinGame.Where(s => s.Status == "P" && s.DrawDate == dd).Select(s => s.DrawType).Distinct().ToList();

					if (providerList.Count > 0)
					{
						foreach (int p in providerList)
						{
							if (checkProviderResultOut(p))
							{
								BT28WIN.GetWinningNumberResponse resp = api.GetWinningNumber(Convert.ToDateTime(drawDate), Convert.ToDateTime(drawDate));

								if (resp != null && resp.errorCode == 0)
								{
									foreach (BT28WIN.WinningBetData d in resp.betData)
									{
										//string betID = xn["betID"].InnerText.ToString();
										var query = db.BT28WinGame.Where(s => s.BetID == d.betID).FirstOrDefault();
										var querystrike = db.BT28WinGameStrike.Where(s => s.BetID == d.betID).FirstOrDefault();

										if (query != null && querystrike == null)
										{
											query.Status = "C";
											query.WinLoss = Convert.ToDecimal(d.strike);
											query.ModifiedDate = DateTime.Now;
											db.SaveChanges();

											BT28WinGameStrike bT28WinGameStrike = new BT28WinGameStrike()
											{
												BetID = d.betID,
												LoginID = d.loginID,
												OrderDateTime = Convert.ToDateTime(d.dateTime),
												DrawType = Convert.ToInt32(d.drawType),
												Provider = GetProviderName(Convert.ToInt32(d.drawType)),
												DrawDate = Convert.ToDateTime(d.drawDate),
												BetNumber = d.betNumber,
												StrikePosition = d.strikePosition,
												BetAmount = Convert.ToDecimal(d.bet),
												StrikeAmount = Convert.ToDecimal(d.strike),
												IsTransferred = false,
												IsProcess = false,
												CreatedDate = DateTime.Now
											};
											db.BT28WinGameStrike.Add(bT28WinGameStrike);
											db.SaveChanges();
										}
									}

									var dd2 = Convert.ToDateTime(drawDate);
									db.BT28WinGame.Where(s => s.DrawDate == dd2 && s.DrawType == p && s.Status == "P").ToList().ForEach(q =>
									{
										q.Status = "C";
										q.WinLoss = 0;
										q.ModifiedDate = DateTime.Now;
									});

									db.SaveChanges();
								}
								//provider result release but no ppl kena
								else
								{
									var dd2 = Convert.ToDateTime(drawDate);
									db.BT28WinGame.Where(s => s.DrawDate == dd2 && s.DrawType == p && s.Status == "P").ToList().ForEach(q =>
									{
										q.Status = "C";
										q.WinLoss = 0;
										q.ModifiedDate = DateTime.Now;
									});

									db.SaveChanges();
								}

								//string method = "/winNumber.aspx";
								//string param = string.Format("?apiUser={0}&apiPass={1}&user={2}&pass={3}&dateFrom={4}&dateTo={5}", apiUser, apiPass, user, pass, drawDate, drawDate);

								//string apiurl = P_ApiUrl + method + param;

								//var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
								//httpWebRequest.ContentType = "text/xml";
								//httpWebRequest.Method = "GET";
								//httpWebRequest.Timeout = 60000;
								//httpWebRequest.ReadWriteTimeout = 60000;

								//var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
								//using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
								//{
								//    var responseText = streamReader.ReadToEnd();

								//    XmlDocument xmlDoc = new XmlDocument();
								//    xmlDoc.LoadXml(responseText);

								//    string rErrorCode = xmlDoc.SelectSingleNode("response/errorCode").InnerText;
								//    string rErrorDesc = xmlDoc.SelectSingleNode("response/desc").InnerText;

								//    if (rErrorCode == "0")
								//    {
								//        XmlNodeList xnList = xmlDoc.SelectNodes("response/betData");
								//        foreach (XmlNode xn in xnList)
								//        {
								//            string betID = xn["betID"].InnerText.ToString();
								//            var query = db.BT28WinGame.Where(s => s.BetID == betID).FirstOrDefault();
								//            var querystrike = db.BT28WinGameStrike.Where(s => s.BetID == betID).FirstOrDefault();

								//            if (query != null && querystrike == null)
								//            {
								//                query.Status = "C";
								//                query.WinLoss = Convert.ToDecimal(xn["strike"].InnerText);
								//                query.ModifiedDate = DateTime.Now;
								//                db.SaveChanges();

								//                BT28WinGameStrike bT28WinGameStrike = new BT28WinGameStrike()
								//                {
								//                    BetID = xn["betID"].InnerText,
								//                    LoginID = xn["loginID"].InnerText,
								//                    OrderDateTime = Convert.ToDateTime(xn["dateTime"].InnerText),
								//                    DrawType = Convert.ToInt32(xn["drawType"].InnerText),
								//                    Provider = GetProviderName(Convert.ToInt32(xn["drawType"].InnerText)),
								//                    DrawDate = Convert.ToDateTime(xn["drawDate"].InnerText),
								//                    BetNumber = xn["betNumber"].InnerText,
								//                    StrikePosition = xn["strikePosition"].InnerText,
								//                    BetAmount = Convert.ToDecimal(xn["bet"].InnerText),
								//                    StrikeAmount = Convert.ToDecimal(xn["strike"].InnerText),
								//                    IsTransferred = false,
								//                    IsProcess = false,
								//                    CreatedDate = DateTime.Now
								//                };
								//                db.BT28WinGameStrike.Add(bT28WinGameStrike);
								//                db.SaveChanges();
								//            }
								//        }

								//        var dd2 = Convert.ToDateTime(drawDate);
								//        db.BT28WinGame.Where(s => s.DrawDate == dd2 && s.DrawType == p && s.Status == "P").ToList().ForEach(q =>
								//        {
								//            q.Status = "C";
								//            q.WinLoss = 0;
								//            q.ModifiedDate = DateTime.Now;
								//        });

								//        db.SaveChanges();
								//    }
								//    //provider result release but no ppl kena
								//    else
								//    {
								//        var dd2 = Convert.ToDateTime(drawDate);
								//        db.BT28WinGame.Where(s => s.DrawDate == dd2 && s.DrawType == p && s.Status == "P").ToList().ForEach(q =>
								//        {
								//            q.Status = "C";
								//            q.WinLoss = 0;
								//            q.ModifiedDate = DateTime.Now;
								//        });

								//        db.SaveChanges();
								//    }
								//}
							}
						}

						var pList = db.BT28WinGame.Where(s => s.Status == "P" && s.DrawDate == dd).ToList();
						if (pList.Count == 0)
						{
							checkAndInsertDrawResult();
						}
					}
					else
					{
						checkAndInsertDrawResult();
					}
				}
			}
			catch (Exception ex)
			{
				_common.LogError("BT28Win/updatePendingRecord" + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), "BT28Win");
			}
		}

		public bool UpdJobTime(string startTime, string endTime, string sMainID)
		{
			try
			{
				DateTime dtEnd = Convert.ToDateTime(endTime);

				_common.UpdateADMJobDate(P_GamePrefix, dtEnd, "");

				//using (var bingoConn = new BingoConn())
				//{
				//	var xyz = bingoConn.ADMJobDates.Where(s => s.GameProvider == P_GamePrefix).FirstOrDefault();
				//	if (xyz != null)
				//	{
				//		xyz.LastRunTillDate = dtEnd;

				//		//bingoConn.Entry(xyz).State = System.Data.Entity.EntityState.Modified;
				//		bingoConn.SaveChanges();
				//	}
				//}
				return true;
			}
			catch (Exception ex)
			{
				_common.LogError("BT28Win/UpdJobTime" + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), "BT28Win");
				return false;
			}
		}

		public string GetProviderName(int DrawType)
		{
			switch (DrawType)
			{
				case 1:
					return "MAGNUM";
				case 2:
					return "DAMACAI";
				case 3:
					return "TOTO";
				case 4:
					return "SINGAPORE";
				case 5:
					return "SABAH";
				case 6:
					return "SANDAKAN";
				case 7:
					return "SARAWAK";
				case 8:
					return "GD LOTTO";
				default:
					return "";
			}
		}

		public bool IsTodayDrawResultOut()
		{
			try
			{
				BT28WIN.GetDrawResultResponse resp = api.GetDrawResult(Convert.ToDateTime(drawDate));

				if (resp != null && resp.errorCode == 0)
				{
					return true;
				}
				else
				{
					return false;
				}
				//string method = "/result.aspx";
				//string param = string.Format("?apiUser={0}&apiPass={1}&user={2}&pass={3}&drawDate={4}", apiUser, apiPass, user, pass, drawDate);

				//string apiurl = P_ApiUrl + method + param;

				//var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
				//httpWebRequest.ContentType = "text/xml";
				//httpWebRequest.Method = "GET";
				//httpWebRequest.Timeout = 60000;
				//httpWebRequest.ReadWriteTimeout = 60000;

				//var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
				//using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
				//{
				//    var responseText = streamReader.ReadToEnd();

				//    XmlDocument xmlDoc = new XmlDocument();
				//    xmlDoc.LoadXml(responseText);

				//    string rErrorCode = xmlDoc.SelectSingleNode("response/errorCode").InnerText;

				//    if (rErrorCode == "0")
				//        return true;
				//    else
				//        return false;
				//}
			}
			catch (Exception ex)
			{
				_common.LogError("BT28Win/IsTodayDrawResultOut" + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), "BT28Win");
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
					//if (action == "Start")
					//{
					//	DateTime dtSTime = Convert.ToDateTime(query.LastRunTillDate);
					//	sTime = dtSTime.ToString("yyyy-MM-dd HH:mm:ss");
					//}
					//else
					//{
					//	DateTime dtSTime = DateTime.Now - new TimeSpan(0, 0, 3, 0);
					//	sTime = dtSTime.ToString("yyyy-MM-dd HH:mm:ss");
					//}
				}

				return sTime;

			}
			catch (Exception ex)
			{
				_common.LogError("BT28Win/GetJobTime" + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), "BT28Win");
				return "Error:" + ex.Message.ToString();
			}
		}
		#endregion

		// GET: BT28Win
		public void BT28WinJob()
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