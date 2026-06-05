using System;

namespace Visa2026.Module.Services.ApplicationItemLinkedDocuments;

/// <summary>One scanned copy (<see cref="BusinessObjects.DocumentBase.File"/>) linked to an application line.</summary>
public sealed class ApplicationItemLinkedDocumentFile
{
    public Guid FileDataId { get; init; }

    public Guid DocumentRowId { get; init; }

    public string DocumentTypeName { get; init; } = string.Empty;

    public string FileName { get; init; } = string.Empty;

    public int SizeBytes { get; init; }

    public bool HasContent { get; init; }
}
