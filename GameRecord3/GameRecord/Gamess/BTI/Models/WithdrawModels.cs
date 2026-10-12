using System.Xml.Serialization;

namespace GameRecord.Gamess.BTI.Models
{
    public class WithdrawRequest
    {
        public string AgentUserName { get; set; }
        public string AgentPassword { get; set; }
        public string MerchantCustomerCode { get; set; }
        public decimal Amount { get; set; }
        public string RefTransactionCode { get; set; }
    }

    [XmlRoot("MerchantResponse", Namespace = "http://networkpot.com/")]
    public class WithdrawResponse
    {
        [XmlElement("ErrorCode")]
        public string ErrorCode { get; set; }
        
        [XmlElement("CustomerID")]
        public long CustomerID { get; set; }
        
        [XmlElement("AuthToken")]
        public string AuthToken { get; set; }
        
        [XmlElement("Balance")]
        public decimal Balance { get; set; }
        
        [XmlElement("TransactionID")]
        public long TransactionID { get; set; }
    }
}
