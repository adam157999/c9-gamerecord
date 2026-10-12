using GameRecord.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace GameRecord.Controllers
{
    public class ClotPlayController : Controller
    {
        private string _gameId = "clotplay";
        private ClotPlayConnector _connector;
        private Common _common = new Common();
        private ClotPlayConnector api = new ClotPlayConnector(); // Removed the Environment parameter to match the available constructor
        public string AzureStorage = ConfigurationManager.AppSettings["AzureStorageAccount_ConnectionString"];

        private ClotPlayConnector connector
        {
            get
            {
                if (_connector == null)
                {
                    _connector = new ClotPlayConnector();
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
            var model = GetGameList(null, lang, null);
            var vm = new ClotPlayLobbyVM()
            {
                lang = lang,
                username = playerid,
                currency = currency,
                games = model
            };
            return View(vm);
        }

        [HttpPost]
        public JsonResult SearchGame(string gameName, string lang, string gameCategory)
        {
            List<ClotPlayLobbyGame> gameList = GetGameList(gameName, lang, gameCategory);
            return Json(gameList, JsonRequestBehavior.AllowGet);
        }

        private List<ClotPlayLobbyGame> GetGameList(string gameName, string lang, string gameCategory)
        {
            try
            {
                using (BingoConn db = new BingoConn())
                {
                    var qList = db.ClotPlayLobbyGame.AsQueryable();

                    if (!string.IsNullOrWhiteSpace(gameName))
                    {
                        if (lang?.ToUpper() == "EN")
                            qList = qList.Where(x => x.Name.Contains(gameName));
                        else
                            qList = qList.Where(x => x.NameCn.Contains(gameName));
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
                _common.LogError("Error at ClotPlay/GetGameList: " + ex.Message, "CLOTPLAY");
                return new List<ClotPlayLobbyGame>();
            }
        }

        [HttpPost]
        public JsonResult LaunchGame([System.Web.Http.FromBody] string gameCode, string playerid, string currency, string ip, string mobile, string Language = "en")
        {
            if (string.IsNullOrEmpty(gameCode))
            {
                return Json(new { success = false, message = "Game code is required." });
            }

            string gameUrl = null;
            connector = new ClotPlayConnector(playerid, currency, ip);
            var res = connector.GetClotPlayGameUrl(Language, gameCode, bool.Parse(mobile.ToLower()));
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
        public ActionResult SyncGameList()
        {
            List<ClotPlayLobbyGame> games = new List<ClotPlayLobbyGame>();
            try
            {
                games = api.GetGameList();
                var value = JsonConvert.SerializeObject(games);
                if (games != null)
                {
                    using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["BingoConn1"].ToString()))
                    {
                        con.Open();
                        SqlCommand cmd = new SqlCommand("sp_ClotPlayGameList_SIUD", con);
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
                _common.LogError("ClotPlay/SyncGameList" + " " + DateTime.Now.ToString() + " " + string.Format("{0}", "") + " " + ex.Message.ToString(), "CLOTP");
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
                    SqlCommand cmd = new SqlCommand("sp_ClotPlayGameList_SIUD", con);
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@Type", "2");
                    cmd.Parameters.AddWithValue("@StoragePrefix", "c9live");

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
                            if (Storage.UploadFile(AzureStorage, "images-lobby-clotplay", fileName, sourceUrl, ref message))
                            {
                                SqlCommand cmd1 = new SqlCommand("sp_ClotPlayGameList_SIUD", con);
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
                _common.LogError("ClotPlay/SyncImages" + " " + DateTime.Now.ToString() + " " + string.Format("{0}", "") + " " + ex.Message.ToString(), "CLOTP");
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
                _common.LogError("ClotPlay/SyncGameImages" + " " + DateTime.Now.ToString() + " " + string.Format("{0}", "") + " " + ex.Message.ToString(), "CLOTP");
                return Content(ex.Message.ToString());  // Internal Server Error = 500
            }
        }
    }
}