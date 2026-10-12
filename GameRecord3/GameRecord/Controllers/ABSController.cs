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
using GameRecord.Controllers.V2;

namespace GameRecord.Controllers
{
	public class ABSController : Controller
	{
		public static readonly object MyLock = new object();
		private string P_GamePrefix = "ABS";

		private string P_FilePrefix = "ABS";
		private string P_DTFormat = "yyyy-MM-ddTHH:mm:ss.fffZ";
		Common _common = new Common();
		private ABSConnector api = new ABSConnector(Environment: BaseConnector.GameEnvironment.PROD);

		public bool UpdJobTime(string startTime, string endTime)
		{
			try
			{
				DateTime dtEnd = Convert.ToDateTime(endTime);

				_common.UpdateADMJobDate(P_GamePrefix, dtEnd, "");

				//using (var bingoConn = new BingoConn())
				//{
				//	var xyz = bingoConn.ADMJobDatess.Where(s => s.GameProvider == P_GamePrefix).FirstOrDefault();
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
				_common.LogError("ABS/UpdJobTime" + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), P_FilePrefix);
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
						DateTime dtSTime = Convert.ToDateTime(query.LastRunTillDate?.ToString("yyyy-MM-dd HH:mm"));
						sTime = dtSTime.ToString("yyyy-MM-dd");
					}
					else
					{
						DateTime dtSTime = Convert.ToDateTime(DateTime.Now.ToString("yyyy-MM-dd HH:mm")) - new TimeSpan(0, 12, 5, 0, 0);
						sTime = dtSTime.ToString("yyyy-MM-dd");
					}
				}

				return sTime;

			}
			catch (Exception ex)
			{
				_common.LogError("BGGame/GetJobTime" + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), P_FilePrefix);
				return "Error:" + ex.Message.ToString();
			}
		}


		public bool UpdRec(int betId, string sMainID)
		{
			try
			{

				using (var bingoConn = new BingoConn())
				{
					var xyz = bingoConn.ADMJobDates.Where(s => s.GameProvider == P_GamePrefix).FirstOrDefault();
					if (xyz != null)
					{
						xyz.Interval = betId;

						//bingoConn.Entry(xyz).State = System.Data.Entity.EntityState.Modified;
						bingoConn.SaveChanges();
					}
				}
				return true;
			}
			catch (Exception ex)
			{
				_common.LogError("ABS/UpdRec" + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), P_FilePrefix);
				return false;
			}
		}

		public int GetRec()
		{
			try
			{
				int rec = 0;

				using (var bingoConn = new BingoConn())
				{
					var query = bingoConn.ADMJobDates.Where(s => s.GameProvider == P_GamePrefix).FirstOrDefault();

					if (query != null)
					{
						if (query.Interval != null)
						{
							rec = (int)query.Interval;
						}

					}
				}

				return rec;

			}
			catch (Exception ex)
			{
				_common.LogError("ABS/GetRec" + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), P_FilePrefix);
				return 0;
			}
		}

		public void ABSJob()
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

		public void ABSHitJob()
		{
			try
			{
				lock (MyLock)
				{
					//string test = "{'Success':true,'Error':0,'Message':'Hit Report: 2023-07-02','Record':{'60154131':{'BetID':'60154131','DrawDate':'02-07-2023','totalBet':36,'totalWin':240,'user':'ABSVCJACKWONG','playerCurrency':'MYR','BetTime':'02-07-2023 16:21:31 PM','Hits':[{'Number':'4164','Class':'P','D4_WinB':240}]},'60167758':{'BetID':'60167758','DrawDate':'02-07-2023','totalBet':48,'totalWin':20,'user':'ABSYIWESMAR89','playerCurrency':'MYR','BetTime':'02-07-2023 18:03:05 PM','Hits':[{'Number':'7143','Class':'P','D4_WinB':20}]},'60139869':{'BetID':'60139869','DrawDate':'02-07-2023','totalBet':27,'totalWin':20,'user':'ABSIRNICH168','playerCurrency':'MYR','BetTime':'02-07-2023 10:26:49 AM','Hits':[{'Number':'8584','Class':'T','D4_WinB':20}]}},'LogID':true}";
					//string test = "{'Success':true,'Error':0,'Message':'Hit Report: 2023-07-02','Record':[],'LogID':true}";
					//dynamic abc = JObject.Parse(test);
					//var zzz = abc.Record.GetType();
					//string qwqwqwq = JsonConvert.SerializeObject(abc.Record);
					//JObject converted = JsonConvert.DeserializeObject<JObject>(qwqwqwq);
					//if (zzz.Name == "JObject")
					//{
					//	Dictionary<string, string> keyValueMap = new Dictionary<string, string>();
					//	foreach (KeyValuePair<string, JToken> keyValuePair in converted)
					//	{
					//		dynamic data = JObject.Parse(keyValuePair.Value.ToString());
					//	}
					//}
					bool isOK = GetHitGameRecord();
				}
			}
			catch (Exception ex)
			{
				_common.LogError("Start - " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), P_FilePrefix);
			}
		}

		public bool GetGameRecord()
		{
			int lastBetId = 0;
			try
			{
				lastBetId = GetRec();

				ABS.Model.GrabTicketResponse result = api.RetrieveTransactionByMinute(lastBetId);

				using (BingoConn db = new BingoConn())
				{
					string sMainID = Guid.NewGuid().ToString();
					if (result.Error == 0)
					{
						if (result.Records.Count > 0)
						{
							foreach (var b in result.Records)
							{
								try
								{
									ABSGame_Ticket ABSTicketGame = new ABSGame_Ticket()
									{
										fId = sMainID,
										BetID = b.BetID,
										BetRecord = b.BetRecord,
										DateCreate = DateTime.ParseExact(b.DateCreate, "dd-MM-yyyy HH:mm:ss tt", CultureInfo.InvariantCulture),
										DateCreate_TimeStamp = Convert.ToInt64(b.DateCreate_timestamp),
										BetTime = FromUnixTime(Convert.ToInt64(b.DateCreate_timestamp)).AddHours(8),
										Total = Convert.ToDecimal(b.Total),
										Currency = b.Currency,
										CreatedDate = DateTime.Now
									};
									db.ABSGame_Ticket.AddOrUpdate(ABSTicketGame);
								}
								catch (DbEntityValidationException e)
								{
									foreach (var eve in e.EntityValidationErrors)
									{
										_common.LogError("ABS/GetGameRecord" + " " + DateTime.Now.ToString() + " " + string.Format("Entity of type \"{0}\" in state \"{1}\" has the following validation errors:",
																										eve.Entry.Entity.GetType().Name, eve.Entry.State), P_FilePrefix);
										foreach (var ve in eve.ValidationErrors)
										{
											_common.LogError("ABS/GetGameRecord" + " " + DateTime.Now.ToString() + " " + string.Format("- Property: \"{0}\", Error: \"{1}\"",
																											ve.PropertyName, ve.ErrorMessage), P_FilePrefix);
										}
									}
								}
								catch (WebException ex)
								{
									var error = new StreamReader(ex.Response.GetResponseStream()).ReadToEnd();
									_common.LogError("Record " + b.BetID + " " + DateTime.Now.ToString() + " " + ex.Message.ToString() + " " + error, P_FilePrefix);
								}
								catch (Exception ex)
								{
									_common.LogError("Record " + b.BetID + " " + DateTime.Now.ToString() + " " + ex.Message.ToString(), P_FilePrefix);
								}
								lastBetId = Convert.ToInt32(b.BetID);
							}
							db.SaveChanges();

						}

						ABSGameMain ABSMain = new ABSGameMain()
						{
							Id = sMainID,
							Total = null,
							StartTime = Convert.ToDateTime(DateTime.Now),
							EndTime = Convert.ToDateTime(DateTime.Now),
							RecNo = 0,
							EndRecNo = lastBetId,
							CreatedDate = DateTime.Now
						};
						db.ABSGameMains.Add(ABSMain);
						db.SaveChanges();

						bool isUpdJob = UpdRec(lastBetId, sMainID);
					}
				}
				return true;
			}
			catch (DbEntityValidationException e)
			{
				foreach (var eve in e.EntityValidationErrors)
				{
					_common.LogError("ABS/GetGameRecord" + " " + DateTime.Now.ToString() + " " + string.Format("Entity of type \"{0}\" in state \"{1}\" has the following validation errors:",
																					eve.Entry.Entity.GetType().Name, eve.Entry.State), P_FilePrefix);
					foreach (var ve in eve.ValidationErrors)
					{
						_common.LogError("ABS/GetGameRecord" + " " + DateTime.Now.ToString() + " " + string.Format("- Property: \"{0}\", Error: \"{1}\"",
																						ve.PropertyName, ve.ErrorMessage), P_FilePrefix);
					}
				}
				return false;
			}
			catch (WebException ex)
			{
				var responseText = new StreamReader(ex.Response.GetResponseStream()).ReadToEnd();
				_common.LogError("ABS/GetGameRecord" + " " + DateTime.Now.ToString() + " " + ex.Message.ToString() + " " + responseText, P_FilePrefix);
				return false;
			}
			catch (Exception ex)
			{
				_common.LogError("ABS/GetGameRecord" + " " + DateTime.Now.ToString() + " " + ex.Message.ToString(), P_FilePrefix);
				return false;
			}
		}

		//public bool GetHitGameRecordOld()
		//{
		//	string startTime = GetJobTime("Start");
		//	string endTime = GetJobTime("End");

		//	DateTime currDt = DateTime.Now;

		//	try
		//	{
		//		string dateFrom = DateTime.Now.ToString("yyyy-MM-dd");

		//		ABS.Model.HitRecordReportResponse result = api.RetrieveHitTransactionByMinute(dateFrom);

		//		using (BingoConn db = new BingoConn())
		//		{
		//			string sMainID = Guid.NewGuid().ToString();
		//			ABSGameMain ABSMain = new ABSGameMain()
		//			{
		//				Id = sMainID,
		//				Total = null,
		//				StartTime = Convert.ToDateTime(startTime),
		//				EndTime = Convert.ToDateTime(endTime),
		//				CreatedDate = currDt
		//			};
		//			db.ABSGameMains.Add(ABSMain);
		//			db.SaveChanges();

		//			if (result != null)
		//			{
		//				if (result.Error == 0)
		//				{
		//					if (result.Record != null)
		//					{
		//						foreach (var r in result.Record)
		//						{
		//							try
		//							{
		//								//summary
		//								string betId = Convert.ToString(r.BetID);
		//								var summrecord = db.ABSGame_Bet.Where(x => x.BetID == betId).FirstOrDefault();
		//								if (summrecord == null)
		//								{
		//									ABSGame_Bet ABSBetGame = new ABSGame_Bet()
		//									{
		//										fId = sMainID,
		//										BetID = r.BetID,
		//										DrawDate = Convert.ToDateTime(r.DrawDate),
		//										totalBet = r.totalBet,
		//										totalWin = Convert.ToDecimal(r.totalWin),
		//										user = r.user,
		//										Currency = r.Currency,
		//										CreatedDate = currDt
		//									};
		//									db.ABSGame_Bet.Add(ABSBetGame);
		//								}

		//								//detail
		//								if (r.Hits.Count > 0)
		//								{
		//									foreach (var c in r.Hits)
		//									{
		//										string betId = Convert.ToString(r.BetID);
		//										var detailrecord = db.ABSGame_Hits.Where(x => x.BetID == r.BetID && x.Number == c.Number && x.Class == c.Class).FirstOrDefault();
		//										if (detailrecord == null)
		//										{
		//											ABSGame_Hits ABSHitGame = new ABSGame_Hits()
		//											{
		//												BetID = r.BetID,
		//												Number = c.Number,
		//												Class = c.Class,
		//												D4_WinB = Convert.ToDecimal(c.D4_WinB),
		//												CreatedDate = currDt
		//											};
		//											db.ABSGame_Hits.Add(ABSHitGame);
		//										}
		//									}
		//								}

		//								//update back processedat to null to let gamedetail table get the latest payout
		//								var rec = db.ABSGame_Ticket.Where(x => x.BetID == r.BetID && x.ModifiedDate == null).FirstOrDefault();
		//								if (rec != null)
		//								{
		//									rec.ProcessedAt = null;
		//									rec.ModifiedDate = currDt;
		//								}

		//							}
		//							catch (DbEntityValidationException e)
		//							{
		//								foreach (var eve in e.EntityValidationErrors)
		//								{
		//									_common.LogError("ABS/GetGameRecord" + " " + DateTime.Now.ToString() + " " + string.Format("Entity of type \"{0}\" in state \"{1}\" has the following validation errors:",
		//																	eve.Entry.Entity.GetType().Name, eve.Entry.State), P_FilePrefix);
		//									foreach (var ve in eve.ValidationErrors)
		//									{
		//										_common.LogError("ABS/GetGameRecord" + " " + DateTime.Now.ToString() + " " + string.Format("- Property: \"{0}\", Error: \"{1}\"",
		//																		ve.PropertyName, ve.ErrorMessage), P_FilePrefix);
		//									}
		//								}
		//							}
		//							catch (WebException ex)
		//							{
		//								var error = new StreamReader(ex.Response.GetResponseStream()).ReadToEnd();
		//								_common.LogError("Record " + r.BetID + " " + DateTime.Now.ToString() + " " + ex.Message.ToString() + " " + error, P_FilePrefix);
		//							}
		//							catch (Exception ex)
		//							{
		//								_common.LogError("Record " + r.BetID + " " + DateTime.Now.ToString() + " " + ex.Message.ToString(), P_FilePrefix);
		//							}
		//						}
		//						db.SaveChanges();
		//					}

		//					//update those not hit prize record even though no hit prize
		//					var drawdate = Convert.ToDateTime(dateFrom);
		//					var rest = db.ABSGame_Ticket.Where(x => x.ModifiedDate == null && x.DateCreate == drawdate).ToList();
		//					db.ABSGame_Ticket.Where(x => x.ModifiedDate == null && x.DateCreate == drawdate).ToList().ForEach(q =>
		//					{
		//						q.ModifiedDate = currDt;
		//					});
		//					db.SaveChanges();
		//				}
		//			}
		//			bool isUpdJob = UpdJobTime(startTime, endTime);
		//		}

		//		return true;
		//	}
		//	catch (DbEntityValidationException e)
		//	{
		//		foreach (var eve in e.EntityValidationErrors)
		//		{
		//			_common.LogError("ABS/GetGameRecord" + " " + DateTime.Now.ToString() + " " + string.Format("Entity of type \"{0}\" in state \"{1}\" has the following validation errors:",
		//											eve.Entry.Entity.GetType().Name, eve.Entry.State), P_FilePrefix);
		//			foreach (var ve in eve.ValidationErrors)
		//			{
		//				_common.LogError("ABS/GetGameRecord" + " " + DateTime.Now.ToString() + " " + string.Format("- Property: \"{0}\", Error: \"{1}\"",
		//												ve.PropertyName, ve.ErrorMessage), P_FilePrefix);
		//			}
		//		}
		//		return false;
		//	}
		//	catch (WebException ex)
		//	{
		//		var responseText = new StreamReader(ex.Response.GetResponseStream()).ReadToEnd();
		//		_common.LogError("ABS/GetGameRecord" + " " + DateTime.Now.ToString() + " " + ex.Message.ToString() + " " + responseText, P_FilePrefix);
		//		return false;
		//	}
		//	catch (Exception ex)
		//	{
		//		_common.LogError("ABS/GetGameRecord" + " " + DateTime.Now.ToString() + " " + ex.Message.ToString(), P_FilePrefix);
		//		return false;
		//	}
		//}

		public bool GetHitGameRecord()
		{
			string startTime = GetJobTime("Start");
			string endTime = GetJobTime("End");

			DateTime currDt = DateTime.Now;
			List<ABSGame_Bet> betArr = new List<ABSGame_Bet>();
			List<ABSGame_Hits> hitArr = new List<ABSGame_Hits>();

			try
			{
				string dateFrom = DateTime.Now.ToString("yyyy-MM-dd");
				//string dateFrom = "2023-07-16";

				ABS.Model.HitRecordReportResponse result = api.RetrieveHitTransactionByMinute(dateFrom);

				using (BingoConn db = new BingoConn())
				{
					string sMainID = Guid.NewGuid().ToString();
					ABSGameMain ABSMain = new ABSGameMain()
					{
						Id = sMainID,
						Total = null,
						StartTime = Convert.ToDateTime(startTime),
						EndTime = Convert.ToDateTime(endTime),
						CreatedDate = currDt
					};
					db.ABSGameMains.Add(ABSMain);
					db.SaveChanges();

					if (result != null)
					{
						if (result.Error == 0)
						{
							if (result.Record != null)
							{
								var rType = result.Record.GetType();

								if (rType.Name == "JObject")
								{
									string aa = JsonConvert.SerializeObject(result.Record);
									JObject converted = JsonConvert.DeserializeObject<JObject>(aa);

									foreach (KeyValuePair<string, JToken> keyValuePair in converted)
									{
										dynamic r = JObject.Parse(keyValuePair.Value.ToString());

										try
										{
											string betId = Convert.ToString(r.BetID);
											var summrecord = db.ABSGame_Bet.Where(x => x.BetID == betId).FirstOrDefault();
											if (summrecord == null)
											{
												ABSGame_Bet ABSBetGame = new ABSGame_Bet()
												{
													fId = sMainID,
													BetID = r.BetID,
													DrawDate = DateTime.ParseExact(Convert.ToString(r.DrawDate), "dd-MM-yyyy", CultureInfo.InvariantCulture),
													totalBet = r.totalBet,
													totalWin = Convert.ToDecimal(r.totalWin),
													user = r.user,
													Currency = r.playerCurrency,
													CreatedDate = currDt
												};
												db.ABSGame_Bet.Add(ABSBetGame);
												db.SaveChanges();
											}

											//detail
											if (r.Hits.Count > 0)
											{
												foreach (var c in r.Hits)
												{
													string num = Convert.ToString(c.Number);
													string cl = Convert.ToString(c.Class);

													var detailrecord = db.ABSGame_Hits.Where(x => x.BetID == betId && x.Number == num && x.Class == cl).FirstOrDefault();
													if (detailrecord == null)
													{
														ABSGame_Hits ABSHitGame = new ABSGame_Hits()
														{
															BetID = r.BetID,
															Number = c.Number,
															Class = c.Class,
															D4_WinB = Convert.ToDecimal(c.D4_WinB),
															CreatedDate = currDt
														};
														db.ABSGame_Hits.Add(ABSHitGame);
														db.SaveChanges();
													}

												}
											}

											//update back processedat to null to let gamedetail table get the latest payout
											var rec = db.ABSGame_Ticket.Where(x => x.BetID == betId).FirstOrDefault();
											if (rec != null)
											{
												rec.ProcessedAt = null;
												//rec.ModifiedDate = currDt;
											}
										}
										catch (DbEntityValidationException e)
										{
											foreach (var eve in e.EntityValidationErrors)
											{
												_common.LogError("ABS/GetHitGameRecord" + " " + DateTime.Now.ToString() + " " + string.Format("Entity of type \"{0}\" in state \"{1}\" has the following validation errors:",
																				eve.Entry.Entity.GetType().Name, eve.Entry.State), P_FilePrefix);
												foreach (var ve in eve.ValidationErrors)
												{
													_common.LogError("ABS/GetHitGameRecord" + " " + DateTime.Now.ToString() + " " + string.Format("- Property: \"{0}\", Error: \"{1}\"",
																					ve.PropertyName, ve.ErrorMessage), P_FilePrefix);
												}
											}
										}
										catch (WebException ex)
										{
											var error = new StreamReader(ex.Response.GetResponseStream()).ReadToEnd();
											_common.LogError("Record " + r.BetID + " " + DateTime.Now.ToString() + " " + ex.InnerException.Message.ToString() + " " + error, P_FilePrefix);
										}
										catch (Exception ex)
										{
											_common.LogError("Record " + r.BetID + " " + DateTime.Now.ToString() + " " + ex.InnerException.Message.ToString(), P_FilePrefix);
										}
									}
									//db.SaveChanges();
								}
							}
						}
					}
					bool isUpdJob = UpdJobTime(startTime, endTime);
				}

				return true;
			}
			catch (DbEntityValidationException e)
			{
				foreach (var eve in e.EntityValidationErrors)
				{
					_common.LogError("ABS/GetHitGameRecord" + " " + DateTime.Now.ToString() + " " + string.Format("Entity of type \"{0}\" in state \"{1}\" has the following validation errors:",
													eve.Entry.Entity.GetType().Name, eve.Entry.State), P_FilePrefix);
					foreach (var ve in eve.ValidationErrors)
					{
						_common.LogError("ABS/GetHitGameRecord" + " " + DateTime.Now.ToString() + " " + string.Format("- Property: \"{0}\", Error: \"{1}\"",
														ve.PropertyName, ve.ErrorMessage), P_FilePrefix);
					}
				}
				return false;
			}
			catch (WebException ex)
			{
				var responseText = new StreamReader(ex.Response.GetResponseStream()).ReadToEnd();
				_common.LogError("ABS/GetHitGameRecord" + " " + DateTime.Now.ToString() + " " + ex.InnerException.InnerException.Message.ToString() + " " + responseText, P_FilePrefix);
				return false;
			}
			catch (Exception ex)
			{
				_common.LogError("ABS/GetHitGameRecord" + " " + DateTime.Now.ToString() + " " + ex.InnerException.InnerException.Message.ToString(), P_FilePrefix);
				return false;
			}
		}

		public DateTime FromUnixTime(long unixTime)
		{
			var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
			return epoch.AddSeconds(unixTime);
		}
	}
}