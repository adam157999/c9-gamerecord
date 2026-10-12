using Newtonsoft.Json;

namespace GameRecord.Gamess.BTG.Models
{
    public class WithdrawAmountRequest : BaseRequest
    {
        [JsonProperty("username")]
        public string Username { get; set; }

        [JsonProperty("external_order_id")]
        public string ExternalOrderId { get; set; }

        [JsonProperty("take_all")]
        public bool? TakeAll { get; set; }

        public override string Function()
        {
            return "user/withdraw_amount";
        }
    }

    public class WithdrawAmountData
    {
        [JsonProperty("balance")]
        public decimal Balance { get; set; }

        [JsonProperty("currency")]
        public string Currency { get; set; }

        [JsonProperty("order_id")]
        public string OrderId { get; set; }
    }

    public class WithdrawAmountResponse : BaseResponse
    {
        [JsonProperty("data")]
        public WithdrawAmountData Data { get; set; }
    }
}
