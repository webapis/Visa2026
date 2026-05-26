using System.Globalization;
using Microsoft.Extensions.Configuration;
using Visa2026.Module.Localization;

namespace Visa2026.Blazor.Server;

public sealed record UsageLicenseBannerViewModel(bool IsExpired, string Title);

public static class UsageLicensePresenter
{
    public static UsageLicenseBannerViewModel? TryCreate(IConfiguration configuration)
    {
        var options = configuration.GetSection("UsageLicense").Get<UsageLicenseOptions>();
        if (options is not { Enabled: true })
            return null;

        int totalDays = options.TrialDays > 0 ? options.TrialDays : 90;
        if (!TryResolveTrialEndUtc(options, totalDays, out DateTime endUtc))
            return null;

        int daysRemaining = Math.Max(0, (endUtc.Date - DateTime.UtcNow.Date).Days);
        string culture = CultureInfo.CurrentUICulture.Name;
        string titleKey = daysRemaining > 0 ? "UsageLicense.Banner.Title" : "UsageLicense.Banner.TitleExpired";

        return new UsageLicenseBannerViewModel(
            daysRemaining <= 0,
            VisaUiMessages.FormatForCulture(culture, titleKey, daysRemaining));
    }

    static bool TryResolveTrialEndUtc(UsageLicenseOptions options, int totalDays, out DateTime endUtc)
    {
        if (options.TrialEndUtc is DateTime configuredEnd)
        {
            endUtc = DateTime.SpecifyKind(configuredEnd, DateTimeKind.Utc).Date;
            return true;
        }

        if (options.TrialStartUtc is DateTime start)
        {
            endUtc = DateTime.SpecifyKind(start, DateTimeKind.Utc).Date.AddDays(totalDays);
            return true;
        }

        endUtc = default;
        return false;
    }
}
