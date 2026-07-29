using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Core;
using Visa2026.Blazor.Server.Services.Migration;
using Bo = Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.MigrationImport;

namespace Visa2026.DataImporter.Legacy.Visa2014;

internal sealed class Visa2014ApplicationTypeCompositeCorrectionResult
{
    public int ApplicationsInScope { get; init; }
    public int Retyped { get; init; }
    public int AlreadyCorrect { get; init; }
    public int SkippedTransform { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = [];
}

/// <summary>
/// Re-applies ApplicationType from legacy SubType enum composite (TypeOfApplicationForEmployee),
/// not the parallel TypeOfApplicationForEmployeeID column shown incorrectly in some audits.
/// </summary>
internal static class Visa2014ApplicationTypeCompositeCorrection
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
        try { source = Visa2014LegacySource.Resolve(dataImporterRoot, solutionRoot, args); }
        catch (Exception ex) { Console.Error.WriteLine($"ERR {ex.Message}"); return Task.FromResult(1); }

        var dryRun = HasArg(args, "--dry-run");
        var targetConnection = GetOptionValue(args, "--target-connection")
            ?? Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? "Host=localhost;Port=5432;Database=visa2026;Username=postgres;Password=Visa2026Local;Persist Security Info=True;EFCoreProvider=Postgres";
        var applicationIdMapPath = GetOptionValue(args, "--application-id-map")
            ?? source.IdMapPath(dataImporterRoot, "Application");

        Console.WriteLine("=== VISA2014 Application ApplicationType composite correction");
        Console.WriteLine($"INF Legacy source: {source.Id}");
        Console.WriteLine($"INF Target SQL: {MaskConnectionString(targetConnection)}");
        Console.WriteLine($"INF Application id-map: {applicationIdMapPath}");
        if (dryRun) Console.WriteLine("INF Mode: dry-run (no writes)");

        if (!File.Exists(applicationIdMapPath))
        {
            Console.Error.WriteLine($"ERR Application id-map not found: {applicationIdMapPath}");
            return Task.FromResult(1);
        }

        try
        {
            Visa2014LegacySqlGuard.EnsureLegacyReadCredentials(source.ConnectionString);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ERR {ex.Message}");
            return Task.FromResult(1);
        }

        var batch = Visa2014ApplicationTransform.PrepareImportBatch(
            source.ConnectionString,
            source.LookupTranslationPaths,
            maxRows: null,
            verbose: verbose);
        var idMap = Visa2014IdMapHelper.Load(applicationIdMapPath);
        var targetTypeByLegacyOid = BuildTargetTypeByLegacyOid(batch.ImportRows);

        HeadlessMigrationHost? host = null;
        IDisposable? importScope = null;
        try
        {
            host = HeadlessMigrationHost.Start(targetConnection);
            importScope = MigrationImportContext.BeginDataImportScope();

            var result = Run(host.ObjectSpaceFactory, idMap, targetTypeByLegacyOid, dryRun, verbose);

            Console.WriteLine($"INF Applications in scope: {result.ApplicationsInScope}");
            Console.WriteLine($"INF ApplicationType retyped: {result.Retyped}");
            Console.WriteLine($"INF Already correct: {result.AlreadyCorrect}");
            Console.WriteLine($"INF Skipped (transform skip/unmapped): {result.SkippedTransform}");
            foreach (var error in result.Errors.Take(20))
                Console.Error.WriteLine($"ERR {error}");

            return Task.FromResult(result.Errors.Count > 0 ? 1 : 0);
        }
        finally
        {
            importScope?.Dispose();
            host?.Dispose();
        }
    }

    private static Dictionary<Guid, string> BuildTargetTypeByLegacyOid(IReadOnlyList<Dictionary<string, object?>> importRows)
    {
        var map = new Dictionary<Guid, string>();
        foreach (var row in importRows)
        {
            if (row.GetValueOrDefault("_importAction") as string == "skip")
                continue;

            var legacyOid = (Guid)row["_legacyRowId"]!;
            var typeName = row.GetValueOrDefault("ApplicationType") as string;
            if (string.IsNullOrWhiteSpace(typeName))
                continue;

            map[legacyOid] = typeName;
        }

        return map;
    }

    private static Visa2014ApplicationTypeCompositeCorrectionResult Run(
        INonSecuredObjectSpaceFactory objectSpaceFactory,
        IReadOnlyDictionary<Guid, Guid> applicationIdMap,
        IReadOnlyDictionary<Guid, string> targetTypeByLegacyOid,
        bool dryRun,
        bool verbose)
    {
        var errors = new List<string>();
        int inScope = 0;
        int retyped = 0;
        int alreadyCorrect = 0;
        int skippedTransform = 0;

        using var objectSpace = objectSpaceFactory.CreateNonSecuredObjectSpace(typeof(Bo.Application));
        MigrationImportContext.ApplyImportObjectSpaceHooks(objectSpace);

        var typeByName = objectSpace.GetObjectsQuery<Bo.ApplicationType>()
            .ToDictionary(t => t.Name, t => t, StringComparer.Ordinal);

        foreach (var (legacyOid, targetId) in applicationIdMap)
        {
            inScope++;

            if (!targetTypeByLegacyOid.TryGetValue(legacyOid, out var targetTypeName))
            {
                skippedTransform++;
                continue;
            }

            if (!typeByName.TryGetValue(targetTypeName, out var targetType))
            {
                errors.Add($"Legacy {legacyOid:D}: ApplicationType '{targetTypeName}' not in target catalog");
                continue;
            }

            var application = objectSpace.GetObjectByKey<Bo.Application>(targetId);
            if (application == null)
            {
                errors.Add($"Legacy {legacyOid:D}: target Application {targetId:D} not found");
                continue;
            }

            var currentName = application.ApplicationType?.Name;
            if (string.Equals(currentName, targetTypeName, StringComparison.Ordinal))
            {
                alreadyCorrect++;
                continue;
            }

            retyped++;
            if (verbose)
            {
                Console.WriteLine(
                    $"  RETYPE Application {targetId:D} ({application.FullApplicationNumber}): " +
                    $"{currentName ?? "(null)"} -> {targetTypeName} (legacy {legacyOid:D})");
            }

            if (!dryRun)
                application.ApplicationType = targetType;
        }

        if (!dryRun && retyped > 0)
            objectSpace.CommitChanges();

        return new Visa2014ApplicationTypeCompositeCorrectionResult
        {
            ApplicationsInScope = inScope,
            Retyped = retyped,
            AlreadyCorrect = alreadyCorrect,
            SkippedTransform = skippedTransform,
            Errors = errors,
        };
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