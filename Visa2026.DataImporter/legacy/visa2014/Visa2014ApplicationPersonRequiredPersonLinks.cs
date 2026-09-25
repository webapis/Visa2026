using System.Data;
using System.Data.Common;
using DevExpress.Data.Filtering;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.EFCore;
using Microsoft.EntityFrameworkCore;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ApplicationPersonRoster;
using Bo = Visa2026.Module.BusinessObjects;

namespace Visa2026.DataImporter.Legacy.Visa2014;

/// <summary>
/// Pins Education / Position / AddressOfResidence / TravelHistory when the instance Application Profile
/// requires that kind. Prefers PersonInApplication FKs (id-map). Roster backfill then
/// attaches each person's own imported current row when the link is still missing.
/// WorkDuty has no legacy table: employees on profiles that show it get Description Ýok
/// when the roster link is still missing. Existing WorkDuty links are not replaced.
/// </summary>
internal static class Visa2014ApplicationPersonRequiredPersonLinks
{
    public static Guid? ResolveAddressLegacyKey(Visa2014ApplicationProfileInstancePersonRawRow raw)
    {
        if (raw.LegacyAddressOfResidenceOid.HasValue)
            return raw.LegacyAddressOfResidenceOid;

        if (raw.ForFamilyMember)
        {
            if (raw.LegacyEmployeeOid.HasValue &&
                (raw.LegacyDirectAddressOid.HasValue || raw.LegacyAddressOfResidenceOid == null))
                return Visa2014PiaAddressInference.PersonCanonicalSyntheticLegacyOid(raw.LegacyEmployeeOid.Value);

            return raw.LegacyDirectAddressOid;
        }

        if (raw.LegacyDirectAddressOid.HasValue)
            return raw.LegacyDirectAddressOid;

        var personOid = Visa2014ApplicationProfileInstancePersonTransform.ResolvePersonOid(raw);
        if (personOid.HasValue)
            return Visa2014PiaAddressInference.PersonCanonicalSyntheticLegacyOid(personOid.Value);

        return null;
    }

    public static int PinRequiredKinds(
        IObjectSpace objectSpace,
        Bo.ApplicationProfileInstance application,
        Bo.Person person,
        Visa2014ApplicationProfileInstancePersonRawRow raw,
        IReadOnlyDictionary<Guid, Guid> educationIdMap,
        IReadOnlyDictionary<Guid, Guid> currentEducationByPerson,
        IReadOnlyDictionary<Guid, Guid> addressIdMap,
        IReadOnlyDictionary<Guid, Guid> positionHistoryIdMap,
        IReadOnlyDictionary<Guid, Guid> travelHistoryIdMap,
        out int educationChanged,
        out int addressChanged,
        out int positionChanged,
        out int travelChanged,
        out int workDutyChanged,
        bool pinTravel = true)
    {
        educationChanged = 0;
        addressChanged = 0;
        positionChanged = 0;
        travelChanged = 0;
        workDutyChanged = 0;
        if (objectSpace == null || application == null || person == null)
            return 0;

        var asOf = application.ApplicationDate == default ? DateTime.Today : application.ApplicationDate.Date;
        var changed = 0;

        if (ApplicationProfileInstancePersonResolver.IsAutoLinkEnabled(
            application, ApplicationProfileInstancePersonLinkKind.Education))
        {
            var ids = ResolveEducationIds(
                objectSpace, person, raw, educationIdMap, currentEducationByPerson);
            if (ids.Count > 0)
                educationChanged = Visa2014ApplicationPersonDocumentLinks.ReplaceKind(
                    objectSpace, application, person,
                    ApplicationProfileInstancePersonLinkKind.Education, ids);
            changed += educationChanged;
        }

        if (ApplicationProfileInstancePersonResolver.IsAutoLinkEnabled(
            application, ApplicationProfileInstancePersonLinkKind.AddressOfResidence))
        {
            var ids = ResolveAddressIds(objectSpace, person, raw, addressIdMap, asOf);
            if (ids.Count > 0)
                addressChanged = Visa2014ApplicationPersonDocumentLinks.ReplaceKind(
                    objectSpace, application, person,
                    ApplicationProfileInstancePersonLinkKind.AddressOfResidence, ids);
            changed += addressChanged;
        }

        if (person.IsEmployee
            && ApplicationProfileInstancePersonResolver.IsAutoLinkEnabled(
            application, ApplicationProfileInstancePersonLinkKind.Position))
        {
            var ids = ResolvePositionIds(objectSpace, person, raw, positionHistoryIdMap, asOf);
            if (ids.Count > 0)
                positionChanged = Visa2014ApplicationPersonDocumentLinks.ReplaceKind(
                    objectSpace, application, person,
                    ApplicationProfileInstancePersonLinkKind.Position, ids);
            changed += positionChanged;
        }

        // PIA checkpoint / RegistrationDate / travel FKs exist only on Registration applications.
        // Do not pin travel from invitation / issuance / cancellation PIA rows.
        if (pinTravel
            && application.ApplicationProfile?.ActionFamily == ApplicationProfileActionFamily.Registration
            && ApplicationProfileInstancePersonResolver.IsAutoLinkEnabled(
                application, ApplicationProfileInstancePersonLinkKind.TravelHistory))
        {
            var ids = ResolveTravelHistoryIds(objectSpace, person, raw, travelHistoryIdMap);
            if (ids.Count > 0)
                travelChanged = Visa2014ApplicationPersonDocumentLinks.ReplaceKind(
                    objectSpace, application, person,
                    ApplicationProfileInstancePersonLinkKind.TravelHistory, ids);
            changed += travelChanged;
        }

        workDutyChanged = PinPlaceholderWorkDuty(objectSpace, application, person);
        changed += workDutyChanged;

        return changed;
    }

    /// <summary>
    /// One placeholder WorkDuty per employee roster line that still has no link.
    /// Officers replace Description Ýok on that case. A later import does not overwrite it.
    /// </summary>
    public static int PinPlaceholderWorkDuty(
        IObjectSpace objectSpace,
        Bo.ApplicationProfileInstance application,
        Bo.Person person)
    {
        if (objectSpace == null || application == null || person == null)
            return 0;

        var shows = ApplicationProfileInstancePersonResolver.IsAutoLinkEnabled(
            application, ApplicationProfileInstancePersonLinkKind.WorkDuty);
        var already = ApplicationProfileInstancePersonResolver
            .LoadLinks(objectSpace, application, person.ID)
            .Any(l => l.LinkKind == ApplicationProfileInstancePersonLinkKind.WorkDuty
                      && l.LinkedObjectId is Guid g && g != Guid.Empty);
        if (!Visa2014WorkDutyDefaults.ShouldCreate(person.IsEmployee, shows, already))
            return 0;

        var duty = objectSpace.CreateObject<Bo.WorkDuty>();
        duty.Person = person;
        duty.Description = Visa2014WorkDutyDefaults.Description;
        if (duty.ID == Guid.Empty)
            duty.ID = Guid.NewGuid();

        return Visa2014ApplicationPersonDocumentLinks.ReplaceKind(
            objectSpace,
            application,
            person,
            ApplicationProfileInstancePersonLinkKind.WorkDuty,
            [duty.ID]);
    }

    public static IReadOnlyList<Guid> ResolveEducationIds(
        IObjectSpace objectSpace,
        Bo.Person person,
        Visa2014ApplicationProfileInstancePersonRawRow raw,
        IReadOnlyDictionary<Guid, Guid> educationIdMap,
        IReadOnlyDictionary<Guid, Guid> currentEducationByPerson)
    {
        var personOid = Visa2014ApplicationProfileInstancePersonTransform.ResolvePersonOid(raw);
        Guid? legacyEducation = null;
        if (personOid.HasValue && currentEducationByPerson.TryGetValue(personOid.Value, out var mappedLegacy))
            legacyEducation = mappedLegacy;

        var mapped = Visa2014ApplicationPersonDocumentLinks.MapLegacyOids(educationIdMap, legacyEducation);
        if (mapped.Count > 0 && EducationBelongsToPerson(objectSpace, mapped[0], person.ID))
            return mapped;

        var current = CurrentEducationForPerson(objectSpace, person.ID);
        return current == Guid.Empty ? [] : [current];
    }

    public static IReadOnlyList<Guid> ResolveAddressIds(
        IObjectSpace objectSpace,
        Bo.Person person,
        Visa2014ApplicationProfileInstancePersonRawRow raw,
        IReadOnlyDictionary<Guid, Guid> addressIdMap,
        DateTime asOf)
    {
        var mapped = Visa2014ApplicationPersonDocumentLinks.MapLegacyOids(
            addressIdMap, ResolveAddressLegacyKey(raw));
        if (mapped.Count > 0 && AddressBelongsToPerson(objectSpace, mapped[0], person.ID))
            return mapped;

        var current = CurrentAddressForPerson(objectSpace, person.ID, asOf);
        return current == Guid.Empty ? [] : [current];
    }

    public static IReadOnlyList<Guid> ResolvePositionIds(
        IObjectSpace objectSpace,
        Bo.Person person,
        Visa2014ApplicationProfileInstancePersonRawRow raw,
        IReadOnlyDictionary<Guid, Guid> positionHistoryIdMap,
        DateTime asOf)
    {
        var mapped = Visa2014ApplicationPersonDocumentLinks.MapLegacyOids(
            positionHistoryIdMap, raw.LegacyPositionOid);
        if (mapped.Count > 0 && PositionBelongsToPerson(objectSpace, mapped[0], person.ID))
            return mapped;

        var current = CurrentPositionForPerson(objectSpace, person.ID, asOf);
        return current == Guid.Empty ? [] : [current];
    }

    public static IReadOnlyList<Guid> ResolveTravelHistoryIds(
        IObjectSpace objectSpace,
        Bo.Person person,
        Visa2014ApplicationProfileInstancePersonRawRow raw,
        IReadOnlyDictionary<Guid, Guid> travelHistoryIdMap)
    {
        var mapped = Visa2014ApplicationPersonDocumentLinks.MapLegacyOids(
            travelHistoryIdMap, raw.LegacyOid);
        if (mapped.Count > 0 && TravelBelongsToPerson(objectSpace, mapped[0], person.ID))
            return mapped;

        var current = CurrentTravelForPerson(objectSpace, person.ID);
        return current == Guid.Empty ? [] : [current];
    }

    /// <summary>
    /// After PIA pins, attach each roster person's own Education / Position / Address
    /// when the profile requires that kind and the ResolvedLink is still missing.
    /// </summary>
    public static (int Education, int Address, int Position, int Travel, int WorkDuty) BackfillFromRoster(
        IObjectSpace objectSpace,
        bool dryRun,
        bool education = true,
        bool address = true,
        bool position = true,
        bool travel = true,
        bool workDuty = true)
    {
        var educationChanged = 0;
        var addressChanged = 0;
        var positionChanged = 0;
        var travelChanged = 0;
        var workDutyChanged = 0;
        var db = (objectSpace as EFCoreObjectSpace)?.DbContext as Visa2026EFCoreDbContext;
        if (db == null)
            return (0, 0, 0, 0, 0);

        var conn = db.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open)
            conn.Open();

        if (education)
            educationChanged += ApplyBackfill(
                objectSpace, conn, dryRun, ApplicationProfileInstancePersonLinkKind.Education, MissingEducationSql);
        if (address)
            addressChanged += ApplyBackfill(
                objectSpace, conn, dryRun, ApplicationProfileInstancePersonLinkKind.AddressOfResidence, MissingAddressSql);
        if (position)
            positionChanged += ApplyBackfill(
                objectSpace, conn, dryRun, ApplicationProfileInstancePersonLinkKind.Position, MissingPositionSql);
        if (travel)
            travelChanged += ApplyBackfill(
                objectSpace, conn, dryRun, ApplicationProfileInstancePersonLinkKind.TravelHistory, MissingTravelSql);
        if (workDuty)
            workDutyChanged += ApplyWorkDutyBackfill(objectSpace, conn, dryRun);
        return (educationChanged, addressChanged, positionChanged, travelChanged, workDutyChanged);
    }

    private static int ApplyWorkDutyBackfill(IObjectSpace objectSpace, DbConnection conn, bool dryRun)
    {
        var rows = new List<(Guid InstanceId, Guid PersonId)>();
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = MissingWorkDutySql;
            cmd.CommandTimeout = 0;
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
                rows.Add((reader.GetGuid(0), reader.GetGuid(1)));
        }

        Console.WriteLine($"INF Roster backfill WorkDuty: {rows.Count} missing pair(s)");
        Console.Out.Flush();
        if (dryRun)
            return rows.Count;

        var changed = 0;
        var pending = 0;
        var done = 0;
        foreach (var (instanceId, personId) in rows)
        {
            var application = objectSpace.GetObjectByKey<Bo.ApplicationProfileInstance>(instanceId);
            var person = objectSpace.GetObjectByKey<Bo.Person>(personId);
            if (application == null || person == null)
                continue;

            changed += PinPlaceholderWorkDuty(objectSpace, application, person);
            pending++;
            done++;
            if (pending >= 50)
            {
                objectSpace.CommitChanges();
                pending = 0;
                Console.WriteLine($"INF Roster backfill WorkDuty: committed {done}/{rows.Count}");
                Console.Out.Flush();
            }
        }

        if (pending > 0)
            objectSpace.CommitChanges();

        return changed;
    }

    private static int ApplyBackfill(
        IObjectSpace objectSpace,
        DbConnection conn,
        bool dryRun,
        ApplicationProfileInstancePersonLinkKind kind,
        string sql)
    {
        var rows = new List<(Guid InstanceId, Guid PersonId, Guid ChildId)>();
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = sql;
            cmd.CommandTimeout = 0;
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                rows.Add((reader.GetGuid(0), reader.GetGuid(1), reader.GetGuid(2)));
            }
        }

        Console.WriteLine($"INF Roster backfill {kind}: {rows.Count} missing pair(s)");
        Console.Out.Flush();
        if (dryRun)
            return rows.Count;

        var changed = 0;
        var pending = 0;
        var done = 0;
        foreach (var (instanceId, personId, childId) in rows)
        {
            var application = objectSpace.GetObjectByKey<Bo.ApplicationProfileInstance>(instanceId);
            var person = objectSpace.GetObjectByKey<Bo.Person>(personId);
            if (application == null || person == null || childId == Guid.Empty)
                continue;

            changed += Visa2014ApplicationPersonDocumentLinks.ReplaceKind(
                objectSpace, application, person, kind, [childId]);
            pending++;
            done++;
            if (pending >= 50)
            {
                objectSpace.CommitChanges();
                pending = 0;
                Console.WriteLine($"INF Roster backfill {kind}: committed {done}/{rows.Count}");
                Console.Out.Flush();
            }
        }

        if (pending > 0)
            objectSpace.CommitChanges();

        return changed;
    }

    private static bool EducationBelongsToPerson(IObjectSpace objectSpace, Guid educationId, Guid personId)
    {
        var row = objectSpace.GetObjectByKey<Bo.Education>(educationId);
        return row?.Person != null && row.Person.ID == personId;
    }

    private static bool AddressBelongsToPerson(IObjectSpace objectSpace, Guid addressId, Guid personId)
    {
        var row = objectSpace.GetObjectByKey<Bo.AddressOfResidence>(addressId);
        return row?.Person != null && row.Person.ID == personId;
    }

    private static bool PositionBelongsToPerson(IObjectSpace objectSpace, Guid positionId, Guid personId)
    {
        var row = objectSpace.GetObjectByKey<Bo.EmployeePositionHistory>(positionId);
        return row?.Person != null && row.Person.ID == personId;
    }

    private static bool TravelBelongsToPerson(IObjectSpace objectSpace, Guid travelId, Guid personId)
    {
        var row = objectSpace.GetObjectByKey<Bo.TravelHistory>(travelId);
        return row?.Person != null && row.Person.ID == personId;
    }

    private static Guid CurrentEducationForPerson(IObjectSpace objectSpace, Guid personId)
    {
        var rows = objectSpace.GetObjectsQuery<Bo.Education>()
            .Where(e => e.Person != null && e.Person.ID == personId)
            .ToList();
        return rows
            .OrderByDescending(e => ParseGraduationYear(e.GraduationYear))
            .ThenByDescending(e => e.ID)
            .FirstOrDefault()?.ID ?? Guid.Empty;
    }

    private static Guid CurrentAddressForPerson(IObjectSpace objectSpace, Guid personId, DateTime asOf)
    {
        var rows = objectSpace.GetObjectsQuery<Bo.AddressOfResidence>()
            .Where(a => a.Person != null && a.Person.ID == personId)
            .ToList();
        return PickCurrentAddress(rows, asOf)?.ID ?? Guid.Empty;
    }

    private static Guid CurrentPositionForPerson(IObjectSpace objectSpace, Guid personId, DateTime asOf)
    {
        var rows = objectSpace.GetObjectsQuery<Bo.EmployeePositionHistory>()
            .Where(h => h.Person != null && h.Person.ID == personId)
            .ToList();
        return PickCurrentPosition(rows, asOf)?.ID ?? Guid.Empty;
    }

    private static Guid CurrentTravelForPerson(IObjectSpace objectSpace, Guid personId)
    {
        var rows = objectSpace.GetObjectsQuery<Bo.TravelHistory>()
            .Where(t => t.Person != null && t.Person.ID == personId)
            .ToList();
        return rows
            .OrderByDescending(t => t.TravelDate.Date)
            .ThenByDescending(t => t.ID)
            .FirstOrDefault()?.ID ?? Guid.Empty;
    }

    private static int ParseGraduationYear(string? year) =>
        int.TryParse(year?.Trim(), out var parsed) ? parsed : int.MinValue;

    private static Bo.AddressOfResidence? PickCurrentAddress(
        IReadOnlyList<Bo.AddressOfResidence> live,
        DateTime asOf)
    {
        if (live.Count == 0)
            return null;

        var stillValid = live
            .Where(a => !a.ExpirationDate.HasValue || a.ExpirationDate.Value.Date >= asOf)
            .ToList();
        var pool = stillValid.Count > 0 ? stillValid : live;
        return pool
            .OrderByDescending(a => a.ExpirationDate?.Date ?? DateTime.MaxValue)
            .ThenByDescending(a => a.ID)
            .FirstOrDefault();
    }

    private static Bo.EmployeePositionHistory? PickCurrentPosition(
        IReadOnlyList<Bo.EmployeePositionHistory> live,
        DateTime asOf)
    {
        if (live.Count == 0)
            return null;

        var open = live
            .Where(h => !h.EndDate.HasValue || h.EndDate.Value.Date >= asOf)
            .ToList();
        var pool = open.Count > 0 ? open : live;
        return pool
            .OrderByDescending(h => h.StartDate)
            .ThenByDescending(h => h.ID)
            .FirstOrDefault();
    }

    private const string MissingWorkDutySql = """
        SELECT pe."ApplicationProfileInstanceId", pe."PersonId"
        FROM "ApplicationProfileInstancePeople" pe
        INNER JOIN "ApplicationProfileInstances" i
            ON i."ID" = pe."ApplicationProfileInstanceId" AND COALESCE(i."GCRecord", 0) = 0
        INNER JOIN "ApplicationProfiles" pr
            ON pr."ID" = i."ApplicationProfileID" AND pr."RequirePersonPosition" = TRUE
        INNER JOIN "People" ppl
            ON ppl."ID" = pe."PersonId"
            AND ppl."IsEmployee" = TRUE
            AND COALESCE(ppl."GCRecord", 0) = 0
        WHERE NOT EXISTS (
            SELECT 1
            FROM "ApplicationProfileInstancePersonResolvedLinks" l
            WHERE l."ApplicationProfileInstanceId" = pe."ApplicationProfileInstanceId"
              AND l."PersonId" = pe."PersonId"
              AND l."LinkKind" = 12
              AND COALESCE(l."GCRecord", 0) = 0)
        """;

    private const string MissingEducationSql = """
        SELECT DISTINCT ON (pe."ApplicationProfileInstanceId", pe."PersonId")
            pe."ApplicationProfileInstanceId",
            pe."PersonId",
            e."ID"
        FROM "ApplicationProfileInstancePeople" pe
        INNER JOIN "ApplicationProfileInstances" i
            ON i."ID" = pe."ApplicationProfileInstanceId" AND COALESCE(i."GCRecord", 0) = 0
        INNER JOIN "ApplicationProfiles" pr
            ON pr."ID" = i."ApplicationProfileID" AND pr."RequirePersonEducation" = TRUE
        INNER JOIN "Educations" e
            ON e."PersonID" = pe."PersonId" AND COALESCE(e."GCRecord", 0) = 0
        WHERE NOT EXISTS (
            SELECT 1
            FROM "ApplicationProfileInstancePersonResolvedLinks" l
            WHERE l."ApplicationProfileInstanceId" = pe."ApplicationProfileInstanceId"
              AND l."PersonId" = pe."PersonId"
              AND l."LinkKind" = 2
              AND COALESCE(l."GCRecord", 0) = 0)
        ORDER BY pe."ApplicationProfileInstanceId", pe."PersonId",
            CASE WHEN e."GraduationYear" ~ '^[0-9]+$' THEN e."GraduationYear"::int ELSE -2147483648 END DESC,
            e."ID" DESC
        """;

    private const string MissingAddressSql = """
        SELECT DISTINCT ON (pe."ApplicationProfileInstanceId", pe."PersonId")
            pe."ApplicationProfileInstanceId",
            pe."PersonId",
            a."ID"
        FROM "ApplicationProfileInstancePeople" pe
        INNER JOIN "ApplicationProfileInstances" i
            ON i."ID" = pe."ApplicationProfileInstanceId" AND COALESCE(i."GCRecord", 0) = 0
        INNER JOIN "ApplicationProfiles" pr
            ON pr."ID" = i."ApplicationProfileID" AND pr."RequirePersonAddressOfResidence" = TRUE
        INNER JOIN "AddressesOfResidence" a
            ON a."PersonID" = pe."PersonId" AND COALESCE(a."GCRecord", 0) = 0
        WHERE NOT EXISTS (
            SELECT 1
            FROM "ApplicationProfileInstancePersonResolvedLinks" l
            WHERE l."ApplicationProfileInstanceId" = pe."ApplicationProfileInstanceId"
              AND l."PersonId" = pe."PersonId"
              AND l."LinkKind" = 3
              AND COALESCE(l."GCRecord", 0) = 0)
        ORDER BY pe."ApplicationProfileInstanceId", pe."PersonId",
            COALESCE(a."ExpirationDate", '9999-12-31'::date) DESC,
            a."ID" DESC
        """;

    private const string MissingPositionSql = """
        SELECT DISTINCT ON (pe."ApplicationProfileInstanceId", pe."PersonId")
            pe."ApplicationProfileInstanceId",
            pe."PersonId",
            h."ID"
        FROM "ApplicationProfileInstancePeople" pe
        INNER JOIN "ApplicationProfileInstances" i
            ON i."ID" = pe."ApplicationProfileInstanceId" AND COALESCE(i."GCRecord", 0) = 0
        INNER JOIN "ApplicationProfiles" pr
            ON pr."ID" = i."ApplicationProfileID" AND pr."RequirePersonPosition" = TRUE
        INNER JOIN "People" ppl
            ON ppl."ID" = pe."PersonId"
            AND ppl."IsEmployee" = TRUE
            AND COALESCE(ppl."GCRecord", 0) = 0
        INNER JOIN "EmployeePositionHistories" h
            ON h."PersonID" = pe."PersonId" AND COALESCE(h."GCRecord", 0) = 0
        WHERE NOT EXISTS (
            SELECT 1
            FROM "ApplicationProfileInstancePersonResolvedLinks" l
            WHERE l."ApplicationProfileInstanceId" = pe."ApplicationProfileInstanceId"
              AND l."PersonId" = pe."PersonId"
              AND l."LinkKind" = 4
              AND COALESCE(l."GCRecord", 0) = 0)
        ORDER BY pe."ApplicationProfileInstanceId", pe."PersonId",
            h."StartDate" DESC,
            h."ID" DESC
        """;

    private const string MissingTravelSql = """
        SELECT DISTINCT ON (pe."ApplicationProfileInstanceId", pe."PersonId")
            pe."ApplicationProfileInstanceId",
            pe."PersonId",
            t."ID"
        FROM "ApplicationProfileInstancePeople" pe
        INNER JOIN "ApplicationProfileInstances" i
            ON i."ID" = pe."ApplicationProfileInstanceId" AND COALESCE(i."GCRecord", 0) = 0
        INNER JOIN "ApplicationProfiles" pr
            ON pr."ID" = i."ApplicationProfileID"
            AND pr."RequirePersonTravelHistory" = TRUE
            AND pr."ActionFamily" = 2
        INNER JOIN "TravelHistories" t
            ON t."PersonID" = pe."PersonId" AND COALESCE(t."GCRecord", 0) = 0
        WHERE NOT EXISTS (
            SELECT 1
            FROM "ApplicationProfileInstancePersonResolvedLinks" l
            WHERE l."ApplicationProfileInstanceId" = pe."ApplicationProfileInstanceId"
              AND l."PersonId" = pe."PersonId"
              AND l."LinkKind" = 11
              AND COALESCE(l."GCRecord", 0) = 0)
        ORDER BY pe."ApplicationProfileInstanceId", pe."PersonId",
            t."TravelDate" DESC,
            t."ID" DESC
        """;
}