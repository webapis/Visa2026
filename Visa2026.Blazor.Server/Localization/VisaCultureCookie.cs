using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;

namespace Visa2026.Blazor.Server.Localization;

/// <summary>
/// Sets the ASP.NET Core culture cookie used by request localization and <see cref="IXafCultureInfoService"/>.
/// </summary>
public static class VisaCultureCookie
{
    public static void SetCulture(HttpResponse response, string cultureName)
    {
        if (!VisaLocalization.TryNormalizeCulture(cultureName, out string normalized))
        {
            normalized = VisaLocalization.DefaultCultureName;
        }

        var requestCulture = new RequestCulture(normalized);
        response.Cookies.Append(
            CookieRequestCultureProvider.DefaultCookieName,
            CookieRequestCultureProvider.MakeCookieValue(requestCulture),
            new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddYears(1),
                IsEssential = true,
                SameSite = SameSiteMode.Lax,
            });
    }
}
