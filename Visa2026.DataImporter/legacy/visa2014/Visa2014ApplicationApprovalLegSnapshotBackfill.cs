using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Core;
using Visa2026.Blazor.Server.Services.Migration;
using Bo = Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.MigrationImport;

namespace Visa2026.DataImporter.Legacy.Visa2014;

/// <summary>
/// Backfills instance approval-leg snapshots and version names from the shared
/// <see cref="Bo.ApprovalLegProfile"/> (instance FK, else template Default).
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
            Console.WriteLine($"INF Applications needing heal: {result.ApplicationsNeedingBackfill}");
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

        Bo.ApplicationProfileInstanceApprovalLegBackfill.Result healed;
        try
        {
            healed = Bo.ApplicationProfileInstanceApprovalLegBackfill.Sync(objectSpace, apply: !dryRun);
            if (!dryRun && (healed.ProfilesAssigned + healed.NamesStamped + healed.SnapshotsFilled) > 0)
                objectSpace.CommitChanges();
        }
        catch (Exception ex)
        {
            errors.Add(ex.Message);
            healed = default;
        }

        if (verbose)
        {
            Console.WriteLine(
                $"  ASSIGNED {healed.ProfilesAssigned}  NAMES {healed.NamesStamped}  SNAPSHOTS {healed.SnapshotsFilled}");
        }

        return new Visa2014ApplicationProfileInstanceApprovalLegSnapshotBackfillResult
        {
            ApplicationsScanned = healed.Scanned,
            ApplicationsNeedingBackfill = healed.ProfilesAssigned + healed.NamesStamped + healed.SnapshotsFilled,
            SnapshotsBackfilled = dryRun ? 0 : healed.SnapshotsFilled,
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