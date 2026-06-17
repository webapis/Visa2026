using System;
using System.Collections.Generic;
using System.Linq;

namespace Visa2026.Module.Services.PersonLinkedDocuments;

public sealed class PersonLinkedDocumentsSnapshot
{
    public Guid PersonId { get; init; }

    public string PersonDisplayName { get; init; } = string.Empty;

    public string? PersonalNumber { get; init; }

    public IReadOnlyList<PersonLinkedDocumentSection> Sections { get; init; } =
        Array.Empty<PersonLinkedDocumentSection>();

    public PersonLinkedDocumentRecord? FindRecord(string recordKey) =>
        Sections
            .SelectMany(section => section.Records)
            .FirstOrDefault(record => string.Equals(record.RecordKey, recordKey, StringComparison.Ordinal));

    public bool ContainsFile(string recordKey, Guid fileDataId)
    {
        var record = FindRecord(recordKey);
        return record != null && record.Files.Any(file => file.FileDataId == fileDataId);
    }
}
