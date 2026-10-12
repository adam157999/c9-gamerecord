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
	public class PragmaticSlotController : Controller
	{
		public static readonly object MyLock = new object();
		private string P_GamePrefix = "PRAGMATICSLOT";
		private string P_FilePrefix = "PRAGMATICSLOT";
		private string P_DTFormat = "yyyy-MM-ddTHH:mm:ss.fffZ";
		private string gameImgUrl = string.Empty;
		Common _common = new Common();
		private PragmaticSlotConnector api = new PragmaticSlotConnector(Environment: BaseConnector.GameEnvironment.PROD);

		public bool UpdJobTime(string startTime, string endTime, string sMainID, string lastVersionKey)
		{
			try
			{
				DateTime dtEnd = Convert.ToDateTime(endTime);

				_common.UpdateADMJobDate(P_GamePrefix, dtEnd, lastVersionKey);

				//using (var bingoConn = new BingoConn())
				//{
				//    var xyz = bingoConn.ADMJobDates.Where(s => s.GameProvider == P_GamePrefix).FirstOrDefault();
				//    if (xyz != null)
				//    {
				//        xyz.LastRunTillDate = dtEnd;
				//        xyz.LastVersionKey = lastVersionKey;

				//        //bingoConn.Entry(xyz).State = System.Data.Entity.EntityState.Modified;
				//        bingoConn.SaveChanges();
				//    }
				//}
				return true;
			}
			catch (Exception ex)
			{
				_common.LogError("PragmaticSlot/UpdJobTime" + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), P_FilePrefix);
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
				_common.LogError("PragmaticSlot/GetLastVersionKey" + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), P_FilePrefix);
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
				_common.LogError("PragmaticSlot/GetJobTime" + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), P_FilePrefix);
				return "Error:" + ex.Message.ToString();
			}
		}

		public void PragmaticSlotJob()
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
				_common.LogError("Start - " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), P_FilePrefix);
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
					PragmaticSlotGameMain pragmaticMain = new PragmaticSlotGameMain()
					{
						Id = sMainID,
						Total = null,
						StartTime = Convert.ToDateTime(startTime),
						EndTime = Convert.ToDateTime(endTime),
						CreatedDate = DateTime.Now
					};
					db.PragmaticSlotGameMains.Add(pragmaticMain);
					db.SaveChanges();

					timeSpan = Convert.ToString(result.timepoint);

					if (result.rounds.Count > 0)
					{
						foreach (var b in result.rounds)
						{
							try
							{
								PragmaticSlotGame pragmaticGame = new PragmaticSlotGame()
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
								db.PragmaticSlotGames.AddOrUpdate(pragmaticGame);
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

		//lobby code here
		public ActionResult Lobby()
		{
			try
			{
				GetCasinoGamesResponseV2 model = api.GetGamesList();
				List<GameList> gameList = GetGameList("", "");
				//bool insertFlag = SaveGameListToDatabase(model.gameList.Where(x=>x.gameTypeID=="vs").ToList());
				Session["gameImgUrl"] = model.imgUrl;
				if (gameList.Count > 0)
				{
					model.gameList = gameList.ToList();
				}
				return View(model);
			}
			catch (Exception ex)
			{
				return View();
			}
		}


		public JsonResult SearchGame(string gameName, string gameCategory = "")
		{
			GetCasinoGamesResponseV2 model = new GetCasinoGamesResponseV2();
			List<GameList> gameList = GetGameList(gameName, gameCategory.ToUpper());
			model.gameList = gameList;
			model.imgUrl = Session["gameImgUrl"] as string;
			if (model.gameList != null)
			{
				model.gameList = model.gameList.ToList();
			}
			return Json(model, JsonRequestBehavior.AllowGet);
		}

		public string GetBalance(string username)
		{
			lock (MyLock)
			{
				try
				{
					_common.LogError("GetBalanceLobby" + " " + DateTime.Now.ToString() + " " + string.Format("{0}", username), P_FilePrefix);
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
			lock (MyLock)
			{
				try
				{
					dynamic res = api.GetSlotGameUrl(username, langid, gameid);

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

		private List<GameList> GetGameList(string gameName = "", string gameCategory = "")
		{
			using (BingoConn db = new BingoConn())
			{
				try
				{
					List<PragmaticSlotGameList> foundGameList = new List<PragmaticSlotGameList>();
					List<GameList> gameList = new List<GameList>();
					switch (gameCategory.ToUpper())
					{
						case "HOT":
							foundGameList = db.PragmaticSlotGameLists.Where(x => x.GameName.Contains(gameName)
									&& x.IsHot == true).OrderByDescending(x => x.HotSeq).ToList();
							break;
						case "FAVOURITE":
							foundGameList = db.PragmaticSlotGameLists.Where(x => x.GameName.Contains(gameName)
									 && x.IsFavorite == true).OrderByDescending(x => x.FavoriteSeq).ToList();
							break;
						case "NEW":
							foundGameList = db.PragmaticSlotGameLists.Where(x => x.GameName.Contains(gameName)
									 && x.IsNew == true).OrderByDescending(x => x.NewSeq).ToList();
							break;
						case "ALL":
							foundGameList = db.PragmaticSlotGameLists.Where(x => x.GameName.Contains(gameName)
									 && x.IsAll == true).OrderByDescending(x => x.AllSeq).ToList();
							break;
						default:
							foundGameList = db.PragmaticSlotGameLists.Where(x => x.GameName.Contains(gameName)
									 && x.IsAll == true).ToList();
							foundGameList = foundGameList.OrderBy(x => x.AllSeq).ToList();
							break;
					}

					for (int index = 0; index < foundGameList.Count(); index++)
					{
						GameList game = new GameList();
						game.aspectRatio = foundGameList[index].AspectRatio;
						game.gameID = foundGameList[index].GameID;
						game.gameIDNumeric = (long)foundGameList[index].GameIdNumeric;
						game.gameName = foundGameList[index].GameName;
						game.gameTypeID = foundGameList[index].GameTypeID;
						game.typeDescription = foundGameList[index].TypeDescription;
						game.technology = foundGameList[index].Technology;
						game.technologyID = foundGameList[index].TechnologyID;
						game.platform = foundGameList[index].Platform;
						game.demoGameAvailable = foundGameList[index].DemoGameAvailable;
						gameList.Add(game);
					}
					return gameList;
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
					throw;
				}
				catch (Exception ex)
				{
					_common.LogError("Record " + " " + DateTime.Now.ToString() + " " + ex.Message.ToString(), P_FilePrefix);
					throw;
				}
			}
		}

		private bool SaveGameListToDatabase(List<GameList> model)
		{
			bool flag = false;
			using (BingoConn db = new BingoConn())
			{
				try
				{
					if (model.Count == db.PragmaticSlotGameLists.Count()) { flag = true; return flag; }
					for (int index = 0; index < model.Count; index++)
					{
						PragmaticSlotGameList game = new PragmaticSlotGameList()
						{
							GameID = model[index].gameID,
							GameName = model[index].gameName,
							GameTypeID = model[index].gameTypeID,
							TypeDescription = model[index].typeDescription,
							Technology = model[index].technology,
							TechnologyID = model[index].technologyID,
							Platform = model[index].platform,
							AspectRatio = model[index].aspectRatio,
							GameIdNumeric = model[index].gameIDNumeric,
							DemoGameAvailable = model[index].demoGameAvailable,
							IsAll = true,
							CreatedDate = DateTime.Now,
							ModifiedDate = DateTime.Now

						};
						db.PragmaticSlotGameLists.Add(game);
						db.SaveChanges();
						flag = true;
					}
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
					_common.LogError("Record " + " " + DateTime.Now.ToString() + " " + ex.Message.ToString() + " " + error, P_FilePrefix);
				}
				catch (Exception ex)
				{
					_common.LogError("Record " + " " + DateTime.Now.ToString() + " " + ex.Message.ToString(), P_FilePrefix);
				}
			}
			return flag;
		}
	}
}