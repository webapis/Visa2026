using System;
using System.Collections.Generic;
using DevExpress.ExpressApp;

namespace Visa2026.Module.Services.ApplicationProfileOverview;

public interface IApplicationProfileOverviewQueryService
{
    ApplicationProfileOverviewSnapshot Load(Guid applicationProfileId, IObjectSpace? objectSpace = null);
}

public sealed class ApplicationProfileOverviewSnapshot
{
    public Guid ApplicationProfileId { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Code { get; init; } = string.Empty;

    public string? SelectionCode { get; init; }

    public string? Description { get; init; }

    public string ActionFamilyLabel { get; init; } = string.Empty;

    public string ProgressRouteLabel { get; init; } = string.Empty;

    public bool IsViaMinistry { get; init; } = true;

    public bool IsAlwaysAvailable { get; init; } = true;

    public string? ApplicabilityCriteria { get; init; }

    public IReadOnlyList<string> AudienceLabels { get; init; } = Array.Empty<string>();

    public bool IsConfigLocked { get; init; }

    public bool IsActive { get; init; } = true;

    public IReadOnlyList<string> LiveConfigurationLines { get; init; } = Array.Empty<string>();

    public int MinistrySlaDays { get; init; }

    public int MigrationSlaDays { get; init; }

    public IReadOnlyList<ApplicationProfileOverviewProgressStateRow> ProgressStates { get; init; }
        = Array.Empty<ApplicationProfileOverviewProgressStateRow>();

    public IReadOnlyList<ApplicationProfileOverviewDefaultRow> PerApplicationDefaults { get; init; }
        = Array.Empty<ApplicationProfileOverviewDefaultRow>();

    public IReadOnlyList<ApplicationProfileOverviewLegRow> ApprovalLegs { get; init; }
        = Array.Empty<ApplicationProfileOverviewLegRow>();

    public IReadOnlyList<ApplicationProfileOverviewVersionRow> ApprovalLegVersions { get; init; }
        = Array.Empty<ApplicationProfileOverviewVersionRow>();

    public IReadOnlyList<string> PersonDataToggles { get; init; } = Array.Empty<string>();

    public IReadOnlyList<ApplicationProfileOverviewTemplateRow> NestedTemplates { get; init; }
        = Array.Empty<ApplicationProfileOverviewTemplateRow>();

    public IReadOnlyList<ApplicationProfileOverviewLinkedAppRow> LinkedApplications { get; init; }
        = Array.Empty<ApplicationProfileOverviewLinkedAppRow>();

    public int LinkedApplicationCount { get; init; }

    public bool IsPrototypeMock { get; init; }
}

public sealed class ApplicationProfileOverviewDefaultRow
{
    public string FieldLabel { get; init; } = string.Empty;

    public string DefaultValue { get; init; } = string.Empty;

    public bool Required { get; init; }
}

public sealed class ApplicationProfileOverviewVersionRow
{
    public string Name { get; init; } = string.Empty;

    public bool IsDefault { get; init; }

    public IReadOnlyList<ApplicationProfileOverviewLegRow> Legs { get; init; }
        = Array.Empty<ApplicationProfileOverviewLegRow>();
}

public sealed class ApplicationProfileOverviewLegRow
{
    public int Sequence { get; init; }

    public string MinistryName { get; init; } = string.Empty;
}

public sealed class ApplicationProfileOverviewProgressStateRow
{
    public string TrackLabel { get; init; } = string.Empty;

    public string StateName { get; init; } = string.Empty;

    public bool IsSlaTracked { get; init; }
}

public sealed class ApplicationProfileOverviewTemplateRow
{
    public string Name { get; init; } = string.Empty;

    public string Kind { get; init; } = string.Empty;

    public string Scope { get; init; } = string.Empty;

    public string DataScope { get; init; } = string.Empty;

    public string? Category { get; init; }
}

public sealed class ApplicationProfileOverviewLinkedAppRow
{
    public Guid ApplicationProfileInstanceId { get; init; }

    public string FullNumber { get; init; } = string.Empty;

    public string ApplicationDate { get; init; } = string.Empty;

    public string Status { get; init; } = string.Empty;
}
