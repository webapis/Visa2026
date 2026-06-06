using DevExpress.ExpressApp.Blazor.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.DependencyInjection;
using Visa2026.Module.Localization;

namespace Visa2026.Blazor.Server.Localization;

/// <summary>
/// Resolves UI culture for custom Blazor editors (request cookie first, then XAF culture service).
/// </summary>
public static class VisaUiCultureResolver
{
    public static string Resolve(
        IXafCultureInfoService? cultureService = null,
        HttpContext? httpContext = null,
        string? componentCultureName = null)
    {
        string? fromRequest = httpContext?.Features.Get<IRequestCultureFeature>()?.RequestCulture.UICulture.Name;
        if (VisaLocalization.TryNormalizeCulture(fromRequest, out string normalized))
        {
            return normalized;
        }

        if (cultureService != null)
        {
            return VisaUiMessages.NormalizeCultureName(cultureService.CurrentUICulture.Name);
        }

        if (!string.IsNullOrWhiteSpace(componentCultureName))
        {
            return VisaUiMessages.NormalizeCultureName(componentCultureName);
        }

        return VisaUiMessages.DefaultCultureName;
    }

    public static string Resolve(IServiceProvider serviceProvider, string? componentCultureName = null)
    {
        var cultureService = serviceProvider.GetService<IXafCultureInfoService>();
        var httpContext = serviceProvider.GetService<IHttpContextAccessor>()?.HttpContext;
        return Resolve(cultureService, httpContext, componentCultureName);
    }
}
