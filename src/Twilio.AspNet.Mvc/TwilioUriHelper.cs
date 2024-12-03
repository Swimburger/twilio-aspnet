﻿using System.Web.Mvc;

namespace Twilio.AspNet.Mvc;

public static class TwilioUriHelper
{
    public static Uri ActionUri(this UrlHelper helper, string actionName, string controllerName) =>
        new(helper.Action(actionName, controllerName), UriKind.Relative);
}