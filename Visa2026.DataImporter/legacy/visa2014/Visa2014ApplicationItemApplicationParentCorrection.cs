using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Core;
using Visa2026.Blazor.Server.Services.Migration;
using Bo = Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.MigrationImport;

namespace Visa2026.DataImporter.Legacy.Visa2014;

internal sealed class Visa2014ApplicationItemApplicationParentCorrectionResult
{
    public int ItemsInScope { get; init; }
    public int Reparented { get; init; }
    public int AlreadyCorrect { get; init; }
    public int MissingApplicationMap { get; init; }
    public int MissingTransformRow { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = [];
}

/// <summary>
/// Reparents imported <see cref="Bo.ApplicationItem"/> rows when Application id-map was rebuilt
/// with FullApplicationNumber + ApplicationDate (legacy Application.Oid is authoritative).
/// </summary>
internal static class Visa2014ApplicationItemApplicationParentCorrection
{
    public static Task<int> RunCommandAsync(IReadOnlyList<string> args, bool verbose)
    {
        var dataImporterRoot = Visa2014ContentRoot.FindDataImporterRoot();
        if (dataImporterRoot == null)
        {
            Console.Error.WriteLine("ERR Could not locate Visa2026.DataImporter content root.");
            return Task.FromResult(1);
        }

        var solutionRoot = Visa2014ContentRoot.FindSolutionRoot();
        Visa2014LegacySourceProfile source;
        try { source = Visa2014LegacySource.Resolve(dataImporterRoot, solutionRoot, args); }
        catch (Exception ex) { Console.Error.WriteLine($"ERR {ex.Message}"); return Task.FromResult(1); }

        var dryRun = HasArg(args, "--dry-run");
        var targetConnection = GetOptionValue(args, "--target-connection")
            ?? Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? "Host=localhost;Port=5432;Database=visa2026;Username=postgres;Password=Visa2026Local;Persist Security Info=True;EFCoreProvider=Postgres";

        var applicationIdMapPath = GetOptionValue(args, "--application-id-map")
            ?? source.IdMapPath(dataImporterRoot, "Application");
        var applicationItemIdMapPath = GetOptionValue(args, "--application-item-id-map")
            ?? source.IdMapPath(dataImporterRoot, "ApplicationItem");

        Console.WriteLine("=== VISA2014 ApplicationItem Application parent correction");
        Console.WriteLine($"INF Legacy source: {source.Id}");
        Console.WriteLine($"INF Target SQL: {MaskConnectionString(targetConnection)}");
        Console.WriteLine($"INF Application id-map: {applicationIdMapPath}");
        Console.WriteLine($"INF ApplicationItem id-map: {applicationItemIdMapPath}");
        if (dryRun) Console.WriteLine("INF Mode: dry-run (no writes)");

        if (!File.Exists(applicationIdMapPath))
        {
            Console.Error.WriteLine($"ERR Application id-map not found: {applicationIdMapPath}");
            return Task.FromResult(1);
        }

        if (!File.Exists(applicationItemIdMapPath))
        {
            Console.Error.WriteLine($"ERR ApplicationItem id-map not found: {applicationItemIdMapPath}");
            return Task.FromResult(1);
        }

        try
        {
            Visa2014LegacySqlGuard.EnsureLegacyReadCredentials(source.ConnectionString);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ERR {ex.Message}");
            return Task.FromResult(1);
        }

        var applicationIdMap = Visa2014IdMapHelper.Load(applicationIdMapPath);
        var applicationItemIdMap = Visa2014IdMapHelper.Load(applicationItemIdMapPath);
        var collisions = Visa2014ApplicationTransform.FindApplicationIdMapCrossDateCollisions(
            applicationIdMap,
            source.ConnectionString,
            source.LookupTranslationPaths);
        if (collisions.Count > 0)
        {
            Console.Error.WriteLine(
                $"ERR Application id-map still has {collisions.Count} cross-date collision(s). " +
                "Rebuild with --rebuild-visa2014-id-maps --entity Application first.");
            foreach (var collision in collisions.Take(10))
                Console.Error.WriteLine($"ERR   {collision}");
            return Task.FromResult(1);
        }

        HeadlessMigrationHost? host = null;
        IDisposable? importScope = null;
        try
        {
            host = HeadlessMigrationHost.Start(targetConnection);
            importScope = MigrationImportContext.BeginDataImportScope();

            var piaToLegacyApplication = BuildLegacyPiaToApplicationMap(source.ConnectionString);
            var result = Run(
                host.ObjectSpaceFactory,
                applicationIdMap,
                applicationItemIdMap,
                piaToLegacyApplication,
                dryRun,
                verbose);

            Console.WriteLine($"INF Items in scope: {result.ItemsInScope}");
            Console.WriteLine($"INF Reparented: {result.Reparented}");
            Console.WriteLine($"INF Already correct: {result.AlreadyCorrect}");
            Console.WriteLine($"INF Missing Application id-map: {result.MissingApplicationMap}");
            Console.WriteLine($"INF Missing legacy PIA row: {result.MissingTransformRow}");
            foreach (var error in result.Errors.Take(20))
                Console.Error.WriteLine($"ERR {error}");

            return Task.FromResult(result.Errors.Count > 0 ? 1 : 0);
        }
        finally
        {
            importScope?.Dispose();
            host?.Dispose();
        }
    }

    private static Dictionary<Guid, Guid> BuildLegacyPiaToApplicationMap(string connectionString)
    {
        const string sql = """
            SELECT CAST(Oid AS varchar(36)) AS Oid, CAST(Application AS varchar(36)) AS ApplicationOid
            FROM dbo.PersonInApplication
            WHERE GCRecord IS NULL
            """;

        var map = new Dictionary<Guid, Guid>();
        foreach (var row in Visa2014SqlCmdReader.Query(connectionString, sql, verbose: false))
        {
            if (Guid.TryParse(row.GetValueOrDefault("Oid"), out var piaOid)
                && Guid.TryParse(row.GetValueOrDefault("ApplicationOid"), out var applicationOid))
                map[piaOid] = applicationOid;
        }

        return map;
    }

    private static Visa2014ApplicationItemApplicationParentCorrectionResult Run(
        INonSecuredObjectSpaceFactory objectSpaceFactory,
        IReadOnlyDictionary<Guid, Guid> applicationIdMap,
        IReadOnlyDictionary<Guid, Guid> applicationItemIdMap,
        IReadOnlyDictionary<Guid, Guid> piaToLegacyApplication,
        bool dryRun,
        bool verbose)
    {
        var errors = new List<string>();
        int inScope = 0;
        int reparented = 0;
        int alreadyCorrect = 0;
        int missingApplicationMap = 0;
        int missingTransformRow = 0;

        using var objectSpace = objectSpaceFactory.CreateNonSecuredObjectSpace(typeof(Bo.ApplicationItem));
        MigrationImportContext.ApplyImportObjectSpaceHooks(objectSpace);

        foreach (var (legacyPiaOid, targetItemId) in applicationItemIdMap)
        {
            inScope++;

            if (!piaToLegacyApplication.TryGetValue(legacyPiaOid, out var legacyApplicationOid))
            {
                missingTransformRow++;
                continue;
            }

            if (!applicationIdMap.TryGetValue(legacyApplicationOid, out var correctApplicationId))
            {
                missingApplicationMap++;
                errors.Add($"PIA {legacyPiaOid:D}: legacy Application {legacyApplicationOid:D} not in id-map");
                continue;
            }

            var item = objectSpace.GetObjectByKey<Bo.ApplicationItem>(targetItemId);
            if (item == null)
            {
                errors.Add($"PIA {legacyPiaOid:D}: target ApplicationItem {targetItemId:D} not found");
                continue;
            }

            var currentApplicationId = item.Application?.ID ?? Guid.Empty;
            if (currentApplicationId == correctApplicationId)
            {
                alreadyCorrect++;
                continue;
            }

            var correctApplication = objectSpace.GetObjectByKey<Bo.Application>(correctApplicationId);
            if (correctApplication == null)
            {
                errors.Add($"PIA {legacyPiaOid:D}: target Application {correctApplicationId:D} not found");
                continue;
            }

            reparented++;
            if (verbose)
            {
                Console.WriteLine(
                    $"  REparent ApplicationItem {targetItemId:D}: " +
                    $"{currentApplicationId:D} -> {correctApplicationId:D} (PIA {legacyPiaOid:D})");
            }

            if (!dryRun)
                item.Application = correctApplication;
        }

        if (!dryRun && reparented > 0)
            objectSpace.CommitChanges();

        return new Visa2014ApplicationItemApplicationParentCorrectionResult
        {
            ItemsInScope = inScope,
            Reparented = reparented,
            AlreadyCorrect = alreadyCorrect,
            MissingApplicationMap = missingApplicationMap,
            MissingTransformRow = missingTransformRow,
            Errors = errors,
        };
    }

    private static string MaskConnectionString(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            return "(empty)";
        return System.Text.RegularExpressions.Regex.Replace(
            connectionString,
            @"(Password|Pwd)\s*=\s*[^;]+",
            "$1=***",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    }

    private static bool HasArg(IReadOnlyList<string> args, string flag) =>
        args.Any(a => string.Equals(a, flag, StringComparison.OrdinalIgnoreCase));

    private static string? GetOptionValue(IReadOnlyList<string> args, string optionName)
    {
        for (int i = 0; i < args.Count - 1; i++)
        {
            if (string.Equals(args[i], optionName, StringComparison.OrdinalIgnoreCase))
                return args[i + 1];
        }

        return null;
    }
}