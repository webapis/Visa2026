using System;
using System.Collections.Generic;

namespace Visa2026.Module.Services.ApplicationItemLinkedDocuments;

/// <summary>A document slot on an <see cref="BusinessObjects.ApplicationItem"/> (passport, visa, diploma, …).</summary>
public sealed class ApplicationItemLinkedDocumentGroup
{
    public string SlotKey { get; init; } = string.Empty;

    public string SlotLabel { get; init; } = string.Empty;

    public Type? SourceObjectType { get; init; }

    public Guid? SourceObjectId { get; init; }

    public string? SourceCaption { get; init; }

    /// <summary>Slot is in scope for this application type but the line FK is not set.</summary>
    public bool LinkMissing { get; init; }

    public IReadOnlyList<ApplicationItemLinkedDocumentFile> Files { get; init; } =
        Array.Empty<ApplicationItemLinkedDocumentFile>();
}
