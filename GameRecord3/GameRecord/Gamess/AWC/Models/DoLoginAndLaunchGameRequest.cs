using System;

namespace AWC.Models
{
    public class LoginGameRequest : BaseRequest
    {
        public string UserId { get; set; }
        public bool IsMobileLogin { get; set; }
        public string Language { get; set; }
        public string GameType { get; set; }
        public string Platform { get; set; }
        public string GameForbidden { get; set; } = "";
    }
}