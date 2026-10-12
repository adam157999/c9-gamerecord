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

namespace GameRecord.Controllers
{
	public class PragmaticLiveController : Controller
	{
		public static readonly object MyLock = new object();
		private string P_GamePrefix = "PRAGMATICLIVE";

		private string P_FilePrefix = "PRAGMATICLIVE";
		private string P_DTFormat = "yyyy-MM-ddTHH:mm:ss.fffZ";
		Common _common = new Common();

		private PragmaticLiveConnector api = new PragmaticLiveConnector(Environment: BaseConnector.GameEnvironment.PROD);

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
				_common.LogError("PragmaticLive/UpdJobTime" + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), P_FilePrefix);
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
				_common.LogError("PragmaticLive/GetLastVersionKey" + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), P_FilePrefix);
				return "Error:" + ex.Message.ToString();
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
				_common.LogError("PragmaticLive/GetJobTime" + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), P_FilePrefix);
				return "Error:" + ex.Message.ToString();
			}
		}

		public void PragmaticLiveJob()
		{
			try
			{
				lock (MyLock)
				{
					string dataType = "LC";
					bool isOK = GetGameRecord(dataType);
				}
			}
			catch (Exception ex)
			{
				_common.LogError("Start - " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), P_FilePrefix);
			}
		}
		public bool GetGameRecord(string dataType)
		{
			string startTime = GetJobTime("Start");
			string endTime = GetJobTime("End");
			string sMainID = Guid.NewGuid().ToString();
			string timeSpan = GetLastVersionKey();
			//string timeSpan = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
			//string timeSpan = Convert.ToString(new DateTimeOffset(Convert.ToDateTime(startTime)).ToUnixTimeMilliseconds());
			//long timeSpan = (long)Convert.ToDateTime(startTime).Subtract(new DateTime(1970, 1, 1)).TotalMilliseconds;

			try
			{

				Pragmatic.Model.GetGameRoundsResponse result = api.RetrieveTransactionByTime(timeSpan, dataType);

				using (BingoConn db = new BingoConn())
				{
					PragmaticLiveGameMain pragmaticMain = new PragmaticLiveGameMain()
					{
						Id = sMainID,
						Total = null,
						StartTime = Convert.ToDateTime(startTime),
						EndTime = Convert.ToDateTime(endTime),
						CreatedDate = DateTime.Now
					};
					db.PragmaticLiveGameMains.Add(pragmaticMain);
					db.SaveChanges();

					timeSpan = Convert.ToString(result.timepoint);

					if (result.rounds.Count > 0)
					{
						foreach (var b in result.rounds)
						{
							try
							{
								PragmaticLiveGame pragmaticGame = new PragmaticLiveGame()
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
									currency = b.currency,
									jackpot = Convert.ToDecimal(b.jackpot),
									platform = b.platform,
									bonusCode = b.bonusCode,
									CreatedDate = DateTime.Now
								};
								db.PragmaticLiveGames.AddOrUpdate(pragmaticGame);
							}
							catch (DbEntityValidationException e)
							{
								foreach (var eve in e.EntityValidationErrors)
								{
									_common.LogError("Pragmatic/GetGameRecord" + " " + DateTime.Now.ToString() + " " + string.Format("Entity of type \"{0}\" in state \"{1}\" has the following validation errors:",
													eve.Entry.Entity.GetType().Name, eve.Entry.State), P_FilePrefix);
									foreach (var ve in eve.ValidationErrors)
									{
										_common.LogError("Pragmatic/GetGameRecord" + " " + DateTime.Now.ToString() + " " + string.Format("- Property: \"{0}\", Error: \"{1}\"",
														ve.PropertyName, ve.ErrorMessage), P_FilePrefix);
									}
								}
							}
							catch (WebException ex)
							{
								var error = new StreamReader(ex.Response.GetResponseStream()).ReadToEnd();
								_common.LogError("Record " + b.playSessionID + " " + DateTime.Now.ToString() + " " + ex.Message.ToString() + " " + error, P_FilePrefix);
							}
							catch (Exception ex)
							{
								_common.LogError("Record " + b.playSessionID + " " + DateTime.Now.ToString() + " " + ex.Message.ToString(), P_FilePrefix);
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
					_common.LogError("Pragmatic/GetGameRecord" + " " + DateTime.Now.ToString() + " " + string.Format("Entity of type \"{0}\" in state \"{1}\" has the following validation errors:",
									eve.Entry.Entity.GetType().Name, eve.Entry.State), P_FilePrefix);
					foreach (var ve in eve.ValidationErrors)
					{
						_common.LogError("Pragmatic/GetGameRecord" + " " + DateTime.Now.ToString() + " " + string.Format("- Property: \"{0}\", Error: \"{1}\"",
										ve.PropertyName, ve.ErrorMessage), P_FilePrefix);
					}
				}
				return false;
			}
			catch (WebException ex)
			{
				var responseText = new StreamReader(ex.Response.GetResponseStream()).ReadToEnd();
				_common.LogError("Pragmatic/GetGameRecord" + " " + DateTime.Now.ToString() + " " + ex.Message.ToString() + " " + responseText, P_FilePrefix);
				return false;
			}
			catch (Exception ex)
			{
				_common.LogError("Pragmatic/GetGameRecord" + " " + DateTime.Now.ToString() + " " + ex.Message.ToString(), P_FilePrefix);
				return false;
			}
		}
	}
}