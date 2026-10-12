using Newtonsoft.Json;

namespace GameRecord.Gamess.BTG.Models
{
    public abstract class BaseRequest
    {
        [JsonProperty("account_id")]
        public string AccountId { get; set; }

        [JsonProperty("check_code")]
        public string CheckCode { get; set; }

        public abstract string Function();
    }

    public class StatusResponse
    {
        [JsonProperty("code")]
        public int Code { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("time")]
        public string Time { get; set; }
    }

    public class BaseResponse
    {
        [JsonProperty("status")]
        public StatusResponse Status { get; set; }
    }
}
