using System;

namespace AWC.Models
{
    public class WithdrawResponse : BaseResponse
    {
        public string Method { get; set; }
        public int DatabaseId { get; set; }
        public string Amount { get; set; }
        public string CurrentBalance { get; set; }
        public string TxCode { get; set; }
        public DateTime LastModified { get; set; }
    }
}