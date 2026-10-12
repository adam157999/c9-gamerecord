using System.Collections.Generic;

namespace GameRecord.Gamess.OnlyPlay.Models
{
    public class GetEgamesListResponse
    {
        public int ErrorCode { get; set; } // Error code
        public string Message { get; set; } // Response message
        public string ProductName { get; set; } // Product name
        public GameList GameList { get; set; } // List of games
    }

    public class GameList
    {
        public List<Game> Game { get; set; } // List of individual games
    }

    public class Game
    {
        public string IsHtml5 { get; set; } // Indicates if the game is HTML5
        public GameCode Code { get; set; } // Game code details
        public string Text { get; set; } // Game code text
        public string EnglishName { get; set; } // English name of the game
        public string ChineseName { get; set; } // Chinese name of the game
        public GameCategory Category { get; set; } // Game category details
        public string Image { get; set; } // URL of the game image
    }

    public class GameCode
    {
        public string Text { get; set; } // Type of the game code
    }


    public class GameCategory
    {
        public string IsNew { get; set; } // Indicates if the game is new
        public string IsBranded { get; set; } // Indicates if the game is branded
        public string Text { get; set; } // Category text
    }
}