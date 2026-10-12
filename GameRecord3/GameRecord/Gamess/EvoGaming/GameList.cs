using Newtonsoft.Json;
using System.Collections.Generic;

namespace GameRecord.Gamess.EvoGaming
{
    public class GameListResponse
    {
        [JsonProperty("data")]
        public List<TableData> Data { get; set; }
    }

    public class TableData
    {
        [JsonProperty("Table Name")]
        public string TableName { get; set; }

        [JsonProperty("Table ID")]
        public string TableID { get; set; }

        [JsonProperty("Game Type")]
        public string GameType { get; set; }

        [JsonProperty("Game Provider")]
        public string GameProvider { get; set; }

        [JsonProperty("thumbnails")]
        public Thumbnails Thumbnails { get; set; }
    }

    public class Thumbnails
    {
        public string S { get; set; }

        public string M { get; set; }

        public string L { get; set; }

        public string XL { get; set; }
    }
}