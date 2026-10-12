namespace GameRecord.Gamess.MT.Models
{
    public class GetURLTokenRequest : BaseRequest
    {
        public string language { get; set; }

        public GetURLTokenRequest(string webId, string systemCode, string userId, string language = "en") : base(webId, systemCode)
        {
            user_id = userId;
            this.language = language;
        }
    }
}