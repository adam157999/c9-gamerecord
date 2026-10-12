using GameRecord.Controllers.V2;
using GameRecord.Models;
using Newtonsoft.Json;
using System;
using System.Data.Entity.Migrations;
using System.Data.Entity.Validation;
using System.IO;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace GameRecord.Controllers
{
	public class KAController : Controller
	{
		public static readonly object MyLock = new object();
		private string P_GamePrefix = "KA";

		private string P_FilePrefix = "KA";
		private string P_DTFormat = "yyyy-MM-ddTHH:mm:ss.fffZ";
		private bool hasNext = false;
		Common _common = new Common();
		private KAConnector api = new KAConnector(Environment: BaseConnector.GameEnvironment.PROD);

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
				_common.LogError("KA/UpdJobTime" + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), P_FilePrefix);
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
				_common.LogError("KA/GetJobTime" + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), P_FilePrefix);
				return "Error:" + ex.Message.ToString();
			}
		}

		public void KAJob()
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
			string mStartTime = startTime;
			string mEndTime = endTime;
			int limit = 1440; // 24 hours
			int count = 0;
			int page = 1;
			long pageSize = 10000;
			int timeOffSet = 8;
			long offSet = 0;

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

					KA.Model.GameReportResponse result = api.RetrieveTransactionByTime(Convert.ToDateTime(mStartTime), Convert.ToDateTime(mEndTime), timeOffSet, offSet, pageSize);

					using (BingoConn db = new BingoConn())
					{
						string sMainID = Guid.NewGuid().ToString();
						KAGameMain KAMain = new KAGameMain()
						{
							Id = sMainID,
							Total = null,
							StartTime = Convert.ToDateTime(startTime),
							EndTime = Convert.ToDateTime(endTime),
							CreatedDate = DateTime.Now
						};
						db.KAGameMains.Add(KAMain);
						db.SaveChanges();
						//Insert for the KA Spin Report

						if (result.statusCode == 0)
						{
							if (result.spinReport != null)
							{
								if (result.spinReport.Count > 0)
								{
									foreach (var b in result.spinReport)
									{
										try
										{
											KASpinGame kaSpinGame = new KASpinGame()
											{
												fId = sMainID,
												TransactionId = b.transactionId,
												PlayerId = b.playerId,
												Operator = b.Operator,
												GameName = b.gameName,
												GameType = b.gameType,
												SpinDate = b.spinDate,
												TzOffset = b.tzOffset,
												Currency = b.currency,
												Rtp = b.rtp,
												Denomination = b.denomination,
												Selections = b.selections,
												CreditPerSelection = b.credirtPerSelection,
												CashPlayed = b.freeSpin == true ? 0 : b.cashPlayed,
												CashPlayedRaw = b.freeSpin == true ? 0 : b.cashPlayedRaw,
												Rollover = (long)_common.GetSlotRollover((decimal)(b.freeSpin == true ? 0 : b.cashPlayed), (decimal)(b.cashWon - (b.freeSpin == true ? 0 : b.cashPlayed))),
												CashWon = b.cashWon,
												CashWonRaw = b.cashWonRaw,
												CreditsPlayed = b.creditsPlayed,
												CreditsWon = b.creditsWon,
												FreeSpin = b.freeSpin,
												Round = b.round,
												RoundRemaining = b.roundsRemaining,
												//CreatedDate = DateTime.Now
											};
											var record = db.KASpinGames.Where(x => x.TransactionId == b.transactionId && x.Round == b.round).FirstOrDefault();

											if (record == null)
											{
												kaSpinGame.CreatedDate = DateTime.Now;
											}
											else
											{
												kaSpinGame.CreatedDate = record.CreatedDate;
												kaSpinGame.ModifiedDate = DateTime.Now;
											}
											db.KASpinGames.AddOrUpdate(kaSpinGame);
										}
										catch (DbEntityValidationException e)
										{
											foreach (var eve in e.EntityValidationErrors)
											{
												_common.LogError("KASpinGame/GetGameRecord" + " " + DateTime.Now.ToString() + " " + string.Format("Entity of type \"{0}\" in state \"{1}\" has the following validation errors:",
																eve.Entry.Entity.GetType().Name, eve.Entry.State), P_FilePrefix);
												foreach (var ve in eve.ValidationErrors)
												{
													_common.LogError("KASpinGame/GetGameRecord" + " " + DateTime.Now.ToString() + " " + string.Format("- Property: \"{0}\", Error: \"{1}\"",
																	ve.PropertyName, ve.ErrorMessage), P_FilePrefix);
												}
											}
										}
										catch (WebException ex)
										{
											var error = new StreamReader(ex.Response.GetResponseStream()).ReadToEnd();
											_common.LogError("Record " + b.transactionId + " " + DateTime.Now.ToString() + " " + ex.Message.ToString() + " " + error, P_FilePrefix);
										}
										catch (Exception ex)
										{
											_common.LogError("Record " + b.transactionId + " " + DateTime.Now.ToString() + " " + ex.Message.ToString(), P_FilePrefix);
										}
									}
									db.SaveChanges();
								}
							}

							if (result.bonusPickReport != null)
							{
								//Insert for the KA Bonus Pick Report
								if (result.bonusPickReport.Count > 0)
								{
									foreach (var b in result.bonusPickReport)
									{
										try
										{
											KABonusPickGame kaBonusPickGame = new KABonusPickGame()
											{
												fId = sMainID,
												TransactionId = b.transactionId,
												PlayerId = b.playerId,
												Operator = b.Operator,
												GameName = b.gameName,
												TzOffset = b.tzOffset,
												Currency = b.currency,
												PickDate = b.pickDate,
												Denomination = b.denomination,
												CashWon = b.cashWon,
												CashWonRaw = b.cashWonRaw,
												CreditsWon = b.creditsWon,
												CreatedDate = DateTime.Now
											};
											db.KABonusPickGames.AddOrUpdate(kaBonusPickGame);
										}
										catch (DbEntityValidationException e)
										{
											foreach (var eve in e.EntityValidationErrors)
											{
												_common.LogError("KABonusPick/GetGameRecord" + " " + DateTime.Now.ToString() + " " + string.Format("Entity of type \"{0}\" in state \"{1}\" has the following validation errors:",
																eve.Entry.Entity.GetType().Name, eve.Entry.State), P_FilePrefix);
												foreach (var ve in eve.ValidationErrors)
												{
													_common.LogError("KABonusPick/GetGameRecord" + " " + DateTime.Now.ToString() + " " + string.Format("- Property: \"{0}\", Error: \"{1}\"",
																	ve.PropertyName, ve.ErrorMessage), P_FilePrefix);
												}
											}
										}
										catch (WebException ex)
										{
											var error = new StreamReader(ex.Response.GetResponseStream()).ReadToEnd();
											_common.LogError("Record " + b.transactionId + " " + DateTime.Now.ToString() + " " + ex.Message.ToString() + " " + error, P_FilePrefix);
										}
										catch (Exception ex)
										{
											_common.LogError("Record " + b.transactionId + " " + DateTime.Now.ToString() + " " + ex.Message.ToString(), P_FilePrefix);
										}
									}
									db.SaveChanges();
								}
							}

							if (result.hasNext == true)
							{
								page = page - 1;
								hasNext = true;
							}
							else
								hasNext = false;


							while (hasNext == true)
							{
								page += 1;
								long os = (long)page * pageSize;
								GetGameRecordExtend(timeOffSet, os, pageSize, mStartTime, mEndTime, sMainID);
							}

							bool isUpdJob = UpdJobTime(startTime, endTime, sMainID);
						}
					}
				}

				return true;
			}
			catch (DbEntityValidationException e)
			{
				foreach (var eve in e.EntityValidationErrors)
				{
					_common.LogError("KA/GetGameRecord" + " " + DateTime.Now.ToString() + " " + string.Format("Entity of type \"{0}\" in state \"{1}\" has the following validation errors:",
									eve.Entry.Entity.GetType().Name, eve.Entry.State), P_FilePrefix);
					foreach (var ve in eve.ValidationErrors)
					{
						_common.LogError("KA/GetGameRecord" + " " + DateTime.Now.ToString() + " " + string.Format("- Property: \"{0}\", Error: \"{1}\"",
										ve.PropertyName, ve.ErrorMessage), P_FilePrefix);
					}
				}
				return false;
			}
			catch (WebException ex)
			{
				var responseText = new StreamReader(ex.Response.GetResponseStream()).ReadToEnd();
				_common.LogError("KA/GetGameRecord" + " " + DateTime.Now.ToString() + " " + ex.ToString() + " " + responseText, P_FilePrefix);
				return false;
			}
			catch (Exception ex)
			{
				_common.LogError("KA/GetGameRecord" + " " + DateTime.Now.ToString() + " " + ex.ToString(), P_FilePrefix);
				return false;
			}
		}

		public bool GetGameRecordExtend(int timeOffSet, long offSet, long pageSize, string startTime, string endTime, string sMainID)
		{
			try
			{

				KA.Model.GameReportResponse result = api.RetrieveTransactionByTime(Convert.ToDateTime(startTime), Convert.ToDateTime(endTime), timeOffSet, offSet, pageSize);

				using (BingoConn db = new BingoConn())
				{
					if (result.spinReport != null)
					{
						//Insert for the KA Spin Report
						if (result.spinReport.Count > 0)
						{
							foreach (var b in result.spinReport)
							{
								try
								{
									KASpinGame kaSpinGame = new KASpinGame()
									{
										fId = sMainID,
										TransactionId = b.transactionId,
										PlayerId = b.playerId,
										Operator = b.Operator,
										GameName = b.gameName,
										GameType = b.gameType,
										SpinDate = b.spinDate,
										TzOffset = b.tzOffset,
										Currency = b.currency,
										Rtp = b.rtp,
										Denomination = b.denomination,
										Selections = b.selections,
										CreditPerSelection = b.credirtPerSelection,
										CashPlayed = b.freeSpin == true ? 0 : b.cashPlayed,
										CashPlayedRaw = b.freeSpin == true ? 0 : b.cashPlayedRaw,
										Rollover = (long)_common.GetSlotRollover((decimal)(b.freeSpin == true ? 0 : b.cashPlayed), (decimal)(b.cashWon - (b.freeSpin == true ? 0 : b.cashPlayed))),
										CashWon = b.cashWon,
										CashWonRaw = b.cashWonRaw,
										CreditsPlayed = b.creditsPlayed,
										CreditsWon = b.creditsWon,
										FreeSpin = b.freeSpin,
										Round = b.round,
										RoundRemaining = b.roundsRemaining,
										//CreatedDate = DateTime.Now
									};
									var record = db.KASpinGames.Where(x => x.TransactionId == b.transactionId && x.Round == b.round).FirstOrDefault();

									if (record == null)
									{
										kaSpinGame.CreatedDate = DateTime.Now;
									}
									else
									{
										kaSpinGame.CreatedDate = record.CreatedDate;
										kaSpinGame.ModifiedDate = DateTime.Now;
									}
									db.KASpinGames.AddOrUpdate(kaSpinGame);
								}
								catch (DbEntityValidationException e)
								{
									foreach (var eve in e.EntityValidationErrors)
									{
										_common.LogError("KASpinGame/GetGameRecord" + " " + DateTime.Now.ToString() + " " + string.Format("Entity of type \"{0}\" in state \"{1}\" has the following validation errors:",
														eve.Entry.Entity.GetType().Name, eve.Entry.State), P_FilePrefix);
										foreach (var ve in eve.ValidationErrors)
										{
											_common.LogError("KASpinGame/GetGameRecord" + " " + DateTime.Now.ToString() + " " + string.Format("- Property: \"{0}\", Error: \"{1}\"",
															ve.PropertyName, ve.ErrorMessage), P_FilePrefix);
										}
									}
								}
								catch (WebException ex)
								{
									var error = new StreamReader(ex.Response.GetResponseStream()).ReadToEnd();
									_common.LogError("Record " + b.transactionId + " " + DateTime.Now.ToString() + " " + ex.Message.ToString() + " " + error, P_FilePrefix);
								}
								catch (Exception ex)
								{
									_common.LogError("Record " + b.transactionId + " " + DateTime.Now.ToString() + " " + ex.Message.ToString(), P_FilePrefix);
								}
							}
							db.SaveChanges();

						}
					}

					if (result.bonusPickReport != null)
					{
						//Insert for the KA Bonus Pick Report
						if (result.bonusPickReport.Count > 0)
						{
							foreach (var b in result.bonusPickReport)
							{
								try
								{
									KABonusPickGame kaBonusPickGame = new KABonusPickGame()
									{
										fId = sMainID,
										TransactionId = b.transactionId,
										PlayerId = b.playerId,
										Operator = b.Operator,
										GameName = b.gameName,
										TzOffset = b.tzOffset,
										Currency = b.currency,
										PickDate = b.pickDate,
										Denomination = b.denomination,
										CashWon = b.cashWon,
										CashWonRaw = b.cashWonRaw,
										CreditsWon = b.creditsWon,
										CreatedDate = DateTime.Now
									};
									db.KABonusPickGames.AddOrUpdate(kaBonusPickGame);
								}
								catch (DbEntityValidationException e)
								{
									foreach (var eve in e.EntityValidationErrors)
									{
										_common.LogError("KABonusPick/GetGameRecord" + " " + DateTime.Now.ToString() + " " + string.Format("Entity of type \"{0}\" in state \"{1}\" has the following validation errors:",
														eve.Entry.Entity.GetType().Name, eve.Entry.State), P_FilePrefix);
										foreach (var ve in eve.ValidationErrors)
										{
											_common.LogError("KABonusPick/GetGameRecord" + " " + DateTime.Now.ToString() + " " + string.Format("- Property: \"{0}\", Error: \"{1}\"",
															ve.PropertyName, ve.ErrorMessage), P_FilePrefix);
										}
									}
								}
								catch (WebException ex)
								{
									var error = new StreamReader(ex.Response.GetResponseStream()).ReadToEnd();
									_common.LogError("Record " + b.transactionId + " " + DateTime.Now.ToString() + " " + ex.Message.ToString() + " " + error, P_FilePrefix);
								}
								catch (Exception ex)
								{
									_common.LogError("Record " + b.transactionId + " " + DateTime.Now.ToString() + " " + ex.Message.ToString(), P_FilePrefix);
								}
							}
							db.SaveChanges();

						}
					}


					if (result.hasNext == true)
						hasNext = true;
					else
						hasNext = false;
				}

				return true;
			}
			catch (DbEntityValidationException e)
			{
				foreach (var eve in e.EntityValidationErrors)
				{
					_common.LogError("KA/GetGameRecord" + " " + DateTime.Now.ToString() + " " + string.Format("Entity of type \"{0}\" in state \"{1}\" has the following validation errors:",
									eve.Entry.Entity.GetType().Name, eve.Entry.State), P_FilePrefix);
					foreach (var ve in eve.ValidationErrors)
					{
						_common.LogError("KA/GetGameRecord" + " " + DateTime.Now.ToString() + " " + string.Format("- Property: \"{0}\", Error: \"{1}\"",
										ve.PropertyName, ve.ErrorMessage), P_FilePrefix);
					}
				}
				return false;
			}
			catch (WebException ex)
			{
				var responseText = new StreamReader(ex.Response.GetResponseStream()).ReadToEnd();
				_common.LogError("KA/GetGameRecord" + " " + DateTime.Now.ToString() + " " + ex.Message.ToString() + " " + responseText, P_FilePrefix);
				return false;
			}
			catch (Exception ex)
			{
				_common.LogError("KA/GetGameRecord" + " " + DateTime.Now.ToString() + " " + ex.Message.ToString(), P_FilePrefix);
				return false;
			}
		}
	}
}