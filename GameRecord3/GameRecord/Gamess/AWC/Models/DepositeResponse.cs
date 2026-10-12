using System;

namespace AWC.Models
{
    public class DepositeResponse : BaseResponse
    {
        public string Method { get; set; }
        public int DatabaseId { get; set; }
        public string CurrentBalance { get; set; }
        public DateTime LastModified { get; set; }
        public string TxCode { get; set; }
        public string Amount { get; set; }
    }
}