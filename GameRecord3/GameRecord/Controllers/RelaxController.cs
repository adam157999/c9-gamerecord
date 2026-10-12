using System;
using System.Linq;
using System.Web.Mvc;
using System.Net;
using System.IO;
using GameRecord.Models;
using System.Data.Entity.Migrations;
using System.Data.Entity.Validation;
using static GameRecord.Controllers.BaseConnector;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Dynamic;
using System.Web;
using System.Web.Script.Serialization;
using GameRecord.Gamess.Relax;
using System.Configuration;
using System.Data.SqlClient;
using System.Data;

namespace GameRecord.Controllers
{
    public class RelaxController : Controller
    {
        public static readonly object MyLock = new object();
        private string P_GamePrefix = "RELAX";
        //private string P_FilePrefix = "RELAX";
        private string Currency = "MYR";
        private string P_DTFormat = "yyyy-MM-ddTHH:mm:ss.fffZ";
        Common _common = new Common();
        private RelaxConnector api = new RelaxConnector(Environment: BaseConnector.GameEnvironment.PROD);
        public string AzureStorage = ConfigurationManager.AppSettings["AzureStorageAccount_ConnectionString"];

        //http://localhost:51148/Relax/Lobby
        public ActionResult Lobby(string username, string langid, string currency)
        {
            ProviderLobbyVM model = new ProviderLobbyVM
            {
                username = username,
                lang = langid,
                currency = currency,
                gameList = GetGameList(null, langid, null),
            };
            return View(model);
        }

        public JsonResult SearchGame(string gameName, string lang, string gameCategory)
        {
            List<ProviderLobbyGame> gameList = GetGameList(gameName, lang, gameCategory);
            return Json(gameList, JsonRequestBehavior.AllowGet);
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
                _common.LogError("Error at Relax/GetGameList: " + ex.Message.ToString(), P_GamePrefix);
                return null;
            }
        }

        //public JsonResult SearchGame(string gameName, string lang, string gameCategory)
        //{
        //    List<RelaxLobbyGame> gameList = GetGameList(gameName, lang, gameCategory);
        //    return Json(gameList, JsonRequestBehavior.AllowGet);
        //}

        //private List<RelaxLobbyGame> GetGameList(string gameName, string lang, string gameCategory)
        //{
        //    try
        //    {
        //        using (BingoConn db = new BingoConn())
        //        {
        //            var q_list = db.RelaxLobbyGames.Where(x => true);

        //            if (!string.IsNullOrWhiteSpace(gameName))
        //            {
        //                if (lang?.ToUpper() == "EN")
        //                    q_list = q_list.Where(x => x.GameName.Contains(gameName));
        //                else
        //                    q_list = q_list.Where(x => x.GameNameCn.Contains(gameName));
        //            }

        //            if (gameCategory == "Hot")
        //                q_list = q_list.Where(x => x.IsHot == true).OrderByDescending(x => x.HotSeq);
        //            else if (gameCategory == "Favourite")
        //                q_list = q_list.Where(x => x.IsFavorite == true).OrderByDescending(x => x.FavoriteSeq);
        //            else if (gameCategory == "New")
        //                q_list = q_list.Where(x => x.IsNew == true).OrderByDescending(x => x.NewSeq);
        //            else
        //                q_list = q_list.Where(x => x.IsAll == true).OrderByDescending(x => x.AllSeq);

        //            return q_list.ToList();
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        _common.LogError("Error at PT/GetGameList: " + ex.Message.ToString(), P_GamePrefix);
        //        return null;
        //    }
        //}

        public string GetBalance(string username)
        {
            lock (MyLock)
            {
                try
                {
                    //_common.LogError("GetBalanceLobby" + " " + DateTime.Now.ToString() + " " + string.Format("{0}", username), P_FilePrefix);

                    dynamic result = api.GetBalanceLobby(username);

                    JsonResult json = new JsonResult
                    {
                        Data = JsonConvert.SerializeObject(result)
                    };

                    return new JavaScriptSerializer().Serialize(json);
                }
                catch (Exception ex)
                {
                    _common.LogError("GetBalance: " + ex.Message.ToString() + " " + username, P_GamePrefix);
                    return null;
                }
            }
        }

        public string LaunchDirectGame(string username, string gameid, string langid, string currency)
        {
            //_common.InsertLog("LaunchDirectGame", "username", username, "gameid", "gameid", "langid", langid);

            lock (MyLock)
            {
                try
                {
                    dynamic res = api.GetSlotGameUrl(username, langid, gameid, currency);

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

        private bool SaveGameListToDatabase(List<GetGameListInnerResponse> model)
        {
            bool flag = false;
            using (BingoConn db = new BingoConn())
            {
                try
                {
                    if (model.Count == db.RelaxGameLists.Count()) { flag = true; return flag; }
                    for (int index = 0; index < model.Count; index++)
                    {
                        RelaxGameList game = new RelaxGameList()
                        {
                            GameID = model[index].game_id.ToString(),
                            GameName = model[index].game_name,
                            GameNameCn = model[index].game_name_cn,
                            ReleaseDate = model[index].release_date,
                            Rtp = model[index].rtp,
                            GameIcon = model[index].game_icon,
                            ContentType = model[index].content_type,
                            GameType = model[index].game_type,
                            CreatedDate = DateTime.Now,
                        };
                        db.RelaxGameLists.Add(game);
                        db.SaveChanges();
                        flag = true;
                    }
                }
                catch (Exception ex)
                {
                    _common.LogError("SaveGameListToDatabase: " + ex.Message.ToString(), P_GamePrefix);
                }
            }
            return flag;
        }

        public ActionResult SyncGameList()
        {
            GetGameListResponse games = new GetGameListResponse();

            try
            {
                games = api.GetGamesList();

                if (games != null)
                {
                    using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["BingoConn1"].ToString()))
                    {
                        con.Open();
                        SqlCommand cmd = new SqlCommand("sp_RelaxGameList_SIUD", con);
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Type", "0");
                        cmd.Parameters.AddWithValue("@JsonData", JsonConvert.SerializeObject(games.data));
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
                _common.LogError("Relax/SyncGameList" + " " + DateTime.Now.ToString() + " " + string.Format("{0}", "") + " " + ex.Message.ToString(), "Relax");
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
                    SqlCommand cmd = new SqlCommand("sp_RelaxGameList_SIUD", con);
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
                            if (Storage.UploadFile(AzureStorage, "images-lobby-relax", fileName, sourceUrl, ref message))
                            {
                                SqlCommand cmd1 = new SqlCommand("sp_RelaxGameList_SIUD", con);
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
                _common.LogError("Relax/SyncImages" + " " + DateTime.Now.ToString() + " " + string.Format("{0}", "") + " " + ex.Message.ToString(), "Relax");
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
                _common.LogError("Relax/SyncGameImages" + " " + DateTime.Now.ToString() + " " + string.Format("{0}", "") + " " + ex.Message.ToString(), "Relax");
                return Content(ex.Message.ToString());  // Internal Server Error = 500
            }
        }

        //new
        //http://localhost:51148/Relax/RelaxJobNew
        public string RelaxJobNew()
        {
            try
            {
                lock (MyLock)
                {
                    api = new RelaxConnector(P_GamePrefix, Environment: GameEnvironment.PROD);
                    using (BingoConn db = new BingoConn())
                    {
                        var adm = db.ADMJobDates.Where(s => s.GameProvider == P_GamePrefix).FirstOrDefault();
                        if (adm == null || adm.LastRunTillDate == null)
                            return "Please setup ADMJobDates with LastRunTillDate.";

                        DateTime dtStart = adm.LastRunTillDate.Value;
                        DateTime dtEnd = _common.DtNowTrimMillisecond();

                        int iMax = 5000;
                        int iPage = 1;
                        int iProcessed = 0;
                        bool bEnd = false;
                        bool bError = false;

                        while (!bEnd)
                        {
                            string sStartTime = dtStart.AddHours(-8).AddMinutes(-15).ToString("yyyy-MM-dd HH:mm:ss");
                            string sEndTime = dtEnd.AddHours(-8).ToString("yyyy-MM-dd HH:mm:ss");

                            var result = api.RetrieveTransactionByMinute(sStartTime, sEndTime, Currency, iPage, iMax);
                            if (result == null)
                            {
                                bEnd = true;
                                bError = true;
                                break;
                            }
                            else
                            {
                                if (result.data != null && result.data.Count > 0)
                                {
                                    string id = Guid.NewGuid().ToString();
                                    string s = "insert into RelaxGameMain (Id,Total,PageIndex,PageSize,Error,StartTime,EndTime,CreatedDate) ";
                                    s += "values ('" + id + "'," + result.data.Count + "," + iPage + "," + iMax + ",null,'" + sStartTime + "','" + sEndTime + "',getdate());";
                                    _common.Sql_RunAndCommit(s);

                                    foreach (var r in result.data)
                                    {
                                        string s2 = "insert into RelaxGame (fId,brand_uid,currency,wager_type,amount,before_amount,after_amount,game_id,game_name,round_id,wager_id,jackpot_contribution,jackpot_win,[description],create_time,game_result,tip,CreatedDate) ";
                                        s2 += "select * from (values ('" + id + "'," + _common.F1(r.brand_uid) + "," + _common.F1(r.currency) + "," + _common.F1(r.wager_type) + "," + r.amount + "," + r.before_amount + "," + r.after_amount + "," + _common.F1(r.game_id) + "," + _common.F1(r.game_name) + "," + _common.F1(r.round_id) + "," + _common.F1(r.wager_id) + "," + r.jackpot_contribution + "," + r.jackpot_win + "," + _common.F1(r.description) + ",'" + r.create_time + "'," + _common.F1(r.game_result) + "," + r.tip + ",getdate())) as a(fId,brand_uid,currency,wager_type,amount,before_amount,after_amount,game_id,game_name,round_id,wager_id,jackpot_contribution,jackpot_win,[description],create_time,game_result,tip,CreatedDate) ";
                                        s2 += "where not exists (select * from RelaxGame b with (updlock) where b.wager_id=a.wager_id and b.wager_type=a.wager_type);";
                                        _common.Sql_RunAndCommit(s2);
                                        iProcessed++;
                                    }

                                    if (result.data.Count >= iMax)
                                        iPage++;
                                    else
                                        bEnd = true;
                                }
                                else
                                {
                                    bEnd = true;
                                }
                            }
                        }

                        if (!bError)
                        {
                            adm.LastRunTillDate = dtEnd;
                            db.SaveChanges();
                        }

                        return "RelaxJob has processed " + iProcessed + " record(s) from " + dtStart.ToString("yyyy-MM-dd HH:mm:ss") + " until " + dtEnd.ToString("yyyy-MM-dd HH:mm:ss") + (bError ? " with error." : "");
                    }
                }
            }
            catch (Exception ex)
            {
                _common.LogError("Error at Relax/RelaxJob: " + ex.Message.ToString(), P_GamePrefix);
                return "Error at Relax/RelaxJob: " + ex.Message.ToString();
            }
        }
    }
}