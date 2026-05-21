using System.Globalization;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor;
using DevExpress.ExpressApp.Blazor.Services;
using DevExpress.ExpressApp.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Blazor.Server.Localization;

/// <summary>
/// Applies and persists per-user UI culture (Layer A).
/// </summary>
public static class UserCultureHelper
{
    public static async Task ApplyStoredCultureAfterLogonAsync(BlazorApplication application)
    {
        if (SecuritySystem.CurrentUser is not ApplicationUser user
            || string.IsNullOrWhiteSpace(user.PreferredCulture))
        {
            return;
        }

        if (!VisaLocalization.TryNormalizeCulture(user.PreferredCulture, out string storedCulture))
        {
            return;
        }

        if (VisaLocalization.TryNormalizeCulture(CultureInfo.CurrentUICulture.Name, out string currentCulture)
            && string.Equals(storedCulture, currentCulture, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var cultureService = (IXafCultureInfoService)application.ServiceProvider
            .GetRequiredService(typeof(IXafCultureInfoService));
        await cultureService.SetCultureAsync(storedCulture).ConfigureAwait(false);
    }

    public static void PersistCurrentCultureToUser(XafApplication application)
    {
        if (SecuritySystem.CurrentUser is not ApplicationUser user)
        {
            return;
        }

        if (!VisaLocalization.TryNormalizeCulture(CultureInfo.CurrentUICulture.Name, out string currentCulture))
        {
            return;
        }

        if (string.Equals(user.PreferredCulture, currentCulture, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        using IObjectSpace objectSpace = application.CreateObjectSpace(typeof(ApplicationUser));
        ApplicationUser userInOs = objectSpace.GetObjectByKey<ApplicationUser>(user.ID);
        if (userInOs == null)
        {
            return;
        }

        userInOs.PreferredCulture = currentCulture;
        objectSpace.CommitChanges();
    }

    /// <summary>
    /// Sets the culture cookie when the user has no saved preference yet (e.g. first login after picking language on the logon page).
    /// </summary>
    public static void SeedPreferredCultureFromRequestIfEmpty(ApplicationUser user, HttpContext? httpContext)
    {
        if (httpContext == null || !string.IsNullOrWhiteSpace(user.PreferredCulture))
        {
            return;
        }

        string? cultureName = httpContext.Features.Get<IRequestCultureFeature>()?.RequestCulture.UICulture.Name;
        if (!VisaLocalization.TryNormalizeCulture(cultureName, out string normalized))
        {
            return;
        }

        user.PreferredCulture = normalized;
    }
}
