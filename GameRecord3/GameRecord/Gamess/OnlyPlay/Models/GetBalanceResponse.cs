namespace GameRecord.Gamess.OnlyPlay.Models
{
    public class GetBalanceResponse
    {
        public int ErrorCode { get; set; } // Error code
        public string Message { get; set; } // Response message
        public decimal Balance { get; set; } // Current balance
    }
}