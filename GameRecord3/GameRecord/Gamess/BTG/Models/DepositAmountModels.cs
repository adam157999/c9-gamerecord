using Newtonsoft.Json;

namespace GameRecord.Gamess.BTG.Models
{
    public class DepositAmountRequest : BaseRequest
    {
        [JsonProperty("username")]
        public string Username { get; set; }

        [JsonProperty("deposit_amount")]
        public string DepositAmount { get; set; }

        [JsonProperty("external_order_id")]
        public string ExternalOrderId { get; set; }

        public override string Function()
        {
            return "user/deposit_amount";
        }
    }

    public class DepositAmountData
    {
        [JsonProperty("balance")]
        public decimal Balance { get; set; }

        [JsonProperty("currency")]
        public string Currency { get; set; }

        [JsonProperty("order_id")]
        public string OrderId { get; set; }
    }

    public class DepositAmountResponse : BaseResponse
    {
        [JsonProperty("data")]
        public DepositAmountData Data { get; set; }
    }
}
