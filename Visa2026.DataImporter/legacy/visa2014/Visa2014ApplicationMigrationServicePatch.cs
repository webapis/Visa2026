namespace Visa2026.DataImporter.Legacy.Visa2014;

internal sealed class Visa2014ApplicationMigrationServicePatchResult
{
    public int Planned { get; init; }
    public int Patched { get; init; }
    public int SkippedAlreadySet { get; init; }
    public int SkippedNoIdMap { get; init; }
    public int SkippedNoMigrationService { get; init; }
    public int Failed { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = [];
}

/// <summary>
/// PATCHes <see cref="Application.MigrationService"/> on already-imported Applications using the same
/// transform + lookup-translations pipeline as Excel preview (approved MigrationService mapping).
/// </summary>
internal static class Visa2014ApplicationMigrationServicePatch
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
        int? maxRows = int.TryParse(GetOptionValue(args, "--max-rows"), out var max) ? max : null;

        Console.WriteLine("=== VISA2014 Application.MigrationService PATCH");
        Console.WriteLine($"INF Legacy source: {source.Id}");
        Console.WriteLine($"INF Target API: {apiBaseUrl}");
        Console.WriteLine($"INF ApplicationProfileInstance id-map: {applicationIdMapPath}");
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
            Console.Error.WriteLine($"ERR ApplicationProfileInstance id-map not found: {applicationIdMapPath}");
            return 1;
        }

        var batch = Visa2014ApplicationTransform.PrepareImportBatch(
            connectionString,
            source.LookupTranslationPaths,
            maxRows,
            verbose);

        var idMap = Visa2014IdMapHelper.Load(applicationIdMapPath);
        Console.WriteLine($"INF ApplicationProfileInstance id-map entries: {idMap.Count}");
        Console.WriteLine($"INF Transform import rows: {batch.ImportRows.Count}");

        var api = new ApiClient(apiBaseUrl, userName, password) { Verbose = verbose };

        if (!noWait)
            await api.WaitForServerAsync();
        await api.LoginAsync();

        try
        {
            var result = await RunAsync(api, batch.ImportRows, idMap, dryRun, verbose);

            Console.WriteLine($"INF Planned PATCH: {result.Planned}");
            Console.WriteLine($"INF Patched: {result.Patched}");
            Console.WriteLine($"INF Skipped (already set): {result.SkippedAlreadySet}");
            Console.WriteLine($"INF Skipped (no id-map): {result.SkippedNoIdMap}");
            Console.WriteLine($"INF Skipped (no MigrationService in transform): {result.SkippedNoMigrationService}");
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

    public static async Task<Visa2014ApplicationMigrationServicePatchResult> RunAsync(
        ApiClient api,
        IReadOnlyList<Dictionary<string, object?>> importRows,
        IReadOnlyDictionary<Guid, Guid> applicationIdMap,
        bool dryRun,
        bool verbose)
    {
        var migrationServices = await api.GetAllAsync<MigrationService>("MigrationService", "?$top=10000");
        Console.WriteLine($"INF OData MigrationService rows: {migrationServices.Count}");

        var errors = new List<string>();
        int planned = 0, patched = 0, skippedSet = 0, skippedMap = 0, skippedNoMs = 0, failed = 0;

        foreach (var row in importRows)
        {
            if (row.GetValueOrDefault("_legacyRowId") is not Guid legacyOid || legacyOid == Guid.Empty)
                continue;

            var migrationServiceName = row.GetValueOrDefault("MigrationService") as string;
            if (string.IsNullOrWhiteSpace(migrationServiceName))
            {
                skippedNoMs++;
                continue;
            }

            if (!applicationIdMap.TryGetValue(legacyOid, out var targetApplicationProfileInstanceId))
            {
                skippedMap++;
                continue;
            }

            if (!TryResolveMigrationServiceId(migrationServiceName, migrationServices, out var migrationServiceId))
            {
                failed++;
                var legacyCode = row.GetValueOrDefault("_legacy_DepartmentForRegistration") as string ?? "?";
                errors.Add($"ApplicationProfileInstance {targetApplicationProfileInstanceId}: MigrationService not in OData — '{migrationServiceName}' (legacy {legacyCode})");
                Console.Error.WriteLine($"ERR ApplicationProfileInstance {targetApplicationProfileInstanceId}: MigrationService not found for '{migrationServiceName}'");
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
                        var fullNumber = row.GetValueOrDefault("FullApplicationNumber") as string ?? targetApplicationProfileInstanceId.ToString();
                        Console.WriteLine($"  DRY PATCH ApplicationProfileInstance {fullNumber} ({targetApplicationProfileInstanceId}) MigrationService={migrationServiceName}");
                    }
                    continue;
                }

                await api.UpdateAsync("Application", targetApplicationProfileInstanceId, new Dictionary<string, object?>
                {
                    ["MigrationService"] = new { ID = migrationServiceId },
                });
                patched++;

                if (patched % 500 == 0)
                    Console.WriteLine($"INF Progress: {patched} patched...");
            }
            catch (Exception ex)
            {
                failed++;
                errors.Add($"{targetApplicationProfileInstanceId}: {ex.Message}");
                Console.Error.WriteLine($"ERR {targetApplicationProfileInstanceId}: {ex.Message}");
            }
        }

        return new Visa2014ApplicationMigrationServicePatchResult
        {
            Planned = planned,
            Patched = patched,
            SkippedAlreadySet = skippedSet,
            SkippedNoIdMap = skippedMap,
            SkippedNoMigrationService = skippedNoMs,
            Failed = failed,
            Errors = errors,
        };
    }

    internal static bool TryResolveMigrationServiceId(
        string targetNameTm,
        IReadOnlyList<MigrationService> rows,
        out Guid id)
    {
        id = Guid.Empty;
        var trimmed = targetNameTm.Trim();

        foreach (var row in rows)
        {
            if (row.Id == Guid.Empty)
                continue;

            if (Visa2014CatalogMatchHelper.KeysEqual(row.NameTm, trimmed))
            {
                id = row.Id;
                return true;
            }
        }

        return false;
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
