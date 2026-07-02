using DevExpress.ExpressApp;
using Visa2026.Blazor.Server.Services.Migration;
using Bo = Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.MigrationImport;

namespace Visa2026.DataImporter.Legacy.Visa2014;

internal sealed class Visa2014PersonRelationshipCorrectionResult
{
    public int FamilyMembersInScope { get; init; }
    public int Updated { get; init; }
    public int Unchanged { get; init; }
    public int Unresolved { get; init; }
    public int NoLegacyRelationship { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = [];
}

internal static class Visa2014PersonRelationshipCorrection
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

        Console.WriteLine("=== VISA2014 Person Relationship correction");
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
            var personIdMap = File.Exists(personIdMapPath)
                ? Visa2014IdMapHelper.Load(personIdMapPath)
                : new Dictionary<Guid, Guid>();

            var result = await RunAsync(
                host.ObjectSpaceFactory,
                resolver,
                source.ConnectionString,
                source.LookupTranslationPaths,
                personIdMap,
                dryRun,
                verbose);

            Console.WriteLine($"INF Family members in scope: {result.FamilyMembersInScope}");
            Console.WriteLine($"INF Relationship updated: {result.Updated}");
            Console.WriteLine($"INF Unchanged: {result.Unchanged}");
            Console.WriteLine($"INF No legacy Relationship FK: {result.NoLegacyRelationship}");
            Console.WriteLine($"INF Unresolved: {result.Unresolved}");
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

    internal static Task<Visa2014PersonRelationshipCorrectionResult> RunAsync(
        INonSecuredObjectSpaceFactory objectSpaceFactory,
        Visa2014ODataLookupResolver resolver,
        string legacyConnectionString,
        IReadOnlyList<string> lookupTranslationPaths,
        IReadOnlyDictionary<Guid, Guid> personIdMap,
        bool dryRun,
        bool verbose)
    {
        var errors = new List<string>();
        if (personIdMap.Count == 0)
        {
            return Task.FromResult(new Visa2014PersonRelationshipCorrectionResult
            {
                Errors = ["Person id-map is empty — import Person first."],
            });
        }

        var catalogs = Visa2014LookupTranslator.Load(lookupTranslationPaths);
        var legacyRelationships = LoadLegacyRelationships(legacyConnectionString, personIdMap.Keys, verbose);

        int familyInScope = 0, updated = 0, unchanged = 0, unresolved = 0, noLegacy = 0;

        using var objectSpace = objectSpaceFactory.CreateNonSecuredObjectSpace(typeof(Bo.Person));
        MigrationImportContext.ApplyImportObjectSpaceHooks(objectSpace);

        foreach (var (legacyOid, targetId) in personIdMap)
        {
            if (!legacyRelationships.TryGetValue(legacyOid, out var legacy) || !legacy.IsFamilyMember)
                continue;

            familyInScope++;

            if (string.IsNullOrWhiteSpace(legacy.LegacyRelationship))
            {
                noLegacy++;
                continue;
            }

            if (!Visa2014LookupTranslator.TryTranslate(
                    catalogs, "Relationship", legacy.LegacyRelationship, out var translatedName, out _))
            {
                translatedName = legacy.LegacyRelationship;
            }

            var relationshipId = resolver.ResolveRelationship(translatedName);
            if (!relationshipId.HasValue)
            {
                unresolved++;
                if (verbose)
                    Console.WriteLine($"WRN {legacyOid}: unresolved relationship '{legacy.LegacyRelationship}' -> '{translatedName}'");
                continue;
            }

            var person = objectSpace.GetObjectByKey<Bo.Person>(targetId);
            if (person == null)
            {
                errors.Add($"{legacyOid}: target Person {targetId} not found");
                continue;
            }

            if (person.PersonRole != Bo.PersonRecordRole.FamilyMember)
                continue;

            if (person.Relationship?.ID == relationshipId)
            {
                unchanged++;
                continue;
            }

            var relationship = objectSpace.GetObjectByKey<Bo.Relationship>(relationshipId);
            if (relationship == null)
            {
                errors.Add($"{legacyOid}: Relationship {relationshipId} not found");
                continue;
            }

            if (!dryRun)
                person.Relationship = relationship;
            updated++;
        }

        if (!dryRun && updated > 0)
            objectSpace.CommitChanges();

        return Task.FromResult(new Visa2014PersonRelationshipCorrectionResult
        {
            FamilyMembersInScope = familyInScope,
            Updated = updated,
            Unchanged = unchanged,
            Unresolved = unresolved,
            NoLegacyRelationship = noLegacy,
            Errors = errors,
        });
    }

    private sealed record LegacyRelationshipRow(bool IsFamilyMember, string? LegacyRelationship);

    private static Dictionary<Guid, LegacyRelationshipRow> LoadLegacyRelationships(
        string legacyConnectionString,
        IEnumerable<Guid> legacyOids,
        bool verbose)
    {
        var oidList = legacyOids.Distinct().ToList();
        if (oidList.Count == 0)
            return new Dictionary<Guid, LegacyRelationshipRow>();

        var inClause = string.Join(",", oidList.Select(o => $"'{o}'"));
        var sql = $"""
            SELECT
                CAST(p.Oid AS varchar(36)) AS Oid,
                CASE WHEN p.IsFamilyMember = 1 THEN '1' ELSE '0' END AS IsFamilyMember,
                NULLIF(LTRIM(RTRIM(rel.RelativeAsL)), '') AS LegacyRelationship
            FROM dbo.Person p
            LEFT JOIN dbo.Relation rel ON p.FamilyMemberRelation = rel.Oid
            WHERE p.GCRecord IS NULL AND p.Oid IN ({inClause})
            """;

        if (verbose)
            Console.WriteLine($"INF Loading legacy relationships for {oidList.Count} person(s)...");

        var rows = Visa2014SqlCmdReader.Query(legacyConnectionString, sql, verbose: false);
        var map = new Dictionary<Guid, LegacyRelationshipRow>();
        foreach (var row in rows)
        {
            if (!Guid.TryParse(row.GetValueOrDefault("Oid"), out var legacyOid))
                continue;

            var isFamilyMember = row.GetValueOrDefault("IsFamilyMember") == "1";
            var relationship = row.GetValueOrDefault("LegacyRelationship");
            map[legacyOid] = new LegacyRelationshipRow(isFamilyMember, relationship);
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