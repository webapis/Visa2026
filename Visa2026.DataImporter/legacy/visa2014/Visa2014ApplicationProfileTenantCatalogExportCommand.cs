namespace Visa2026.DataImporter.Legacy.Visa2014;

/// <summary>
/// Wave 1 — tenant <c>application-profile.calik-energi.json</c> from legacy VISA2015 (after Excel sign-off).
/// </summary>
internal static class Visa2014ApplicationProfileTenantCatalogExportCommand
{
    public static int Run(IReadOnlyList<string> args, bool verbose)
    {
        var dataImporterRoot = Visa2014ContentRoot.FindDataImporterRoot();
        if (dataImporterRoot == null)
        {
            Console.Error.WriteLine("ERR Could not locate Visa2026.DataImporter content root.");
            return 1;
        }

        var solutionRoot = Visa2014ContentRoot.FindSolutionRoot();
        Visa2014LegacySourceProfile source;
        try
        {
            source = Visa2014LegacySource.Resolve(dataImporterRoot, solutionRoot, args);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ERR {ex.Message}");
            return 1;
        }

        var defaultOutput = Path.Combine(
            solutionRoot ?? dataImporterRoot,
            "Visa2026.Module",
            "DatabaseUpdate",
            "LookupCatalogs",
            "tenant",
            "application-profile.calik-energi.json");

        var output = GetOptionValue(args, "--output") ?? defaultOutput;

        int? maxRows = null;
        var maxRowsText = GetOptionValue(args, "--max-rows");
        if (int.TryParse(maxRowsText, out var parsedMax) && parsedMax > 0)
            maxRows = parsedMax;

        Console.WriteLine("=== VISA2014 ApplicationProfile tenant JSON export (Wave 1)");
        Console.WriteLine($"INF Legacy source: {source.Id} ({source.Label})");
        Console.WriteLine($"INF Database: {MaskConnectionForLog(source.ConnectionString)}");
        Console.WriteLine($"INF Lookup translations:");
        foreach (var path in source.LookupTranslationPaths)
            Console.WriteLine($"INF   - {path}");
        Console.WriteLine($"INF Output: {Path.GetFullPath(output)}");
        if (maxRows.HasValue)
            Console.WriteLine($"INF Max rows: {maxRows.Value}");

        try
        {
            var written = Visa2014ApplicationProfileTenantCatalogExporter.ExportTenantJson(
                source.ConnectionString,
                source.LookupTranslationPaths,
                output,
                maxRows,
                verbose);

            Console.WriteLine($"OK Tenant catalog JSON written: {written}");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ERR {ex.Message}");
            if (verbose)
                Console.Error.WriteLine(ex);
            return 1;
        }
    }

    private static string? GetOptionValue(IReadOnlyList<string> args, string optionName)
    {
        for (int i = 0; i < args.Count - 1; i++)
        {
            if (string.Equals(args[i], optionName, StringComparison.OrdinalIgnoreCase))
                return args[i + 1];
        }

        return null;
    }

    private static string MaskConnectionForLog(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            return "(empty)";

        var parts = connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries);
        return string.Join(";", parts.Where(p =>
            !p.TrimStart().StartsWith("Password", StringComparison.OrdinalIgnoreCase)
            && !p.TrimStart().StartsWith("Pwd", StringComparison.OrdinalIgnoreCase)));
    }
}
