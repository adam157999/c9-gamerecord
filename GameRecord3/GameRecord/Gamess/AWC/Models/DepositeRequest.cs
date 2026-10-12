using System;

namespace AWC.Models
{
    public class DepositeRequest : BaseRequest
    {
        public string UserId { get; set; }
        public string TxCode { get; set; }
        public decimal TransferAmount { get; set; }
    }
}