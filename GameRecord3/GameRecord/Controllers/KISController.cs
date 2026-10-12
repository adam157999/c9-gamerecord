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
	public class KISController : Controller
	{
		public static readonly object MyLock = new object();
		private string P_GamePrefix = "KISS918";
		private string P_FilePrefix = "KISS918";
		private int P_PageSize = 1000;
		Common _common = new Common();
		private KISS918Connector api = new KISS918Connector(Environment: BaseConnector.GameEnvironment.PROD);


		#region API function 
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
				_common.LogError("KISS918/UpdJobTime" + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), P_FilePrefix);
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
						DateTime dtSTime = Convert.ToDateTime(query.LastRunTillDate?.ToString("yyyy-MM-dd HH:mm")) - new TimeSpan(0, 0, 15, 0, 0);
						sTime = dtSTime.ToString("yyyy-MM-dd HH:mm:ss");
					}
					else
					{
						DateTime dtSTime = Convert.ToDateTime(DateTime.Now.ToString("yyyy-MM-dd HH:mm"));
						sTime = dtSTime.ToString("yyyy-MM-dd HH:mm:ss");
					}
				}

				return sTime;

			}
			catch (Exception ex)
			{
				_common.LogError("KISS918/GetJobTime" + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), P_FilePrefix);
				return "Error:" + ex.Message.ToString();
			}
		}

		public void KISS918Job()
		{
			try
			{
				lock (MyLock)
				{
					//_common.LogError("Job Start - " + DateTime.Now.ToString(), P_FilePrefix);
					bool isOK = GetRunningPlayer();
					//_common.LogError("Job End - " + DateTime.Now.ToString(), P_FilePrefix);
				}
			}
			catch (Exception ex)
			{
				_common.LogError("Start - " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), P_FilePrefix);
			}
		}

		public bool GetRunningPlayer()
		{
			try
			{
				using (BingoConn db = new BingoConn())
				{
					var query = db.KISScoreLogs.Where(s => s.Flag == "R").ToList();

					if (query != null)
					{
						foreach (var x in query)
						{
							DateTime From = Convert.ToDateTime(x.FromDate);
							DateTime To = DateTime.Now;
							if (x.ToDate != null)
								To = Convert.ToDateTime(x.ToDate);

							bool success = GetGameRecord(x.Username, x.UID, From.ToString("yyyy-MM-dd HH:mm:ss"), To.ToString("yyyy-MM-dd HH:mm:ss"));

							if (success == true)
							{
								if (x.ToDate != null && x.Flag == "R")
								{
									x.Flag = "D";
									db.SaveChanges();
								}
							}
						}
					}
				}

				return true;
			}
			catch (Exception ex)
			{
				return false;
			}
		}

		public bool GetGameRecord(string username, string UID, string sStartTime, string sEndTime)
		{
			Guid sMainID = Guid.NewGuid();

			try
			{
				KISS918.GameLogResponse result = api.GameLog(UID, DateTime.Parse(sStartTime), DateTime.Parse(sEndTime), 1, P_PageSize);

				using (BingoConn dbConn = new BingoConn())
				{

					var kISGameMain = new KISGameMain
					{
						Id = sMainID,
						currMoney = api.currencyID,
						msg = result.msg,
						success = result.success,
						startTime = Convert.ToDateTime(sStartTime),
						endTime = Convert.ToDateTime(sEndTime),
						CreatedDate = DateTime.Now
					};
					dbConn.KISGameMains.Add(kISGameMain);

					if (result.results != null)
					{
						foreach (var p in result.results)
						{
							var kISGame = new KISGame
							{
								Id = Guid.NewGuid(),
								fId = kISGameMain.Id,
								username = username,
								UID = UID,
								BeginBalance = p.BeginBalance.ToString(),
								EndBalance = p.EndBalance.ToString(),
								GameID = p.GameID,
								GameName = p.GameName,
								ClassID = p.ClassID,
								tableID = p.TableID,
								LineNum = p.LineNum,
								RoundNo = p.RoundNO,
								CreateTime = Convert.ToDateTime(p.CreateTime),
								Rownum = p.Rownum,
								betID = p.id,
								Win = p.Win.ToString(),
								bet = p.bet.ToString(),
								uuid = username,
								CreateDate = DateTime.Now
							};
							dbConn.KISGames.Add(kISGame);
						}
						dbConn.SaveChanges();

						//If record more than 1 page, need to get the record from Page 2 and above
						decimal dCnt = Convert.ToDecimal(result.total) / Convert.ToDecimal(P_PageSize);
						int iPageIndex = Int32.Parse(Math.Ceiling(dCnt).ToString());

						if (iPageIndex > 1)
						{
							for (int x = 2; x <= iPageIndex; x++)
							{
								bool IsOK = GetGameRecordByPage(x, username, UID, sStartTime, sEndTime, sMainID);
							}
						}
					}

					bool isUpdJob = UpdJobTime(sStartTime, sEndTime, sMainID.ToString());
				}

				return true;
			}
			catch (Exception ex)
			{
				_common.LogError("KISS918/GetGameRecord" + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), P_FilePrefix);
				return false;
			}
		}

		public bool GetGameRecordByPage(int iPage, string username, string UID, string sStartTime, string sEndTime, Guid sMainID)
		{
			try
			{
				KISS918.GameLogResponse result = api.GameLog(UID, DateTime.Parse(sStartTime), DateTime.Parse(sEndTime), iPage, P_PageSize);

				using (BingoConn dbConn = new BingoConn())
				{

					foreach (var p in result.results)
					{
						var kISGame = new KISGame
						{
							Id = Guid.NewGuid(),
							fId = sMainID,
							username = username,
							UID = UID,
							BeginBalance = p.BeginBalance.ToString(),
							EndBalance = p.EndBalance.ToString(),
							GameID = p.GameID,
							GameName = p.GameName,
							ClassID = p.ClassID,
							tableID = p.TableID,
							LineNum = p.LineNum,
							RoundNo = p.RoundNO,
							CreateTime = Convert.ToDateTime(p.CreateTime),
							Rownum = p.Rownum,
							betID = p.id,
							Win = p.Win.ToString(),
							bet = p.bet.ToString(),
							uuid = username,
							CreateDate = DateTime.Now
						};
						dbConn.KISGames.Add(kISGame);

					}
					dbConn.SaveChanges();
				}

				return true;
			}
			catch (Exception ex)
			{
				_common.LogError("KISS918/GetGameecordByPage" + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), P_FilePrefix);
				return false;
			}
		}
		#endregion

	}
}