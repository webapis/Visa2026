using Visa2026.Module.Services.PreviewSlot;

namespace Visa2026.Blazor.Server.Editors;

public sealed class OfficerShellCaseProgressAdvanceRequest
{
    public string? StateCode { get; init; }

    public string Notes { get; init; } = string.Empty;

    public DateTime Date { get; init; }

    public string? LetterFileName { get; init; }

    public byte[]? LetterContent { get; init; }

    public string? ProcessNumber { get; init; }

    public string? ResultNumber { get; init; }

    public string? SubmissionDocumentNumber { get; init; }

    public IReadOnlyList<ProgressLetterInlineFile> AdditionalFiles { get; init; }
        = Array.Empty<ProgressLetterInlineFile>();
}

public sealed class OfficerShellCaseProgressStepCorrection
{
    public Guid ProgressId { get; init; }

    public DateTime Date { get; init; }

    public string? SubmissionDocumentNumber { get; init; }

    public string? ResultNumber { get; init; }
}

public sealed class OfficerShellCaseProgressFileChange
{
    public Guid ProgressId { get; init; }

    public Guid? FileId { get; init; }

    public bool Remove { get; init; }

    public int Slot { get; init; }

    public string FileName { get; init; } = string.Empty;

    public byte[] Content { get; init; } = Array.Empty<byte>();
}

public sealed class OfficerShellCaseMigrationSubmissionRequest
{
    public DateTime Date { get; init; }

    public string? DocumentNumber { get; init; }

    public string? FileName { get; init; }

    public byte[]? FileContent { get; init; }
}

public sealed class OfficerShellCaseProgressRevertRequest
{
    public string? StepKey { get; init; }
}

public sealed class OfficerShellCaseProgressFileUpload
{
    public string FileName { get; init; } = string.Empty;

    public byte[] Content { get; init; } = Array.Empty<byte>();

    public Guid? ProgressId { get; init; }
}
