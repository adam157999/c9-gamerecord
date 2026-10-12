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
using Newtonsoft.Json.Linq;
using System.Data.Entity.Migrations;
using System.Data.Entity.Validation;
using System.Globalization;
using static GameRecord.Controllers.BaseConnector;

namespace GameRecord.Controllers
{
	public class LKL365OldController : Controller
	{
		public static readonly object MyLock = new object();
		private string P_GamePrefix = "";

		private string P_FilePrefix = "";
		private string P_DTFormat = "yyyy-MM-ddTHH:mm:ss.fffZ";
		Common _common = new Common();
		private LKL365Connector api = null;

		//public LKL365Controller(GameEnvironment Environment = GameEnvironment.PROD)
		//{
		//    api = new LKL365Connector(P_GamePrefix, Environment: Environment);
		//}

		//public LKL365Controller()
		//{
		//    api = new LKL365Connector(P_GamePrefix, Environment: GameEnvironment.STAG);
		//}

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
				_common.LogError("LKL365/UpdJobTime" + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), P_FilePrefix);
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
						DateTime dtSTime = Convert.ToDateTime(DateTime.Now.ToString("yyyy-MM-dd HH:mm")) - new TimeSpan(0, 8, 0, 0, 0);
						sTime = dtSTime.ToString("yyyy-MM-dd HH:mm:ss");
					}
				}

				return sTime;

			}
			catch (Exception ex)
			{
				_common.LogError("LKL365/GetJobTime" + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), P_FilePrefix);
				return "Error:" + ex.Message.ToString();
			}
		}

		public void LKL365Job(string gameId)
		{
			try
			{
				lock (MyLock)
				{
					P_GamePrefix = gameId.ToUpper();
					P_FilePrefix = gameId.ToUpper();
					api = new LKL365Connector(P_GamePrefix, Environment: GameEnvironment.STAG);
					bool isOK = GetGameRecord();
				}
			}
			catch (Exception ex)
			{
				_common.LogError("Start - " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), P_FilePrefix);
			}
		}

		public int GetGameRecordCore(string startTime, string endTime, string sMainID, int pageSize = 0, int pageIndex = 1)
		{
			int count = 0;
			int currentPageIndex = 0;
			LK_L365.Model.GetGameRecordByTimeResponseObject result = api.GetBrandCompletedGameResultsV2(startTime, endTime, pageIndex);
			using (BingoConn db = new BingoConn())
			{
				LKL365GameMain lkl365Main = new LKL365GameMain()
				{
					Id = sMainID,
					Total = result.TotalRows,
					PageIndex = result.CurrentPage,
					PageSize = pageSize,
					StartTime = Convert.ToDateTime(startTime),
					EndTime = Convert.ToDateTime(endTime),
					CreatedDate = DateTime.Now
				};
				db.LKL365GameMain.Add(lkl365Main);
				db.SaveChanges();

				if (result.Item.Count > 0)
				{
					foreach (var b in result.Item)
					{
						count++;
						string id = count.ToString();
						try
						{
							LKL365Game lkl365Game = new LKL365Game()
							{
								fId = sMainID,
								LoginId = b.LoginId,
								TotalBet = b.TotalBet,
								TotalWin = b.TotalWin,
								GameName = b.GameName,
								BetDetail = b.BetDetail,
								WinDetail = b.WinDetail,
								OrderId = b.OrderId,
								ActionDate = b.ActionDate,
								CreationDate = b.CreationDate,
								ValidCommission = b.ValidCommission,
								ValidBet = b.ValidBet,
								ValidWin = b.ValidWin,
								MJPComm = b.MJPComm,
								MJPWin = b.MJPWin
							};
							db.LKL365Game.AddOrUpdate(lkl365Game);
						}
						catch (DbEntityValidationException e)
						{
							foreach (var eve in e.EntityValidationErrors)
							{
								_common.LogError("LKL365/GetGameRecord" + " " + DateTime.Now.ToString() + " " + string.Format("Entity of type \"{0}\" in state \"{1}\" has the following validation errors:",
																eve.Entry.Entity.GetType().Name, eve.Entry.State), P_FilePrefix);
								foreach (var ve in eve.ValidationErrors)
								{
									_common.LogError("LKL365/GetGameRecord" + " " + DateTime.Now.ToString() + " " + string.Format("- Property: \"{0}\", Error: \"{1}\"",
																	ve.PropertyName, ve.ErrorMessage), P_FilePrefix);
								}
							}
							throw;
						}
						catch (WebException ex)
						{
							var responseText = new StreamReader(ex.Response.GetResponseStream()).ReadToEnd();
							_common.LogError("LKL365/GetGameRecord" + " " + DateTime.Now.ToString() + " " + ex.Message.ToString() + " " + responseText, P_FilePrefix);
							throw;
						}
						catch (Exception ex)
						{
							_common.LogError("LKL365/GetGameRecord" + " " + DateTime.Now.ToString() + " " + ex.Message.ToString(), P_FilePrefix);
							throw;
						}
					}
					db.SaveChanges();
					if (result.CurrentPage != result.TotalPage)
					{
						currentPageIndex = result.CurrentPage + 1;
					}

					else
					{
						currentPageIndex = 0;
					}
				}
				return currentPageIndex;
			}
		}


		public bool GetGameRecord()
		{
			string startTime = GetJobTime("Start");
			string endTime = GetJobTime("End");
			string mStartTime = startTime;
			string mEndTime = endTime;
			int limit = 120; // 24 hours
			int pageSize = 2000;
			int pageIndex = 1;
			bool successFlag = false;
			string sMainID = Guid.NewGuid().ToString();
			//int MinDiff = Convert.ToInt32((Convert.ToDateTime(endTime) - Convert.ToDateTime(startTime)).TotalMinutes);
			int times = 1;
			for (int j = limit; j <= times * limit; j += limit)
			{
				mStartTime = j == limit ? startTime : mEndTime;

				mEndTime = Convert.ToString(Convert.ToDateTime(mStartTime).AddMinutes(limit).ToString("yyyy-MM-dd HH:mm:ss"));
				int reportCurrentPage = GetGameRecordCore(startTime, mEndTime, sMainID, pageSize, pageIndex);
				while (reportCurrentPage > 0)
				{
					sMainID = Guid.NewGuid().ToString();
					reportCurrentPage = GetGameRecordCore(startTime, mEndTime, sMainID, pageSize, reportCurrentPage);
					bool isUpdJob = UpdJobTime(startTime, endTime, sMainID);
				}
				successFlag = true;
			}
			//}


			return successFlag;
		}
	}
}