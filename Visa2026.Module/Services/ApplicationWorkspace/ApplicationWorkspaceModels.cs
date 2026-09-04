using System;
using System.Collections.Generic;
using DevExpress.ExpressApp;

namespace Visa2026.Module.Services.ApplicationWorkspace;

public interface IApplicationWorkspaceQueryService
{
    ApplicationWorkspaceSnapshot Load(IObjectSpace objectSpace, Guid applicationId);
}

public sealed class ApplicationWorkspaceSnapshot
{
    public Guid ApplicationProfileInstanceId { get; init; }

    public ApplicationWorkspaceHeader Header { get; init; } = new();

    public IReadOnlyList<ApplicationWorkspaceProgressRow> ProgressHistory { get; init; }
        = Array.Empty<ApplicationWorkspaceProgressRow>();

    public ApplicationWorkspaceProfileSummary Profile { get; init; } = new();

    public IReadOnlyList<ApplicationWorkspaceProfileRailItem> ProfileRail { get; init; }
        = Array.Empty<ApplicationWorkspaceProfileRailItem>();

    public IReadOnlyList<ApplicationWorkspaceTab> Tabs { get; init; }
        = Array.Empty<ApplicationWorkspaceTab>();

    public IReadOnlyList<string> LinkContextItems { get; init; }
        = Array.Empty<string>();

    public ApplicationWorkspaceCaseChrome CaseChrome { get; init; } = new();

    public ApplicationWorkspaceCaseView CaseView { get; init; } = new();

    public bool IsPrototypeMock { get; init; } = true;
}

public sealed class ApplicationWorkspaceCaseChrome
{
    public string DisplayNumber { get; init; } = string.Empty;

    public string ProcessNumber { get; init; } = string.Empty;

    public string TemplateFamilyKey { get; init; } = "inv";

    public string TemplateFamilyLabel { get; init; } = string.Empty;

    public string StartedOn { get; init; } = string.Empty;

    public string CurrentStep { get; init; } = string.Empty;

    public string ProjectName { get; init; } = string.Empty;

    public int? SlaDaysRemaining { get; init; }

    public IReadOnlyList<string> PeopleNames { get; init; } = Array.Empty<string>();

    public int? MergedFromCount { get; init; }

    public bool ShowProcessNumber { get; init; }

    /// <summary>
    /// Live <see cref="BusinessObjects.ApplicationProfile.Name"/> for this case
    /// (same value as the Application Profile ListView column). Not a nested Word/Excel template name.
    /// </summary>
    public string ProfileTemplateName { get; init; } = string.Empty;

    /// <summary>Person roster + resolved child links are immutable (workflow terminal).</summary>
    public bool ResolvedLinksLocked { get; init; }
}

public sealed class ApplicationWorkspaceHeader
{
    public string ApplicationNumber { get; init; } = string.Empty;
    public string ApplicationDate { get; init; } = string.Empty;
    public string Urgency { get; init; } = string.Empty;
    public int ProgressStep { get; init; }
    public int ProgressTotalSteps { get; init; }
    public int SlaDaysElapsed { get; init; }
    public int SlaDaysTotal { get; init; }
}

public sealed class ApplicationWorkspaceProgressRow
{
    public string State { get; init; } = string.Empty;
    public string Date { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
}

public sealed class ApplicationWorkspaceProfileSummary
{
    public Guid ProfileId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Code { get; init; } = string.Empty;
    public IReadOnlyList<string> Chips { get; init; } = Array.Empty<string>();
}

public sealed class ApplicationWorkspaceProfileRailItem
{
    public Guid ProfileId { get; init; }

    public string Key { get; init; } = string.Empty;

    public string Label { get; init; } = string.Empty;

    public bool IsActive { get; init; }
}

public sealed class ApplicationWorkspaceTab
{
    public string Key { get; init; } = string.Empty;
    public string Label { get; init; } = string.Empty;
    public bool Visible { get; init; } = true;
    public IReadOnlyList<string> Columns { get; init; } = Array.Empty<string>();
    public IReadOnlyList<IReadOnlyList<string>> Rows { get; init; } = Array.Empty<IReadOnlyList<string>>();

    /// <summary>Parallel to <see cref="Rows"/> when tab rows map to domain ids (Person tab).</summary>
    public IReadOnlyList<Guid> RowPersonIds { get; init; } = Array.Empty<Guid>();

    /// <summary>Parallel to <see cref="Rows"/> — Person ids on this instance (Person tab). Property name kept for callers.</summary>
    public IReadOnlyList<Guid> RowApplicationProfileInstancePersonIds { get; init; } = Array.Empty<Guid>();

    public string? EmptyMessage { get; init; }
    public string? SqlViewHint { get; init; }
}
