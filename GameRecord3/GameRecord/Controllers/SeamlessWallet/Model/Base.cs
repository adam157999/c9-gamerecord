using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace GameRecord.Controllers.SeamlessWallet.Model
{
    public abstract class BaseRequest
    {
        public string HASH { get; set; }
        public abstract string GetParameters();
    }

    public class BaseResponse
    {
    }

    
    public class VGSSYSTEM
    {
        //public Xml xml { get; set; } = new Xml();

    }

    public class BaseResult
    {
        public string RESULT { get; set; }
    }

    public class ErrorResult : BaseResult
    {
        public string CODE { get; set; }
    }
    public class Xml
    {
        public string @version { get => "1.0"; }
        public string @standalone { get => "no"; } 

    }

}