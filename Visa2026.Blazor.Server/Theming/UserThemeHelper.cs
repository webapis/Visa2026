using DevExpress.Blazor;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor;
using DevExpress.ExpressApp.Blazor.Services;
using DevExpress.ExpressApp.Security;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Blazor.Server.Theming;

/// <summary>
/// Applies and persists per-user UI theme and size mode (mirrors <see cref="Localization.UserCultureHelper"/>).
/// </summary>
public static class UserThemeHelper
{
    static int applyDepth;

    public static async Task ApplyStoredThemeAfterLogonAsync(BlazorApplication application)
    {
        if (SecuritySystem.CurrentUser is not ApplicationUser user)
        {
            return;
        }

        string? themeCaption;
        string? themeMode;
        string? sizeMode;
        using (IObjectSpace objectSpace = application.CreateObjectSpace(typeof(ApplicationUser)))
        {
            ApplicationUser? storedUser = objectSpace.GetObjectByKey<ApplicationUser>(user.ID);
            if (storedUser == null)
            {
                return;
            }

            themeCaption = storedUser.PreferredThemeCaption;
            themeMode = storedUser.PreferredThemeMode;
            sizeMode = storedUser.PreferredSizeMode;
        }

        if (string.IsNullOrWhiteSpace(themeCaption) && string.IsNullOrWhiteSpace(sizeMode))
        {
            return;
        }

        IServiceProvider services = application.ServiceProvider;
        IThemeService? themeService = services.GetService(typeof(IThemeService)) as IThemeService;
        IXafSizeModeService? sizeModeService = services.GetService(typeof(IXafSizeModeService)) as IXafSizeModeService;

        if (themeService != null
            && !string.IsNullOrWhiteSpace(themeCaption)
            && ThemeAlreadyMatchesStored(themeService, sizeModeService, themeCaption, themeMode, sizeMode))
        {
            return;
        }

        applyDepth++;
        try
        {
            if (!string.IsNullOrWhiteSpace(themeCaption) && themeService != null)
            {
                Theme? theme = themeService.GetThemeByCaption(themeCaption);
                theme ??= TryResolveFluentTheme(themeService, themeCaption);

                if (theme != null)
                {
                    await themeService.SetCurrentThemeAsync(theme).ConfigureAwait(false);

                    if (TryParseThemeMode(themeMode, out ThemeMode parsedMode))
                    {
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
        finally
        {
            applyDepth--;
        }
    }

    public static void PersistCurrentThemeToUser(XafApplication application)
    {
        if (applyDepth > 0 || SecuritySystem.CurrentUser is not ApplicationUser user)
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

        if (string.Equals(user.PreferredThemeCaption, currentCaption, StringComparison.Ordinal)
            && string.Equals(user.PreferredThemeMode, currentMode, StringComparison.OrdinalIgnoreCase)
            && string.Equals(user.PreferredSizeMode, currentSizeMode, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        using IObjectSpace objectSpace = application.CreateObjectSpace(typeof(ApplicationUser));
        ApplicationUser userInOs = objectSpace.GetObjectByKey<ApplicationUser>(user.ID);
        if (userInOs == null)
        {
            return;
        }

        userInOs.PreferredThemeCaption = currentCaption;
        userInOs.PreferredThemeMode = currentMode;
        userInOs.PreferredSizeMode = currentSizeMode;
        objectSpace.CommitChanges();
    }

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

        return FormatFluentAccentCaption(theme.AccentColor);
    }

    static Theme? TryResolveFluentTheme(IThemeService themeService, string themeCaption)
    {
        ThemeFluentAccentColor? accent = TryParseFluentAccentFromCaption(themeCaption);
        if (accent == null)
        {
            return null;
        }

        return themeService.GetThemeByCaption(themeCaption)
            ?? themeService.GetThemeByCaption("DevExpress Fluent");
    }

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
