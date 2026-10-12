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
using System.Web.Script.Serialization;
using System.Dynamic;

namespace GameRecord.Controllers
{
	public class MKY888Controller : Controller
	{
		public static readonly object MyLock = new object();
		private string P_GamePrefix = "MKY888";

		private string P_FilePrefix = "MKY888";
		private string P_DTFormat = "yyyy-MM-ddTHH:mm:ss.fffZ";
		Common _common = new Common();
		string nextId = string.Empty;
		private MKY888Connector api = new MKY888Connector(Environment: BaseConnector.GameEnvironment.PROD);

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
				_common.LogError("MKY888/UpdJobTime" + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), P_FilePrefix);
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
				_common.LogError("MKY888/GetJobTime" + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), P_FilePrefix);
				return "Error:" + ex.Message.ToString();
			}
		}
		
		//http://localhost:51148/MKY888/MKY888Job
		public void MKY888Job()
		{
			try
			{
				lock (MyLock)
				{
					//_common.LogError("Job Start - " + DateTime.Now.ToString(), P_FilePrefix);
					bool isOK = GetGameRecord();
					//_common.LogError("Job End - " + DateTime.Now.ToString(), P_FilePrefix);
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

					MKY888.Models.RetrieveTransactionByMinuteResponse result = api.RetrieveTransactionByMinute(Convert.ToDateTime(mStartTime), Convert.ToDateTime(mEndTime), nextId);

					using (BingoConn db = new BingoConn())
					{
						string sMainID = Guid.NewGuid().ToString();
						Monkey888GameMain mkyMain = new Monkey888GameMain()
						{
							Id = sMainID,
							Total = null,
							StartTime = Convert.ToDateTime(startTime),
							EndTime = Convert.ToDateTime(endTime),
							CreatedDate = DateTime.Now
						};
						db.Monkey888GameMain.Add(mkyMain);
						db.SaveChanges();

						if (result.data != null)
						{
							if (result.data.Game != null)
							{
								if (result.data.Game.Count > 0)
								{
									foreach (var b in result.data.Game)
									{
										string id = b.OCode;
										try
										{
											Monkey888Game mkyGame = new Monkey888Game()
											{
												fId = sMainID,
												OCode = b.OCode,
												Username = b.Username,
												GameCode = b.GameCode,
												Description = b.Description,
												Type = b.Type,
												Amount = b.Amount,
												Rollover = _common.GetSlotRollover(Math.Abs(b.Amount), b.Result - Math.Abs(b.Amount)),
												Result = b.Result,
												Time = b.Time
												//CreatedDate = DateTime.Now
											};
											var record = db.Monkey888Game.Where(x => x.OCode == b.OCode).FirstOrDefault();

											if (record == null)
											{
												mkyGame.CreatedDate = DateTime.Now;
											}
											else
											{
												mkyGame.CreatedDate = record.CreatedDate;
												mkyGame.ModifiedDate = DateTime.Now;
											}
											db.Monkey888Game.AddOrUpdate(mkyGame);
										}
										catch (DbEntityValidationException e)
										{
											foreach (var eve in e.EntityValidationErrors)
											{
												_common.LogError("MKY888/GetGameRecord" + " " + DateTime.Now.ToString() + " " + string.Format("Entity of type \"{0}\" in state \"{1}\" has the following validation errors:",
																eve.Entry.Entity.GetType().Name, eve.Entry.State), P_FilePrefix);
												foreach (var ve in eve.ValidationErrors)
												{
													_common.LogError("MKY888/GetGameRecord" + " " + DateTime.Now.ToString() + " " + string.Format("- Property: \"{0}\", Error: \"{1}\"",
																	ve.PropertyName, ve.ErrorMessage), P_FilePrefix);
												}
											}
										}
										catch (WebException ex)
										{
											var error = new StreamReader(ex.Response.GetResponseStream()).ReadToEnd();
											_common.LogError("Record " + id + " " + DateTime.Now.ToString() + " " + ex.Message.ToString() + " " + error, P_FilePrefix);
										}
										catch (Exception ex)
										{
											_common.LogError("Record " + id + " " + DateTime.Now.ToString() + " " + ex.Message.ToString(), P_FilePrefix);
										}
									}
									db.SaveChanges();

								}
							}

							if (result.data.Jackpot != null)
							{
								if (result.data.Jackpot.Count > 0)
								{
									foreach (var b in result.data.Jackpot)
									{
										string id = b.OCode;
										try
										{
											Monkey888Game mkyGame = new Monkey888Game()
											{
												fId = sMainID,
												OCode = b.OCode,
												Username = b.Username,
												GameCode = b.GameCode,
												Description = b.Description,
												Type = b.Type,
												Amount = b.Amount,
												Rollover = _common.GetSlotRollover(Math.Abs(b.Amount), b.Result - Math.Abs(b.Amount)),
												Result = b.Result,
												Time = b.Time
												//CreatedDate = DateTime.Now
											};
											var record = db.Monkey888Game.Where(x => x.OCode == b.OCode).FirstOrDefault();

											if (record == null)
											{
												mkyGame.CreatedDate = DateTime.Now;
											}
											else
											{
												mkyGame.CreatedDate = record.CreatedDate;
												mkyGame.ModifiedDate = DateTime.Now;
											}
											db.Monkey888Game.AddOrUpdate(mkyGame);
										}
										catch (DbEntityValidationException e)
										{
											foreach (var eve in e.EntityValidationErrors)
											{
												_common.LogError("MKY888/GetGameRecord" + " " + DateTime.Now.ToString() + " " + string.Format("Entity of type \"{0}\" in state \"{1}\" has the following validation errors:",
																eve.Entry.Entity.GetType().Name, eve.Entry.State), P_FilePrefix);
												foreach (var ve in eve.ValidationErrors)
												{
													_common.LogError("MKY888/GetGameRecord" + " " + DateTime.Now.ToString() + " " + string.Format("- Property: \"{0}\", Error: \"{1}\"",
																	ve.PropertyName, ve.ErrorMessage), P_FilePrefix);
												}
											}
										}
										catch (WebException ex)
										{
											var error = new StreamReader(ex.Response.GetResponseStream()).ReadToEnd();
											_common.LogError("Record " + id + " " + DateTime.Now.ToString() + " " + ex.Message.ToString() + " " + error, P_FilePrefix);
										}
										catch (Exception ex)
										{
											_common.LogError("Record " + id + " " + DateTime.Now.ToString() + " " + ex.Message.ToString(), P_FilePrefix);
										}
									}
									db.SaveChanges();

								}
							}
						}
						nextId = result.nextId;

						while (!string.IsNullOrEmpty(nextId))
						{
							GetGameRecordExtend(mStartTime, mEndTime, sMainID);
						}

						bool isUpdJob = UpdJobTime(mStartTime, mEndTime, sMainID);
					}
				}

				return true;
			}
			catch (DbEntityValidationException e)
			{
				foreach (var eve in e.EntityValidationErrors)
				{
					_common.LogError("MKY888/GetGameRecord" + " " + DateTime.Now.ToString() + " " + string.Format("Entity of type \"{0}\" in state \"{1}\" has the following validation errors:",
									eve.Entry.Entity.GetType().Name, eve.Entry.State), P_FilePrefix);
					foreach (var ve in eve.ValidationErrors)
					{
						_common.LogError("MKY888/GetGameRecord" + " " + DateTime.Now.ToString() + " " + string.Format("- Property: \"{0}\", Error: \"{1}\"",
										ve.PropertyName, ve.ErrorMessage), P_FilePrefix);
					}
				}
				return false;
			}
			catch (WebException ex)
			{
				var responseText = new StreamReader(ex.Response.GetResponseStream()).ReadToEnd();
				_common.LogError("MKY888/GetGameRecord" + " " + DateTime.Now.ToString() + " " + ex.Message.ToString() + " " + responseText, P_FilePrefix);
				return false;
			}
			catch (Exception ex)
			{
				_common.LogError("MKY888/GetGameRecord" + " " + DateTime.Now.ToString() + " " + ex.Message.ToString(), P_FilePrefix);
				return false;
			}
		}

		public bool GetGameRecordExtend(string startTime, string endTime, string sMainID)
		{
			try
			{
				MKY888.Models.RetrieveTransactionByMinuteResponse result = api.RetrieveTransactionByMinute(Convert.ToDateTime(startTime), Convert.ToDateTime(endTime), nextId);

				using (BingoConn db = new BingoConn())
				{
					if (result.data != null)
					{
						if (result.data.Game != null)
						{
							if (result.data.Game.Count > 0)
							{
								foreach (var b in result.data.Game)
								{
									string id = b.OCode;
									try
									{
										Monkey888Game mkyGame = new Monkey888Game()
										{
											fId = sMainID,
											OCode = b.OCode,
											Username = b.Username,
											GameCode = b.GameCode,
											Description = b.Description,
											Type = b.Type,
											Amount = b.Amount,
											Rollover = _common.GetSlotRollover(Math.Abs(b.Amount), b.Result - Math.Abs(b.Amount)),
											Result = b.Result,
											Time = b.Time
											//CreatedDate = DateTime.Now
										};
										var record = db.Monkey888Game.Where(x => x.OCode == b.OCode).FirstOrDefault();

										if (record == null)
										{
											mkyGame.CreatedDate = DateTime.Now;
										}
										else
										{
											mkyGame.CreatedDate = record.CreatedDate;
											mkyGame.ModifiedDate = DateTime.Now;
										}
										db.Monkey888Game.AddOrUpdate(mkyGame);
									}
									catch (DbEntityValidationException e)
									{
										foreach (var eve in e.EntityValidationErrors)
										{
											_common.LogError("MKY888/GetGameRecord" + " " + DateTime.Now.ToString() + " " + string.Format("Entity of type \"{0}\" in state \"{1}\" has the following validation errors:",
															eve.Entry.Entity.GetType().Name, eve.Entry.State), P_FilePrefix);
											foreach (var ve in eve.ValidationErrors)
											{
												_common.LogError("MKY888/GetGameRecord" + " " + DateTime.Now.ToString() + " " + string.Format("- Property: \"{0}\", Error: \"{1}\"",
																ve.PropertyName, ve.ErrorMessage), P_FilePrefix);
											}
										}
									}
									catch (WebException ex)
									{
										var error = new StreamReader(ex.Response.GetResponseStream()).ReadToEnd();
										_common.LogError("Record " + id + " " + DateTime.Now.ToString() + " " + ex.Message.ToString() + " " + error, P_FilePrefix);
									}
									catch (Exception ex)
									{
										_common.LogError("Record " + id + " " + DateTime.Now.ToString() + " " + ex.Message.ToString(), P_FilePrefix);
									}
								}
								db.SaveChanges();

							}
						}

						if (result.data.Jackpot != null)
						{
							if (result.data.Jackpot.Count > 0)
							{
								foreach (var b in result.data.Jackpot)
								{
									string id = b.OCode;
									try
									{
										Monkey888Game mkyGame = new Monkey888Game()
										{
											fId = sMainID,
											OCode = b.OCode,
											Username = b.Username,
											GameCode = b.GameCode,
											Description = b.Description,
											Type = b.Type,
											Amount = b.Amount,
											Rollover = _common.GetSlotRollover(Math.Abs(b.Amount), b.Result - Math.Abs(b.Amount)),
											Result = b.Result,
											Time = b.Time
											//CreatedDate = DateTime.Now
										};
										var record = db.Monkey888Game.Where(x => x.OCode == b.OCode).FirstOrDefault();

										if (record == null)
										{
											mkyGame.CreatedDate = DateTime.Now;
										}
										else
										{
											mkyGame.CreatedDate = record.CreatedDate;
											mkyGame.ModifiedDate = DateTime.Now;
										}
										db.Monkey888Game.AddOrUpdate(mkyGame);
									}
									catch (DbEntityValidationException e)
									{
										foreach (var eve in e.EntityValidationErrors)
										{
											_common.LogError("MKY888/GetGameRecord" + " " + DateTime.Now.ToString() + " " + string.Format("Entity of type \"{0}\" in state \"{1}\" has the following validation errors:",
															eve.Entry.Entity.GetType().Name, eve.Entry.State), P_FilePrefix);
											foreach (var ve in eve.ValidationErrors)
											{
												_common.LogError("MKY888/GetGameRecord" + " " + DateTime.Now.ToString() + " " + string.Format("- Property: \"{0}\", Error: \"{1}\"",
																ve.PropertyName, ve.ErrorMessage), P_FilePrefix);
											}
										}
									}
									catch (WebException ex)
									{
										var error = new StreamReader(ex.Response.GetResponseStream()).ReadToEnd();
										_common.LogError("Record " + id + " " + DateTime.Now.ToString() + " " + ex.Message.ToString() + " " + error, P_FilePrefix);
									}
									catch (Exception ex)
									{
										_common.LogError("Record " + id + " " + DateTime.Now.ToString() + " " + ex.Message.ToString(), P_FilePrefix);
									}
								}
								db.SaveChanges();

							}
						}
					}

					nextId = result.nextId;
				}

				return true;
			}
			catch (DbEntityValidationException e)
			{
				foreach (var eve in e.EntityValidationErrors)
				{
					_common.LogError("MKY888/GetGameRecord" + " " + DateTime.Now.ToString() + " " + string.Format("Entity of type \"{0}\" in state \"{1}\" has the following validation errors:",
									eve.Entry.Entity.GetType().Name, eve.Entry.State), P_FilePrefix);
					foreach (var ve in eve.ValidationErrors)
					{
						_common.LogError("MKY888/GetGameRecord" + " " + DateTime.Now.ToString() + " " + string.Format("- Property: \"{0}\", Error: \"{1}\"",
										ve.PropertyName, ve.ErrorMessage), P_FilePrefix);
					}
				}
				return false;
			}
			catch (WebException ex)
			{
				var responseText = new StreamReader(ex.Response.GetResponseStream()).ReadToEnd();
				_common.LogError("MKY888/GetGameRecord" + " " + DateTime.Now.ToString() + " " + ex.Message.ToString() + " " + responseText, P_FilePrefix);
				return false;
			}
			catch (Exception ex)
			{
				_common.LogError("MKY888/GetGameRecord" + " " + DateTime.Now.ToString() + " " + ex.Message.ToString(), P_FilePrefix);
				return false;
			}
		}

		//lobby code here
		public ActionResult Lobby()
		{
			try
			{
				MKY888.Models.ListGamesResponse model = api.GetGamesList();
				Session["MKY888GameList"] = model.ListGames;
				model.ListGames = model.ListGames.OrderBy(x => x.GameName).ToList();
				return View(model);
			}
			catch (Exception ex)
			{
				return View();
			}
		}

		public JsonResult SearchGame(string gameName)
		{
			MKY888.Models.ListGamesResponse model = new MKY888.Models.ListGamesResponse();
			if (Session["MKY888GameList"] == null)
			{
				model = api.GetGamesList();
			}
			else
			{
				var listGame = Session["MKY888GameList"];
				model.ListGames = (List<MKY888.Models.Game>)listGame;
			}
			if (model.ListGames != null)
			{
				model.ListGames = model.ListGames.Where(x => x.GameName.ToUpper().Contains(gameName.ToUpper()))
					.OrderByDescending(x => x.GameName).ToList();
			}
			return Json(model, JsonRequestBehavior.AllowGet);
		}

		public string GetBalance(string username)
		{
			lock (MyLock)
			{
				try
				{
					//_common.LogError("GetBalanceLobby" + " " + DateTime.Now.ToString() + " " + string.Format("{0}", username), P_FilePrefix);

					dynamic result = api.GetBalanceLobby(username);

					JsonResult json = new JsonResult
					{
						Data = JsonConvert.SerializeObject(result)
					};

					return new JavaScriptSerializer().Serialize(json);
				}
				catch (Exception ex)
				{
					_common.LogError("GetBalance" + " " + DateTime.Now.ToString() + " " + string.Format("{0}", username) + " " + ex.Message.ToString(), P_FilePrefix);
					return null;
				}
			}
		}

		public string LaunchDirectGame(string username, string gameid, string langid)
		{
			//_common.InsertLog("LaunchDirectGame", "username", username, "gameid", "gameid", "langid", langid);

			lock (MyLock)
			{
				try
				{
					dynamic res = api.GetSlotGameUrl(username, langid, gameid);

					//_common.InsertLog(gameid, "LaunchDirectGame", "request", api.request, "response", api.response);

					dynamic result = new ExpandoObject();
					result.ErrorCode = 0;
					result.url = res.Url;

					JsonResult json = new JsonResult
					{
						Data = JsonConvert.SerializeObject(result)
					};

					return new JavaScriptSerializer().Serialize(json);
				}
				catch (Exception ex)
				{
					_common.LogError("LaunchDirectGame" + " " + DateTime.Now.ToString() + " " + string.Format("{0}|{1}", gameid, langid) + " " + ex.Message.ToString(), P_FilePrefix);
					return null;
				}
			}
		}
	}
}