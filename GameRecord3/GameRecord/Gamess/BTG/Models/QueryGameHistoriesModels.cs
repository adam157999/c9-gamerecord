using Newtonsoft.Json;
using System.Collections.Generic;

namespace GameRecord.Gamess.BTG.Models
{
    public class GameHistory
    {
        [JsonProperty("round_id")]
        public string RoundId { get; set; }

        [JsonProperty("order_id")]
        public string OrderId { get; set; }

        [JsonProperty("transaction_type")]
        public int TransactionType { get; set; }

        [JsonProperty("special")]
        public List<string> Special { get; set; }

        [JsonProperty("bet")]
        public decimal Bet { get; set; }

        [JsonProperty("win")]
        public decimal Win { get; set; }

        [JsonProperty("valid_bet")]
        public decimal ValidBet { get; set; }

        [JsonProperty("rake_amount")]
        public decimal RakeAmount { get; set; }

        [JsonProperty("diff")]
        public decimal Diff { get; set; }

        [JsonProperty("username")]
        public string Username { get; set; }

        [JsonProperty("player_name")]
        public string PlayerName { get; set; }

        [JsonProperty("game_vendor_name")]
        public string GameVendorName { get; set; }

        [JsonProperty("game_code")]
        public string GameCode { get; set; }

        [JsonProperty("game_type")]
        public string GameType { get; set; }

        [JsonProperty("detail_url")]
        public string DetailUrl { get; set; }

        [JsonProperty("cn_detail_url")]
        public string CnDetailUrl { get; set; }

        [JsonProperty("bet_time")]
        public string BetTime { get; set; }

        [JsonProperty("end_time")]
        public string EndTime { get; set; }

        [JsonProperty("result_time")]
        public string ResultTime { get; set; }

        [JsonProperty("data_updated_time")]
        public string DataUpdatedTime { get; set; }

        [JsonProperty("balance_after")]
        public string BalanceAfter { get; set; }
    }

    public class GameHistoriesData
    {
        [JsonProperty("data")]
        public List<GameHistory> Data { get; set; }

        [JsonProperty("bet_count")]
        public int BetCount { get; set; }

        [JsonProperty("total_page")]
        public int TotalPage { get; set; }

        [JsonProperty("total_record")]
        public int TotalRecord { get; set; }
    }

    public class QueryGameHistoriesRequest : BaseRequest
    {
        [JsonProperty("start_time")]
        public string StartTime { get; set; }

        [JsonProperty("end_time")]
        public string EndTime { get; set; }

        [JsonProperty("page")]
        public int Page { get; set; }

        [JsonProperty("page_size")]
        public int PageSize { get; set; }

        [JsonProperty("game_type")]
        public string GameType { get; set; }

        public override string Function()
        {
            return "game/query_game_histories";
        }
    }

    public class QueryGameHistoriesResponse : BaseResponse
    {
        [JsonProperty("data")]
        public GameHistoriesData Data { get; set; }
    }
}
