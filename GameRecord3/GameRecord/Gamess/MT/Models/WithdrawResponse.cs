namespace GameRecord.Gamess.MT.Models
{
    public class WithdrawResponse : BaseResponse
    {
        public WithdrawData Data { get; set; }
    }

    public class WithdrawData
    {
        public string user_id { get; set; } // Unique Identifier for the Player
        public float balance { get; set; } // Points
        public string transfer_id { get; set; } // Transaction Number
    }
}