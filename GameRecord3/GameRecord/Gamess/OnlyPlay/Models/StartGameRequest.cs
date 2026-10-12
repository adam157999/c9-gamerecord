namespace GameRecord.Gamess.OnlyPlay.Models
{
    public class StartGameRequest
    {
        public string Operator { get; set; } // Operator's name
        public string Product { get; set; } // Product's name
        public string Username { get; set; } // Member's username
        public string Category { get; set; } // Product's category
        public bool IsMobile { get; set; } // Indicates if the game URL is for mobile or desktop
        public string  Game { get; set; } // Game code (optional)
    }
}