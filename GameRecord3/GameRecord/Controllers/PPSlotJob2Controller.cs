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
using Newtonsoft.Json;
using System.Web.Script.Serialization;
using System.Dynamic;
using Pragmatic.Model;

namespace GameRecord.Controllers
{
	public class PPSlotJob2Controller : Controller
	{
		public static readonly object MyLock = new object();
		private string P_GamePrefix = "PPSLOT2";
		private string P_FilePrefix = "PPSLOT2";
		private string P_DTFormat = "yyyy-MM-ddTHH:mm:ss.fffZ";
		private string gameImgUrl = string.Empty;
		Common _common = new Common();
		private PPSlotConnector api = new PPSlotConnector(Environment: BaseConnector.GameEnvironment.PROD);

		public bool UpdJobTime(string startTime, string endTime, string sMainID, string lastVersionKey)
		{
			try
			{
				DateTime dtEnd = Convert.ToDateTime(endTime);

				_common.UpdateADMJobDate(P_GamePrefix, dtEnd, lastVersionKey);

				//using (var bingoConn = new BingoConn())
				//{
				//	var xyz = bingoConn.ADMJobDates.Where(s => s.GameProvider == P_GamePrefix).FirstOrDefault();
				//	if (xyz != null)
				//	{
				//		xyz.LastRunTillDate = dtEnd;
				//		xyz.LastVersionKey = lastVersionKey;

				//		//bingoConn.Entry(xyz).State = System.Data.Entity.EntityState.Modified;
				//		bingoConn.SaveChanges();
				//	}
				//}
				return true;
			}
			catch (Exception ex)
			{
				_common.LogError("PPSlot/UpdJobTime" + " " + DateTime.Now.ToString() + "  " + ex.InnerException.InnerException.ToString(), P_FilePrefix);
				return false;
			}
		}

		public string GetLastVersionKey()
		{
			try
			{
				string sKey = "";

				using (var bingoConn = new BingoConn())
				{
					var query = bingoConn.ADMJobDates.Where(s => s.GameProvider == P_GamePrefix).FirstOrDefault();

					sKey = query.LastVersionKey;
				}

				return sKey;
			}
			catch (Exception ex)
			{
				_common.LogError("PPSlot/GetLastVersionKey" + " " + DateTime.Now.ToString() + "  " + ex.InnerException.InnerException.ToString(), P_FilePrefix);
				return "Error:" + ex.InnerException.InnerException.ToString();
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
				}

				return sTime;

			}
			catch (Exception ex)
			{
				_common.LogError("PPSlot/GetJobTime" + " " + DateTime.Now.ToString() + "  " + ex.InnerException.InnerException.ToString(), P_FilePrefix);
				return "Error:" + ex.InnerException.InnerException.ToString();
			}
		}

		public void PPSlotJob()
		{
			try
			{
				lock (MyLock)
				{
					string dataType = "RNG";
					bool isOK = GetGameRecord(dataType);
				}
			}
			catch (Exception ex)
			{
				_common.LogError("Start - " + DateTime.Now.ToString() + "  " + ex.InnerException.InnerException.ToString(), P_FilePrefix);
			}
		}
		public bool GetGameRecord(string dataType)
		{
			string startTime = GetJobTime("Start");
			string endTime = GetJobTime("End");
			string timeSpan = GetLastVersionKey();
			string sMainID = Guid.NewGuid().ToString();
			//long timeSpan = (long)Convert.ToDateTime(startTime).Subtract(new DateTime(1970, 1, 1)).TotalMilliseconds;

			try
			{

				Pragmatic.Model.GetGameRoundsResponse result = api.RetrieveTransactionByTime(timeSpan, dataType);

				using (BingoConn db = new BingoConn())
				{
					PPSlotGameMain pragmaticMain = new PPSlotGameMain()
					{
						Id = sMainID,
						Total = null,
						StartTime = Convert.ToDateTime(startTime),
						EndTime = Convert.ToDateTime(endTime),
						CreatedDate = DateTime.Now
					};
					db.PPSlotGameMains.Add(pragmaticMain);
					db.SaveChanges();

					timeSpan = Convert.ToString(result.timepoint);

					if (result.rounds.Count > 0)
					{
						foreach (var b in result.rounds)
						{
							try
							{
								PPSlotGame pragmaticGame = new PPSlotGame()
								{
									fId = sMainID,
									playerID = b.playerID,
									extPlayerID = b.extPlayerID,
									gameID = b.gameID,
									playSessionID = b.playSessionID,
									parentSessionID = b.parentSessionID,
									startDate = b.startDate,
									endDate = b.endDate,
									status = b.status,
									type = b.type,
									bet = Convert.ToDecimal(b.bet),
									win = Convert.ToDecimal(b.win),
									rollover = _common.GetSlotRollover(Convert.ToDecimal(b.bet), Convert.ToDecimal(b.win - b.bet)),
									currency = b.currency,
									jackpot = Convert.ToDecimal(b.jackpot),
									platform = b.platform,
									bonusCode = b.bonusCode,
									CreatedDate = DateTime.Now
								};
								db.PPSlotGames.AddOrUpdate(pragmaticGame);
							}
							catch (DbEntityValidationException e)
							{
								foreach (var eve in e.EntityValidationErrors)
								{
									_common.LogError("PPSlot/GetGameRecord" + " " + DateTime.Now.ToString() + " " + string.Format("Entity of type \"{0}\" in state \"{1}\" has the following validation errors:",
																	eve.Entry.Entity.GetType().Name, eve.Entry.State), P_FilePrefix);
									foreach (var ve in eve.ValidationErrors)
									{
										_common.LogError("PPSlot/GetGameRecord" + " " + DateTime.Now.ToString() + " " + string.Format("- Property: \"{0}\", Error: \"{1}\"",
																		ve.PropertyName, ve.ErrorMessage), P_FilePrefix);
									}
								}
							}
							catch (WebException ex)
							{
								var error = new StreamReader(ex.Response.GetResponseStream()).ReadToEnd();
								_common.LogError("Record " + b.playSessionID + " " + DateTime.Now.ToString() + " " + ex.InnerException.InnerException.ToString() + " " + error, P_FilePrefix);
							}
							catch (Exception ex)
							{
								_common.LogError("Record " + b.playSessionID + " " + DateTime.Now.ToString() + " " + ex.InnerException.InnerException.ToString(), P_FilePrefix);
							}
						}
						db.SaveChanges();

					}

					bool isUpdJob = UpdJobTime(startTime, endTime, sMainID, timeSpan);
				}


				return true;
			}
			catch (DbEntityValidationException e)
			{
				foreach (var eve in e.EntityValidationErrors)
				{
					_common.LogError("PPSlot/GetGameRecord" + " " + DateTime.Now.ToString() + " " + string.Format("Entity of type \"{0}\" in state \"{1}\" has the following validation errors:",
													eve.Entry.Entity.GetType().Name, eve.Entry.State), P_FilePrefix);
					foreach (var ve in eve.ValidationErrors)
					{
						_common.LogError("PPSlot/GetGameRecord" + " " + DateTime.Now.ToString() + " " + string.Format("- Property: \"{0}\", Error: \"{1}\"",
														ve.PropertyName, ve.ErrorMessage), P_FilePrefix);
					}
				}
				return false;
			}
			catch (WebException ex)
			{
				var responseText = new StreamReader(ex.Response.GetResponseStream()).ReadToEnd();
				_common.LogError("PPSlot/GetGameRecord" + " " + DateTime.Now.ToString() + " " + ex.InnerException.InnerException.ToString() + " " + responseText, P_FilePrefix);
				return false;
			}
			catch (Exception ex)
			{
				_common.LogError("PPSlot/GetGameRecord" + " " + DateTime.Now.ToString() + " " + ex.InnerException.InnerException.ToString(), P_FilePrefix);
				return false;
			}
		}
	}
}