using System.Collections.Generic;
using System.Web.Http;
using System.Web.Mvc;
using static GameRecord.Gamess.MicroGaming.MGAPI;

namespace GameRecord.Controllers
{
    public class MGController : Controller
    {

        private MGConnector _connector;

        private MGConnector connector
        {
            get
            {
                if (_connector == null)
                {
                    _connector = new MGConnector(Environment: BaseConnector.GameEnvironment.PROD);
                }
                return _connector;
            }
            set
            {
                _connector = value;
            }
        }

        public ActionResult Lobby(string playerid, string currency, string ip, string lang)
        {
            List<GetGamesResponse> model = connector.GetGameList();
            ViewBag.playerid = playerid;
            ViewBag.currency = currency;
            ViewBag.ip = ip;
            ViewBag.lang = lang;

            return View(model);
        }

        [System.Web.Mvc.HttpPost]
        public JsonResult LaunchGame([FromBody] string gameCode, string playerid, string currency, string ip, string platform, string Language = "en")
        {
            if (string.IsNullOrEmpty(gameCode))
            {
                return Json(new { success = false, message = "Game code is required." });
            }

            string gameUrl = null;

            connector = new MGConnector(playerid, currency, ip, BaseConnector.GameEnvironment.PROD);

            var res = connector.GetSlotGameUrl(Language, gameCode, platform);
            if (res != null && res.Code == 0)
            {
                gameUrl = res.Url;
            }
            connector = null;

            if (string.IsNullOrEmpty(gameUrl))
            {
                return Json(new { success = false, message = "Failed to launch game." });
            }

            return Json(new { success = true, url = gameUrl });
        }


    }
}