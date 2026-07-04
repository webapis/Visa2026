namespace Visa2026.DataImporter.Legacy.Visa2014;

internal sealed class Visa2014ApplicationMigrationServiceInferencePatchResult
{
    public int Planned { get; init; }
    public int Patched { get; init; }
    public int SkippedNoProposal { get; init; }
    public int SkippedNoIdMap { get; init; }
    public int Failed { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = [];
}

/// <summary>
/// Second-pass PATCH: <see cref="Application.MigrationService"/> from person address inference
/// for <c>App_Reg_Check_In</c> rows with null legacy <c>DepartmentForRegistration</c>.
/// </summary>
internal static class Visa2014ApplicationMigrationServiceInferencePatch
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

        var rulesYamlPath = GetOptionValue(args, "--inference-rules")
            ?? Visa2014MigrationServiceInferenceRules.ResolveRulesPath(solutionRoot);

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
        bool forceUnapproved = HasArg(args, "--force");
        int? maxRows = int.TryParse(GetOptionValue(args, "--max-rows"), out var max) ? max : null;

        Console.WriteLine("=== VISA2014 Application.MigrationService inference PATCH");
        Console.WriteLine($"INF Legacy source: {source.Id}");
        Console.WriteLine($"INF Inference rules: {rulesYamlPath}");
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

        var (inferenceRows, rules, confidenceCounts) = Visa2014ApplicationMigrationServiceInferencePreview.PrepareInferenceRows(
            connectionString,
            rulesYamlPath,
            maxRows,
            verbose);

        if (!rules.ApprovedForPatch && !forceUnapproved)
        {
            Console.Error.WriteLine("ERR migration-service-inference.yaml approvedForPatch is false — review preview Excel first, or pass --force.");
            return 1;
        }

        foreach (var pair in confidenceCounts.OrderBy(p => p.Key))
            Console.WriteLine($"INF Confidence {pair.Key}: {pair.Value}");

        var idMap = Visa2014IdMapHelper.Load(applicationIdMapPath);
        Console.WriteLine($"INF Application id-map entries: {idMap.Count}");
        Console.WriteLine($"INF Inference rows: {inferenceRows.Count}");

        var api = new ApiClient(apiBaseUrl, userName, password) { Verbose = verbose };

        if (!noWait)
            await api.WaitForServerAsync();
        await api.LoginAsync();

        try
        {
            var result = await RunAsync(api, inferenceRows, idMap, dryRun, verbose);

            Console.WriteLine($"INF Planned PATCH: {result.Planned}");
            Console.WriteLine($"INF Patched: {result.Patched}");
            Console.WriteLine($"INF Skipped (no proposal / none confidence): {result.SkippedNoProposal}");
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

    public static async Task<Visa2014ApplicationMigrationServiceInferencePatchResult> RunAsync(
        ApiClient api,
        IReadOnlyList<Dictionary<string, object?>> inferenceRows,
        IReadOnlyDictionary<Guid, Guid> applicationIdMap,
        bool dryRun,
        bool verbose)
    {
        var migrationServices = await api.GetAllAsync<MigrationService>("MigrationService", "?$top=10000");
        Console.WriteLine($"INF OData MigrationService rows: {migrationServices.Count}");

        var errors = new List<string>();
        int planned = 0, patched = 0, skippedNoProposal = 0, skippedMap = 0, failed = 0;

        foreach (var row in inferenceRows)
        {
            if (row.GetValueOrDefault("_legacyApplicationOid") is not Guid legacyOid || legacyOid == Guid.Empty)
                continue;

            var confidence = row.GetValueOrDefault("Confidence") as string ?? "none";
            var migrationServiceName = row.GetValueOrDefault("ProposedMigrationService") as string;
            if (string.IsNullOrWhiteSpace(migrationServiceName) ||
                string.Equals(confidence, "none", StringComparison.OrdinalIgnoreCase))
            {
                skippedNoProposal++;
                continue;
            }

            if (!applicationIdMap.TryGetValue(legacyOid, out var targetApplicationId))
            {
                skippedMap++;
                continue;
            }

            if (!Visa2014ApplicationMigrationServicePatch.TryResolveMigrationServiceId(
                    migrationServiceName, migrationServices, out var migrationServiceId))
            {
                failed++;
                var fullNumber = row.GetValueOrDefault("ManualApplicationNumber") as string ?? legacyOid.ToString();
                errors.Add($"Application {fullNumber} ({targetApplicationId}): MigrationService not in OData — '{migrationServiceName}'");
                Console.Error.WriteLine($"ERR Application {fullNumber}: MigrationService not found for '{migrationServiceName}'");
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
                        var fullNumber = row.GetValueOrDefault("ManualApplicationNumber") as string ?? targetApplicationId.ToString();
                        Console.WriteLine($"  DRY PATCH Application {fullNumber} ({targetApplicationId}) MigrationService={migrationServiceName} [{confidence}]");
                    }
                    continue;
                }

                await api.UpdateAsync("Application", targetApplicationId, new Dictionary<string, object?>
                {
                    ["MigrationService"] = new { ID = migrationServiceId },
                });
                patched++;
            }
            catch (Exception ex)
            {
                failed++;
                errors.Add($"{targetApplicationId}: {ex.Message}");
                Console.Error.WriteLine($"ERR {targetApplicationId}: {ex.Message}");
            }
        }

        return new Visa2014ApplicationMigrationServiceInferencePatchResult
        {
            Planned = planned,
            Patched = patched,
            SkippedNoProposal = skippedNoProposal,
            SkippedNoIdMap = skippedMap,
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
