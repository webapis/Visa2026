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
    public int SnapshotsBackfilled { get; init; }
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
            Console.WriteLine($"INF Snapshots backfilled: {result.SnapshotsBackfilled}");
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
        var targetAppIds = Visa2014ApplicationMinistryLegCountResolver.ResolveTargetApplicationIdsMissingMinistryProgress(
            objectSpaceFactory, targetLegCounts);
        var legacyLegCounts = Visa2014ApplicationMinistryLegCountResolver.MapLegacyLegCounts(applicationIdMap, targetLegCounts);

        if (verbose)
            Console.WriteLine($"INF Via-ministry apps missing ministry progress rows: {targetAppIds.Count}");

        var legacyToTarget = applicationIdMap
            .Where(kv => targetAppIds.Contains(kv.Value))
            .ToDictionary(kv => kv.Key, kv => kv.Value);

        var snapshotsBackfilled = BackfillApprovalLegSnapshots(
            objectSpaceFactory, targetAppIds, dryRun, verbose);

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

        if (!dryRun && !string.IsNullOrWhiteSpace(progressIdMapPath))
            PruneProgressIdMapForLegacyApplications(progressIdMapPath, legacyToTarget.Keys);

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
            SnapshotsBackfilled = snapshotsBackfilled,
            ProgressDeleted = progressDeleted,
            ProgressPosted = regen.PostedCount,
            ProgressFailed = regen.FailedCount,
            Errors = errors,
        };
    }

    private static int BackfillApprovalLegSnapshots(
        INonSecuredObjectSpaceFactory objectSpaceFactory,
        HashSet<Guid> targetAppIds,
        bool dryRun,
        bool verbose)
    {
        if (targetAppIds.Count == 0)
            return 0;

        using var objectSpace = objectSpaceFactory.CreateNonSecuredObjectSpace(typeof(Bo.Application));
        MigrationImportContext.ApplyImportObjectSpaceHooks(objectSpace);
        var profileLegCounts = objectSpace.GetObjectsQuery<Bo.ApprovalLegProfileMinistryLeg>()
            .Where(l => l.ApprovingMinistry != null && l.ApprovalLegProfile != null)
            .AsEnumerable()
            .GroupBy(l => l.ApprovalLegProfile!.ID)
            .ToDictionary(g => g.Key, g => g.Count());

        var applications = objectSpace.GetObjectsQuery<Bo.Application>()
            .Where(a => targetAppIds.Contains(a.ID) && a.ApprovalLegProfile != null)
            .ToList();

        var backfilled = 0;
        foreach (var application in applications)
        {
            var expectedLegs = Visa2014ApplicationMinistryLegCountResolver.ResolveLegCount(application, profileLegCounts);
            if (expectedLegs <= 0)
                continue;

            var snapshotLegs = application.ApprovalLegSnapshots?
                .Count(s => !string.IsNullOrWhiteSpace(s.MinistryShortName)) ?? 0;
            if (snapshotLegs == expectedLegs)
                continue;

            if (!dryRun)
                Bo.ApprovalLegProfileMinistryHelper.ApplySnapshot(objectSpace, application, application.ApprovalLegProfile);
            backfilled++;
            if (verbose)
                Console.WriteLine($"  SNAPSHOT {application.FullApplicationNumber ?? application.ID.ToString()} legs {snapshotLegs} -> {expectedLegs}");
        }

        if (!dryRun && backfilled > 0)
            objectSpace.CommitChanges();

        return backfilled;
    }

    private static void PruneProgressIdMapForLegacyApplications(string path, IEnumerable<Guid> legacyApplicationOids)
    {
        if (!File.Exists(path))
            return;

        var existing = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(path))
            ?? new Dictionary<string, string>(StringComparer.Ordinal);
        var prefixes = legacyApplicationOids
            .Select(id => $"{id:D}:")
            .ToHashSet(StringComparer.Ordinal);
        var pruned = existing
            .Where(kv => !prefixes.Any(p => kv.Key.StartsWith(p, StringComparison.Ordinal)))
            .ToDictionary(kv => kv.Key, kv => kv.Value, StringComparer.Ordinal);
        File.WriteAllText(path, JsonSerializer.Serialize(pruned, new JsonSerializerOptions { WriteIndented = true }));
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
