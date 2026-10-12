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
using System.Dynamic;
using Pragmatic.Model;
using System.Web.Script.Serialization;


namespace GameRecord.Controllers
{
	public class PPSlotController : Controller
	{
		public static readonly object MyLock = new object();
		private string P_GamePrefix = "PPSLOT";
		private string P_FilePrefix = "PPSLOT";
		private string P_DTFormat = "yyyy-MM-ddTHH:mm:ss.fffZ";
		private string gameImgUrl = string.Empty;
		Common _common = new Common();
		private PPSlotConnector api = new PPSlotConnector(Environment: BaseConnector.GameEnvironment.PROD);

		//lobby code here
		public ActionResult Lobby()
		{
			try
			{
				//GetCasinoGamesResponseV2 model = api.GetGamesList();
				//List<GameList> gameList = GetGameList("", "");
				//if (gameList.Count > 0)
				//{
				//	model.gameList = gameList.ToList();
				//}
				//return View(model);

				//GetCasinoGamesResponseV2 model = api.GetGamesList();
				//model.gameList = GetGameList("", "");
				//return View(model);

				ProviderLobbyVM model = new ProviderLobbyVM
				{
					username = "",
					lang = "",
					currency = "",
					gameList = GetGameList(null, null, null),
				};
				return View(model);

			}
			catch (Exception ex)
			{
				return View();
			}
		}

		public JsonResult SearchGame(string gameName, string lang, string gameCategory = "")
		{
			List<ProviderLobbyGame> gameList = GetGameList(gameName, lang, gameCategory);
			return Json(gameList, JsonRequestBehavior.AllowGet);
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

		public string LaunchDirectGame(string username, string gameid, string langid, string token)
		{
			lock (MyLock)
			{
				try
				{
					//_common.InsertLog("LaunchDirectGame", "username", username, "gameid", gameid, "langid", langid, "token", token);

					dynamic res = api.GetSlotGameUrl(username, langid, gameid, token);


					//_common.InsertLog("PPSLOT", "LaunchDirectGame", "request", api.request, "response", api.response);

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

		private List<ProviderLobbyGame> GetGameList(string gameName = "", string lang = "", string gameCategory = "")
		{
			try
			{
				using (BingoConn db = new BingoConn())
				{
					var q_list = db.ProviderLobbyGame.Where(x => x.ProviderName.ToUpper() == P_GamePrefix.ToUpper());

					if (!string.IsNullOrWhiteSpace(gameName))
					{
						if (lang?.ToUpper() == "EN")
							q_list = q_list.Where(x => x.GameNameEn.Contains(gameName));
						else
							q_list = q_list.Where(x => x.GameNameCn.Contains(gameName));
					}

					if (gameCategory == "Hot")
						q_list = q_list.Where(x => x.IsHot == true).OrderBy(x => x.HotSeq);
					else if (gameCategory == "BonusBuy")
						q_list = q_list.Where(x => x.IsBonusBuy == true).OrderBy(x => x.BonusBuySeq);
					else if (gameCategory == "New")
						q_list = q_list.Where(x => x.IsNew == true).OrderBy(x => x.NewSeq);
					else
						q_list = q_list.Where(x => x.IsAll == true).OrderBy(x => x.AllSeq);

					return q_list.ToList();
				}
			}
			catch (DbEntityValidationException e)
			{
				foreach (var eve in e.EntityValidationErrors)
				{
					_common.LogError("PPSlot/GetGameList" + " " + DateTime.Now.ToString() + " " + string.Format("Entity of type \"{0}\" in state \"{1}\" has the following validation errors:",
													eve.Entry.Entity.GetType().Name, eve.Entry.State), P_FilePrefix);
					foreach (var ve in eve.ValidationErrors)
					{
						_common.LogError("PPSlot/GetGameList" + " " + DateTime.Now.ToString() + " " + string.Format("- Property: \"{0}\", Error: \"{1}\"",
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

		//public JsonResult SearchGame(string gameName, string gameCategory = "")
		//{
		//	GetCasinoGamesResponseV2 model = api.GetGamesList();
		//	List<GameList> gameList = GetGameList(gameName, gameCategory.ToUpper());
		//	model.gameList = gameList;
		//	if (model.gameList != null)
		//	{
		//		model.gameList = model.gameList.ToList();
		//	}
		//	return Json(model, JsonRequestBehavior.AllowGet);
		//}

		//private List<GameList> GetGameList(string gameName = "", string lang = "", string gameCategory = "")
		//{
		//	using (BingoConn db = new BingoConn())
		//	{
		//		try
		//		{
		//			List<PragmaticSlotGameList> foundGameList = new List<PragmaticSlotGameList>();
		//			List<GameList> gameList = new List<GameList>();
		//			switch (gameCategory.ToUpper())
		//			{
		//				case "HOT":
		//					//foundGameList = db.PragmaticSlotGameLists.Where(x => x.GameName.Contains(gameName)
		//					//		&& x.IsHot == true).OrderBy(x => x.HotSeq).ToList();
		//					foundGameList = db.PragmaticSlotGameLists.Where(x => x.GameName.Contains(gameName)
		//							&& x.IsHot == true).ToList();
		//					foundGameList = foundGameList.OrderByDescending(x => x.HotSeq).ToList();
		//					break;
		//				case "FAVOURITE":
		//					foundGameList = db.PragmaticSlotGameLists.Where(x => x.GameName.Contains(gameName)
		//							 && x.IsFavorite == true).OrderByDescending(x => x.FavoriteSeq).ToList();
		//					break;
		//				case "NEW":
		//					foundGameList = db.PragmaticSlotGameLists.Where(x => x.GameName.Contains(gameName)
		//							 && x.IsNew == true).OrderByDescending(x => x.NewSeq).ToList();
		//					break;
		//				case "ALL":
		//					foundGameList = db.PragmaticSlotGameLists.Where(x => x.GameName.Contains(gameName)
		//							 && x.IsAll == true).OrderByDescending(x => x.AllSeq).ToList();
		//					break;
		//				default:
		//					foundGameList = db.PragmaticSlotGameLists.Where(x => x.GameName.Contains(gameName)
		//							 && x.IsAll == true).ToList();
		//					foundGameList = foundGameList.OrderBy(x => x.AllSeq).ToList();
		//					break;
		//			}
		//			//switch (gameCategory.ToUpper())
		//			//{
		//			//	case "HOT":
		//			//		foundGameList = db.PragmaticSlotGameLists.Where(x => x.GameName.Contains(gameName)
		//			//				&& x.IsHot == true).OrderByDescending(x => x.HotSeq).ToList();
		//			//		break;
		//			//	case "FAVOURITE":
		//			//		foundGameList = db.PragmaticSlotGameLists.Where(x => x.GameName.Contains(gameName)
		//			//				 && x.IsFavorite == true).OrderByDescending(x => x.FavoriteSeq).ToList();
		//			//		break;
		//			//	case "NEW":
		//			//		foundGameList = db.PragmaticSlotGameLists.Where(x => x.GameName.Contains(gameName)
		//			//				 && x.IsNew == true).OrderByDescending(x => x.NewSeq).ToList();
		//			//		break;
		//			//	case "ALL":
		//			//		foundGameList = db.PragmaticSlotGameLists.Where(x => x.GameName.Contains(gameName)
		//			//				 && x.IsAll == true).OrderByDescending(x => x.AllSeq).ToList();
		//			//		break;
		//			//	default:
		//			//		foundGameList = db.PragmaticSlotGameLists.Where(x => x.GameName.Contains(gameName)
		//			//				 && x.IsAll == true).ToList();
		//			//		foundGameList = foundGameList.OrderBy(x => x.AllSeq).ToList();
		//			//		break;
		//			//}

		//			for (int index = 0; index < foundGameList.Count(); index++)
		//			{
		//				GameList game = new GameList();
		//				game.aspectRatio = foundGameList[index].AspectRatio;
		//				game.gameID = foundGameList[index].GameID;
		//				game.gameIDNumeric = (long)foundGameList[index].GameIdNumeric;
		//				game.gameName = foundGameList[index].GameName;
		//				game.gameTypeID = foundGameList[index].GameTypeID;
		//				game.typeDescription = foundGameList[index].TypeDescription;
		//				game.technology = foundGameList[index].Technology;
		//				game.technologyID = foundGameList[index].TechnologyID;
		//				game.platform = foundGameList[index].Platform;
		//				game.demoGameAvailable = foundGameList[index].DemoGameAvailable;
		//				gameList.Add(game);
		//			}
		//			return gameList;
		//		}
		//		catch (DbEntityValidationException e)
		//		{
		//			foreach (var eve in e.EntityValidationErrors)
		//			{
		//				_common.LogError("PPSlot/GetGameList" + " " + DateTime.Now.ToString() + " " + string.Format("Entity of type \"{0}\" in state \"{1}\" has the following validation errors:",
		//												eve.Entry.Entity.GetType().Name, eve.Entry.State), P_FilePrefix);
		//				foreach (var ve in eve.ValidationErrors)
		//				{
		//					_common.LogError("PPSlot/GetGameList" + " " + DateTime.Now.ToString() + " " + string.Format("- Property: \"{0}\", Error: \"{1}\"",
		//													ve.PropertyName, ve.ErrorMessage), P_FilePrefix);
		//				}
		//			}
		//			throw;
		//		}
		//		catch (Exception ex)
		//		{
		//			_common.LogError("Record " + " " + DateTime.Now.ToString() + " " + ex.Message.ToString(), P_FilePrefix);
		//			throw;
		//		}
		//	}
		//}

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
						_common.LogError("PPSlot/SaveGameListToDatabase" + " " + DateTime.Now.ToString() + " " + string.Format("Entity of type \"{0}\" in state \"{1}\" has the following validation errors:",
														eve.Entry.Entity.GetType().Name, eve.Entry.State), P_FilePrefix);
						foreach (var ve in eve.ValidationErrors)
						{
							_common.LogError("PPSlot/SaveGameListToDatabase" + " " + DateTime.Now.ToString() + " " + string.Format("- Property: \"{0}\", Error: \"{1}\"",
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

        public JsonResult AddFRBPlayers(string bonusCode, string players)
        {
            try
            {
                List<string> playerList = players.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToList();

                if (string.IsNullOrEmpty(bonusCode))
                    return Json(new { success = false, message = "bonus code cannot be null" });

                if (playerList.Count == 0)
                    return Json(new { success = false, message = "player list must contain at least one player id" });

                (bool success, string message) = api.AddFRBPlayers(bonusCode, playerList);
                return Json(new { success = success, message = message });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }

        }

        public JsonResult RemoveFRBPlayers(string bonusCode, string players)
        {
            try
            {
                List<string> playerList = players.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToList();

                if (string.IsNullOrEmpty(bonusCode))
                    return Json(new { success = false, message = "bonus code cannot be null" });

                if (playerList.Count == 0)
                    return Json(new { success = false, message = "player list must contain at least one player id" });

                (bool success, string message) = api.RemoveFRBPlayers(bonusCode, playerList);
                return Json(new { success = success, message = message });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }

        }

        public JsonResult GetPlayerFRB(string playerId)
        {
            try
            {
                if (string.IsNullOrEmpty(playerId))
                    return Json(new { success = false, message = "player id cannot be null" });

                (bool success, string message) = api.GetPlayerFRB(playerId);
                return Json(new { success = success, message = message });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }

        }

		// GET: /PPSlot/OpenGame?gameId=vs20fruitsw&username=player1&lang=en&currency=MYR
		public ActionResult OpenGame(string gameId, string username, string lang = "en",
									  string currency = "MYR", decimal cash = 0, decimal chip = 0)
		{
			try
			{
				if (string.IsNullOrWhiteSpace(gameId) || string.IsNullOrWhiteSpace(username))
					return Content("gameId and username are required");

				string trackerId = Guid.NewGuid().ToString("N");
				string ip = Request.UserHostAddress; // or your existing GetIPAddress() helper

				// Field order must match whatever your seamless wallet callback recomputes
				string parameter = "PPSLOT" + username + currency + cash.ToString() + chip.ToString() + trackerId + lang;
				string token = TokenGenerator.GetMD5Signature(parameter);

				using (BingoConn db = new BingoConn())
				{
					
					var record = db.SeamlessWallets.SingleOrDefault(x => x.Game == "PPSLOT" && x.Username == username);
					if (record == null)
					{
						record = new Models.SeamlessWallet
						{
							Game = "PPSLOT",
							Username = username,
							Currency = currency,
							Cash = cash,
							Chip = chip,
							TrackerId = trackerId,
							Token = token
						};
						db.SeamlessWallets.Add(record);
					}
					else
					{
						record.Cash += cash;
						record.Chip += chip;
						record.Currency = currency;
						record.TrackerId = trackerId;
						record.Token = token;
					}

					db.SaveChanges();
				}

				// IMPORTANT: construct per-request, with the actual player + currency + ip,
				// same as GetGameUrlSeamless does — do NOT reuse the shared field-level `api`.
				var connector = new PPSlotConnector(username, currency, ip, BaseConnector.GameEnvironment.PROD);
				//var res = connector.GetSlotGameUrl(username, lang, "vs9chen", token);
				var res = connector.GetSlotGameUrl(username, lang, gameId, token);

				if (res.Code != 0 || string.IsNullOrEmpty(res.Url))
				{
					_common.LogError($"OpenGame: launch failed for {username}/{gameId} - {res.Message}", P_FilePrefix);
					return Content("Unable to launch game: " + res.Message);
				}

				return Redirect(res.Url);
			}
			catch (Exception ex)
			{
				_common.LogError($"OpenGame {gameId}/{username} " + ex.Message, P_FilePrefix);
				return Content("Error launching game: " + ex.Message);
			}
		}

		//// GET: /PPSlot/OpenGame?gameId=vs20fruitsw&username=player1&lang=en&currency=MYR
		//public ActionResult OpenGameWOUser(string gameId, string lang = "en",
		//							  string currency = "MYR", decimal cash = 0, decimal chip = 0)
		//{
		//	try
		//	{
		//		string UserId = User.Identity.GetUserId();
		//		using (ApplicationDbContext db = new ApplicationDbContext())
  //              {

  //              }


		//		if (string.IsNullOrWhiteSpace(gameId) || string.IsNullOrWhiteSpace(username))
		//		return Content("gameId and username are required");

		//		string trackerId = Guid.NewGuid().ToString("N");
		//		string ip = Request.UserHostAddress; // or your existing GetIPAddress() helper

		//		// Field order must match whatever your seamless wallet callback recomputes
		//		string parameter = gameId + username + currency + cash.ToString() + chip.ToString() + trackerId + lang;
		//		string token = TokenGenerator.GetMD5Signature(parameter);

		//		using (BingoConn db = new BingoConn())
		//		{
		//			var record = db.SeamlessWallets.SingleOrDefault(x => x.Game == gameId && x.Username == username);

		//			if (record == null)
		//			{
		//				record = new Models.SeamlessWallet
		//				{
		//					Game = gameId,
		//					Username = username,
		//					Currency = currency,
		//					Cash = cash,
		//					Chip = chip,
		//					TrackerId = trackerId,
		//					Token = token
		//				};
		//				db.SeamlessWallets.Add(record);
		//			}
		//			else
		//			{
		//				record.Cash += cash;
		//				record.Chip += chip;
		//				record.Currency = currency;
		//				record.TrackerId = trackerId;
		//				record.Token = token;
		//			}

		//			db.SaveChanges();
		//		}

		//		// IMPORTANT: construct per-request, with the actual player + currency + ip,
		//		// same as GetGameUrlSeamless does — do NOT reuse the shared field-level `api`.
		//		var connector = new PPSlotConnector(username, currency, ip, BaseConnector.GameEnvironment.PROD);
		//		var res = connector.GetSlotGameUrl(username, lang, gameId, token);

		//		if (res.Code != 0 || string.IsNullOrEmpty(res.Url))
		//		{
		//			_common.LogError($"OpenGame: launch failed for {username}/{gameId} - {res.Message}", P_FilePrefix);
		//			return Content("Unable to launch game: " + res.Message);
		//		}

		//		return Redirect(res.Url);
		//	}
		//	catch (Exception ex)
		//	{
		//		_common.LogError($"OpenGame {gameId}/{username} " + ex.Message, P_FilePrefix);
		//		return Content("Error launching game: " + ex.Message);
		//	}
		//}

	}
}