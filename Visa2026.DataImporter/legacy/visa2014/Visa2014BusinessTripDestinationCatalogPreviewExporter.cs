namespace Visa2026.DataImporter.Legacy.Visa2014;

internal static class Visa2014BusinessTripDestinationCatalogPreviewExporter
{
    public static Visa2014PreviewExportResult Export(
        string connectionString,
        IReadOnlyList<string> lookupTranslationPaths,
        string outputPath,
        int? maxRows,
        bool verbose,
        string? legacySourceId = null,
        string? solutionRoot = null)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outputPath))!);

        var batch = Visa2014BusinessTripDestinationCatalogTransform.PrepareImportBatch(
            connectionString,
            lookupTranslationPaths,
            solutionRoot,
            maxRows,
            verbose);

        var addCount = batch.ImportRows.Count(r =>
            string.Equals(r.GetValueOrDefault("_importAction") as string, "add_to_catalog", StringComparison.Ordinal));
        var alreadyCount = batch.ImportRows.Count(r =>
            string.Equals(r.GetValueOrDefault("_importAction") as string, "already_in_catalog", StringComparison.Ordinal));

        var metaRows = new List<IReadOnlyDictionary<string, object?>>
        {
            Row("_key", "exportedAt", DateTime.UtcNow.ToString("O")),
            Row("_key", "entity", "BusinessTripDestinationCatalog"),
            Row("_key", "database", GetDatabaseName(connectionString)),
            Row("_key", "legacyDistinctAddressCount", batch.LegacyRowCount),
            Row("_key", "catalogRowCount", batch.ImportRows.Count),
            Row("_key", "addToCatalogCount", addCount),
            Row("_key", "alreadyInCatalogCount", alreadyCount),
            Row("_key", "skippedRowCount", batch.Skipped.Count),
            Row("_key", "dedupeMergedCount", batch.DedupeMergedCount),
            Row("_key", "source", "VISA2015 AddressOnBusinessTrip → Address.AddressLine"),
            Row("_key", "unmatchedPolicy", "OtherSite"),
            Row("_key", "normalizer", "Lodging/Hotel/Hospital cleaners (same as AddressOfResidence)"),
            Row("_key", "review", "Human review Excel before writing tenant *.calik-energi.json"),
        };
        if (!string.IsNullOrWhiteSpace(legacySourceId))
            metaRows.Add(Row("_key", "legacySource", legacySourceId));

        var byCatalog = batch.ImportRows
            .GroupBy(r => r.GetValueOrDefault("ProposedCatalog") as string ?? "OtherSite")
            .Select(g => Row("_key", "proposed_" + g.Key, g.Count()))
            .ToList();
        metaRows.AddRange(byCatalog);

        var writtenPath = Visa2014MinimalXlsxWriter.WriteWorkbook(outputPath,
        [
            new Visa2014Worksheet
            {
                Name = "Destinations",
                Columns = Visa2014BusinessTripDestinationCatalogTransform.MainColumnOrder,
                Rows = batch.ImportRows,
            },
            new Visa2014Worksheet
            {
                Name = "_DedupeMerged",
                Columns = Visa2014PersonTransform.InferColumns(
                    batch.DedupeSummary.ToList(),
                    Visa2014BusinessTripDestinationCatalogTransform.MainColumnOrder),
                Rows = batch.DedupeSummary.ToList(),
            },
            new Visa2014Worksheet
            {
                Name = "_Skipped",
                Columns = Visa2014PersonTransform.InferColumns(
                    batch.Skipped.ToList(),
                    ["reason", "_legacy_AddressLine", "UsageCount"]),
                Rows = batch.Skipped.ToList(),
            },
            new Visa2014Worksheet
            {
                Name = "_UnmappedLookups",
                Columns = ["catalog", "legacyValue", "reason"],
                Rows = batch.UnmappedLookups.ToList(),
            },
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