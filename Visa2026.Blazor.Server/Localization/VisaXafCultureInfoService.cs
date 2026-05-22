using System.Globalization;
using DevExpress.ExpressApp.Blazor.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;

namespace Visa2026.Blazor.Server.Localization;

/// <summary>
/// Wraps the DevExpress culture service so language changes reload with <c>?culture=</c> query parameters.
/// The culture cookie is written on that full page load by <see cref="VisaCulturePersistenceMiddleware"/>
/// (not here — Blazor circuits have already started the HTTP response).
/// </summary>
public sealed class VisaXafCultureInfoService : IXafCultureInfoService
{
    private readonly XafCultureInfoService inner;
    private readonly NavigationManager navigationManager;

    public VisaXafCultureInfoService(
        NavigationManager navigationManager,
        XafCultureInfoService innerCultureService)
    {
        this.navigationManager = navigationManager;
        inner = innerCultureService;
    }

    public CultureInfo CurrentCulture => inner.CurrentCulture;

    public CultureInfo CurrentUICulture => inner.CurrentUICulture;

    public IList<CultureInfo> SupportedCultures => inner.SupportedCultures;

    public IList<CultureInfo> SupportedUICultures => inner.SupportedUICultures;

    public event EventHandler<CultureChangingEventArgs>? CultureChanging
    {
        add => inner.CultureChanging += value;
        remove => inner.CultureChanging -= value;
    }

    public Task SetCultureAsync(string culture) => SetCultureAsync(culture, culture);

    public Task SetCultureAsync(string culture, string uiCulture)
    {
        if (!VisaLocalization.TryNormalizeCulture(culture, out string normalizedCulture))
        {
            normalizedCulture = VisaLocalization.DefaultCultureName;
        }

        if (!VisaLocalization.TryNormalizeCulture(uiCulture, out string normalizedUiCulture))
        {
            normalizedUiCulture = normalizedCulture;
        }

        Uri absoluteUri = navigationManager.ToAbsoluteUri(navigationManager.Uri);
        var query = QueryHelpers.ParseQuery(absoluteUri.Query);
        query["culture"] = normalizedCulture;
        query["ui-culture"] = normalizedUiCulture;
        string newUri = QueryHelpers.AddQueryString(absoluteUri.GetLeftPart(UriPartial.Path), query);
        navigationManager.NavigateTo(newUri, forceLoad: true);
        return Task.CompletedTask;
    }
}
