using System;
using System.Collections.Generic;

namespace AWC.Models
{
    public class LoginResponse : BaseResponse
    {
        public string Url { get; set; }
        public List<object> Extension { get; set; }
        public string Desc { get; set; }
    }
}