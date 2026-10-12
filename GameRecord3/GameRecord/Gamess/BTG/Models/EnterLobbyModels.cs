using Newtonsoft.Json;

namespace GameRecord.Gamess.BTG.Models
{
    public class EnterLobbyRequest : BaseRequest
    {
        [JsonProperty("username")]
        public string Username { get; set; }

        [JsonProperty("lang")]
        public string Lang { get; set; }

        [JsonProperty("game_type")]
        public string GameType { get; set; }

        [JsonProperty("cur_type")]
        public string CurType { get; set; }

        [JsonProperty("cn_routing")]
        public int? CnRouting { get; set; }

        [JsonProperty("prior_gametype")]
        public string PriorGametype { get; set; }

        [JsonProperty("home_url")]
        public string HomeUrl { get; set; }

        public override string Function()
        {
            return "agent/lobby_login";
        }
    }

    public class EnterLobbyData
    {
        [JsonProperty("lobby_url")]
        public string LobbyUrl { get; set; }
        [JsonProperty("usertoken")]
        public string UserToken { get; set; }
    }

    public class EnterLobbyResponse : BaseResponse
    {
        [JsonProperty("data")]
        public EnterLobbyData Data { get; set; }
    }
}
