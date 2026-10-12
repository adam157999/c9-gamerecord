using System;
using System.Linq;
using System.Web.Mvc;
using System.Net;
using System.IO;
using GameRecord.Models;
using System.Data.Entity.Migrations;
using System.Data.Entity.Validation;
using GameRecord.Controllers.V2;
using System.Collections.Generic;

namespace GameRecord.Controllers
{
	public class KING855Controller : Controller
	{
		public static readonly object MyLock = new object();
		private string P_GamePrefix = "KING855";

		private string P_FilePrefix = "KING855";
		private string P_DTFormat = "yyyy-MM-ddTHH:mm:ss.fffZ";
		Common _common = new Common();
		private KING855Connector api = new KING855Connector(Environment: BaseConnector.GameEnvironment.PROD);

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
				_common.LogError("KING855Game/UpdJobTime" + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), P_FilePrefix);
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
				_common.LogError("KING855Game/GetJobTime" + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), P_FilePrefix);
				return "Error:" + ex.Message.ToString();
			}
		}

		public void KING855Job()
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
			List<long> list = new List<long>();

			try
			{

				King855.Model.GetReportResponse result = api.RetrieveTransactionByMinute();

				using (BingoConn db = new BingoConn())
				{
					King855GameMain king855GameMain = new King855GameMain
					{
						Id = sMainID,
						Total = null,
						StartTime = Convert.ToDateTime(startTime),
						EndTime = Convert.ToDateTime(endTime),
						CreatedDate = DateTime.Now
					};
					db.King855GameMain.Add(king855GameMain);
					db.SaveChanges();

					if (result != null)
					{
						if (result.list != null)
						{
							if (result.list.Count > 0)
							{
								foreach (var b in result.list)
								{
									try
									{
										King855Game king855Game = new King855Game()
										{
											fId = sMainID,
											id = b.id,
											lobbyId = b.lobbyId,
											shoeId = b.shoeId,
											playId = b.playId,
											gameType = b.GameType,
											gameId = b.GameId,
											memberId = b.memberId,
											betTime = b.betTime,
											calTime = b.calTime,
											winOrLoss = Convert.ToDecimal(b.winOrLoss),
											winOrLossz = Convert.ToDecimal(b.winOrLossz),
											betPoints = Convert.ToDecimal(b.betPoints),
											betPointsz = Convert.ToDecimal(b.betPointsz),
											availableBet = Convert.ToDecimal(b.availableBet),
											userName = b.userName,
											result = b.result,
											betDetail = b.betDetail,
											betDetailz = b.betDetailz,
											ip = b.ip,
											ext = b.ext,
											isRevocation = b.isRevocation,
											balanceBefore = Convert.ToDecimal(b.balanceBefore),
											parentBetId = b.parentBetId,
											currencyId = b.currencyId,
											deviceType = b.deviceType,
											pluginid = b.pluginid,
											CreatedDate = DateTime.Now
										};
										db.King855Game.AddOrUpdate(king855Game);
										list.Add(b.id);
									}
									catch (DbEntityValidationException e)
									{
										foreach (var eve in e.EntityValidationErrors)
										{
											_common.LogError("KING855Game/GetGameRecord" + " " + DateTime.Now.ToString() + " " + string.Format("Entity of type \"{0}\" in state \"{1}\" has the following validation errors:",
															eve.Entry.Entity.GetType().Name, eve.Entry.State), P_FilePrefix);
											foreach (var ve in eve.ValidationErrors)
											{
												_common.LogError("KING855Game/GetGameRecord" + " " + DateTime.Now.ToString() + " " + string.Format("- Property: \"{0}\", Error: \"{1}\"",
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
								db.SaveChanges();

								if (list.Count > 0)
								{
									King855.Model.MarkResponse mresult = api.MarkRecord(list);
									if (mresult != null)
									{
										bool isUpdJob = UpdJobTime(startTime, endTime, sMainID);
									}
								}
							}
							else
							{
								bool isUpdJob = UpdJobTime(startTime, endTime, sMainID);
							}
						}
					}
				}

				return true;
			}
			catch (DbEntityValidationException e)
			{
				foreach (var eve in e.EntityValidationErrors)
				{
					_common.LogError("KING855Game/GetGameRecord" + " " + DateTime.Now.ToString() + " " + string.Format("Entity of type \"{0}\" in state \"{1}\" has the following validation errors:",
									eve.Entry.Entity.GetType().Name, eve.Entry.State), P_FilePrefix);
					foreach (var ve in eve.ValidationErrors)
					{
						_common.LogError("KING855Game/GetGameRecord" + " " + DateTime.Now.ToString() + " " + string.Format("- Property: \"{0}\", Error: \"{1}\"",
										ve.PropertyName, ve.ErrorMessage), P_FilePrefix);
					}
				}
				return false;
			}
			catch (WebException ex)
			{
				var responseText = new StreamReader(ex.Response.GetResponseStream()).ReadToEnd();
				_common.LogError("KING855Game/GetGameRecord" + " " + DateTime.Now.ToString() + " " + ex.Message.ToString() + " " + responseText, P_FilePrefix);
				return false;
			}
			catch (Exception ex)
			{
				_common.LogError("KING855Game/GetGameRecord" + " " + DateTime.Now.ToString() + " " + ex.Message.ToString(), P_FilePrefix);
				return false;
			}
		}

		public string UpdateLimit()
        {
			try
			{
				int iCntS = 0;
				int iCntF = 0;

				using (var bingoConn = new BingoConn())
				{
					var gameMappings = bingoConn.GameMappings
							.Where(s => s.Game == P_GamePrefix)
							.ToList();

					foreach (var mapping in gameMappings)
					{
						King855.Model.UpdateLimitResponse result = api.UpdateLimit(mapping.GamePlayerID);

                        if (result.codeId == 0)
                        {
							//return "Success";
							iCntS++;
                        }
                        else
                        {
							//return "Failed";
							iCntF++;
                        }
                    }
				}

				return iCntS.ToString() + " -- " + iCntF.ToString();
			}
			catch (Exception ex)
			{
				_common.LogError("KING855/UpdateLimit" + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), P_FilePrefix);
				return "Error:" + ex.Message.ToString();
			}
		}
	}
}