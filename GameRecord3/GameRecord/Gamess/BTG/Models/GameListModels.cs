using Newtonsoft.Json;
using System.Collections.Generic;

namespace GameRecord.Gamess.BTG.Models
{
    public class GetGameListRequest : BaseRequest
    {
        [JsonProperty("lang")]
        public string Lang { get; set; }

        [JsonProperty("game_type")]
        public string GameType { get; set; }

        public override string Function()
        {
            return "game/get_game_list";
        }
    }

    public class GameItem
    {
        [JsonProperty("game_code")]
        public string GameCode { get; set; }

        [JsonProperty("game_name")]
        public string GameName { get; set; }

        [JsonProperty("game_type")]
        public string GameType { get; set; }

        [JsonProperty("game_vendor_name")]
        public string GameVendorName { get; set; }

        [JsonProperty("game_vendor_code")]
        public string GameVendorCode { get; set; }
    }

    public class GetGameListResponse : BaseResponse
    {
        [JsonProperty("data")]
        public List<GameItem> Data { get; set; }
    }
}
