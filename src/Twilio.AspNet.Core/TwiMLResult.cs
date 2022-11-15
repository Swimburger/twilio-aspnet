﻿using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Twilio.AspNet.Core
{
    // ReSharper disable once InconsistentNaming
    public partial class TwiMLResult : IActionResult
    {
        public string Data { get; protected set; }

        public TwiMLResult()
        {
        }

        public TwiMLResult(string twiml)
        {
            Data = twiml;
        }

        public TwiMLResult(TwiML.TwiML response)
        {
            if (response != null)
                Data = response.ToString();
        }

        public async Task ExecuteResultAsync(ActionContext actionContext)
        {
            var response = actionContext.HttpContext.Response;
            await WriteTwiMLToResponse(response);
        }

        // ReSharper disable once InconsistentNaming
        private async Task WriteTwiMLToResponse(HttpResponse response)
        {
            response.ContentType = "application/xml";

            if (Data == null)
            {
                Data = "<Response></Response>";
            }

            await response.WriteAsync(Data);
        }
    }
}
