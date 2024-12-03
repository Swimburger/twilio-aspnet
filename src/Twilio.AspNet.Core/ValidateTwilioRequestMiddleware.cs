using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Twilio.AspNet.Core;

/// <summary>
/// Validates that incoming HTTP request originates from Twilio.
/// </summary>
public class ValidateTwilioRequestMiddleware
{
    private readonly RequestDelegate _next;

    public ValidateTwilioRequestMiddleware(RequestDelegate next)
    {
        _next = next;
    }
        
    public async Task InvokeAsync(HttpContext context)
    {
        if (await RequestValidationHelper.IsValidRequestAsync(context).ConfigureAwait(false))
        {
            await _next(context);
            return;
        }
            
        context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
    }
}

public static class ValidateTwilioRequestMiddlewareExtensions
{
    /// <summary>
    /// Validates that incoming HTTP request originates from Twilio.
    /// </summary>
    /// <param name="builder"></param>
    /// <returns></returns>
    public static IApplicationBuilder UseTwilioRequestValidation(this IApplicationBuilder builder)
        => builder.UseMiddleware<ValidateTwilioRequestMiddleware>();
}