using System.Linq;
using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services.ApplicationProfileWizard;

/// Retained for existing `ApplicationProfileProgressStateSetting` rows. The wizard no longer seeds or edits them.
public static class ApplicationProfileProgressStateSeeder
{
    public static void EnsureDefaults(ApplicationProfile profile, IObjectSpace objectSpace)
    {
        if (profile == null || objectSpace == null || objectSpace.IsNewObject(profile))
            return;

        if (profile.ProgressStateSettings == null || profile.ProgressStateSettings.Count > 0)
            return;

        foreach (var row in ApplicationProfileProgressStateCatalog.All)
        {
            var setting = objectSpace.CreateObject<ApplicationProfileProgressStateSetting>();
            setting.ApplicationProfile = profile;
            setting.Track = row.Track;
            setting.StateCode = row.StateCode;
            setting.IsIncluded = row.DefaultIncluded;
            setting.IsSlaTracked = row.DefaultSlaTracked;
        }
    }

    public static string GetDisplayName(ApplicationProfileProgressStateSetting setting)
    {
        var row = ApplicationProfileProgressStateCatalog.All
            .FirstOrDefault(r => r.Track == setting.Track
                && string.Equals(r.StateCode, setting.StateCode, System.StringComparison.OrdinalIgnoreCase));
        return row?.DisplayName ?? setting.StateCode;
    }
}
