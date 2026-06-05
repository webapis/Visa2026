using System;

namespace Visa2026.Module.Services.ApplicationItemLinkedDocuments;

public sealed class ApplicationItemLinkedDocumentMissingLineEntry
{
    public Guid ApplicationItemId { get; init; }

    public string LineLabel { get; init; } = string.Empty;

    public bool LinkMissing { get; init; }
}
