namespace Visa2026.DataImporter.Legacy.Visa2014;

internal static class Visa2014PreviewExportCommand
{
    public static int Run(IReadOnlyList<string> args, bool verbose)
    {
        var entity = GetOptionValue(args, "--entity");
        if (string.IsNullOrWhiteSpace(entity))
        {
            Console.Error.WriteLine("ERR --export-visa2014-preview requires --entity <Name> (e.g. Person).");
            return 1;
        }

        if (!string.Equals(entity, "Person", StringComparison.OrdinalIgnoreCase))
        {
            Console.Error.WriteLine($"ERR Entity '{entity}' is not supported yet. Supported: Person.");
            return 1;
        }

        var dataImporterRoot = Visa2014ContentRoot.FindDataImporterRoot();
        if (dataImporterRoot == null)
        {
            Console.Error.WriteLine("ERR Could not locate Visa2026.DataImporter content root.");
            return 1;
        }

        var solutionRoot = Visa2014ContentRoot.FindSolutionRoot();
        var lookupPath = Visa2014ContentRoot.LookupTranslationsPath(solutionRoot);
        if (lookupPath == null || !File.Exists(lookupPath))
        {
            Console.Error.WriteLine("ERR lookup-translations.yaml not found under docs/VISA2014_MIGRATION/.");
            return 1;
        }

        var output = GetOptionValue(args, "--output")
                     ?? Visa2014ContentRoot.DefaultPreviewOutputPath(dataImporterRoot, entity);

        var connection = Visa2014ContentRoot.ResolveConnectionString(GetOptionValue(args, "--connection"));
        int? maxRows = null;
        var maxRowsText = GetOptionValue(args, "--max-rows");
        if (int.TryParse(maxRowsText, out var parsedMax) && parsedMax > 0)
            maxRows = parsedMax;

        Console.WriteLine($"=== VISA2014 Excel preview export — {entity}");
        Console.WriteLine($"INF Database: {MaskConnectionForLog(connection)}");
        Console.WriteLine($"INF Lookup translations: {Path.GetFullPath(lookupPath)}");
        Console.WriteLine($"INF Output: {Path.GetFullPath(output)}");
        if (maxRows.HasValue)
            Console.WriteLine($"INF Max rows: {maxRows.Value}");

        try
        {
            var result = Visa2014PersonPreviewExporter.Export(
                connection,
                lookupPath,
                output,
                maxRows,
                verbose);

            Console.WriteLine($" OK Wrote {result.ImportRowCount} import row(s) (+ {result.DedupeMergedCount} duplicate_merged, {result.SkippedRowCount} skipped).");
            Console.WriteLine($"INF Legacy SQL rows: {result.LegacyRowCount}");
            Console.WriteLine($"INF Unmapped lookup distinct: {result.UnmappedLookupCount}");
            if (!string.Equals(Path.GetFullPath(output), result.OutputPath, StringComparison.OrdinalIgnoreCase))
                Console.WriteLine($"WRN Target locked — wrote fallback: {result.OutputPath}");
            Console.WriteLine($" OK {result.OutputPath}");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ERR Export failed: {ex.Message}");
            if (verbose)
                Console.Error.WriteLine(ex);
            return 1;
        }
    }

    private static string? GetOptionValue(IReadOnlyList<string> args, string optionName)
    {
        for (int i = 0; i < args.Count; i++)
        {
            if (!string.Equals(args[i], optionName, StringComparison.OrdinalIgnoreCase))
                continue;

            if (i + 1 < args.Count && !args[i + 1].StartsWith('-'))
                return args[i + 1];
            return null;
        }

        return null;
    }

    private static string MaskConnectionForLog(string connectionString)
    {
        string? server = null;
        string? database = null;
        bool trusted = connectionString.Contains("Trusted_Connection=True", StringComparison.OrdinalIgnoreCase);

        foreach (var part in connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            if (part.StartsWith("Server=", StringComparison.OrdinalIgnoreCase))
                server = part["Server=".Length..].Trim();
            else if (part.StartsWith("Data Source=", StringComparison.OrdinalIgnoreCase))
                server = part["Data Source=".Length..].Trim();
            else if (part.StartsWith("Database=", StringComparison.OrdinalIgnoreCase))
                database = part["Database=".Length..].Trim();
            else if (part.StartsWith("Initial Catalog=", StringComparison.OrdinalIgnoreCase))
                database = part["Initial Catalog=".Length..].Trim();
        }

        return $"Server={server ?? "?"};Database={database ?? "?"};Auth={(trusted ? "Windows" : "SQL")}";
    }
}
