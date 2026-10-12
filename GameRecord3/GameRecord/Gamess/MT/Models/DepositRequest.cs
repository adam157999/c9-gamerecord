namespace GameRecord.Gamess.MT.Models
{
    public class DepositRequest : BaseRequest
    {
        public decimal balance { get; set; }
        public string transfer_id { get; set; }

        public DepositRequest(string webId, string systemCode, decimal balance, string transferId, string userId)
            : base(webId, systemCode)
        {
            this.balance = balance;
            this.transfer_id = transferId;
            this.user_id = userId;
        }

    }
}