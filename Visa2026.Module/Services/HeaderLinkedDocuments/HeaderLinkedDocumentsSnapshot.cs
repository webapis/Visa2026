using System;
using System.Collections.Generic;
using System.Linq;

namespace Visa2026.Module.Services.HeaderLinkedDocuments;

public sealed class HeaderLinkedDocumentsSnapshot
{
    public HeaderDocumentCopiesFamily Family { get; init; }

    public Guid ParentId { get; init; }

    public Guid? ContextItemId { get; init; }

    public string HeaderTitle { get; init; } = string.Empty;

    public string? Subtitle { get; init; }

    public bool ShowSharedScansHint { get; init; }

    public IReadOnlyList<HeaderLinkedDocumentRecord> Records { get; init; } =
        Array.Empty<HeaderLinkedDocumentRecord>();

    public HeaderLinkedDocumentRecord? FindRecord(string recordKey) =>
        Records.FirstOrDefault(record => string.Equals(record.RecordKey, recordKey, StringComparison.Ordinal));

    public bool ContainsFile(string recordKey, Guid fileDataId)
    {
        var record = FindRecord(recordKey);
        return record != null && record.Files.Any(file => file.FileDataId == fileDataId);
    }
}
