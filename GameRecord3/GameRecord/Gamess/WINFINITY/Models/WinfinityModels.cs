using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GameRecord.Gamess.WINFINITY.Models
{
    public class WfTimestamp
    {
        [JsonProperty("seconds")] public long seconds { get; set; }
        [JsonProperty("nanos")] public int nanos { get; set; }
    }

    public class WfErrorResponse
    {
        [JsonProperty("error")] public string error { get; set; }
    }

    // ---------------- Token ----------------
    public class WfTokenRequest
    {
        public WfTokenRequest(string b2bId, string apiKey) { b2b_id = b2bId; api_key = apiKey; }
        public string b2b_id { get; set; }
        public string api_key { get; set; }
    }

    public class WfTokenResponse
    {
        public string token { get; set; }
        public WfTimestamp expired_at { get; set; }
    }

    // ---------------- Game list ----------------
    public class WfGameListResponse
    {
        public List<WfGameItem> games { get; set; }
    }

    public class WfGameItem
    {
        public string id { get; set; }
        public string title { get; set; }
        public string vendor_id { get; set; }
        public string type { get; set; }
        public JObject metadata { get; set; }
        public bool is_demo { get; set; }
        public bool is_deleted { get; set; }
    }

    // ---------------- Start game ----------------
    public class WfStartGameRequest
    {
        /// <summary>Game id from game list, or "lobby" (then config.vendor_id is required).</summary>
        public string game_id { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)] public bool? is_test { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)] public string b2b_session_id { get; set; }

        public WfPlayer b2c { get; set; }

        /// <summary>Transfer wallet: currency only. amount is for seamless wallet.</summary>
        public WfStartBalance balance { get; set; }

        public WfBrowserSession browser_session { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)] public Dictionary<string, object> config { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)] public string lobby_url { get; set; }
    }

    public class WfPlayer
    {
        /// <summary>^[A-Za-z0-9_-]+$ max 50. Returned as b2c_id in every other call.</summary>
        public string id { get; set; }
        /// <summary>ISO 3166-1 alpha-2, e.g. "MY".</summary>
        public string country { get; set; }
        public string nickname { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)] public int? int_id { get; set; }
    }

    public class WfStartBalance
    {
        public string currency { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)] public decimal? amount { get; set; }
    }

    public class WfBrowserSession
    {
        public string ip { get; set; }
        public string os { get; set; } = "unknown";
        /// <summary>"desktop" or "mobile".</summary>
        public string device { get; set; } = "desktop";
        /// <summary>max 50 chars</summary>
        public string browser { get; set; } = "unknown";
        public string fingerprint { get; set; } = "unknown";
        public bool java_enabled { get; set; }
        /// <summary>exactly 2 chars</summary>
        public string language { get; set; } = "en";
        public int color_depth { get; set; } = 24;
        public int screen_height { get; set; }
        public int screen_width { get; set; }
        /// <summary>"HH:MM:SS"</summary>
        public string timezone_offset { get; set; } = "08:00:00";
    }

    public class WfStartGameResponse
    {
        public string game_session_id { get; set; }
        public bool is_test { get; set; }
        public string b2c_id { get; set; }
        public string b2b_session_id { get; set; }
        public string url { get; set; }
        public string html { get; set; }
    }

    public class WfStartDemoGameRequest
    {
        public string game_id { get; set; }
        public WfDemoPlayer b2c { get; set; }
        public WfDemoBrowserSession browser_session { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)] public string lobby_url { get; set; }
    }

    public class WfDemoPlayer
    {
        public string id { get; set; }
    }

    public class WfDemoBrowserSession
    {
        public string language { get; set; } = "en";
    }

    // ---------------- Transfer ----------------
    public class WfTransferRequest
    {
        public WfTransferRequest(string b2cId, string transactionId, string currency, string amount, string gameId = null)
        {
            b2c_id = b2cId; transaction_id = transactionId; this.currency = currency; this.amount = amount; game_id = gameId;
        }

        public string b2c_id { get; set; }
        /// <summary>Our id (TrackerId). Idempotency key — same id on retry.</summary>
        public string transaction_id { get; set; }
        public string currency { get; set; }
        /// <summary>Decimal string, invariant culture.</summary>
        public string amount { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)] public string game_session_id { get; set; }
        /// <summary>Only for accounts configured as "1 wallet : 1 game".</summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)] public string game_id { get; set; }
    }

    public class WfBalanceRequest
    {
        public WfBalanceRequest(string b2cId, string currency, string gameId = null)
        {
            b2c_id = b2cId; this.currency = currency; game_id = gameId;
        }

        public string b2c_id { get; set; }
        public string currency { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)] public string game_session_id { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)] public string game_id { get; set; }
    }

    public class WfBalance
    {
        public string currency { get; set; }
        public string amount { get; set; }
    }

    public class WfTransferResponse
    {
        /// <summary>Aggregator transaction id.</summary>
        public string transaction_id { get; set; }
        public WfBalance balance { get; set; }
    }

    public class WfBalanceResponse
    {
        public string b2c_id { get; set; }
        public WfBalance balance { get; set; }
    }

    // ---------------- Report ----------------
    public class WfReportRequest
    {
        /// <summary>UTC "yyyy-MM-ddTHH:00:00Z"</summary>
        public string start_time { get; set; }
        /// <summary>UTC, max 24h after start, at least 1h before now</summary>
        public string end_time { get; set; }
        public string vendor_id { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)] public int? page { get; set; }
    }

    public class WfReportResponse
    {
        public List<WfReportItem> items { get; set; }
        public WfPagination pagination { get; set; }
    }

    public class WfReportItem
    {
        public string b2c_id { get; set; }
        public string currency { get; set; }
        public string total_bet { get; set; }
        public string total_win { get; set; }
        public string total_turnover { get; set; }
        public string total_win_lose { get; set; }
    }

    public class WfPagination
    {
        public int page { get; set; }
        public int total { get; set; }
        public int per_page { get; set; }
    }
}
