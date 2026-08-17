namespace Visa2026.Blazor.Server.Editors;

public sealed class OfficerShellCaseProgressAdvanceRequest
{
    public string? StateCode { get; init; }

    public string Notes { get; init; } = string.Empty;

    public DateTime Date { get; init; }
}

public sealed class OfficerShellCaseProgressRevertRequest
{
    public string? StepKey { get; init; }
}

public sealed class OfficerShellCaseProgressFileUpload
{
    public string FileName { get; init; } = string.Empty;

    public byte[] Content { get; init; } = Array.Empty<byte>();
}
