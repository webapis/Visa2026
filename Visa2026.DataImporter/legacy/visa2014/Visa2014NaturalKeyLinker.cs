using DevExpress.ExpressApp;
using Bo = Visa2026.Module.BusinessObjects;

namespace Visa2026.DataImporter.Legacy.Visa2014;

internal enum Visa2014NaturalKeyKind
{
    Passport,
    Visa,
    Education,
    EmployeePositionHistory,
    EmployeeSalary,
    WorkPermit,
    WorkPermitItem,
    Invitation,
    InvitationItem,
}

/// <summary>
/// Natural key used to link a legacy id onto a row that is already on the target.
/// A hit is recorded in the id-map and the existing row is left unchanged.
/// </summary>
internal sealed class Visa2014NaturalKeyLookup
{
    private Visa2014NaturalKeyLookup(
        Visa2014NaturalKeyKind kind,
        Type entityType,
        string cacheKey,
        Guid personId,
        Guid relatedId,
        string? number,
        DateTime? onDate)
    {
        Kind = kind;
        EntityType = entityType;
        CacheKey = cacheKey;
        PersonId = personId;
        RelatedId = relatedId;
        Number = number;
        OnDate = onDate;
    }

    public Visa2014NaturalKeyKind Kind { get; }
    public Type EntityType { get; }
    public string CacheKey { get; }
    public Guid PersonId { get; }
    public Guid RelatedId { get; }
    public string? Number { get; }
    public DateTime? OnDate { get; }

    public static Visa2014NaturalKeyLookup? ForPassport(Guid personId, object? passportNumber)
    {
        var number = ReadText(passportNumber);
        if (personId == Guid.Empty || number == null)
            return null;

        return new Visa2014NaturalKeyLookup(
            Visa2014NaturalKeyKind.Passport,
            typeof(Bo.Passport),
            $"passport|{personId:D}|{number}",
            personId,
            Guid.Empty,
            number,
            null);
    }

    public static Visa2014NaturalKeyLookup? ForVisa(Guid passportId, object? visaNumber)
    {
        var number = ReadText(visaNumber);
        if (passportId == Guid.Empty || number == null)
            return null;

        return new Visa2014NaturalKeyLookup(
            Visa2014NaturalKeyKind.Visa,
            typeof(Bo.Visa),
            $"visa|{passportId:D}|{number}",
            Guid.Empty,
            passportId,
            number,
            null);
    }

    public static Visa2014NaturalKeyLookup? ForEducation(Guid personId, object? graduationYear)
    {
        if (personId == Guid.Empty)
            return null;

        var year = ReadText(graduationYear) ?? string.Empty;
        return new Visa2014NaturalKeyLookup(
            Visa2014NaturalKeyKind.Education,
            typeof(Bo.Education),
            $"education|{personId:D}|{year}",
            personId,
            Guid.Empty,
            year,
            null);
    }

    public static Visa2014NaturalKeyLookup? ForEmployeePositionHistory(Guid personId, object? startDate)
    {
        if (personId == Guid.Empty || !TryReadDate(startDate, out var day))
            return null;

        return new Visa2014NaturalKeyLookup(
            Visa2014NaturalKeyKind.EmployeePositionHistory,
            typeof(Bo.EmployeePositionHistory),
            $"position|{personId:D}|{day:yyyy-MM-dd}",
            personId,
            Guid.Empty,
            null,
            day);
    }

    public static Visa2014NaturalKeyLookup? ForEmployeeSalary(Guid personId)
    {
        if (personId == Guid.Empty)
            return null;

        return new Visa2014NaturalKeyLookup(
            Visa2014NaturalKeyKind.EmployeeSalary,
            typeof(Bo.EmployeeSalary),
            $"salary|{personId:D}",
            personId,
            Guid.Empty,
            null,
            null);
    }

    public static Visa2014NaturalKeyLookup? ForWorkPermit(object? workPermitNumber, object? issuedDate)
    {
        var number = ReadText(workPermitNumber);
        if (number == null || !TryReadDate(issuedDate, out var day))
            return null;

        return new Visa2014NaturalKeyLookup(
            Visa2014NaturalKeyKind.WorkPermit,
            typeof(Bo.WorkPermit),
            $"workpermit|{number}|{day:yyyy-MM-dd}",
            Guid.Empty,
            Guid.Empty,
            number,
            day);
    }

    public static Visa2014NaturalKeyLookup? ForWorkPermitItem(Guid personId, object? workPermitNumber, object? startDate)
    {
        var number = ReadText(workPermitNumber);
        if (personId == Guid.Empty || number == null || !TryReadDate(startDate, out var day))
            return null;

        return new Visa2014NaturalKeyLookup(
            Visa2014NaturalKeyKind.WorkPermitItem,
            typeof(Bo.WorkPermitItem),
            $"workpermit-item|{personId:D}|{number}|{day:yyyy-MM-dd}",
            personId,
            Guid.Empty,
            number,
            day);
    }

    public static Visa2014NaturalKeyLookup? ForInvitation(object? invitationNumber, object? issuedDate)
    {
        var number = ReadText(invitationNumber);
        if (number == null || !TryReadDate(issuedDate, out var day))
            return null;

        return new Visa2014NaturalKeyLookup(
            Visa2014NaturalKeyKind.Invitation,
            typeof(Bo.Invitation),
            $"invitation|{number}|{day:yyyy-MM-dd}",
            Guid.Empty,
            Guid.Empty,
            number,
            day);
    }

    public static Visa2014NaturalKeyLookup? ForInvitationItem(Guid personId, Guid invitationId)
    {
        if (personId == Guid.Empty || invitationId == Guid.Empty)
            return null;

        return new Visa2014NaturalKeyLookup(
            Visa2014NaturalKeyKind.InvitationItem,
            typeof(Bo.InvitationItem),
            $"invitation-item|{personId:D}|{invitationId:D}",
            personId,
            invitationId,
            null,
            null);
    }

    internal static string? ReadText(object? value)
    {
        var text = value as string;
        if (string.IsNullOrWhiteSpace(text))
            return null;
        return text.Trim();
    }

    internal static bool TryReadDate(object? value, out DateTime date)
    {
        switch (value)
        {
            case DateTime dt:
                date = dt.Date;
                return true;
            case string text when DateTime.TryParse(text, out var parsed):
                date = parsed.Date;
                return true;
            default:
                date = default;
                return false;
        }
    }
}

internal static class Visa2014NaturalKeyLinker
{
    public static Guid? TryFind(IObjectSpace objectSpace, Visa2014NaturalKeyLookup lookup)
    {
        return lookup.Kind switch
        {
            Visa2014NaturalKeyKind.Passport => FirstId(
                objectSpace.GetObjectsQuery<Bo.Passport>()
                    .Where(p => p.Person != null && p.Person.ID == lookup.PersonId && p.PassportNumber == lookup.Number)
                    .OrderBy(p => p.ID)
                    .Select(p => (Guid?)p.ID)),
            Visa2014NaturalKeyKind.Visa => FirstId(
                objectSpace.GetObjectsQuery<Bo.Visa>()
                    .Where(v => v.Passport != null && v.Passport.ID == lookup.RelatedId && v.VisaNumber == lookup.Number)
                    .OrderBy(v => v.ID)
                    .Select(v => (Guid?)v.ID)),
            Visa2014NaturalKeyKind.Education => FirstId(FindEducation(objectSpace, lookup)),
            Visa2014NaturalKeyKind.EmployeePositionHistory => FindPositionHistory(objectSpace, lookup),
            Visa2014NaturalKeyKind.EmployeeSalary => FirstId(
                objectSpace.GetObjectsQuery<Bo.EmployeeSalary>()
                    .Where(s => s.Person != null && s.Person.ID == lookup.PersonId)
                    .OrderByDescending(s => s.StartDate)
                    .ThenByDescending(s => s.ID)
                    .Select(s => (Guid?)s.ID)),
            Visa2014NaturalKeyKind.WorkPermit => FindWorkPermit(objectSpace, lookup),
            Visa2014NaturalKeyKind.WorkPermitItem => FindWorkPermitItem(objectSpace, lookup),
            Visa2014NaturalKeyKind.Invitation => FindInvitation(objectSpace, lookup),
            Visa2014NaturalKeyKind.InvitationItem => FirstId(
                objectSpace.GetObjectsQuery<Bo.InvitationItem>()
                    .Where(i => i.Person != null && i.Person.ID == lookup.PersonId
                        && i.Invitation != null && i.Invitation.ID == lookup.RelatedId)
                    .OrderBy(i => i.ID)
                    .Select(i => (Guid?)i.ID)),
            _ => null,
        };
    }

    private static Guid? FindPositionHistory(IObjectSpace objectSpace, Visa2014NaturalKeyLookup lookup)
    {
        if (lookup.OnDate is not DateTime day)
            return null;

        var start = day.Date;
        var next = start.AddDays(1);
        return FirstId(
            objectSpace.GetObjectsQuery<Bo.EmployeePositionHistory>()
                .Where(h => h.Person != null && h.Person.ID == lookup.PersonId
                    && h.StartDate >= start && h.StartDate < next)
                .OrderBy(h => h.ID)
                .Select(h => (Guid?)h.ID));
    }

    private static Guid? FindWorkPermit(IObjectSpace objectSpace, Visa2014NaturalKeyLookup lookup)
    {
        if (lookup.OnDate is not DateTime day)
            return null;

        var start = day.Date;
        var next = start.AddDays(1);
        return FirstId(
            objectSpace.GetObjectsQuery<Bo.WorkPermit>()
                .Where(w => w.WorkPermitNumber == lookup.Number && w.IssuedDate >= start && w.IssuedDate < next)
                .OrderBy(w => w.ID)
                .Select(w => (Guid?)w.ID));
    }

    private static Guid? FindInvitation(IObjectSpace objectSpace, Visa2014NaturalKeyLookup lookup)
    {
        if (lookup.OnDate is not DateTime day)
            return null;

        var start = day.Date;
        var next = start.AddDays(1);
        return FirstId(
            objectSpace.GetObjectsQuery<Bo.Invitation>()
                .Where(i => i.InvitationNumber == lookup.Number && i.IssuedDate >= start && i.IssuedDate < next)
                .OrderBy(i => i.ID)
                .Select(i => (Guid?)i.ID));
    }

    private static Guid? FindWorkPermitItem(IObjectSpace objectSpace, Visa2014NaturalKeyLookup lookup)
    {
        if (lookup.OnDate is not DateTime day)
            return null;

        var start = day.Date;
        var next = start.AddDays(1);
        return FirstId(
            objectSpace.GetObjectsQuery<Bo.WorkPermitItem>()
                .Where(i => i.Person != null && i.Person.ID == lookup.PersonId
                    && i.WorkPermitNumber == lookup.Number
                    && i.StartDate >= start && i.StartDate < next)
                .OrderBy(i => i.ID)
                .Select(i => (Guid?)i.ID));
    }

    private static IQueryable<Guid?> FindEducation(IObjectSpace objectSpace, Visa2014NaturalKeyLookup lookup)
    {
        var query = objectSpace.GetObjectsQuery<Bo.Education>()
            .Where(e => e.Person != null && e.Person.ID == lookup.PersonId);
        query = string.IsNullOrEmpty(lookup.Number)
            ? query.Where(e => e.GraduationYear == null || e.GraduationYear == "")
            : query.Where(e => e.GraduationYear == lookup.Number);
        return query.OrderByDescending(e => e.ID).Select(e => (Guid?)e.ID);
    }

    private static Guid? FirstId(IQueryable<Guid?> query)
    {
        var id = query.FirstOrDefault();
        return id is Guid value && value != Guid.Empty ? value : null;
    }
}

internal static class Visa2014NaturalKeyRelink
{
    public static async Task<bool> TryLinkAsync(
        IVisa2014ImportTarget target,
        Visa2014NaturalKeyLookup? lookup,
        Guid legacyOid,
        IDictionary<Guid, Guid> idMap,
        bool verbose,
        string label)
    {
        if (lookup == null)
            return false;

        var existing = await target.TryFindExistingByNaturalKeyAsync(lookup);
        if (!existing.HasValue)
            return false;

        idMap[legacyOid] = existing.Value;
        target.RememberNaturalKey(lookup, existing.Value);
        if (verbose)
            Console.WriteLine($"  RELINK {label} {existing.Value} <- legacy {legacyOid} (natural key, row unchanged)");
        return true;
    }
}