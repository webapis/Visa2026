namespace Visa2026.DataImporter.Legacy.Visa2014;

/// <summary>
/// Wave 3 — Excel proposal and tenant JSON for nested <c>ApplicationProfileTemplate</c> rows.
/// </summary>
internal static class Visa2014ApplicationProfileNestedTemplateExportCommand
{
    public static int RunPreview(IReadOnlyList<string> args, bool verbose)
    {
        var dataImporterRoot = Visa2014ContentRoot.FindDataImporterRoot();
        if (dataImporterRoot == null)
        {
            Console.Error.WriteLine("ERR Could not locate Visa2026.DataImporter content root.");
            return 1;
        }

        var targetConnection = ResolveTargetConnection(args);
        if (string.IsNullOrWhiteSpace(targetConnection))
        {
            Console.Error.WriteLine(
                "ERR --export-visa2014-application-profile-nested-template-preview requires --target-connection " +
                "(or ConnectionStrings__DefaultConnection / VISA2026_SQL_CONNECTION).");
            return 1;
        }

        var solutionRoot = Visa2014ContentRoot.FindSolutionRoot();
        var defaultOutput = Path.Combine(
            dataImporterRoot,
            "legacy",
            "visa2014",
            "preview-export",
            "ApplicationProfileNestedTemplates-proposal.calik-energi.xlsx");
        var output = GetOptionValue(args, "--output") ?? defaultOutput;

        Console.WriteLine("=== VISA2014 ApplicationProfile nested templates preview (Wave 3)");
        Console.WriteLine($"INF Target database: {MaskConnectionForLog(targetConnection)}");
        Console.WriteLine($"INF Output: {Path.GetFullPath(output)}");

        try
        {
            var result = Visa2014ApplicationProfileNestedTemplateExporter.Export(targetConnection, output, verbose);
            Console.WriteLine(
                $"OK Preview workbook: {result.OutputPath} " +
                $"({result.ImportRowCount} nested rows, {result.LegacyRowCount} profile keys, {result.SkippedRowCount} without templates)");
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

    public static int RunTenantJson(IReadOnlyList<string> args, bool verbose)
    {
        var dataImporterRoot = Visa2014ContentRoot.FindDataImporterRoot();
        if (dataImporterRoot == null)
        {
            Console.Error.WriteLine("ERR Could not locate Visa2026.DataImporter content root.");
            return 1;
        }

        var targetConnection = ResolveTargetConnection(args);
        if (string.IsNullOrWhiteSpace(targetConnection))
        {
            Console.Error.WriteLine(
                "ERR --export-visa2014-application-profile-nested-template-tenant-json requires --target-connection " +
                "(or ConnectionStrings__DefaultConnection / VISA2026_SQL_CONNECTION).");
            return 1;
        }

        var solutionRoot = Visa2014ContentRoot.FindSolutionRoot();
        var defaultOutput = Path.Combine(
            solutionRoot ?? dataImporterRoot,
            "Visa2026.Module",
            "DatabaseUpdate",
            "LookupCatalogs",
            "tenant",
            "application-profile-nested-templates.calik-energi.json");
        var output = GetOptionValue(args, "--output") ?? defaultOutput;

        Console.WriteLine("=== VISA2014 ApplicationProfile nested templates tenant JSON (Wave 3)");
        Console.WriteLine($"INF Target database: {MaskConnectionForLog(targetConnection)}");
        Console.WriteLine($"INF Output: {Path.GetFullPath(output)}");
        Console.WriteLine("INF SignOff is empty until developer approves — set \"SignOff\": \"approved\" before patch/deploy sync.");

        try
        {
            Visa2014ApplicationProfileNestedTemplateTenantCatalogExporter.ExportTenantJson(
                targetConnection,
                output,
                verbose);
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

    private static string? ResolveTargetConnection(IReadOnlyList<string> args) =>
        GetOptionValue(args, "--target-connection")
        ?? Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
        ?? Environment.GetEnvironmentVariable("VISA2026_SQL_CONNECTION");

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
