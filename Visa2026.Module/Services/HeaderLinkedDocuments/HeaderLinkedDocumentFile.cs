using System;
using System.Collections.Generic;

namespace Visa2026.Module.Services.HeaderLinkedDocuments;

public sealed class HeaderLinkedDocumentFile
{
    public Guid FileDataId { get; init; }

    public Guid DocumentRowId { get; init; }

    public string DocumentTypeName { get; init; } = string.Empty;

    public string FileName { get; init; } = string.Empty;

    public int SizeBytes { get; init; }

    public bool HasContent { get; init; }
}
