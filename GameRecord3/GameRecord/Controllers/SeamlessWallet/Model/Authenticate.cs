using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace GameRecord.Controllers.SeamlessWallet.Model
{
    public class AuthenticateRequest : BaseRequest
    {
        public string TOKEN { get; set; }
        public override string GetParameters()
        {
            return TOKEN;
        }
    }

    public class AuthenticateResponse : BaseResponse
    {
        public AuthenticateRootResponse VGSSYSTEM { get; set; }
    }


    public class AuthenticateRootResponse : VGSSYSTEM
    {
        [Display(Order = 3)]
        public string TIME { get; set; } = DateTime.Now.ToString("dd MMMM yyyy HH:mm:ss");
        [Display(Order = 2)]
        public AuthenticateRequest REQUEST { get; set; }
        [Display(Order=1)]
        public dynamic RESPONSE { get; set; }



    }

    public class AuthenticateChildResponse : BaseResult
    {
        public string USERID { get; set; } = "";
        public string USERNAME { get; set; } = "";
        public string FIRSTNAME { get; set; } = "";
        public string LASTNAME { get; set; } = "";
        public string EMAIL { get; set; } = "";
        public string CURRENCY { get; set; } = "";
        public string BALANCE { get; set; } = "";
        public string GAMESESSIONID { get; set; } = "";
    }
}