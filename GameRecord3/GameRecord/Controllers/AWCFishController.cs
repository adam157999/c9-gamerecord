using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using GameRecord.Models;

namespace GameRecord.Controllers
{
    public class AWCFishController : Controller
    {
        private AWCFishConnector _connector;

        private AWCFishConnector connector
        {
            get
            {
                if (_connector == null)
                {
                    _connector = new AWCFishConnector(environment: BaseConnector.GameEnvironment.PROD);
                }
                return _connector;
            }
            set
            {
                _connector = value;
            }
        }

        public ActionResult Lobby(string playerid, string currency, string ip, string lang, string mobile)
        {
            var vm = new AWCFishLobbyVM();
            var model = connector.GetGameList(lang);
            vm.gameList = model;
            vm.lang = lang;
            vm.username = playerid;
            vm.currency = currency;
            return View(vm);
        }

        [HttpPost]
        public JsonResult LaunchGame([System.Web.Http.FromBody] int mType, string playerid, string currency, string ip, string mobile, string Language = "en")
        {
            if (mType <= 0)
            {
                return Json(new { success = false, message = "Game code is required." });
            }

            string gameUrl = null;
            connector = new AWCFishConnector(playerid, currency, ip, BaseConnector.GameEnvironment.PROD);
            var res = connector.GetFishGameUrl(Language, "7", mType.ToString(), bool.Parse(mobile.ToLower()));
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

        [HttpPost]
        public JsonResult SearchGame(string gameName, string lang, string gameCategory)
        {
            var model = connector.GetGameList(lang);
            var result = new List<object>();
            if (model != null)
            {
                foreach (var g in model)
                {
                    bool match = true;
                    if (!string.IsNullOrWhiteSpace(gameName))
                    {
                        match = g.name.IndexOf(gameName, StringComparison.OrdinalIgnoreCase) >= 0;
                    }
                    if (match)
                    {
                        result.Add(new {
                            mType = g.mType,
                            GameName = g.name,
                            GameIcon = g.image
                        });
                    }
                }
            }
            return Json(result, JsonRequestBehavior.AllowGet);
        }
    }
}