using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;

namespace GameRecord.Controllers.Callback
{
    public class Ace333HeaderAttribute : ActionFilterAttribute
    {
        private readonly string _headerName;

        public Ace333HeaderAttribute(string headerName)
        {
            _headerName = headerName;
        }

        public override void OnActionExecuting(HttpActionContext actionContext)
        {
            if (actionContext.Request.Headers.Contains(_headerName))
            {
                base.OnActionExecuting(actionContext);
            }
            else
            {
                actionContext.Response = actionContext.Request.CreateErrorResponse(HttpStatusCode.BadRequest, $"Header {_headerName} is required.");
            }
        }
    }

    
}