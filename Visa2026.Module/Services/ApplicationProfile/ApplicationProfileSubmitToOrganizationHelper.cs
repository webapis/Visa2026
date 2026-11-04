using System;
using System.Linq;
using DevExpress.ExpressApp;

namespace Visa2026.Module.BusinessObjects;

/// <summary>
/// Destination for profiles with <see cref="ApplicationProfile.RequireSubmitToOrganization"/>:
/// one ApprovingMinistry or one MigrationService (not Approval legs).
/// </summary>
public static class ApplicationProfileSubmitToOrganizationHelper
{
    public const string MinistryValuePrefix = "ministry:";
    public const string MigrationValuePrefix = "migration:";

    public static bool IsRequired(ApplicationProfile? profile) =>
        profile?.RequireSubmitToOrganization == true;

    public static bool IsRequired(ApplicationProfileInstance? application) =>
        IsRequired(application?.ApplicationProfile);

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

        return application.SubmitToMigrationService?.NameTm?.Trim();
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
        if (application?.SubmitToMigrationServiceId is Guid migrationId && migrationId != Guid.Empty)
            return MigrationValuePrefix + migrationId.ToString("D");
        if (application?.SubmitToApprovingMinistry?.ID is Guid mid && mid != Guid.Empty)
            return MinistryValuePrefix + mid.ToString("D");
        if (application?.SubmitToMigrationService?.ID is Guid sid && sid != Guid.Empty)
            return MigrationValuePrefix + sid.ToString("D");
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
            application.SubmitToApprovingMinistry = null;
            application.SubmitToApprovingMinistryId = null;
            application.SubmitToMigrationService = null;
            application.SubmitToMigrationServiceId = null;
            return true;
        }

        if (!TryParseValue(value, out var kind, out var id) || id == Guid.Empty)
        {
            error = "Select a ministry or migration service.";
            return false;
        }

        if (kind == "ministry"
            || (kind == "unknown" && objectSpace.GetObjectByKey<ApprovingMinistry>(id) != null))
        {
            var ministry = objectSpace.GetObjectByKey<ApprovingMinistry>(id);
            if (ministry == null)
            {
                error = "Ministry not found.";
                return false;
            }

            application.SubmitToApprovingMinistry = ministry;
            application.SubmitToApprovingMinistryId = ministry.ID;
            application.SubmitToMigrationService = null;
            application.SubmitToMigrationServiceId = null;
            return true;
        }

        var migration = objectSpace.GetObjectByKey<MigrationService>(id);
        if (migration == null)
        {
            error = "Migration service not found.";
            return false;
        }

        application.SubmitToMigrationService = migration;
        application.SubmitToMigrationServiceId = migration.ID;
        application.SubmitToApprovingMinistry = null;
        application.SubmitToApprovingMinistryId = null;
        return true;
    }

    public static bool HasDestination(ApplicationProfileInstance? application) =>
        !string.IsNullOrWhiteSpace(FormatDestinationLabel(application));
}