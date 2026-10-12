using System;

namespace GameRecord.Gamess.OctoPlay.Models
{
    public class FetchWalletRequest
    {
        public string BrandId { get; set; } // Brand ID
        public string Currency { get; set; } // Currency
        public string PlayerId { get; set; } // Player ID
    }

    public class FetchWalletResponse
    {
        public decimal Balance { get; set; } // Status of the wallet creation
        public string Currency { get; set; } // Created wallet ID (if applicable)
    }
}
