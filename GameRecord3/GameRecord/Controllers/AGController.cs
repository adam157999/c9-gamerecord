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
using System.Data.Entity.Migrations;

namespace GameRecord.Controllers
{
	public class AGController : Controller
	{
		public static readonly object MyLock = new object();
		private string P_GamePrefix = "AG";
		private string P_FtpPlatformFolderName = "AGIN/";
		private string P_FtpUrl = "ftp://xh.gdcapi.com/";
		private string P_FtpUserName = "HN7.ManCity";
		private string P_FtpPassword = "jLpJkhFmHM";
		private string P_PendingFolder = System.Web.HttpContext.Current.Server.MapPath(@"\AGPendingFolder\");
		private string P_ProcessedFolder = System.Web.HttpContext.Current.Server.MapPath(@"\AGProcessedFolder\");
		private string P_NamePrefix = "AGC";
		//private string P_PendingFolder = "C:\\inetpub\\wwwroot\\GameRecord Publish\\AGPendingFolder\\";
		//private string P_ProcessedFolder = "C:\\inetpub\\wwwroot\\GameRecord Publish\\AGProcessedFolder\\";
		int totalDownload = 0;
		int totalDuplicate = 0;
		Common _common = new Common();
		private AGConnector api = new AGConnector(Environment: BaseConnector.GameEnvironment.PROD);

		//###################################################################################
		#region Main Function

		public bool DownloadXmlFiles(string fileDate, string sMainID)
		{
			string fileName = string.Empty;
			string fileDateTime = string.Empty;
			int fileSize = 0;

			try
			{
				AsiaGaming.FtpListDirectoryResponse resp = api.FtpListDirectory(fileDate);

				foreach (var entry in resp.entries)
				{
					string[] splits = entry.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
					fileName = splits[8];
					fileDateTime = splits[6] + splits[5].ToUpper() + splits[7];
					fileSize = Convert.ToInt32(splits[4]);

					if (fileName.Substring(fileName.Length - 4).ToUpper() == ".XML")
					{
						using (BingoConn db = new BingoConn())
						{
							if (db.AGDownloadedFiles.Where(x => x.FileName == fileName && x.FileSize == fileSize && x.FileDateTime == fileDateTime).FirstOrDefault() == null)
							{
								AsiaGaming.FtpDownloadResponse respFile = api.FtpDownload(fileDate, fileName);

								if (!Directory.Exists(P_PendingFolder + fileDate))
									Directory.CreateDirectory(P_PendingFolder + fileDate);


								using (System.IO.StreamWriter file = new System.IO.StreamWriter(P_PendingFolder + fileDate + @"\" + fileName))
								{
									file.Write(respFile.fileContent);
								};

								AGDownloadedFile agDownloadFile = new AGDownloadedFile()
								{
									Id = Guid.NewGuid().ToString(),
									fId = sMainID,
									SourceFolder = fileDate,
									FileName = fileName,
									FileDateTime = fileDateTime,
									FileSize = fileSize,
									DownloadDt = DateTime.Now,
									IsProcessed = false
								};
								db.AGDownloadedFiles.Add(agDownloadFile);
								db.SaveChanges();
								totalDownload += 1;
							}
							else
							{
								totalDuplicate += 1;
							}
						}
					}
				}


				//string ftpDateFolderName = fileDate;

				//List<string> entries = new List<string>();
				//var ftpFolderRequest = (FtpWebRequest)WebRequest.Create(P_FtpUrl + P_FtpPlatformFolderName + ftpDateFolderName);
				//ftpFolderRequest.Method = WebRequestMethods.Ftp.ListDirectoryDetails;
				//ftpFolderRequest.Credentials = new NetworkCredential(P_FtpUserName, P_FtpPassword);
				//ftpFolderRequest.UsePassive = true;
				//ftpFolderRequest.UseBinary = true;
				//ftpFolderRequest.EnableSsl = false;

				//var ftpFolderResponse = (FtpWebResponse)ftpFolderRequest.GetResponse();
				//using (var folderReader = new StreamReader(ftpFolderResponse.GetResponseStream()))
				//{
				//	entries = folderReader.ReadToEnd().Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries).ToList();
				//	foreach (var entry in entries)
				//	{
				//		string[] splits = entry.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
				//		fileName = splits[8];
				//		fileDateTime = splits[6] + splits[5].ToUpper() + splits[7];
				//		fileSize = Convert.ToInt32(splits[4]);

				//		if (fileName.Substring(fileName.Length - 4).ToUpper() == ".XML")
				//		{
				//			using (BingoConn db = new BingoConn())
				//			{
				//				if (db.AGDownloadedFiles.Where(x => x.FileName == fileName && x.FileSize == fileSize && x.FileDateTime == fileDateTime).FirstOrDefault() == null)
				//				{
				//					var ftpFileRequest = (FtpWebRequest)WebRequest.Create(P_FtpUrl + P_FtpPlatformFolderName + ftpDateFolderName + @"\" + fileName);
				//					ftpFileRequest.Method = WebRequestMethods.Ftp.DownloadFile;
				//					ftpFileRequest.Credentials = new NetworkCredential(P_FtpUserName, P_FtpPassword);
				//					ftpFileRequest.UsePassive = true;
				//					ftpFileRequest.UseBinary = true;
				//					ftpFileRequest.EnableSsl = false;

				//					using (Stream fileStream = ftpFileRequest.GetResponse().GetResponseStream())
				//					{
				//						if (!Directory.Exists(P_PendingFolder + ftpDateFolderName))
				//							Directory.CreateDirectory(P_PendingFolder + ftpDateFolderName);

				//						using (Stream fs = System.IO.File.Create(P_PendingFolder + ftpDateFolderName + @"\" + fileName))
				//						{
				//							fileStream.CopyTo(fs);
				//						}
				//					}

				//					AGDownloadedFile agDownloadFile = new AGDownloadedFile()
				//					{
				//						Id = Guid.NewGuid().ToString(),
				//						fId = sMainID,
				//						SourceFolder = fileDate,
				//						FileName = fileName,
				//						FileDateTime = fileDateTime,
				//						FileSize = fileSize,
				//						DownloadDt = DateTime.Now,
				//						IsProcessed = false
				//					};
				//					db.AGDownloadedFiles.Add(agDownloadFile);
				//					db.SaveChanges();
				//					totalDownload += 1;
				//				}
				//				else
				//				{
				//					totalDuplicate += 1;
				//				}
				//			}
				//		}
				//	}
				//}

				return true;
			}
			catch (Exception ex)
			{
				_common.LogError("AG/DownloadXmlFiles" + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), "AG");
				return false;
			}
		}

		public bool UpsertBetRecord(string sMainID)
		{
			try
			{
				XmlDocument doc = new XmlDocument();
				string filePath = "";

				using (BingoConn db = new BingoConn())
				{
					var query = db.AGDownloadedFiles.Where(s => s.IsProcessed == false).OrderBy(s => s.FileName).ToList();

					if (query != null)
					{
						try
						{
							foreach (var q in query)
							{
								filePath = P_PendingFolder + q.SourceFolder + @"\" + q.FileName;
								if (System.IO.File.Exists(filePath))
								{
									if (Path.GetExtension(filePath).ToString().ToUpper() == ".XML")
									{
										using (var objReader = new StreamReader(filePath))
										{
											while (objReader.Peek() > -1)
											{
												doc.LoadXml(objReader.ReadLine());

												if (doc.ChildNodes[0].Attributes["playerName"].InnerText.ToUpper().Trim().Contains(P_NamePrefix))
												{
													if (doc.ChildNodes[0].Attributes["dataType"].InnerText.ToUpper() == "BR" && Convert.ToInt32(doc.ChildNodes[0].Attributes["flag"].InnerText) != -8)
													{
														AGGame agGame = new AGGame()
														{
															billNo = doc.ChildNodes[0].Attributes["billNo"] != null ? doc.ChildNodes[0].Attributes["billNo"].InnerText : null,
															playerName = doc.ChildNodes[0].Attributes["playerName"] != null ? doc.ChildNodes[0].Attributes["playerName"].InnerText : null,
															agentCode = doc.ChildNodes[0].Attributes["agentCode"] != null ? doc.ChildNodes[0].Attributes["agentCode"].InnerText : null,
															gameCode = doc.ChildNodes[0].Attributes["gameCode"] != null ? doc.ChildNodes[0].Attributes["gameCode"].InnerText : null,
															netAmount = doc.ChildNodes[0].Attributes["netAmount"] != null ? Convert.ToDecimal(doc.ChildNodes[0].Attributes["netAmount"].InnerText) : (decimal?)null,
															betTime = doc.ChildNodes[0].Attributes["betTime"] != null ? Convert.ToDateTime(doc.ChildNodes[0].Attributes["betTime"].InnerText) : (DateTime?)null,
															gameType = doc.ChildNodes[0].Attributes["gameType"] != null ? doc.ChildNodes[0].Attributes["gameType"].InnerText : null,
															betAmount = doc.ChildNodes[0].Attributes["betAmount"] != null ? Convert.ToDecimal(doc.ChildNodes[0].Attributes["betAmount"].InnerText) : (decimal?)null,
															validBetAmount = doc.ChildNodes[0].Attributes["validBetAmount"] != null ? Convert.ToDecimal(doc.ChildNodes[0].Attributes["validBetAmount"].InnerText) : (decimal?)null,
															flag = doc.ChildNodes[0].Attributes["flag"] != null ? Convert.ToInt32(doc.ChildNodes[0].Attributes["flag"].InnerText) : (int?)null,
															playType = doc.ChildNodes[0].Attributes["playType"] != null ? doc.ChildNodes[0].Attributes["playType"].InnerText : null,
															currency = doc.ChildNodes[0].Attributes["currency"] != null ? doc.ChildNodes[0].Attributes["currency"].InnerText : null,
															tableCode = doc.ChildNodes[0].Attributes["tableCode"] != null ? doc.ChildNodes[0].Attributes["tableCode"].InnerText : null,
															loginIP = doc.ChildNodes[0].Attributes["loginIP"] != null ? doc.ChildNodes[0].Attributes["loginIP"].InnerText : null,
															recalcuTime = doc.ChildNodes[0].Attributes["recalcuTime"] != null ? Convert.ToDateTime(doc.ChildNodes[0].Attributes["recalcuTime"].InnerText) : (DateTime?)null,
															platformId = doc.ChildNodes[0].Attributes["platformId"] != null ? doc.ChildNodes[0].Attributes["platformId"].InnerText : null,
															platformType = doc.ChildNodes[0].Attributes["platformType"] != null ? doc.ChildNodes[0].Attributes["platformType"].InnerText : null,
															stringex = doc.ChildNodes[0].Attributes["stringex"] != null ? doc.ChildNodes[0].Attributes["stringex"].InnerText : null,
															remark = doc.ChildNodes[0].Attributes["remark"] != null ? doc.ChildNodes[0].Attributes["remark"].InnerText : null,
															round = doc.ChildNodes[0].Attributes["round"] != null ? doc.ChildNodes[0].Attributes["round"].InnerText : null,
															result = doc.ChildNodes[0].Attributes["result"] != null ? doc.ChildNodes[0].Attributes["result"].InnerText : null,
															beforeCredit = doc.ChildNodes[0].Attributes["beforeCredit"] != null ? Convert.ToDecimal(doc.ChildNodes[0].Attributes["beforeCredit"].InnerText) : (decimal?)null,
															deviceType = doc.ChildNodes[0].Attributes["deviceType"] != null ? Convert.ToInt32(doc.ChildNodes[0].Attributes["deviceType"].InnerText) : (int?)null,
															betAmountBonus = doc.ChildNodes[0].Attributes["betAmountBonus"] != null ? Convert.ToDecimal(doc.ChildNodes[0].Attributes["betAmountBonus"].InnerText) : (decimal?)null,
															netAmountBonus = doc.ChildNodes[0].Attributes["netAmountBonus"] != null ? Convert.ToDecimal(doc.ChildNodes[0].Attributes["netAmountBonus"].InnerText) : (decimal?)null,
															gameCategory = doc.ChildNodes[0].Attributes["gameCategory"] != null ? doc.ChildNodes[0].Attributes["gameCategory"].InnerText : null,
														};
														var bn = doc.ChildNodes[0].Attributes["billNo"].InnerText;
														var record = db.AGGames.Where(s => s.billNo == bn).FirstOrDefault();

														if (record == null)
														{
															agGame.fId = sMainID;
															agGame.CreatedDate = DateTime.Now;
														}
														else
														{
															agGame.fId = sMainID;
															agGame.CreatedDate = record.CreatedDate;
															agGame.ModifiedDate = DateTime.Now;
														}
														db.AGGames.AddOrUpdate(agGame);
														db.SaveChanges();
													}
												}

											}
										}

										MoveFile(q.SourceFolder, Path.GetFileName(filePath), q.Id);
									}
								}
							}
						}
						catch (Exception ex)
						{
							_common.LogError("AG/UpsertBetRecord/FileFailed" + " " + DateTime.Now.ToString() + "  " + filePath + " " + ex.Message.ToString(), "AG");
						}
					}
				}
				return true;
			}
			catch (Exception ex)
			{
				_common.LogError("AG/UpsertBetRecord" + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), "AG");
				return false;
			}
		}

		public bool MoveFile(string fileDate, string fileName, string Id)
		{
			try
			{
				using (BingoConn db = new BingoConn())
				{
					if (System.IO.File.Exists(P_PendingFolder + fileDate + @"\" + fileName))
					{
						if (Path.GetExtension(P_PendingFolder + fileDate + @"\" + fileName).ToString().ToUpper() == ".XML")
						{
							if (!Directory.Exists(P_ProcessedFolder + fileDate))
								Directory.CreateDirectory(P_ProcessedFolder + fileDate);

							if (System.IO.File.Exists(P_ProcessedFolder + fileDate + @"\" + fileName))
								System.IO.File.Delete(P_ProcessedFolder + fileDate + @"\" + fileName);

							System.IO.File.Move(P_PendingFolder + fileDate + @"\" + fileName, P_ProcessedFolder + fileDate + @"\" + fileName);

							//System.IO.File.Delete(P_PendingFolder + fileDate + @"\" + fileName);

							db.AGDownloadedFiles.Where(s => s.Id == Id).ToList().ForEach(q =>
							{
								q.IsProcessed = true;
							});

							db.SaveChanges();
						}
					}

					return true;
				}
			}
			catch (Exception ex)
			{
				_common.LogError("AG/MoveFile" + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), "AG");
				return false;
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
				_common.LogError("AG/UpdJobTime" + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), "AG");
				return false;
			}
		}

		public bool GetGameRecord()
		{
			string startTime = GetJobTime("Start");
			string endTime = GetJobTime("End");
			//string startTime = "2023-03-23 09:00:00.000";
			// string endTime = "2023-03-23 13:30:00.000";
			string sMainID = Guid.NewGuid().ToString();

			try
			{
				using (BingoConn db = new BingoConn())
				{
					AGGameMain agGameMain = new AGGameMain()
					{
						Id = sMainID,
						TotalDownload = null,
						TotalDuplicate = null,
						StartTime = Convert.ToDateTime(startTime),
						EndTime = Convert.ToDateTime(endTime),
						CreatedDate = DateTime.Now
					};
					db.AGGameMains.Add(agGameMain);
					db.SaveChanges();

					string startFileDate = Convert.ToDateTime(startTime).ToString("yyyyMMdd");
					string endFileDate = Convert.ToDateTime(endTime).ToString("yyyyMMdd");

					if (DownloadXmlFiles(endFileDate, sMainID))
					{
						UpsertBetRecord(sMainID);
					}

					//Give 1 hour bufer to download previous day data
					int DayDiff = (Convert.ToDateTime(endTime).Date - Convert.ToDateTime(startTime).AddHours(-1).Date).Days;
					if (DayDiff > 0)
					{
						if (DownloadXmlFiles(startFileDate, sMainID))
						{
							UpsertBetRecord(sMainID);
						}
					}

					if (DownloadXmlFiles("lostAndfound", sMainID))
					{
						UpsertBetRecord(sMainID);
					}

					agGameMain.TotalDownload = totalDownload;
					agGameMain.TotalDuplicate = totalDuplicate;
					db.AGGameMains.AddOrUpdate(agGameMain);
					db.SaveChanges();

					bool isUpdJob = UpdJobTime(startTime, endTime, sMainID);
				}

				return true;
			}
			catch (Exception ex)
			{
				_common.LogError("AG/GetGameRecord" + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), "AG");
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
						//sTime = dtSTime.ToString("yyyyMMdd");
						sTime = dtSTime.ToString("yyyy-MM-dd HH:mm:ss");
					}
					else
					{
						DateTime dtSTime = Convert.ToDateTime(DateTime.Now.ToString("yyyy-MM-dd HH:mm")) - new TimeSpan(0, 12, 3, 0, 0);
						//sTime = dtSTime.ToString("yyyyMMdd");
						sTime = dtSTime.ToString("yyyy-MM-dd HH:mm:ss");
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

		#endregion

		// GET: AG
		public void AGJob()
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
				_common.LogError("Start - " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), "AG");
			}
		}
	}
}