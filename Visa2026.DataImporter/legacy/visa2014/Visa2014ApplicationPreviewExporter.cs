namespace Visa2026.DataImporter.Legacy.Visa2014;

internal static class Visa2014ApplicationPreviewExporter
{
    public static Visa2014PersonImportBatch PrepareImportBatch(
        string connectionString,
        IReadOnlyList<string> lookupTranslationPaths,
        int? maxRows,
        bool verbose) =>
        Visa2014ApplicationTransform.PrepareImportBatch(connectionString, lookupTranslationPaths, maxRows, verbose);

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
            Row("_key", "entity", "Application"),
            Row("_key", "database", GetDatabaseName(connectionString)),
            Row("_key", "legacyRowCount", batch.LegacyRowCount),
            Row("_key", "importRowCount", batch.ImportRows.Count),
            Row("_key", "skippedRowCount", batch.Skipped.Count),
            Row("_key", "dedupeMergedCount", batch.DedupeMergedCount),
            Row("_key", "dedupeGroupCount", batch.DedupeSummary.Count),
            Row("_key", "fieldMap", "legacy/visa2014/field-maps/Application.yaml"),
        };
        if (!string.IsNullOrWhiteSpace(legacySourceId))
            metaRows.Add(Row("_key", "legacySource", legacySourceId));

        var migrationMappingRows = BuildMigrationServiceMappingRows(batch.ImportRows);
        metaRows.Add(Row("_key", "migrationServiceMappedCount", migrationMappingRows.Count));
        Console.WriteLine($"INF MigrationService mapped: {migrationMappingRows.Count} (sheet _MigrationServiceMapping)");

        var writtenPath = Visa2014MinimalXlsxWriter.WriteWorkbook(outputPath,
        [
            new Visa2014Worksheet { Name = "Application", Columns = Visa2014ApplicationTransform.ApplicationMainColumnOrder, Rows = batch.ImportRows },
            new Visa2014Worksheet
            {
                Name = "_MigrationServiceMapping",
                Columns =
                [
                    "_legacyRowId", "FullApplicationNumber", "ApplicationType",
                    "_legacy_DepartmentForRegistration", "_legacy_DepartmentForRegistrationName",
                    "MigrationService",
                ],
                Rows = migrationMappingRows,
            },
            new Visa2014Worksheet { Name = "_Skipped", Columns = Visa2014PersonTransform.InferColumns(batch.Skipped.ToList(), Visa2014ApplicationTransform.ApplicationMainColumnOrder), Rows = batch.Skipped.ToList() },
            new Visa2014Worksheet { Name = "_UnmappedLookups", Columns = ["catalog", "legacyValue", "reason"], Rows = batch.UnmappedLookups.ToList() },
            new Visa2014Worksheet { Name = "_DedupeSummary", Columns = Visa2014PersonTransform.InferColumns(batch.DedupeSummary.ToList(), ["_dedupeGroupId", "key", "normalizedValue", "memberCount", "canonicalRule"]), Rows = batch.DedupeSummary.ToList() },
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

    private static List<Dictionary<string, object?>> BuildMigrationServiceMappingRows(
        IReadOnlyList<Dictionary<string, object?>> importRows) =>
        importRows
            .Where(r => r.GetValueOrDefault("MigrationService") is string ms && !string.IsNullOrWhiteSpace(ms))
            .Select(r => new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["_legacyRowId"] = r.GetValueOrDefault("_legacyRowId"),
                ["FullApplicationNumber"] = r.GetValueOrDefault("FullApplicationNumber"),
                ["ApplicationType"] = r.GetValueOrDefault("ApplicationType"),
                ["_legacy_DepartmentForRegistration"] = r.GetValueOrDefault("_legacy_DepartmentForRegistration"),
                ["_legacy_DepartmentForRegistrationName"] = r.GetValueOrDefault("_legacy_DepartmentForRegistrationName"),
                ["MigrationService"] = r.GetValueOrDefault("MigrationService"),
            })
            .ToList();

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
