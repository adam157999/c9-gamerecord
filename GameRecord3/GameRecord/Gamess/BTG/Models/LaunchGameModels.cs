using Newtonsoft.Json;

namespace GameRecord.Gamess.BTG.Models
{
    public class LaunchGameRequest : BaseRequest
    {
        [JsonProperty("username")]
        public string Username { get; set; }

        [JsonProperty("game_code")]
        public string GameCode { get; set; }

        [JsonProperty("lang")]
        public string Lang { get; set; }

        [JsonProperty("cur_type")]
        public string CurType { get; set; }

        [JsonProperty("demo")]
        public int? Demo { get; set; }

        [JsonProperty("home_url")]
        public string HomeUrl { get; set; }

        public override string Function()
        {
            return "user/launch_game";
        }
    }

    public class LaunchGameData
    {
        [JsonProperty("url")]
        public string Url { get; set; }
    }

    public class LaunchGameResponse : BaseResponse
    {
        [JsonProperty("data")]
        public LaunchGameData Data { get; set; }
    }
}
