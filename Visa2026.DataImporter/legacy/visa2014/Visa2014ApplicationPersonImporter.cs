using System.Linq;
using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ApplicationPersonRoster;
using Visa2026.Module.Services.MigrationImport;
using Bo = Visa2026.Module.BusinessObjects;

namespace Visa2026.DataImporter.Legacy.Visa2014;

internal sealed class Visa2014ApplicationProfileInstancePersonImportResult
{
    public int LegacyRowCount { get; init; }
    public int PreparedCount { get; init; }
    public int SkippedCount { get; init; }
    public int DedupeMergedCount { get; init; }
    public int SkippedMissingRequiredIdMap { get; init; }
    public int SkippedAlreadyImported { get; init; }
    public int PostedCount { get; init; }
    public int FailedCount { get; init; }
    public int AutoLinkedCount { get; init; }
    public string? IdMapPath { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = [];
}

/// <summary>
/// Wave 2b: PersonInApplication → skip-navigation People on ApplicationProfileInstance (id-map keys PersonInApplication.Oid → Person.ID).
/// Headless ObjectSpace only (not OData).
/// </summary>
internal static class Visa2014ApplicationProfileInstancePersonImporter
{
    public static async Task<Visa2014ApplicationProfileInstancePersonImportResult> RunAsync(
        INonSecuredObjectSpaceFactory? objectSpaceFactory,
        string legacyConnectionString,
        IReadOnlyList<string> lookupTranslationPaths,
        string applicationIdMapPath,
        string personIdMapPath,
        string? applicationPersonIdMapOutputPath,
        int? maxRows,
        bool dryRun,
        bool verbose,
        int batchSize = 50,
        string? passportIdMapPath = null,
        string? visaIdMapPath = null,
        string? workPermitItemIdMapPath = null,
        string? educationIdMapPath = null,
        string? addressIdMapPath = null,
        string? positionHistoryIdMapPath = null,
        string? travelHistoryIdMapPath = null)
    {
        if (!dryRun && objectSpaceFactory == null)
        {
            return new Visa2014ApplicationProfileInstancePersonImportResult
            {
                Errors = ["ApplicationProfileInstancePerson import requires ObjectSpaceFactory when not dry-run."],
                FailedCount = 1,
            };
        }

        var applicationIdMap = Visa2014IdMapHelper.Load(applicationIdMapPath);
        var personIdMap = Visa2014IdMapHelper.Load(personIdMapPath);
        var passportIdMap = LoadOptionalIdMap(passportIdMapPath);
        var visaIdMap = LoadOptionalIdMap(visaIdMapPath);
        var workPermitItemIdMap = LoadOptionalIdMap(workPermitItemIdMapPath);
        var educationIdMap = LoadOptionalIdMap(educationIdMapPath);
        var addressIdMap = LoadOptionalIdMap(addressIdMapPath);
        var positionHistoryIdMap = LoadOptionalIdMap(positionHistoryIdMapPath);
        var travelHistoryIdMap = LoadOptionalIdMap(travelHistoryIdMapPath);
        var currentEducationByPerson = Visa2014PersonCurrentFieldInference.BuildCurrentEducationByPerson(
            legacyConnectionString, verbose);
        if (applicationIdMap.Count == 0)
        {
            return new Visa2014ApplicationProfileInstancePersonImportResult
            {
                Errors = ["ApplicationProfileInstance id-map is empty — import ApplicationProfileInstance first."],
                FailedCount = 1,
            };
        }

        if (personIdMap.Count == 0)
        {
            return new Visa2014ApplicationProfileInstancePersonImportResult
            {
                Errors = ["Person id-map is empty — import Person first."],
                FailedCount = 1,
            };
        }

        var collisions = Visa2014ApplicationTransform.FindApplicationProfileInstanceIdMapCrossDateCollisions(
            applicationIdMap,
            legacyConnectionString,
            lookupTranslationPaths);
        if (collisions.Count > 0)
        {
            Console.Error.WriteLine(
                $"ERR ApplicationProfileInstance id-map has {collisions.Count} cross-date collision(s). " +
                "Rebuild with --rebuild-visa2014-id-maps --entity ApplicationProfileInstance.");
            return new Visa2014ApplicationProfileInstancePersonImportResult
            {
                FailedCount = collisions.Count,
                Errors = collisions,
            };
        }

        var rawRows = Visa2014ApplicationProfileInstancePersonTransform.LoadRawRows(legacyConnectionString, maxRows, verbose);
        var rawByOid = rawRows.ToDictionary(r => r.LegacyOid);
        var batch = Visa2014ApplicationProfileInstancePersonTransform.Transform(rawRows, out var skipped, out _);
        var existingMap = LoadOptionalIdMap(applicationPersonIdMapOutputPath);

        if (dryRun)
        {
            var gap = AnalyzeGap(batch.ImportRows, applicationIdMap, personIdMap, existingMap);
            Console.WriteLine(
                $"DRY RUN: {batch.ImportRows.Count} prepared ApplicationProfileInstancePerson row(s) " +
                $"({skipped.Count} transform-skipped).");
            Console.WriteLine($"INF Already imported (id-map): {gap.AlreadyImported}");
            Console.WriteLine($"INF Missing parent id-map: {gap.MissingRequiredIdMap}");
            Console.WriteLine($"INF Ready to link: {gap.ReadyToPost}");
            return new Visa2014ApplicationProfileInstancePersonImportResult
            {
                LegacyRowCount = batch.LegacyRowCount,
                PreparedCount = batch.ImportRows.Count,
                SkippedCount = skipped.Count,
                SkippedMissingRequiredIdMap = gap.MissingRequiredIdMap,
                SkippedAlreadyImported = gap.AlreadyImported,
            };
        }

        var posted = 0;
        var failed = 0;
        var skippedAlready = 0;
        var skippedMissing = 0;
        var autoLinked = 0;
        var processed = 0;
        var errors = new List<string>();
        var idMap = new Dictionary<Guid, Guid>(existingMap);
        var total = batch.ImportRows.Count;
        if (batchSize < 1)
            batchSize = 50;

        Console.WriteLine($"INF ApplicationProfileInstancePerson in-process link: {total} row(s), batchSize={batchSize}");
        Console.Out.Flush();

        using var objectSpace = objectSpaceFactory!.CreateNonSecuredObjectSpace(typeof(Bo.ApplicationProfileInstance));
        MigrationImportContext.ApplyImportObjectSpaceHooks(objectSpace);

        var pendingInBatch = 0;
        foreach (var row in batch.ImportRows)
        {
            var legacyOid = (Guid)row["_legacyRowId"]!;
            processed++;

            var alreadyImported = idMap.ContainsKey(legacyOid);
            if (!TryResolveIds(row, applicationIdMap, personIdMap, out var applicationId, out var personId, out var miss))
            {
                if (alreadyImported)
                    skippedAlready++;
                else
                    skippedMissing++;
                if (verbose)
                    Console.WriteLine($"  SKIP {legacyOid}: {miss}");
                ReportProgress(applicationPersonIdMapOutputPath, processed, total, posted, failed, skippedAlready + skippedMissing);
                continue;
            }

            try
            {
                var application = objectSpace.GetObjectByKey<Bo.ApplicationProfileInstance>(applicationId);
                var person = objectSpace.GetObjectByKey<Bo.Person>(personId);
                if (application == null || person == null)
                {
                    if (alreadyImported)
                        skippedAlready++;
                    else
                        skippedMissing++;
                    if (verbose)
                        Console.WriteLine($"  SKIP {legacyOid}: target Application/Person missing in DB");
                    ReportProgress(applicationPersonIdMapOutputPath, processed, total, posted, failed, skippedAlready + skippedMissing);
                    continue;
                }

                var linked = ApplicationProfileInstancePersonService.LinkPerson(objectSpace, application, person);
                if (linked == null)
                {
                    failed++;
                    errors.Add($"{legacyOid}: LinkPerson returned null");
                    ReportProgress(applicationPersonIdMapOutputPath, processed, total, posted, failed, skippedAlready + skippedMissing);
                    continue;
                }

                PinDocumentSnapshot(objectSpace, application, linked, row, passportIdMap, visaIdMap, workPermitItemIdMap);
                if (rawByOid.TryGetValue(legacyOid, out var raw))
                {
                    Visa2014ApplicationPersonRequiredPersonLinks.PinRequiredKinds(
                        objectSpace, application, linked, raw,
                        educationIdMap, currentEducationByPerson, addressIdMap, positionHistoryIdMap,
                        travelHistoryIdMap,
                        out _, out _, out _, out _,
                        pinTravel: false);
                }

                if (alreadyImported)
                    skippedAlready++;
                else
                {
                    idMap[legacyOid] = personId;
                    posted++;
                }

                autoLinked += ApplicationProfileInstancePersonResolver.LoadLinks(objectSpace, application.ID, personId).Count;
                pendingInBatch++;

                if (pendingInBatch >= batchSize)
                {
                    objectSpace.CommitChanges();
                    pendingInBatch = 0;
                    if (!string.IsNullOrWhiteSpace(applicationPersonIdMapOutputPath))
                        await Visa2014IdMapHelper.SaveAsync(applicationPersonIdMapOutputPath, idMap);
                }
            }
            catch (Exception ex)
            {
                failed++;
                errors.Add($"{legacyOid}: {ex.Message}");
                try { objectSpace.Rollback(); } catch { /* ignore */ }
                pendingInBatch = 0;
            }

            ReportProgress(applicationPersonIdMapOutputPath, processed, total, posted, failed, skippedAlready + skippedMissing);
        }

        if (pendingInBatch > 0)
            objectSpace.CommitChanges();

        var rosterBackfill = Visa2014ApplicationPersonRequiredPersonLinks.BackfillFromRoster(
            objectSpace, dryRun: false);
        Console.WriteLine(
            $"INF Roster EPA backfill: education {rosterBackfill.Education} " +
            $"address {rosterBackfill.Address} position {rosterBackfill.Position} travel {rosterBackfill.Travel}");
        Console.Out.Flush();

        if (!string.IsNullOrWhiteSpace(applicationPersonIdMapOutputPath))
            await Visa2014IdMapHelper.SaveAsync(applicationPersonIdMapOutputPath, idMap);

        return new Visa2014ApplicationProfileInstancePersonImportResult
        {
            LegacyRowCount = batch.LegacyRowCount,
            PreparedCount = batch.ImportRows.Count,
            SkippedCount = skipped.Count,
            SkippedMissingRequiredIdMap = skippedMissing,
            SkippedAlreadyImported = skippedAlready,
            PostedCount = posted,
            FailedCount = failed,
            AutoLinkedCount = autoLinked,
            IdMapPath = applicationPersonIdMapOutputPath,
            Errors = errors,
        };
    }

    private static void ReportProgress(
        string? idMapPath,
        int processed,
        int total,
        int posted,
        int failed,
        int skipped)
    {
        Visa2014SyncUpsertHelper.ReportImportLoopProgress(
            idMapPath,
            "ApplicationProfileInstancePerson",
            processed,
            total,
            posted,
            failed,
            skipped);
    }

    private static void PinDocumentSnapshot(
        DevExpress.ExpressApp.IObjectSpace objectSpace,
        Bo.ApplicationProfileInstance application,
        Bo.Person person,
        Dictionary<string, object?> row,
        IReadOnlyDictionary<Guid, Guid> passportIdMap,
        IReadOnlyDictionary<Guid, Guid> visaIdMap,
        IReadOnlyDictionary<Guid, Guid> workPermitItemIdMap)
    {
        var passportIds = Visa2014ApplicationPersonDocumentLinks.MapLegacyOids(
            passportIdMap,
            ParseGuid(row.GetValueOrDefault("PreviousPassport") as string),
            ParseGuid(row.GetValueOrDefault("CurrentPassport") as string));
        var visaIds = Visa2014ApplicationPersonDocumentLinks.MapLegacyOids(
            visaIdMap,
            ParseGuid(row.GetValueOrDefault("CurrentVisa") as string));
        var workPermitItemIds = Visa2014ApplicationPersonDocumentLinks.MapLegacyOids(
            workPermitItemIdMap,
            ParseGuid(row.GetValueOrDefault("CurrentWorkPermitItem") as string));

        if (passportIds.Count > 0)
        {
            Visa2014ApplicationPersonDocumentLinks.ReplaceKind(
                objectSpace, application, person,
                ApplicationProfileInstancePersonLinkKind.Passport, passportIds);
        }

        if (visaIds.Count > 0)
        {
            Visa2014ApplicationPersonDocumentLinks.ReplaceKind(
                objectSpace, application, person,
                ApplicationProfileInstancePersonLinkKind.Visa, visaIds);
        }

        if (workPermitItemIds.Count > 0)
        {
            Visa2014ApplicationPersonDocumentLinks.ReplaceKind(
                objectSpace, application, person,
                ApplicationProfileInstancePersonLinkKind.WorkPermitItem, workPermitItemIds);
        }
    }

    private static Guid? ParseGuid(string? text) =>
        Guid.TryParse(text, out var g) ? g : null;

    private static bool TryResolveIds(
        Dictionary<string, object?> row,
        IReadOnlyDictionary<Guid, Guid> applicationIdMap,
        IReadOnlyDictionary<Guid, Guid> personIdMap,
        out Guid applicationId,
        out Guid personId,
        out string miss)
    {
        applicationId = Guid.Empty;
        personId = Guid.Empty;
        miss = "";

        if (!Guid.TryParse(Convert.ToString(row.GetValueOrDefault("Application")), out var legacyApp)
            || !applicationIdMap.TryGetValue(legacyApp, out applicationId))
        {
            miss = "ApplicationProfileInstance not in id-map";
            return false;
        }

        if (!Guid.TryParse(Convert.ToString(row.GetValueOrDefault("Person")), out var legacyPerson)
            || !personIdMap.TryGetValue(legacyPerson, out personId))
        {
            miss = "Person not in id-map";
            return false;
        }

        return true;
    }

    private static (int AlreadyImported, int MissingRequiredIdMap, int ReadyToPost) AnalyzeGap(
        IReadOnlyList<Dictionary<string, object?>> rows,
        IReadOnlyDictionary<Guid, Guid> applicationIdMap,
        IReadOnlyDictionary<Guid, Guid> personIdMap,
        IReadOnlyDictionary<Guid, Guid> existingMap)
    {
        var already = 0;
        var missing = 0;
        var ready = 0;
        foreach (var row in rows)
        {
            var legacyOid = (Guid)row["_legacyRowId"]!;
            if (existingMap.ContainsKey(legacyOid))
            {
                already++;
                continue;
            }

            if (!TryResolveIds(row, applicationIdMap, personIdMap, out _, out _, out _))
            {
                missing++;
                continue;
            }

            ready++;
        }

        return (already, missing, ready);
    }

    private static Dictionary<Guid, Guid> LoadOptionalIdMap(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            return new Dictionary<Guid, Guid>();
        return Visa2014IdMapHelper.Load(path);
    }
}