using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor;
using DevExpress.ExpressApp.Security;
using DevExpress.ExpressApp.SystemModule;
using Visa2026.Blazor.Server.Localization;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Blazor.Server.Controllers;

/// <summary>
/// Layer A: restore <see cref="ApplicationUser.PreferredCulture"/> after logon and persist changes from the runtime language switcher.
/// </summary>
public sealed class UserCultureController : WindowController
{
    private bool loggedOnHandlerAttached;

    public UserCultureController()
    {
        TargetWindowType = WindowType.Main;
    }

    protected override void OnActivated()
    {
        base.OnActivated();
        AttachLoggedOnHandler();
        UserCultureHelper.PersistCurrentCultureToUser(Application);
    }

    protected override void OnDeactivated()
    {
        DetachLoggedOnHandler();
        base.OnDeactivated();
    }

    void AttachLoggedOnHandler()
    {
        if (loggedOnHandlerAttached)
        {
            return;
        }

        Application.LoggedOn += Application_LoggedOn;
        loggedOnHandlerAttached = true;
    }

    void DetachLoggedOnHandler()
    {
        if (!loggedOnHandlerAttached)
        {
            return;
        }

        Application.LoggedOn -= Application_LoggedOn;
        loggedOnHandlerAttached = false;
    }

    async void Application_LoggedOn(object sender, LogonEventArgs e)
    {
        if (Application is not BlazorApplication blazorApplication)
        {
            return;
        }

        if (SecuritySystem.CurrentUser is ApplicationUser user)
        {
            var httpContext = blazorApplication.ServiceProvider
                .GetService<Microsoft.AspNetCore.Http.IHttpContextAccessor>()?.HttpContext;
            using IObjectSpace objectSpace = Application.CreateObjectSpace(typeof(ApplicationUser));
            ApplicationUser userInOs = objectSpace.GetObjectByKey<ApplicationUser>(user.ID);
            if (userInOs != null)
            {
                UserCultureHelper.SeedPreferredCultureFromRequestIfEmpty(userInOs, httpContext);
                if (VisaLocalization.TryNormalizeCulture(
                        System.Globalization.CultureInfo.CurrentUICulture.Name,
                        out string currentCulture)
                    && !string.Equals(userInOs.PreferredCulture, currentCulture, StringComparison.OrdinalIgnoreCase))
                {
                    userInOs.PreferredCulture = currentCulture;
                }

                objectSpace.CommitChanges();
            }
        }

        try
        {
            await UserCultureHelper.ApplyStoredCultureAfterLogonAsync(blazorApplication).ConfigureAwait(false);
        }
        catch
        {
            // SetCultureAsync may fail if the circuit is tearing down; ignore.
        }
    }
}
