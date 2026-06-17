using System;
using System.Collections.Generic;

namespace Visa2026.Module.Services.PersonLinkedDocuments;

/// <summary>One child BO instance (passport, visa, education, …) with attached scans.</summary>
public sealed class PersonLinkedDocumentRecord
{
    public string RecordKey { get; init; } = string.Empty;

    public string RecordLabel { get; init; } = string.Empty;

    public string? SourceCaption { get; init; }

    public Type? SourceObjectType { get; init; }

    public Guid? SourceObjectId { get; init; }

    public bool IsCurrent { get; init; }

    public bool IsNested { get; init; }

    public IReadOnlyList<PersonLinkedDocumentFile> Files { get; init; } =
        Array.Empty<PersonLinkedDocumentFile>();
}
