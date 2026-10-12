using GameRecord.Gamess.EvoGaming;
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
    public class EVONLCController : Controller
    {
        private string _gameId = "nlc";
        private string P_GamePrefix = "EVONLC";
        private EVONLCConnector _connector;
        private Common _common = new Common();
        private EVONLCConnector api = new EVONLCConnector(Environment: BaseConnector.GameEnvironment.PROD);
        public string AzureStorage = ConfigurationManager.AppSettings["AzureStorageAccount_ConnectionString"];

        private EVONLCConnector connector
        {
            get
            {
                if (_connector == null)
                {
                    _connector = new EVONLCConnector();
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
            ProviderLobbyVM model = new ProviderLobbyVM
            {
                username = playerid,
                lang = lang,
                currency = currency,
                gameList = GetGameList(null, lang, null),
            };

            return View(model);

            //var model = GetGameList(null, lang, null);
            //var vm = new EVONLCGamingLobbyVM()
            //{
            //    lang = lang,
            //    username = playerid,
            //    currency = currency,
            //    games = model
            //};
            //return View(vm);
        }

        [HttpPost]
        public JsonResult SearchGame(string gameName, string lang, string gameCategory)
        {
            List<ProviderLobbyGame> gameList = GetGameList(gameName, lang, gameCategory);
            return Json(gameList, JsonRequestBehavior.AllowGet);
            //List<EVONLCLobbyGame> gameList = GetGameList(gameName, lang, gameCategory);
            //return Json(gameList, JsonRequestBehavior.AllowGet);
        }

        private List<ProviderLobbyGame> GetGameList(string gameName, string lang, string gameCategory)
        {
            try
            {
                using (BingoConn db = new BingoConn())
                {
                    var q_list = db.ProviderLobbyGame.Where(x => x.ProviderName.ToUpper() == P_GamePrefix.ToUpper());

                    if (!string.IsNullOrWhiteSpace(gameName))
                    {
                        if (lang?.ToUpper() == "EN")
                            q_list = q_list.Where(x => x.GameNameEn.Contains(gameName));
                        else
                            q_list = q_list.Where(x => x.GameNameCn.Contains(gameName));
                    }

                    if (gameCategory == "Hot")
                        q_list = q_list.Where(x => x.IsHot == true).OrderBy(x => x.HotSeq);
                    else if (gameCategory == "BonusBuy")
                        q_list = q_list.Where(x => x.IsBonusBuy == true).OrderBy(x => x.BonusBuySeq);
                    else if (gameCategory == "New")
                        q_list = q_list.Where(x => x.IsNew == true).OrderBy(x => x.NewSeq);
                    else
                        q_list = q_list.Where(x => x.IsAll == true).OrderBy(x => x.AllSeq);

                    return q_list.ToList();
                }
            }
            catch (Exception ex)
            {
                _common.LogError("Error at EVONLC/GetGameList: " + ex.Message.ToString(), P_GamePrefix);
                return null;
            }
        }

        //private List<EVONLCLobbyGame> GetGameList(string gameName, string lang, string gameCategory)
        //{
        //    try
        //    {
        //        using (BingoConn db = new BingoConn())
        //        {
        //            var q_list = db.EVONLCLobbyGame.Where(x => true);

        //            if (!string.IsNullOrWhiteSpace(gameName))
        //            {
        //                if (lang?.ToUpper() == "EN")
        //                    q_list = q_list.Where(x => x.TableName.Contains(gameName));
        //                else
        //                    q_list = q_list.Where(x => x.TableNameCn.Contains(gameName));
        //            }

        //            if (gameCategory == "Hot")
        //                q_list = q_list.Where(x => x.IsHot == true).OrderBy(x => x.HotSeq);
        //            else if (gameCategory == "Favourite")
        //                q_list = q_list.Where(x => x.IsFavorite == true).OrderBy(x => x.FavoriteSeq);
        //            else if (gameCategory == "New")
        //                q_list = q_list.Where(x => x.IsNew == true).OrderBy(x => x.NewSeq);
        //            else
        //                q_list = q_list.Where(x => x.IsAll == true).OrderBy(x => x.AllSeq);

        //            return q_list.ToList();
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        _common.LogError("Error at EVONLC/GetGameList: " + ex.Message.ToString(), "EVONLC");
        //        return new List<EVONLCLobbyGame>();
        //    }
        //}

        [HttpPost]
        public ActionResult SyncGameList()
        {
            List<EVONLCLobbyGame> games = new List<EVONLCLobbyGame>();
            try
            {
                games = api.GetGameList(_gameId);
                var value = JsonConvert.SerializeObject(games);
                if (games != null)
                {
                    using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["BingoConn1"].ToString()))
                    {
                        con.Open();
                        SqlCommand cmd = new SqlCommand("sp_EVONLCGameList_SIUD", con);
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
                _common.LogError("EVONLC/SyncGameList" + " " + DateTime.Now.ToString() + " " + string.Format("{0}", "") + " " + ex.Message.ToString(), "EVONLC");
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
                    SqlCommand cmd = new SqlCommand("sp_EVONLCGameList_SIUD", con);
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
                            if (Storage.UploadFile(AzureStorage, "images-lobby-evonlc", fileName, sourceUrl, ref message))
                            {
                                SqlCommand cmd1 = new SqlCommand("sp_EVONLCGameList_SIUD", con);
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
                _common.LogError("EVONLC/SyncImages" + " " + DateTime.Now.ToString() + " " + string.Format("{0}", "") + " " + ex.Message.ToString(), "EVONLC");
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
                _common.LogError("EVONLC/SyncGameImages" + " " + DateTime.Now.ToString() + " " + string.Format("{0}", "") + " " + ex.Message.ToString(), "EVONLC");
                return Content(ex.Message.ToString());  // Internal Server Error = 500
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
            connector = new EVONLCConnector(playerid, currency, ip);
            var res = connector.GetEVONLCGameUrl(Language, gameCode, bool.Parse(mobile.ToLower()));
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