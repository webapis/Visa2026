using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Core;
using Visa2026.Blazor.Server.Services.Migration;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.MigrationImport;
using Bo = Visa2026.Module.BusinessObjects;

namespace Visa2026.DataImporter.Legacy.Visa2014;

internal sealed class Visa2014ApplicationProfileInstanceProgressOrderCorrectionResult
{
    public int ApplicationsUpdated { get; init; }
    public int ProgressRowsUpdated { get; init; }
}

internal static class Visa2014ApplicationProfileInstanceProgressOrderCorrection
{
    public static async Task<int> RunCommandAsync(IReadOnlyList<string> args, bool verbose)
    {
        var dryRun = HasArg(args, "--dry-run");
        var targetConnection = GetOptionValue(args, "--target-connection")
            ?? Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? "Host=localhost;Port=5432;Database=visa2026;Username=postgres;Password=Visa2026Local;Persist Security Info=True;EFCoreProvider=Postgres";

        Console.WriteLine("=== VISA2014 ApplicationProfileInstanceProgress workflow order correction");
        Console.WriteLine($"INF Target SQL: {MaskConnectionString(targetConnection)}");
        if (dryRun)
            Console.WriteLine("INF Mode: dry-run (no writes)");

        HeadlessMigrationHost? host = null;
        IDisposable? importScope = null;
        try
        {
            host = HeadlessMigrationHost.Start(targetConnection);
            importScope = MigrationImportContext.BeginDataImportScope();

            var result = await RunAsync(host.ObjectSpaceFactory, dryRun, verbose);

            Console.WriteLine($"INF Applications updated: {result.ApplicationsUpdated}");
            Console.WriteLine($"INF Progress rows updated: {result.ProgressRowsUpdated}");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ERR Correction failed: {ex.Message}");
            if (verbose)
                Console.Error.WriteLine(ex);
            return 1;
        }
        finally
        {
            importScope?.Dispose();
            host?.Dispose();
        }
    }

    internal static Task<Visa2014ApplicationProfileInstanceProgressOrderCorrectionResult> RunAsync(
        INonSecuredObjectSpaceFactory objectSpaceFactory,
        bool dryRun,
        bool verbose)
    {
        using var objectSpace = objectSpaceFactory.CreateNonSecuredObjectSpace(typeof(Bo.ApplicationProfileInstanceProgress));
        MigrationImportContext.ApplyImportObjectSpaceHooks(objectSpace);

        var progresses = objectSpace.GetObjectsQuery<Bo.ApplicationProfileInstanceProgress>()
            .Where(p => p.ApplicationProfileInstance != null)
            .ToList();

        var groups = progresses
            .GroupBy(p => p.ApplicationProfileInstance!.ID)
            .ToList();

        var rowsUpdated = 0;
        var applicationsUpdated = 0;
        foreach (var group in groups)
        {
            var siblings = group.ToList();
            var before = siblings.ToDictionary(p => p.ID, p => p.Order);
            ApplicationProfileInstanceProgressOrderHelper.AssignTimelineOrders(siblings);

            var groupChanged = false;
            foreach (var progress in siblings)
            {
                if (before.TryGetValue(progress.ID, out var prior) && prior == progress.Order)
                    continue;

                rowsUpdated++;
                groupChanged = true;
                if (verbose)
                {
                    Console.WriteLine(
                        $"  ORDER app={group.Key} progress={progress.ID} " +
                        $"{progress.State?.Code} {prior} -> {progress.Order}");
                }
            }

            if (groupChanged)
                applicationsUpdated++;
        }

        if (!dryRun && rowsUpdated > 0)
            objectSpace.CommitChanges();

        return Task.FromResult(new Visa2014ApplicationProfileInstanceProgressOrderCorrectionResult
        {
            ApplicationsUpdated = applicationsUpdated,
            ProgressRowsUpdated = rowsUpdated,
        });
    }

    private static string MaskConnectionString(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            return "(empty)";

        return string.Join("; ",
            connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
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