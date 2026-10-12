using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace GameRecord.Controllers.SeamlessWallet.Model
{
    public class StatusRequest : BaseRequest
    {
        public string USERID { get; set; }
        public string CASIONTRANSCATIONID { get; set; }
        public override string GetParameters()
        {
            return USERID+CASIONTRANSCATIONID;
        }
    }


    public class StatusResposne : BaseResponse
    {
        public StatusRootResponse VGSSYSTEM { get; set; }
    }


    public class StatusRootResponse : VGSSYSTEM
    {
        public string TIME { get; set; } = DateTime.Now.ToString("dd MMMM yyyy HH:mm:ss");
        public StatusRequest REQUEST { get; set; }
        public dynamic RESPONSE { get; set; }
    }

    public class StatusChildResponse : BaseResult
    {
        public long ECSYSTEMTRANSACTIONID { get; set; }
    }
}