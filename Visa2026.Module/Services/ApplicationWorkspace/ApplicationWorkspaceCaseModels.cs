using System;
using System.Collections.Generic;

namespace Visa2026.Module.Services.ApplicationWorkspace;

public sealed class ApplicationWorkspaceCaseView
{
    public ApplicationWorkspaceCaseChrome Chrome { get; init; } = new();

    public IReadOnlyList<ApplicationWorkspaceCaseSummaryTile> SummaryTiles { get; init; }
        = Array.Empty<ApplicationWorkspaceCaseSummaryTile>();

    public IReadOnlyList<ApplicationWorkspaceCaseLinkedTile> LinkedRecordTiles { get; init; }
        = Array.Empty<ApplicationWorkspaceCaseLinkedTile>();

    public IReadOnlyList<ApplicationWorkspaceCaseIssuedTile> IssuedRecordTiles { get; init; }
        = Array.Empty<ApplicationWorkspaceCaseIssuedTile>();

    public IReadOnlyList<ApplicationWorkspaceCaseProgressStep> ProgressSteps { get; init; }
        = Array.Empty<ApplicationWorkspaceCaseProgressStep>();

    public IReadOnlyList<ApplicationWorkspaceCasePerson> People { get; init; }
        = Array.Empty<ApplicationWorkspaceCasePerson>();

    public IReadOnlyList<ApplicationWorkspaceCaseActivity> Activities { get; init; }
        = Array.Empty<ApplicationWorkspaceCaseActivity>();

    public ApplicationWorkspaceCasePeopleSummary PeopleSummary { get; init; } = new();

    public IReadOnlyDictionary<string, int> LinkedRecordsSummary { get; init; }
        = new Dictionary<string, int>();

    public ApplicationWorkspaceCaseSlaDashboard Sla { get; init; } = new();
}

public sealed class ApplicationWorkspaceCaseSummaryTile
{
    public string Label { get; init; } = string.Empty;

    public string Value { get; init; } = string.Empty;

    public string Tone { get; init; } = "blue";

    public string Glyph { get; init; } = "•";
}

public sealed class ApplicationWorkspaceCaseLinkedTile
{
    public string TabKey { get; init; } = string.Empty;

    public string Label { get; init; } = string.Empty;

    public int Count { get; init; }

    public string Tone { get; init; } = "blue";

    public string Glyph { get; init; } = "•";
}

public sealed class ApplicationWorkspaceCaseIssuedTile
{
    public string Key { get; init; } = string.Empty;

    public string Label { get; init; } = string.Empty;

    public int Count { get; init; }

    public string Tone { get; init; } = "blue";

    public string Glyph { get; init; } = "•";

    public string AddCaption { get; init; } = string.Empty;

    public string NewCaption { get; init; } = string.Empty;

    public string PanelTitle { get; init; } = string.Empty;

    public string EmptyHint { get; init; } = string.Empty;

    public IReadOnlyList<ApplicationWorkspaceCaseIssuedRow> Rows { get; init; }
        = Array.Empty<ApplicationWorkspaceCaseIssuedRow>();
}

public sealed class ApplicationWorkspaceCaseIssuedRow
{
    public Guid Id { get; init; }

    public string Title { get; init; } = string.Empty;

    public string Subtitle { get; init; } = string.Empty;
}

public sealed class ApplicationWorkspaceIssuedHeaderOpenRequest
{
    public string Key { get; init; } = string.Empty;

    public Guid Id { get; init; }
}

public sealed class ApplicationWorkspaceCaseProgressStep
{
    public string Key { get; init; } = string.Empty;

    public string Label { get; init; } = string.Empty;

    public string Date { get; init; } = string.Empty;

    public string State { get; init; } = "pending";

    public string CurrentStateLabel { get; init; } = string.Empty;

    public string SlaTargetDate { get; init; } = string.Empty;

    public int? SlaDaysRemaining { get; init; }

    public Guid? ProgressId { get; init; }

    public string OfficerNotes { get; init; } = string.Empty;

    public string MinistryLetterFileName { get; init; } = string.Empty;

    public bool ShowMinistryLetterUpload { get; init; }

    /// <summary>
    /// Visual tone for the stepper: pending, current, ok, issued, rejected, cancelled.
    /// Terminal outcomes (issued / rejected / cancelled) win over slot current/done.
    /// </summary>
    public string OutcomeKind { get; init; } = "pending";

    public bool CanAdvance { get; init; }

    public string AdvanceBlockedReason { get; init; } = string.Empty;

    public IReadOnlyList<ApplicationWorkspaceCaseProgressAdvanceOption> AdvanceOptions { get; init; }
        = Array.Empty<ApplicationWorkspaceCaseProgressAdvanceOption>();
}

public sealed class ApplicationWorkspaceCaseProgressAdvanceOption
{
    public string StateCode { get; init; } = string.Empty;

    public string Label { get; init; } = string.Empty;
}

public sealed class ApplicationWorkspaceCasePerson
{
    public int Index { get; init; }

    public Guid PersonId { get; init; }

    public Guid ApplicationProfileInstancePersonId { get; init; }

    public string Name { get; init; } = string.Empty;

    public string RoleLabel { get; init; } = string.Empty;

    public string PassportNumber { get; init; } = string.Empty;

    public string VisaNumber { get; init; } = string.Empty;

    public IReadOnlyList<ApplicationWorkspaceCasePersonRecord> Records { get; init; }
        = Array.Empty<ApplicationWorkspaceCasePersonRecord>();
}

public sealed class ApplicationWorkspaceCasePersonRecord
{
    public string Key { get; init; } = string.Empty;

    public string Label { get; init; } = string.Empty;

    public int Count { get; init; }

    public string State { get; init; } = "empty";

    public string Glyph { get; init; } = "•";

    public string Tone { get; init; } = "blue";
}

public sealed class ApplicationWorkspaceCaseActivity
{
    public string Title { get; init; } = string.Empty;

    public string Subtitle { get; init; } = string.Empty;
}

public sealed class ApplicationWorkspaceCasePeopleSummary
{
    public int Total { get; init; }

    public int Primary { get; init; }

    public int Dependents { get; init; }

    public int Sponsors { get; init; }
}

public sealed class ApplicationWorkspaceCaseSlaDashboard
{
    public int? CaseDaysRemaining { get; init; }

    public int TotalSlaDays { get; init; }

    public int ElapsedDays { get; init; }

    public int? CurrentStepDaysRemaining { get; init; }

    public string CurrentStepDueDate { get; init; } = string.Empty;

    public string StartedOn { get; init; } = string.Empty;

    public string MinistryDueDate { get; init; } = string.Empty;

    public string ExpectedCompletionDate { get; init; } = string.Empty;

    public string MigrationSlaLabel { get; init; } = string.Empty;

    public string ProfileSlaSource { get; init; } = string.Empty;

    public string AlertMessage { get; init; } = string.Empty;

    public IReadOnlyList<ApplicationWorkspaceCaseSlaDeadline> Deadlines { get; init; }
        = Array.Empty<ApplicationWorkspaceCaseSlaDeadline>();
}

public sealed class ApplicationWorkspaceCaseSlaDeadline
{
    public string Step { get; init; } = string.Empty;

    public string DueDate { get; init; } = string.Empty;

    public string DaysLeft { get; init; } = string.Empty;

    public string Status { get; init; } = "pending";

    public bool IsCurrent { get; init; }
}
