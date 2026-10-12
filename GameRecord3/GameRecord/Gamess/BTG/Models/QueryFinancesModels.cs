using Newtonsoft.Json;
using System.Collections.Generic;

namespace GameRecord.Gamess.BTG.Models
{
    public class QueryFinancesRequest : BaseRequest
    {
        [JsonProperty("start_time")]
        public string StartTime { get; set; }

        [JsonProperty("end_time")]
        public string EndTime { get; set; }

        [JsonProperty("page")]
        public int Page { get; set; }

        [JsonProperty("page_size")]
        public int PageSize { get; set; }

        public override string Function()
        {
            return "game/query_finances";
        }
    }

    public class FinanceItem
    {
        [JsonProperty("username")]
        public string Username { get; set; }

        [JsonProperty("player_name")]
        public string PlayerName { get; set; }

        [JsonProperty("bet_count")]
        public int BetCount { get; set; }

        [JsonProperty("bet_amount")]
        public decimal BetAmount { get; set; }

        [JsonProperty("valid_amount")]
        public decimal ValidAmount { get; set; }

        [JsonProperty("rake_amount")]
        public decimal RakeAmount { get; set; }

        [JsonProperty("reward_amount")]
        public decimal RewardAmount { get; set; }

        [JsonProperty("win_amount")]
        public decimal WinAmount { get; set; }
    }

    public class FinancesData
    {
        [JsonProperty("data")]
        public List<FinanceItem> Data { get; set; }

        [JsonProperty("total_page")]
        public int TotalPage { get; set; }

        [JsonProperty("total_record")]
        public int TotalRecord { get; set; }
    }

    public class QueryFinancesResponse : BaseResponse
    {
        [JsonProperty("data")]
        public FinancesData Data { get; set; }
    }
}
