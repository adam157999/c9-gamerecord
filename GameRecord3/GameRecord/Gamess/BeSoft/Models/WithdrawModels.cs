namespace GameRecord.Gamess.BeSoft.Models
{
    public class WithdrawRequest
    {
        public string Cert { get; set; } // Security code
        public string User { get; set; } // User ID
        public string Extension1 { get; set; } // Agent ID
        public string Score { get; set; } // Updated score (negative: agent gives points, positive: agent deducts points)
        public string TraceId { get; set; } // Unique identification code for each transaction
    }

    public class WithdrawResponse
    {
        public double AfterUserScore { get; set; } // Score after transaction
        public double BeforeUserScore { get; set; } // Score before transaction
        public int Status { get; set; } // Status code
        public double UserAddScore { get; set; } // Score added to user
        public string TraceId { get; set; } // Transaction trace ID
    }
}
