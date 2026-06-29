namespace Visa2026.DataImporter.Legacy.Visa2014;

internal static class Visa2014EmployeeSalaryPreviewExporter
{
    public static Visa2014PersonImportBatch PrepareImportBatch(
        string connectionString,
        IReadOnlyList<string> lookupTranslationPaths,
        int? maxRows,
        bool verbose) =>
        Visa2014EmployeeSalaryTransform.PrepareImportBatch(connectionString, lookupTranslationPaths, maxRows, verbose);

    public static Visa2014PreviewExportResult Export(
        string connectionString,
        IReadOnlyList<string> lookupTranslationPaths,
        string outputPath,
        int? maxRows,
        bool verbose,
        string? legacySourceId = null)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outputPath))!);

        var batch = Visa2014EmployeeSalaryTransform.PrepareImportBatch(
            connectionString, lookupTranslationPaths, maxRows, verbose);

        var amountParseRows = batch.ImportRows
            .Concat(batch.Skipped)
            .Select(BuildAmountParseRowFromExportRow)
            .ToList();

        var metaRows = new List<IReadOnlyDictionary<string, object?>>
        {
            Row("_key", "exportedAt", DateTime.UtcNow.ToString("O")),
            Row("_key", "entity", "EmployeeSalary"),
            Row("_key", "database", GetDatabaseName(connectionString)),
            Row("_key", "legacyRowCount", batch.LegacyRowCount),
            Row("_key", "importRowCount", batch.ImportRows.Count),
            Row("_key", "skippedRowCount", batch.Skipped.Count),
            Row("_key", "dedupeMergedCount", batch.DedupeMergedCount),
            Row("_key", "fieldMap", "legacy/visa2014/field-maps/EmployeeSalary.yaml"),
        };
        if (!string.IsNullOrWhiteSpace(legacySourceId))
            metaRows.Add(Row("_key", "legacySource", legacySourceId));

        var writtenPath = Visa2014MinimalXlsxWriter.WriteWorkbook(outputPath,
        [
            new Visa2014Worksheet
            {
                Name = "EmployeeSalary",
                Columns = Visa2014EmployeeSalaryTransform.EmployeeSalaryMainColumnOrder,
                Rows = batch.ImportRows,
            },
            new Visa2014Worksheet
            {
                Name = "_AmountParse",
                Columns = Visa2014EmployeeSalaryTransform.AmountParseColumnOrder,
                Rows = amountParseRows,
            },
            new Visa2014Worksheet
            {
                Name = "_Skipped",
                Columns = Visa2014PersonTransform.InferColumns(batch.Skipped.ToList(), Visa2014EmployeeSalaryTransform.EmployeeSalaryMainColumnOrder),
                Rows = batch.Skipped.ToList(),
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
            UnmappedLookupCount = 0,
        };
    }

    private static Dictionary<string, object?> BuildAmountParseRowFromExportRow(IReadOnlyDictionary<string, object?> row)
    {
        var rawDetail = row.GetValueOrDefault("_legacy_SalaryDetail") as string;
        var parseNote = "empty";
        string? normalized = null;
        if (Visa2014SalaryAmountNormalizer.TryNormalize(rawDetail, out var amount, out parseNote))
            normalized = amount;

        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["_legacyPersonOid"] = row.GetValueOrDefault("_legacy_PersonOid"),
            ["_rawDetail"] = rawDetail,
            ["_normalizedAmount"] = normalized,
            ["_currency"] = Visa2014SalaryAmountNormalizer.ResolveCurrency(rawDetail),
            ["_parseNote"] = parseNote,
            ["_startDate"] = row.GetValueOrDefault("StartDate"),
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
