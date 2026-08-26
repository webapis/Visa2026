using System;
using System.Collections.Generic;

namespace Visa2026.Module.Services.PreviewSlot;

public enum IssueIssuedHeaderKind
{
    Invitation = 0,
    WorkPermit = 1,
    Rejection = 2,
    BorderZone = 3,
}

public sealed class IssueIssuedHeaderSlotRequest
{
    public Guid ApplicationProfileInstanceId { get; init; }

    public IssueIssuedHeaderKind Kind { get; init; }

    /// <summary>Catalog key from workspace Issued records (invitation, workPermit, …).</summary>
    public string CatalogKey { get; init; } = string.Empty;

    /// <summary>When set, slot opens existing issued header in edit mode.</summary>
    public Guid? ExistingHeaderId { get; init; }
}

public sealed class IssueIssuedHeaderLookupOption
{
    public Guid Id { get; init; }

    public string Caption { get; init; } = string.Empty;
}

public sealed class IssueIssuedHeaderPersonLineDraft
{
    public Guid PersonId { get; init; }

    public string PersonName { get; init; } = string.Empty;

    public Guid? PassportId { get; set; }

    public string PassportNumber { get; set; } = string.Empty;

    public DateTime? PassportExpiration { get; init; }

    public bool Include { get; set; } = true;

    public bool IsReady { get; set; }

    public string StatusCaption { get; set; } = string.Empty;

    public bool IsEmployee { get; init; }

    /// <summary>Existing line BO id when editing (InvitationItem / WorkPermitItem / …).</summary>
    public Guid? ExistingLineId { get; set; }

    public bool CanIssueVisa { get; set; }

    /// <summary>Line already on the letter and locked (e.g. visa issued) — cannot uncheck/remove.</summary>
    public bool IncludeLocked { get; set; }

    public string ItemNumber { get; set; } = string.Empty;

    public string ASNumber { get; set; } = string.Empty;

    public Guid? PositionId { get; set; }

    public List<IssueIssuedHeaderLookupOption> Positions { get; set; } = new();

    public DateTime? ItemStartDate { get; set; }

    public DateTime? ItemExpirationDate { get; set; }

    public string WorkPermittedLocations { get; set; } = string.Empty;

    public string DatePrefillNote { get; set; } = string.Empty;
}

public sealed class IssueIssuedHeaderComposeDraft
{
    public IssueIssuedHeaderKind Kind { get; init; }

    public Guid ApplicationProfileInstanceId { get; init; }

    public string ApplicationCaption { get; init; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string HeaderNumber { get; set; } = string.Empty;

    public DateTime PrimaryDate { get; set; } = DateTime.Today;

    public DateTime? ExpirationDate { get; set; }

    public Guid? VisaCategoryId { get; set; }

    public Guid? VisaPeriodId { get; set; }

    public string BorderZoneLocation { get; set; } = string.Empty;

    public Guid? ValidityDurationId { get; set; }

    public string Reason { get; set; } = string.Empty;

    public bool IsVisaStartAndEndDateDefined { get; set; }

    public DateTime? VisaStartDate { get; set; }

    public DateTime? VisaEndDate { get; set; }

    /// <summary>When set, Save updates this header instead of creating.</summary>
    public Guid? ExistingHeaderId { get; set; }

    /// <summary>
    /// When false on edit Save, only header fields are updated — InvitationItems are left untouched.
    /// </summary>
    public bool SyncPeopleOnSave { get; set; }

    public IReadOnlyList<IssueIssuedHeaderLookupOption> VisaCategories { get; init; } =
        Array.Empty<IssueIssuedHeaderLookupOption>();

    public IReadOnlyList<IssueIssuedHeaderLookupOption> VisaPeriods { get; init; } =
        Array.Empty<IssueIssuedHeaderLookupOption>();

    public IReadOnlyList<string> BorderZoneNames { get; init; } =
        Array.Empty<string>();

    public IReadOnlyList<IssueIssuedHeaderLookupOption> ValidityDurations { get; init; } =
        Array.Empty<IssueIssuedHeaderLookupOption>();

    public List<IssueIssuedHeaderPersonLineDraft> People { get; init; } = new();

    public List<IssueIssuedHeaderDocumentRow> Documents { get; } = new();

    public bool IsEditMode => ExistingHeaderId is Guid id && id != Guid.Empty;
}

public sealed class IssueIssuedHeaderDocumentRow
{
    public Guid DocumentId { get; init; }

    public string FileName { get; init; } = string.Empty;

    public int SizeBytes { get; init; }
}

public sealed class IssueIssuedHeaderCreateResult
{
    public bool Succeeded { get; init; }

    public string? ErrorMessage { get; init; }

    public Guid HeaderId { get; init; }

    public string HeaderCaption { get; init; } = string.Empty;

    public IReadOnlyList<IssueIssuedHeaderCreatedLine> Lines { get; init; } =
        Array.Empty<IssueIssuedHeaderCreatedLine>();
}

public sealed class IssueIssuedHeaderCreatedLine
{
    public Guid LineId { get; init; }

    public Guid PersonId { get; init; }

    public string PersonName { get; init; } = string.Empty;

    public string PassportNumber { get; set; } = string.Empty;

    public bool CanIssueVisa { get; init; }

    public string ItemNumber { get; init; } = string.Empty;

    public string ASNumber { get; init; } = string.Empty;

    public string PositionCaption { get; init; } = string.Empty;

    public DateTime? StartDate { get; init; }

    public DateTime? ExpirationDate { get; init; }

    public string LocationsCaption { get; init; } = string.Empty;
}