namespace Visa2026.DataImporter.Legacy.Visa2014;

/// <summary>
/// CLI: --export-visa2014-application-profile-approval-leg-version-matrix
/// </summary>
internal static class Visa2014ApplicationProfileApprovalLegVersionMatrixCommand
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

        var tenantDir = Path.Combine(
            solutionRoot ?? dataImporterRoot,
            "Visa2026.Module",
            "DatabaseUpdate",
            "LookupCatalogs",
            "tenant");

        var profileCatalog = GetOptionValue(args, "--profile-catalog")
            ?? Path.Combine(tenantDir, "application-profile.calik-energi.json");
        var approvalCatalog = GetOptionValue(args, "--approval-leg-catalog")
            ?? Path.Combine(tenantDir, "approval-leg-profile.json");
        var seedJson = GetOptionValue(args, "--output")
            ?? Path.Combine(tenantDir, "application-profile-approval-leg-versions.calik-energi.json");
        var matrixMd = GetOptionValue(args, "--matrix-report")
            ?? Path.Combine(
                solutionRoot ?? dataImporterRoot,
                "docs",
                "VISA2014_MIGRATION",
                "lookup-comparisons",
                "ApplicationProfileApprovalLegVersions.calik-energi.md");

        int? maxRows = null;
        var maxRowsText = GetOptionValue(args, "--max-rows");
        if (int.TryParse(maxRowsText, out var parsedMax) && parsedMax > 0)
            maxRows = parsedMax;

        Console.WriteLine("=== VISA2014 ApplicationProfile approval-leg version matrix (Phase A)");
        Console.WriteLine($"INF Legacy source: {source.Id} ({source.Label})");
        Console.WriteLine($"INF Profile catalog: {Path.GetFullPath(profileCatalog)}");
        Console.WriteLine($"INF Approval leg catalog: {Path.GetFullPath(approvalCatalog)}");
        Console.WriteLine($"INF Seed JSON: {Path.GetFullPath(seedJson)}");
        Console.WriteLine($"INF Matrix report: {Path.GetFullPath(matrixMd)}");
        if (maxRows.HasValue)
            Console.WriteLine($"INF Max rows: {maxRows.Value}");

        try
        {
            var result = Visa2014ApplicationProfileApprovalLegVersionMatrixExporter.Export(
                source.ConnectionString,
                source.LookupTranslationPaths,
                profileCatalog,
                approvalCatalog,
                seedJson,
                matrixMd,
                maxRows,
                verbose);

            Console.WriteLine($"OK Cells: {result.Cells.Count}");
            Console.WriteLine($"OK Apps scanned={result.AppsScanned} mapped={result.AppsMapped} skippedType={result.AppsSkippedType} noLeg={result.AppsNoProfileCode}");
            Console.WriteLine($"OK Via profiles without legacy apps: {result.ViaProfilesWithoutLegacyApps.Count}");
            Console.WriteLine($"OK Seed: {result.SeedJsonPath}");
            Console.WriteLine($"OK Matrix: {result.MatrixMarkdownPath}");
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
}