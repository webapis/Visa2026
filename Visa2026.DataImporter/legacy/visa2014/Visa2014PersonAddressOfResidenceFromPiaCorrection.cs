using DevExpress.ExpressApp;
using Visa2026.Blazor.Server.Services.Migration;
using Bo = Visa2026.Module.BusinessObjects;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.MigrationImport;

namespace Visa2026.DataImporter.Legacy.Visa2014;

internal sealed class Visa2014PersonAddressOfResidenceFromPiaCorrectionResult
{
    public int EmployeePersonsInScope { get; init; }
    public int AddressesCreated { get; init; }
    public int AddressesSkippedExisting { get; init; }
    public int ApplicationItemsInScope { get; init; }
    public int ApplicationItemsUpdated { get; init; }
    public int ApplicationItemsUnchanged { get; init; }
    public int ApplicationItemsUnresolved { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = [];
}

internal static class Visa2014PersonAddressOfResidenceFromPiaCorrection
{
    public static async Task<int> RunCommandAsync(IReadOnlyList<string> args, bool verbose)
    {
        var dataImporterRoot = Visa2014ContentRoot.FindDataImporterRoot();
        if (dataImporterRoot == null)
        {
            Console.Error.WriteLine("ERR Could not locate Visa2026.DataImporter content root.");
            return 1;
        }

        var solutionRoot = Visa2014ContentRoot.FindSolutionRoot();
        Visa2014LegacySourceProfile source;
        try { source = Visa2014LegacySource.Resolve(dataImporterRoot, solutionRoot, args); }
        catch (Exception ex) { Console.Error.WriteLine($"ERR {ex.Message}"); return 1; }

        var dryRun = HasArg(args, "--dry-run");
        var targetConnection = GetOptionValue(args, "--target-connection")
            ?? Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? "Server=(localdb)\\mssqllocaldb;Database=Visa2026;Trusted_Connection=True;";

        Console.WriteLine("=== VISA2014 Person/ApplicationItem AddressOfResidence (PIA inference) correction");
        Console.WriteLine($"INF Legacy source: {source.Id}");
        Console.WriteLine($"INF Target SQL: {MaskConnectionString(targetConnection)}");
        if (dryRun) Console.WriteLine("INF Mode: dry-run (no writes)");

        HeadlessMigrationHost? host = null;
        IDisposable? importScope = null;
        try
        {
            host = HeadlessMigrationHost.Start(targetConnection);
            importScope = MigrationImportContext.BeginDataImportScope();

            var resolver = new Visa2014ODataLookupResolver();
            using (var lookupSpace = host.ObjectSpaceFactory.CreateNonSecuredObjectSpace(typeof(Bo.Person)))
            {
                MigrationImportContext.ApplyImportObjectSpaceHooks(lookupSpace);
                resolver.LoadFromObjectSpace(lookupSpace, Visa2014HeadlessImportSession.ResolveTenantCatalogDirStatic());
            }

            var personIdMapPath = source.IdMapPath(dataImporterRoot, "Person");
            var addressIdMapPath = source.IdMapPath(dataImporterRoot, "AddressOfResidence");
            var applicationItemIdMapPath = source.IdMapPath(dataImporterRoot, "ApplicationItem");

            var personIdMap = File.Exists(personIdMapPath)
                ? Visa2014IdMapHelper.Load(personIdMapPath)
                : new Dictionary<Guid, Guid>();

            var addressIdMap = File.Exists(addressIdMapPath)
                ? Visa2014IdMapHelper.Load(addressIdMapPath)
                : new Dictionary<Guid, Guid>();

            var applicationItemIdMap = File.Exists(applicationItemIdMapPath)
                ? Visa2014IdMapHelper.Load(applicationItemIdMapPath)
                : new Dictionary<Guid, Guid>();

            var result = await RunAsync(
                host.ObjectSpaceFactory,
                resolver,
                source.ConnectionString,
                source.LookupTranslationPaths,
                personIdMap,
                addressIdMap,
                applicationItemIdMap,
                dryRun,
                verbose);

            if (!dryRun && addressIdMap.Count > 0)
                await Visa2014IdMapHelper.SaveAsync(addressIdMapPath, addressIdMap);

            Console.WriteLine($"INF Employee persons in scope: {result.EmployeePersonsInScope}");
            Console.WriteLine($"INF AddressOfResidence created on Person: {result.AddressesCreated}");
            Console.WriteLine($"INF AddressOfResidence skipped (already on Person): {result.AddressesSkippedExisting}");
            Console.WriteLine($"INF ApplicationItems in scope: {result.ApplicationItemsInScope}");
            Console.WriteLine($"INF ApplicationItem CurrentAddressOfResidence updated: {result.ApplicationItemsUpdated}");
            Console.WriteLine($"INF ApplicationItem unchanged: {result.ApplicationItemsUnchanged}");
            Console.WriteLine($"INF ApplicationItem unresolved address: {result.ApplicationItemsUnresolved}");
            foreach (var error in result.Errors.Take(20))
                Console.Error.WriteLine($"ERR {error}");
            return result.Errors.Count > 0 ? 1 : 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ERR Correction failed: {ex.Message}");
            if (verbose) Console.Error.WriteLine(ex);
            return 1;
        }
        finally { importScope?.Dispose(); host?.Dispose(); }
    }

    internal static Task<Visa2014PersonAddressOfResidenceFromPiaCorrectionResult> RunAsync(
        INonSecuredObjectSpaceFactory objectSpaceFactory,
        Visa2014ODataLookupResolver resolver,
        string legacyConnectionString,
        IReadOnlyList<string> lookupTranslationPaths,
        IReadOnlyDictionary<Guid, Guid> personIdMap,
        Dictionary<Guid, Guid> addressIdMap,
        IReadOnlyDictionary<Guid, Guid> applicationItemIdMap,
        bool dryRun,
        bool verbose)
    {
        var errors = new List<string>();
        if (personIdMap.Count == 0)
        {
            return Task.FromResult(new Visa2014PersonAddressOfResidenceFromPiaCorrectionResult
            {
                Errors = ["Person id-map is empty — import Person first."],
            });
        }

        RegisterSponsorCanonicalFromExistingLegacyAorForCorrection(
            legacyConnectionString, personIdMap, addressIdMap, verbose);

        var batch = Visa2014PiaAddressInference.PrepareEmployeeInferredAddresses(
            legacyConnectionString, lookupTranslationPaths, verbose);

        int employeeInScope = batch.Plans.Count;
        int addressesCreated = 0;
        int addressesSkipped = 0;

        using var personSpace = objectSpaceFactory.CreateNonSecuredObjectSpace(typeof(Bo.Person));
        MigrationImportContext.ApplyImportObjectSpaceHooks(personSpace);

        foreach (var plan in batch.Plans)
        {
            if (!personIdMap.TryGetValue(plan.LegacyPersonOid, out var targetPersonId))
                continue;

            var person = personSpace.GetObjectByKey<Bo.Person>(targetPersonId);
            if (person == null)
            {
                errors.Add($"{plan.LegacyPersonOid}: target Person {targetPersonId} not found");
                continue;
            }

            if (person.AddressesOfResidence?.Any(a => a != null) == true)
            {
                addressesSkipped++;
                var existing = PersonCurrentItems.GetCurrentAddressOfResidence(person);
                if (existing != null)
                    Visa2014PiaAddressInference.RegisterPlanAliases(plan, existing.ID, addressIdMap);
                continue;
            }

            if (dryRun)
            {
                addressesCreated++;
                continue;
            }

            if (!Visa2014AddressOfResidenceImportApplier.TryCreateOnObjectSpace(
                    personSpace, person, plan.ImportRow, resolver, out var created) ||
                created == null)
            {
                errors.Add($"{plan.LegacyPersonOid}: could not create AddressOfResidence from PIA inference");
                continue;
            }

            if (!dryRun)
            {
                personSpace.CommitChanges();
                Visa2014PiaAddressInference.RegisterPlanAliases(plan, created.ID, addressIdMap);
            }

            addressesCreated++;
        }

        int itemsInScope = 0;
        int itemsUpdated = 0;
        int itemsUnchanged = 0;
        int itemsUnresolved = 0;

        if (applicationItemIdMap.Count > 0)
        {
            var legacyPiaRows = LoadLegacyApplicationItemRows(
                legacyConnectionString, applicationItemIdMap.Keys, verbose);

            using var itemSpace = objectSpaceFactory.CreateNonSecuredObjectSpace(typeof(Bo.ApplicationItem));
            MigrationImportContext.ApplyImportObjectSpaceHooks(itemSpace);

            foreach (var (legacyPiaOid, targetItemId) in applicationItemIdMap)
            {
                if (!legacyPiaRows.TryGetValue(legacyPiaOid, out var raw))
                    continue;

                var legacyAddressKey = Visa2014PiaAddressInference.ResolveApplicationItemCurrentAddressLegacyKey(raw);
                if (!legacyAddressKey.HasValue)
                    continue;

                itemsInScope++;

                if (!addressIdMap.TryGetValue(legacyAddressKey.Value, out var targetAddressId))
                {
                    itemsUnresolved++;
                    if (verbose)
                        Console.WriteLine($"WRN PIA {legacyPiaOid}: no id-map for address key {legacyAddressKey.Value}");
                    continue;
                }

                var item = itemSpace.GetObjectByKey<Bo.ApplicationItem>(targetItemId);
                if (item == null)
                {
                    errors.Add($"PIA {legacyPiaOid}: ApplicationItem {targetItemId} not found");
                    continue;
                }

                if (item.CurrentAddressOfResidence?.ID == targetAddressId)
                {
                    itemsUnchanged++;
                    continue;
                }

                var address = itemSpace.GetObjectByKey<Bo.AddressOfResidence>(targetAddressId);
                if (address == null)
                {
                    errors.Add($"PIA {legacyPiaOid}: AddressOfResidence {targetAddressId} not found");
                    continue;
                }

                if (!dryRun)
                    item.CurrentAddressOfResidence = address;
                itemsUpdated++;
            }

            if (!dryRun && itemsUpdated > 0)
                itemSpace.CommitChanges();
        }

        return Task.FromResult(new Visa2014PersonAddressOfResidenceFromPiaCorrectionResult
        {
            EmployeePersonsInScope = employeeInScope,
            AddressesCreated = addressesCreated,
            AddressesSkippedExisting = addressesSkipped,
            ApplicationItemsInScope = itemsInScope,
            ApplicationItemsUpdated = itemsUpdated,
            ApplicationItemsUnchanged = itemsUnchanged,
            ApplicationItemsUnresolved = itemsUnresolved,
            Errors = errors,
        });
    }

    private static void RegisterSponsorCanonicalFromExistingLegacyAorForCorrection(
        string legacyConnectionString,
        IReadOnlyDictionary<Guid, Guid> personIdMap,
        IDictionary<Guid, Guid> addressIdMap,
        bool verbose)
    {
        const string sql = """
            SELECT
                CAST(aor.Person AS varchar(36)) AS PersonOid,
                CAST(aor.Oid AS varchar(36)) AS AorOid,
                CONVERT(varchar(10), addr.ExpiringDateOfAddressDocument, 23) AS ExpirationDate
            FROM dbo.AddressOfResidence aor
            INNER JOIN dbo.Address addr ON addr.Oid = aor.Address AND addr.GCRecord IS NULL
            WHERE aor.GCRecord IS NULL
            """;

        var rows = Visa2014SqlCmdReader.Query(legacyConnectionString, sql, verbose: false);
        var bestPerPerson = new Dictionary<Guid, (Guid AorOid, DateTime? Expiration)>();
        foreach (var row in rows)
        {
            if (!Guid.TryParse(row.GetValueOrDefault("PersonOid"), out var personOid))
                continue;
            if (!Guid.TryParse(row.GetValueOrDefault("AorOid"), out var aorOid))
                continue;
            if (!personIdMap.ContainsKey(personOid))
                continue;

            DateTime? expiration = DateTime.TryParse(row.GetValueOrDefault("ExpirationDate"), out var exp) ? exp : null;
            if (!bestPerPerson.TryGetValue(personOid, out var current) ||
                CompareAddressRecency(expiration, aorOid, current.Expiration, current.AorOid) > 0)
            {
                bestPerPerson[personOid] = (aorOid, expiration);
            }
        }

        int registered = 0;
        foreach (var (personOid, best) in bestPerPerson)
        {
            if (!addressIdMap.TryGetValue(best.AorOid, out var targetId))
                continue;

            var synthetic = Visa2014PiaAddressInference.PersonCanonicalSyntheticLegacyOid(personOid);
            if (addressIdMap.ContainsKey(synthetic))
                continue;

            addressIdMap[synthetic] = targetId;
            registered++;
        }

        if (verbose && registered > 0)
            Console.WriteLine($"INF Registered {registered} sponsor canonical alias(es) in id-map.");
    }

    private static int CompareAddressRecency(DateTime? expA, Guid oidA, DateTime? expB, Guid oidB)
    {
        var rankA = expA?.Date ?? DateTime.MaxValue;
        var rankB = expB?.Date ?? DateTime.MaxValue;
        var cmp = rankA.CompareTo(rankB);
        return cmp != 0 ? cmp : oidA.CompareTo(oidB);
    }

    private static Dictionary<Guid, Visa2014ApplicationItemRawRow> LoadLegacyApplicationItemRows(
        string legacyConnectionString,
        IEnumerable<Guid> legacyPiaOids,
        bool verbose)
    {
        var oidList = legacyPiaOids.Distinct().ToList();
        if (oidList.Count == 0)
            return new Dictionary<Guid, Visa2014ApplicationItemRawRow>();

        var inClause = string.Join(",", oidList.Select(o => $"'{o:D}'"));
        var sql = $"""
            SELECT q.*
            FROM ({Visa2014ApplicationItemTransform.ExtractSql}) AS q
            WHERE q.Oid IN ({inClause})
            """;

        if (verbose)
            Console.WriteLine($"INF Loading legacy PIA address fields for {oidList.Count} ApplicationItem row(s)...");

        var dictRows = Visa2014SqlCmdReader.Query(legacyConnectionString, sql, verbose: false);
        var map = new Dictionary<Guid, Visa2014ApplicationItemRawRow>();
        foreach (var dict in dictRows)
        {
            if (Visa2014ApplicationItemTransform.TryParseRawRow(dict, out var parsed))
                map[parsed.LegacyOid] = parsed;
        }

        return map;
    }

    private static string MaskConnectionString(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString)) return "(empty)";
        return string.Join("; ", connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(p => !p.StartsWith("Password=", StringComparison.OrdinalIgnoreCase) && !p.StartsWith("Pwd=", StringComparison.OrdinalIgnoreCase)));
    }

    private static bool HasArg(IReadOnlyList<string> args, string flag) =>
        args.Any(a => string.Equals(a, flag, StringComparison.OrdinalIgnoreCase));

    private static string? GetOptionValue(IReadOnlyList<string> args, string optionName)
    {
        for (var i = 0; i < args.Count - 1; i++)
            if (string.Equals(args[i], optionName, StringComparison.OrdinalIgnoreCase)) return args[i + 1];
        return null;
    }
}
