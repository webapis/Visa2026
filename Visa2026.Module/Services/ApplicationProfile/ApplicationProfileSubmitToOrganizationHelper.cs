using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp;
using Visa2026.Module.Localization;
using Visa2026.Module.Services.ApplicationWorkspace;
using Visa2026.Module.Services.OfficerShell;

namespace Visa2026.Module.BusinessObjects;

/// <summary>
/// Destination for profiles with <see cref="ApplicationProfile.RequireSubmitToOrganization"/>:
/// one ApprovingMinistry or Migration service (not Approval legs).
/// </summary>
public static class ApplicationProfileSubmitToOrganizationHelper
{
    public const string MinistryValuePrefix = "ministry:";
    public const string MigrationValuePrefix = "migration:";

    /// <summary>Single Submit-to card for any migration office (not per-department rows).</summary>
    public const string MigrationGenericValue = MigrationValuePrefix + "any";

    private const string MigrationServiceDisplayKey = "ApplicationProfileInstance.Workspace.MigrationService";

    public static bool IsRequired(ApplicationProfile? profile) =>
        profile?.RequireSubmitToOrganization == true;

    public static bool IsRequired(ApplicationProfileInstance? application) =>
        IsRequired(application?.ApplicationProfile);

    public static string StaticMigrationServiceDisplayName() =>
        VisaUiMessages.Get(MigrationServiceDisplayKey);

    public static string? FormatDestinationLabel(ApplicationProfileInstance? application)
    {
        if (application == null)
            return null;

        var ministry = application.SubmitToApprovingMinistry;
        if (ministry != null)
        {
            var shortName = ministry.ShortNameTm?.Trim();
            if (!string.IsNullOrWhiteSpace(shortName))
                return shortName;
            return ministry.NameTm?.Trim();
        }

        if (application.SubmitToMigrationService != null
            || application.SubmitToMigrationServiceId is Guid mid && mid != Guid.Empty)
            return StaticMigrationServiceDisplayName();

        return null;
    }

    public static string TimelineSubmissionLabel(ApplicationProfileInstance? application)
    {
        var name = FormatDestinationLabel(application);
        // Direct-create (no destination) uses the same generic hand-in label as invitation change.
        return string.IsNullOrWhiteSpace(name)
            ? ApplicationMigrationSubmission.TimelineLabel
            : "Submitted to " + name;
    }

    public static string EncodeValue(ApplicationProfileInstance? application)
    {
        if (application?.SubmitToApprovingMinistryId is Guid ministryId && ministryId != Guid.Empty)
            return MinistryValuePrefix + ministryId.ToString("D");
        if (application?.SubmitToApprovingMinistry?.ID is Guid mid && mid != Guid.Empty)
            return MinistryValuePrefix + mid.ToString("D");

        // Any migration office shares one destination key (one localized card).
        if (application?.SubmitToMigrationServiceId is Guid migrationId && migrationId != Guid.Empty)
            return MigrationGenericValue;
        if (application?.SubmitToMigrationService?.ID is Guid sid && sid != Guid.Empty)
            return MigrationGenericValue;

        return string.Empty;
    }

    public static bool TryParseValue(string? value, out string kind, out Guid id)
    {
        kind = string.Empty;
        id = Guid.Empty;
        if (string.IsNullOrWhiteSpace(value))
            return false;

        var trimmed = value.Trim();
        if (trimmed.StartsWith(MinistryValuePrefix, StringComparison.OrdinalIgnoreCase)
            && Guid.TryParse(trimmed[MinistryValuePrefix.Length..], out id))
        {
            kind = "ministry";
            return true;
        }

        if (string.Equals(trimmed, MigrationGenericValue, StringComparison.OrdinalIgnoreCase)
            || string.Equals(trimmed, MigrationValuePrefix, StringComparison.OrdinalIgnoreCase))
        {
            kind = "migration";
            id = Guid.Empty;
            return true;
        }

        if (trimmed.StartsWith(MigrationValuePrefix, StringComparison.OrdinalIgnoreCase)
            && Guid.TryParse(trimmed[MigrationValuePrefix.Length..], out id))
        {
            kind = "migration";
            return true;
        }

        // Legacy plain Guid: resolve later by table lookup.
        if (Guid.TryParse(trimmed, out id))
        {
            kind = "unknown";
            return true;
        }

        return false;
    }

    public static bool TryApply(
        IObjectSpace objectSpace,
        ApplicationProfileInstance application,
        string? value,
        out string? error)
    {
        error = null;
        if (objectSpace == null || application == null)
        {
            error = "Application is not ready.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            if (!ApplicationProfileInstanceSeretmezlikRosterHelper.TryValidateSubmitToChange(
                    objectSpace, application, string.Empty, out error))
                return false;

            application.SubmitToApprovingMinistry = null;
            application.SubmitToApprovingMinistryId = null;
            application.SubmitToMigrationService = null;
            application.SubmitToMigrationServiceId = null;
            return true;
        }

        if (!TryParseValue(value, out var kind, out var id))
        {
            error = "Select a ministry or migration service.";
            return false;
        }

        if (kind == "ministry"
            || (kind == "unknown" && objectSpace.GetObjectByKey<ApprovingMinistry>(id) != null))
        {
            if (id == Guid.Empty)
            {
                error = "Select a ministry or migration service.";
                return false;
            }

            var ministry = objectSpace.GetObjectByKey<ApprovingMinistry>(id);
            if (ministry == null)
            {
                error = "Ministry not found.";
                return false;
            }

            var ministryValue = MinistryValuePrefix + ministry.ID.ToString("D");
            if (!ApplicationProfileInstanceSeretmezlikRosterHelper.TryValidateSubmitToChange(
                    objectSpace, application, ministryValue, out error))
                return false;

            application.SubmitToApprovingMinistry = ministry;
            application.SubmitToApprovingMinistryId = ministry.ID;
            application.SubmitToMigrationService = null;
            application.SubmitToMigrationServiceId = null;
            return true;
        }

        var migration = id != Guid.Empty
            ? objectSpace.GetObjectByKey<MigrationService>(id)
            : ResolveCanonicalMigrationService(objectSpace);
        if (migration == null)
        {
            error = "Migration service not found.";
            return false;
        }

        if (!ApplicationProfileInstanceSeretmezlikRosterHelper.TryValidateSubmitToChange(
                objectSpace, application, MigrationGenericValue, out error))
            return false;

        application.SubmitToMigrationService = migration;
        application.SubmitToMigrationServiceId = migration.ID;
        application.SubmitToApprovingMinistry = null;
        application.SubmitToApprovingMinistryId = null;
        return true;
    }

    /// <summary>Ministry cards plus one localized Migration service card.</summary>
    public static IReadOnlyList<ApplicationWorkspaceLookupOption> ListLookupOptions(IObjectSpace objectSpace)
    {
        if (objectSpace == null)
            return Array.Empty<ApplicationWorkspaceLookupOption>();

        var ministries = objectSpace.GetObjectsQuery<ApprovingMinistry>()
            .AsEnumerable()
            .Select(m =>
            {
                var name = string.IsNullOrWhiteSpace(m.ShortNameTm) ? m.NameTm : m.ShortNameTm;
                name = name?.Trim() ?? string.Empty;
                return new ApplicationWorkspaceLookupOption
                {
                    Id = m.ID,
                    DisplayName = string.IsNullOrWhiteSpace(name) ? m.ID.ToString("D") : name,
                    Detail = "ministry",
                    SearchText = MinistryValuePrefix + m.ID.ToString("D"),
                };
            })
            .Where(o => !string.IsNullOrWhiteSpace(o.DisplayName))
            .OrderBy(o => o.DisplayName, StringComparer.CurrentCultureIgnoreCase);

        var options = ministries.ToList();
        var canonical = ResolveCanonicalMigrationService(objectSpace);
        if (canonical != null)
        {
            options.Add(new ApplicationWorkspaceLookupOption
            {
                Id = canonical.ID,
                DisplayName = StaticMigrationServiceDisplayName(),
                Detail = "migration",
                SearchText = MigrationGenericValue,
            });
        }

        return options;
    }

    public static bool HasDestination(ApplicationProfileInstance? application) =>
        !string.IsNullOrWhiteSpace(FormatDestinationLabel(application));

    private static MigrationService? ResolveCanonicalMigrationService(IObjectSpace objectSpace) =>
        objectSpace.GetObjectsQuery<MigrationService>()
            .AsEnumerable()
            .OrderBy(m => m.NameTm ?? m.Code ?? string.Empty, StringComparer.CurrentCultureIgnoreCase)
            .FirstOrDefault();
}