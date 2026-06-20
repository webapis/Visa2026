using DevExpress.Blazor;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor;
using DevExpress.ExpressApp.Blazor.Services;
using DevExpress.ExpressApp.Core;
using DevExpress.ExpressApp.Security;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Blazor.Server.Theming;

/// <summary>
/// Applies and persists per-user UI theme and size mode (mirrors <see cref="Localization.UserCultureHelper"/>).
/// </summary>
public static class UserThemeHelper
{
    const int ApplyRetryCount = 20;
    static readonly TimeSpan ApplyRetryDelay = TimeSpan.FromMilliseconds(100);

    static int applyDepth;
    static int suppressPersistDepth;

    /// <summary>
    /// Blocks persist while the main window attaches theme handlers and before stored theme is applied.
    /// Prevents startup default theme from overwriting saved preferences (timing-sensitive on IIS).
    /// </summary>
    public static void SuppressPersist() => Interlocked.Increment(ref suppressPersistDepth);

    public static void AllowPersist()
    {
        if (Interlocked.Decrement(ref suppressPersistDepth) < 0)
        {
            Interlocked.Increment(ref suppressPersistDepth);
        }
    }

    public static async Task ApplyStoredThemeAfterLogonAsync(BlazorApplication application)
    {
        if (SecuritySystem.CurrentUser is not ApplicationUser user)
        {
            return;
        }

        if (!TryLoadStoredPreferences(application, user.ID, out string? themeCaption, out string? themeMode, out string? sizeMode))
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(themeCaption) && string.IsNullOrWhiteSpace(sizeMode))
        {
            return;
        }

        applyDepth++;
        try
        {
            for (int attempt = 0; attempt < ApplyRetryCount; attempt++)
            {
                IServiceProvider services = application.ServiceProvider;
                IThemeService? themeService = services.GetService(typeof(IThemeService)) as IThemeService;
                IXafSizeModeService? sizeModeService = services.GetService(typeof(IXafSizeModeService)) as IXafSizeModeService;

                if (themeService?.CurrentTheme == null)
                {
                    await Task.Delay(ApplyRetryDelay).ConfigureAwait(false);
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(themeCaption)
                    && ThemeAlreadyMatchesStored(themeService, sizeModeService, themeCaption, themeMode, sizeMode))
                {
                    return;
                }

                await ApplyThemeStateAsync(themeService, sizeModeService, themeCaption, themeMode, sizeMode)
                    .ConfigureAwait(false);

                if (string.IsNullOrWhiteSpace(themeCaption)
                    || ThemeAlreadyMatchesStored(themeService, sizeModeService, themeCaption, themeMode, sizeMode))
                {
                    return;
                }

                await Task.Delay(ApplyRetryDelay).ConfigureAwait(false);
            }
        }
        finally
        {
            applyDepth--;
        }
    }

    public static void PersistCurrentThemeToUser(XafApplication application)
    {
        if (suppressPersistDepth > 0 || applyDepth > 0 || SecuritySystem.CurrentUser is not ApplicationUser user)
        {
            return;
        }

        IServiceProvider services = application.ServiceProvider;
        IThemeService? themeService = services.GetService(typeof(IThemeService)) as IThemeService;
        IXafSizeModeService? sizeModeService = services.GetService(typeof(IXafSizeModeService)) as IXafSizeModeService;

        string? currentCaption = GetPersistedThemeCaption(themeService);
        string? currentMode = IsClassicTheme(themeService?.CurrentTheme)
            ? null
            : FormatThemeMode(themeService?.CurrentFluentTheme?.Mode);
        string? currentSizeMode = sizeModeService?.SizeMode switch
        {
            SizeMode.Small => "Compact",
            SizeMode.Medium => "Standard",
            _ => null
        };

        if (string.IsNullOrWhiteSpace(currentCaption) && string.IsNullOrWhiteSpace(currentMode) && string.IsNullOrWhiteSpace(currentSizeMode))
        {
            return;
        }

        using IObjectSpace objectSpace = CreateUserObjectSpace(application);
        ApplicationUser userInOs = objectSpace.GetObjectByKey<ApplicationUser>(user.ID);
        if (userInOs == null)
        {
            return;
        }

        if (string.Equals(userInOs.PreferredThemeCaption, currentCaption, StringComparison.Ordinal)
            && string.Equals(userInOs.PreferredThemeMode, currentMode, StringComparison.OrdinalIgnoreCase)
            && string.Equals(userInOs.PreferredSizeMode, currentSizeMode, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        userInOs.PreferredThemeCaption = currentCaption;
        userInOs.PreferredThemeMode = currentMode;
        userInOs.PreferredSizeMode = currentSizeMode;
        objectSpace.CommitChanges();
    }

    static async Task ApplyThemeStateAsync(
        IThemeService themeService,
        IXafSizeModeService? sizeModeService,
        string? themeCaption,
        string? themeMode,
        string? sizeMode)
    {
        if (!string.IsNullOrWhiteSpace(themeCaption))
        {
            Theme? theme = themeService.GetThemeByCaption(themeCaption);
            theme ??= TryResolveFluentTheme(themeService, themeCaption);

            if (theme != null)
            {
                await themeService.SetCurrentThemeAsync(theme).ConfigureAwait(false);

                if (!IsClassicTheme(theme))
                {
                    ThemeMode parsedMode = TryParseThemeMode(themeMode, out ThemeMode mode)
                        ? mode
                        : ThemeMode.Dark;
                    ThemeFluentAccentColor accent = TryParseFluentAccentFromCaption(themeCaption)
                        ?? theme.AccentColor;
                    await themeService.SetCurrentFluentThemeAsync(
                        parsedMode,
                        accent,
                        themeCaption).ConfigureAwait(false);
                }
            }
        }

        if (!string.IsNullOrWhiteSpace(sizeMode)
            && sizeModeService != null
            && TryParseSizeMode(sizeMode, out SizeMode parsedSizeMode))
        {
            await sizeModeService.SetSizeModeAsync(parsedSizeMode).ConfigureAwait(false);
        }
    }

    static bool TryLoadStoredPreferences(
        XafApplication application,
        Guid userId,
        out string? themeCaption,
        out string? themeMode,
        out string? sizeMode)
    {
        using IObjectSpace objectSpace = CreateUserObjectSpace(application);
        ApplicationUser? storedUser = objectSpace.GetObjectByKey<ApplicationUser>(userId);
        if (storedUser == null)
        {
            themeCaption = null;
            themeMode = null;
            sizeMode = null;
            return false;
        }

        themeCaption = NormalizeStoredThemeCaption(storedUser.PreferredThemeCaption);
        themeMode = storedUser.PreferredThemeMode;
        sizeMode = storedUser.PreferredSizeMode;
        return true;
    }

    static IObjectSpace CreateUserObjectSpace(XafApplication application)
    {
        INonSecuredObjectSpaceFactory? factory = application.ServiceProvider
            .GetService(typeof(INonSecuredObjectSpaceFactory)) as INonSecuredObjectSpaceFactory;
        return factory != null
            ? factory.CreateNonSecuredObjectSpace(typeof(ApplicationUser))
            : application.CreateObjectSpace(typeof(ApplicationUser));
    }

    static string? NormalizeStoredThemeCaption(string? caption) =>
        string.IsNullOrWhiteSpace(caption)
        || string.Equals(caption, "DevExpress Fluent", StringComparison.Ordinal)
            ? null
            : caption;

    static bool ThemeAlreadyMatchesStored(
        IThemeService themeService,
        IXafSizeModeService? sizeModeService,
        string themeCaption,
        string? themeMode,
        string? sizeMode)
    {
        if (!string.Equals(GetPersistedThemeCaption(themeService), themeCaption, StringComparison.Ordinal))
        {
            return false;
        }

        if (!IsClassicTheme(themeService.CurrentTheme))
        {
            string? currentMode = FormatThemeMode(themeService.CurrentFluentTheme?.Mode);
            if (!string.Equals(currentMode, themeMode, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        if (string.IsNullOrWhiteSpace(sizeMode))
        {
            return true;
        }

        string? currentSizeMode = sizeModeService?.SizeMode switch
        {
            SizeMode.Small => "Compact",
            SizeMode.Medium => "Standard",
            _ => null
        };
        return string.Equals(currentSizeMode, sizeMode, StringComparison.OrdinalIgnoreCase);
    }

    static string? GetPersistedThemeCaption(IThemeService? themeService)
    {
        Theme? theme = themeService?.CurrentTheme;
        if (theme == null)
        {
            return null;
        }

        if (IsClassicTheme(theme))
        {
            return theme.Caption;
        }

        DxThemeFluent? fluent = themeService.CurrentFluentTheme;
        if (fluent != null)
        {
            return FormatFluentAccentCaption(fluent.AccentColor);
        }

        return FormatFluentAccentCaption(theme.AccentColor);
    }

    static Theme? TryResolveFluentTheme(IThemeService themeService, string themeCaption) =>
        themeService.GetThemeByCaption(themeCaption)
        ?? themeService.GetThemeByCaption("DevExpress Fluent");

    static string FormatFluentAccentCaption(ThemeFluentAccentColor accent) =>
        accent switch
        {
            ThemeFluentAccentColor.CoolBlue => "Cool Blue",
            _ => accent.ToString()
        };

    static ThemeFluentAccentColor? TryParseFluentAccentFromCaption(string caption)
    {
        if (string.Equals(caption, "Cool Blue", StringComparison.OrdinalIgnoreCase))
        {
            return ThemeFluentAccentColor.CoolBlue;
        }

        return Enum.TryParse(caption, true, out ThemeFluentAccentColor parsed)
            ? parsed
            : null;
    }

    static bool IsClassicTheme(Theme? theme) =>
        theme != null && !string.IsNullOrWhiteSpace(theme.Url);

    static string? FormatThemeMode(ThemeMode? mode) =>
        mode switch
        {
            ThemeMode.Dark => "Dark",
            ThemeMode.Light => "Light",
            _ => null
        };

    static bool TryParseThemeMode(string? value, out ThemeMode mode)
    {
        if (string.Equals(value, "Dark", StringComparison.OrdinalIgnoreCase))
        {
            mode = ThemeMode.Dark;
            return true;
        }

        if (string.Equals(value, "Light", StringComparison.OrdinalIgnoreCase))
        {
            mode = ThemeMode.Light;
            return true;
        }

        mode = ThemeMode.Light;
        return false;
    }

    static bool TryParseSizeMode(string value, out SizeMode mode)
    {
        if (string.Equals(value, "Compact", StringComparison.OrdinalIgnoreCase))
        {
            mode = SizeMode.Small;
            return true;
        }

        if (string.Equals(value, "Standard", StringComparison.OrdinalIgnoreCase))
        {
            mode = SizeMode.Medium;
            return true;
        }

        mode = SizeMode.Medium;
        return false;
    }
}
