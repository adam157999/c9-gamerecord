using System.Web.Http;
using System.Web.Mvc;

namespace GameRecord.Controllers
{
    public class FachaiController : Controller
	{

        private FACHAIConnector _connector;

        private FACHAIConnector connector
        {
            get
            {
                if (_connector == null)
                {
                    _connector = new FACHAIConnector(Environment: BaseConnector.GameEnvironment.PROD);
                }
                return _connector;
            }
            set
            {
                _connector = value;
            }
        } 

        public ActionResult Lobby(string playerid, string currency, string ip,string lang, string mobile)
        {
            GameRecord.Gamess.FACHAI.FCAPI.GetGameListandIconResponse model = connector.GetGameList();
            ViewBag.playerid = playerid;
            ViewBag.currency = currency;
            ViewBag.ip = ip;
            ViewBag.lang = lang;
            ViewBag.mobile = mobile.ToLower();

             return View(model);
        }


        [System.Web.Mvc.HttpPost]
        public JsonResult LaunchGame([FromBody] int gameCode, string playerid, string currency, string ip, string mobile, string Language = "en")
        {
            if (gameCode <= 0)
            {
                return Json(new { success = false, message = "Game code is required." });
            }

            string gameUrl = null;

            connector = new FACHAIConnector(playerid, currency, ip, BaseConnector.GameEnvironment.PROD);
            
                var res = connector.GetSlotGameUrl(Language, gameCode, bool.Parse(mobile.ToLower()));
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