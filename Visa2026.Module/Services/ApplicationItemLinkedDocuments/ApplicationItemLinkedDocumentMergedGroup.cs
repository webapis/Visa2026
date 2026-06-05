using System;
using System.Collections.Generic;

namespace Visa2026.Module.Services.ApplicationItemLinkedDocuments;

public sealed class ApplicationItemLinkedDocumentMergedGroup
{
    public string SlotKey { get; init; } = string.Empty;

    public string SlotLabel { get; init; } = string.Empty;

    /// <summary>No files in the merged group and every in-scope line has a missing FK for this slot.</summary>
    public bool LinkMissing { get; init; }

    public int InScopeLineCount { get; init; }

    public int LinesWithFilesCount { get; init; }

    public IReadOnlyList<ApplicationItemLinkedDocumentFileEntry> Files { get; init; } =
        Array.Empty<ApplicationItemLinkedDocumentFileEntry>();

    public IReadOnlyList<ApplicationItemLinkedDocumentMissingLineEntry> MissingLines { get; init; } =
        Array.Empty<ApplicationItemLinkedDocumentMissingLineEntry>();
}
