using GameRecord.Controllers.V2;
using GameRecord.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using System.Web.Script.Serialization;

namespace GameRecord.Controllers
{
    public class CQ9FishController : Controller
    {
        public static readonly object MyLock = new object();
        private CQ9FishConnector _connector;
        private CQ9FishConnector api = new CQ9FishConnector(environment: BaseConnector.GameEnvironment.PROD);
        private Common _common = new Common();
        private string gameHall = "cq9";
        private string P_GamePrefix = "CQ9FISH";
        public string AzureStorage = ConfigurationManager.AppSettings["AzureStorageAccount_ConnectionString"];


        private CQ9FishConnector connector
        {
            get
            {
                if (_connector == null)
                {
                    _connector = new CQ9FishConnector();
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
            var vm = new CQ9FishLobbyVM()
            {
                lang = lang,
                username = playerid,
                currency = currency,
                games = GetGameList(null, lang, null)
            };
            return View(vm);
        }

        [HttpPost]
        public JsonResult SearchGame(string gameName, string lang, string gameCategory)
        {
            var gameList = GetGameList(gameName, lang, gameCategory);
            return Json(gameList, JsonRequestBehavior.AllowGet);
        }

        private List<CQ9FishLobbyGame> GetGameList(string gameName, string lang, string gameCategory)
        {   
            try
            {
                using (BingoConn db = new BingoConn())
                {
                    var qList = db.CQ9FishLobbyGame.AsQueryable();

                    if (!string.IsNullOrWhiteSpace(gameName))
                    {
                        if (lang?.ToUpper() == "EN")
                            qList = qList.Where(x => x.GameName.Contains(gameName));
                        else
                            qList = qList.Where(x => x.GameNameCn.Contains(gameName));
                    }

                    if (gameCategory == "Hot")
                        qList = qList.Where(x => x.IsHot == true).OrderByDescending(x => x.HotSeq);
                    else if (gameCategory == "Favourite")
                        qList = qList.Where(x => x.IsFavorite == true).OrderByDescending(x => x.FavoriteSeq);
                    else if (gameCategory == "New")
                        qList = qList.Where(x => x.IsNew == true).OrderByDescending(x => x.NewSeq);
                    else
                        qList = qList.Where(x => x.IsAll == true).OrderByDescending(x => x.AllSeq);

                    return qList.ToList();
                }
            }
            catch (Exception ex)
            {
                _common.LogError("Error at CQ9Fish/GetGameList: " + ex.Message, "CQ9FISH");
                return new List<CQ9FishLobbyGame>();
            }
        }


        public ActionResult GetGameHall()
        {
            try
            {
                var gameHalls = api.GetGameProviderList();
                if (gameHalls != null )
                {
                    return Json(gameHalls, JsonRequestBehavior.AllowGet);
                }
                else
                {
                    return new HttpStatusCodeResult(HttpStatusCode.BadGateway);  // Bad Gateway = 502
                }
            }
            catch (Exception ex)
            {
                _common.LogError("CQ9Fish/GetGameHall" + " " + DateTime.Now.ToString() + " " + string.Format("{0}", "") + " " + ex.Message.ToString(), "CQ9Fish");
                return new HttpStatusCodeResult(HttpStatusCode.InternalServerError);  // Internal Server Error = 500
            }
        }

        public ActionResult SyncGameList()
        {
            try
            {
                var games = api.GetGameList(gameHall); // Use the GetGameList method with a default game hall

                if (games != null && games.Count > 0)
                {
                    using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["BingoConn1"].ToString()))
                    {
                        con.Open();
                        SqlCommand cmd = new SqlCommand("sp_CQ9FishGameList_SIUD", con);
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Type", "0");
                        cmd.Parameters.AddWithValue("@JsonData", JsonConvert.SerializeObject(games));
                        cmd.ExecuteNonQuery();
                        con.Close();
                    }

                    SyncImages();

                    return new HttpStatusCodeResult(HttpStatusCode.OK);  // OK = 200
                }
                else
                {
                    return new HttpStatusCodeResult(HttpStatusCode.BadGateway);  // Bad Gateway = 502
                }
            }
            catch (Exception ex)
            {
                _common.LogError("CQ9Fish/SyncGameList" + " " + DateTime.Now.ToString() + " " + string.Format("{0}", "") + " " + ex.Message.ToString(), "CQ9Fish");
                return new HttpStatusCodeResult(HttpStatusCode.InternalServerError);  // Internal Server Error = 500
            }
        }

        private void SyncImages()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["BingoConn1"].ToString()))
                {
                    con.Open();
                    SqlCommand cmd = new SqlCommand("sp_CQ9FishGameList_SIUD", con);
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@Type", "2");
                    cmd.Parameters.AddWithValue("@StoragePrefix", "cq9live");

                    SqlDataAdapter adapter = new SqlDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);

                    if (dt != null && dt.Rows.Count > 0)
                    {
                        foreach (DataRow dr in dt.Rows)
                        {
                            string message = "";
                            string gameId = dr["GameID"].ToString();
                            string sourceUrl = dr["GameIcon"].ToString();
                            var fileName = DateTime.Now.ToString("yyyyMMddHHmmssfff") + Path.GetExtension(sourceUrl);
                            if (Storage.UploadFile(AzureStorage, "images-lobby-cq9fish", fileName, sourceUrl, ref message))
                            {
                                SqlCommand cmd1 = new SqlCommand("sp_CQ9FishGameList_SIUD", con);
                                cmd1.CommandType = CommandType.StoredProcedure;
                                cmd1.Parameters.AddWithValue("@Type", "1");
                                cmd1.Parameters.AddWithValue("@GameIcon", message);
                                cmd1.Parameters.AddWithValue("@GameID", gameId);
                                cmd1.ExecuteNonQuery();
                            }
                            else
                            {
                                break;
                            }
                        }
                    }

                    con.Close();
                }
            }
            catch (Exception ex)
            {
                _common.LogError("CQ9Fish/SyncImages" + " " + DateTime.Now.ToString() + " " + string.Format("{0}", "") + " " + ex.Message.ToString(), "CQ9Fish");
            }
        }

        public ActionResult SyncGameImages()
        {
            try
            {
                SyncImages();
                return new HttpStatusCodeResult(HttpStatusCode.OK);  // OK = 200
            }
            catch (Exception ex)
            {
                _common.LogError("CQ9Fish/SyncGameImages" + " " + DateTime.Now.ToString() + " " + string.Format("{0}", "") + " " + ex.Message.ToString(), "CQ9Fish");
                return Content(ex.Message.ToString());  // Internal Server Error = 500
            }
        }

        public string LaunchDirectGame(string playerid, string ip, string gameid, string langid, string currency, bool isMobile = false)
        {
            //_common.InsertLog("LaunchDirectGame", "username", username, "gameid", "gameid", "langid", langid);

            lock (MyLock)
            {
                try
                {
                    connector = new CQ9FishConnector(playerid, currency, ip);
                    dynamic res = connector.GetFishGameUrl(langid, gameid, gameHall,isMobile);

                    //_common.InsertLog(gameid, "LaunchDirectGame", "request", api.request, "response", api.response);

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
                    _common.LogError("LaunchDirectGame: " + " " + string.Format("{0}|{1}", gameid, langid) + " " + ex.Message.ToString(), P_GamePrefix);
                    return null;
                }
            }
        }
    }
}