using System.Xml.Serialization;

namespace GameRecord.Gamess.BTI.Models
{
    public class CreateUserNewRequest
    {
        public string AgentUserName { get; set; }
        public string AgentPassword { get; set; }
        public string MerchantCustomerCode { get; set; }
        public string LoginName { get; set; }
        public string CurrencyCode { get; set; }
        public string CountryCode { get; set; }
        public string City { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public int Group1ID { get; set; }
    }

    [XmlRoot("MerchantResponse", Namespace = "http://networkpot.com/")]
    public class CreateUserNewResponse
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
