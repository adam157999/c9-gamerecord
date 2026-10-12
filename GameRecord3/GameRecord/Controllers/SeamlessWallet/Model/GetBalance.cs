using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace GameRecord.Controllers.SeamlessWallet.Model
{
    public class GetBalanceRequest : BaseRequest
    {
        public string USERID { get; set; }
        public override string GetParameters()
        {
            return USERID;
        }
    }

    public class GetBalanceResponse : BaseResponse
    {
        public GetBalanceRootResponse VGSSYSTEM { get; set; }
    }


    public class GetBalanceRootResponse : VGSSYSTEM
    {
        public string TIME { get; set; } = DateTime.Now.ToString("dd MMMM yyyy HH:mm:ss");
        public GetBalanceRequest REQUEST { get; set; }
        public dynamic RESPONSE { get; set; }
    }


    public class GetBalanceChildResponse : BaseResult
    {
        public string BALANCE { get; set; }
    }
}