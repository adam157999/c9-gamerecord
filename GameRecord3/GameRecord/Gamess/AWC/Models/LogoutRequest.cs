using System;
using System.Collections.Generic;

namespace AWC.Models
{
    public class LogoutRequest : BaseRequest
    {
        public string UserIds { get; set; }
    }
}