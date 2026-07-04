namespace Visa2026.DataImporter.Legacy.Visa2014;

internal static class Visa2014ApplicationProgressPreviewExporter
{
    public static Visa2014PreviewExportResult Export(
        string connectionString,
        IReadOnlyList<string> lookupTranslationPaths,
        string outputPath,
        int? maxRows,
        bool verbose,
        string? legacySourceId = null)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outputPath))!);

        var batch = Visa2014ApplicationProgressTransform.PrepareImportBatch(
            connectionString, lookupTranslationPaths, maxRows, verbose);

        var metaRows = new List<IReadOnlyDictionary<string, object?>>
        {
            Row("_key", "exportedAt", DateTime.UtcNow.ToString("O")),
            Row("_key", "entity", "ApplicationProgress"),
            Row("_key", "database", GetDatabaseName(connectionString)),
            Row("_key", "legacyApplicationCount", batch.LegacyRowCount),
            Row("_key", "importRowCount", batch.ImportRows.Count),
            Row("_key", "skippedParentCount", batch.Skipped.Count),
            Row("_key", "synthesisApproved", "2026-06-29"),
            Row("_key", "fieldMap", "legacy/visa2014/field-maps/ApplicationProgress.yaml"),
        };
        if (!string.IsNullOrWhiteSpace(legacySourceId))
            metaRows.Add(Row("_key", "legacySource", legacySourceId));

        var writtenPath = Visa2014MinimalXlsxWriter.WriteWorkbook(outputPath,
        [
            new Visa2014Worksheet
            {
                Name = "ApplicationProgress",
                Columns = Visa2014ApplicationProgressTransform.ApplicationProgressMainColumnOrder,
                Rows = batch.ImportRows,
            },
            new Visa2014Worksheet
            {
                Name = "_Skipped",
                Columns = Visa2014PersonTransform.InferColumns(
                    batch.Skipped.ToList(),
                    ["_legacyApplicationOid", "_reason", "_processKind", "_legacy_ManualApplicationNumber", "_legacy_ApplicationTypeComposite"]),
                Rows = batch.Skipped.ToList(),
            },
            new Visa2014Worksheet
            {
                Name = "_SynthesisSummary",
                Columns = ["_legacyApplicationOid", "_processKind", "stepCount", "_legacy_ManualApplicationNumber", "_legacy_ApplicationTypeComposite"],
                Rows = batch.DedupeSummary.ToList(),
            },
            new Visa2014Worksheet { Name = "_Meta", Columns = ["_key", "value"], Rows = metaRows },
        ]);

        return new Visa2014PreviewExportResult
        {
            OutputPath = Path.GetFullPath(writtenPath),
            LegacyRowCount = batch.LegacyRowCount,
            ImportRowCount = batch.ImportRows.Count,
            SkippedRowCount = batch.Skipped.Count,
            DedupeMergedCount = 0,
            UnmappedLookupCount = 0,
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
