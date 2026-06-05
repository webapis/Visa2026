using System;

namespace Visa2026.Module.Services.ApplicationItemLinkedDocuments;

public sealed class ApplicationItemLinkedDocumentFileEntry
{
    public Guid ApplicationItemId { get; init; }

    public string LineLabel { get; init; } = string.Empty;

    public ApplicationItemLinkedDocumentFile File { get; init; } = null!;
}
