namespace GameRecord.Gamess.MT.Models
{
    public class GetBalanceResponse : BaseResponse
    {
        public BalanceData Data { get; set; }
    }

    public class BalanceData
    {
        public string user_id { get; set; } // Unique Identifier for the Player
        public float balance { get; set; } // Points
    }
}