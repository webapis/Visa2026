using Microsoft.Extensions.DependencyInjection;
using Visa2026.Blazor.Server.Services.Migration;
using Visa2026.Module.DatabaseUpdate;
using Visa2026.Module.Services.MigrationImport;
using Bo = Visa2026.Module.BusinessObjects;

namespace Visa2026.DataImporter.Legacy.Visa2014;

internal sealed class Visa2014ApplicationProfileNestedTemplatePatchResult
{
    public int ProfilesInScope { get; init; }
    public int TemplatesBefore { get; init; }
    public int TemplatesAfter { get; init; }
}

/// <summary>
/// Wave 3 — sync nested <see cref="Bo.ApplicationProfileTemplate"/> from tenant JSON on target DB.
/// </summary>
internal static class Visa2014ApplicationProfileNestedTemplatePatch
{
    public static Task<int> RunCommandAsync(IReadOnlyList<string> args, bool verbose)
    {
        var dryRun = HasArg(args, "--dry-run");
        var targetConnection = GetOptionValue(args, "--target-connection")
            ?? Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? "Host=localhost;Port=5432;Database=visa2026;Username=postgres;Password=Visa2026Local;Persist Security Info=True;EFCoreProvider=Postgres";

        Console.WriteLine("=== VISA2014 ApplicationProfile nested templates PATCH (Wave 3)");
        Console.WriteLine($"INF Target SQL: {MaskConnectionString(targetConnection)}");
        if (dryRun)
            Console.WriteLine("INF Mode: dry-run (no writes)");

        HeadlessMigrationHost? host = null;
        IDisposable? importScope = null;
        try
        {
            host = HeadlessMigrationHost.Start(targetConnection);
            importScope = MigrationImportContext.BeginDataImportScope();

            using var objectSpace = host.ObjectSpaceFactory.CreateNonSecuredObjectSpace(typeof(Bo.ApplicationProfile));
            MigrationImportContext.ApplyImportObjectSpaceHooks(objectSpace);

            ApplicationProfileTenantCatalogSeedUpdater.SyncNow(objectSpace);

            var before = objectSpace.GetObjectsQuery<Bo.ApplicationProfileTemplate>().Count();
            var profiles = objectSpace.GetObjectsQuery<Bo.ApplicationProfile>().Count();
            var approvedRows = ApplicationProfileNestedTemplateTenantCatalogSeedUpdater.CountApprovedCatalogRows();

            Console.WriteLine($"INF Profiles in target DB: {profiles}");
            Console.WriteLine($"INF Approved nested-template rows in tenant JSON: {approvedRows}");
            Console.WriteLine($"INF Nested templates before: {before}");

            if (dryRun)
            {
                Console.WriteLine("INF Dry-run: no writes (approve tenant JSON rows, then re-run without --dry-run).");
                return Task.FromResult(approvedRows > 0 ? 0 : 1);
            }

            if (approvedRows == 0)
            {
                Console.Error.WriteLine(
                    "ERR No approved rows in application-profile-nested-templates*.json. " +
                    "Set SignOff to approved, rebuild Module, then re-run.");
                return Task.FromResult(1);
            }

            ApplicationProfileNestedTemplateTenantCatalogSeedUpdater.SyncNow(objectSpace);

            var after = objectSpace.GetObjectsQuery<Bo.ApplicationProfileTemplate>().Count();
            Console.WriteLine($"INF Nested templates after: {after}");

            return Task.FromResult(0);
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
