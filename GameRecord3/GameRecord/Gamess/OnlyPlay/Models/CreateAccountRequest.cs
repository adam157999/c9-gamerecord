namespace GameRecord.Gamess.OnlyPlay.Models
{
    public class CreateAccountRequest
    {
        public string Operator { get; set; } // Operator's name
        public string Product { get; set; } // Product's name
        public string Username { get; set; } // Member's username
        public string Password { get; set; } // Member's password
        public string Language { get; set; } = "en-US"; // Account's language, default is English
    }
}