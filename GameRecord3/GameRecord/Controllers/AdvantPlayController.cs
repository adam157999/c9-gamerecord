using GameRecord.Controllers.V2;
using GameRecord.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using static GameRecord.Controllers.BaseConnector;

namespace GameRecord.Controllers
{
    public class AdvantPlayController : Controller
    {
		public static readonly object MyLock = new object();
		private string P_GamePrefix = "AP";
		Common _common = new Common();
		private AdvantPlayConnector api = new AdvantPlayConnector(Environment: BaseConnector.GameEnvironment.PROD);

		//http://localhost:51148/AdvantPlay/Lobby
		//http://localhost:51148/AdvantPlay/Lobby?PlayerId=APV7PPLIMP1&LangCode=zh-CN
		public ActionResult Lobby(string PlayerId, string LangCode)
		{
			try
			{
				APLobbyVM model = new APLobbyVM
				{
					PlayerId = PlayerId,
					LangCode = LangCode,
					Games = new List<APLobbyVM_Game>(),
				};

				var r = api.GetGamesList();
				if (r != null)
				{
					foreach (var g in r.Games)
					{
						var G = new APLobbyVM_Game
						{
							GameCode = g.GameCode,
						};

						var q_icon = g.IconList.Where(x => true);
						if (!string.IsNullOrWhiteSpace(LangCode))
							q_icon = q_icon.Where(x => x.Language == LangCode);
						var icon = q_icon.FirstOrDefault();
						if (icon != null)
                        {
							G.GameName = icon.Name;
							G.ImageUrl = icon.Url?.x?.bkg;
						}

						model.Games.Add(G);
					}
				}

				return View(model);
			}
			catch (Exception ex)
			{
				_common.LogError("Error at AdvantPlayController/Lobby: " + ex.Message.ToString(), P_GamePrefix);
				return Content("Error at AdvantPlayController/Lobby: " + ex.Message.ToString());
			}
		}

		[HttpPost]
		public JsonResult LaunchGame(string PlayerId, string GameCode, string LangCode, string BackUrl)
		{
			try
			{
				var r = api.GetLaunchGameUrl(PlayerId, GameCode, LangCode, BackUrl);
				if (r != null)
					return Json(new { Code = 0, Url = r.Url });
				else
					return Json(new { Code = -1, Message = "Failed to get URL." });
			}
			catch (Exception ex)
			{
				_common.LogError("AdvantPlay/LaunchGame: " + ex.Message.ToString(), P_GamePrefix);
				return Json(new { Code = -1, Message = ex.Message.ToString() });
			}
		}

		//http://localhost:51148/AdvantPlay/AdvantPlayJob
		public string AdvantPlayJob()
		{
			try
			{
				lock (MyLock)
				{
					api = new AdvantPlayConnector(P_GamePrefix, Environment: GameEnvironment.PROD);
					using (BingoConn db = new BingoConn())
					{
						var adm = db.ADMJobDates.Where(s => s.GameProvider == P_GamePrefix).FirstOrDefault();
						if (adm == null || adm.LastRunTillDate == null)
							return "Please setup ADMJobDates with LastRunTillDate.";

						DateTime dtStart = adm.LastRunTillDate.Value.AddMinutes(-15);
						DateTime dtEnd = _common.DtNowTrimMillisecond();

						string DateFrom = _common.GetTimestamp_ISO8601_Microsecond(dtStart);
						string DateTo = _common.GetTimestamp_ISO8601_Microsecond(dtEnd);

						int iMax = 1000;
						int iPage = 1;
						int iProcessed = 0;
						bool bEnd = false;
						bool bError = false;

						while (!bEnd)
						{
							var result = api.GetBetHistory(DateFrom, DateTo, iPage, iMax);
							if (result == null)
							{
								bEnd = true;
								bError = true;
								break;
							}
							else
							{
								if (result.Results != null && result.Results.Count > 0)
								{
									foreach (var r in result.Results)
									{
										string s = "insert into AdvantPlayGame (BrandCode,SiteCode,GameCode,GameRoundId,PlayerId,Currency,StatusCode,BetTime,SettleTime,TotalStake,TotalWin,CreatedDate) ";
										s += "select * from (values (" + _common.F1(r.BrandCode) + "," + _common.F1(r.SiteCode) + "," + _common.F1(r.GameCode) + "," + _common.F1(r.GameRoundId) + "," + _common.F1(r.PlayerId) + "," + _common.F1(r.Currency) + "," + _common.F1(r.StatusCode) + ",'" + r.BetTime.ToString("yyyy-MM-dd HH:mm:ss") + "','" + r.SettleTime.ToString("yyyy-MM-dd HH:mm:ss") + "'," + r.TotalStake + "," + r.TotalWin  + ",getdate())) ";
										s += "as a(BrandCode,SiteCode,GameCode,GameRoundId,PlayerId,Currency,StatusCode,BetTime,SettleTime,TotalStake,TotalWin,CreatedDate) ";
										s += "where not exists (select * from AdvantPlayGame b with (updlock) where b.GameRoundId=a.GameRoundId);";
										_common.Sql_RunAndCommit(s);
										iProcessed++;
									}

									if (result.Results.Count >= iMax)
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

						return "AdvantPlayJob has processed " + iProcessed + " record(s) from " + dtStart.ToString("yyyy-MM-dd HH:mm:ss") + " until " + dtEnd.ToString("yyyy-MM-dd HH:mm:ss") + (bError ? " with error." : "");
					}
				}
			}
			catch (Exception ex)
			{
				_common.LogError("Error at AdvantPlay/AdvantPlayJob: " + ex.Message.ToString(), P_GamePrefix);
				return "Error at AdvantPlay/AdvantPlayJob: " + ex.Message.ToString();
			}
		}
	}
}