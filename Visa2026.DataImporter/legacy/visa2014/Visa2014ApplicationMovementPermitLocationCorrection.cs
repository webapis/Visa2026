using DevExpress.ExpressApp;
using Visa2026.Blazor.Server.Services.Migration;
using Visa2026.Module.Services.MigrationImport;
using Bo = Visa2026.Module.BusinessObjects;

namespace Visa2026.DataImporter.Legacy.Visa2014;

internal sealed class Visa2014ApplicationMovementPermitLocationCorrectionResult
{
    public int InScope { get; init; }
    public int Updated { get; init; }
    public int AlreadyFilled { get; init; }
    public int SkippedWrongType { get; init; }
    public int SkippedNoFallback { get; init; }
    public int SkippedMissingTarget { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = [];
}

/// <summary>
/// Backfill empty MovementPermitLocation on App_Additional_WP_location instances from legacy WP location bits.
/// </summary>
internal static class Visa2014ApplicationMovementPermitLocationCorrection
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
            ?? "Host=localhost;Port=5432;Database=visa2026;Username=postgres;Password=Visa2026Local;Persist Security Info=True;EFCoreProvider=Postgres";
        var applicationIdMapPath = GetOptionValue(args, "--application-id-map")
            ?? source.IdMapPath(dataImporterRoot, "ApplicationProfileInstance");

        Console.WriteLine("=== VISA2014 App_Additional_WP_location MovementPermitLocation backfill");
        Console.WriteLine($"INF Legacy source: {source.Id}");
        Console.WriteLine($"INF Target SQL: {MaskConnectionString(targetConnection)}");
        Console.WriteLine($"INF ApplicationProfileInstance id-map: {applicationIdMapPath}");
        if (dryRun) Console.WriteLine("INF Mode: dry-run (no writes)");

        try
        {
            Visa2014LegacySqlGuard.EnsureLegacyReadCredentials(source.ConnectionString);
            await Visa2014LegacySqlGuard.EnsureLegacyConnectionAsync(source.ConnectionString);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ERR Legacy SQL: {ex.Message}");
            return 1;
        }

        HeadlessMigrationHost? host = null;
        IDisposable? importScope = null;
        try
        {
            host = HeadlessMigrationHost.Start(targetConnection);
            importScope = MigrationImportContext.BeginDataImportScope();

            var catalogs = Visa2014LookupTranslator.Load(source.LookupTranslationPaths);
            var fallback = Visa2014ApplicationWorkPermitLocationFallbackIndex.Load(
                source.ConnectionString, catalogs, verbose);
            var applicationIdMap = Visa2014IdMapHelper.LoadOrEmpty(applicationIdMapPath);

            var result = Run(host.ObjectSpaceFactory, applicationIdMap, fallback, dryRun, verbose);
            Console.WriteLine($"INF Instances in id-map: {result.InScope}");
            Console.WriteLine($"INF MovementPermitLocation updated: {result.Updated}");
            Console.WriteLine($"INF Already filled: {result.AlreadyFilled}");
            Console.WriteLine($"INF Wrong type: {result.SkippedWrongType}");
            Console.WriteLine($"INF No WP-location fallback: {result.SkippedNoFallback}");
            Console.WriteLine($"INF Missing target: {result.SkippedMissingTarget}");
            foreach (var error in result.Errors.Take(40))
                Console.Error.WriteLine($"ERR {error}");
            return result.Errors.Count > 0 ? 1 : 0;
        }
        finally
        {
            importScope?.Dispose();
            host?.Dispose();
        }
    }

    internal static Visa2014ApplicationMovementPermitLocationCorrectionResult Run(
        INonSecuredObjectSpaceFactory objectSpaceFactory,
        IReadOnlyDictionary<Guid, Guid> applicationIdMap,
        IReadOnlyDictionary<Guid, string> labelsByLegacyApplicationOid,
        bool dryRun,
        bool verbose)
    {
        var errors = new List<string>();
        int updated = 0;
        int alreadyFilled = 0;
        int skippedWrongType = 0;
        int skippedNoFallback = 0;
        int skippedMissingTarget = 0;

        using var objectSpace = objectSpaceFactory.CreateNonSecuredObjectSpace(typeof(Bo.ApplicationProfileInstance));

        foreach (var (legacyOid, targetId) in applicationIdMap)
        {
            var instance = objectSpace.GetObjectByKey<Bo.ApplicationProfileInstance>(targetId);
            if (instance == null)
            {
                skippedMissingTarget++;
                continue;
            }

            var typeName = instance.ApplicationType?.Name;
            if (!Visa2014ApplicationWorkPermitLocationFallbackIndex.IsAdditionalWpLocationType(typeName))
            {
                skippedWrongType++;
                continue;
            }

            if (!string.IsNullOrWhiteSpace(instance.MovementPermitLocation))
            {
                alreadyFilled++;
                continue;
            }

            if (!labelsByLegacyApplicationOid.TryGetValue(legacyOid, out var labels)
                || string.IsNullOrWhiteSpace(labels))
            {
                skippedNoFallback++;
                continue;
            }

            if (verbose)
            {
                Console.WriteLine(
                    dryRun
                        ? $"  DRY {instance.FullApplicationNumber}: MovementPermitLocation <- {labels}"
                        : $"  PATCH {instance.FullApplicationNumber}: MovementPermitLocation <- {labels}");
            }

            if (!dryRun)
                instance.MovementPermitLocation = labels;
            updated++;
        }

        if (!dryRun && updated > 0)
            objectSpace.CommitChanges();

        return new Visa2014ApplicationMovementPermitLocationCorrectionResult
        {
            InScope = applicationIdMap.Count,
            Updated = updated,
            AlreadyFilled = alreadyFilled,
            SkippedWrongType = skippedWrongType,
            SkippedNoFallback = skippedNoFallback,
            SkippedMissingTarget = skippedMissingTarget,
            Errors = errors,
        };
    }

    private static bool HasArg(IReadOnlyList<string> args, string name) =>
        args.Any(a => string.Equals(a, name, StringComparison.OrdinalIgnoreCase));

    private static string? GetOptionValue(IReadOnlyList<string> args, string optionName)
    {
        for (var i = 0; i < args.Count - 1; i++)
        {
            if (string.Equals(args[i], optionName, StringComparison.OrdinalIgnoreCase))
                return args[i + 1];
        }

        return null;
    }

    private static string MaskConnectionString(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            return "(empty)";

        var parts = connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        for (var i = 0; i < parts.Length; i++)
        {
            if (parts[i].StartsWith("Password=", StringComparison.OrdinalIgnoreCase))
                parts[i] = "Password=***";
        }

        return string.Join(';', parts);
    }
}