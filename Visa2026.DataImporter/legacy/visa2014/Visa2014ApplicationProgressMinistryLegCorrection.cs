using System.Text.Json;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Core;
using Visa2026.Blazor.Server.Services.Migration;
using Bo = Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.MigrationImport;

namespace Visa2026.DataImporter.Legacy.Visa2014;

internal sealed class Visa2014ApplicationProgressMinistryLegCorrectionResult
{
    public int ApplicationsInScope { get; init; }
    public int ProgressDeleted { get; init; }
    public int ProgressPosted { get; init; }
    public int ProgressFailed { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = [];
}

internal static class Visa2014ApplicationProgressMinistryLegCorrection
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

        Console.WriteLine("=== VISA2014 ApplicationProgress ministry-leg correction");
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

            var target = new Visa2014ObjectSpaceImportTarget(host.ObjectSpaceFactory, batchSize: 50);
            var applicationIdMap = File.Exists(source.IdMapPath(dataImporterRoot, "Application"))
                ? Visa2014IdMapHelper.Load(source.IdMapPath(dataImporterRoot, "Application"))
                : new Dictionary<Guid, Guid>();

            var result = await RunAsync(
                host.ObjectSpaceFactory,
                target,
                resolver,
                source.ConnectionString,
                source.LookupTranslationPaths,
                applicationIdMap,
                source.IdMapPath(dataImporterRoot, "ApplicationProgress"),
                dryRun,
                verbose);

            Console.WriteLine($"INF Applications in scope: {result.ApplicationsInScope}");
            Console.WriteLine($"INF Progress deleted: {result.ProgressDeleted}");
            Console.WriteLine($"INF Progress posted: {result.ProgressPosted}");
            Console.WriteLine($"INF Progress failed: {result.ProgressFailed}");
            foreach (var error in result.Errors.Take(20))
                Console.Error.WriteLine($"ERR {error}");
            return result.Errors.Count > 0 || result.ProgressFailed > 0 ? 1 : 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ERR Correction failed: {ex.Message}");
            if (verbose) Console.Error.WriteLine(ex);
            return 1;
        }
        finally { importScope?.Dispose(); host?.Dispose(); }
    }

    internal static async Task<Visa2014ApplicationProgressMinistryLegCorrectionResult> RunAsync(
        INonSecuredObjectSpaceFactory objectSpaceFactory,
        IVisa2014ImportTarget target,
        Visa2014ODataLookupResolver resolver,
        string legacyConnectionString,
        IReadOnlyList<string> lookupTranslationPaths,
        IReadOnlyDictionary<Guid, Guid> applicationIdMap,
        string? progressIdMapPath,
        bool dryRun,
        bool verbose)
    {
        var errors = new List<string>();
        var targetLegCounts = Visa2014ApplicationMinistryLegCountResolver.LoadFromObjectSpace(objectSpaceFactory);
        var legacyLegCounts = Visa2014ApplicationMinistryLegCountResolver.MapLegacyLegCounts(applicationIdMap, targetLegCounts);
        var targetAppIds = Visa2014ApplicationMinistryLegCountResolver.ResolveTargetApplicationIdsInScope(
            applicationIdMap, targetLegCounts);

        if (verbose)
            Console.WriteLine($"INF Via-ministry applications with snapshots: {targetAppIds.Count}");

        var legacyToTarget = applicationIdMap
            .Where(kv => targetAppIds.Contains(kv.Value))
            .ToDictionary(kv => kv.Key, kv => kv.Value);

        int progressDeleted = 0;
        if (targetAppIds.Count > 0)
        {
            using var progressSpace = objectSpaceFactory.CreateNonSecuredObjectSpace(typeof(Bo.ApplicationProgress));
            MigrationImportContext.ApplyImportObjectSpaceHooks(progressSpace);
            var progresses = progressSpace.GetObjectsQuery<Bo.ApplicationProgress>()
                .Where(p => p.Application != null && targetAppIds.Contains(p.Application.ID))
                .ToList();
            progressDeleted = progresses.Count;
            if (!dryRun)
            {
                foreach (var progress in progresses)
                    progressSpace.Delete(progress);
                progressSpace.CommitChanges();
            }
        }

        var regen = await Visa2014ApplicationProgressODataImporter.RegenerateForLegacyApplicationsAsync(
            target,
            resolver,
            legacyConnectionString,
            lookupTranslationPaths,
            legacyToTarget,
            legacyLegCounts,
            dryRun,
            verbose);

        errors.AddRange(regen.Errors);
        if (!dryRun && regen.ProgressIdMapUpdates.Count > 0 && !string.IsNullOrWhiteSpace(progressIdMapPath))
            MergeProgressIdMap(progressIdMapPath, regen.ProgressIdMapUpdates);

        return new Visa2014ApplicationProgressMinistryLegCorrectionResult
        {
            ApplicationsInScope = targetAppIds.Count,
            ProgressDeleted = progressDeleted,
            ProgressPosted = regen.PostedCount,
            ProgressFailed = regen.FailedCount,
            Errors = errors,
        };
    }

    private static void MergeProgressIdMap(string path, IReadOnlyDictionary<string, Guid> updates)
    {
        var existing = File.Exists(path)
            ? JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(path)) ?? new Dictionary<string, string>(StringComparer.Ordinal)
            : new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var (key, value) in updates)
            existing[key] = value.ToString();
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        File.WriteAllText(path, JsonSerializer.Serialize(existing, new JsonSerializerOptions { WriteIndented = true }));
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
