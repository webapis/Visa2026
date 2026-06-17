using System;
using System.Collections.Generic;

namespace Visa2026.Module.Services.PersonLinkedDocuments;

public sealed class PersonLinkedDocumentSection
{
    public string SectionId { get; init; } = string.Empty;

    public string SectionLabel { get; init; } = string.Empty;

    public int SortOrder { get; init; }

    public IReadOnlyList<PersonLinkedDocumentRecord> Records { get; init; } =
        Array.Empty<PersonLinkedDocumentRecord>();
}
