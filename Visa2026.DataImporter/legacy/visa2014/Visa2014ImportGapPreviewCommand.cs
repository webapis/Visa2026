namespace Visa2026.DataImporter.Legacy.Visa2014;

/// <summary>
/// CLI: --export-visa2014-import-gaps --entity AddressOfResidence
/// Loads Demo/target lookups via headless session, then writes Excel of unresolved AoR rows.
/// </summary>
internal static class Visa2014ImportGapPreviewCommand
{
    public static async Task<int> RunAsync(IReadOnlyList<string> args, bool verbose)
    {
        var entity = GetOptionValue(args, "--entity");
        if (!string.Equals(entity, "AddressOfResidence", StringComparison.OrdinalIgnoreCase))
        {
            Console.Error.WriteLine("ERR --export-visa2014-import-gaps currently supports --entity AddressOfResidence only.");
            return 1;
        }

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

        var targetCs = GetOptionValue(args, "--target-connection")
            ?? Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? Environment.GetEnvironmentVariable("VISA2026_SQL_CONNECTION");
        if (string.IsNullOrWhiteSpace(targetCs))
        {
            Console.Error.WriteLine("ERR --export-visa2014-import-gaps requires --target-connection (or ConnectionStrings__DefaultConnection / VISA2026_SQL_CONNECTION).");
            return 1;
        }

        var personIdMapPath = GetOptionValue(args, "--person-id-map")
            ?? source.IdMapPath(dataImporterRoot, "Person");
        var addressIdMapPath = GetOptionValue(args, "--address-id-map")
            ?? source.IdMapPath(dataImporterRoot, "AddressOfResidence");

        var stamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
        var output = GetOptionValue(args, "--output")
            ?? Path.Combine(
                dataImporterRoot,
                "legacy",
                "visa2014",
                "preview-export",
                $"AddressOfResidence-ImportGaps-{source.Id}-{stamp}.xlsx");

        int? maxRows = null;
        var maxRowsText = GetOptionValue(args, "--max-rows");
        if (int.TryParse(maxRowsText, out var parsedMax) && parsedMax > 0)
            maxRows = parsedMax;

        Console.WriteLine("=== VISA2014 AddressOfResidence import-gap Excel preview");
        Console.WriteLine($"INF Legacy source: {source.Id} ({source.Label})");
        Console.WriteLine($"INF Person id-map: {Path.GetFullPath(personIdMapPath)}");
        Console.WriteLine($"INF Address id-map: {Path.GetFullPath(addressIdMapPath)}");
        Console.WriteLine($"INF Output: {Path.GetFullPath(output)}");

        await using var session = await Visa2014HeadlessImportSession.OpenAsync(targetCs);
        var result = Visa2014AddressOfResidenceImportGapPreviewExporter.Export(
            source.ConnectionString,
            source.LookupTranslationPaths,
            session.Resolver,
            personIdMapPath,
            addressIdMapPath,
            output,
            maxRows,
            verbose,
            source.Id);

        Console.WriteLine($"OK Gap preview written: {result.OutputPath} ({result.ImportRowCount} gap row(s))");
        return 0;
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
}