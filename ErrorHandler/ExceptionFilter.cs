using Microsoft.ApplicationInsights;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Web.Http.Filters;

namespace SimpleEchoBot.ErrorHandler
{
    public class ExceptionFilter : ExceptionFilterAttribute
    {
        public override void OnException(HttpActionExecutedContext ctx)
        {
            HandleError(ctx);
        }

        private static void HandleError(HttpActionExecutedContext ctx)
        {
            ctx.Response = new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                Content = new StringContent(ctx.Exception.Message)
            };

            var client = new TelemetryClient();
            client.TrackException(ctx.Exception);
        }
    }
}