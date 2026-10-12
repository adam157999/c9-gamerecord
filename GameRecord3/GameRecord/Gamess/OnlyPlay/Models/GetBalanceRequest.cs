namespace GameRecord.Gamess.OnlyPlay.Models
{
    public class GetBalanceRequest
    {
        public string Operator { get; set; } // Operator's name
        public string Product { get; set; } // Product's name
        public string Username { get; set; } // Member's username
    }
}