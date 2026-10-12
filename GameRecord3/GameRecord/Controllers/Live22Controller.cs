using GameRecord.Controllers.V2;
using GameRecord.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using static GameRecord.Controllers.BaseConnector;

namespace GameRecord.Controllers
{
    public class Live22Controller : Controller
    {
		public static readonly object MyLock = new object();
		private string P_GamePrefix = "LIVE22";
		private string P_FilePrefix = "LIVE22";
		private string Currency = "MYR";
		private string P_DTFormat = "yyyy-MM-ddTHH:mm:ss.fffZ";
		Common _common = new Common();
		private Live22Connector api = new Live22Connector(Environment: BaseConnector.GameEnvironment.PROD);

		public Live22Controller(GameEnvironment Environment = GameEnvironment.PROD)
		{
			api = new Live22Connector(Environment: Environment);
		}

		public Live22Controller()
		{
			api = new Live22Connector(Environment: GameEnvironment.PROD);
		}

		//http://localhost:51148/Live22/Lobby
		public ActionResult Lobby(string Username, string Lang, string Currency)
		{
			try
			{
				Live22LobbyVM model = new Live22LobbyVM
				{
					Username = Username,
					Lang = Lang,
					Currency = Currency,
					Games = new List<Live22LobbyVM_Game>(),
				};

				var r = api.GetGamesList();
				if (r != null)
				{
					foreach (var g in r.Game)
					{
						model.Games.Add(new Live22LobbyVM_Game
						{
							GameCode = g.GameCode,
							GameName = g.GameName,
							ImageUrl = g.ImageUrl,
						});
					}
				}

				return View(model);
			}
			catch (Exception ex)
			{
				_common.LogError("Error at Live22Controller/Lobby: " + ex.Message.ToString(), P_FilePrefix);
				return Content("Error at Live22Controller/Lobby: " + ex.Message.ToString());
			}
		}

		[HttpPost]
        public ActionResult LaunchDirectGame(string Username, string GameCode, string Lang)
        {
            try
            {
                string url = api.GetSlotGameUrl(Username, GameCode, Lang, Request.UserHostAddress);

				if (!string.IsNullOrWhiteSpace(url))
					return Json(new { Code = 0, Message = "Successfully get.", Url = url });
				else
					return Json(new { Code = -1, Message = "Unable to get game URL." });
			}
            catch (Exception ex)
            {
				_common.LogError("Error at: Live22Controller/LaunchDirectGame" + "\r\nError message: " + ex.Message.ToString(), P_FilePrefix);
				return Json(new { Code = -1, Message = "Error at Live22Controller/LaunchDirectGame: " + ex.Message.ToString() });
            }
        }

		//logic app will call this
		//http://localhost:51148/Live22/Live22Job
		//https://c9live-gamerecord-qa.azurewebsites.net/Live22/Live22Job
		public void Live22Job()
		{
			try
			{
				lock (MyLock)
				{
					api = new Live22Connector(Environment: GameEnvironment.PROD);
					bool isOK = GetGameRecord();
				}
			}
			catch (Exception ex)
			{
				_common.LogError("Error at Live22Controller/Live22Job:" + ex.Message.ToString(), P_FilePrefix);
			}
		}

		public bool GetGameRecord()
		{
			try
			{
				var ids = new List<long>();

				//PullLog
				var result = api.GetBetHistory();
				if (result != null && result.Logs != null)
				{
					foreach (var r in result.Logs)
					{
						try
						{
							string s = "insert into Live22Game (TranId,PlayerId,Turnover,ValidTurnover,Payout,WinLose,TranDt,TranType,GameCode,GameType,GameName) ";
							s += "select * from (values (" + r.TranId + "," + _common.F1(r.PlayerId) + "," + r.Turnover + "," + r.ValidTurnover + "," + r.Payout + ","
								+ r.WinLose + "," + _common.F1(r.TranDt.AddHours(8).ToString("yyyy-MM-dd HH:mm:ss")) + "," + r.TranType + "," + _common.F1(r.GameCode) + ","
								+ r.GameType + "," + _common.F1(r.GameName.Replace("'", "")) + ")) as a(TranId,PlayerId,Turnover,ValidTurnover,Payout,WinLose,TranDt,TranType,GameCode,GameType,GameName) ";
							s += "where not exists (select * from Live22Game b with (updlock) where b.TranId=a.TranId);";
							_common.Sql_RunAndCommit(s);

							ids.Add(r.TranId);

						}
						catch (Exception ex)
						{
							_common.LogError("Error at Live22Controller/GetGameRecord (2):" + ex.Message.ToString(), P_FilePrefix);
						}
					}
				}

				//FlagLog
				if (ids.Count > 0)
				{
					var result2 = api.FlagBetHistory(ids);
				}

				return true;
			}
			catch (Exception ex)
			{
				_common.LogError("Error at Live22Controller/GetGameRecord:" + ex.Message.ToString(), P_FilePrefix);
				return false;
			}
		}
	}
}