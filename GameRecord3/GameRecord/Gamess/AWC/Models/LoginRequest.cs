using System;

namespace AWC.Models
{
    public class LoginRequest : BaseRequest
    {
        public string UserId { get; set; }
        public bool IsMobileLogin { get; set; }
        public string ExternalURL { get; set; }
        public string Platform { get; set; }
        public string GameType { get; set; }
        public string GameForbidden { get; set; }
        public string Language { get; set; }
        public string BetLimit { get; set; }
        public string AutoBetMode { get; set; }
        public bool IsEnableJackpot { get; set; }
    }
}