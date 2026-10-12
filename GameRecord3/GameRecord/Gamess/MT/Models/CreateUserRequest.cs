namespace GameRecord.Gamess.MT.Models
{
    public class CreateUserRequest : BaseRequest
    {
        public string user_name { get; set; }
        public string currency { get; set; }

        public CreateUserRequest(string webId, string systemCode, string userId, string userName, string currency)
            : base(webId, systemCode)
        {
            user_id = userId;
            user_name = userName;
            this.currency = currency;
        }
    }
}