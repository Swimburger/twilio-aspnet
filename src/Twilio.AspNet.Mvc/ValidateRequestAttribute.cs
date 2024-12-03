﻿using System.Configuration;
using System.Net;
using System.Web.Mvc;

namespace Twilio.AspNet.Mvc;

/// <summary>
/// Represents an attribute that is used to prevent forgery of a request.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Module)]
public class ValidateRequestAttribute : ActionFilterAttribute
{
    protected internal string? AuthToken { get; set; }
    protected internal string? BaseUrlOverride { get; set; }
    protected internal bool AllowLocal { get; set; }

    /// <summary>
    /// Initializes a new instance of the ValidateRequestAttribute class.
    /// </summary>
    public ValidateRequestAttribute()
    {
        ConfigureProperties();
    }

    /// <summary>
    /// Configures the properties of the attribute.
    /// </summary>
    /// <remarks>
    /// This method exists so ValidateRequestAttribute can be inherited from 
    /// and ConfigureProperties can be overridden.
    /// </remarks>
    /// <exception cref="Exception"></exception>
    protected virtual void ConfigureProperties()
    {
        var requestValidationConfiguration =
            ConfigurationManager.GetSection("twilio/requestValidation") as RequestValidationConfigurationSection;
        var appSettings = ConfigurationManager.AppSettings;

        AuthToken = appSettings["twilio:requestValidation:authToken"]
                    ?? appSettings["twilio:authToken"]
                    ?? requestValidationConfiguration?.AuthToken
                    ?? throw new Exception("Twilio Auth Token not configured");

        BaseUrlOverride = (appSettings["twilio:requestValidation:baseUrlOverride"]
                           ?? requestValidationConfiguration?.BaseUrlOverride)
            ?.TrimEnd('/');

        var allowLocalAppSetting = appSettings["twilio:requestValidation:allowLocal"];
        AllowLocal = allowLocalAppSetting is not null
            ? bool.Parse(allowLocalAppSetting)
            : requestValidationConfiguration?.AllowLocal
              ?? false;
    }

    public override void OnActionExecuting(ActionExecutingContext filterContext)
    {
        var httpContext = filterContext.HttpContext;
        string? urlOverride = null;
        if (BaseUrlOverride is not null)
        {
            urlOverride = $"{BaseUrlOverride}{httpContext.Request.Path}{httpContext.Request.QueryString}";
        }

        if (!RequestValidationHelper.IsValidRequest(filterContext.HttpContext, AuthToken!, urlOverride, AllowLocal))
        {
            filterContext.Result = new HttpStatusCodeResult(HttpStatusCode.Forbidden);
        }

        base.OnActionExecuting(filterContext);
    }
}