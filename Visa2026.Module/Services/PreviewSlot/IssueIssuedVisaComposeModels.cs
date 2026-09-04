using System;
using System.Collections.Generic;

namespace Visa2026.Module.Services.PreviewSlot;

public sealed class IssueIssuedVisaSlotRequest
{
    public Guid ApplicationProfileInstanceId { get; init; }

    public Guid? ExistingVisaId { get; init; }
}

public sealed class IssueIssuedVisaLookupOption
{
    public Guid Id { get; init; }

    public string Caption { get; init; } = string.Empty;
}

public sealed class IssueIssuedVisaPersonCardDraft
{
    public Guid InvitationId { get; init; }

    public string InvitationNumber { get; init; } = string.Empty;

    public DateTime InvitationIssuedDate { get; init; }

    public Guid InvitationItemId { get; init; }

    public Guid PersonId { get; init; }

    public string PersonName { get; init; } = string.Empty;

    public Guid? PassportId { get; init; }

    public string PassportNumber { get; init; } = string.Empty;

    public bool Include { get; set; }

    public bool AlreadyIssued { get; init; }

    public bool IsReady { get; init; }

    public Guid? ExistingVisaId { get; init; }

    public string? ExistingVisaNumber { get; init; }

    public string VisaNumber { get; set; } = string.Empty;

    public Guid? VisaTypeId { get; set; }

    public Guid? VisaCategoryId { get; set; }

    public Guid? VisaPeriodId { get; set; }

    public Guid? VisaIssuedPlaceId { get; set; }

    public DateTime IssueDate { get; set; } = DateTime.Today;

    public DateTime? ExpirationDate { get; set; }

    public string BorderZoneLocation { get; set; } = string.Empty;

    public List<IssueIssuedHeaderDocumentRow> Documents { get; set; } = new();

    public string? PendingCopyFileName { get; set; }

    public byte[]? PendingCopyBytes { get; set; }
}

public sealed class IssueIssuedVisaInvitationGroupDraft
{
    public Guid InvitationId { get; init; }

    public string InvitationNumber { get; init; } = string.Empty;

    public DateTime InvitationIssuedDate { get; init; }

    public List<IssueIssuedVisaPersonCardDraft> People { get; init; } = new();
}

public sealed class IssueIssuedVisaComposeDraft
{
    public Guid ApplicationProfileInstanceId { get; init; }

    public Guid? ExistingVisaId { get; init; }

    public bool IsEditMode => ExistingVisaId is Guid id && id != Guid.Empty;

    public string Title { get; init; } = string.Empty;

    public string ApplicationCaption { get; init; } = string.Empty;

    public string ProfileCode { get; init; } = string.Empty;

    /// <summary>
    /// True when people come from issued invitation lines (ProduceInvitation).
    /// False when people come from the case roster (visa without invitation).
    /// </summary>
    public bool UsesInvitationSource { get; init; }

    public IReadOnlyList<string> PeopleWithoutIssuedInvitation { get; init; } =
        Array.Empty<string>();

    public IReadOnlyList<IssueIssuedVisaLookupOption> VisaTypes { get; init; } =
        Array.Empty<IssueIssuedVisaLookupOption>();

    public IReadOnlyList<IssueIssuedVisaLookupOption> VisaCategories { get; init; } =
        Array.Empty<IssueIssuedVisaLookupOption>();

    public IReadOnlyList<IssueIssuedVisaLookupOption> VisaPeriods { get; init; } =
        Array.Empty<IssueIssuedVisaLookupOption>();

    public IReadOnlyList<IssueIssuedVisaLookupOption> VisaIssuedPlaces { get; init; } =
        Array.Empty<IssueIssuedVisaLookupOption>();

    public IReadOnlyList<string> BorderZoneNames { get; init; } =
        Array.Empty<string>();

    public List<IssueIssuedVisaInvitationGroupDraft> Groups { get; init; } = new();

    public IEnumerable<IssueIssuedVisaPersonCardDraft> AllPeople =>
        Groups.SelectMany(g => g.People);

    public bool HasUnusedLine => AllPeople.Any(p => !p.AlreadyIssued);

    public int SelectedUnusedCount => AllPeople.Count(p => p.Include && !p.AlreadyIssued);
}

public sealed class IssueIssuedVisaCreateResult
{
    public bool Succeeded { get; init; }

    public string? ErrorMessage { get; init; }

    public IReadOnlyList<IssueIssuedVisaCreatedRow> Rows { get; init; } =
        Array.Empty<IssueIssuedVisaCreatedRow>();
}

public sealed class IssueIssuedVisaCreatedRow
{
    public Guid VisaId { get; init; }

    public string VisaNumber { get; init; } = string.Empty;

    public string PersonName { get; init; } = string.Empty;

    public string InvitationNumber { get; init; } = string.Empty;

    public string VisaTypeCaption { get; init; } = string.Empty;

    public string VisaCategoryCaption { get; init; } = string.Empty;

    public string VisaPeriodCaption { get; init; } = string.Empty;

    public string PassportNumber { get; init; } = string.Empty;

    public DateTime IssueDate { get; init; }

    public DateTime? ExpirationDate { get; init; }

    public string BorderZoneCaption { get; init; } = string.Empty;
}