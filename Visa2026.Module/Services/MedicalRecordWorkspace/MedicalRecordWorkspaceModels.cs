using System;
using System.Collections.Generic;

namespace Visa2026.Module.Services.MedicalRecordWorkspace;

public enum MedicalRecordWorkspaceMode
{
    List,
    Detail
}

public sealed class MedicalRecordListResult
{
    public IReadOnlyList<MedicalRecordListRow> Rows { get; init; } = Array.Empty<MedicalRecordListRow>();
    public bool Truncated { get; init; }
}

public sealed class MedicalRecordListRow
{
    public Guid Id { get; init; }
    public Guid PersonId { get; init; }
    public bool HasCopies { get; init; }
    public string PersonName { get; init; } = string.Empty;
    public string PersonRole { get; init; } = string.Empty;
    public string DocumentNumber { get; init; } = string.Empty;
    public DateTime IssueDate { get; init; }
    public string Validity { get; init; } = string.Empty;
    public DateTime? ExpirationDate { get; init; }
    public int? DaysRemaining { get; init; }
}

public sealed class MedicalRecordPersonHit
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Role { get; init; } = string.Empty;
    public string PersonalNumber { get; init; } = string.Empty;
}

public sealed class MedicalRecordValidityOption
{
    public Guid Id { get; init; }
    public string Caption { get; init; } = string.Empty;
    public int NumberOfDays { get; init; }
}

public sealed class MedicalRecordScanDto
{
    public Guid Id { get; init; }
    public Guid FileId { get; init; }
    public bool HasContent { get; init; }
    public string FileName { get; init; } = string.Empty;
}

public sealed class MedicalRecordPreviewRequest
{
    public Guid PersonId { get; init; }
    public Guid? RecordId { get; init; }
    public Guid FileId { get; init; }
    public string DisplayName { get; init; } = string.Empty;
    public byte[]? InlineContent { get; init; }
}

public sealed class MedicalRecordDetailDto
{
    public Guid? Id { get; init; }
    public Guid PersonId { get; init; }
    public string PersonName { get; init; } = string.Empty;
    public string DocumentNumber { get; init; } = string.Empty;
    public DateTime IssueDate { get; init; }
    public Guid? ValidityDurationId { get; init; }
    public DateTime? ExpirationDate { get; init; }
    public IReadOnlyList<MedicalRecordScanDto> Scans { get; init; } = Array.Empty<MedicalRecordScanDto>();
}

public sealed class MedicalRecordFileChange
{
    public Guid ScanId { get; init; }
    public string FileName { get; init; } = string.Empty;
    public byte[] Content { get; init; } = Array.Empty<byte>();
}

public sealed class MedicalRecordSaveRequest
{
    public Guid? Id { get; init; }
    public Guid PersonId { get; init; }
    public string DocumentNumber { get; init; } = string.Empty;
    public DateTime IssueDate { get; init; }
    public Guid ValidityDurationId { get; init; }
    public IReadOnlyList<Guid> RemoveScanIds { get; init; } = Array.Empty<Guid>();
    public IReadOnlyList<MedicalRecordFileChange> ReplaceScans { get; init; } = Array.Empty<MedicalRecordFileChange>();
    public IReadOnlyList<MedicalRecordFileChange> AddScans { get; init; } = Array.Empty<MedicalRecordFileChange>();
    public bool CloseAfterSave { get; init; }
}
