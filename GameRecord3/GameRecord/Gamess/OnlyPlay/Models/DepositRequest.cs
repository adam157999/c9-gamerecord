namespace GameRecord.Gamess.OnlyPlay.Models
{
    public class DepositRequest
    {
        public string Operator { get; set; } // Operator's name
        public string Product { get; set; } // Product's name
        public string Username { get; set; } // Member's username
        public decimal Amount { get; set; } // Amount to be deposited
        public string TransactionID { get; set; } // Unique transaction ID
    }
}