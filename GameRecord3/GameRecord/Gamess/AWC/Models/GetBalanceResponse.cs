using System;
using System.Collections.Generic;

namespace AWC.Models
{
    public class GetBalanceResponse : BaseResponse
    {
        public List<GetBalanceResult> Results { get; set; }
        public string QueryTime { get; set; }
        public int Count { get; set; }
    }

    public class GetBalanceResult
    {
        public string UserId { get; set; }
        public string Balance { get; set; }
        public DateTime LastModified { get; set; }
    }
}