using System;
using System.Collections.Generic;

namespace AWC.Models
{
    public class LogoutResponse : BaseResponse
    {
        public List<string> LogoutUsers { get; set; }
        public int Count { get; set; }
    }
}