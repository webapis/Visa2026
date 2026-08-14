using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Core;
using Visa2026.Blazor.Server.Services.Migration;
using Bo = Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.MigrationImport;

namespace Visa2026.DataImporter.Legacy.Visa2014;

/// <summary>
/// Backfills <see cref="Bo.ApplicationProfileInstanceApprovalLegSnapshot"/> from <see cref="Bo.ApprovalLegProfile"/>
/// for via-ministry applications with incomplete snapshots (empty Ministrlik on progress).
/// Does <strong>not</strong> delete or regenerate <see cref="Bo.ApplicationProfileInstanceProgress"/> rows.
/// </summary>
internal sealed class Visa2014ApplicationProfileInstanceApprovalLegSnapshotBackfillResult
{
    public int ApplicationsScanned { get; init; }
    public int ApplicationsNeedingBackfill { get; init; }
    public int SnapshotsBackfilled { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = [];
}

internal static class Visa2014ApplicationProfileInstanceApprovalLegSnapshotBackfill
{
    public static async Task<int> RunCommandAsync(IReadOnlyList<string> args, bool verbose)
    {
        await Task.CompletedTask;

        var dryRun = HasArg(args, "--dry-run");
        var targetConnection = GetOptionValue(args, "--target-connection")
            ?? Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? "Host=localhost;Port=5432;Database=visa2026;Username=postgres;Password=Visa2026Local;Persist Security Info=True;EFCoreProvider=Postgres";

        Console.WriteLine("=== VISA2014 ApplicationProfileInstance ApprovalLegSnapshot backfill (Ministrlik)");
        Console.WriteLine($"INF Target SQL: {MaskConnectionString(targetConnection)}");
        if (dryRun) Console.WriteLine("INF Mode: dry-run (no writes)");

        HeadlessMigrationHost? host = null;
        IDisposable? importScope = null;
        try
        {
            host = HeadlessMigrationHost.Start(targetConnection);
            importScope = MigrationImportContext.BeginDataImportScope();

            var result = Run(host.ObjectSpaceFactory, dryRun, verbose);

            Console.WriteLine($"INF Applications scanned: {result.ApplicationsScanned}");
            Console.WriteLine($"INF Applications needing backfill: {result.ApplicationsNeedingBackfill}");
            Console.WriteLine($"INF Snapshots backfilled: {result.SnapshotsBackfilled}");
            foreach (var error in result.Errors.Take(20))
                Console.Error.WriteLine($"ERR {error}");
            return result.Errors.Count > 0 ? 1 : 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ERR Backfill failed: {ex.Message}");
            if (verbose) Console.Error.WriteLine(ex);
            return 1;
        }
        finally
        {
            importScope?.Dispose();
            host?.Dispose();
        }
    }

    internal static Visa2014ApplicationProfileInstanceApprovalLegSnapshotBackfillResult Run(
        INonSecuredObjectSpaceFactory objectSpaceFactory,
        bool dryRun,
        bool verbose)
    {
        var errors = new List<string>();
        using var objectSpace = objectSpaceFactory.CreateNonSecuredObjectSpace(typeof(Bo.ApplicationProfileInstance));
        MigrationImportContext.ApplyImportObjectSpaceHooks(objectSpace);

        var viaMinistryTypeIds = objectSpace.GetObjectsQuery<Bo.ApplicationType>()
            .Where(t => t.ApplicationProfileInstanceProgressRoute == Bo.ApplicationProfileInstanceProgressRouteKind.ViaMinistries)
            .Select(t => t.ID)
            .ToHashSet();

        var applications = objectSpace.GetObjectsQuery<Bo.ApplicationProfileInstance>()
            .Where(a => a.ApplicationType != null
                && viaMinistryTypeIds.Contains(a.ApplicationType.ID)
                && a.ApprovalLegProfile != null)
            .ToList();

        var needing = 0;
        var backfilled = 0;

        foreach (var application in applications)
        {
            try
            {
                var expectedLegs = Bo.ApprovalLegProfileMinistryHelper.GetLegCount(application.ApprovalLegProfile);
                if (expectedLegs <= 0)
                    continue;

                var snapshotLegs = application.ApprovalLegSnapshots?
                    .Count(s => !string.IsNullOrWhiteSpace(s.MinistryShortName)) ?? 0;
                if (snapshotLegs == expectedLegs)
                    continue;

                needing++;
                if (verbose)
                {
                    Console.WriteLine(
                        $"  SNAPSHOT {application.FullApplicationNumber ?? application.ID.ToString()} " +
                        $"legs {snapshotLegs} -> {expectedLegs} " +
                        $"(profile {application.ApprovalLegProfile?.Code ?? "?"})");
                }

                if (dryRun)
                    continue;

                Bo.ApprovalLegProfileMinistryHelper.ApplySnapshot(
                    objectSpace,
                    application,
                    application.ApprovalLegProfile);
                backfilled++;
            }
            catch (Exception ex)
            {
                errors.Add($"{application.FullApplicationNumber ?? application.ID.ToString()}: {ex.Message}");
            }
        }

        if (!dryRun && backfilled > 0)
            objectSpace.CommitChanges();

        return new Visa2014ApplicationProfileInstanceApprovalLegSnapshotBackfillResult
        {
            ApplicationsScanned = applications.Count,
            ApplicationsNeedingBackfill = needing,
            SnapshotsBackfilled = backfilled,
            Errors = errors,
        };
    }

    private static string MaskConnectionString(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString)) return "(empty)";
        return string.Join("; ", connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(p => !p.StartsWith("Password=", StringComparison.OrdinalIgnoreCase)
                && !p.StartsWith("Pwd=", StringComparison.OrdinalIgnoreCase)));
    }

    private static bool HasArg(IReadOnlyList<string> args, string flag) =>
        args.Any(a => string.Equals(a, flag, StringComparison.OrdinalIgnoreCase));

    private static string? GetOptionValue(IReadOnlyList<string> args, string optionName)
    {
        for (var i = 0; i < args.Count - 1; i++)
            if (string.Equals(args[i], optionName, StringComparison.OrdinalIgnoreCase)) return args[i + 1];
        return null;
    }
}