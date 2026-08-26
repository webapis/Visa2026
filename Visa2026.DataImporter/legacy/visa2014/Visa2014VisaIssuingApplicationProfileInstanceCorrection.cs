using DevExpress.ExpressApp;
using Visa2026.Blazor.Server.Services.Migration;
using Visa2026.Module.Services.MigrationImport;
using Bo = Visa2026.Module.BusinessObjects;

namespace Visa2026.DataImporter.Legacy.Visa2014;

internal sealed class Visa2014VisaIssuingApplicationProfileInstanceCorrectionResult
{
    public int VisasInScope { get; init; }
    public int Updated { get; init; }
    public int AlreadyCorrect { get; init; }
    public int SkippedNoLegacyApplication { get; init; }
    public int SkippedMissingApplicationMap { get; init; }
    public int SkippedMissingVisaTarget { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = [];
}

/// <summary>
/// Path B post-pass: set <see cref="Bo.Visa.IssuingApplicationProfileInstance"/> from legacy ProcessNumber / extension sibling index.
/// </summary>
internal static class Visa2014VisaIssuingApplicationProfileInstanceCorrection
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
        var visaIdMapPath = GetOptionValue(args, "--visa-id-map")
            ?? source.IdMapPath(dataImporterRoot, "Visa");
        var applicationIdMapPath = GetOptionValue(args, "--application-id-map")
            ?? source.IdMapPath(dataImporterRoot, "ApplicationProfileInstance");

        Console.WriteLine("=== VISA2014 Visa IssuingApplicationProfileInstance correction (Path B)");
        Console.WriteLine($"INF Legacy source: {source.Id}");
        Console.WriteLine($"INF Target SQL: {MaskConnectionString(targetConnection)}");
        Console.WriteLine($"INF Visa id-map: {visaIdMapPath}");
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

            var visaIdMap = Visa2014IdMapHelper.LoadOrEmpty(visaIdMapPath);
            if (visaIdMap.Count == 0)
            {
                Console.Error.WriteLine($"ERR Visa id-map empty or missing: {visaIdMapPath}");
                return 1;
            }

            var applicationIdMap = Visa2014IdMapHelper.LoadOrEmpty(applicationIdMapPath);
            var issuingByLegacyVisa = Visa2014VisaIssuingApplicationProfileInstanceIndex.Load(
                source.ConnectionString,
                verbose);

            var result = Run(
                host.ObjectSpaceFactory,
                visaIdMap,
                applicationIdMap,
                issuingByLegacyVisa,
                dryRun,
                verbose);

            Console.WriteLine($"INF Visas in id-map: {result.VisasInScope}");
            Console.WriteLine($"INF IssuingApplicationProfileInstance updated: {result.Updated}");
            Console.WriteLine($"INF Already correct: {result.AlreadyCorrect}");
            Console.WriteLine($"INF No legacy Application link: {result.SkippedNoLegacyApplication}");
            Console.WriteLine($"INF Application not in id-map: {result.SkippedMissingApplicationMap}");
            Console.WriteLine($"INF Missing Visa target: {result.SkippedMissingVisaTarget}");

            foreach (var error in result.Errors.Take(40))
                Console.Error.WriteLine($"ERR {error}");
            if (result.Errors.Count > 40)
                Console.Error.WriteLine($"ERR ... and {result.Errors.Count - 40} more");

            return result.Errors.Count > 0 ? 1 : 0;
        }
        finally
        {
            importScope?.Dispose();
            host?.Dispose();
        }
    }

    internal static Visa2014VisaIssuingApplicationProfileInstanceCorrectionResult Run(
        INonSecuredObjectSpaceFactory objectSpaceFactory,
        IReadOnlyDictionary<Guid, Guid> visaIdMap,
        IReadOnlyDictionary<Guid, Guid> applicationIdMap,
        IReadOnlyDictionary<Guid, Guid> issuingByLegacyVisa,
        bool dryRun,
        bool verbose)
    {
        var errors = new List<string>();
        int updated = 0;
        int alreadyCorrect = 0;
        int skippedNoLegacyApplication = 0;
        int skippedMissingApplicationMap = 0;
        int skippedMissingVisaTarget = 0;

        using var objectSpace = objectSpaceFactory.CreateNonSecuredObjectSpace(typeof(Bo.Visa));

        foreach (var (legacyVisaOid, targetVisaId) in visaIdMap)
        {
            if (!issuingByLegacyVisa.TryGetValue(legacyVisaOid, out var legacyApplicationOid))
            {
                skippedNoLegacyApplication++;
                continue;
            }

            if (!applicationIdMap.TryGetValue(legacyApplicationOid, out var applicationId))
            {
                skippedMissingApplicationMap++;
                continue;
            }

            var visa = objectSpace.GetObjectByKey<Bo.Visa>(targetVisaId);
            if (visa == null)
            {
                skippedMissingVisaTarget++;
                errors.Add($"{legacyVisaOid}: Visa target {targetVisaId} not found");
                continue;
            }

            if (visa.IssuingApplicationProfileInstance != null
                && visa.IssuingApplicationProfileInstance.ID == applicationId)
            {
                alreadyCorrect++;
                continue;
            }

            var application = objectSpace.GetObjectByKey<Bo.ApplicationProfileInstance>(applicationId);
            if (application == null)
            {
                skippedMissingVisaTarget++;
                errors.Add($"{legacyVisaOid}: ApplicationProfileInstance {applicationId} not found");
                continue;
            }

            if (dryRun)
            {
                if (verbose)
                {
                    Console.WriteLine(
                        $"  DRY Visa {targetVisaId}: IssuingApplicationProfileInstance -> {applicationId} " +
                        $"(legacy app {legacyApplicationOid})");
                }

                updated++;
                continue;
            }

            visa.IssuingApplicationProfileInstance = application;
            updated++;
            if (verbose)
                Console.WriteLine($"  PATCH Visa {targetVisaId} IssuingApplicationProfileInstance={applicationId}");
        }

        if (!dryRun && updated > 0)
            objectSpace.CommitChanges();

        return new Visa2014VisaIssuingApplicationProfileInstanceCorrectionResult
        {
            VisasInScope = visaIdMap.Count,
            Updated = updated,
            AlreadyCorrect = alreadyCorrect,
            SkippedNoLegacyApplication = skippedNoLegacyApplication,
            SkippedMissingApplicationMap = skippedMissingApplicationMap,
            SkippedMissingVisaTarget = skippedMissingVisaTarget,
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
