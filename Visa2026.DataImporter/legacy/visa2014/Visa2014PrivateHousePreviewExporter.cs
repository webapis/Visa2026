namespace Visa2026.DataImporter.Legacy.Visa2014;

internal static class Visa2014PrivateHousePreviewExporter
{
    internal static readonly string[] PrivateHouseMainColumnOrder =
    [
        "_legacyRowId", "_legacyTable", "_importAction",
        "Person", "Region", "City", "FullAddress", "ExpirationDate",
        "_legacy_AddressLine", "_legacy_RegionMgCode", "_legacy_CityMgCode", "_legacy_PersonOid",
    ];

    public static Visa2014PreviewExportResult Export(
        string connectionString,
        IReadOnlyList<string> lookupTranslationPaths,
        string outputPath,
        int? maxRows,
        bool verbose,
        string? legacySourceId = null)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outputPath))!);

        var batch = Visa2014AddressOfResidenceTransform.PrepareImportBatch(
            connectionString, lookupTranslationPaths, maxRows, verbose);

        var privateHouseRows = batch.ImportRows
            .Where(r => string.Equals(r.GetValueOrDefault("Type") as string, "PrivateHouse", StringComparison.OrdinalIgnoreCase))
            .ToList();

        var skippedRows = batch.Skipped.ToList();

        var metaRows = new List<IReadOnlyDictionary<string, object?>>
        {
            Row("_key", "exportedAt", DateTime.UtcNow.ToString("O")),
            Row("_key", "entity", "PrivateHouse"),
            Row("_key", "database", GetDatabaseName(connectionString)),
            Row("_key", "addressOfResidenceLegacyRowCount", batch.LegacyRowCount),
            Row("_key", "addressOfResidenceImportRowCount", batch.ImportRows.Count),
            Row("_key", "privateHouseRowCount", privateHouseRows.Count),
            Row("_key", "skippedRowCount", skippedRows.Count),
            Row("_key", "source", "VISA2015 AddressOfResidence (DocumentOfAddress TypeOfDocument=Patent)"),
            Row("_key", "parentExport", "AddressOfResidence-preview (Type=PrivateHouse slice)"),
            Row("_key", "fieldMap", "legacy/visa2014/field-maps/AddressOfResidence.yaml"),
        };
        if (!string.IsNullOrWhiteSpace(legacySourceId))
            metaRows.Add(Row("_key", "legacySource", legacySourceId));

        var writtenPath = Visa2014MinimalXlsxWriter.WriteWorkbook(outputPath,
        [
            new Visa2014Worksheet { Name = "PrivateHouse", Columns = PrivateHouseMainColumnOrder, Rows = privateHouseRows },
            new Visa2014Worksheet { Name = "_Skipped", Columns = Visa2014PersonTransform.InferColumns(skippedRows, ["_legacyRowId", "reason", "_legacy_AddressLine", "_legacy_PersonOid"]), Rows = skippedRows },
            new Visa2014Worksheet { Name = "_UnmappedLookups", Columns = ["catalog", "legacyValue", "reason"], Rows = batch.UnmappedLookups.ToList() },
            new Visa2014Worksheet { Name = "_Meta", Columns = ["_key", "value"], Rows = metaRows },
        ]);

        return new Visa2014PreviewExportResult
        {
            OutputPath = Path.GetFullPath(writtenPath),
            LegacyRowCount = batch.LegacyRowCount,
            ImportRowCount = privateHouseRows.Count,
            SkippedRowCount = skippedRows.Count,
            DedupeMergedCount = 0,
            UnmappedLookupCount = batch.UnmappedLookups.Count,
        };
    }

    private static Dictionary<string, object?> Row(string k1, string k2, object? v) =>
        new(StringComparer.Ordinal) { [k1] = k2, ["value"] = v };

    private static string GetDatabaseName(string connectionString)
    {
        foreach (var part in connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            if (part.StartsWith("Database=", StringComparison.OrdinalIgnoreCase))
                return part["Database=".Length..].Trim();
            if (part.StartsWith("Initial Catalog=", StringComparison.OrdinalIgnoreCase))
                return part["Initial Catalog=".Length..].Trim();
        }

        return "VISA2015";
    }
}
