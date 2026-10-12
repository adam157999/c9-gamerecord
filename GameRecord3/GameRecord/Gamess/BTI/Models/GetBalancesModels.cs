using System.Collections.Generic;

namespace GameRecord.Gamess.BTI.Models
{
    public class GetBalancesRequest
    {
        public string AgentUserName { get; set; }
        public string AgentPassword { get; set; }
        public string ListOfCustomers { get; set; }
        public string FromDate { get; set; }
    }

    public class BalanceInfo
    {
        public string MerchantCustomerCode { get; set; }
        public decimal Balance { get; set; }
        public decimal OpenBetsBalance { get; set; }
    }

    public class GetBalancesResponse
    {
        public string ErrorCode { get; set; }
        public long CustomerID { get; set; }
        public string AuthToken { get; set; }
        public decimal Balance { get; set; }
        public long TransactionID { get; set; }
        public List<BalanceInfo> Balances { get; set; }
    }
}
