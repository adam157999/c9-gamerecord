namespace GameRecord.Gamess.MT.Models
{
    public class DepositResponse : BaseResponse
    {
        public DepositData data { get; set; }
    }

    public class DepositData
    {
        public string user_id { get; set; } // Unique Identifier for the Player
        public float balance { get; set; } // Points
        public string transfer_id { get; set; } // Transaction Number
    }
}