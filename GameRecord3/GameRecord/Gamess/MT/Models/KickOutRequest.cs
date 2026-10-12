namespace GameRecord.Gamess.MT.Models
{
    public class kickOutRequest : BaseRequest
    {
        public kickOutRequest(string webId, string systemCode, string userId) : base(webId, systemCode)
        {
            user_id = userId;
        }
    }
}