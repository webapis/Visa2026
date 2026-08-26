using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ApplicationItemLinkedDocuments;
using Visa2026.Module.Services.HeaderLinkedDocuments;
using Visa2026.Module.Services.WordReports;

namespace Visa2026.Module.Services.PreviewSlot;

public enum VisaPreviewSlotMode
{
    Closed = 0,
    File = 1,
    Resminamalar = 2,
    DocumentCopies = 3,
    ProgressLetters = 4,
    PersonDocumentCopies = 5,
    HeaderDocumentCopies = 6,
    PlaceholderManual = 7,
    IssueIssuedHeader = 8,
    IssueIssuedVisa = 9,
}

public sealed class ResminamalarSlotRequest
{
    public Guid ApplicationProfileInstanceId { get; init; }

    public WordReportPackageScope Scope { get; init; } = WordReportPackageScope.ApplicationProfileInstance;

    public IReadOnlyList<Guid> ApplicationItemIds { get; init; } = Array.Empty<Guid>();

    /// <summary>When set, catalog area shows this localized message instead of the report list.</summary>
    public string? EmptyCatalogMessage { get; init; }

    /// <summary>When set, the slot catalog auto-opens preview for this report entry key.</summary>
    public string? FocusEntryKey { get; init; }

    /// <summary>Display name for <see cref="FocusEntryKey"/> when <see cref="OpenPreviewOnly"/> is true.</summary>
    public string? FocusDisplayName { get; init; }

    /// <summary>
    /// When true, the slot shows only the report preview viewer (no catalog).
    /// Used when the catalog already lives in the officer case workspace tab.
    /// </summary>
    public bool OpenPreviewOnly { get; init; }
}

public sealed class PlaceholderManualSlotRequest
{
    public UserReportBoType? FilterRootBoType { get; init; }
}

public sealed class VisaPreviewSlotState
{
    public VisaPreviewSlotMode Mode { get; init; } = VisaPreviewSlotMode.Closed;

    /// <summary>Stable key for the current slot occupant (Resminamalar scope, file source, etc.).</summary>
    public string? OccupantKey { get; init; }

    /// <summary>XAF <see cref="View.Id"/> that opened the current occupant; used for owner-aware auto-close.</summary>
    public string? OwnerViewId { get; init; }

    public string? FileSourceType { get; init; }

    public Guid FileObjectId { get; init; }

    public ResminamalarSlotRequest? Resminamalar { get; init; }

    public DocumentCopiesSlotRequest? DocumentCopies { get; init; }

    public ProgressLettersSlotRequest? ProgressLetters { get; init; }

    public PersonDocumentCopiesSlotRequest? PersonDocumentCopies { get; init; }

    public HeaderDocumentCopiesSlotRequest? HeaderDocumentCopies { get; init; }

    public PlaceholderManualSlotRequest? PlaceholderManual { get; init; }

    public IssueIssuedHeaderSlotRequest? IssueIssuedHeader { get; init; }

    public IssueIssuedVisaSlotRequest? IssueIssuedVisa { get; init; }

    public int Version { get; init; }
}

public sealed class DocumentCopiesSlotRequest
{
    /// <summary>Person ids on this application (with <see cref="ApplicationProfileInstanceId"/>).</summary>
    public IReadOnlyList<Guid> ApplicationProfileInstancePersonIds { get; init; } = Array.Empty<Guid>();

    /// <summary>Parent application for the roster lines (application form PDF).</summary>
    public Guid ApplicationProfileInstanceId { get; init; }

    /// <summary>When set, the slot auto-opens preview for this document slot key.</summary>
    public string? FocusSlotKey { get; init; }

    /// <summary>Display name for <see cref="FocusSlotKey"/> when <see cref="OpenPreviewOnly"/> is true.</summary>
    public string? FocusDisplayName { get; init; }

    /// <summary>
    /// When true, the slot shows only the document preview viewer (no catalog).
    /// Used when the catalog already lives in the officer case workspace tab.
    /// </summary>
    public bool OpenPreviewOnly { get; init; }
}

public sealed class ProgressLettersSlotRequest
{
    public Guid ApplicationProfileInstanceId { get; init; }

    /// <summary>When set, the catalog opens and previews this progress row if it has a ministry letter file.</summary>
    public Guid? FocusProgressId { get; init; }

    /// <summary>Display name for <see cref="FocusProgressId"/> when <see cref="OpenPreviewOnly"/> is true.</summary>
    public string? FocusDisplayName { get; init; }

    /// <summary>
    /// When true, the slot shows only the letter preview viewer (no catalog).
    /// Used when the officer clicks a filename already shown in the case workspace Progress tab.
    /// </summary>
    public bool OpenPreviewOnly { get; init; }
}

public sealed class PersonDocumentCopiesSlotRequest
{
    public IReadOnlyList<Guid> PersonIds { get; init; } = Array.Empty<Guid>();
}

public sealed class HeaderDocumentCopiesSlotRequest
{
    public HeaderDocumentCopiesFamily Family { get; init; }

    public Guid ParentId { get; init; }

    public Guid? ContextItemId { get; init; }

    /// <summary>When set, the slot auto-opens preview for this header document record key.</summary>
    public string? FocusRecordKey { get; init; }

    /// <summary>Display name for <see cref="FocusRecordKey"/> when <see cref="OpenPreviewOnly"/> is true.</summary>
    public string? FocusDisplayName { get; init; }

    /// <summary>
    /// When true, the slot shows only the document preview viewer (no catalog).
    /// Used when the officer clicks Preview on an issued-header row in the case workspace.
    /// </summary>
    public bool OpenPreviewOnly { get; init; }
}

/// <summary>
/// Global right-side preview slot orchestrator (file preview + inline Resminamalar).
/// Implemented in the Blazor host; callable from XAF Module controllers via DI.
/// </summary>
public interface IVisaPreviewSlotService
{
    VisaPreviewSlotState State { get; }

    event Action? StateChanged;

    Task OpenResminamalarAsync(ResminamalarSlotRequest request, string? ownerViewId = null);

    Task OpenDocumentCopiesAsync(DocumentCopiesSlotRequest request, string? ownerViewId = null);

    Task OpenProgressLettersAsync(ProgressLettersSlotRequest request, string? ownerViewId = null);

    Task OpenPersonDocumentCopiesAsync(PersonDocumentCopiesSlotRequest request, string? ownerViewId = null);

    Task OpenHeaderDocumentCopiesAsync(HeaderDocumentCopiesSlotRequest request, string? ownerViewId = null);

    Task OpenPlaceholderManualAsync(PlaceholderManualSlotRequest? request = null, string? ownerViewId = null);

    Task OpenIssueIssuedHeaderAsync(IssueIssuedHeaderSlotRequest request, string? ownerViewId = null);

    Task OpenIssueIssuedVisaAsync(IssueIssuedVisaSlotRequest request, string? ownerViewId = null);

    Task OpenFileAsync(string sourceType, Guid objectId, string? ownerViewId = null);

    Task CloseAsync();
}

public sealed class ReportPackagePreviewRequest
{
    public required Guid ApplicationProfileInstanceId { get; init; }

    public required string EntryKey { get; init; }

    public required string DisplayName { get; init; }

    public IReadOnlyList<Guid>? ApplicationItemIds { get; init; }
}

public sealed class DocumentCopiesInlinePreviewRequest
{
    public required string SlotKey { get; init; }

    public required string DisplayName { get; init; }

    public ApplicationItemDocumentPackageOptions PackageOptions { get; init; } =
        ApplicationItemDocumentPackageOptions.CreateDefaults();

    /// <summary>
    /// When set, preview/merge uses only these roster person ids.
    /// Workspace person-grouped Preview passes the clicked person.
    /// </summary>
    public IReadOnlyList<Guid>? ApplicationProfileInstancePersonIds { get; init; }

    /// <summary>
    /// When set, preview merges every ready file of this family for the roster ids.
    /// <see cref="SlotKey"/> is <c>Family:{family}</c>.
    /// </summary>
    public string? FamilyKey { get; init; }
}

public sealed class ProgressLettersInlinePreviewRequest
{
    public Guid ApplicationProfileInstanceId { get; init; }

    public Guid ProgressId { get; init; }

    public required string DisplayName { get; init; }
}

public sealed class PersonDocumentCopiesInlinePreviewRequest
{
    public required string RecordKey { get; init; }

    public required string DisplayName { get; init; }
}

public sealed class HeaderDocumentCopiesInlinePreviewRequest
{
    public HeaderDocumentCopiesFamily Family { get; init; }

    public Guid ParentId { get; init; }

    public required string RecordKey { get; init; }

    public required string DisplayName { get; init; }
}
