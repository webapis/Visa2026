using System;
using System.Collections.Generic;

namespace Visa2026.Module.Services.ApplicationResultsDesk;

public static class ApplicationResultsDeskStates
{
    public const string Missing = "missing";
    public const string Issued = "issued";
    public const string Valid = "valid";
    public const string Cancelled = "cancelled";
    public const string Changed = "changed";
    public const string Seretmezlik = "seretmezlik";
}

public sealed class ApplicationResultsDeskRequest
{
    public string SearchText { get; set; } = string.Empty;

    public string TypeKey { get; set; } = string.Empty;

    public string StateKey { get; set; } = ApplicationResultsDeskStates.Missing;

    public Guid PeopleInstanceId { get; set; }

    public string PeopleKind { get; set; } = string.Empty;

    public string HeaderKind { get; set; } = string.Empty;

    public bool DetailOpen { get; set; }

    public Guid DetailPersonId { get; set; }
}

public sealed class ApplicationResultsDeskPage
{
    public int MissingCaseCount { get; set; }

    public bool Truncated { get; set; }

    public bool SearchMiss { get; set; }

    public string SearchedText { get; set; } = string.Empty;

    public string TypeKey { get; set; } = string.Empty;

    public string StateKey { get; set; } = ApplicationResultsDeskStates.Missing;

    public ApplicationResultsDeskCaseCard Hit { get; set; }

    public IReadOnlyList<ApplicationResultsDeskQueueRow> Rows { get; set; } =
        Array.Empty<ApplicationResultsDeskQueueRow>();

    public ApplicationResultsDeskPeoplePage People { get; set; }

    public ApplicationResultsDeskDetail Detail { get; set; }

    public ApplicationResultsDeskHeaderForm Header { get; set; }

    public string Notice { get; set; }
}

public sealed class ApplicationResultsDeskCaseCard
{
    public Guid InstanceId { get; set; }

    public string ProcessNumber { get; set; } = string.Empty;

    public string ApplicationNumber { get; set; } = string.Empty;

    public string ProfileName { get; set; } = string.Empty;

    public int PeopleCount { get; set; }

    public bool HasProcessNumber { get; set; }

    public IReadOnlyList<ApplicationResultsDeskTypeCard> Types { get; set; } =
        Array.Empty<ApplicationResultsDeskTypeCard>();
}

public sealed class ApplicationResultsDeskTypeCard
{
    public string Key { get; set; } = string.Empty;

    public string Label { get; set; } = string.Empty;

    public int Coverage { get; set; }

    public int Expected { get; set; }

    public bool Optional { get; set; }

    public bool Complete { get; set; }

    public bool OpensOnDesk { get; set; }

    public string Ratio { get; set; } = string.Empty;
}

public sealed class ApplicationResultsDeskQueueRow
{
    public Guid InstanceId { get; set; }

    public string ProcessNumber { get; set; } = string.Empty;

    public string ApplicationNumber { get; set; } = string.Empty;

    public string ProfileName { get; set; } = string.Empty;

    public string ResultKey { get; set; } = string.Empty;

    public string ResultLabel { get; set; } = string.Empty;

    public string CoverageText { get; set; } = string.Empty;

    public string GapText { get; set; } = string.Empty;

    public string StateLabel { get; set; } = string.Empty;

    public string StateKey { get; set; } = string.Empty;

    public bool OpensOnDesk { get; set; }
}

public sealed class ApplicationResultsDeskPeoplePage
{
    public Guid InstanceId { get; set; }

    public string Kind { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string ProcessNumber { get; set; } = string.Empty;

    public string ApplicationNumber { get; set; } = string.Empty;

    public string ProfileName { get; set; } = string.Empty;

    public bool LetterReady { get; set; }

    public Guid LetterId { get; set; }

    public string LetterNumber { get; set; } = string.Empty;

    public string LetterIssued { get; set; } = string.Empty;

    public string Note { get; set; } = string.Empty;

    public IReadOnlyList<ApplicationResultsDeskPersonRow> Rows { get; set; } =
        Array.Empty<ApplicationResultsDeskPersonRow>();
}

public sealed class ApplicationResultsDeskPersonRow
{
    public Guid PersonId { get; set; }

    public Guid RecordId { get; set; }

    public string PersonName { get; set; } = string.Empty;

    public string Passport { get; set; } = string.Empty;

    public string Number { get; set; } = string.Empty;

    public string Detail { get; set; } = string.Empty;

    public string Start { get; set; } = string.Empty;

    public string End { get; set; } = string.Empty;

    public string Locations { get; set; } = string.Empty;

    public bool CanOpen { get; set; }

    public string BlockReason { get; set; } = string.Empty;
}

public sealed class ApplicationResultsDeskLetterSave
{
    public Guid InstanceId { get; set; }

    public string Number { get; set; } = string.Empty;

    public string IssuedDate { get; set; } = string.Empty;
}

public sealed class ApplicationResultsDeskHeaderOpen
{
    public Guid InstanceId { get; set; }

    public string Kind { get; set; } = string.Empty;
}

public sealed class ApplicationResultsDeskPersonOpen
{
    public Guid InstanceId { get; set; }

    public Guid PersonId { get; set; }

    public string Kind { get; set; } = string.Empty;
}

public sealed class ApplicationResultsDeskOption
{
    public Guid Id { get; set; }

    public string Caption { get; set; } = string.Empty;
}

public sealed class ApplicationResultsDeskDetail
{
    public string Kind { get; set; } = string.Empty;

    public bool IsVisa { get; set; }

    public Guid InstanceId { get; set; }

    public Guid PersonId { get; set; }

    public Guid RecordId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string PersonName { get; set; } = string.Empty;

    public string Passport { get; set; } = string.Empty;

    public string ProcessNumber { get; set; } = string.Empty;

    public string ApplicationNumber { get; set; } = string.Empty;

    public string ProfileName { get; set; } = string.Empty;

    public string ItemNumber { get; set; } = string.Empty;

    public string LetterNumber { get; set; } = string.Empty;

    public string Position { get; set; } = string.Empty;

    public string AsNumber { get; set; } = string.Empty;

    public string Start { get; set; } = string.Empty;

    public string End { get; set; } = string.Empty;

    public string Locations { get; set; } = string.Empty;

    public string VisaNumber { get; set; } = string.Empty;

    public Guid VisaTypeId { get; set; }

    public Guid VisaCategoryId { get; set; }

    public Guid VisaIssuedPlaceId { get; set; }

    public string Issued { get; set; } = string.Empty;

    public string BorderZone { get; set; } = string.Empty;

    public string InvitationNumber { get; set; } = string.Empty;

    public IReadOnlyList<ApplicationResultsDeskOption> VisaTypes { get; set; } =
        Array.Empty<ApplicationResultsDeskOption>();

    public IReadOnlyList<ApplicationResultsDeskOption> VisaCategories { get; set; } =
        Array.Empty<ApplicationResultsDeskOption>();

    public IReadOnlyList<ApplicationResultsDeskOption> IssuedPlaces { get; set; } =
        Array.Empty<ApplicationResultsDeskOption>();
}

public sealed class ApplicationResultsDeskDetailSave
{
    public string Kind { get; set; } = string.Empty;

    public Guid InstanceId { get; set; }

    public Guid PersonId { get; set; }

    public string ItemNumber { get; set; } = string.Empty;

    public string Start { get; set; } = string.Empty;

    public string End { get; set; } = string.Empty;

    public string Locations { get; set; } = string.Empty;

    public string VisaNumber { get; set; } = string.Empty;

    public Guid VisaTypeId { get; set; }

    public Guid VisaCategoryId { get; set; }

    public Guid VisaIssuedPlaceId { get; set; }

    public string Issued { get; set; } = string.Empty;

    public string BorderZone { get; set; } = string.Empty;
}

public sealed class ApplicationResultsDeskHeaderPerson
{
    public Guid PersonId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Passport { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public bool Include { get; set; }

    public bool Ready { get; set; }

    public bool Locked { get; set; }
}

public sealed class ApplicationResultsDeskHeaderForm
{
    public Guid InstanceId { get; set; }

    public string Kind { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string ProcessNumber { get; set; } = string.Empty;

    public string ApplicationNumber { get; set; } = string.Empty;

    public string ProfileName { get; set; } = string.Empty;

    public bool IsEdit { get; set; }

    public string Number { get; set; } = string.Empty;

    public string PrimaryDate { get; set; } = string.Empty;

    public string Expiration { get; set; } = string.Empty;

    public string Reason { get; set; } = string.Empty;

    public string BorderZone { get; set; } = string.Empty;

    public Guid VisaCategoryId { get; set; }

    public Guid VisaPeriodId { get; set; }

    public bool ShowReason { get; set; }

    public bool ShowBorder { get; set; }

    public bool ShowVisaLookups { get; set; }

    public bool ShowExpiration { get; set; }

    public IReadOnlyList<ApplicationResultsDeskOption> VisaCategories { get; set; } =
        Array.Empty<ApplicationResultsDeskOption>();

    public IReadOnlyList<ApplicationResultsDeskOption> VisaPeriods { get; set; } =
        Array.Empty<ApplicationResultsDeskOption>();

    public IReadOnlyList<ApplicationResultsDeskHeaderPerson> People { get; set; } =
        Array.Empty<ApplicationResultsDeskHeaderPerson>();
}

public sealed class ApplicationResultsDeskHeaderSave
{
    public Guid InstanceId { get; set; }

    public string Kind { get; set; } = string.Empty;

    public string Number { get; set; } = string.Empty;

    public string PrimaryDate { get; set; } = string.Empty;

    public string Expiration { get; set; } = string.Empty;

    public string Reason { get; set; } = string.Empty;

    public string BorderZone { get; set; } = string.Empty;

    public Guid VisaCategoryId { get; set; }

    public Guid VisaPeriodId { get; set; }

    public IReadOnlyList<Guid> IncludedPersonIds { get; set; } = Array.Empty<Guid>();
}
