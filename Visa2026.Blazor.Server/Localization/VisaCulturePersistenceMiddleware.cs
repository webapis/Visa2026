using System.Globalization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;

namespace Visa2026.Blazor.Server.Localization;

/// <summary>
/// Persists culture from the query string (language switcher redirect) into the ASP.NET culture cookie
/// so the next full page load keeps the selected language (e.g. on /LoginPage over plain HTTP).
/// </summary>
public sealed class VisaCulturePersistenceMiddleware
{
    private readonly RequestDelegate next;

    public VisaCulturePersistenceMiddleware(RequestDelegate next)
    {
        this.next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (TryGetCultureFromQuery(context.Request, out string normalized))
        {
            VisaCultureCookie.SetCulture(context.Response, normalized, context.Request);
        }

        await next(context);

        if (context.Response.HasStarted)
        {
            return;
        }

        // If localization picked a non-default culture but the cookie header is missing, persist it.
        if (TryGetCultureFromQuery(context.Request, out normalized))
        {
            VisaCultureCookie.SetCulture(context.Response, normalized, context.Request);
            return;
        }

        string current = CultureInfo.CurrentUICulture.Name;
        if (VisaLocalization.TryNormalizeCulture(current, out normalized)
            && !string.Equals(normalized, VisaLocalization.DefaultCultureName, StringComparison.OrdinalIgnoreCase)
            && !RequestHasMatchingCultureCookie(context.Request, normalized))
        {
            VisaCultureCookie.SetCulture(context.Response, normalized, context.Request);
        }
    }

    static bool TryGetCultureFromQuery(HttpRequest request, out string normalized)
    {
        normalized = VisaLocalization.DefaultCultureName;
        string? culture = request.Query["culture"].FirstOrDefault()
            ?? request.Query["ui-culture"].FirstOrDefault();
        return VisaLocalization.TryNormalizeCulture(culture, out normalized);
    }

    static bool RequestHasMatchingCultureCookie(HttpRequest request, string normalized)
    {
        if (!request.Cookies.TryGetValue(CookieRequestCultureProvider.DefaultCookieName, out string? value)
            || string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        try
        {
            ProviderCultureResult parsed = CookieRequestCultureProvider.ParseCookieValue(value);
            string? cookieCulture = parsed.Cultures.FirstOrDefault().Value;
            return VisaLocalization.TryNormalizeCulture(cookieCulture, out string cookieNormalized)
                && string.Equals(cookieNormalized, normalized, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }
}
