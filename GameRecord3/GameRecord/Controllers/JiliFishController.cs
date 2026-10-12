using GameRecord.Gamess.JiLi;
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
    public class JiliFishController : Controller
    {
        private JiliFishConnector _connector;
        private Common _common = new Common();
        private JiliFishConnector api = new JiliFishConnector(environment: BaseConnector.GameEnvironment.PROD);
        public string AzureStorage = ConfigurationManager.AppSettings["AzureStorageAccount_ConnectionString"];

        private JiliFishConnector connector
        {
            get
            {
                if (_connector == null)
                {
                    _connector = new JiliFishConnector();
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
            var vm = new JiliFishLobbyVM() { 
                lang = lang,
                username = playerid,
                currency = currency,
                Games = GetGameList(null, lang, null)
            };
            return View(vm);
        }

        [HttpPost]
        public JsonResult SearchGame(string gameName, string lang, string gameCategory)
        {
            List<JiLiFishLobbyGame> gameList = GetGameList(gameName, lang, gameCategory);
            return Json(gameList, JsonRequestBehavior.AllowGet);
        }

        private List<JiLiFishLobbyGame> GetGameList(string gameName, string lang, string gameCategory)
        {
            try
            {
                using (BingoConn db = new BingoConn())
                {
                    var q_list = db.JiLiFishLobbyGame.Where(x => true);

                    if (!string.IsNullOrWhiteSpace(gameName))
                    {
                        if (lang?.ToUpper() == "EN")
                            q_list = q_list.Where(x => x.GameName.Contains(gameName));
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
                _common.LogError("Error at JiliFish/GetGameList: " + ex.Message.ToString(), "JILIFISH");
                return new List<JiLiFishLobbyGame>();
            }
        }

        [HttpPost]
        public ActionResult SyncGameList()
        {
            GetGameListDtoResponse games = new GetGameListDtoResponse();
            try
            {
                games = api.GetGameList();
                var value = JsonConvert.SerializeObject(games.Data);
                if (games != null)
                {
                    using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["BingoConn1"].ToString()))
                    {
                        con.Open();
                        SqlCommand cmd = new SqlCommand("sp_JiLiFishGameList_SIUD", con);
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Type", "0");
                        cmd.Parameters.AddWithValue("@JsonData", JsonConvert.SerializeObject(games.Data));
                        cmd.ExecuteNonQuery();
                        con.Close();
                    }

                    SyncImages();

                    //return Json(games);
                    return new HttpStatusCodeResult(HttpStatusCode.OK);  // OK = 200
                }
                else
                {
                    return new HttpStatusCodeResult(HttpStatusCode.BadGateway);  // Bad Gateway = 502
                }
            }
            catch (Exception ex)
            {
                _common.LogError("JiLiFish/SyncGameList" + " " + DateTime.Now.ToString() + " " + string.Format("{0}", "") + " " + ex.Message.ToString(), "JiLiFish");
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
                    SqlCommand cmd = new SqlCommand("sp_JiLiFishGameList_SIUD", con);
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
                            if (Storage.UploadFile(AzureStorage, "images-lobby-jilifish", fileName, sourceUrl, ref message))
                            {
                                SqlCommand cmd1 = new SqlCommand("sp_JiLiFishGameList_SIUD", con);
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
                _common.LogError("JiLiFish/SyncImages" + " " + DateTime.Now.ToString() + " " + string.Format("{0}", "") + " " + ex.Message.ToString(), "JiLiFish");
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
                _common.LogError("JiLiFish/SyncGameImages" + " " + DateTime.Now.ToString() + " " + string.Format("{0}", "") + " " + ex.Message.ToString(), "JiLiFish");
                return Content(ex.Message.ToString());  // Internal Server Error = 500
            }
        }


        [HttpPost]
        public JsonResult LaunchGame([System.Web.Http.FromBody] int gameCode, string playerid, string currency, string ip, string mobile, string Language = "en")
        {
            if (gameCode <= 0)
            {
                return Json(new { success = false, message = "Game code is required." });
            }

            string gameUrl = null;
            connector = new JiliFishConnector(playerid, currency, ip);
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
