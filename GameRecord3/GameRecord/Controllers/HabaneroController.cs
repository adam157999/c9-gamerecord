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
	public class HabaneroController : Controller
	{
		public static readonly object MyLock = new object();
		private string P_GamePrefix = "Habanero";

		private string P_FilePrefix = "Habanero";
		private string P_DTFormat = "yyyy-MM-ddTHH:mm:ss.fffZ";
		Common _common = new Common();
		private HabaneroConnector api = new HabaneroConnector(Environment: BaseConnector.GameEnvironment.PROD);

		public HabaneroController(GameEnvironment Environment = GameEnvironment.PROD)
		{
			api = new HabaneroConnector(Environment: Environment);
		}

		public HabaneroController()
		{
			api = new HabaneroConnector(Environment: GameEnvironment.PROD);
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
				_common.LogError("Habanero/UpdJobTime" + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), P_FilePrefix);
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
				_common.LogError("Habanero/GetJobTime" + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), P_FilePrefix);
				return "Error:" + ex.Message.ToString();
			}
		}

		public void HabaneroJob()
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
			int limit = 1440; // 24 hours
			int count = 0;
			try
			{
				int MinDiff = Convert.ToInt32((Convert.ToDateTime(endTime) - Convert.ToDateTime(startTime)).TotalMinutes);
				int times = (MinDiff / limit) + 1;
				for (int j = limit; j <= times * limit; j += limit)
				{
					mStartTime = j == limit ? startTime : mEndTime;

					if ((Convert.ToDateTime(mStartTime).AddMinutes(limit) - Convert.ToDateTime(endTime)).TotalMinutes > 0)
						mEndTime = endTime;
					else
						mEndTime = Convert.ToString(Convert.ToDateTime(mStartTime).AddMinutes(limit));

					//Habanero.Model.GetBrandCompletedGameResultV2RequestResponse result = api.GetBrandCompletedGameResultsV2(Convert.ToDateTime(startTime), Convert.ToDateTime(endTime));
					List<Habanero.Model.PlayerCompletedGamesV2DTO> result = api.GetBrandCompletedGameResultsV2(Convert.ToDateTime(mStartTime), Convert.ToDateTime(mEndTime));

					using (BingoConn db = new BingoConn())
					{
						HabaneroGameMain hbrMain = new HabaneroGameMain()
						{
							Id = sMainID,
							Total = result.Count,
							StartTime = Convert.ToDateTime(startTime),
							EndTime = Convert.ToDateTime(endTime),
							CreatedDate = DateTime.Now
						};
						db.HabaneroGameMains.Add(hbrMain);
						db.SaveChanges();

						if (result.Count > 0)
						{
							foreach (var b in result)
							{
								try
								{
									HabaneroGame habaneroGame = new HabaneroGame()
									{
										fId = sMainID,
										PlayerId = b.PlayerId,
										BrandId = b.BrandId,
										Username = b.Username,
										BrandGameId = b.BrandGameId,
										GameKeyName = b.GameKeyName,
										GameTypeId = b.GameTypeId,
										DtStarted = b.DtStarted,
										DtCompleted = b.DtCompleted,
										FriendlyGameInstanceId = b.FriendlyGameInstanceId,
										GameInstanceId = b.GameInstanceId,
										GameStateId = b.GameStateId,
										Stake = b.Stake,
										Rollover = _common.GetSlotRollover(Math.Abs(b.Stake), b.Payout - Math.Abs(b.Stake)),
										Payout = b.Payout,
										JackpotWin = b.JackpotWin,
										JackpotContribution = b.JackpotContribution,
										CurrencyCode = b.CurrencyCode,
										ChannelTypeId = b.ChannelTypeId,
										BalanceAfter = b.BalanceAfter,
										BonusStake = b.BonusStake,
										BonusCoupon = b.BonusCoupon,
										BonusToReal = b.BonusToReal,
										BonusPayout = b.BonusPayout,
										FeatureCount = b.FeatureCount,
										BuyFeatureId = b.BuyFeatureId,
										CreatedDate = DateTime.Now
									};
									db.HabaneroGames.AddOrUpdate(habaneroGame);
								}
								catch (DbEntityValidationException e)
								{
									foreach (var eve in e.EntityValidationErrors)
									{
										_common.LogError("Habenero/GetGameRecord" + " " + DateTime.Now.ToString() + " " + string.Format("Entity of type \"{0}\" in state \"{1}\" has the following validation errors:",
														eve.Entry.Entity.GetType().Name, eve.Entry.State), P_FilePrefix);
										foreach (var ve in eve.ValidationErrors)
										{
											_common.LogError("Habanero/GetGameRecord" + " " + DateTime.Now.ToString() + " " + string.Format("- Property: \"{0}\", Error: \"{1}\"",
															ve.PropertyName, ve.ErrorMessage), P_FilePrefix);
										}
									}
								}
								catch (WebException ex)
								{
									var error = new StreamReader(ex.Response.GetResponseStream()).ReadToEnd();
									_common.LogError("Record " + b.GameInstanceId + " " + DateTime.Now.ToString() + " " + ex.Message.ToString() + " " + error, P_FilePrefix);
								}
								catch (Exception ex)
								{
									_common.LogError("Record " + b.GameInstanceId + " " + DateTime.Now.ToString() + " " + ex.Message.ToString(), P_FilePrefix);
								}
							}
							db.SaveChanges();

						}

						bool isUpdJob = UpdJobTime(startTime, endTime, sMainID);
					}
				}
				//}


				return true;
			}
			catch (DbEntityValidationException e)
			{
				foreach (var eve in e.EntityValidationErrors)
				{
					_common.LogError("Habanero/GetGameRecord" + " " + DateTime.Now.ToString() + " " + string.Format("Entity of type \"{0}\" in state \"{1}\" has the following validation errors:",
									eve.Entry.Entity.GetType().Name, eve.Entry.State), P_FilePrefix);
					foreach (var ve in eve.ValidationErrors)
					{
						_common.LogError("Habanero/GetGameRecord" + " " + DateTime.Now.ToString() + " " + string.Format("- Property: \"{0}\", Error: \"{1}\"",
										ve.PropertyName, ve.ErrorMessage), P_FilePrefix);
					}
				}
				return false;
			}
			catch (WebException ex)
			{
				var responseText = new StreamReader(ex.Response.GetResponseStream()).ReadToEnd();
				_common.LogError("Habanero/GetGameRecord" + " " + DateTime.Now.ToString() + " " + ex.Message.ToString() + " " + responseText, P_FilePrefix);
				return false;
			}
			catch (Exception ex)
			{
				_common.LogError("Habanero/GetGameRecord" + " " + DateTime.Now.ToString() + " " + ex.Message.ToString(), P_FilePrefix);
				return false;
			}
		}
	}
}