using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;

namespace Visa2026.Blazor.Server.Localization;

/// <summary>
/// Sets the ASP.NET Core culture cookie used by request localization and <see cref="IXafCultureInfoService"/>.
/// </summary>
public static class VisaCultureCookie
{
    public static bool TrySetCulture(HttpResponse response, string cultureName, HttpRequest? request = null)
    {
        if (response.HasStarted)
        {
            return false;
        }

        SetCulture(response, cultureName, request);
        return true;
    }

    public static void SetCulture(HttpResponse response, string cultureName, HttpRequest? request = null)
    {
        if (response.HasStarted)
        {
            throw new InvalidOperationException("Cannot set culture cookie after the response has started.");
        }

        if (!VisaLocalization.TryNormalizeCulture(cultureName, out string normalized))
        {
            normalized = VisaLocalization.DefaultCultureName;
        }

        var requestCulture = new RequestCulture(normalized);
        response.Cookies.Append(
            CookieRequestCultureProvider.DefaultCookieName,
            CookieRequestCultureProvider.MakeCookieValue(requestCulture),
            CreateCookieOptions(request));
    }

    internal static CookieOptions CreateCookieOptions(HttpRequest? request) =>
        new()
        {
            Expires = DateTimeOffset.UtcNow.AddYears(1),
            IsEssential = true,
            SameSite = SameSiteMode.Lax,
            Path = "/",
            Secure = request?.IsHttps == true,
        };
}
