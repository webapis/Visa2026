using System;
using System.Collections.Generic;
using System.Linq;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services.PersonDossier;

/// <summary>
/// Read-only 360 view of one <see cref="Person"/>: identity, derived "right now" status, and
/// every visa / permit / travel record grouped into sections.
/// </summary>
/// <remarks>
/// Record keys intentionally use the same format as
/// <see cref="Visa2026.Module.Services.PersonLinkedDocuments.PersonLinkedDocumentRecord.RecordKey"/>
/// so a dossier row can be matched to its document-copies row.
/// </remarks>
public sealed class PersonDossierSnapshot
{
    public Guid PersonId { get; init; }

    public string PersonDisplayName { get; init; } = string.Empty;

    public string? PersonalNumber { get; init; }

    public PersonRecordRole PersonRole { get; init; }

    public string PersonRoleLabel { get; init; } = string.Empty;

    public string ProjectContractName { get; init; } = string.Empty;

    /// <summary>Base64 data URI built from <see cref="Person.Photo"/>, or null when no photo.</summary>
    public string? PhotoDataUri { get; init; }

    public bool IsArchived { get; init; }

    public IReadOnlyList<PersonDossierField> IdentityFields { get; init; } =
        Array.Empty<PersonDossierField>();

    public IReadOnlyList<PersonDossierStatusTile> StatusTiles { get; init; } =
        Array.Empty<PersonDossierStatusTile>();

    public IReadOnlyList<PersonDossierSection> Sections { get; init; } =
        Array.Empty<PersonDossierSection>();

    public PersonDossierSection? FindSection(string sectionId) =>
        Sections.FirstOrDefault(section =>
            string.Equals(section.SectionId, sectionId, StringComparison.Ordinal));

    public int TotalRecordCount => Sections.Sum(section => section.Records.Count);
}

/// <summary>One label / value pair in the identity header.</summary>
public sealed class PersonDossierField
{
    public string Label { get; init; } = string.Empty;

    public string Value { get; init; } = string.Empty;
}

/// <summary>
/// A derived "right now" tile (passport / visa / work permit / registration). This is the part the
/// typed Person DetailView tabs do not compute.
/// </summary>
public sealed class PersonDossierStatusTile
{
    public string TileId { get; init; } = string.Empty;

    public string Label { get; init; } = string.Empty;

    /// <summary>Document number or short identifier; empty when nothing is on file.</summary>
    public string Value { get; init; } = string.Empty;

    public string StatusLabel { get; init; } = string.Empty;

    /// <summary>Report Dashboard status vocabulary: st-approved / st-pending / st-expiring.</summary>
    public string StatusCssClass { get; init; } = string.Empty;
}

/// <summary>A group of like records (Passports, Visas, Education, ...).</summary>
public sealed class PersonDossierSection
{
    public string SectionId { get; init; } = string.Empty;

    public string SectionLabel { get; init; } = string.Empty;

    public int SortOrder { get; init; }

    /// <summary>Column captions for <see cref="PersonDossierRecord.Cells"/> (same length).</summary>
    public IReadOnlyList<string> ColumnHeaders { get; init; } = Array.Empty<string>();

    public IReadOnlyList<PersonDossierRecord> Records { get; init; } =
        Array.Empty<PersonDossierRecord>();
}

/// <summary>One child business object rendered as a summary row.</summary>
public sealed class PersonDossierRecord
{
    /// <summary>Matches the document-copies record key for the same object (Phase 2 linkage).</summary>
    public string RecordKey { get; init; } = string.Empty;

    public IReadOnlyList<string> Cells { get; init; } = Array.Empty<string>();

    public string StatusLabel { get; init; } = string.Empty;

    public string StatusCssClass { get; init; } = string.Empty;

    public bool IsCurrent { get; init; }

    public Guid? SourceObjectId { get; init; }

    public Type? SourceObjectType { get; init; }
}
