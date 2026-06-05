using System;
using System.Collections.Generic;

namespace Visa2026.Module.Services.ApplicationItemLinkedDocuments;

public sealed class ApplicationItemLinkedDocumentsLineSnapshot
{
    public Guid ApplicationItemId { get; init; }

    public string LineLabel { get; init; } = string.Empty;

    public IReadOnlyList<ApplicationItemLinkedDocumentGroup> Groups { get; init; } =
        Array.Empty<ApplicationItemLinkedDocumentGroup>();
}
