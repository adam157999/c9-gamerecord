namespace GameRecord.Gamess.MT.Models
{
    public class BaseRequest
    {
        public string web_id { get; set; } 
        public string user_id { get; set; }
        public string system_code { get; set; }

        public BaseRequest(string webId, string systemCode)
        {
            web_id = webId;
            system_code = systemCode;
        }
    }
}