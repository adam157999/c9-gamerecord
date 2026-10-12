using Newtonsoft.Json;
using System.Collections.Generic;

namespace GameRecord.Models
{
    public class JDBFishGame
    {
        public int mType { get; set; }
        public string image { get; set; }
        public string name { get; set; }
        public List<string> betList { get; set; }
        public List<string> freeSpinTypes { get; set; }
    }

    public class JDBFishGameGroup
    {
        public int gType { get; set; }
        public List<JDBFishGame> list { get; set; }
    }

    public class QueryGameListResponse
    {
        public string status { get; set; }
        public List<JDBFishGameGroup> data { get; set; }
    }
}
