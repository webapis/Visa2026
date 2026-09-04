using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Core;
using Visa2026.Blazor.Server.Services.Migration;
using Bo = Visa2026.Module.BusinessObjects;
using Visa2026.Module.DatabaseUpdate;
using Visa2026.Module.Services.MigrationImport;

namespace Visa2026.DataImporter.Legacy.Visa2014;

internal sealed class Visa2014ApplicationProfilePatchResult
{
    public int ApplicationsInScope { get; init; }
    public int Patched { get; init; }
    public int AlreadyCorrect { get; init; }
    public int SkippedNoTransform { get; init; }
    public int SkippedNoProfile { get; init; }
    public int Failed { get; init; }
    public IReadOnlyDictionary<string, int> ProfileHistogram { get; init; } =
        new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
    public IReadOnlyDictionary<string, int> SkipHistogram { get; init; } =
        new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
    public IReadOnlyList<string> Errors { get; init; } = [];
}

/// <summary>
/// Sets <see cref="Bo.ApplicationProfileInstance.ApplicationProfile"/> on already-imported applications from each
/// legacy row's translated <c>ApplicationType</c> (Wave 2 backfill).
/// </summary>
internal static class Visa2014ApplicationProfilePatch
{
    public static Task<int> RunCommandAsync(IReadOnlyList<string> args, bool verbose)
    {
        var dataImporterRoot = Visa2014ContentRoot.FindDataImporterRoot();
        if (dataImporterRoot == null)
        {
            Console.Error.WriteLine("ERR Could not locate Visa2026.DataImporter content root.");
            return Task.FromResult(1);
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
            return Task.FromResult(1);
        }

        var dryRun = HasArg(args, "--dry-run");
        var targetConnection = GetOptionValue(args, "--target-connection")
            ?? Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? "Host=localhost;Port=5432;Database=visa2026;Username=postgres;Password=Visa2026Local;Persist Security Info=True;EFCoreProvider=Postgres";
        var applicationIdMapPath = GetOptionValue(args, "--application-id-map")
            ?? source.IdMapPath(dataImporterRoot, "ApplicationProfileInstance");

        Console.WriteLine("=== VISA2014 Application.ApplicationProfile PATCH (Wave 2)");
        Console.WriteLine($"INF Legacy source: {source.Id}");
        Console.WriteLine($"INF Target SQL: {MaskConnectionString(targetConnection)}");
        Console.WriteLine($"INF ApplicationProfileInstance id-map: {applicationIdMapPath}");
        if (dryRun)
            Console.WriteLine("INF Mode: dry-run (no writes)");

        try
        {
            Visa2014LegacySqlGuard.EnsureLegacyReadCredentials(source.ConnectionString);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ERR {ex.Message}");
            return Task.FromResult(1);
        }

        if (!File.Exists(applicationIdMapPath))
        {
            Console.Error.WriteLine($"ERR ApplicationProfileInstance id-map not found: {applicationIdMapPath}");
            return Task.FromResult(1);
        }

        var batch = Visa2014ApplicationTransform.PrepareImportBatch(
            source.ConnectionString,
            source.LookupTranslationPaths,
            maxRows: null,
            verbose: verbose);
        var idMap = Visa2014IdMapHelper.Load(applicationIdMapPath);
        var targetProfileByLegacyOid = BuildTargetProfileContextByLegacyOid(batch.ImportRows);

        HeadlessMigrationHost? host = null;
        IDisposable? importScope = null;
        try
        {
            host = HeadlessMigrationHost.Start(targetConnection);
            importScope = MigrationImportContext.BeginDataImportScope();
            var result = Run(host.ObjectSpaceFactory, idMap, targetProfileByLegacyOid, dryRun, verbose);

            Console.WriteLine($"INF Applications in scope: {result.ApplicationsInScope}");
            Console.WriteLine($"INF Patched: {result.Patched}");
            Console.WriteLine($"INF Already correct: {result.AlreadyCorrect}");
            Console.WriteLine($"INF Skipped (no transform type): {result.SkippedNoTransform}");
            Console.WriteLine($"INF Skipped (no profile for type): {result.SkippedNoProfile}");
            Console.WriteLine($"INF Failed: {result.Failed}");

            if (result.ProfileHistogram.Count > 0)
            {
                Console.WriteLine("INF Profile histogram (patched rows):");
                foreach (var entry in result.ProfileHistogram.OrderByDescending(e => e.Value).ThenBy(e => e.Key, StringComparer.OrdinalIgnoreCase))
                    Console.WriteLine($"INF   {entry.Key}: {entry.Value}");
            }

            if (result.SkipHistogram.Count > 0)
            {
                Console.WriteLine("INF Skip histogram (no profile match):");
                foreach (var entry in result.SkipHistogram.OrderByDescending(e => e.Value).ThenBy(e => e.Key, StringComparer.OrdinalIgnoreCase))
                    Console.WriteLine($"INF   {entry.Value,6}  {entry.Key}");
            }

            var skipReportPath = GetOptionValue(args, "--skip-report");
            if (!string.IsNullOrWhiteSpace(skipReportPath) && result.SkipHistogram.Count > 0)
                WriteSkipReport(skipReportPath, result);

            foreach (var error in result.Errors.Take(20))
                Console.Error.WriteLine($"ERR {error}");
            if (result.Errors.Count > 20)
                Console.Error.WriteLine($"ERR ... and {result.Errors.Count - 20} more");

            return Task.FromResult(result.Failed > 0 ? 1 : 0);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ERR PATCH failed: {ex.Message}");
            if (verbose)
                Console.Error.WriteLine(ex);
            return Task.FromResult(1);
        }
        finally
        {
            importScope?.Dispose();
            host?.Dispose();
        }
    }

    private sealed record LegacyApplicationProfileContext(string TypeName, string? ProjectContractCode);

    private static Dictionary<Guid, LegacyApplicationProfileContext> BuildTargetProfileContextByLegacyOid(
        IReadOnlyList<Dictionary<string, object?>> importRows)
    {
        var map = new Dictionary<Guid, LegacyApplicationProfileContext>();
        foreach (var row in importRows)
        {
            if (row.GetValueOrDefault("_legacyRowId") is not Guid legacyOid || legacyOid == Guid.Empty)
                continue;

            var typeName = row.GetValueOrDefault("ApplicationType") as string;
            if (string.IsNullOrWhiteSpace(typeName))
                continue;

            map[legacyOid] = new LegacyApplicationProfileContext(
                typeName.Trim(),
                row.GetValueOrDefault("ProjectContract") as string);
        }

        return map;
    }

    private static Visa2014ApplicationProfilePatchResult Run(
        INonSecuredObjectSpaceFactory objectSpaceFactory,
        IReadOnlyDictionary<Guid, Guid> applicationIdMap,
        IReadOnlyDictionary<Guid, LegacyApplicationProfileContext> targetProfileByLegacyOid,
        bool dryRun,
        bool verbose)
    {
        var errors = new List<string>();
        var histogram = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var skipHistogram = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        int inScope = 0;
        int patched = 0;
        int alreadyCorrect = 0;
        int skippedTransform = 0;
        int skippedNoProfile = 0;
        int failed = 0;

        using var objectSpace = objectSpaceFactory.CreateNonSecuredObjectSpace(typeof(Bo.ApplicationProfileInstance));
        MigrationImportContext.ApplyImportObjectSpaceHooks(objectSpace);

        ApplicationProfileTenantCatalogSeedUpdater.SyncNow(objectSpace);

        var typeByName = objectSpace.GetObjectsQuery<Bo.ApplicationType>()
            .ToDictionary(t => t.Name, t => t, StringComparer.Ordinal);
        var contracts = objectSpace.GetObjectsQuery<Bo.ProjectContract>().ToList();
        var profiles = objectSpace.GetObjectsQuery<Bo.ApplicationProfile>().ToList();
        Console.WriteLine($"INF Profiles in target DB after tenant sync: {profiles.Count} (with contract FK: {profiles.Count(p => p.DefaultProjectContractId != null)})");

        foreach (var (legacyOid, targetId) in applicationIdMap)
        {
            inScope++;

            if (!targetProfileByLegacyOid.TryGetValue(legacyOid, out var targetContext))
            {
                skippedTransform++;
                continue;
            }

            if (!typeByName.ContainsKey(targetContext.TypeName))
            {
                failed++;
                errors.Add($"Legacy {legacyOid:D}: ApplicationType '{targetContext.TypeName}' not in target catalog");
                continue;
            }

            var targetProfile = Visa2014ApplicationProfileResolver.FindProfile(
                profiles,
                contracts,
                targetContext.TypeName,
                targetContext.ProjectContractCode);
            if (targetProfile == null)
            {
                skippedNoProfile++;
                var skipKey = ClassifySkipKey(targetContext, profiles, contracts);
                skipHistogram[skipKey] = skipHistogram.GetValueOrDefault(skipKey) + 1;
                if (verbose)
                {
                    Console.WriteLine(
                        $"  SKIP {targetId:D}: no ApplicationProfile for type '{targetContext.TypeName}'" +
                        (string.IsNullOrWhiteSpace(targetContext.ProjectContractCode)
                            ? string.Empty
                            : $" contract '{targetContext.ProjectContractCode}'") +
                        $" [{skipKey}]");
                }
                continue;
            }

            var application = objectSpace.GetObjectByKey<Bo.ApplicationProfileInstance>(targetId);
            if (application == null)
            {
                failed++;
                errors.Add($"Legacy {legacyOid:D}: target ApplicationProfileInstance {targetId:D} not found");
                continue;
            }

            var currentProfileId = application.ApplicationProfile?.ID;
            if (currentProfileId == targetProfile.ID)
            {
                alreadyCorrect++;
                continue;
            }

            patched++;
            histogram[targetProfile.Code] = histogram.GetValueOrDefault(targetProfile.Code) + 1;

            if (verbose)
            {
                Console.WriteLine(
                    $"  PATCH ApplicationProfileInstance {targetId:D} ({application.FullApplicationNumber}): " +
                    $"profile {application.ApplicationProfile?.Code ?? "(null)"} -> {targetProfile.Code} " +
                    $"(type {targetContext.TypeName}, legacy {legacyOid:D})");
            }

            if (!dryRun)
                application.ApplicationProfile = targetProfile;
        }

        if (!dryRun && patched > 0)
            objectSpace.CommitChanges();

        return new Visa2014ApplicationProfilePatchResult
        {
            ApplicationsInScope = inScope,
            Patched = patched,
            AlreadyCorrect = alreadyCorrect,
            SkippedNoTransform = skippedTransform,
            SkippedNoProfile = skippedNoProfile,
            Failed = failed,
            ProfileHistogram = histogram,
            SkipHistogram = skipHistogram,
            Errors = errors,
        };
    }

    private static string ClassifySkipKey(
        LegacyApplicationProfileContext context,
        IReadOnlyList<Bo.ApplicationProfile> profiles,
        IReadOnlyList<Bo.ProjectContract> contracts)
    {
        var typeName = context.TypeName;
        var contractCode = context.ProjectContractCode?.Trim();

        if (!ApplicationProfileCatalogPreviewHelper.TryBuild(typeName, out _))
            return $"UNKNOWN_TYPE|{typeName}|{contractCode ?? ""}";

        if (!ApplicationProfileCatalogGrouping.TryResolveGroupKey(typeName, contractCode, out var groupKey))
            return $"NO_GROUP_KEY|{typeName}|{contractCode ?? ""}";

        var profileCode = Visa2014ApplicationProfileResolver.ResolveProfileCodeByTypeName(typeName);
        if (string.IsNullOrWhiteSpace(profileCode))
            return $"NO_PROFILE_CODE|{typeName}|{contractCode ?? ""}";

        var catalogKey = groupKey.CatalogKey;
        var hasCode = profiles.Any(p =>
            string.Equals(p.Code, profileCode, StringComparison.OrdinalIgnoreCase));
        if (!hasCode)
            return $"MISSING_DB_PROFILE|{profileCode}|{catalogKey}";

        var contract = string.IsNullOrWhiteSpace(groupKey.ProjectContractCode)
            ? null
            : contracts.FirstOrDefault(c =>
                (c.NameTm?.StartsWith(groupKey.ProjectContractCode, StringComparison.OrdinalIgnoreCase) ?? false)
                || string.Equals(c.Code, groupKey.ProjectContractCode, StringComparison.OrdinalIgnoreCase));

        var contractProfiles = profiles
            .Where(p => string.Equals(p.Code, profileCode, StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (contractProfiles.Count == 0)
            return $"MISSING_DB_PROFILE|{profileCode}|{catalogKey}";

        if (groupKey.Granularity == ApplicationProfileCatalogGranularity.TypeAndContract)
        {
            if (contract == null)
                return $"CONTRACT_NOT_IN_DB|{profileCode}|{catalogKey}";

            var hasContractVariant = contractProfiles.Any(p =>
                ApplicationProfileCatalogGroupKey.ProfileMatchesLegacyContract(
                    p,
                    contract.ID,
                    groupKey.ProjectContractCode));
            if (!hasContractVariant)
                return $"CONTRACT_VARIANT_GAP|{profileCode}|{catalogKey}";
        }
        else
        {
            var hasTypeOnly = contractProfiles.Any(p =>
                p.DefaultProjectContractId == null
                && !ApplicationProfileCatalogGroupKey.NameLooksLikeContractVariant(p.Name));
            if (!hasTypeOnly)
                return $"TYPE_ONLY_GAP|{profileCode}|{catalogKey}";
        }

        return $"UNRESOLVED|{profileCode}|{catalogKey}";
    }

    private static void WriteSkipReport(string path, Visa2014ApplicationProfilePatchResult result)
    {
        var lines = new List<string>
        {
            "# Application Profile patch — skip histogram",
            "",
            $"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC",
            "",
            $"Applications in scope: {result.ApplicationsInScope}",
            $"Skipped (no profile): {result.SkippedNoProfile}",
            $"Skipped (no transform): {result.SkippedNoTransform}",
            "",
            "| Count | Bucket |",
            "|------:|--------|",
        };

        foreach (var entry in result.SkipHistogram.OrderByDescending(e => e.Value).ThenBy(e => e.Key, StringComparer.OrdinalIgnoreCase))
            lines.Add($"| {entry.Value} | `{entry.Key}` |");

        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

        File.WriteAllLines(path, lines);
        Console.WriteLine($"INF Skip report written: {path}");
    }

    private static string MaskConnectionString(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            return "(empty)";
        return System.Text.RegularExpressions.Regex.Replace(
            connectionString,
            @"(Password|Pwd)\s*=\s*[^;]+",
            "$1=***",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    }

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
