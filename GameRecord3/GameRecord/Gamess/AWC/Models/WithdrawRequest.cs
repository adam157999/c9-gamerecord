using System;

namespace AWC.Models
{
    public class WithdrawRequest : BaseRequest
    {
        public string UserId { get; set; }
        public string TxCode { get; set; }
        public int WithdrawType { get; set; }
        public decimal TransferAmount { get; set; }
    }
}