using DevExpress.ExpressApp;
using Visa2026.Blazor.Server.Services.Migration;
using Bo = Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.MigrationImport;

namespace Visa2026.DataImporter.Legacy.Visa2014;

internal sealed class Visa2014VisaIssuingApplicationItemCorrectionResult
{
    public int VisasInScope { get; init; }
    public int Updated { get; init; }
    public int AlreadyCorrect { get; init; }
    public int SkippedNoLink { get; init; }
    public int SkippedMissingVisaMap { get; init; }
    public int SkippedMissingApplicationItemMap { get; init; }
    public int SkippedMissingTarget { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = [];
    public IReadOnlyDictionary<string, int> SourceHistogram { get; init; } =
        new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
}

/// <summary>
/// Post-ApplicationItem correction: set Visa.IssuingApplicationItem from ProcessNumber or extension sibling.
/// </summary>
internal static class Visa2014VisaIssuingApplicationItemCorrection
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
        var visaIdMapPath = GetOptionValue(args, "--visa-id-map")
            ?? source.IdMapPath(dataImporterRoot, "Visa");
        var applicationItemIdMapPath = GetOptionValue(args, "--application-item-id-map")
            ?? source.IdMapPath(dataImporterRoot, "ApplicationItem");

        Console.WriteLine("=== VISA2014 Visa IssuingApplicationItem correction");
        Console.WriteLine($"INF Legacy source: {source.Id}");
        Console.WriteLine($"INF Target SQL: {MaskConnectionString(targetConnection)}");
        Console.WriteLine($"INF Visa id-map: {visaIdMapPath}");
        Console.WriteLine($"INF ApplicationItem id-map: {applicationItemIdMapPath}");
        if (dryRun) Console.WriteLine("INF Mode: dry-run (no writes)");

        try { Visa2014LegacySqlGuard.EnsureLegacyReadCredentials(source.ConnectionString); }
        catch (Exception ex) { Console.Error.WriteLine($"ERR {ex.Message}"); return 1; }

        HeadlessMigrationHost? host = null;
        IDisposable? importScope = null;
        try
        {
            host = HeadlessMigrationHost.Start(targetConnection);
            importScope = MigrationImportContext.BeginDataImportScope();

            var visaIdMap = Visa2014IdMapHelper.LoadOrEmpty(visaIdMapPath);
            var applicationItemIdMap = Visa2014IdMapHelper.LoadOrEmpty(applicationItemIdMapPath);
            if (visaIdMap.Count == 0)
            {
                Console.Error.WriteLine($"ERR Visa id-map empty or missing: {visaIdMapPath}");
                return 1;
            }

            if (applicationItemIdMap.Count == 0)
            {
                Console.Error.WriteLine($"ERR ApplicationItem id-map empty or missing: {applicationItemIdMapPath}");
                return 1;
            }

            var result = await RunAsync(
                host.ObjectSpaceFactory,
                source.ConnectionString,
                visaIdMap,
                applicationItemIdMap,
                dryRun,
                verbose);

            Console.WriteLine($"INF Visas in id-map: {result.VisasInScope}");
            Console.WriteLine($"INF IssuingApplicationItem updated: {result.Updated}");
            Console.WriteLine($"INF Already correct: {result.AlreadyCorrect}");
            Console.WriteLine($"INF No legacy link: {result.SkippedNoLink}");
            Console.WriteLine($"INF Missing Visa target: {result.SkippedMissingVisaMap}");
            Console.WriteLine($"INF Missing ApplicationItem map/target: {result.SkippedMissingApplicationItemMap + result.SkippedMissingTarget}");
            if (result.SourceHistogram.Count > 0)
            {
                var hist = string.Join(", ",
                    result.SourceHistogram.OrderByDescending(kv => kv.Value).Select(kv => $"{kv.Key}={kv.Value}"));
                Console.WriteLine($"INF Source histogram (updates+already): {hist}");
            }

            foreach (var error in result.Errors.Take(40))
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

    internal static Task<Visa2014VisaIssuingApplicationItemCorrectionResult> RunAsync(
        INonSecuredObjectSpaceFactory objectSpaceFactory,
        string legacyConnectionString,
        IReadOnlyDictionary<Guid, Guid> visaIdMap,
        IReadOnlyDictionary<Guid, Guid> applicationItemIdMap,
        bool dryRun,
        bool verbose)
    {
        var links = Visa2014VisaIssuingApplicationItemIndex.Load(legacyConnectionString, verbose);
        var errors = new List<string>();
        int updated = 0, alreadyCorrect = 0, noLink = 0, missingVisa = 0, missingItemMap = 0, missingTarget = 0;
        var sourceHistogram = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        using var objectSpace = objectSpaceFactory.CreateNonSecuredObjectSpace(typeof(Bo.Visa));
        MigrationImportContext.ApplyImportObjectSpaceHooks(objectSpace);

        var visasById = objectSpace.GetObjectsQuery<Bo.Visa>()
            .Where(v => v.GCRecord == 0)
            .ToDictionary(v => v.ID);
        var itemsById = objectSpace.GetObjectsQuery<Bo.ApplicationItem>()
            .Where(ai => ai.GCRecord == 0)
            .ToDictionary(ai => ai.ID);

        foreach (var (legacyVisaOid, targetVisaId) in visaIdMap)
        {
            if (!links.TryGetValue(legacyVisaOid, out var link))
            {
                noLink++;
                continue;
            }

            if (!visasById.TryGetValue(targetVisaId, out var visa))
            {
                missingVisa++;
                continue;
            }

            if (!applicationItemIdMap.TryGetValue(link.LegacyApplicationItemOid, out var targetItemId))
            {
                missingItemMap++;
                continue;
            }

            if (!itemsById.TryGetValue(targetItemId, out var item))
            {
                missingTarget++;
                continue;
            }

            sourceHistogram[link.Source] = sourceHistogram.TryGetValue(link.Source, out var c) ? c + 1 : 1;

            if (visa.IssuingApplicationItem != null && visa.IssuingApplicationItem.ID == item.ID)
            {
                alreadyCorrect++;
                continue;
            }

            if (dryRun)
            {
                updated++;
                if (verbose && updated <= 20)
                    Console.WriteLine($"  DRY Visa {targetVisaId} <- ApplicationItem {targetItemId} ({link.Source})");
                continue;
            }

            visa.IssuingApplicationItem = item;
            updated++;
            if (verbose && updated % 500 == 0)
                Console.WriteLine($"INF Progress: {updated} updated...");
        }

        if (!dryRun && updated > 0)
            objectSpace.CommitChanges();

        return Task.FromResult(new Visa2014VisaIssuingApplicationItemCorrectionResult
        {
            VisasInScope = visaIdMap.Count,
            Updated = updated,
            AlreadyCorrect = alreadyCorrect,
            SkippedNoLink = noLink,
            SkippedMissingVisaMap = missingVisa,
            SkippedMissingApplicationItemMap = missingItemMap,
            SkippedMissingTarget = missingTarget,
            Errors = errors,
            SourceHistogram = sourceHistogram,
        });
    }

    private static string MaskConnectionString(string connectionString) =>
        System.Text.RegularExpressions.Regex.Replace(
            connectionString,
            @"(Password|Pwd)\s*=\s*[^;]+",
            "$1=***",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

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