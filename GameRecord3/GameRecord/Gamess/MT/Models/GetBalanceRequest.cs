namespace GameRecord.Gamess.MT.Models
{
    public class GetBalanceRequest : BaseRequest
    {
        public GetBalanceRequest(string webId, string systemCode, string userId) : base(webId, systemCode)
        {
            user_id = userId;
        }
    }
}