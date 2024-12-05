﻿using System.Xml.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Twilio.AspNet.Core;

/// <summary>
/// TwiMLResult writes TwiML to the HTTP response body
/// </summary>
public partial class TwiMLResult : IActionResult
{
    private readonly TwiML.TwiML _twiml;
    private readonly SaveOptions _formattingOptions;

    /// <param name="twiml">The TwiML to respond with</param>
    public TwiMLResult(TwiML.TwiML twiml) : this(twiml, SaveOptions.None)
    {
    }
        
    /// <param name="twiml">The TwiML to respond with</param>
    /// <param name="formattingOptions">Specifies how to format TwiML</param>
    public TwiMLResult(TwiML.TwiML twiml, SaveOptions formattingOptions)
    {
        _twiml = twiml;
        _formattingOptions = formattingOptions;
    }

    public async Task ExecuteResultAsync(ActionContext actionContext)
    {
        var response = actionContext.HttpContext.Response;
        await WriteTwiMLToResponse(response);
    }
        
    private async Task WriteTwiMLToResponse(HttpResponse response)
    {
        response.ContentType = "application/xml";
        if (_twiml is null)
        {
            await response.WriteAsync("<Response></Response>");
            return;
        }

        var data = _twiml.ToString(_formattingOptions);
        await response.WriteAsync(data);
    }
}