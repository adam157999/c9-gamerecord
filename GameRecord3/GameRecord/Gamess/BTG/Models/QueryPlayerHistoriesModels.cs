using Newtonsoft.Json;

namespace GameRecord.Gamess.BTG.Models
{
    public class QueryPlayerHistoriesRequest : BaseRequest
    {
        [JsonProperty("username")]
        public string Username { get; set; }

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
            return "game/query_player_histories";
        }
    }

    public class QueryPlayerHistoriesResponse : BaseResponse
    {
        [JsonProperty("data")]
        public GameHistoriesData Data { get; set; }
    }
}
