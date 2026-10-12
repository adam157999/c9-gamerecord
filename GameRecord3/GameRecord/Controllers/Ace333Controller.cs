using GameRecord.Controllers.V2;
using GameRecord.Gamess.Ace333;
using GameRecord.Models;
using System;
using System.Linq;
using System.Web.Mvc;
using static GameRecord.Controllers.BaseConnector;

namespace GameRecord.Controllers
{
    public class Ace333Controller : Controller
    {
        public static readonly object MyLock = new object();
        private string P_GamePrefix = "Ace333";
        Common _common = new Common();
        private Ace333Connector api;

        public string GetIPAddress()
        {
            var result = string.Empty;
            System.Web.HttpRequest request = System.Web.HttpContext.Current.Request;
            string forwardedFor = request.ServerVariables["HTTP_X_FORWARDED_FOR"];
            string remoteAddr = string.IsNullOrWhiteSpace(forwardedFor) ? request.ServerVariables["REMOTE_ADDR"] : forwardedFor.Split(',').Select(s => s.Trim()).First();
            result = String.IsNullOrWhiteSpace(remoteAddr) ? request.UserHostAddress : remoteAddr;
            return result;
        }

        public Ace333Controller()
        {
            api = new Ace333Connector(IPAddr: GetIPAddress(), Environment: BaseConnector.GameEnvironment.PROD);
        }

        public ActionResult Lobby()
        {
            try
            {
                GameListRes model = api.GetGamesList();
                model.Games = model.Games.OrderBy(x => x.gameName).ToList();

                return View(model);
            }
            catch (Exception ex)
            {
                _common.LogError("Error at Ace333Controller/Lobby: " + ex.Message.ToString(), P_GamePrefix);
                return Content("Error at Ace333Controller/Lobby: " + ex.Message.ToString());
            }
        }

        [HttpGet]
        public JsonResult GetProviderLobbyUrl(string langid)
        {
            try
            {
                var gameUrl = api.GetGameUrl(langid);
                if (gameUrl.Url != null)
                    return Json(new { Code = 0, Url = gameUrl.Url });
                else
                    return Json(new { Code = -1, Message = "Failed to get URL." });
            }
            catch (Exception ex)
            {
                _common.LogError("Ace333/GetProviderLobbyUrl: " + ex.Message.ToString(), P_GamePrefix);
                return Json(new { Code = -1, Message = ex.Message.ToString() });
            }
        }

        [HttpGet]
        public JsonResult GetGameLogin(string playerId)
        {
            try
            {
                var result = api.GetGameLogin(playerId);
                if (result != null)
                    return Json(result);
                else
                    return Json(new { Code = -1, Message = "Failed login to game." });
            }
            catch (Exception ex)
            {
                _common.LogError("Ace333/GetGameLogin: " + ex.Message.ToString(), P_GamePrefix);
                return Json(new { Code = -1, Message = ex.Message.ToString() });
            }
        }

        [HttpPost]
        public string Ace333Job()
        {
            try
            {
                lock (MyLock)
                {
                    api = new Ace333Connector(P_GamePrefix, Environment: GameEnvironment.PROD);
                    using (BingoConn db = new BingoConn())
                    {
                        var adm = db.ADMJobDates.Where(s => s.GameProvider == P_GamePrefix).FirstOrDefault();
                        if (adm == null || adm.LastRunTillDate == null)
                            return "Please setup ADMJobDates with LastRunTillDate.";

                        DateTime timeFrom = adm.LastRunTillDate.Value.AddMinutes(-15);
                        DateTime timeTo = _common.DtNowTrimMillisecond();

                        int iProcessed = 0;
                        bool bEnd = false;
                        bool bError = false;

                        while (!bEnd)
                        {
                            var result = api.GetAccountTransactions(timeFrom);
                            if (result == null)
                            {
                                bEnd = true;
                                bError = true;
                                break;
                            }
                            else
                            {
                                if (result.AccountTransactions != null && result.AccountTransactions.Count > 0)
                                {
                                    foreach (var r in result.AccountTransactions)
                                    {
                                        string s = "insert into Ace333Game (referenceID,playerID,extPlayerID,gameID,methodType,playSessionID,status,created,updated,betAmount,winAmount,jackpotModule,jackpotContributionAmt,currency,resultUrl,roundDetails,platform, ProcessedAt) ";
                                        s += "select * from (values (" + _common.F1(r.referenceID) + "," + _common.F3(r.playerID) + "," + _common.F3(r.extPlayerID) + "," + _common.F1(r.gameID) + "," + _common.F1(r.methodType) + "," + _common.F1(r.playSessionID) + "," + _common.F1(r.status) + "," + _common.F2(r.created) + "," + _common.F2(r.updated) + "," + _common.F5(r.betAmount) + "," + _common.F5(r.winAmount) + "," + _common.F3(r.jackpotModule) + "," + _common.F5(r.jackpotContributionAmt) + "," + _common.F1(r.currency) + "," + _common.F1(r.resultUrl) + "," + _common.F1(r.roundDetails) + "," + _common.F1(r.platform) + ", getdate())) ";
                                        s += "as a(referenceID,playerID,extPlayerID,gameID,methodType,playSessionID,status,created,updated,betAmount,winAmount,jackpotModule,jackpotContributionAmt,currency,resultUrl,roundDetails,platform) ";
                                        s += "where not exists (select * from Ace333Game b with (updlock) where b.round_id=a.round_id);";
                                        _common.Sql_RunAndCommit(s);
                                        iProcessed++;
                                    }

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
                            adm.LastRunTillDate = timeTo;
                            db.SaveChanges();
                        }

                        return "Ace333Job has processed " + iProcessed + " record(s) from " + timeFrom.ToString("yyyy-MM-dd HH:mm:ss") + " until " + timeTo.ToString("yyyy-MM-dd HH:mm:ss") + (bError ? " with error." : "");
                    }
                }
            }
            catch (Exception ex)
            {
                _common.LogError("Error at Ace333/Ace333Job: " + ex.Message.ToString(), P_GamePrefix);
                return "Error at Ace333/Ace333Job: " + ex.Message.ToString();
            }
        }
    }
}