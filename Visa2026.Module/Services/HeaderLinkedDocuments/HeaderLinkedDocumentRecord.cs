using System;
using System.Collections.Generic;

namespace Visa2026.Module.Services.HeaderLinkedDocuments;

public sealed class HeaderLinkedDocumentRecord
{
    public string RecordKey { get; init; } = string.Empty;

    public string RecordLabel { get; init; } = string.Empty;

    public IReadOnlyList<HeaderLinkedDocumentFile> Files { get; init; } =
        Array.Empty<HeaderLinkedDocumentFile>();
}
