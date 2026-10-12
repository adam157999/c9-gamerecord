using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace GameRecord.Controllers.SeamlessWallet.Model
{
    public class ChangeBalanceRequest : BaseRequest
    {
        public string USERID { get; set; }
        public string AMOUNT { get; set; }
        public string TRANSACTIONID { get; set; }
        public string TRNTYPE { get; set; }
        public string TRNDESCRIPTION { get; set; }
        public int ROUNDID { get; set; }
        public int GAMEID { get; set; }
        public string HISTORY { get; set; }
        public bool ISROUNDFINISHED { get; set; }
        public override string GetParameters()
        {
            return USERID+AMOUNT+TRNTYPE+TRNDESCRIPTION+ROUNDID+GAMEID+HISTORY;
        }
    }

    public class ChangeBalanceResposne : BaseResponse
    {
        public ChangeBalanceRootResponse VGSSYSTEM { get; set; }
    }


    public class ChangeBalanceRootResponse : VGSSYSTEM
    {
        public string TIME { get; set; } = DateTime.Now.ToString("dd MMMM yyyy HH:mm:ss");
        public ChangeBalanceRequest REQUEST { get; set; }
        public dynamic RESPONSE { get; set; }
    }

    public class ChangeBalanceChildResponse : BaseResult
    {
        public long ECSYSTEMTRANSACTIONID { get; set; }
        public string BALANCE { get; set; }
    }
}