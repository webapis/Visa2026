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

        if (!IsSupportedEntity(entity))
        {
            Console.Error.WriteLine($"ERR Entity '{entity}' is not supported yet. Supported: Person, Passport, Visa, Education, EmployeePositionHistory, EmployeeSalary, AddressOfResidence, WorkPermit, WorkPermitItem, Invitation, InvitationItem, Rejection, RejectionItem, PrivateHouse, Lodging, Hotel, Hospital, OtherSite, ApplicationProfileInstance, ApplicationProfileCatalog, ApplicationItem, ApplicationProfileInstanceProgress, ProjectContractMinistryLeg, ApplicationMigrationServiceInference.");
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

        var output = GetOptionValue(args, "--output")
                     ?? source.PreviewOutputPath(dataImporterRoot, entity);

        int? maxRows = null;
        var maxRowsText = GetOptionValue(args, "--max-rows");
        if (int.TryParse(maxRowsText, out var parsedMax) && parsedMax > 0)
            maxRows = parsedMax;

        Console.WriteLine($"=== VISA2014 Excel preview export — {entity}");
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
            var result = string.Equals(entity, "ApplicationMigrationServiceInference", StringComparison.OrdinalIgnoreCase)
                ? Visa2014ApplicationMigrationServiceInferencePreview.Export(
                    source.ConnectionString,
                    Visa2014MigrationServiceInferenceRules.ResolveRulesPath(solutionRoot),
                    output,
                    maxRows,
                    verbose,
                    source.Id)
                : string.Equals(entity, "ProjectContractMinistryLeg", StringComparison.OrdinalIgnoreCase)
                ? Visa2014ProjectContractMinistryLegPreviewExporter.Export(
                    source.ConnectionString,
                    source.LookupTranslationPaths,
                    output,
                    maxRows,
                    verbose,
                    source.Id)
                : string.Equals(entity, "ApplicationItem", StringComparison.OrdinalIgnoreCase)
                ? Visa2014ApplicationItemPreviewExporter.Export(
                    source.ConnectionString,
                    source.LookupTranslationPaths,
                    output,
                    maxRows,
                    verbose,
                    source.Id)
                : string.Equals(entity, "ApplicationProfileInstanceProgress", StringComparison.OrdinalIgnoreCase)
                ? Visa2014ApplicationProfileInstanceProgressPreviewExporter.Export(
                    source.ConnectionString,
                    source.LookupTranslationPaths,
                    output,
                    maxRows,
                    verbose,
                    source.Id)
                : string.Equals(entity, "Application", StringComparison.OrdinalIgnoreCase)
                ? Visa2014ApplicationPreviewExporter.Export(
                    source.ConnectionString,
                    source.LookupTranslationPaths,
                    output,
                    maxRows,
                    verbose,
                    source.Id)
                : string.Equals(entity, "ApplicationProfileCatalog", StringComparison.OrdinalIgnoreCase)
                ? Visa2014ApplicationProfileCatalogExporter.Export(
                    source.ConnectionString,
                    source.LookupTranslationPaths,
                    output,
                    maxRows,
                    verbose,
                    source.Id)
                : string.Equals(entity, "Hospital", StringComparison.OrdinalIgnoreCase)
                ? Visa2014HospitalPreviewExporter.Export(
                    source.ConnectionString,
                    source.LookupTranslationPaths,
                    output,
                    maxRows,
                    verbose,
                    source.Id)
                : string.Equals(entity, "Hotel", StringComparison.OrdinalIgnoreCase)
                ? Visa2014HotelPreviewExporter.Export(
                    source.ConnectionString,
                    source.LookupTranslationPaths,
                    output,
                    maxRows,
                    verbose,
                    source.Id)
                : string.Equals(entity, "Lodging", StringComparison.OrdinalIgnoreCase)
                ? Visa2014LodgingPreviewExporter.Export(
                    source.ConnectionString,
                    source.LookupTranslationPaths,
                    output,
                    maxRows,
                    verbose,
                    source.Id)
                : string.Equals(entity, "OtherSite", StringComparison.OrdinalIgnoreCase)
                ? Visa2014OtherSitePreviewExporter.Export(
                    source.ConnectionString,
                    source.LookupTranslationPaths,
                    output,
                    maxRows,
                    verbose,
                    source.Id)
                : string.Equals(entity, "PrivateHouse", StringComparison.OrdinalIgnoreCase)
                ? Visa2014PrivateHousePreviewExporter.Export(
                    source.ConnectionString,
                    source.LookupTranslationPaths,
                    output,
                    maxRows,
                    verbose,
                    source.Id)
                : string.Equals(entity, "AddressOfResidence", StringComparison.OrdinalIgnoreCase)
                ? Visa2014AddressOfResidencePreviewExporter.Export(
                    source.ConnectionString,
                    source.LookupTranslationPaths,
                    output,
                    maxRows,
                    verbose,
                    source.Id)
                : string.Equals(entity, "EmployeePositionHistory", StringComparison.OrdinalIgnoreCase)
                ? Visa2014EmployeePositionHistoryPreviewExporter.Export(
                    source.ConnectionString,
                    source.LookupTranslationPaths,
                    output,
                    maxRows,
                    verbose,
                    source.Id)
                : string.Equals(entity, "EmployeeSalary", StringComparison.OrdinalIgnoreCase)
                ? Visa2014EmployeeSalaryPreviewExporter.Export(
                    source.ConnectionString,
                    source.LookupTranslationPaths,
                    output,
                    maxRows,
                    verbose,
                    source.Id)
                : string.Equals(entity, "WorkPermit", StringComparison.OrdinalIgnoreCase)
                ? Visa2014WorkPermitPreviewExporter.Export(
                    source.ConnectionString,
                    source.LookupTranslationPaths,
                    output,
                    maxRows,
                    verbose,
                    source.Id)
                : string.Equals(entity, "WorkPermitItem", StringComparison.OrdinalIgnoreCase)
                ? Visa2014WorkPermitItemPreviewExporter.Export(
                    source.ConnectionString,
                    source.LookupTranslationPaths,
                    output,
                    maxRows,
                    verbose,
                    source.Id)
                : string.Equals(entity, "Invitation", StringComparison.OrdinalIgnoreCase)
                ? Visa2014InvitationPreviewExporter.Export(
                    source.ConnectionString,
                    source.LookupTranslationPaths,
                    output,
                    maxRows,
                    verbose,
                    source.Id)
                : string.Equals(entity, "InvitationItem", StringComparison.OrdinalIgnoreCase)
                ? Visa2014InvitationItemPreviewExporter.Export(
                    source.ConnectionString,
                    source.LookupTranslationPaths,
                    output,
                    maxRows,
                    verbose,
                    source.Id)
                : string.Equals(entity, "Rejection", StringComparison.OrdinalIgnoreCase)
                ? Visa2014RejectionPreviewExporter.Export(
                    source.ConnectionString,
                    source.LookupTranslationPaths,
                    output,
                    maxRows,
                    verbose,
                    source.Id)
                : string.Equals(entity, "RejectionItem", StringComparison.OrdinalIgnoreCase)
                ? Visa2014RejectionItemPreviewExporter.Export(
                    source.ConnectionString,
                    source.LookupTranslationPaths,
                    output,
                    maxRows,
                    verbose,
                    source.Id)
                : string.Equals(entity, "Education", StringComparison.OrdinalIgnoreCase)
                ? Visa2014EducationPreviewExporter.Export(
                    source.ConnectionString,
                    source.LookupTranslationPaths,
                    output,
                    maxRows,
                    verbose,
                    source.Id)
                : string.Equals(entity, "Visa", StringComparison.OrdinalIgnoreCase)
                ? Visa2014VisaPreviewExporter.Export(
                    source.ConnectionString,
                    source.LookupTranslationPaths,
                    output,
                    maxRows,
                    verbose,
                    source.Id)
                : string.Equals(entity, "Passport", StringComparison.OrdinalIgnoreCase)
                ? Visa2014PassportPreviewExporter.Export(
                    source.ConnectionString,
                    source.LookupTranslationPaths,
                    output,
                    maxRows,
                    verbose,
                    source.Id)
                : Visa2014PersonPreviewExporter.Export(
                    source.ConnectionString,
                    source.LookupTranslationPaths,
                    output,
                    maxRows,
                    verbose,
                    source.Id);

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

    private static bool IsSupportedEntity(string entity) =>
        string.Equals(entity, "Person", StringComparison.OrdinalIgnoreCase)
        || string.Equals(entity, "Passport", StringComparison.OrdinalIgnoreCase)
        || string.Equals(entity, "Visa", StringComparison.OrdinalIgnoreCase)
        || string.Equals(entity, "Education", StringComparison.OrdinalIgnoreCase)
        || string.Equals(entity, "EmployeePositionHistory", StringComparison.OrdinalIgnoreCase)
        || string.Equals(entity, "EmployeeSalary", StringComparison.OrdinalIgnoreCase)
        || string.Equals(entity, "WorkPermit", StringComparison.OrdinalIgnoreCase)
        || string.Equals(entity, "WorkPermitItem", StringComparison.OrdinalIgnoreCase)
        || string.Equals(entity, "Invitation", StringComparison.OrdinalIgnoreCase)
        || string.Equals(entity, "InvitationItem", StringComparison.OrdinalIgnoreCase)
        || string.Equals(entity, "Rejection", StringComparison.OrdinalIgnoreCase)
        || string.Equals(entity, "RejectionItem", StringComparison.OrdinalIgnoreCase)
        || string.Equals(entity, "AddressOfResidence", StringComparison.OrdinalIgnoreCase)
        || string.Equals(entity, "PrivateHouse", StringComparison.OrdinalIgnoreCase)
        || string.Equals(entity, "Lodging", StringComparison.OrdinalIgnoreCase)
        || string.Equals(entity, "Hotel", StringComparison.OrdinalIgnoreCase)
        || string.Equals(entity, "Hospital", StringComparison.OrdinalIgnoreCase)
        || string.Equals(entity, "OtherSite", StringComparison.OrdinalIgnoreCase)
        || string.Equals(entity, "Application", StringComparison.OrdinalIgnoreCase)
        || string.Equals(entity, "ApplicationProfileCatalog", StringComparison.OrdinalIgnoreCase)
        || string.Equals(entity, "ApplicationItem", StringComparison.OrdinalIgnoreCase)
        || string.Equals(entity, "ApplicationProfileInstanceProgress", StringComparison.OrdinalIgnoreCase)
        || string.Equals(entity, "ProjectContractMinistryLeg", StringComparison.OrdinalIgnoreCase)
        || string.Equals(entity, "ApplicationMigrationServiceInference", StringComparison.OrdinalIgnoreCase);

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
