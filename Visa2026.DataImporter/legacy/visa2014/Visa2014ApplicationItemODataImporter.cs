using System.Text.Json;
using Visa2026.DataImporter;

namespace Visa2026.DataImporter.Legacy.Visa2014;

internal sealed class Visa2014ApplicationItemImportResult
{
    public int LegacyRowCount { get; init; }
    public int PreparedCount { get; init; }
    public int SkippedCount { get; init; }
    public int DedupeMergedCount { get; init; }
    public int SkippedMissingRequiredIdMap { get; init; }
    public int SkippedAlreadyImported { get; init; }
    public int PostedCount { get; init; }
    public int FailedCount { get; init; }
    public string? IdMapPath { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = [];
}

internal static class Visa2014ApplicationItemODataImporter
{
    public static async Task<Visa2014ApplicationItemImportResult> RunAsync(
        IVisa2014ImportTarget target,
        Visa2014ODataLookupResolver resolver,
        string legacyConnectionString,
        IReadOnlyList<string> lookupTranslationPaths,
        string applicationIdMapPath,
        string personIdMapPath,
        string passportIdMapPath,
        string visaIdMapPath,
        string employeePositionHistoryIdMapPath,
        string addressOfResidenceIdMapPath,
        string educationIdMapPath,
        string employeeSalaryIdMapPath,
        string? workPermitItemIdMapPath,
        string? invitationItemIdMapPath,
        string? applicationItemIdMapOutputPath,
        int? maxRows,
        bool dryRun,
        bool verbose)
    {
        var applicationIdMap = Visa2014IdMapHelper.Load(applicationIdMapPath);
        var personIdMap = Visa2014IdMapHelper.Load(personIdMapPath);
        var passportIdMap = Visa2014IdMapHelper.Load(passportIdMapPath);
        var visaIdMap = LoadOptionalIdMap(visaIdMapPath);
        var positionHistoryIdMap = LoadOptionalIdMap(employeePositionHistoryIdMapPath);
        var addressIdMap = LoadOptionalIdMap(addressOfResidenceIdMapPath);
        var educationIdMap = LoadOptionalIdMap(educationIdMapPath);
        var employeeSalaryIdMap = LoadOptionalIdMap(employeeSalaryIdMapPath);
        var workPermitItemIdMap = LoadOptionalIdMap(workPermitItemIdMapPath);
        var invitationItemIdMap = LoadOptionalIdMap(invitationItemIdMapPath);

        var applicationIdMapCollisions = Visa2014ApplicationTransform.FindApplicationIdMapCrossDateCollisions(
            applicationIdMap,
            legacyConnectionString,
            lookupTranslationPaths);
        if (applicationIdMapCollisions.Count > 0)
        {
            Console.Error.WriteLine(
                $"ERR Application id-map has {applicationIdMapCollisions.Count} cross-date collision(s). " +
                "Rebuild with --rebuild-visa2014-id-maps --entity Application (matches FullApplicationNumber + ApplicationDate per legacy Oid).");
            foreach (var collision in applicationIdMapCollisions.Take(20))
                Console.Error.WriteLine($"ERR   {collision}");
            if (applicationIdMapCollisions.Count > 20)
                Console.Error.WriteLine($"ERR   ... and {applicationIdMapCollisions.Count - 20} more");
            return new Visa2014ApplicationItemImportResult
            {
                LegacyRowCount = 0,
                FailedCount = applicationIdMapCollisions.Count,
                Errors = applicationIdMapCollisions,
            };
        }

        if (verbose)
        {
            Console.WriteLine($"INF Application id-map entries: {applicationIdMap.Count}");
            Console.WriteLine($"INF Person id-map entries: {personIdMap.Count}");
            Console.WriteLine($"INF Passport id-map entries: {passportIdMap.Count}");
            Console.WriteLine($"INF Visa id-map entries: {visaIdMap.Count}");
            Console.WriteLine($"INF EmployeePositionHistory id-map entries: {positionHistoryIdMap.Count}");
            Console.WriteLine($"INF AddressOfResidence id-map entries: {addressIdMap.Count}");
            Console.WriteLine($"INF Education id-map entries: {educationIdMap.Count}");
            Console.WriteLine($"INF EmployeeSalary id-map entries: {employeeSalaryIdMap.Count}");
            Console.WriteLine($"INF WorkPermitItem id-map entries: {workPermitItemIdMap.Count}");
            Console.WriteLine($"INF InvitationItem id-map entries: {invitationItemIdMap.Count}");
        }

        var batch = Visa2014ApplicationItemTransform.PrepareImportBatch(
            legacyConnectionString,
            lookupTranslationPaths,
            maxRows,
            verbose);

        var applicationItemIdMap = LoadOptionalIdMap(applicationItemIdMapOutputPath);

        if (dryRun)
        {
            var gap = AnalyzeImportGap(
                batch.ImportRows,
                applicationIdMap,
                personIdMap,
                passportIdMap,
                applicationItemIdMap);
            Console.WriteLine(
                $"DRY RUN: {batch.ImportRows.Count} prepared row(s) " +
                $"({batch.Skipped.Count} transform-skipped, {batch.DedupeMergedCount} dedupe merged).");
            Console.WriteLine($"INF Already imported (id-map): {gap.AlreadyImported}");
            Console.WriteLine($"INF Missing parent id-map: {gap.MissingRequiredIdMap}");
            if (gap.MissingRequiredIdMap > 0)
            {
                Console.WriteLine($"INF   Application not in id-map: {gap.MissingApplication}");
                Console.WriteLine($"INF   Person not in id-map: {gap.MissingPerson}");
                Console.WriteLine($"INF   Passport not in id-map: {gap.MissingPassport}");
                Console.WriteLine($"INF   Missing legacy FK on row: {gap.MissingLegacyField}");
            }
            Console.WriteLine($"INF Ready to POST (remainder): {gap.ReadyToPost}");
            return new Visa2014ApplicationItemImportResult
            {
                LegacyRowCount = batch.LegacyRowCount,
                PreparedCount = batch.ImportRows.Count,
                SkippedCount = batch.Skipped.Count,
                DedupeMergedCount = batch.DedupeMergedCount,
                SkippedMissingRequiredIdMap = gap.MissingRequiredIdMap,
                SkippedAlreadyImported = gap.AlreadyImported,
            };
        }
        if (verbose && applicationItemIdMap.Count > 0)
            Console.WriteLine($"INF Existing ApplicationItem id-map entries: {applicationItemIdMap.Count}");

        var errors = new List<string>();
        int posted = 0;
        int failed = 0;
        int skippedMissingRequired = 0;
        int skippedAlreadyImported = 0;

        foreach (var row in batch.ImportRows)
        {
            var legacyOid = (Guid)row["_legacyRowId"]!;
            if (applicationItemIdMap.ContainsKey(legacyOid))
            {
                skippedAlreadyImported++;
                if (verbose)
                    Console.WriteLine($"  SKIP {legacyOid}: already in ApplicationItem id-map");
                continue;
            }

            if (!TryResolveRequiredIds(
                    row,
                    applicationIdMap,
                    personIdMap,
                    passportIdMap,
                    out var applicationId,
                    out var personId,
                    out var passportId,
                    out var missingReason))
            {
                skippedMissingRequired++;
                if (verbose)
                    Console.WriteLine($"  SKIP {legacyOid}: {missingReason}");
                continue;
            }

            try
            {
                var payload = BuildPayload(
                    row,
                    resolver,
                    applicationId,
                    personId,
                    passportId,
                    passportIdMap,
                    visaIdMap,
                    positionHistoryIdMap,
                    addressIdMap,
                    educationIdMap,
                    employeeSalaryIdMap,
                    workPermitItemIdMap,
                    invitationItemIdMap);
                if (payload == null)
                {
                    failed++;
                    errors.Add($"{legacyOid}: incomplete OData payload ({DescribePayloadGap(row, resolver)})");
                    continue;
                }

                var createdId = await target.CreateAsync(typeof(Visa2026.Module.BusinessObjects.ApplicationItem), payload);
                if (!createdId.HasValue)
                {
                    failed++;
                    errors.Add($"{legacyOid}: create returned null");
                    continue;
                }

                applicationItemIdMap[legacyOid] = createdId.Value;
                posted++;
                if (posted % 250 == 0)
                {
                    Console.WriteLine(
                        $"INF Progress: {posted} posted, {failed} failed, {skippedMissingRequired} missing required map...");
                    if (!string.IsNullOrWhiteSpace(applicationItemIdMapOutputPath))
                        await Visa2014IdMapHelper.SaveAsync(applicationItemIdMapOutputPath, applicationItemIdMap);
                }
                if (verbose)
                    Console.WriteLine($"  SAVE ApplicationItem {createdId.Value} <- legacy {legacyOid}");
            }
            catch (Exception ex)
            {
                failed++;
                errors.Add($"{legacyOid}: {ex.Message}");
                Console.Error.WriteLine($"ERR {legacyOid}: {ex.Message}");
            }
        }

        await target.FlushAsync();

        string? idMapPath = null;
        if (applicationItemIdMap.Count > 0 && !string.IsNullOrWhiteSpace(applicationItemIdMapOutputPath))
        {
            await Visa2014IdMapHelper.SaveAsync(applicationItemIdMapOutputPath, applicationItemIdMap);
            idMapPath = Path.GetFullPath(applicationItemIdMapOutputPath);
        }

        return new Visa2014ApplicationItemImportResult
        {
            LegacyRowCount = batch.LegacyRowCount,
            PreparedCount = batch.ImportRows.Count,
            SkippedCount = batch.Skipped.Count,
            DedupeMergedCount = batch.DedupeMergedCount,
            SkippedMissingRequiredIdMap = skippedMissingRequired,
            SkippedAlreadyImported = skippedAlreadyImported,
            PostedCount = posted,
            FailedCount = failed,
            IdMapPath = idMapPath,
            Errors = errors,
        };
    }

    private sealed record ApplicationItemImportGap(
        int AlreadyImported,
        int MissingRequiredIdMap,
        int MissingApplication,
        int MissingPerson,
        int MissingPassport,
        int MissingLegacyField,
        int ReadyToPost);

    private static ApplicationItemImportGap AnalyzeImportGap(
        IReadOnlyList<Dictionary<string, object?>> importRows,
        IReadOnlyDictionary<Guid, Guid> applicationIdMap,
        IReadOnlyDictionary<Guid, Guid> personIdMap,
        IReadOnlyDictionary<Guid, Guid> passportIdMap,
        IReadOnlyDictionary<Guid, Guid> applicationItemIdMap)
    {
        int alreadyImported = 0;
        int missingApplication = 0;
        int missingPerson = 0;
        int missingPassport = 0;
        int missingLegacyField = 0;
        int readyToPost = 0;

        foreach (var row in importRows)
        {
            var legacyOid = (Guid)row["_legacyRowId"]!;
            if (applicationItemIdMap.ContainsKey(legacyOid))
            {
                alreadyImported++;
                continue;
            }

            if (!TryResolveRequiredIds(row, applicationIdMap, personIdMap, passportIdMap, out _, out _, out _, out var missingReason))
            {
                if (missingReason.StartsWith("Application ", StringComparison.Ordinal))
                    missingApplication++;
                else if (missingReason.StartsWith("Person ", StringComparison.Ordinal))
                    missingPerson++;
                else if (missingReason.StartsWith("Passport ", StringComparison.Ordinal))
                    missingPassport++;
                else
                    missingLegacyField++;
                continue;
            }

            readyToPost++;
        }

        var missingRequired = missingApplication + missingPerson + missingPassport + missingLegacyField;
        return new ApplicationItemImportGap(
            alreadyImported,
            missingRequired,
            missingApplication,
            missingPerson,
            missingPassport,
            missingLegacyField,
            readyToPost);
    }

    private static bool TryResolveRequiredIds(
        Dictionary<string, object?> row,
        IReadOnlyDictionary<Guid, Guid> applicationIdMap,
        IReadOnlyDictionary<Guid, Guid> personIdMap,
        IReadOnlyDictionary<Guid, Guid> passportIdMap,
        out Guid applicationId,
        out Guid personId,
        out Guid passportId,
        out string missingReason)
    {
        applicationId = Guid.Empty;
        personId = Guid.Empty;
        passportId = Guid.Empty;
        missingReason = "";

        if (!TryParseLegacyGuid(row, "Application", out var legacyApplicationOid))
        {
            missingReason = "missing legacy Application Oid on row";
            return false;
        }

        if (!applicationIdMap.TryGetValue(legacyApplicationOid, out applicationId))
        {
            missingReason = $"Application {legacyApplicationOid} not in id-map";
            return false;
        }

        if (!TryParseLegacyGuid(row, "Person", out var legacyPersonOid))
        {
            missingReason = "missing legacy Person Oid on row";
            return false;
        }

        if (!personIdMap.TryGetValue(legacyPersonOid, out personId))
        {
            missingReason = $"Person {legacyPersonOid} not in id-map";
            return false;
        }

        if (!TryParseLegacyGuid(row, "CurrentPassport", out var legacyPassportOid))
        {
            missingReason = "missing legacy CurrentPassport Oid on row";
            return false;
        }

        if (!passportIdMap.TryGetValue(legacyPassportOid, out passportId))
        {
            missingReason = $"Passport {legacyPassportOid} not in id-map";
            return false;
        }

        return true;
    }

    private static bool TryParseLegacyGuid(Dictionary<string, object?> row, string fieldName, out Guid legacyOid)
    {
        legacyOid = Guid.Empty;
        var text = row.GetValueOrDefault(fieldName) as string;
        return !string.IsNullOrWhiteSpace(text) && Guid.TryParse(text, out legacyOid);
    }

    private static Dictionary<string, object?>? BuildPayload(
        Dictionary<string, object?> row,
        Visa2014ODataLookupResolver resolver,
        Guid applicationId,
        Guid personId,
        Guid passportId,
        IReadOnlyDictionary<Guid, Guid> passportIdMap,
        IReadOnlyDictionary<Guid, Guid> visaIdMap,
        IReadOnlyDictionary<Guid, Guid> positionHistoryIdMap,
        IReadOnlyDictionary<Guid, Guid> addressIdMap,
        IReadOnlyDictionary<Guid, Guid> educationIdMap,
        IReadOnlyDictionary<Guid, Guid> employeeSalaryIdMap,
        IReadOnlyDictionary<Guid, Guid> workPermitItemIdMap,
        IReadOnlyDictionary<Guid, Guid> invitationItemIdMap)
    {
        var payload = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["SuppressPersonCurrentFieldSync"] = true,
            ["Application"] = new { ID = applicationId },
            ["Person"] = new { ID = personId },
            ["CurrentPassport"] = new { ID = passportId },
        };

        TryAddOptionalFkFromMap(payload, row, "PreviousPassport", "PreviousPassport", passportIdMap);
        TryAddOptionalFkFromMap(payload, row, "CurrentVisa", "CurrentVisa", visaIdMap);
        TryAddOptionalFkFromMap(payload, row, "NextVisa", "NextVisa", visaIdMap);
        TryAddOptionalFkFromMap(payload, row, "CurrentPositionHistory", "CurrentPositionHistory", positionHistoryIdMap);
        TryAddOptionalFkFromMap(payload, row, "CurrentAddressOfResidence", "CurrentAddressOfResidence", addressIdMap);
        TryAddOptionalFkFromMap(payload, row, "CurrentEducation", "CurrentEducation", educationIdMap);
        TryAddOptionalFkFromMap(payload, row, "CurrentSalary", "CurrentSalary", employeeSalaryIdMap);
        TryAddOptionalFkFromMap(payload, row, "CurrentWorkPermitItem", "CurrentWorkPermitItem", workPermitItemIdMap);
        TryAddOptionalFkFromMap(payload, row, "CurrentInvitationItem", "CurrentInvitationItem", invitationItemIdMap);

        if (TryParseDate(row.GetValueOrDefault("RegistrationDate") as string, out var registrationDate))
            payload["RegistrationDate"] = DateTime.SpecifyKind(registrationDate, DateTimeKind.Utc);

        if (TryParseDate(row.GetValueOrDefault("TravelDate") as string, out var travelDate))
            payload["TravelDate"] = DateTime.SpecifyKind(travelDate, DateTimeKind.Utc);

        if (row.GetValueOrDefault("TravelType") is string travelType &&
            Enum.TryParse<TravelType>(travelType, ignoreCase: true, out var parsedTravelType))
        {
            payload["TravelType"] = parsedTravelType.ToString();
        }

        if (row.GetValueOrDefault("MovementType") is string movementType &&
            Enum.TryParse<MovementType>(movementType, ignoreCase: true, out var parsedMovementType))
        {
            payload["MovementType"] = parsedMovementType.ToString();
        }

        var checkPointLabel = row.GetValueOrDefault("CheckPoint") as string;
        if (!string.IsNullOrWhiteSpace(checkPointLabel))
        {
            var checkPointId = resolver.ResolveCheckPoint(checkPointLabel.Trim());
            if (!checkPointId.HasValue)
                return null;

            payload["CheckPoint"] = new { ID = checkPointId.Value };
        }

        var borderZoneLocation = row.GetValueOrDefault("BorderZoneLocation") as string;
        if (!string.IsNullOrWhiteSpace(borderZoneLocation))
            payload["BorderZoneLocation"] = borderZoneLocation.Trim();

        var workPermittedLocations = row.GetValueOrDefault("WorkPermittedLocations") as string;
        if (!string.IsNullOrWhiteSpace(workPermittedLocations))
            payload["WorkPermittedLocations"] = workPermittedLocations.Trim();

        payload["IsCancelled"] = row.GetValueOrDefault("IsCancelled") is bool cancelled && cancelled;
        payload["InvitationItemIsCancelled"] =
            row.GetValueOrDefault("InvitationItemIsCancelled") is bool invitationCancelled && invitationCancelled;
        payload["VisaIsCancelled"] = row.GetValueOrDefault("VisaIsCancelled") is bool visaCancelled && visaCancelled;
        payload["RejectionIssued"] = row.GetValueOrDefault("RejectionIssued") is bool rejected && rejected;
        payload["VisaIssued"] = row.GetValueOrDefault("VisaIssued") is bool visaIssued && visaIssued;

        var businessTripAddress = row.GetValueOrDefault("BusinessTripAddress") as string;
        var businessTripCity = row.GetValueOrDefault("BusinessTripCity") as string;
        if (!string.IsNullOrWhiteSpace(businessTripAddress) && !string.IsNullOrWhiteSpace(businessTripCity))
        {
            var cityId = resolver.ResolveCity(businessTripCity.Trim());
            if (!cityId.HasValue)
                return null;

            payload["BusinessTripAddress"] = new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["City"] = new { ID = cityId.Value },
                ["FullAddress"] = businessTripAddress.Trim(),
            };
        }

        return payload;
    }

    private static void TryAddOptionalFkFromMap(
        Dictionary<string, object?> payload,
        Dictionary<string, object?> row,
        string rowField,
        string payloadField,
        IReadOnlyDictionary<Guid, Guid> idMap)
    {
        if (!TryParseLegacyGuid(row, rowField, out var legacyOid))
            return;

        if (idMap.TryGetValue(legacyOid, out var targetId))
            payload[payloadField] = new { ID = targetId };
    }

    private static bool TryParseDate(string? text, out DateTime date) =>
        DateTime.TryParse(text, out date);

    private static string DescribePayloadGap(Dictionary<string, object?> row, Visa2014ODataLookupResolver resolver)
    {
        var gaps = new List<string>();

        var checkPointLabel = row.GetValueOrDefault("CheckPoint") as string;
        if (!string.IsNullOrWhiteSpace(checkPointLabel) && !resolver.ResolveCheckPoint(checkPointLabel).HasValue)
            gaps.Add($"CheckPoint={checkPointLabel}");

        var businessTripAddress = row.GetValueOrDefault("BusinessTripAddress") as string;
        var businessTripCity = row.GetValueOrDefault("BusinessTripCity") as string;
        if (!string.IsNullOrWhiteSpace(businessTripAddress) && !string.IsNullOrWhiteSpace(businessTripCity) &&
            !resolver.ResolveCity(businessTripCity.Trim()).HasValue)
        {
            gaps.Add($"BusinessTripCity={businessTripCity}");
        }

        return gaps.Count > 0 ? string.Join("; ", gaps) : "lookup or required field";
    }

    private static Dictionary<Guid, Guid> LoadOptionalIdMap(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            return new Dictionary<Guid, Guid>();

        return Visa2014IdMapHelper.Load(path);
    }

    public static async Task<Visa2014SyncEntityResult> RunSyncAsync(
        IVisa2014ImportTarget target,
        Visa2014ODataLookupResolver resolver,
        string legacyConnectionString,
        IReadOnlyList<string> lookupTranslationPaths,
        string applicationIdMapPath,
        string personIdMapPath,
        string passportIdMapPath,
        string visaIdMapPath,
        string employeePositionHistoryIdMapPath,
        string addressOfResidenceIdMapPath,
        string educationIdMapPath,
        string employeeSalaryIdMapPath,
        string? workPermitItemIdMapPath,
        string? invitationItemIdMapPath,
        Visa2014SyncContext sync,
        int? maxRows,
        bool verbose)
    {
        var applicationIdMap = Visa2014IdMapHelper.Load(applicationIdMapPath);
        var personIdMap = Visa2014IdMapHelper.Load(personIdMapPath);
        var passportIdMap = Visa2014IdMapHelper.Load(passportIdMapPath);
        var visaIdMap = LoadOptionalIdMap(visaIdMapPath);
        var positionHistoryIdMap = LoadOptionalIdMap(employeePositionHistoryIdMapPath);
        var addressIdMap = LoadOptionalIdMap(addressOfResidenceIdMapPath);
        var educationIdMap = LoadOptionalIdMap(educationIdMapPath);
        var employeeSalaryIdMap = LoadOptionalIdMap(employeeSalaryIdMapPath);
        var workPermitItemIdMap = LoadOptionalIdMap(workPermitItemIdMapPath);
        var invitationItemIdMap = LoadOptionalIdMap(invitationItemIdMapPath);

        var applicationIdMapCollisions = Visa2014ApplicationTransform.FindApplicationIdMapCrossDateCollisions(
            applicationIdMap,
            legacyConnectionString,
            lookupTranslationPaths);
        if (applicationIdMapCollisions.Count > 0)
        {
            return new Visa2014SyncEntityResult
            {
                LegacyRowCount = 0,
                FailedCount = applicationIdMapCollisions.Count,
                Errors = applicationIdMapCollisions,
            };
        }

        if (verbose)
        {
            Console.WriteLine($"INF Application id-map entries: {applicationIdMap.Count}");
            Console.WriteLine($"INF Person id-map entries: {personIdMap.Count}");
            Console.WriteLine($"INF Passport id-map entries: {passportIdMap.Count}");
            Console.WriteLine($"INF Visa id-map entries: {visaIdMap.Count}");
            Console.WriteLine($"INF EmployeePositionHistory id-map entries: {positionHistoryIdMap.Count}");
            Console.WriteLine($"INF AddressOfResidence id-map entries: {addressIdMap.Count}");
            Console.WriteLine($"INF Education id-map entries: {educationIdMap.Count}");
            Console.WriteLine($"INF EmployeeSalary id-map entries: {employeeSalaryIdMap.Count}");
            Console.WriteLine($"INF WorkPermitItem id-map entries: {workPermitItemIdMap.Count}");
            Console.WriteLine($"INF InvitationItem id-map entries: {invitationItemIdMap.Count}");
        }

        var batch = Visa2014ApplicationItemTransform.PrepareImportBatch(
            legacyConnectionString,
            lookupTranslationPaths,
            maxRows,
            verbose);

        return await Visa2014SyncUpsertHelper.RunAsync(
            target,
            typeof(Visa2026.Module.BusinessObjects.ApplicationItem),
            "ApplicationItem",
            batch.ImportRows,
            sync,
            row => BuildSyncPayload(
                row,
                resolver,
                applicationIdMap,
                personIdMap,
                passportIdMap,
                visaIdMap,
                positionHistoryIdMap,
                addressIdMap,
                educationIdMap,
                employeeSalaryIdMap,
                workPermitItemIdMap,
                invitationItemIdMap,
                sync.IdMap),
            batch.LegacyRowCount,
            batch.Skipped.Count,
            batch.DedupeMergedCount,
            verbose);
    }

    private static Dictionary<string, object?>? BuildSyncPayload(
        Dictionary<string, object?> row,
        Visa2014ODataLookupResolver resolver,
        IReadOnlyDictionary<Guid, Guid> applicationIdMap,
        IReadOnlyDictionary<Guid, Guid> personIdMap,
        IReadOnlyDictionary<Guid, Guid> passportIdMap,
        IReadOnlyDictionary<Guid, Guid> visaIdMap,
        IReadOnlyDictionary<Guid, Guid> positionHistoryIdMap,
        IReadOnlyDictionary<Guid, Guid> addressIdMap,
        IReadOnlyDictionary<Guid, Guid> educationIdMap,
        IReadOnlyDictionary<Guid, Guid> employeeSalaryIdMap,
        IReadOnlyDictionary<Guid, Guid> workPermitItemIdMap,
        IReadOnlyDictionary<Guid, Guid> invitationItemIdMap,
        IReadOnlyDictionary<Guid, Guid> applicationItemIdMap)
    {
        var legacyOid = (Guid)row["_legacyRowId"]!;
        var isUpdate = applicationItemIdMap.ContainsKey(legacyOid);

        if (!isUpdate)
        {
            if (!TryResolveRequiredIds(
                    row,
                    applicationIdMap,
                    personIdMap,
                    passportIdMap,
                    out var applicationId,
                    out var personId,
                    out var passportId,
                    out _))
                return null;

            return BuildPayload(
                row,
                resolver,
                applicationId,
                personId,
                passportId,
                passportIdMap,
                visaIdMap,
                positionHistoryIdMap,
                addressIdMap,
                educationIdMap,
                employeeSalaryIdMap,
                workPermitItemIdMap,
                invitationItemIdMap);
        }

        if (TryResolveRequiredIds(
                row,
                applicationIdMap,
                personIdMap,
                passportIdMap,
                out var updateApplicationId,
                out var updatePersonId,
                out var updatePassportId,
                out _))
        {
            return BuildPayload(
                row,
                resolver,
                updateApplicationId,
                updatePersonId,
                updatePassportId,
                passportIdMap,
                visaIdMap,
                positionHistoryIdMap,
                addressIdMap,
                educationIdMap,
                employeeSalaryIdMap,
                workPermitItemIdMap,
                invitationItemIdMap);
        }

        return BuildPayloadWithoutRequiredParents(
            row,
            resolver,
            passportIdMap,
            visaIdMap,
            positionHistoryIdMap,
            addressIdMap,
            educationIdMap,
            employeeSalaryIdMap,
            workPermitItemIdMap,
            invitationItemIdMap);
    }

    private static Dictionary<string, object?>? BuildPayloadWithoutRequiredParents(
        Dictionary<string, object?> row,
        Visa2014ODataLookupResolver resolver,
        IReadOnlyDictionary<Guid, Guid> passportIdMap,
        IReadOnlyDictionary<Guid, Guid> visaIdMap,
        IReadOnlyDictionary<Guid, Guid> positionHistoryIdMap,
        IReadOnlyDictionary<Guid, Guid> addressIdMap,
        IReadOnlyDictionary<Guid, Guid> educationIdMap,
        IReadOnlyDictionary<Guid, Guid> employeeSalaryIdMap,
        IReadOnlyDictionary<Guid, Guid> workPermitItemIdMap,
        IReadOnlyDictionary<Guid, Guid> invitationItemIdMap)
    {
        var payload = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["SuppressPersonCurrentFieldSync"] = true,
        };

        TryAddOptionalFkFromMap(payload, row, "PreviousPassport", "PreviousPassport", passportIdMap);
        TryAddOptionalFkFromMap(payload, row, "CurrentVisa", "CurrentVisa", visaIdMap);
        TryAddOptionalFkFromMap(payload, row, "NextVisa", "NextVisa", visaIdMap);
        TryAddOptionalFkFromMap(payload, row, "CurrentPositionHistory", "CurrentPositionHistory", positionHistoryIdMap);
        TryAddOptionalFkFromMap(payload, row, "CurrentAddressOfResidence", "CurrentAddressOfResidence", addressIdMap);
        TryAddOptionalFkFromMap(payload, row, "CurrentEducation", "CurrentEducation", educationIdMap);
        TryAddOptionalFkFromMap(payload, row, "CurrentSalary", "CurrentSalary", employeeSalaryIdMap);
        TryAddOptionalFkFromMap(payload, row, "CurrentWorkPermitItem", "CurrentWorkPermitItem", workPermitItemIdMap);
        TryAddOptionalFkFromMap(payload, row, "CurrentInvitationItem", "CurrentInvitationItem", invitationItemIdMap);

        if (TryParseDate(row.GetValueOrDefault("RegistrationDate") as string, out var registrationDate))
            payload["RegistrationDate"] = DateTime.SpecifyKind(registrationDate, DateTimeKind.Utc);

        if (TryParseDate(row.GetValueOrDefault("TravelDate") as string, out var travelDate))
            payload["TravelDate"] = DateTime.SpecifyKind(travelDate, DateTimeKind.Utc);

        if (row.GetValueOrDefault("TravelType") is string travelType &&
            Enum.TryParse<TravelType>(travelType, ignoreCase: true, out var parsedTravelType))
        {
            payload["TravelType"] = parsedTravelType.ToString();
        }

        if (row.GetValueOrDefault("MovementType") is string movementType &&
            Enum.TryParse<MovementType>(movementType, ignoreCase: true, out var parsedMovementType))
        {
            payload["MovementType"] = parsedMovementType.ToString();
        }

        var checkPointLabel = row.GetValueOrDefault("CheckPoint") as string;
        if (!string.IsNullOrWhiteSpace(checkPointLabel))
        {
            var checkPointId = resolver.ResolveCheckPoint(checkPointLabel.Trim());
            if (!checkPointId.HasValue)
                return null;

            payload["CheckPoint"] = new { ID = checkPointId.Value };
        }

        var borderZoneLocation = row.GetValueOrDefault("BorderZoneLocation") as string;
        if (!string.IsNullOrWhiteSpace(borderZoneLocation))
            payload["BorderZoneLocation"] = borderZoneLocation.Trim();

        var workPermittedLocations = row.GetValueOrDefault("WorkPermittedLocations") as string;
        if (!string.IsNullOrWhiteSpace(workPermittedLocations))
            payload["WorkPermittedLocations"] = workPermittedLocations.Trim();

        payload["IsCancelled"] = row.GetValueOrDefault("IsCancelled") is bool cancelled && cancelled;
        payload["InvitationItemIsCancelled"] =
            row.GetValueOrDefault("InvitationItemIsCancelled") is bool invitationCancelled && invitationCancelled;
        payload["VisaIsCancelled"] = row.GetValueOrDefault("VisaIsCancelled") is bool visaCancelled && visaCancelled;
        payload["RejectionIssued"] = row.GetValueOrDefault("RejectionIssued") is bool rejected && rejected;
        payload["VisaIssued"] = row.GetValueOrDefault("VisaIssued") is bool visaIssued && visaIssued;

        var businessTripAddress = row.GetValueOrDefault("BusinessTripAddress") as string;
        var businessTripCity = row.GetValueOrDefault("BusinessTripCity") as string;
        if (!string.IsNullOrWhiteSpace(businessTripAddress) && !string.IsNullOrWhiteSpace(businessTripCity))
        {
            var cityId = resolver.ResolveCity(businessTripCity.Trim());
            if (!cityId.HasValue)
                return null;

            payload["BusinessTripAddress"] = new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["City"] = new { ID = cityId.Value },
                ["FullAddress"] = businessTripAddress.Trim(),
            };
        }

        return payload;
    }
}
