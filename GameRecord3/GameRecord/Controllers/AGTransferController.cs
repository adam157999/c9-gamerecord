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
	public class AGTransferController : Controller
	{
		public static readonly object MyLock = new object();
		private string P_GamePrefix = "AGTransfer";
		//private string P_FtpPlatformFolderName = "AGIN/";
		//private string P_FtpUrl = "ftp://xh.gdcapi.com/";
		//private string P_FtpUserName = "HN7.ManCity";
		//private string P_FtpPassword = "jLpJkhFmHM";
		//private string P_PendingFolder = System.Web.HttpContext.Current.Server.MapPath(@"\AGPendingFolder\");
		//private string P_ProcessedFolder = System.Web.HttpContext.Current.Server.MapPath(@"\AGProcessedFolder\");
		private string P_PendingFolder = "C:\\inetpub\\wwwroot\\GameRecord Publish\\AGPendingFolder\\";
		//private string P_ProcessedFolder = "C:\\inetpub\\wwwroot\\GameRecord Publish\\AGProcessedFolder\\";
		int totalDownload = 0;
		int totalDuplicate = 0;
		Common _common = new Common();

		//###################################################################################
		#region Main Function

		public bool UpsertBetRecord(string sMainID)
		{
			try
			{
				XmlDocument doc = new XmlDocument();
				string filePath = "";

				using (BingoConn db = new BingoConn())
				{
					var query = db.AGDownloadedFilesTransfers.Where(s => s.IsProcessed == false).OrderBy(s => s.FileName).ToList();

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

												if (doc.ChildNodes[0].Attributes["dataType"].InnerText.ToUpper() == "TR")
												{
													AGTransfer agGame = new AGTransfer()
													{
														ID = doc.ChildNodes[0].Attributes["ID"] != null ? doc.ChildNodes[0].Attributes["ID"].InnerText : null,
														agentCode = doc.ChildNodes[0].Attributes["agentCode"] != null ? doc.ChildNodes[0].Attributes["agentCode"].InnerText : null,
														transferId = doc.ChildNodes[0].Attributes["transferId"] != null ? doc.ChildNodes[0].Attributes["transferId"].InnerText : null,
														tradeNo = doc.ChildNodes[0].Attributes["tradeNo"] != null ? doc.ChildNodes[0].Attributes["tradeNo"].InnerText : null,
														platformType = doc.ChildNodes[0].Attributes["platformType"] != null ? doc.ChildNodes[0].Attributes["platformType"].InnerText : null,
														playerName = doc.ChildNodes[0].Attributes["playerName"] != null ? doc.ChildNodes[0].Attributes["playerName"].InnerText : null,
														transferType = doc.ChildNodes[0].Attributes["transferType"] != null ? doc.ChildNodes[0].Attributes["transferType"].InnerText : null,
														transferAmount = doc.ChildNodes[0].Attributes["transferAmount"] != null ? Convert.ToDecimal(doc.ChildNodes[0].Attributes["transferAmount"].InnerText) : (decimal?)null,
														previousAmount = doc.ChildNodes[0].Attributes["previousAmount"] != null ? Convert.ToDecimal(doc.ChildNodes[0].Attributes["previousAmount"].InnerText) : (decimal?)null,
														currentAmount = doc.ChildNodes[0].Attributes["currentAmount"] != null ? Convert.ToDecimal(doc.ChildNodes[0].Attributes["currentAmount"].InnerText) : (decimal?)null,
														currency = doc.ChildNodes[0].Attributes["currency"] != null ? doc.ChildNodes[0].Attributes["currency"].InnerText : null,
														exchangeRate = doc.ChildNodes[0].Attributes["exchangeRate"] != null ? Convert.ToDecimal(doc.ChildNodes[0].Attributes["exchangeRate"].InnerText) : (decimal?)null,
														IP = doc.ChildNodes[0].Attributes["IP"] != null ? doc.ChildNodes[0].Attributes["IP"].InnerText : null,
														flag = doc.ChildNodes[0].Attributes["flag"] != null ? Convert.ToInt32(doc.ChildNodes[0].Attributes["flag"].InnerText) : (int?)null,
														creationTime = doc.ChildNodes[0].Attributes["creationTime"] != null ? Convert.ToDateTime(doc.ChildNodes[0].Attributes["creationTime"].InnerText) : (DateTime?)null,
														gameCode = doc.ChildNodes[0].Attributes["gameCode"] != null ? doc.ChildNodes[0].Attributes["gameCode"].InnerText : null,

													};
													var bn = doc.ChildNodes[0].Attributes["tradeNo"].InnerText;
													var record = db.AGTransfers.Where(s => s.tradeNo == bn).FirstOrDefault();

													if (record == null)
													{
														agGame.CreatedDate = DateTime.Now;
													}
													else
													{
														agGame.CreatedDate = record.CreatedDate;
														agGame.ModifiedDate = DateTime.Now;
													}

													db.AGTransfers.AddOrUpdate(agGame);
													db.SaveChanges();
												}
											}
										}

										string Id = Convert.ToString(q.Id);

										db.AGDownloadedFilesTransfers.Where(s => s.Id == Id).ToList().ForEach(x =>
										{
											x.IsProcessed = true;
										});

										db.SaveChanges();

										//MoveFile(q.SourceFolder, Path.GetFileName(filePath), q.Id);
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

		public bool GetGameRecord()
		{
			//string startTime = GetJobTime("Start");
			//string endTime = GetJobTime("End");
			//string startTime = "2020-09-18 00:00:00";
			//string endTime = "2020-09-18 23:59:59";
			string sMainID = Guid.NewGuid().ToString();

			try
			{
				using (BingoConn db = new BingoConn())
				{
					UpsertBetRecord(sMainID);

					//bool isUpdJob = UpdJobTime(startTime, endTime, sMainID);
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
						DateTime dtSTime = Convert.ToDateTime(query.JobStart?.ToString("yyyy-MM-dd HH:mm"));
						//sTime = dtSTime.ToString("yyyyMMdd");
						sTime = dtSTime.ToString("yyyy-MM-dd HH:mm:ss");
					}
					else
					{
						DateTime dtSTime = Convert.ToDateTime(query.JobEnd?.ToString("yyyy-MM-dd HH:mm"));
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