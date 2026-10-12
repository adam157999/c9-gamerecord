using System.Xml.Serialization;

namespace GameRecord.Gamess.BTI.Models
{
    public class GetBalanceRequest
    {
        public string AgentUserName { get; set; }
        public string AgentPassword { get; set; }
        public string MerchantCustomerCode { get; set; }
    }

    [XmlRoot("MerchantResponse", Namespace = "http://networkpot.com/")]
    public class GetBalanceResponse
    {
        [XmlElement("ErrorCode")]
        public string ErrorCode { get; set; }
        
        [XmlElement("CustomerID")]
        public long CustomerID { get; set; }
        
        [XmlElement("AuthToken")]
        public string AuthToken { get; set; }
        
        [XmlElement("Balance")]
        public decimal Balance { get; set; }
        
        [XmlElement("OpenBetsBalance")]
        public decimal OpenBetsBalance { get; set; }
        
        [XmlElement("TransactionID")]
        public long TransactionID { get; set; }
    }
}
