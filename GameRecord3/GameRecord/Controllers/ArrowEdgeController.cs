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
//using Constant;

namespace GameRecord.Controllers
{
    public class ArrowEdgeController : Controller
    {
        public static readonly object MyLock = new object();
        private string P_GamePrefix = "ArrowEdge";
        private string P_FilePrefix = "ArrowEdge";
        private string P_DTFormat = "yyyy-MM-ddTHH:mm:ss.fffZ";
        private string gameImgUrl = string.Empty;
        Common _common = new Common();
        private ArrowEdgeConnector api = new ArrowEdgeConnector(Environment: BaseConnector.GameEnvironment.PROD);

        //lobby code here
        public ActionResult Lobby()
        {
            try
            {
                ArrowEdgeConnector.GameList gameList = GetGameList();
                return View(gameList);
            }
            catch (Exception ex)
            {
                return View();
            }
        }

        public JsonResult SearchGame(string gameName, string gameCategory = "")
        {
            ArrowEdgeConnector.GameList gameList = GetGameList(gameName, gameCategory.ToUpper());
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

        public string LaunchDirectGame(string username, string gameid, string langid, string token, string currency)
        {
            lock (MyLock)
            {
                try
                {
                    //_common.InsertLog("LaunchDirectGame", "username", username, "gameid", "gameid", "langid", langid, "token", token);

                    dynamic res = api.GetSlotGameUrl(username, langid, gameid, token,currency);

                    //_common.InsertLog("ArrowEdge", "LaunchDirectGame", "request", api.request, "response", api.response);

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

        private ArrowEdgeConnector.GameList GetGameList(string gameName = "", string platform = "Mobile")
        {
            try
            {
                ArrowEdgeConnector.GameList gameList = new ArrowEdgeConnector.GameList();
                List<ArrowEdgeConnector.Games> foundGameList = new List<ArrowEdgeConnector.Games>();
                foundGameList = api.GetGameList().Games.Where(x => x.friendlyName.Contains(gameName) && x.platform==platform).OrderByDescending(x => x.friendlyName).ToList();
                gameList.Games = foundGameList;
                return gameList;
            }
            catch (Exception ex)
            {
                _common.LogError("Record " + " " + DateTime.Now.ToString() + " " + ex.Message.ToString(), P_FilePrefix);
                throw;
            }
        }

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
                        _common.LogError("ArrowEdge/SaveGameListToDatabase" + " " + DateTime.Now.ToString() + " " + string.Format("Entity of type \"{0}\" in state \"{1}\" has the following validation errors:",
                                                        eve.Entry.Entity.GetType().Name, eve.Entry.State), P_FilePrefix);
                        foreach (var ve in eve.ValidationErrors)
                        {
                            _common.LogError("ArrowEdge/SaveGameListToDatabase" + " " + DateTime.Now.ToString() + " " + string.Format("- Property: \"{0}\", Error: \"{1}\"",
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
    }
}