namespace GameRecord.Gamess.BTI.Models
{
    public class ErrorResponse
    {
        public string ErrorCode { get; set; }
        public long CustomerID { get; set; }
        public string AuthToken { get; set; }
        public decimal Balance { get; set; }
        public long TransactionID { get; set; }
    }
}
