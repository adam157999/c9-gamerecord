using GameRecord.Models;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Script.Serialization;

namespace GameRecord.Controllers
{
    public class FachaiFishController : Controller
    {
        private FACHAIFISHConnector _connector;

        private FACHAIFISHConnector connector
        {
            get
            {
                if (_connector == null)
                {
                    _connector = new FACHAIFISHConnector(Environment: BaseConnector.GameEnvironment.PROD);
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
            FachaiFishLobbyVM vm = new FachaiFishLobbyVM();
            GameRecord.Gamess.FACHAI.FCAPI.GetGameListandIconResponse model = connector.GetGameList();
            vm.gameList = model;
            vm.lang = lang;
            vm.username = playerid;
            vm.currency = currency;
            return View(vm);
        }

        [System.Web.Mvc.HttpPost]
        public JsonResult LaunchGame([FromBody] int gameCode, string playerid, string currency, string ip, string mobile, string Language = "en")
        {
            if (gameCode <= 0)
            {
                return Json(new { success = false, message = "Game code is required." });
            }

            string gameUrl = null;
            connector = new FACHAIFISHConnector(playerid, currency, ip, BaseConnector.GameEnvironment.PROD);
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

        [System.Web.Mvc.HttpPost]
        public JsonResult SearchGame(string gameName, string lang, string gameCategory)
        {
            var model = connector.GetGameList();
            var fishingGames = model.GetGameIconList?.fishing;
            var result = new List<object>();
            if (fishingGames != null)
            {
                foreach (var g in fishingGames)
                {
                    bool match = true;
                    if (!string.IsNullOrWhiteSpace(gameName))
                    {
                        if (lang?.ToLower() == "en")
                            match = g.Value.gameNameOfEnglish.IndexOf(gameName, StringComparison.OrdinalIgnoreCase) >= 0;
                        else
                            match = g.Value.gameNameOfChinese.IndexOf(gameName, StringComparison.OrdinalIgnoreCase) >= 0;
                    }
                    if (match)
                    {
                        result.Add(new {
                            GameID = g.Key,
                            GameName = g.Value.gameNameOfEnglish,
                            GameNameCn = g.Value.gameNameOfChinese,
                            GameIcon = g.Value.enUrl
                        });
                    }
                }
            }
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        [System.Web.Mvc.HttpGet]
        public string LaunchDirectGame(string username, string gameid, string langid, string currency, bool isMobile)
        {
            try
            {
                connector = new FACHAIFISHConnector(username, currency, null, BaseConnector.GameEnvironment.PROD);
                int gameCode;
                if (!int.TryParse(gameid, out gameCode))
                {
                    dynamic errorResult = new ExpandoObject();
                    errorResult.ErrorCode = 1;
                    errorResult.url = null;
                    JsonResult json = new JsonResult { Data = new JavaScriptSerializer().Serialize(errorResult) };
                    return new JavaScriptSerializer().Serialize(json);
                }
                var res = connector.GetFishGameUrl(langid,gameCode);
                dynamic result = new ExpandoObject();
                if (res != null && res.Code == 0)
                {
                    result.ErrorCode = 0;
                    result.url = res.Url;
                }
                else
                {
                    result.ErrorCode = 1;
                    result.url = null;
                }
                JsonResult jsonResult = new JsonResult { Data = new JavaScriptSerializer().Serialize(result) };
                return new JavaScriptSerializer().Serialize(jsonResult);
            }
            catch (Exception)
            {
                dynamic errorResult = new ExpandoObject();
                errorResult.ErrorCode = 1;
                errorResult.url = null;
                JsonResult json = new JsonResult { Data = new JavaScriptSerializer().Serialize(errorResult) };
                return new JavaScriptSerializer().Serialize(json);
            }
            finally
            {
                connector = null;
            }
        }
    }
}
