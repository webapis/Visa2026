using Visa2026.Blazor.Server.Services.Migration;
using Visa2026.Module.Services.MigrationImport;
using Bo = Visa2026.Module.BusinessObjects;

namespace Visa2026.DataImporter.Legacy.Visa2014;

internal sealed class Visa2014ApplicationBusinessTripCaseSummaryPatchResult
{
    public int Planned { get; init; }
    public int Patched { get; init; }
    public int SkippedNoIdMap { get; init; }
    public int SkippedNoFields { get; init; }
    public int Failed { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = [];
}

/// <summary>
/// PATCHes Case summary From*/To* geo and business-trip destination
/// (Type + Lodging/Hotel/Hospital/OtherSite or private-house text) on imported
/// ApplicationProfileInstance rows. To* prefers BusinessTripDestination then
/// NewRegistrationLocation; From* from PreviousRegistrationLocation.
/// Purpose has no legacy source on E:13; App_Business_Trip_Departure gets dummy
/// Purpose "İs maksatly". Does not invent From* when PreviousRegistrationLocation is null.
/// </summary>
internal static class Visa2014ApplicationBusinessTripCaseSummaryPatch
{
    private static readonly string[] DefaultTypes =
    [
        "App_Business_Trip_Departure",
        "App_Business_Trip_Arrival",
        "App_Reg_Check_In_Internal",
        "App_Reg_Check_Out_Internal",
    ];

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
        var filterType = GetOptionValue(args, "--application-type");
        var types = string.IsNullOrWhiteSpace(filterType) ? DefaultTypes : [filterType.Trim()];

        Console.WriteLine("=== VISA2014 ApplicationProfileInstance business-trip case-summary PATCH");
        Console.WriteLine($"INF Legacy source: {source.Id}");
        Console.WriteLine($"INF Target SQL: {MaskConnectionString(targetConnection)}");
        Console.WriteLine($"INF ApplicationProfileInstance id-map: {applicationIdMapPath}");
        Console.WriteLine($"INF Types: {string.Join(", ", types)}");
        if (dryRun)
            Console.WriteLine("INF Mode: dry-run (no writes)");

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

        if (!File.Exists(applicationIdMapPath))
        {
            Console.Error.WriteLine($"ERR ApplicationProfileInstance id-map not found: {applicationIdMapPath}");
            return 1;
        }

        HeadlessMigrationHost? host = null;
        IDisposable? importScope = null;
        try
        {
            host = HeadlessMigrationHost.Start(targetConnection);
            importScope = MigrationImportContext.BeginDataImportScope();

            var resolver = new Visa2014ODataLookupResolver();
            using (var lookupSpace = host.ObjectSpaceFactory.CreateNonSecuredObjectSpace(typeof(Bo.ApplicationProfileInstance)))
            {
                MigrationImportContext.ApplyImportObjectSpaceHooks(lookupSpace);
                resolver.LoadFromObjectSpace(lookupSpace, Visa2014HeadlessImportSession.ResolveTenantCatalogDirStatic());
            }

            var applicationIdMap = Visa2014IdMapHelper.Load(applicationIdMapPath);
            using var target = new Visa2014ObjectSpaceImportTarget(host.ObjectSpaceFactory, batchSize: 50);
            var result = await RunAsync(
                source,
                resolver,
                target,
                applicationIdMap,
                types,
                dryRun,
                verbose);

            if (!dryRun)
                await target.FlushAsync();

            Console.WriteLine($"INF Planned: {result.Planned}");
            Console.WriteLine($"INF Patched: {result.Patched}");
            Console.WriteLine($"INF Skipped (no id-map): {result.SkippedNoIdMap}");
            Console.WriteLine($"INF Skipped (no city/address): {result.SkippedNoFields}");
            Console.WriteLine($"INF Failed: {result.Failed}");
            foreach (var error in result.Errors.Take(20))
                Console.Error.WriteLine($"ERR {error}");

            return result.Failed > 0 ? 1 : 0;
        }
        finally
        {
            importScope?.Dispose();
            host?.Dispose();
        }
    }

    private static async Task<Visa2014ApplicationBusinessTripCaseSummaryPatchResult> RunAsync(
        Visa2014LegacySourceProfile source,
        Visa2014ODataLookupResolver resolver,
        IVisa2014ImportTarget target,
        IReadOnlyDictionary<Guid, Guid> applicationIdMap,
        IReadOnlyList<string> types,
        bool dryRun,
        bool verbose)
    {
        var errors = new List<string>();
        int planned = 0, patched = 0, skippedMap = 0, skippedFields = 0, failed = 0;

        foreach (var typeName in types)
        {
            var batch = Visa2014ApplicationTransform.PrepareImportBatch(
                source.ConnectionString,
                source.LookupTranslationPaths,
                maxRows: null,
                verbose,
                typeName);

            foreach (var row in batch.ImportRows)
            {
                if (row.GetValueOrDefault("_legacyRowId") is not Guid legacyOid || legacyOid == Guid.Empty)
                    continue;

                if (!applicationIdMap.TryGetValue(legacyOid, out var targetId))
                {
                    skippedMap++;
                    continue;
                }

                var payload = new Dictionary<string, object?>(StringComparer.Ordinal);
                if (dryRun)
                {
                    var dryTarget = new Visa2014DryRunImportTarget();
                    await Visa2014ApplicationODataImporter.TryAddCaseSummaryFieldsAsync(
                        payload, row, resolver, dryTarget);
                }
                else
                {
                    await Visa2014ApplicationODataImporter.TryAddCaseSummaryFieldsAsync(
                        payload, row, resolver, target);
                }

                if (payload.Count == 0)
                {
                    skippedFields++;
                    continue;
                }

                planned++;
                var fullNumber = row.GetValueOrDefault("FullApplicationNumber") as string ?? targetId.ToString();
                try
                {
                    if (dryRun)
                    {
                        patched++;
                        if (verbose)
                            Console.WriteLine($"  DRY PATCH ApplicationProfileInstance {fullNumber} ({targetId}) fields={payload.Count}");
                        continue;
                    }

                    await target.UpdateAsync(typeof(Bo.ApplicationProfileInstance), targetId, payload);
                    patched++;
                    if (verbose)
                        Console.WriteLine($"  PATCH ApplicationProfileInstance {fullNumber} ({targetId})");
                }
                catch (Exception ex)
                {
                    failed++;
                    errors.Add($"{fullNumber}: {ex.Message}");
                    Console.Error.WriteLine($"ERR {fullNumber}: {ex.Message}");
                }
            }
        }

        return new Visa2014ApplicationBusinessTripCaseSummaryPatchResult
        {
            Planned = planned,
            Patched = patched,
            SkippedNoIdMap = skippedMap,
            SkippedNoFields = skippedFields,
            Failed = failed,
            Errors = errors,
        };
    }

    private static string MaskConnectionString(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            return "(empty)";
        return string.Join("; ", connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(p => !p.StartsWith("Password=", StringComparison.OrdinalIgnoreCase)
                && !p.StartsWith("Pwd=", StringComparison.OrdinalIgnoreCase)));
    }

    private static bool HasArg(IReadOnlyList<string> args, string flag) =>
        args.Any(a => string.Equals(a, flag, StringComparison.OrdinalIgnoreCase));

    private static string? GetOptionValue(IReadOnlyList<string> args, string optionName)
    {
        for (var i = 0; i < args.Count - 1; i++)
            if (string.Equals(args[i], optionName, StringComparison.OrdinalIgnoreCase))
                return args[i + 1];
        return null;
    }
}