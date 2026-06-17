using System;

namespace Visa2026.Module.Services.PersonLinkedDocuments;

/// <summary>One scanned copy linked to a person child record.</summary>
public sealed class PersonLinkedDocumentFile
{
    public Guid FileDataId { get; init; }

    public Guid DocumentRowId { get; init; }

    public string DocumentTypeName { get; init; } = string.Empty;

    public string FileName { get; init; } = string.Empty;

    public int SizeBytes { get; init; }

    public bool HasContent { get; init; }
}
