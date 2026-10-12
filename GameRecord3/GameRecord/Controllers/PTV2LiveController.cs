using GameRecord.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using System.Web.Script.Serialization;

namespace GameRecord.Controllers
{
    public class PTV2LiveController : Controller
    {
        public static readonly object MyLock = new object();
        private string P_GamePrefix = "PTV2LIVE";
        private string gameID = "PTV2LIVE";
        private string systemId = "";
        private string virtualDatabase = "";
        private string mobileHub = "";
        private string currencyID = "";
        private string environment = "STAG";

        Common _common = new Common();

        //http://localhost:51148/PTV2/Lobby
        public ActionResult Lobby(string playerName, string lang = "en")
        {
            PTV2LobbyVM model = new PTV2LobbyVM
            {
                playerName = playerName,
                lang = lang,
                isLive = true,
                gameList = GetGameList(null, lang, null),
            };

            return View("~/Views/PTV2/Lobby.cshtml", model);

        }

        public JsonResult SearchGame(string gameName, string lang, string gameCategory)
        {
            List<PTV2SlotLobbyGame> gameList = GetGameList(gameName, lang, gameCategory);
            return Json(gameList, JsonRequestBehavior.AllowGet);
        }

        private List<PTV2SlotLobbyGame> GetGameList(string gameName, string lang, string gameCategory)
        {
            try
            {
                using (BingoConn db = new BingoConn())
                {
                    var q_list = db.PTV2SlotLobbyGames.Where(x => x.GameType == "Live");

                    if (!string.IsNullOrWhiteSpace(gameName))
                    {
                        if (lang?.ToUpper() == "EN")
                            q_list = q_list.Where(x => x.GameNameEn.Contains(gameName));
                        else
                            q_list = q_list.Where(x => x.GameNameCn.Contains(gameName));
                    }

                    if (gameCategory == "Hot")
                        q_list = q_list.Where(x => x.IsHot == true).OrderByDescending(x => x.HotSeq);
                    else if (gameCategory == "Favourite")
                        q_list = q_list.Where(x => x.IsFavorite == true).OrderByDescending(x => x.FavoriteSeq);
                    else if (gameCategory == "New")
                        q_list = q_list.Where(x => x.IsNew == true).OrderByDescending(x => x.NewSeq);
                    else
                        q_list = q_list.Where(x => x.IsAll == true).OrderByDescending(x => x.AllSeq);

                    return q_list.ToList();
                }
            }
            catch (Exception ex)
            {
                _common.LogError("Error at PTV2LIVE/GetGameList: " + ex.Message.ToString(), P_GamePrefix);
                return null;
            }
        }

        public ActionResult LaunchGame(string playerName, string gameCode, string lang = "en", bool mobile = false, string lobby = "")
        {
            try
            {
                PTV2LiveConnector api = new PTV2LiveConnector(Environment: BaseConnector.GameEnvironment.PROD);
                using (BingoConn db = new BingoConn())
                {
                    var gameMap = db.GameMappings.Where(x => x.Game.ToUpper().Trim() == gameID.ToUpper().Trim() &&
                                                                                                x.GamePlayerID == playerName &&
                                                                                                x.IsActive == true).FirstOrDefault();

                    var launch = api.LaunchGame(playerName, gameMap?.GamePassword ?? "", gameCode, lang, mobile);
                    launch.Lobby = lobby;
                    return View("~/Views/PTV2/Live.cshtml", launch);
                }
            }
            catch (Exception ex)
            {
                _common.LogError("LaunchGame" + " " + DateTime.Now.ToString() + " " + string.Format("{0}|{1}|{2}", playerName, lang, gameID) + " " + ex.StackTrace.ToString(), P_GamePrefix);
                return View("Error", ex.Message);
            }
        }
    }
}