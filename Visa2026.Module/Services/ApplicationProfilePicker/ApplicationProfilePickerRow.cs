using System;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services.ApplicationProfilePicker;

public sealed class ApplicationProfilePickerRow
{
    public Guid ProfileId { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Code { get; init; } = string.Empty;

    public string? SelectionCode { get; init; }

    public ApplicationProfileActionFamily ActionFamily { get; init; }

    public ApplicationProfileRegistrationKind RegistrationKind { get; init; }

    public ApplicationProfileInstanceProgressRouteKind ProgressRoute { get; init; }

    public bool IsConfigLocked { get; init; }

    public DateTime? LastUsedAt { get; init; }

    public int UsedBySeedPersonCount { get; init; }

    public DateTime? LastUsedBySeedPersonAt { get; init; }

    public bool HasOpenApplicationForSeedPerson { get; init; }

    public IReadOnlyList<ApplicationProfilePickerVersionOption> ApprovalLegVersions { get; init; }
        = Array.Empty<ApplicationProfilePickerVersionOption>();

    public bool RequiresApprovalLegVersion =>
        ProgressRoute == ApplicationProfileInstanceProgressRouteKind.ViaMinistries
        && ApprovalLegVersions.Count > 0;

    public string MetaLine =>
        $"{Code} · Related to: {ApplicationProfilePickerDisplayHelper.FormatRelatedTo(ActionFamily, RegistrationKind)} · "
        + ApplicationProfilePickerDisplayHelper.FormatProgressRoute(ProgressRoute);

    public string SeedUsageLine
    {
        get
        {
            if (UsedBySeedPersonCount <= 0)
                return "Not used for this person before";

            var date = LastUsedBySeedPersonAt.HasValue
                ? LastUsedBySeedPersonAt.Value.ToString("dd.MM.yyyy")
                : "—";
            return $"Used {UsedBySeedPersonCount}× · last {date}";
        }
    }
}

public sealed class ApplicationProfilePickerVersionOption
{
    public Guid VersionId { get; init; }

    public string Name { get; init; } = string.Empty;

    public bool IsDefault { get; init; }

    public IReadOnlyList<string> MinistryNames { get; init; } = Array.Empty<string>();
}
