using System;

namespace Visa2026.Module.Services.OfficerShell;

public sealed class OfficerShellInProcessRow
{
    public Guid ApplicationProfileInstanceId { get; init; }
    public string ApplicationNumber { get; init; } = string.Empty;
    public string? ProcessNumber { get; init; }
    public string PersonName { get; init; } = string.Empty;
    public string ProfileName { get; init; } = string.Empty;
    public string? ProfileCode { get; init; }
    public string? ProjectName { get; init; }
    public string StartedOn { get; init; } = string.Empty;
    public string CurrentStep { get; init; } = string.Empty;
    public int? SlaDaysRemaining { get; init; }
    public string Status { get; init; } = "process";
    public string SearchHaystack { get; init; } = string.Empty;
    public string TemplateFamilyKey { get; init; } = OfficerShellTemplateFamily.Invitation;
}
