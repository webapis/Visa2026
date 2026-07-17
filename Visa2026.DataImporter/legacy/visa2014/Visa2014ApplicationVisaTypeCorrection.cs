using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Core;
using Visa2026.Blazor.Server.Services.Migration;
using Bo = Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.MigrationImport;

namespace Visa2026.DataImporter.Legacy.Visa2014;

internal sealed class Visa2014ApplicationVisaTypeCorrectionResult
{
    public int ApplicationsInScope { get; init; }
    public int Updated { get; init; }
    public int AlreadyCorrect { get; init; }
    public int SkippedNoInference { get; init; }
    public int UnresolvedVisaType { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = [];
}

/// <summary>
/// Patches <see cref="Bo.Application.VisaType"/> from ApplicationType.Name using
/// <see cref="Visa2014ApplicationVisaTypeInference"/> (legacy has no Application.VisaType FK).
/// </summary>
internal static class Visa2014ApplicationVisaTypeCorrection
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
            ?? "Server=(localdb)\\mssqllocaldb;Database=Visa2026;Trusted_Connection=True;";

        Console.WriteLine("=== VISA2014 Application VisaType inference correction");
        Console.WriteLine($"INF Legacy source: {source.Id}");
        Console.WriteLine($"INF Target SQL: {MaskConnectionString(targetConnection)}");
        if (dryRun) Console.WriteLine("INF Mode: dry-run (no writes)");

        HeadlessMigrationHost? host = null;
        IDisposable? importScope = null;
        try
        {
            host = HeadlessMigrationHost.Start(targetConnection);
            importScope = MigrationImportContext.BeginDataImportScope();

            var resolver = new Visa2014ODataLookupResolver();
            using (var lookupSpace = host.ObjectSpaceFactory.CreateNonSecuredObjectSpace(typeof(Bo.Application)))
            {
                MigrationImportContext.ApplyImportObjectSpaceHooks(lookupSpace);
                resolver.LoadFromObjectSpace(lookupSpace, Visa2014HeadlessImportSession.ResolveTenantCatalogDirStatic());
            }

            resolver.EnsureVisaTypeLookupKeysLoaded();

            var result = Run(host.ObjectSpaceFactory, resolver, dryRun, verbose);
            Console.WriteLine($"INF In scope: {result.ApplicationsInScope}");
            Console.WriteLine($"INF Updated: {result.Updated}");
            Console.WriteLine($"INF Already correct: {result.AlreadyCorrect}");
            Console.WriteLine($"INF Skipped (no inference rule): {result.SkippedNoInference}");
            Console.WriteLine($"INF Unresolved VisaType key: {result.UnresolvedVisaType}");
            foreach (var error in result.Errors.Take(20))
                Console.Error.WriteLine($"ERR {error}");

            return Task.FromResult(result.Errors.Count > 0 || result.UnresolvedVisaType > 0 ? 1 : 0);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ERR Correction failed: {ex.Message}");
            if (verbose) Console.Error.WriteLine(ex);
            return Task.FromResult(1);
        }
        finally
        {
            importScope?.Dispose();
            host?.Dispose();
        }
    }

    internal static Visa2014ApplicationVisaTypeCorrectionResult Run(
        INonSecuredObjectSpaceFactory objectSpaceFactory,
        Visa2014ODataLookupResolver resolver,
        bool dryRun,
        bool verbose)
    {
        var errors = new List<string>();
        var updated = 0;
        var alreadyCorrect = 0;
        var skippedNoInference = 0;
        var unresolved = 0;

        using var objectSpace = objectSpaceFactory.CreateNonSecuredObjectSpace(typeof(Bo.Application));
        MigrationImportContext.ApplyImportObjectSpaceHooks(objectSpace);

        var applications = objectSpace.GetObjectsQuery<Bo.Application>()
            .Where(a => a.ApplicationType != null)
            .ToList();

        var visaTypeCache = new Dictionary<string, Bo.VisaType?>(StringComparer.OrdinalIgnoreCase);
        foreach (var application in applications)
        {
            var typeName = application.ApplicationType?.Name;
            if (!Visa2014ApplicationVisaTypeInference.TryInferVisaType(typeName, out var key))
            {
                skippedNoInference++;
                continue;
            }

            if (!visaTypeCache.TryGetValue(key, out var targetVisaType))
            {
                var id = resolver.ResolveVisaType(key);
                targetVisaType = id.HasValue
                    ? objectSpace.GetObjectByKey<Bo.VisaType>(id.Value)
                    : null;
                visaTypeCache[key] = targetVisaType;
            }

            if (targetVisaType == null)
            {
                unresolved++;
                errors.Add($"Application {application.ID} ({application.FullApplicationNumber}): VisaType key '{key}' not found");
                continue;
            }

            if (application.VisaType != null
                && application.VisaType.ID == targetVisaType.ID)
            {
                alreadyCorrect++;
                continue;
            }

            if (verbose)
            {
                var from = application.VisaType?.LocalizationKey ?? "(null)";
                Console.WriteLine(
                    $"INF {application.FullApplicationNumber}: {typeName} VisaType {from} → {key}");
            }

            if (!dryRun)
                application.VisaType = targetVisaType;
            updated++;
        }

        if (!dryRun && updated > 0)
            objectSpace.CommitChanges();

        return new Visa2014ApplicationVisaTypeCorrectionResult
        {
            ApplicationsInScope = applications.Count,
            Updated = updated,
            AlreadyCorrect = alreadyCorrect,
            SkippedNoInference = skippedNoInference,
            UnresolvedVisaType = unresolved,
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
