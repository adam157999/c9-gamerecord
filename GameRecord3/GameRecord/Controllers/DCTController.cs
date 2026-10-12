using GameRecord.Controllers.V2;
using GameRecord.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using static GameRecord.Controllers.BaseConnector;

namespace GameRecord.Controllers
{
    public class DCTController : Controller
    {
        public static readonly object MyLock = new object();
        private string P_GamePrefix = "DCT";
        Common _common = new Common();
		//private DCTConnector api = new DCTConnector(Environment: BaseConnector.GameEnvironment.PROD);
		private DCTConnector api;

		public string GetIPAddress()
		{
			var result = string.Empty;
			System.Web.HttpRequest request = System.Web.HttpContext.Current.Request;
			string forwardedFor = request.ServerVariables["HTTP_X_FORWARDED_FOR"];
			string remoteAddr = string.IsNullOrWhiteSpace(forwardedFor) ? request.ServerVariables["REMOTE_ADDR"] : forwardedFor.Split(',').Select(s => s.Trim()).First();
			result = String.IsNullOrWhiteSpace(remoteAddr) ? request.UserHostAddress : remoteAddr;
			return result;
		}

		public DCTController ()
        {
			api = new DCTConnector(IPAddr: GetIPAddress(),  Environment: BaseConnector.GameEnvironment.PROD);
		}

		public ActionResult Lobby(string playerId, string langCode)
		{
			try
			{
				DCTLobbyVM model = new DCTLobbyVM
				{
					PlayerId = playerId,
					LangCode = langCode,
					Games = new List<DCTLobbyVM_Game>(),
				};

				var r = api.GetGamesList();
				if (r != null)
				{
					foreach (var g in r.data)
					{
						var G = new DCTLobbyVM_Game
						{
							GameId = g.game_id,
							GameType = g.game_type,
							ImageUrl = g.game_icon
						};

						G.GameName = langCode == "cn" ? g.game_name_cn : g.game_name;

						model.Games.Add(G);
					}
				}

				return View(model);
			}
			catch (Exception ex)
			{
				_common.LogError("Error at DCTController/Lobby: " + ex.Message.ToString(), P_GamePrefix);
				return Content("Error at DCTController/Lobby: " + ex.Message.ToString());
			}
		}

		[HttpPost]
		public JsonResult LaunchGame(int gameId, string langCode, bool isMobile, string returnUrl, bool isFullScreen)
		{
			try
			{
				var r = api.GetLaunchGameUrl(gameId, langCode,isMobile, returnUrl, isFullScreen);
				if (r != null)
					return Json(new { Code = 0, Url = r.data.game_url });
				else
					return Json(new { Code = -1, Message = "Failed to get URL." });
			}
			catch (Exception ex)
			{
				_common.LogError("DCT/LaunchGame: " + ex.Message.ToString(), P_GamePrefix);
				return Json(new { Code = -1, Message = ex.Message.ToString() });
			}
		}

		[HttpPost]
		public string DCTJob()
		{
			try
			{
				lock (MyLock)
				{
					api = new DCTConnector(P_GamePrefix, Environment: GameEnvironment.PROD);
					using (BingoConn db = new BingoConn())
					{
						var adm = db.ADMJobDates.Where(s => s.GameProvider == P_GamePrefix).FirstOrDefault();
						if (adm == null || adm.LastRunTillDate == null)
							return "Please setup ADMJobDates with LastRunTillDate.";

						DateTime dtStart = adm.LastRunTillDate.Value.AddMinutes(-15);
						DateTime dtEnd = _common.DtNowTrimMillisecond();

						//string dateFrom = dtStart.ToString("yyyy-MM-dd HH:mm:ss");
						//string dateTo = dtEnd.ToString("yyyy-MM-dd HH:mm:ss");

						int iMax = 5000;
						int iPage = 1;
						int iProcessed = 0;
						bool bEnd = false;
						bool bError = false;

						while (!bEnd)
						{
							var result = api.GetBetData(dtStart, dtEnd, iPage);
							if (result == null)
							{
								bEnd = true;
								bError = true;
								break;
							}
							else
							{
								if (result.data != null && result.data.Count > 0)
								{
									foreach (var r in result.data)
									{
										string s = "insert into HacksawGame (brand_uid,currency,wager_type,amount,before_amount,after_amount,game_id,game_name,round_id,wager_id,jackpot_contribution,jackpot_win,description, create_time, tip, is_endround, ProcessedAt) ";
										s += "select * from (values (" + _common.F1(r.brand_uid) + "," + _common.F1(r.currency) + "," + _common.F1(r.wager_type) + "," + _common.F5(r.amount) + "," + _common.F5(r.before_amount) + "," + _common.F5(r.after_amount) + "," + r.game_id + "," + _common.F1(r.game_name) + "," + r.round_id + "," + _common.F3(r.wager_id) + "," + _common.F5(r.jackpot_contribution) + "," + _common.F5(r.jackpot_win) + "," + _common.F1(r.description) + "," + _common.F2(r.create_time) + "," + _common.F5(r.tip) + "," + _common.F4(r.is_endround) + ",getdate())) ";
										s += "as a(brand_uid,currency,wager_type,amount,before_amount,after_amount,game_id,game_name,round_id,wager_id,jackpot_contribution,jackpot_win,description, create_time, tip, is_endround, CreatedDate) ";
										s += "where not exists (select * from HacksawGame b with (updlock) where b.round_id=a.round_id);";
										_common.Sql_RunAndCommit(s);
										iProcessed++;
									}

									if (result.data.Count >= iMax)
										iPage++;
									else
										bEnd = true;
								}
								else
								{
									bEnd = true;
								}
							}
						}

						if (!bError)
						{
							adm.LastRunTillDate = dtEnd;
							db.SaveChanges();
						}

						return "DCTJob has processed " + iProcessed + " record(s) from " + dtStart.ToString("yyyy-MM-dd HH:mm:ss") + " until " + dtEnd.ToString("yyyy-MM-dd HH:mm:ss") + (bError ? " with error." : "");
					}
				}
			}
			catch (Exception ex)
			{
				_common.LogError("Error at DCT/DCTJob: " + ex.Message.ToString(), P_GamePrefix);
				return "Error at DCT/DCTJob: " + ex.Message.ToString();
			}
		}

		////pp
		//public ActionResult GetGameList_Manual()
		//{
		//	try
		//	{
		//		DCTLobbyVM model = new DCTLobbyVM
		//		{
		//			PlayerId = playerId,
		//			LangCode = langCode,
		//			Games = new List<DCTLobbyVM_Game>(),
		//		};

		//		var r = api.GetGamesList();
		//		if (r != null)
		//		{
		//			foreach (var g in r.data)
		//			{
		//				var G = new DCTLobbyVM_Game
		//				{
		//					GameId = g.game_id,
		//					GameType = g.game_type,
		//					ImageUrl = g.game_icon
		//				};

		//				G.GameName = langCode == "cn" ? g.game_name_cn : g.game_name;

		//				model.Games.Add(G);
		//			}
		//		}

		//		return View(model);
		//	}
		//	catch (Exception ex)
		//	{
		//		_common.LogError("Error at DCTController/Lobby: " + ex.Message.ToString(), P_GamePrefix);
		//		return Content("Error at DCTController/Lobby: " + ex.Message.ToString());
		//	}
		//}
	}
}