using System;

namespace Visa2026.Module.Services.OfficerShell;

public sealed class OfficerShellStagedRow
{
    public Guid ApplicationProfileInstanceId { get; init; }
    public string PersonName { get; init; } = string.Empty;
    public string ProfileName { get; init; } = string.Empty;
    public string? ProfileCode { get; init; }
    public string? ProjectName { get; init; }
    public string StagedOn { get; init; } = string.Empty;
    public string Readiness { get; init; } = "ready";
    public bool IsSelectable { get; init; } = true;
    public string? MissingSummary { get; init; }
    public string SearchHaystack { get; init; } = string.Empty;
    public string TemplateFamilyKey { get; init; } = OfficerShellTemplateFamily.Invitation;
    public Guid? ProfileId { get; init; }
}
