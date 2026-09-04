using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Visa2026.Blazor.Server.Services.Migration;
using Visa2026.Module.DatabaseUpdate;
using Visa2026.Module.DatabaseUpdate.LookupCatalogs;
using Visa2026.Module.Services.MigrationImport;
using Visa2026.Module.Services.UserReports;
using Bo = Visa2026.Module.BusinessObjects;

namespace Visa2026.DataImporter.Legacy.Visa2014;

internal static class Visa2014ApplicationProfileNestedTemplateTenantCatalogExporter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = null,
    };

    public static string ExportTenantJson(string targetConnection, string outputPath, bool verbose)
    {
        HeadlessMigrationHost? host = null;
        IDisposable? importScope = null;
        try
        {
            host = HeadlessMigrationHost.Start(targetConnection);
            importScope = MigrationImportContext.BeginDataImportScope();
            using var objectSpace = host.ObjectSpaceFactory.CreateNonSecuredObjectSpace(typeof(Bo.ApplicationProfile));
            MigrationImportContext.ApplyImportObjectSpaceHooks(objectSpace);

            ApplicationProfileTenantCatalogSeedUpdater.SyncNow(objectSpace);

            var visibility = new UserReportVisibilityService();
            var proposals = ApplicationProfileNestedTemplateProposalBuilder.BuildForTenantCatalog(
                objectSpace,
                visibility);

            var rows = proposals
                .Select(ApplicationProfileNestedTemplateTenantCatalogRow.FromProposal)
                .ToList();

            var catalog = new ApplicationProfileNestedTemplateTenantCatalogFile { Rows = rows };
            var json = JsonSerializer.Serialize(catalog, JsonOptions);
            var utf8 = new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
            File.WriteAllText(Path.GetFullPath(outputPath), json, utf8);

            if (verbose)
                Console.WriteLine($"INF Nested template rows: {rows.Count}");

            Console.WriteLine($"INF Wrote tenant JSON: {Path.GetFullPath(outputPath)}");
            return Path.GetFullPath(outputPath);
        }
        finally
        {
            importScope?.Dispose();
            host?.Dispose();
        }
    }
}
