namespace Visa2026.DataImporter.Legacy.Visa2014;

internal sealed class Visa2014ApplicationApprovalLegProfilePatchResult
{
    public int Planned { get; init; }
    public int Patched { get; init; }
    public int SkippedNoProfileCode { get; init; }
    public int SkippedNoIdMap { get; init; }
    public int SkippedNotShowApprovalLegProfile { get; init; }
    public int Failed { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = [];
}

/// <summary>
/// PATCHes <see cref="Application.ApprovalLegProfile"/> on imported Applications from legacy ministry routing inference.
/// </summary>
internal static class Visa2014ApplicationApprovalLegProfilePatch
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

        Console.WriteLine("=== VISA2014 Application.ApprovalLegProfile PATCH");
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
            Console.WriteLine($"INF Skipped (not ShowApprovalLegProfile type): {result.SkippedNotShowApprovalLegProfile}");
            Console.WriteLine($"INF Skipped (no ApprovalLegProfile code): {result.SkippedNoProfileCode}");
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

    public static async Task<Visa2014ApplicationApprovalLegProfilePatchResult> RunAsync(
        ApiClient api,
        Visa2014ODataLookupResolver resolver,
        IReadOnlyList<Dictionary<string, object?>> importRows,
        IReadOnlyDictionary<Guid, Guid> applicationIdMap,
        bool dryRun,
        bool verbose)
    {
        var errors = new List<string>();
        int planned = 0, patched = 0, skippedNoCode = 0, skippedMap = 0, skippedType = 0, failed = 0;

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

            var profileCode = row.GetValueOrDefault("ApprovalLegProfile") as string;
            if (string.IsNullOrWhiteSpace(profileCode))
            {
                skippedNoCode++;
                continue;
            }

            if (!applicationIdMap.TryGetValue(legacyOid, out var targetApplicationProfileInstanceId))
            {
                skippedMap++;
                continue;
            }

            var profileId = resolver.ResolveApprovalLegProfile(profileCode);
            if (!profileId.HasValue)
            {
                failed++;
                var fullNumber = row.GetValueOrDefault("FullApplicationNumber") as string ?? legacyOid.ToString();
                errors.Add($"ApplicationProfileInstance {fullNumber} ({targetApplicationProfileInstanceId}): ApprovalLegProfile not in OData — '{profileCode}'");
                Console.Error.WriteLine($"ERR ApplicationProfileInstance {fullNumber}: ApprovalLegProfile not found for '{profileCode}'");
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
                        Console.WriteLine($"  DRY PATCH ApplicationProfileInstance {fullNumber} ({targetApplicationProfileInstanceId}) ApprovalLegProfile={profileCode}");
                    }
                    continue;
                }

                await api.UpdateAsync("Application", targetApplicationProfileInstanceId, new Dictionary<string, object?>
                {
                    ["ApprovalLegProfile"] = new { ID = profileId.Value },
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

        return new Visa2014ApplicationApprovalLegProfilePatchResult
        {
            Planned = planned,
            Patched = patched,
            SkippedNoProfileCode = skippedNoCode,
            SkippedNoIdMap = skippedMap,
            SkippedNotShowApprovalLegProfile = skippedType,
            Failed = failed,
            Errors = errors,
        };
    }

    private static bool HasArg(IReadOnlyList<string> args, string flag) =>
        args.Any(a => string.Equals(a, flag, StringComparison.OrdinalIgnoreCase));

    private static string? GetOptionValue(IReadOnlyList<string> args, string optionName)
    {
        for (var i = 0; i < args.Count - 1; i++)
        {
            if (string.Equals(args[i], optionName, StringComparison.OrdinalIgnoreCase))
                return args[i + 1];
        }

        return null;
    }
}
