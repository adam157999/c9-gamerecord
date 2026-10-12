using System;

namespace AWC.Models
{
    public class CheckTransferOperationResponse : BaseResponse
    {
        public int TxStatus { get; set; }
        public decimal Balance { get; set; }
        public decimal TransferAmount { get; set; }
        public string TransferType { get; set; }
        public string TxCode { get; set; }
    }
}