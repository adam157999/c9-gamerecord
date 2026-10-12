using Newtonsoft.Json;

namespace GameRecord.Gamess.BTG.Models
{
    public class QueryBettingRecordRequest : BaseRequest
    {
        [JsonProperty("order_id")]
        public string OrderId { get; set; }

        [JsonProperty("lang")]
        public string Lang { get; set; }

        public override string Function()
        {
            return "game/query_betting_record";
        }
    }

    public class QueryBettingRecordResponse : BaseResponse
    {
        [JsonProperty("data")]
        public GameHistory Data { get; set; }
    }
}
