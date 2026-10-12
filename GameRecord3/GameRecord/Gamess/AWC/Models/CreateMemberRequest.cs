using System;

namespace AWC.Models
{
    public class CreateMemberRequest : BaseRequest
    {
        public string UserId { get; set; }
        public string Currency { get; set; }
        public string BetLimit { get; set; }
        public string Language { get; set; }
        public string UserName { get; set; }
        public string Desc { get; set; }
    }
}