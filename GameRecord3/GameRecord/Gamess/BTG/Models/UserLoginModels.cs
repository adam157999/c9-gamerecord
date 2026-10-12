using Newtonsoft.Json;
using System;

namespace GameRecord.Gamess.BTG.Models
{
    #region User Login Request
    public class UserLoginRequest : BaseRequest
    {
        [JsonProperty("username")]
        public string username { get; set; }

        [JsonProperty("game_code")]
        public string game_code { get; set; }

        [JsonProperty("plat_username")]
        public string plat_username { get; set; }

        [JsonProperty("lang")]
        public string lang { get; set; }

        [JsonProperty("back_url")]
        public string back_url { get; set; }

        [JsonProperty("routing")]
        public string routing { get; set; }

        [JsonProperty("cur_type")]
        public int? cur_type { get; set; }

        [JsonProperty("demo")]
        public bool? demo { get; set; }

        [JsonProperty("device")]
        public string device { get; set; }

        public override string Function()
        {
            return "agent/user_login";
        }
    }
    #endregion

    #region User Login Response
    public class UserLoginResponse : BaseResponse
    {
        [JsonProperty("data")]
        public UserLoginData Data { get; set; }
    }

    public class UserLoginData
    {
        [JsonProperty("game_url")]
        public string game_url { get; set; }
    }
    #endregion
}
