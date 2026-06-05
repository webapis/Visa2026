using System;
using System.Collections.Generic;
using System.Linq;

namespace Visa2026.Module.Services.ApplicationItemLinkedDocuments;

public sealed class ApplicationItemLinkedDocumentsSnapshot
{
    public Guid ApplicationItemId { get; init; }

    public IReadOnlyList<ApplicationItemLinkedDocumentGroup> Groups { get; init; } =
        Array.Empty<ApplicationItemLinkedDocumentGroup>();

    public bool ContainsFile(Guid fileDataId) =>
        Groups.Any(g => g.Files.Any(f => f.FileDataId == fileDataId));

    public int TotalFileCount =>
        Groups.Sum(g => g.Files.Count(f => f.HasContent));
}
