using Newtonsoft.Json;

namespace GameRecord.Gamess.BTG.Models
{
    public class UserLogoutRequest : BaseRequest
    {
        [JsonProperty("username")]
        public string Username { get; set; }

        public override string Function()
        {
            return "user/logout";
        }
    }

    public class UserLogoutResponse : BaseResponse
    {
        [JsonProperty("data")]
        public object Data { get; set; }
    }
}
