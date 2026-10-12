using System;
using System.Linq;
using System.Web.Mvc;
using System.Net;
using System.IO;
using GameRecord.Models;
using System.Data.Entity.Migrations;
using System.Data.Entity.Validation;
using static GameRecord.Controllers.BaseConnector;
using System.Collections.Generic;

namespace GameRecord.Controllers
{
	public class DGController : Controller
	{
		public static readonly object MyLock = new object();
		private string P_GamePrefix = "DG";

		private string P_FilePrefix = "DG";
		private string P_DTFormat = "yyyy-MM-ddTHH:mm:ss.fffZ";
		Common _common = new Common();
		private DGConnector api = new DGConnector(Environment: BaseConnector.GameEnvironment.PROD);

		public DGController(GameEnvironment Environment = GameEnvironment.PROD)
		{
			api = new DGConnector(Environment: Environment);
		}

		public DGController()
		{
			api = new DGConnector(Environment: GameEnvironment.PROD);
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
				_common.LogError("DGGame/UpdJobTime" + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), P_FilePrefix);
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
				_common.LogError("DGGame/GetJobTime" + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), P_FilePrefix);
				return "Error:" + ex.Message.ToString();
			}
		}

		public void DGJob()
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
				_common.LogError("Start - " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), P_FilePrefix);
			}
		}

		public bool GetGameRecord()
		{
			string startTime = GetJobTime("Start");
			string endTime = GetJobTime("End");
			string sMainID = Guid.NewGuid().ToString();
			string mStartTime = startTime;
			string mEndTime = endTime;
			List<long> mfList = new List<long>();

			try
			{
				DG_DreamGaming.Model.GetReportResponse result = api.GetReport();

				using (BingoConn db = new BingoConn())
				{
					DGGameMain dgMain = new DGGameMain()
					{
						Id = sMainID,
						Total = null,
						StartTime = Convert.ToDateTime(startTime),
						EndTime = Convert.ToDateTime(endTime),
						CreatedDate = DateTime.Now
					};
					db.DGGameMains.Add(dgMain);
					db.SaveChanges();

					if (result.codeId == 0)
					{
						if (result.list != null)
						{
							if (result.list.Count > 0)
							{
								foreach (var b in result.list)
								{
									try
									{
										DGGame dgGame = new DGGame()
										{
											fId = sMainID,
											id = b.id,
											lobbyId = b.lobbyId,
											shoeId = b.shoeId,
											playId = b.playId,
											gameType = b.gameType,
											gameId = b.gameId,
											memberId = b.memberId,
											betTime = Convert.ToDateTime(b.betTime) == DateTime.MinValue ? (DateTime?)null : Convert.ToDateTime(b.betTime),
											calTime = Convert.ToDateTime(b.calTime) == DateTime.MinValue ? (DateTime?)null : Convert.ToDateTime(b.calTime),
											winOrLoss = b.winOrLoss,
											winOrLossz = b.winOrLossz,
											betPoints = b.betPoints,
											betPointsz = b.betPointsz,
											availableBet = b.availableBet,
											userName = b.userName,
											result = b.result,
											betDetail = b.betDetail,
											betDetailz = b.betDetailz,
											ip = b.ip,
											ext = b.ext,
											isRevocation = b.isRevocation,
											balanceBefore = b.balanceBefore,
											parentBetId = b.parentBetId,
											currencyId = b.currencyId,
											deviceType = b.deviceType,
											pluginid = b.pluginid,
											CreatedDate = DateTime.Now
										};
										db.DGGames.AddOrUpdate(dgGame);
										db.SaveChanges();

										mfList.Add(b.id);
									}
									catch (DbEntityValidationException e)
									{
										foreach (var eve in e.EntityValidationErrors)
										{
											_common.LogError("DGGame/GetGameRecord" + " " + DateTime.Now.ToString() + " " + string.Format("Entity of type \"{0}\" in state \"{1}\" has the following validation errors:",
															eve.Entry.Entity.GetType().Name, eve.Entry.State), P_FilePrefix);
											foreach (var ve in eve.ValidationErrors)
											{
												_common.LogError("DGGame/GetGameRecord" + " " + DateTime.Now.ToString() + " " + string.Format("- Property: \"{0}\", Error: \"{1}\"",
																ve.PropertyName, ve.ErrorMessage), P_FilePrefix);
											}
										}
									}
									catch (WebException ex)
									{
										var error = new StreamReader(ex.Response.GetResponseStream()).ReadToEnd();
										_common.LogError("Record " + b.id + " " + DateTime.Now.ToString() + " " + ex.Message.ToString() + " " + error, P_FilePrefix);
									}
									catch (Exception ex)
									{
										_common.LogError("Record " + b.id + " " + DateTime.Now.ToString() + " " + ex.Message.ToString(), P_FilePrefix);
									}
								}
								//db.SaveChanges();
							}

							if (mfList.Count > 0)
							{
								DG_DreamGaming.Model.MarkReportResponse mfResult = api.MarkReport(mfList);
							}
						}

						bool isUpdJob = UpdJobTime(startTime, endTime, sMainID);
					}

					//bool isUpdJob = UpdJobTime(startTime, endTime, sMainID);
				}


				return true;
			}
			catch (DbEntityValidationException e)
			{
				foreach (var eve in e.EntityValidationErrors)
				{
					_common.LogError("DGGame/GetGameRecord" + " " + DateTime.Now.ToString() + " " + string.Format("Entity of type \"{0}\" in state \"{1}\" has the following validation errors:",
									eve.Entry.Entity.GetType().Name, eve.Entry.State), P_FilePrefix);
					foreach (var ve in eve.ValidationErrors)
					{
						_common.LogError("DGGame/GetGameRecord" + " " + DateTime.Now.ToString() + " " + string.Format("- Property: \"{0}\", Error: \"{1}\"",
										ve.PropertyName, ve.ErrorMessage), P_FilePrefix);
					}
				}
				return false;
			}
			catch (WebException ex)
			{
				var responseText = new StreamReader(ex.Response.GetResponseStream()).ReadToEnd();
				_common.LogError("DGGame/GetGameRecord" + " " + DateTime.Now.ToString() + " " + ex.Message.ToString() + " " + responseText, P_FilePrefix);
				return false;
			}
			catch (Exception ex)
			{
				_common.LogError("DGGame/GetGameRecord" + " " + DateTime.Now.ToString() + " " + ex.Message.ToString(), P_FilePrefix);
				return false;
			}
		}
	}
}