using System.Collections.Generic;

namespace GameRecord.Gamess.BeSoft.Models
{
    public class GetGameListRequest
    {
        public string Extension1 { get; set; } // Agent ID (agent id to login ARS)
    }

    public class GameList
    {
        public string Gid { get; set; } // Game ID
        public string Name { get; set; } // Game Name
        public string GameType { get; set; }
        public string Hot { get; set; }
        public string New { get; set; }
    }

    public class GetGameListResponse
    {
        public string Gameicourl { get; set; } // Agent or admin
        public List<GameList> GameList { get; set; } // List of user balances
    }
}
