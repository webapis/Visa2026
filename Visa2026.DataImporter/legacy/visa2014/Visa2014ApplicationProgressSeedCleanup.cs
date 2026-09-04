using System.Text.Json;
using Visa2026.DataImporter;

namespace Visa2026.DataImporter.Legacy.Visa2014;

internal sealed class Visa2014ApplicationProfileInstanceProgressSeedCleanupResult
{
    public int ProgressRowsScanned { get; init; }
    public int SeedRowsMatched { get; init; }
    public int Deleted { get; init; }
    public int SkippedNotInApplicationProfileInstanceIdMap { get; init; }
    public int Failed { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = [];
}

/// <summary>
/// Removes auto-seeded ApplicationProfileInstanceProgress rows (IS_BEING_PREPARED @ AT_OFFICE, empty Description)
/// created by <see cref="ApplicationProfileInstanceProgressInitializer"/> during ApplicationProfileInstance OData import.
/// </summary>
internal static class Visa2014ApplicationProfileInstanceProgressSeedCleanup
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
        try
        {
            source = Visa2014LegacySource.Resolve(dataImporterRoot, solutionRoot, args);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ERR {ex.Message}");
            return 1;
        }

        var apiBaseUrl = GetOptionValue(args, "--api-base-url")
            ?? Environment.GetEnvironmentVariable("ApiOptions__BaseUrl")
            ?? Environment.GetEnvironmentVariable("API_BASE_URL")
            ?? "https://localhost:5001";
        var userName = GetOptionValue(args, "--user") ?? "Admin";
        var password = GetOptionValue(args, "--password") ?? "";

        var applicationIdMapPath = GetOptionValue(args, "--application-id-map")
            ?? source.IdMapPath(dataImporterRoot, "ApplicationProfileInstance");

        bool dryRun = HasArg(args, "--dry-run");
        bool noWait = HasArg(args, "--no-wait");
        bool restrictToIdMap = !HasArg(args, "--all-applications");

        Console.WriteLine("=== VISA2014 ApplicationProfileInstanceProgress seed cleanup");
        Console.WriteLine($"INF Target API: {apiBaseUrl}");
        Console.WriteLine($"INF ApplicationProfileInstance id-map: {applicationIdMapPath}");
        Console.WriteLine($"INF Scope: {(restrictToIdMap ? "applications in id-map only" : "all applications")}");
        if (dryRun)
            Console.WriteLine("INF Mode: dry-run (no DELETE)");

        var api = new ApiClient(apiBaseUrl, userName, password) { Verbose = verbose };

        if (!noWait)
            await api.WaitForServerAsync();
        await api.LoginAsync();

        try
        {
            HashSet<Guid>? applicationIds = null;
            if (restrictToIdMap)
            {
                if (!File.Exists(applicationIdMapPath))
                {
                    Console.Error.WriteLine($"ERR ApplicationProfileInstance id-map not found: {applicationIdMapPath}");
                    return 1;
                }

                applicationIds = Visa2014IdMapHelper.Load(applicationIdMapPath).Values.ToHashSet();
                Console.WriteLine($"INF ApplicationProfileInstance id-map entries: {applicationIds.Count}");
            }

            var result = await RunAsync(api, applicationIds, dryRun, verbose);

            Console.WriteLine($"INF Progress rows scanned: {result.ProgressRowsScanned}");
            Console.WriteLine($"INF Seed rows matched: {result.SeedRowsMatched}");
            Console.WriteLine($"INF Deleted: {result.Deleted}");
            if (result.SkippedNotInApplicationProfileInstanceIdMap > 0)
                Console.WriteLine($"INF Skipped (not in ApplicationProfileInstance id-map): {result.SkippedNotInApplicationProfileInstanceIdMap}");
            Console.WriteLine($"INF Failed: {result.Failed}");

            foreach (var error in result.Errors.Take(20))
                Console.Error.WriteLine($"ERR {error}");
            if (result.Errors.Count > 20)
                Console.Error.WriteLine($"ERR ... and {result.Errors.Count - 20} more");

            return result.Failed > 0 ? 1 : 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ERR Cleanup failed: {ex.Message}");
            if (verbose)
                Console.Error.WriteLine(ex);
            return 1;
        }
    }

    public static async Task<Visa2014ApplicationProfileInstanceProgressSeedCleanupResult> RunAsync(
        ApiClient api,
        IReadOnlySet<Guid>? restrictToApplicationProfileInstanceIds,
        bool dryRun,
        bool verbose)
    {
        var progressRows = await api.GetAllAsync<ApplicationProfileInstanceProgress>(
            "ApplicationProfileInstanceProgress",
            "$expand=ApplicationProfileInstance,State,Location");

        var errors = new List<string>();
        int matched = 0, deleted = 0, skippedScope = 0, failed = 0;

        foreach (var row in progressRows)
        {
            if (!Visa2014ApplicationProfileInstanceProgressSeedHelper.IsInitializerSeed(row))
                continue;

            matched++;

            var applicationId = row.ApplicationProfileInstance?.Id ?? Guid.Empty;
            if (applicationId == Guid.Empty)
                continue;

            if (restrictToApplicationProfileInstanceIds != null && !restrictToApplicationProfileInstanceIds.Contains(applicationId))
            {
                skippedScope++;
                continue;
            }

            try
            {
                if (dryRun)
                {
                    if (verbose)
                        Console.WriteLine($"  DRY DELETE ApplicationProfileInstanceProgress {row.Id} (ApplicationProfileInstance {applicationId})");
                    deleted++;
                    continue;
                }

                await api.DeleteAsync("ApplicationProfileInstanceProgress", row.Id);
                deleted++;
                if (deleted % 500 == 0)
                    Console.WriteLine($"INF Progress: {deleted} seed row(s) deleted...");
                if (verbose)
                    Console.WriteLine($"  DELETE ApplicationProfileInstanceProgress {row.Id} (ApplicationProfileInstance {applicationId})");
            }
            catch (Exception ex)
            {
                failed++;
                errors.Add($"{row.Id}: {ex.Message}");
                Console.Error.WriteLine($"ERR {row.Id}: {ex.Message}");
            }
        }

        return new Visa2014ApplicationProfileInstanceProgressSeedCleanupResult
        {
            ProgressRowsScanned = progressRows.Count,
            SeedRowsMatched = matched,
            Deleted = deleted,
            SkippedNotInApplicationProfileInstanceIdMap = skippedScope,
            Failed = failed,
            Errors = errors,
        };
    }

    private static bool HasArg(IReadOnlyList<string> args, string flag) =>
        args.Any(a => string.Equals(a, flag, StringComparison.OrdinalIgnoreCase));

    private static string? GetOptionValue(IReadOnlyList<string> args, string optionName)
    {
        for (int i = 0; i < args.Count; i++)
        {
            if (!string.Equals(args[i], optionName, StringComparison.OrdinalIgnoreCase))
                continue;

            if (i + 1 < args.Count && !args[i + 1].StartsWith('-'))
                return args[i + 1];
            return null;
        }

        return null;
    }
}
