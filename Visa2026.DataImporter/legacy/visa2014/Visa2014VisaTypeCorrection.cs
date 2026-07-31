using DevExpress.ExpressApp;
using Visa2026.Blazor.Server.Services.Migration;
using Bo = Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.MigrationImport;

namespace Visa2026.DataImporter.Legacy.Visa2014;

internal sealed class Visa2014VisaTypeCorrectionResult
{
    public int VisasInScope { get; init; }
    public int Updated { get; init; }
    public int AlreadyCorrect { get; init; }
    public int SkippedMissingTarget { get; init; }
    public int UnresolvedType { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = [];
    public IReadOnlyDictionary<string, int> TargetTypeHistogram { get; init; } =
        new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
}

/// <summary>
/// Patches Visa.VisaType from the Visa transform (family-member → FM; else TypeOfVisaL:mgCode).
/// Also repairs older imports that collapsed every row to default WP when LocalizationKey was not mapped.
/// </summary>
internal static class Visa2014VisaTypeCorrection
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

        Console.WriteLine("=== VISA2014 Visa VisaType correction");
        Console.WriteLine($"INF Legacy source: {source.Id}");
        Console.WriteLine($"INF Target SQL: {MaskConnectionString(targetConnection)}");
        Console.WriteLine($"INF Visa id-map: {visaIdMapPath}");
        if (dryRun) Console.WriteLine("INF Mode: dry-run (no writes)");

        try { Visa2014LegacySqlGuard.EnsureLegacyReadCredentials(source.ConnectionString); }
        catch (Exception ex) { Console.Error.WriteLine($"ERR {ex.Message}"); return 1; }

        HeadlessMigrationHost? host = null;
        IDisposable? importScope = null;
        try
        {
            host = HeadlessMigrationHost.Start(targetConnection);
            importScope = MigrationImportContext.BeginDataImportScope();

            var resolver = new Visa2014ODataLookupResolver();
            using (var lookupSpace = host.ObjectSpaceFactory.CreateNonSecuredObjectSpace(typeof(Bo.Visa)))
            {
                MigrationImportContext.ApplyImportObjectSpaceHooks(lookupSpace);
                resolver.LoadFromObjectSpace(lookupSpace, Visa2014HeadlessImportSession.ResolveTenantCatalogDirStatic());
            }

            resolver.EnsureVisaTypeLookupKeysLoaded();

            var visaIdMap = File.Exists(visaIdMapPath)
                ? Visa2014IdMapHelper.Load(visaIdMapPath)
                : new Dictionary<Guid, Guid>();

            var result = await RunAsync(
                host.ObjectSpaceFactory,
                resolver,
                source.ConnectionString,
                source.LookupTranslationPaths,
                visaIdMap,
                dryRun,
                verbose);

            Console.WriteLine($"INF Visas in scope: {result.VisasInScope}");
            Console.WriteLine($"INF VisaType updated: {result.Updated}");
            Console.WriteLine($"INF Already correct: {result.AlreadyCorrect}");
            Console.WriteLine($"INF Missing target: {result.SkippedMissingTarget}");
            Console.WriteLine($"INF Unresolved type: {result.UnresolvedType}");
            if (result.TargetTypeHistogram.Count > 0)
            {
                var hist = string.Join(", ",
                    result.TargetTypeHistogram.OrderByDescending(kv => kv.Value).Select(kv => $"{kv.Key}={kv.Value}"));
                Console.WriteLine($"INF Result histogram (matched rows): {hist}");
            }

            foreach (var error in result.Errors.Take(30))
                Console.Error.WriteLine($"ERR {error}");
            return result.Errors.Count > 0 || result.UnresolvedType > 0 ? 1 : 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ERR Correction failed: {ex.Message}");
            if (verbose) Console.Error.WriteLine(ex);
            return 1;
        }
        finally { importScope?.Dispose(); host?.Dispose(); }
    }

    internal static Task<Visa2014VisaTypeCorrectionResult> RunAsync(
        INonSecuredObjectSpaceFactory objectSpaceFactory,
        Visa2014ODataLookupResolver resolver,
        string legacyConnectionString,
        IReadOnlyList<string> lookupTranslationPaths,
        IReadOnlyDictionary<Guid, Guid> visaIdMap,
        bool dryRun,
        bool verbose)
    {
        var errors = new List<string>();
        var batch = Visa2014VisaTransform.PrepareImportBatch(
            legacyConnectionString,
            lookupTranslationPaths,
            maxRows: null,
            verbose: verbose);

        int updated = 0, alreadyCorrect = 0, missingTarget = 0, unresolved = 0;
        var histogram = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        using var objectSpace = objectSpaceFactory.CreateNonSecuredObjectSpace(typeof(Bo.Visa));
        MigrationImportContext.ApplyImportObjectSpaceHooks(objectSpace);

        var visasById = objectSpace.GetObjectsQuery<Bo.Visa>()
            .Where(v => v.GCRecord == 0)
            .ToDictionary(v => v.ID);
        var visasByNumber = objectSpace.GetObjectsQuery<Bo.Visa>()
            .Where(v => v.GCRecord == 0)
            .AsEnumerable()
            .GroupBy(v => (v.VisaNumber ?? "").Trim(), StringComparer.OrdinalIgnoreCase)
            .Where(g => !string.IsNullOrWhiteSpace(g.Key))
            .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

        var typeCache = objectSpace.GetObjectsQuery<Bo.VisaType>()
            .AsEnumerable()
            .ToDictionary(t => t.ID);

        foreach (var row in batch.ImportRows)
        {
            var legacyOid = (Guid)row["_legacyRowId"]!;
            var localizationKey = row.GetValueOrDefault("VisaType") as string;
            var visaNumber = (row.GetValueOrDefault("VisaNumber") as string)?.Trim();

            var typeId = resolver.ResolveVisaType(localizationKey);
            if (!typeId.HasValue)
            {
                unresolved++;
                errors.Add($"{legacyOid}: cannot resolve VisaType '{localizationKey}' (composite={row.GetValueOrDefault("_legacy_VisaTypeComposite")})");
                continue;
            }

            var key = localizationKey?.Trim() ?? "?";
            histogram[key] = histogram.TryGetValue(key, out var c) ? c + 1 : 1;

            Bo.Visa? target = null;
            if (visaIdMap.TryGetValue(legacyOid, out var mappedId) &&
                visasById.TryGetValue(mappedId, out var byMap))
            {
                target = byMap;
            }
            else if (!string.IsNullOrWhiteSpace(visaNumber) &&
                     visasByNumber.TryGetValue(visaNumber, out var candidates))
            {
                target = candidates.Count == 1
                    ? candidates[0]
                    : candidates.FirstOrDefault(v => v.VisaType == null || v.VisaType.ID != typeId.Value)
                      ?? candidates[0];
            }

            if (target == null)
            {
                missingTarget++;
                if (verbose)
                    Console.WriteLine($"  SKIP {legacyOid}: no target Visa (number={visaNumber})");
                continue;
            }

            if (target.VisaType != null && target.VisaType.ID == typeId.Value)
            {
                alreadyCorrect++;
                continue;
            }

            if (!typeCache.TryGetValue(typeId.Value, out var typeRow))
            {
                unresolved++;
                errors.Add($"{legacyOid}: VisaType id {typeId} not in ObjectSpace cache");
                continue;
            }

            if (dryRun)
            {
                updated++;
                if (verbose)
                    Console.WriteLine($"  DRY {visaNumber}: {target.VisaType?.LocalizationKey ?? "(null)"} -> {key}");
                continue;
            }

            target.VisaType = typeRow;
            updated++;
            if (verbose && updated % 500 == 0)
                Console.WriteLine($"INF Progress: {updated} updated...");
        }

        if (!dryRun && updated > 0)
            objectSpace.CommitChanges();

        return Task.FromResult(new Visa2014VisaTypeCorrectionResult
        {
            VisasInScope = batch.ImportRows.Count,
            Updated = updated,
            AlreadyCorrect = alreadyCorrect,
            SkippedMissingTarget = missingTarget,
            UnresolvedType = unresolved,
            Errors = errors,
            TargetTypeHistogram = histogram,
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