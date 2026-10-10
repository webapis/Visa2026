using System;
using System.Collections.Generic;
using System.Linq;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.HeaderLinkedDocuments;
using Visa2026.Module.Services.PreviewSlot;

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

    /// <summary>Screen mode shows a Copy column (Preview link or "No copy") for this section.</summary>
    public bool HasCopyColumn { get; init; }

    /// <summary>
    /// Applications: the instance-list progress track. The generic Status column stays only when a
    /// row still has its own pill (Seretmezlik).
    /// </summary>
    public bool HasProgressColumn { get; init; }

    /// <summary>Screen filter chips for Applications. Paper still prints every row.</summary>
    public IReadOnlyList<PersonDossierApplicationGroup> ApplicationGroups { get; init; } =
        Array.Empty<PersonDossierApplicationGroup>();

    /// <summary>Applications: invitation or rejection column after the progress track.</summary>
    public bool HasIssuedOutcomeColumn { get; init; }

    public bool ShowsStatusColumn =>
        !HasProgressColumn
        || Records.Any(record =>
            record.IsCurrent || !string.IsNullOrWhiteSpace(record.StatusLabel));
}

/// <summary>One child business object rendered as a summary row.</summary>
public sealed class PersonDossierRecord
{
    /// <summary>Matches the document-copies record key for the same object (Phase 2 linkage).</summary>
    public string RecordKey { get; init; } = string.Empty;

    public IReadOnlyList<string> Cells { get; init; } = Array.Empty<string>();

    /// <summary>Profile name under the application number, in the same column as the date.</summary>
    public string FirstCellDetail { get; init; } = string.Empty;

    /// <summary>Second line under the first cell. Applications use it for the application date.</summary>
    public string FirstCellNote { get; init; } = string.Empty;

    /// <summary>This person's invitation or rejection, once Migration service is finished.</summary>
    public PersonDossierIssuedOutcome? IssuedOutcome { get; init; }

    public string StatusLabel { get; init; } = string.Empty;

    public string StatusCssClass { get; init; } = string.Empty;

    /// <summary>Screen group id for the Applications filter. Empty rows appear only under All.</summary>
    public string ApplicationGroupId { get; init; } = string.Empty;

    /// <summary>Instance-list progress track. Empty on sections that are not applications.</summary>
    public IReadOnlyList<PersonDossierProgressStep> ProgressSteps { get; init; } =
        Array.Empty<PersonDossierProgressStep>();

    public bool IsCurrent { get; init; }

    public Guid? SourceObjectId { get; init; }

    public Type? SourceObjectType { get; init; }

    /// <summary>Issued header (invitation, work permit) whose scan opens in the header copies slot.</summary>
    public HeaderDocumentCopiesFamily? PreviewFamily { get; init; }

    /// <summary>Header id whose copies open in the preview slot.</summary>
    public Guid? PreviewParentId { get; init; }

    /// <summary>Person whose child scan (passport, visa, education) opens in the person copies slot.</summary>
    public Guid? PersonCopyPersonId { get; init; }

    /// <summary>Person-copies record key (<c>Passport:{id:N}</c>, …) for preview-only.</summary>
    public string? PersonCopyRecordKey { get; init; }

    public string? PersonCopyDisplayName { get; init; }

    public bool HasHeaderPreview =>
        PreviewFamily != null && PreviewParentId is { } headerId && headerId != Guid.Empty;

    public bool HasPersonCopyPreview =>
        PersonCopyPersonId is { } personId && personId != Guid.Empty
        && !string.IsNullOrWhiteSpace(PersonCopyRecordKey);

    public bool HasPreview => HasHeaderPreview || HasPersonCopyPreview;

    /// <summary>Issued header that has no copy yet.</summary>
    public Guid? UploadHeaderId { get; init; }

    public IssueIssuedHeaderKind? UploadIssuedKind { get; init; }

    /// <summary>Owning case; when set, Upload opens the issued-header slot.</summary>
    public Guid? UploadApplicationProfileInstanceId { get; init; }

    /// <summary>Passport, visa, or education opened for upload when there is no scan.</summary>
    public Type? UploadDetailType { get; init; }

    public Guid? UploadDetailId { get; init; }

    public bool CanUploadHeader =>
        !HasPreview && UploadHeaderId is { } headerId && headerId != Guid.Empty;

    public bool CanUploadDetail =>
        !HasPreview
        && UploadDetailType != null
        && UploadDetailId is { } detailId
        && detailId != Guid.Empty;

    public bool CanUpload => CanUploadHeader || CanUploadDetail;
}

/// <summary>
/// One step of the application progress track, same tones and glyphs as the instance ListView stepper.
/// </summary>
public sealed class PersonDossierProgressStep
{
    public string Label { get; init; } = string.Empty;

    public string Date { get; init; } = string.Empty;

    /// <summary>Submitted, Approved, In progress, Pending — the workspace step badge.</summary>
    public string StatusLabel { get; init; } = string.Empty;

    /// <summary>Ministry decision number. Empty when this step has none.</summary>
    public string ResultNumber { get; init; } = string.Empty;

    /// <summary>Office preparation uses View file. Ministry steps use View letter.</summary>
    public bool IsOfficeFile { get; init; }

    /// <summary>Ministry letter or office file name. Empty when there is no copy.</summary>
    public string LetterFileName { get; init; } = string.Empty;

    /// <summary>Progress row that holds the letter, for the preview slot.</summary>
    public Guid? LetterProgressId { get; init; }

    /// <summary>Approved or unapproved ministry step with no letter file.</summary>
    public bool MissingLetter { get; init; }

    /// <summary>True for the step the case is on now. Letter actions stay off that step.</summary>
    public bool IsCurrentStep { get; init; }

    /// <summary>done / issued / current / pending / rej / cancel.</summary>
    public string Tone { get; init; } = "pending";

    public string Glyph { get; init; } = string.Empty;

    /// <summary>The line after this step is complete (same rule as the instance list).</summary>
    public bool ConnectorDone { get; init; }

    public bool ShowLetter =>
        !IsCurrentStep
        && LetterProgressId is Guid id
        && id != Guid.Empty
        && !string.IsNullOrWhiteSpace(LetterFileName);

    public bool ShowMissingLetter =>
        !IsCurrentStep
        && MissingLetter
        && string.IsNullOrWhiteSpace(LetterFileName);
}

/// <summary>Invitation or rejection shown after the progress track for this person only.</summary>
public sealed class PersonDossierIssuedOutcome
{
    public string Number { get; init; } = string.Empty;

    public string StatusLabel { get; init; } = string.Empty;

    public string StatusCssClass { get; init; } = string.Empty;

    public HeaderDocumentCopiesFamily? PreviewFamily { get; init; }

    public Guid? PreviewParentId { get; init; }

    public bool HasPreview =>
        PreviewFamily != null && PreviewParentId is { } id && id != Guid.Empty;
}

/// <summary>One Applications filter button (Çakylyklar, Wizalar, …).</summary>
public sealed class PersonDossierApplicationGroup
{
    public string GroupId { get; init; } = string.Empty;

    public string Label { get; init; } = string.Empty;

    public int Count { get; init; }

    /// <summary>Shown even when the count is zero (the default invitation group).</summary>
    public bool AlwaysVisible { get; init; }
}
