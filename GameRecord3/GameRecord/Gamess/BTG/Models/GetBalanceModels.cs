using Newtonsoft.Json;

namespace GameRecord.Gamess.BTG.Models
{
    public class GetBalanceRequest : BaseRequest
    {
        [JsonProperty("username")]
        public string Username { get; set; }

        public override string Function()
        {
            return "user/get_balance";
        }
    }

    public class GetBalanceData
    {
        [JsonProperty("balance")]
        public decimal Balance { get; set; }

        [JsonProperty("currency")]
        public string Currency { get; set; }
    }

    public class GetBalanceResponse : BaseResponse
    {
        [JsonProperty("data")]
        public GetBalanceData Data { get; set; }
    }
}
