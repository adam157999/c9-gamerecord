using System;
using System.Linq;
using System.Web.Mvc;
using System.Net;
using System.IO;
using Newtonsoft.Json;
using GameRecord.Models;
using System.Data.Entity.Migrations;
using System.Data.Entity.Validation;
using System.Web.Script.Serialization;
using System.Dynamic;
using GameRecord.Gamess.GFG;
using System.Collections.Generic;

namespace GameRecord.Controllers
{
	public class GFGController : Controller
	{
		public static readonly object MyLock = new object();
		private string P_GamePrefix = "GFG";

		private string P_FilePrefix = "GFG";
		//private string P_DTFormat = "yyyy-MM-ddTHH:mm:ss.fffZ";
		Common _common = new Common();
		private GFGConnector api = new GFGConnector(Environment: BaseConnector.GameEnvironment.PROD);

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
						DateTime dtSTime = Convert.ToDateTime(DateTime.Now.ToString("yyyy-MM-dd HH:mm"));
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

		public void GFGJob()
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
			int page = 0; //0 is first page
			int pageSize = 2;
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

					TakeBetLogsResponse result = api.RetrieveTransactionByTime(Convert.ToDateTime(mStartTime), Convert.ToDateTime(mEndTime), page, pageSize);

					using (BingoConn db = new BingoConn())
					{
						GFGGameMain GFGMain = new GFGGameMain()
						{
							Id = sMainID,
							Total = null,
							StartTime = Convert.ToDateTime(startTime),
							EndTime = Convert.ToDateTime(endTime),
							CreatedDate = DateTime.Now
						};
						db.GFGGameMains.Add(GFGMain);
						db.SaveChanges();

						if (result.code == 0)
						{
							if (result.data.bets.Count > 0)
							{
								foreach (var b in result.data.bets)
								{
									try
									{
										GFGGame GFGGame = new GFGGame()
										{
											fId = sMainID,
											GameId = b.gameId,
											Account = b.account,
											AccountId = b.accountId,
											Platform = b.platform,
											RoundId = b.roundId,
											GameResult = b.gameResult,
											//FieldId = b.fieldId,
											//FieldName = b.fieldName,
											//TableId = b.tableId,
											//Chair = b.chair,
											FieldId = 0,
											FieldName = "",
											TableId = 0,
											Chair = 0,
											Bet = b.bet,
											ValidBet = b.validBet,
											Win = b.win,
											Lose = b.lose,
											Fee = b.fee,
											EnterMoney = b.enterMoney,
											Ip = b.ip,
											CreateTime = b.createTime,
											RoundBeginTime = b.roundBeginTime,
											RoundEndTime = b.roundEndTime,
											CreatedDate = DateTime.Now
										};
										db.GFGGames.AddOrUpdate(GFGGame);
									}
									catch (DbEntityValidationException e)
									{
										foreach (var eve in e.EntityValidationErrors)
										{
											_common.LogError("GFG/GetGameRecord" + " " + DateTime.Now.ToString() + " " + string.Format("Entity of type \"{0}\" in state \"{1}\" has the following validation errors:",
															eve.Entry.Entity.GetType().Name, eve.Entry.State), P_FilePrefix);
											foreach (var ve in eve.ValidationErrors)
											{
												_common.LogError("GFG/GetGameRecord" + " " + DateTime.Now.ToString() + " " + string.Format("- Property: \"{0}\", Error: \"{1}\"",
																ve.PropertyName, ve.ErrorMessage), P_FilePrefix);
											}
										}
									}
									catch (WebException ex)
									{
										var error = new StreamReader(ex.Response.GetResponseStream()).ReadToEnd();
										_common.LogError("Record " + b.gameId + " " + DateTime.Now.ToString() + " " + ex.Message.ToString() + " " + error, P_FilePrefix);
									}
									catch (Exception ex)
									{
										_common.LogError("Record " + b.gameId + " " + DateTime.Now.ToString() + " " + ex.Message.ToString(), P_FilePrefix);
									}
								}
								db.SaveChanges();

								decimal dCnt = Convert.ToDecimal(result.data.total) / Convert.ToDecimal(pageSize);
								int iPageIndex = Int32.Parse(Math.Ceiling(dCnt).ToString());

								if (iPageIndex > 1)
								{
									for (int x = 2; x <= iPageIndex; x++)
									{
										bool IsOK = GetGameRecordExtend(x - 1, pageSize, mStartTime, mEndTime, sMainID);
									}
								}

							}
						}

						if (result.code == 0)
						{
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
					_common.LogError("GFG/GetGameRecord" + " " + DateTime.Now.ToString() + " " + string.Format("Entity of type \"{0}\" in state \"{1}\" has the following validation errors:",
									eve.Entry.Entity.GetType().Name, eve.Entry.State), P_FilePrefix);
					foreach (var ve in eve.ValidationErrors)
					{
						_common.LogError("GFG/GetGameRecord" + " " + DateTime.Now.ToString() + " " + string.Format("- Property: \"{0}\", Error: \"{1}\"",
										ve.PropertyName, ve.ErrorMessage), P_FilePrefix);
					}
				}
				return false;
			}
			catch (WebException ex)
			{
				var responseText = new StreamReader(ex.Response.GetResponseStream()).ReadToEnd();
				_common.LogError("GFG/GetGameRecord" + " " + DateTime.Now.ToString() + " " + ex.Message.ToString() + " " + responseText, P_FilePrefix);
				return false;
			}
			catch (Exception ex)
			{
				_common.LogError("GFG/GetGameRecord" + " " + DateTime.Now.ToString() + " " + ex.Message.ToString(), P_FilePrefix);
				return false;
			}
		}

		public bool GetGameRecordExtend(int pageIndex, int pageLimit, string startTime, string endTime, string sMainID)
		{
			try
			{
				TakeBetLogsResponse result = api.RetrieveTransactionByTime(Convert.ToDateTime(startTime), Convert.ToDateTime(endTime), pageIndex, pageLimit);

				using (BingoConn db = new BingoConn())
				{
					if (result.code == 0)
					{
						if (result.data.bets.Count > 0)
						{
							foreach (var b in result.data.bets)
							{
								try
								{
									GFGGame GFGGame = new GFGGame()
									{
										fId = sMainID,
										GameId = b.gameId,
										Account = b.account,
										AccountId = b.accountId,
										Platform = b.platform,
										RoundId = b.roundId,
										GameResult = b.gameResult,
										//FieldId = b.fieldId,
										//FieldName = b.fieldName,
										//TableId = b.tableId,
										//Chair = b.chair,
										FieldId = 0,
										FieldName = "",
										TableId = 0,
										Chair = 0,
										Bet = b.bet,
										ValidBet = b.validBet,
										Win = b.win,
										Lose = b.lose,
										Fee = b.fee,
										EnterMoney = b.enterMoney,
										Ip = b.ip,
										CreateTime = b.createTime,
										RoundBeginTime = b.roundBeginTime,
										RoundEndTime = b.roundEndTime,
										CreatedDate = DateTime.Now
									};
									db.GFGGames.AddOrUpdate(GFGGame);
								}
								catch (DbEntityValidationException e)
								{
									foreach (var eve in e.EntityValidationErrors)
									{
										_common.LogError("GFG/GetGameRecord" + " " + DateTime.Now.ToString() + " " + string.Format("Entity of type \"{0}\" in state \"{1}\" has the following validation errors:",
														eve.Entry.Entity.GetType().Name, eve.Entry.State), P_FilePrefix);
										foreach (var ve in eve.ValidationErrors)
										{
											_common.LogError("GFG/GetGameRecord" + " " + DateTime.Now.ToString() + " " + string.Format("- Property: \"{0}\", Error: \"{1}\"",
															ve.PropertyName, ve.ErrorMessage), P_FilePrefix);
										}
									}
								}
								catch (WebException ex)
								{
									var error = new StreamReader(ex.Response.GetResponseStream()).ReadToEnd();
									_common.LogError("Record " + b.gameId + " " + DateTime.Now.ToString() + " " + ex.Message.ToString() + " " + error, P_FilePrefix);
								}
								catch (Exception ex)
								{
									_common.LogError("Record " + b.gameId + " " + DateTime.Now.ToString() + " " + ex.Message.ToString(), P_FilePrefix);
								}
							}
							db.SaveChanges();
						}
					}
				}

				return true;
			}
			catch (DbEntityValidationException e)
			{
				foreach (var eve in e.EntityValidationErrors)
				{
					_common.LogError("GFG/GetGameRecord" + " " + DateTime.Now.ToString() + " " + string.Format("Entity of type \"{0}\" in state \"{1}\" has the following validation errors:",
									eve.Entry.Entity.GetType().Name, eve.Entry.State), P_FilePrefix);
					foreach (var ve in eve.ValidationErrors)
					{
						_common.LogError("GFG/GetGameRecord" + " " + DateTime.Now.ToString() + " " + string.Format("- Property: \"{0}\", Error: \"{1}\"",
										ve.PropertyName, ve.ErrorMessage), P_FilePrefix);
					}
				}
				return false;
			}
			catch (WebException ex)
			{
				var responseText = new StreamReader(ex.Response.GetResponseStream()).ReadToEnd();
				_common.LogError("GFG/GetGameRecord" + " " + DateTime.Now.ToString() + " " + ex.Message.ToString() + " " + responseText, P_FilePrefix);
				return false;
			}
			catch (Exception ex)
			{
				_common.LogError("GFG/GetGameRecord" + " " + DateTime.Now.ToString() + " " + ex.Message.ToString(), P_FilePrefix);
				return false;
			}
		}

		//http://localhost:51148/GFG/Lobby
		//http://localhost:51148/GFG/Lobby?username=&langid=EN&currency=MYR
		//http://localhost:51148/GFG/Lobby?username=&langid=CN&currency=MYR
		public ActionResult Lobby(string username, string langid="EN", string currency="MYR")
		{
			try
			{
				using (BingoConn db = new BingoConn())
				{
					GFGLobbyVM model = new GFGLobbyVM
					{
						username = username,
						langid = langid,
						currency = currency,
						Games = new List<GFGLobbyVM_Game>(),
					};

					var gs = db.GFGLobbyGames.OrderByDescending(x => x.AllSeq).ToList();
					foreach (var g in gs)
                    {
						model.Games.Add(new GFGLobbyVM_Game {
							GameID = g.GameID,
							GameName = langid.ToUpper().Contains("EN") ? g.GameNameEn : g.GameNameCn,
							GameType = g.GameType,
							ImageUrl = langid.ToUpper().Contains("EN") ? g.Image : g.ImageCn,
						});
					}

					return View(model);
				}
			}
			catch (Exception ex)
			{
				_common.LogError("Error at GFG/Lobby: " + ex.Message.ToString(), P_GamePrefix);
				return Content("Error at GFG/Lobby: " + ex.Message.ToString());
			}
		}

		public ActionResult GetGames(string gameName, string gameCategory, string langid="EN")
		{
			try
			{
				using (BingoConn db = new BingoConn())
				{
					GFGLobbyVM model = new GFGLobbyVM
					{
						Games = new List<GFGLobbyVM_Game>(),
					};

					var q_gs = db.GFGLobbyGames.Where(x => true);

					if (!string.IsNullOrWhiteSpace(gameName))
					{
						if (langid?.ToUpper() == "EN")
							q_gs = q_gs.Where(x => x.GameNameEn.Contains(gameName));
						else
							q_gs = q_gs.Where(x => x.GameNameCn.Contains(gameName));
					}

					if (gameCategory == "Hot")
						q_gs = q_gs.Where(x => x.IsHot == true).OrderByDescending(x => x.HotSeq);
					else if (gameCategory == "Favourite")
						q_gs = q_gs.Where(x => x.IsFavorite == true).OrderByDescending(x => x.FavoriteSeq);
					else if (gameCategory == "New")
						q_gs = q_gs.Where(x => x.IsNew == true).OrderByDescending(x => x.NewSeq);
					else
						q_gs = q_gs.Where(x => x.IsAll == true).OrderByDescending(x => x.AllSeq);

					var gs = q_gs.ToList();
					foreach (var g in gs)
					{
						model.Games.Add(new GFGLobbyVM_Game
						{
							GameID = g.GameID,
							GameName = langid.ToUpper().Contains("EN") ? g.GameNameEn : g.GameNameCn,
							GameType = g.GameType,
							ImageUrl = langid.ToUpper().Contains("EN") ? g.Image : g.ImageCn,
						});
					}

					return Json(model, JsonRequestBehavior.AllowGet);
				}
			}
			catch (Exception ex)
			{
				_common.LogError("Error at GFG/GetGames: " + ex.Message.ToString(), P_GamePrefix);
				return null;
			}
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

		public string LaunchDirectGame(string username, int gameid, string langid, string currency)
		{
			//_common.InsertLog("LaunchDirectGame", "username", username, "gameid", "gameid", "langid", langid);

			lock (MyLock)
			{
				try
				{
					dynamic res = api.GetSlotGameUrl(username, langid, gameid, currency);

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