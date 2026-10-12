using System;

namespace GameRecord.Gamess.OctoPlay.Models
{
    public class TransferWalletRequest
    {
        public decimal Amount { get; set; } // Transfer amount
        public string BrandId { get; set; } // Brand ID
        public string Currency { get; set; } // Currency
        public string PlayerId { get; set; } // Player ID
        public string TransactionId { get; set; } // Transaction ID
        public string Type { get; set; } // Type (deposit/withdraw)
    }

    public class TransferWalletResponse
    {
        public decimal Balance { get; set; } // Wallet balance after transfer
        public string Currency { get; set; } // Currency
    }
}
