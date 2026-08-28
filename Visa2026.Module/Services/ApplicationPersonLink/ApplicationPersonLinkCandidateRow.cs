using System;

namespace Visa2026.Module.Services.ApplicationPersonLink;

public sealed class ApplicationProfileInstancePersonLinkCandidateRow
{
    public Guid PersonId { get; init; }

    public string FullName { get; init; } = string.Empty;

    public string PersonalNumber { get; init; } = string.Empty;

    public string RoleLabel { get; init; } = string.Empty;

    public string PassportNumber { get; init; } = string.Empty;

    public bool HasPhoto { get; init; }

    public bool CanLink { get; init; } = true;

    public string? BlockReason { get; init; }
}
