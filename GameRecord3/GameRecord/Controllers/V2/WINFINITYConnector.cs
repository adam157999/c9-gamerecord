using GameRecord.Gamess.WINFINITY;
using GameRecord.Gamess.WINFINITY.Models;
using GameRecord.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using vm = GameRecord.Controllers.GameAPIController;

namespace GameRecord.Controllers
{
    /// <summary>
    /// Winfinity (agg.bet) — transfer wallet. Same shape as MTConnector.
    ///
    /// ApiKeys.ApiConfig JSON:
    /// {
    ///   "WFApiUrl": "https://<aggregator-domain>",
    ///   "WFB2bId": "<b2b_company_id uuid>",
    ///   "WFApiKey": "<api key>",
    ///   "WFVendorId": "<vendor uuid>",
    ///   "WFTimeOutSec": "30",
    ///   "WFGameId": "lobby",            // or a specific game_id from game list
    ///   "WFCountry": "MY",
    ///   "WFCurrency": "",               // optional: override currency code sent to Winfinity
    ///   "WFLobbyUrl": "",               // optional: where the game's home/exit button returns
    ///   "WFIsTest": "false",            // true only in UAT
    ///   "WFWalletPerGame": "false"      // true if account is "1 wallet : 1 game" (sends game_id on transfers)
    /// }
    /// </summary>
    public class WINFINITYConnector : BaseConnector
    {
        private string gameID = "WINFINITY";   // must match Games.Code / GameMappings.Game
        private string apiUrl = "";
        private string b2bId = "";
        private string apiKey = "";
        private string vendorId = "";
        private int timeout = 30;
        private string launchGameId = "lobby";
        private string country = "MY";
        private string wfCurrency = "";
        private string lobbyUrl = null;
        private bool isTest = false;
        private bool walletPerGame = false;
        private string clientIp = "";
        private string createdGamePlayerID = null;
        private GameMapping gameMap;
        private WINFINITY api = null;

        private const int UnknownRetryCount = 2;      // extra attempts with SAME transaction id
        private const int UnknownRetryDelayMs = 1500;

        public WINFINITYConnector(string playerID = "", string currencyID = "", string ipAddr = "", GameEnvironment environment = GameEnvironment.PROD) : base(playerID, currencyID, ipAddr, environment)
        {
            clientIp = ipAddr;

            using (BingoConn db = new BingoConn())
            {
                gameMap = db.GameMappings.Where(x => x.Game.ToUpper().Trim() == gameID &&
                                                     x.PlayerID == playerID &&
                                                     x.Currency.ToUpper() == currencyID &&
                                                     x.IsActive == true).FirstOrDefault();

                var apiConfig = CatchData.GetApiKeys(gameID.ToUpper().Trim(), environment.ToString(), currencyID.ToUpper().Trim());
                if (string.IsNullOrEmpty(currencyID)) apiConfig = CatchData.GetApiKeys(gameID.ToUpper().Trim(), environment.ToString());

                if (apiConfig != null)
                {
                    JObject jObj = JObject.Parse(apiConfig.ApiConfig);
                    apiUrl = jObj["WFApiUrl"].ToString();
                    b2bId = jObj["WFB2bId"].ToString();
                    apiKey = jObj["WFApiKey"].ToString();
                    vendorId = jObj["WFVendorId"].ToString();
                    timeout = int.Parse(jObj["WFTimeOutSec"].ToString());
                    launchGameId = Cfg(jObj, "WFGameId", "lobby");
                    country = Cfg(jObj, "WFCountry", "MY");
                    wfCurrency = Cfg(jObj, "WFCurrency", "");
                    lobbyUrl = Cfg(jObj, "WFLobbyUrl", null);
                    isTest = Cfg(jObj, "WFIsTest", "false").Equals("true", StringComparison.OrdinalIgnoreCase);
                    walletPerGame = Cfg(jObj, "WFWalletPerGame", "false").Equals("true", StringComparison.OrdinalIgnoreCase);
                    api = new WINFINITY(apiUrl, b2bId, apiKey, vendorId, timeout);
                }
            }
        }

        private string Currency { get { return string.IsNullOrEmpty(wfCurrency) ? currencyID.ToUpper().Trim() : wfCurrency; } }
        private string GamePlayerID { get { return gameMap != null ? gameMap.GamePlayerID : createdGamePlayerID; } }
        private string TransferGameId { get { return walletPerGame && launchGameId != "lobby" ? launchGameId : null; } }

        // ---------------------------------------------------------------------
        // Create player
        // Winfinity has no create-user API: the player is created on first start game.
        // We call start game once (lobby/configured game) so deposit won't fail "player not found",
        // then save the mapping as usual.
        // ---------------------------------------------------------------------
        public override vm.CheckAndCreateAccountVM CreatePlayer(string loginID, string loginPass, string fullName, string mcUID)
        {
            var res = new vm.CheckAndCreateAccountVM();

            try
            {
                if (!IsValidPlayerId(loginID))
                {
                    res.PlayerID = playerID;
                    res.GamePlayerID = null;
                    res.Game = gameID;
                    res.Message = "Invalid game player id (A-Z a-z 0-9 _ - , max 50): " + loginID;
                    res.Code = 2;
                    InsertLog(gameID, "CreateUser", "Error", res.Message);
                    return res;
                }

                var req = BuildStartGameRequest(loginID, fullName, "en", false);
                if (api.StartGame(req))
                {
                    InsertLog(gameID, "CreateUser", "request", api.LastRequest, "response", api.LastResponse);

                    InsertGameMapping(gameID, loginID, loginPass, mcUID);
                    createdGamePlayerID = loginID;

                    res.PlayerID = playerID;
                    res.GamePlayerID = loginID;
                    res.Game = gameID;
                    res.Message = "Success";
                    res.Code = 0;
                }
                else
                {
                    InsertLog(gameID, "CreateUser", "request", api.LastRequest, "response", api.LastResponse ?? api.ErrorMessage);

                    res.PlayerID = playerID;
                    res.GamePlayerID = null;
                    res.Game = gameID;
                    res.Message = api.ErrorMessage;
                    res.Code = 2;
                }
            }
            catch (Exception ex)
            {
                _common.LogError("CreateUser" + " " + DateTime.Now.ToString() + " " + string.Format("{0}", playerID) + " " + ex.Message.ToString(), gameID);
                InsertLog(gameID, "CreateUser", "Error", JsonConvert.SerializeObject(new { ErrorCode = "999", ErrorDesc = ex.Message.ToString() }));

                res.PlayerID = playerID;
                res.GamePlayerID = loginID;
                res.Game = gameID;
                res.Message = ex.Message.ToString();
                res.Code = 99;
            }

            return res;
        }

        // ---------------------------------------------------------------------
        // Transfer in / out
        // transactionID = TrackerId (or TrackerId-n). It is Winfinity's idempotency key, so on an
        // unknown result (timeout / 5xx / "unknown error") we resend the SAME id: if the first one
        // went through, Winfinity returns the original result instead of moving money twice.
        // If still unknown we return false and GetGameUrl's balance-compare / rollback logic takes over.
        // ---------------------------------------------------------------------
        public override bool GameDeposit(decimal amount, string transactionID, ref Error error)
        {
            var req = new WfTransferRequest(GamePlayerID, transactionID, Currency, WINFINITY.FormatAmount(amount), TransferGameId);
            var ok = SendTransfer("Deposit", req, true);

            // Player not registered on Winfinity side yet -> register via start game, then retry once.
            // New id (suffix "R"): the first one definitely failed, and replaying the same id could
            // just return the stored failure.
            if (!ok && !api.IsUnknown && string.Equals(api.ErrorMessage, "player not found", StringComparison.OrdinalIgnoreCase))
            {
                if (RegisterPlayer())
                {
                    req.transaction_id = transactionID + "R";
                    ok = SendTransfer("Deposit", req, true);
                }
            }

            if (ok) return true;

            error = new Error
            {
                ErrorCode = "-9999",
                ErrorDesc = (api.IsUnknown ? "[UNKNOWN] " : "") + api.ErrorMessage
            };
            return false;
        }

        public override bool GameWithdraw(decimal amount, string transactionID, ref Error error)
        {
            var req = new WfTransferRequest(GamePlayerID, transactionID, Currency, WINFINITY.FormatAmount(amount), TransferGameId);
            if (SendTransfer("Withdraw", req, false)) return true;

            error = new Error
            {
                ErrorCode = "-9999",
                ErrorDesc = (api.IsUnknown ? "[UNKNOWN] " : "") + api.ErrorMessage
            };
            return false;
        }

        private bool SendTransfer(string action, WfTransferRequest req, bool isDeposit)
        {
            for (int attempt = 0; attempt <= UnknownRetryCount; attempt++)
            {
                if (attempt > 0) Thread.Sleep(UnknownRetryDelayMs);

                var ok = isDeposit ? api.Deposit(req) : api.Withdraw(req);
                var logAction = attempt == 0 ? action : action + "Retry" + attempt;
                InsertLog(gameID, logAction, "request", api.LastRequest, "response", api.LastResponse ?? api.ErrorMessage);

                if (ok) return true;
                if (!api.IsUnknown) return false; // definite failure: money did not move
            }
            return false; // still unknown after retries
        }

        // ---------------------------------------------------------------------
        // Balance
        // ---------------------------------------------------------------------
        public override vm.GetBalanceVM GetBalance()
        {
            var res = new vm.GetBalanceVM();

            try
            {
                var balanceRequest = new WfBalanceRequest(GamePlayerID, Currency, TransferGameId);

                if (api.GetBalance(balanceRequest))
                {
                    InsertLog(gameID, "GetBalance", "request", api.LastRequest, "response", api.LastResponse);

                    res.PlayerID = playerID;
                    res.Balance = WINFINITY.ParseAmount(api.GetBalanceResponse.balance.amount);
                    res.Game = gameID;
                    res.Message = "Successfully retrieved balance.";
                    res.Code = 0;
                }
                else
                {
                    InsertLog(gameID, "GetBalance", "request", api.LastRequest, "response", api.LastResponse ?? api.ErrorMessage);

                    // Player never launched yet => provider wallet is empty.
                    if (string.Equals(api.ErrorMessage, "player not found", StringComparison.OrdinalIgnoreCase))
                    {
                        res.PlayerID = playerID;
                        res.Balance = 0;
                        res.Game = gameID;
                        res.Message = "Successfully retrieved balance.";
                        res.Code = 0;
                        return res;
                    }

                    res.PlayerID = playerID;
                    res.Balance = null;
                    res.Game = gameID;
                    res.Message = api.ErrorMessage;
                    res.Code = 2;
                }
            }
            catch (Exception ex)
            {
                _common.LogError("GetBalance" + " " + DateTime.Now.ToString() + " " + string.Format("{0}", playerID) + " " + ex.Message.ToString(), gameID);
                InsertLog(gameID, "GetBalance", "Error", JsonConvert.SerializeObject(new Error { ErrorCode = "999", ErrorDesc = ex.Message.ToString() }));

                res.PlayerID = playerID;
                res.Balance = null;
                res.Game = gameID;
                res.Message = ex.Message.ToString();
                res.Code = 99;
            }

            return res;
        }

        public override decimal? GetGameBalance()
        {
            var balanceRequest = new WfBalanceRequest(GamePlayerID, Currency, TransferGameId);

            if (api.GetBalance(balanceRequest))
            {
                InsertLog(gameID, "GetGameBalance", "request", api.LastRequest, "response", api.LastResponse);
                return WINFINITY.ParseAmount(api.GetBalanceResponse.balance.amount);
            }

            InsertLog(gameID, "GetGameBalance", "request", api.LastRequest, "response", api.LastResponse ?? api.ErrorMessage);

            // Not registered yet => 0, so GetGameUrl's initial-balance check doesn't block the first deposit.
            if (!api.IsUnknown && string.Equals(api.ErrorMessage, "player not found", StringComparison.OrdinalIgnoreCase))
                return 0m;

            return null;
        }

        // ---------------------------------------------------------------------
        // Launch
        // ---------------------------------------------------------------------
        public override vm.GetGameUrlVM GetGameUrl(string langId, bool isMobile = false)
        {
            var res = new vm.GetGameUrlVM();
            var req = BuildStartGameRequest(GamePlayerID, GamePlayerID, langId, isMobile);

            if (api.StartGame(req))
            {
                InsertLog(gameID, "StartGame", "request", api.LastRequest, "response", api.LastResponse);

                if (!string.IsNullOrEmpty(api.StartGameResponse.url))
                {
                    res.PlayerID = playerID;
                    res.Url = api.StartGameResponse.url;
                    res.Game = gameID;
                    res.Code = 0;
                }
                else
                {
                    // Provider returned an HTML page instead of a URL. Our front end expects a URL,
                    // so surface it as a failure (balance stays in game, same as other launch failures).
                    res.PlayerID = playerID;
                    res.Url = null;
                    res.Game = gameID;
                    res.Message = "Provider returned HTML launch, URL expected.";
                    res.Code = 1;
                }
            }
            else
            {
                InsertLog(gameID, "StartGame", "request", api.LastRequest, "response", api.LastResponse ?? api.ErrorMessage);
                res.PlayerID = playerID;
                res.Url = null;
                res.Game = gameID;
                res.Message = api.ErrorMessage;
                res.Code = 1;
            }

            return res;
        }

        // Winfinity has no kick-out API. Return true so withdraw flows are not blocked.
        public override bool KickPlayer(string gamePlayerId, ref Error error)
        {
            InsertLog(gameID, "KickPlayer", "request", gamePlayerId, "response", "Not supported by Winfinity - skipped");
            return true;
        }

        // ---------------------------------------------------------------------
        // Retry: resend the SAME transaction id (idempotent on Winfinity side).
        // Returns Winfinity transaction id on success, null on failure (error set).
        // ---------------------------------------------------------------------
        public override string RetryDeposit(decimal amount, string trackerId, ref Error error)
        {
            if (GameDeposit(amount, trackerId, ref error))
                return api.DepositResponse.transaction_id;
            return null;
        }

        public override string RetryWithdraw(decimal amount, string trackerId, ref Error error)
        {
            if (GameWithdraw(amount, trackerId, ref error))
                return api.WithdrawResponse.transaction_id;
            return null;
        }

        // ---------------------------------------------------------------------
        // Helpers
        // ---------------------------------------------------------------------
        private bool RegisterPlayer()
        {
            var req = BuildStartGameRequest(GamePlayerID, GamePlayerID, "en", false);
            var ok = api.StartGame(req);
            InsertLog(gameID, "RegisterPlayer", "request", api.LastRequest, "response", api.LastResponse ?? api.ErrorMessage);
            return ok;
        }

        private WfStartGameRequest BuildStartGameRequest(string gamePlayerId, string nickname, string langId, bool isMobile)
        {
            var req = new WfStartGameRequest
            {
                game_id = launchGameId,
                is_test = isTest ? (bool?)true : null,
                b2b_session_id = Guid.NewGuid().ToString("N"),
                b2c = new WfPlayer
                {
                    id = gamePlayerId,
                    country = country,
                    nickname = string.IsNullOrEmpty(nickname) ? gamePlayerId : nickname
                },
                balance = new WfStartBalance { currency = Currency },
                browser_session = new WfBrowserSession
                {
                    //ip = string.IsNullOrEmpty(clientIp) ? "127.0.0.1" : clientIp,
                    ip = CleanIp(clientIp),
                    os = isMobile ? "Android" : "Windows",
                    device = isMobile ? "mobile" : "desktop",
                    browser = "Chrome",
                    fingerprint = gamePlayerId,
                    java_enabled = false,
                    language = MapLanguage(langId),
                    color_depth = 24,
                    screen_width = isMobile ? 390 : 1920,
                    screen_height = isMobile ? 844 : 1080,
                    timezone_offset = "08:00:00"
                },
                lobby_url = string.IsNullOrEmpty(lobbyUrl) ? null : lobbyUrl
            };

            if (launchGameId == "lobby")
                req.config = new Dictionary<string, object> { { "vendor_id", vendorId } };

            return req;
        }

        private static string MapLanguage(string langId)
        {
            if (string.IsNullOrEmpty(langId)) return "en";
            var l = langId.Trim().ToLowerInvariant();
            if (l.StartsWith("cn") || l.StartsWith("zh")) return "zh";
            if (l.StartsWith("ms") || l.StartsWith("my") || l.StartsWith("bm")) return "ms";
            return l.Length >= 2 ? l.Substring(0, 2) : "en";
        }

        private static string CleanIp(string ip)
        {
            if (string.IsNullOrWhiteSpace(ip)) return "127.0.0.1";
            ip = ip.Split(',')[0].Trim();                                   // "a, b" from proxies
            if (ip.StartsWith("["))                                         // "[::1]:443"
            {
                int end = ip.IndexOf(']');
                if (end > 0) ip = ip.Substring(1, end - 1);
            }
            else if (ip.Count(c => c == ':') == 1)                          // "1.2.3.4:6785"
            {
                ip = ip.Substring(0, ip.IndexOf(':'));
            }
            if (ip == "::1") return "127.0.0.1";
            System.Net.IPAddress parsed;
            return System.Net.IPAddress.TryParse(ip, out parsed) ? ip : "127.0.0.1";
        }

        private static bool IsValidPlayerId(string id)
        {
            if (string.IsNullOrEmpty(id) || id.Length > 50) return false;
            return id.All(c => (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') || c == '_' || c == '-');
        }

        private static string Cfg(JObject j, string key, string def)
        {
            var t = j[key];
            return t == null || string.IsNullOrWhiteSpace(t.ToString()) ? def : t.ToString().Trim();
        }

        public object GetGameList(bool filterByVendor = true, string type = null)
        {
            if (api == null)
                return new { Code = 8, Message = "WINFINITY ApiKeys config not found for currency " + currencyID, Games = (object)null };

            if (api.GetGameList(filterByVendor ? vendorId : null))
            {
                InsertLog(gameID, "GetGameList", "request", filterByVendor ? vendorId : "", "response", api.LastResponse);
                var all = (api.GameListResponse.games ?? new List<WfGameItem>()).Where(g => !g.is_deleted).ToList();

                var types = all.GroupBy(g => new { g.vendor_id, g.type })
                               .Select(x => new { x.Key.vendor_id, x.Key.type, Count = x.Count() })
                               .OrderBy(x => x.vendor_id).ThenBy(x => x.type).ToList();

                var games = string.IsNullOrEmpty(type) ? all
                    : all.Where(g => (g.type ?? "").IndexOf(type, StringComparison.OrdinalIgnoreCase) >= 0).ToList();

                return new
                {
                    Code = 0,
                    Message = "Success",
                    Types = types,
                    Count = games.Count,
                    Games = games.Select(g => new { g.id, g.title, g.vendor_id, g.type, g.is_demo }).ToList()
                };
            }

            InsertLog(gameID, "GetGameList", "request", filterByVendor ? vendorId : "", "response", api.LastResponse ?? api.ErrorMessage);
            return new { Code = 2, Message = api.ErrorMessage, Games = (object)null };
        }
    }
}
