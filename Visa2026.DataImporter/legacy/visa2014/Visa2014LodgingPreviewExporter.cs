namespace Visa2026.DataImporter.Legacy.Visa2014;

internal static class Visa2014LodgingPreviewExporter
{
    public static Visa2014PersonImportBatch PrepareImportBatch(
        string connectionString,
        IReadOnlyList<string> lookupTranslationPaths,
        int? maxRows,
        bool verbose) =>
        Visa2014LodgingTransform.PrepareImportBatch(connectionString, lookupTranslationPaths, maxRows, verbose);

    public static Visa2014PreviewExportResult Export(
        string connectionString,
        IReadOnlyList<string> lookupTranslationPaths,
        string outputPath,
        int? maxRows,
        bool verbose,
        string? legacySourceId = null)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outputPath))!);

        var batch = PrepareImportBatch(connectionString, lookupTranslationPaths, maxRows, verbose);

        var metaRows = new List<IReadOnlyDictionary<string, object?>>
        {
            Row("_key", "exportedAt", DateTime.UtcNow.ToString("O")),
            Row("_key", "entity", "Lodging"),
            Row("_key", "database", GetDatabaseName(connectionString)),
            Row("_key", "legacyDistinctAddressCount", batch.LegacyRowCount),
            Row("_key", "catalogRowCount", batch.ImportRows.Count),
            Row("_key", "skippedRowCount", batch.Skipped.Count),
            Row("_key", "dedupeMergedCount", batch.DedupeMergedCount),
            Row("_key", "source", "VISA2015 Address (DocumentOfAddress TypeOfDocument=Lojman)"),
            Row("_key", "normalizer", "Visa2014AddressLineNormalizer.NormalizeLodgingCatalogAddress"),
        };
        if (!string.IsNullOrWhiteSpace(legacySourceId))
            metaRows.Add(Row("_key", "legacySource", legacySourceId));

        var dedupeColumns = Visa2014PersonTransform.InferColumns(
            batch.DedupeSummary.ToList(),
            LodgingMainColumnOrder());

        var writtenPath = Visa2014MinimalXlsxWriter.WriteWorkbook(outputPath,
        [
            new Visa2014Worksheet { Name = "Lodging", Columns = Visa2014LodgingTransform.LodgingMainColumnOrder, Rows = batch.ImportRows },
            new Visa2014Worksheet { Name = "_DedupeMerged", Columns = dedupeColumns, Rows = batch.DedupeSummary.ToList() },
            new Visa2014Worksheet { Name = "_Skipped", Columns = Visa2014PersonTransform.InferColumns(batch.Skipped.ToList(), ["reason", "_legacy_AddressLine", "UsageCount"]), Rows = batch.Skipped.ToList() },
            new Visa2014Worksheet { Name = "_UnmappedLookups", Columns = ["catalog", "legacyValue", "reason"], Rows = batch.UnmappedLookups.ToList() },
            new Visa2014Worksheet { Name = "_Meta", Columns = ["_key", "value"], Rows = metaRows },
        ]);

        return new Visa2014PreviewExportResult
        {
            OutputPath = Path.GetFullPath(writtenPath),
            LegacyRowCount = batch.LegacyRowCount,
            ImportRowCount = batch.ImportRows.Count,
            SkippedRowCount = batch.Skipped.Count,
            DedupeMergedCount = batch.DedupeMergedCount,
            UnmappedLookupCount = batch.UnmappedLookups.Count,
        };
    }

    private static string[] LodgingMainColumnOrder() => Visa2014LodgingTransform.LodgingMainColumnOrder;

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
