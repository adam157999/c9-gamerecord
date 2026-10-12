using GameRecord.Controllers.V2;
using GameRecord.Gamess.VPOWER;
using GameRecord.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web.Mvc;

namespace GameRecord.Controllers
{
    public class VPowerFishController : Controller
    {
        private VPowerFishConnector _connector;
        private Common _common = new Common();
        private VPowerFishConnector api = new VPowerFishConnector(environment: BaseConnector.GameEnvironment.PROD);

        private VPowerFishConnector connector
        {
            get
            {
                if (_connector == null)
                {
                    _connector = new VPowerFishConnector();
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
            VPOWERAPI.GetGameListResponse model = connector.GetFishingGames();
            var vm= new VPowerFishLobbyVM
            {
                lang = lang,
                username = playerid,
                currency = currency,
                games = MapGameListResponseToLobbyGames(model)
            };

            return View(vm);
        }

        [HttpPost]
        public JsonResult SearchGame(string gameName, string lang, string gameCategory)
        {
            var gameList = GetGameList(gameName, lang, gameCategory);
            var convertedGameList = MapGameListResponseToLobbyGames(gameList);
            return Json(convertedGameList, JsonRequestBehavior.AllowGet);
        }

        private VPOWERAPI.GetGameListResponse GetGameList(string gameName, string lang, string gameCategory)
        {
            try
            {
                using (BingoConn db = new BingoConn())
                {
                    var qList = connector.GetFishingGames(gameName,lang);
                    return qList;
                }
            }
            catch (Exception ex)
            {
                _common.LogError("Error at VPowerFish/GetGameList: " + ex.Message.ToString(), "VPOWERFISH");
                return new VPOWERAPI.GetGameListResponse();
            }
        }

        private List<VPowerFishLobbyGame> MapGameListResponseToLobbyGames(GameRecord.Gamess.VPOWER.VPOWERAPI.GetGameListResponse gameListResponse)
        {
            return gameListResponse.GameList.Select(g => new VPowerFishLobbyGame
            {
                GameID = g.GameID,
                GameName = g.GameName,
                GameNameEN = g.GameNameEN,
                GameIcon = g.Image1
            }).ToList();
        }

        [HttpPost]
        public JsonResult LaunchGame(string gameCode, string playerid, string currency, string ip, string mobile, string Language = "en")
        {
            if (string.IsNullOrEmpty(gameCode))
            {
                return Json(new { success = false, message = "Game code is required." });
            }

            string gameUrl = null;
            connector = new VPowerFishConnector(playerid, currency, ip);
            var res = connector.GetFishGameUrl(Language, gameCode, bool.Parse(mobile.ToLower()));
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