using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor;
using DevExpress.ExpressApp.Blazor.Services;
using Visa2026.Blazor.Server.Theming;

namespace Visa2026.Blazor.Server.Controllers;

/// <summary>
/// Restore <see cref="Module.BusinessObjects.ApplicationUser"/> theme preferences after logon
/// and persist changes from the runtime theme switcher.
/// </summary>
public sealed class UserThemeController : WindowController
{
    IThemeService? themeService;
    IXafSizeModeService? sizeModeService;
    bool loggedOnHandlerAttached;
    bool loggingOffHandlerAttached;
    bool themeHandlersAttached;

    public UserThemeController()
    {
        TargetWindowType = WindowType.Main;
    }

    protected override void OnActivated()
    {
        base.OnActivated();
        AttachLoggedOnHandler();
        AttachLoggingOffHandler();
        UserThemeHelper.SuppressPersist();
        AttachThemeHandlers();
        ApplyStoredThemeOnMainWindowActivated();
    }

    protected override void OnDeactivated()
    {
        UserThemeHelper.PersistCurrentThemeToUser(Application);
        DetachThemeHandlers();
        DetachLoggingOffHandler();
        DetachLoggedOnHandler();
        base.OnDeactivated();
    }

    void ApplyStoredThemeOnMainWindowActivated()
    {
        if (Application is not BlazorApplication blazorApplication)
        {
            UserThemeHelper.AllowPersist();
            return;
        }

        _ = ApplyStoredThemeSafeAsync(blazorApplication);
    }

    static async Task ApplyStoredThemeSafeAsync(BlazorApplication blazorApplication)
    {
        try
        {
            await UserThemeHelper.ApplyStoredThemeAfterLogonAsync(blazorApplication).ConfigureAwait(false);
        }
        catch
        {
            // Theme apply may fail if the circuit is tearing down; ignore.
        }
        finally
        {
            UserThemeHelper.AllowPersist();
        }
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

    void AttachLoggingOffHandler()
    {
        if (loggingOffHandlerAttached)
        {
            return;
        }

        Application.LoggingOff += Application_LoggingOff;
        loggingOffHandlerAttached = true;
    }

    void DetachLoggingOffHandler()
    {
        if (!loggingOffHandlerAttached)
        {
            return;
        }

        Application.LoggingOff -= Application_LoggingOff;
        loggingOffHandlerAttached = false;
    }

    void AttachThemeHandlers()
    {
        if (themeHandlersAttached || Application is not BlazorApplication blazorApplication)
        {
            return;
        }

        themeService = blazorApplication.ServiceProvider.GetService(typeof(IThemeService)) as IThemeService;
        sizeModeService = blazorApplication.ServiceProvider.GetService(typeof(IXafSizeModeService)) as IXafSizeModeService;

        if (themeService != null)
        {
            themeService.CurrentThemeChanged += ThemeService_CurrentThemeChanged;
        }

        if (sizeModeService != null)
        {
            sizeModeService.SizeModeChanged += SizeModeService_SizeModeChanged;
        }

        themeHandlersAttached = themeService != null || sizeModeService != null;
    }

    void DetachThemeHandlers()
    {
        if (!themeHandlersAttached)
        {
            return;
        }

        if (themeService != null)
        {
            themeService.CurrentThemeChanged -= ThemeService_CurrentThemeChanged;
            themeService = null;
        }

        if (sizeModeService != null)
        {
            sizeModeService.SizeModeChanged -= SizeModeService_SizeModeChanged;
            sizeModeService = null;
        }

        themeHandlersAttached = false;
    }

    async void Application_LoggedOn(object sender, LogonEventArgs e)
    {
        if (Application is not BlazorApplication blazorApplication)
        {
            return;
        }

        UserThemeHelper.SuppressPersist();
        await ApplyStoredThemeSafeAsync(blazorApplication).ConfigureAwait(false);
    }

    void Application_LoggingOff(object sender, LoggingOffEventArgs e) =>
        UserThemeHelper.PersistCurrentThemeToUser(Application);

    void ThemeService_CurrentThemeChanged(object? sender, EventArgs e) =>
        UserThemeHelper.PersistCurrentThemeToUser(Application);

    void SizeModeService_SizeModeChanged(object? sender, EventArgs e) =>
        UserThemeHelper.PersistCurrentThemeToUser(Application);
}
