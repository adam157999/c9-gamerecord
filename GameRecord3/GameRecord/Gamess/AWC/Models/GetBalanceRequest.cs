using System;

namespace AWC.Models
{
    public class GetBalanceRequest : BaseRequest
    {
        public string UserIds { get; set; }
    }
}