using System.Text.Json;
using Visa2026.Module.DatabaseUpdate;
using Visa2026.Module.DatabaseUpdate.LookupCatalogs;

namespace Visa2026.DataImporter.Legacy.Visa2014;

internal static class Visa2014ApplicationProfileTenantCatalogExporter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = null,
    };

    public static string ExportTenantJson(
        string connectionString,
        IReadOnlyList<string> lookupTranslationPaths,
        string outputPath,
        int? maxRows,
        bool verbose)
    {
        var batch = Visa2014ApplicationPreviewExporter.PrepareImportBatch(
            connectionString,
            lookupTranslationPaths,
            maxRows,
            verbose);

        var groupKeys = ApplicationProfileCatalogGrouping.DistinctGroupKeysFromImportRows(batch.ImportRows);
        var rows = new List<ApplicationProfileTenantCatalogRow>();
        foreach (var groupKey in groupKeys)
        {
            if (ApplicationProfileCatalogPreviewHelper.TryBuildTenantCatalogRow(
                    groupKey.ApplicationTypeName,
                    groupKey.ProjectContractCode,
                    out var row)
                && row != null)
            {
                rows.Add(row);
            }
        }

        var catalog = new ApplicationProfileTenantCatalogFile { Rows = rows };
        var json = JsonSerializer.Serialize(catalog, JsonOptions);
        var utf8 = new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
        System.IO.File.WriteAllText(Path.GetFullPath(outputPath), json, utf8);
        Console.WriteLine($"INF Profile rows: {rows.Count}");
        return Path.GetFullPath(outputPath);
    }
}
