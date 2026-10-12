using Newtonsoft.Json;

namespace GameRecord.Gamess.BTG.Models
{
    public class QueryPlayerFinancesRequest : BaseRequest
    {
        [JsonProperty("username")]
        public string Username { get; set; }

        [JsonProperty("start_time")]
        public string StartTime { get; set; }

        [JsonProperty("end_time")]
        public string EndTime { get; set; }

        public override string Function()
        {
            return "game/query_player_finances";
        }
    }

    public class PlayerFinancesData
    {
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

    public class QueryPlayerFinancesResponse : BaseResponse
    {
        [JsonProperty("data")]
        public PlayerFinancesData Data { get; set; }
    }
}
