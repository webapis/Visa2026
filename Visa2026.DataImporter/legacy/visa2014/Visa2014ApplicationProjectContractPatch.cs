namespace Visa2026.DataImporter.Legacy.Visa2014;

internal sealed class Visa2014ApplicationProjectContractPatchResult
{
    public int Planned { get; init; }
    public int Patched { get; init; }
    public int SkippedNoProjectContract { get; init; }
    public int SkippedNoIdMap { get; init; }
    public int SkippedNotShowProjectContract { get; init; }
    public int Failed { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = [];
}

/// <summary>
/// PATCHes <see cref="Application.ProjectContract"/> on imported Applications.
/// Resolves contract from legacy Application.Contract or linked Person (same coalesce as transform).
/// </summary>
internal static class Visa2014ApplicationProjectContractPatch
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
            ?? source.IdMapPath(dataImporterRoot, "Application");

        bool dryRun = HasArg(args, "--dry-run");
        bool noWait = HasArg(args, "--no-wait");
        int? maxRows = int.TryParse(GetOptionValue(args, "--max-rows"), out var max) ? max : null;

        Console.WriteLine("=== VISA2014 Application.ProjectContract PATCH");
        Console.WriteLine($"INF Legacy source: {source.Id}");
        Console.WriteLine($"INF Target API: {apiBaseUrl}");
        Console.WriteLine($"INF Application id-map: {applicationIdMapPath}");
        if (dryRun)
            Console.WriteLine("INF Mode: dry-run (no PATCH)");

        var connectionString = GetOptionValue(args, "--connection") ?? source.ConnectionString;
        try
        {
            Visa2014LegacySqlGuard.EnsureLegacyReadCredentials(connectionString);
            await Visa2014LegacySqlGuard.EnsureLegacyConnectionAsync(connectionString);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ERR {ex.Message}");
            return 1;
        }

        if (!File.Exists(applicationIdMapPath))
        {
            Console.Error.WriteLine($"ERR Application id-map not found: {applicationIdMapPath}");
            return 1;
        }

        var batch = Visa2014ApplicationTransform.PrepareImportBatch(
            connectionString,
            source.LookupTranslationPaths,
            maxRows,
            verbose);

        var idMap = Visa2014IdMapHelper.Load(applicationIdMapPath);
        Console.WriteLine($"INF Application id-map entries: {idMap.Count}");
        Console.WriteLine($"INF Transform import rows: {batch.ImportRows.Count}");

        var api = new ApiClient(apiBaseUrl, userName, password) { Verbose = verbose };
        var resolver = new Visa2014ODataLookupResolver();

        if (!noWait)
            await api.WaitForServerAsync();
        await api.LoginAsync();
        await resolver.LoadAsync(api);

        try
        {
            var result = await RunAsync(api, resolver, batch.ImportRows, idMap, dryRun, verbose);

            Console.WriteLine($"INF Planned PATCH: {result.Planned}");
            Console.WriteLine($"INF Patched: {result.Patched}");
            Console.WriteLine($"INF Skipped (not ShowProjectContract type): {result.SkippedNotShowProjectContract}");
            Console.WriteLine($"INF Skipped (no ProjectContract in transform): {result.SkippedNoProjectContract}");
            Console.WriteLine($"INF Skipped (no id-map): {result.SkippedNoIdMap}");
            Console.WriteLine($"INF Failed: {result.Failed}");

            foreach (var error in result.Errors.Take(20))
                Console.Error.WriteLine($"ERR {error}");
            if (result.Errors.Count > 20)
                Console.Error.WriteLine($"ERR ... and {result.Errors.Count - 20} more");

            return result.Failed > 0 ? 1 : 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ERR PATCH failed: {ex.Message}");
            if (verbose)
                Console.Error.WriteLine(ex);
            return 1;
        }
    }

    public static async Task<Visa2014ApplicationProjectContractPatchResult> RunAsync(
        ApiClient api,
        Visa2014ODataLookupResolver resolver,
        IReadOnlyList<Dictionary<string, object?>> importRows,
        IReadOnlyDictionary<Guid, Guid> applicationIdMap,
        bool dryRun,
        bool verbose)
    {
        var errors = new List<string>();
        int planned = 0, patched = 0, skippedNoPc = 0, skippedMap = 0, skippedType = 0, failed = 0;

        foreach (var row in importRows)
        {
            if (row.GetValueOrDefault("_legacyRowId") is not Guid legacyOid || legacyOid == Guid.Empty)
                continue;

            var applicationType = row.GetValueOrDefault("ApplicationType") as string;
            if (string.IsNullOrWhiteSpace(applicationType)
                || !Visa2014ApplicationTransform.ShowProjectContractApplicationTypes.Contains(applicationType))
            {
                skippedType++;
                continue;
            }

            var projectContractCode = row.GetValueOrDefault("ProjectContract") as string;
            if (string.IsNullOrWhiteSpace(projectContractCode))
            {
                skippedNoPc++;
                continue;
            }

            if (!applicationIdMap.TryGetValue(legacyOid, out var targetApplicationId))
            {
                skippedMap++;
                continue;
            }

            var projectContractId = resolver.ResolveProjectContract(projectContractCode);
            if (!projectContractId.HasValue)
            {
                failed++;
                var fullNumber = row.GetValueOrDefault("FullApplicationNumber") as string ?? legacyOid.ToString();
                errors.Add($"Application {fullNumber} ({targetApplicationId}): ProjectContract not in OData — '{projectContractCode}'");
                Console.Error.WriteLine($"ERR Application {fullNumber}: ProjectContract not found for '{projectContractCode}'");
                continue;
            }

            planned++;

            try
            {
                if (dryRun)
                {
                    patched++;
                    if (verbose)
                    {
                        var fullNumber = row.GetValueOrDefault("FullApplicationNumber") as string ?? targetApplicationId.ToString();
                        Console.WriteLine($"  DRY PATCH Application {fullNumber} ({targetApplicationId}) ProjectContract={projectContractCode}");
                    }
                    continue;
                }

                await api.UpdateAsync("Application", targetApplicationId, new Dictionary<string, object?>
                {
                    ["ProjectContract"] = new { ID = projectContractId.Value },
                });
                patched++;

                if (patched % 500 == 0)
                    Console.WriteLine($"INF Progress: {patched} patched...");
            }
            catch (Exception ex)
            {
                failed++;
                errors.Add($"{targetApplicationId}: {ex.Message}");
                Console.Error.WriteLine($"ERR {targetApplicationId}: {ex.Message}");
            }
        }

        return new Visa2014ApplicationProjectContractPatchResult
        {
            Planned = planned,
            Patched = patched,
            SkippedNoProjectContract = skippedNoPc,
            SkippedNoIdMap = skippedMap,
            SkippedNotShowProjectContract = skippedType,
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
