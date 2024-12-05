﻿using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Twilio.Security;

namespace Twilio.AspNet.Core;

/// <summary>
/// Class used to validate incoming requests from Twilio using 'Request Validation' as described
/// in the Security section of the Twilio TwiML API documentation.
/// </summary>
public static class RequestValidationHelper
{
    /// <summary>
    /// Performs request validation using the current HTTP context passed in manually or from
    /// the ASP.NET MVC ValidateRequestAttribute
    /// </summary>
    /// <param name="context">HttpContext to use for validation</param>
    internal static async Task<bool> IsValidRequestAsync(HttpContext context)
    {
        var options = context.RequestServices
            .GetRequiredService<IOptionsSnapshot<TwilioRequestValidationOptions>>().Value;

        var authToken = options.AuthToken;
        var baseUrlOverride = options.BaseUrlOverride;
        var allowLocal = options.AllowLocal ?? false;

        var request = context.Request;

        string? urlOverride = null;
        if (!string.IsNullOrEmpty(baseUrlOverride))
        {
            urlOverride = $"{baseUrlOverride}{request.Path}{request.QueryString}";
        }

        return await IsValidRequestAsync(context, authToken, urlOverride, allowLocal).ConfigureAwait(false);
    }

    /// <summary>
    /// Performs request validation using the current HTTP context passed in manually or from
    /// the ASP.NET MVC ValidateRequestAttribute
    /// </summary>
    /// <param name="context">HttpContext to use for validation</param>
    /// <param name="authToken">AuthToken for the account used to sign the request</param>
    /// <param name="allowLocal">
    /// Skip validation for local requests. 
    /// ⚠️ Only use this during development, as this will make your application vulnerable to Server-Side Request Forgery.
    /// </param>
    public static Task<bool> IsValidRequestAsync(HttpContext context, string authToken, bool allowLocal = false)
        => IsValidRequestAsync(context, authToken, null, allowLocal);

    /// <summary>
    /// Performs request validation using the current HTTP context passed in manually or from
    /// the ASP.NET MVC ValidateRequestAttribute
    /// </summary>
    /// <param name="context">HttpContext to use for validation</param>
    /// <param name="authToken">AuthToken for the account used to sign the request</param>
    /// <param name="urlOverride">The URL to use for validation, if different from Request.Url (sometimes needed if web site is behind a proxy or load-balancer)</param>
    /// <param name="allowLocal">
    /// Skip validation for local requests. 
    /// ⚠️ Only use this during development, as this will make your application vulnerable to Server-Side Request Forgery.
    /// </param>
    public static async Task<bool> IsValidRequestAsync(
        HttpContext context, 
        string authToken, 
        string? urlOverride, 
        bool allowLocal = false
    )
    {
        if (context.Request.HasFormContentType)
        {
            // this will load the form async, but then cache is in context.Request.Form which is used later
            await context.Request.ReadFormAsync(context.RequestAborted).ConfigureAwait(false);
        }

        return IsValidRequest(context, authToken, urlOverride, allowLocal);
    }
        
    /// <summary>
    /// Performs request validation using the current HTTP context passed in manually or from
    /// the ASP.NET MVC ValidateRequestAttribute
    /// </summary>
    /// <param name="context">HttpContext to use for validation</param>
    /// <param name="authToken">AuthToken for the account used to sign the request</param>
    /// <param name="allowLocal">
    /// Skip validation for local requests. 
    /// ⚠️ Only use this during development, as this will make your application vulnerable to Server-Side Request Forgery.
    /// </param>
    public static bool IsValidRequest(HttpContext context, string authToken, bool allowLocal = false)
        => IsValidRequest(context, authToken, null, allowLocal);

    /// <summary>
    /// Performs request validation using the current HTTP context passed in manually or from
    /// the ASP.NET MVC ValidateRequestAttribute
    /// </summary>
    /// <param name="context">HttpContext to use for validation</param>
    /// <param name="authToken">AuthToken for the account used to sign the request</param>
    /// <param name="urlOverride">The URL to use for validation, if different from Request.Url (sometimes needed if web site is behind a proxy or load-balancer)</param>
    /// <param name="allowLocal">
    /// Skip validation for local requests. 
    /// ⚠️ Only use this during development, as this will make your application vulnerable to Server-Side Request Forgery.
    /// </param>
    public static bool IsValidRequest(
        HttpContext context, 
        string authToken, 
        string? urlOverride, 
        bool allowLocal = false
    )
    {
        var request = context.Request;

        if (allowLocal && IsLocal(request))
        {
            return true;
        }

        // validate request
        // http://www.twilio.com/docs/security-reliability/security
        // Take the full URL of the request, from the protocol (http...) through the end of the query string (everything after the ?)
        var fullUrl = string.IsNullOrEmpty(urlOverride)
            ? $"{request.Scheme}://{(request.IsHttps ? request.Host.Host : request.Host.ToUriComponent())}{request.Path}{request.QueryString}"
            : urlOverride;

        Dictionary<string, string>? parameters = null;
        if (request.HasFormContentType)
        {
            parameters = request.Form.ToDictionary(kv => kv.Key, kv => kv.Value.ToString());
        }

        var validator = new RequestValidator(authToken);
        return validator.Validate(
            url: fullUrl,
            parameters: parameters,
            expected: request.Headers["X-Twilio-Signature"]
        );
    }

    private static bool IsLocal(HttpRequest req)
    {
        if (req.Headers.ContainsKey("X-Forwarded-For"))
        {
            // Assume not local if we're behind a proxy
            return false;
        }

        var connection = req.HttpContext.Connection;
        if (connection.RemoteIpAddress is not null)
        {
            if (connection.LocalIpAddress is not null)
            {
                return connection.RemoteIpAddress.Equals(connection.LocalIpAddress);
            }

            return IPAddress.IsLoopback(connection.RemoteIpAddress);
        }

        // for in memory TestServer or when dealing with default connection info
        if (connection.RemoteIpAddress is null && connection.LocalIpAddress is null)
        {
            return true;
        }

        return false;
    }
}