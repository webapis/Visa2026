using Microsoft.Extensions.DependencyInjection;
using Visa2026.Blazor.Server.Services.Migration;
using Visa2026.Module.DatabaseUpdate;
using Visa2026.Module.DatabaseUpdate.LookupCatalogs;
using Visa2026.Module.Services.MigrationImport;
using Visa2026.Module.Services.UserReports;
using Bo = Visa2026.Module.BusinessObjects;

namespace Visa2026.DataImporter.Legacy.Visa2014;

/// <summary>
/// Wave 3 — Excel proposal: nested <see cref="Bo.ApplicationProfileTemplate"/> rows per profile catalog key.
/// </summary>
internal static class Visa2014ApplicationProfileNestedTemplateExporter
{
    private static readonly string[] ColumnOrder =
    [
        "ProfileCatalogKey", "ApplicationTypeName", "DefaultProjectContractCode", "ProfileCode",
        "TemplateName", "TemplateKind", "SortOrder", "RootBoType",
        "Decision", "SignOff",
    ];

    public static Visa2014PreviewExportResult Export(string targetConnection, string outputPath, bool verbose)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outputPath))!);

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

            var profileKeys = proposals
                .Select(p => p.ProfileCatalogKey)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count();
            var templateNames = proposals
                .Select(p => p.TemplateName)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count();

            var rows = proposals
                .Select(p => new Dictionary<string, object?>(StringComparer.Ordinal)
                {
                    ["ProfileCatalogKey"] = p.ProfileCatalogKey,
                    ["ApplicationTypeName"] = p.ApplicationTypeName,
                    ["DefaultProjectContractCode"] = p.DefaultProjectContractCode ?? string.Empty,
                    ["ProfileCode"] = p.ProfileCode,
                    ["TemplateName"] = p.TemplateName,
                    ["TemplateKind"] = p.TemplateKind.ToString(),
                    ["SortOrder"] = p.SortOrder,
                    ["RootBoType"] = p.RootBoType,
                    ["Decision"] = string.Empty,
                    ["SignOff"] = string.Empty,
                })
                .ToList<IReadOnlyDictionary<string, object?>>();

            var profilesWithoutTemplates = ApplicationProfileTenantCatalogLoader.TryLoadRows(out var catalogRows)
                ? catalogRows
                    .Select(r => !string.IsNullOrWhiteSpace(r.ProfileCatalogKey)
                        ? r.ProfileCatalogKey!
                        : ApplicationProfileCatalogGroupKey.BuildCatalogKey(
                            r.ApplicationTypeName,
                            r.DefaultProjectContractCode))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Except(proposals.Select(p => p.ProfileCatalogKey), StringComparer.OrdinalIgnoreCase)
                    .OrderBy(k => k, StringComparer.OrdinalIgnoreCase)
                    .Select(k => new Dictionary<string, object?>(StringComparer.Ordinal)
                    {
                        ["ProfileCatalogKey"] = k,
                        ["Note"] = "No UserReportTemplate visibility match (review type/group/contract filters).",
                    })
                    .Cast<IReadOnlyDictionary<string, object?>>()
                    .ToList()
                : [];

            var metaRows = new List<IReadOnlyDictionary<string, object?>>
            {
                Meta("exportedAt", DateTime.UtcNow.ToString("O")),
                Meta("entity", "ApplicationProfileNestedTemplates"),
                Meta("wave", "3-proposal"),
                Meta("targetDatabase", MaskConnection(targetConnection)),
                Meta("profileCatalogKeys", profileKeys),
                Meta("distinctTemplateNames", templateNames),
                Meta("nestedTemplateRowCount", proposals.Count),
                Meta("source", "UserReportTemplate visibility on seeded target DB"),
            };

            var writtenPath = Visa2014MinimalXlsxWriter.WriteWorkbook(outputPath,
            [
                new Visa2014Worksheet { Name = "ProfileNestedTemplates", Columns = ColumnOrder, Rows = rows },
                new Visa2014Worksheet
                {
                    Name = "_ProfilesWithoutTemplates",
                    Columns = ["ProfileCatalogKey", "Note"],
                    Rows = profilesWithoutTemplates,
                },
                new Visa2014Worksheet { Name = "_Meta", Columns = ["_key", "value"], Rows = metaRows },
            ]);

            if (verbose)
            {
                Console.WriteLine($"INF Profile catalog keys with templates: {profileKeys}");
                Console.WriteLine($"INF Distinct template names: {templateNames}");
                Console.WriteLine($"INF Nested template rows: {proposals.Count}");
            }

            return new Visa2014PreviewExportResult
            {
                OutputPath = Path.GetFullPath(writtenPath),
                LegacyRowCount = profileKeys,
                ImportRowCount = proposals.Count,
                SkippedRowCount = profilesWithoutTemplates.Count,
            };
        }
        finally
        {
            importScope?.Dispose();
            host?.Dispose();
        }
    }

    private static Dictionary<string, object?> Meta(string key, object? value) =>
        new(StringComparer.Ordinal) { ["_key"] = key, ["value"] = value?.ToString() ?? string.Empty };

    private static string MaskConnection(string connectionString) =>
        System.Text.RegularExpressions.Regex.Replace(
            connectionString,
            @"(Password|Pwd)\s*=\s*[^;]+",
            "$1=***",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
}
