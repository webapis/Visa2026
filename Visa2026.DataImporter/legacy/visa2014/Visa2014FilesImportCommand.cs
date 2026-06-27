namespace Visa2026.DataImporter.Legacy.Visa2014;

internal static class Visa2014FilesImportCommand
{
    public static async Task<int> RunAsync(IReadOnlyList<string> args, bool verbose)
    {
        var entity = GetOptionValue(args, "--entity");
        if (string.IsNullOrWhiteSpace(entity))
        {
            Console.Error.WriteLine("ERR --import-visa2014-files requires --entity <Name> (e.g. Person, Passport).");
            return 1;
        }

        var property = GetOptionValue(args, "--property");
        if (string.IsNullOrWhiteSpace(property))
        {
            Console.Error.WriteLine("ERR --import-visa2014-files requires --property <Name> (e.g. Photo, PassportDocument).");
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

        var apiBaseUrl = GetOptionValue(args, "--api-base-url")
            ?? Environment.GetEnvironmentVariable("ApiOptions__BaseUrl")
            ?? Environment.GetEnvironmentVariable("API_BASE_URL")
            ?? "https://localhost:5001";
        var userName = GetOptionValue(args, "--user") ?? "Admin";
        var password = GetOptionValue(args, "--password") ?? "";

        int? maxRows = null;
        var maxRowsText = GetOptionValue(args, "--max-rows");
        if (int.TryParse(maxRowsText, out var parsedMax) && parsedMax > 0)
            maxRows = parsedMax;

        bool dryRun = HasArg(args, "--dry-run");
        bool noWait = HasArg(args, "--no-wait");

        Console.WriteLine($"=== VISA2014 file import — {entity}.{property}");
        Console.WriteLine($"INF Legacy source: {source.Id} ({source.Label})");
        Console.WriteLine($"INF Legacy (read-only): {Visa2014LegacySqlGuard.DescribeLegacyConnection(source.ConnectionString, source.LegacyDatabase)}");
        Console.WriteLine($"INF Target (write): Visa2026 via OData at {apiBaseUrl}");
        if (maxRows.HasValue)
            Console.WriteLine($"INF Max rows: {maxRows.Value}");
        if (dryRun)
            Console.WriteLine("INF Mode: dry-run (no POST)");

        if (!dryRun)
        {
            try
            {
                Visa2014LegacySqlGuard.EnsureLegacyReadCredentials(source.ConnectionString);
                await Visa2014LegacySqlGuard.EnsureLegacyConnectionAsync(source.ConnectionString);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"ERR {ex.Message}");
                return 1;
            }
        }

        var api = new Visa2026.DataImporter.ApiClient(apiBaseUrl, userName, password) { Verbose = verbose };

        if (!dryRun)
        {
            if (!noWait)
                await api.WaitForServerAsync();
            await api.LoginAsync();
        }

        try
        {
            if (string.Equals(entity, "Person", StringComparison.OrdinalIgnoreCase))
                return await RunPersonFileImportAsync(api, source, dataImporterRoot, args, property, maxRows, dryRun, verbose);

            if (string.Equals(entity, "Passport", StringComparison.OrdinalIgnoreCase))
                return await RunPassportFileImportAsync(api, source, dataImporterRoot, args, property, maxRows, dryRun, verbose);

            if (string.Equals(entity, "Visa", StringComparison.OrdinalIgnoreCase))
                return await RunVisaFileImportAsync(api, source, dataImporterRoot, args, property, maxRows, dryRun, verbose);

            if (string.Equals(entity, "Education", StringComparison.OrdinalIgnoreCase))
                return await RunEducationFileImportAsync(api, source, dataImporterRoot, args, property, maxRows, dryRun, verbose);

            Console.Error.WriteLine($"ERR Entity '{entity}' is not supported yet. Supported: Person, Passport, Visa, Education.");
            return 1;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ERR File import failed: {ex.Message}");
            if (verbose)
                Console.Error.WriteLine(ex);
            return 1;
        }
    }

    private static async Task<int> RunPersonFileImportAsync(
        Visa2026.DataImporter.ApiClient api,
        Visa2014LegacySourceProfile source,
        string dataImporterRoot,
        IReadOnlyList<string> args,
        string property,
        int? maxRows,
        bool dryRun,
        bool verbose)
    {
        var idMapPath = GetOptionValue(args, "--id-map")
            ?? GetOptionValue(args, "--id-map-output")
            ?? source.IdMapPath(dataImporterRoot, "Person");

        Console.WriteLine($"INF Id-map: {idMapPath}");

        var isPhoto = string.Equals(property, "Photo", StringComparison.OrdinalIgnoreCase);
        var isFamilyText = string.Equals(property, "VisaApplicationFamilyMembersText", StringComparison.OrdinalIgnoreCase);
        if (!isPhoto && !isFamilyText)
        {
            Console.Error.WriteLine($"ERR Property '{property}' is not supported for Person. Supported: Photo, VisaApplicationFamilyMembersText.");
            return 1;
        }

        if (isPhoto)
        {
            var result = await Visa2014PersonPhotoImporter.RunAsync(
                api,
                source.ConnectionString,
                idMapPath,
                maxRows,
                dryRun,
                verbose);

            Console.WriteLine($"INF Id-map entries: {result.IdMapEntries}");
            Console.WriteLine($"INF Processed: {result.Processed}  Patched: {result.Patched}  No blob: {result.SkippedNoBlob}  Failed: {result.Failed}");

            foreach (var error in result.Errors.Take(20))
                Console.Error.WriteLine($"ERR {error}");
            if (result.Errors.Count > 20)
                Console.Error.WriteLine($"ERR ... and {result.Errors.Count - 20} more");

            return result.Failed > 0 ? 1 : 0;
        }

        var familyResult = await Visa2014PersonVisaFamilyTextImporter.RunAsync(
            api,
            source.ConnectionString,
            idMapPath,
            maxRows,
            dryRun,
            verbose);

        Console.WriteLine($"INF Id-map entries: {familyResult.IdMapEntries}");
        Console.WriteLine(
            $"INF Processed: {familyResult.Processed}  Patched: {familyResult.Patched}  " +
            $"Single→Ýok: {familyResult.PatchedSingleNone}  " +
            $"Not employee: {familyResult.SkippedNotEmployee}  No StatusL text: {familyResult.SkippedNoText}  Failed: {familyResult.Failed}");

        foreach (var error in familyResult.Errors.Take(20))
            Console.Error.WriteLine($"ERR {error}");
        if (familyResult.Errors.Count > 20)
            Console.Error.WriteLine($"ERR ... and {familyResult.Errors.Count - 20} more");

        return familyResult.Failed > 0 ? 1 : 0;
    }

    private static async Task<int> RunPassportFileImportAsync(
        Visa2026.DataImporter.ApiClient api,
        Visa2014LegacySourceProfile source,
        string dataImporterRoot,
        IReadOnlyList<string> args,
        string property,
        int? maxRows,
        bool dryRun,
        bool verbose)
    {
        var isPassportDocument = string.Equals(property, "PassportDocument", StringComparison.OrdinalIgnoreCase)
            || string.Equals(property, "PassportCopy", StringComparison.OrdinalIgnoreCase);
        if (!isPassportDocument)
        {
            Console.Error.WriteLine($"ERR Property '{property}' is not supported for Passport. Supported: PassportDocument (PassportCopy).");
            return 1;
        }

        var passportIdMapPath = GetOptionValue(args, "--passport-id-map")
            ?? GetOptionValue(args, "--id-map")
            ?? source.IdMapPath(dataImporterRoot, "Passport");
        var copyIdMapPath = GetOptionValue(args, "--copy-id-map-output")
            ?? source.IdMapPath(dataImporterRoot, "PassportCopy");

        Console.WriteLine($"INF Passport id-map: {passportIdMapPath}");
        Console.WriteLine($"INF Copy id-map: {copyIdMapPath}");

        var result = await Visa2014PassportCopyImporter.RunAsync(
            api,
            source.ConnectionString,
            passportIdMapPath,
            dryRun ? null : copyIdMapPath,
            maxRows,
            dryRun,
            verbose);

        Console.WriteLine($"INF Passport id-map entries: {result.PassportIdMapEntries}");
        Console.WriteLine($"INF Legacy copy rows: {result.LegacyCopyRows}");
        Console.WriteLine(
            $"INF Posted: {result.Posted}  Failed: {result.Failed}  " +
            $"No passport map: {result.SkippedNoPassportMap}  No blob: {result.SkippedNoBlob}  " +
            $"Oversize (>5MB): {result.SkippedOversize}  Already imported: {result.SkippedAlreadyImported}  " +
            $"Duplicate blob: {result.SkippedDuplicateBlob}");
        if (result.CopyIdMapPath != null)
            Console.WriteLine($"INF Copy id-map: {result.CopyIdMapPath}");

        foreach (var error in result.Errors.Take(20))
            Console.Error.WriteLine($"ERR {error}");
        if (result.Errors.Count > 20)
            Console.Error.WriteLine($"ERR ... and {result.Errors.Count - 20} more");

        return result.Failed > 0 ? 1 : 0;
    }

    private static async Task<int> RunVisaFileImportAsync(
        Visa2026.DataImporter.ApiClient api,
        Visa2014LegacySourceProfile source,
        string dataImporterRoot,
        IReadOnlyList<string> args,
        string property,
        int? maxRows,
        bool dryRun,
        bool verbose)
    {
        var isVisaDocument = string.Equals(property, "VisaDocument", StringComparison.OrdinalIgnoreCase)
            || string.Equals(property, "GöçürmeNusga", StringComparison.OrdinalIgnoreCase);
        if (!isVisaDocument)
        {
            Console.Error.WriteLine($"ERR Property '{property}' is not supported for Visa. Supported: VisaDocument (GöçürmeNusga).");
            return 1;
        }

        var visaIdMapPath = GetOptionValue(args, "--visa-id-map")
            ?? GetOptionValue(args, "--id-map")
            ?? source.IdMapPath(dataImporterRoot, "Visa");
        var documentIdMapPath = GetOptionValue(args, "--document-id-map-output")
            ?? source.IdMapPath(dataImporterRoot, "VisaDocument");

        Console.WriteLine($"INF Visa id-map: {visaIdMapPath}");
        Console.WriteLine($"INF Document id-map: {documentIdMapPath}");

        var result = await Visa2014VisaDocumentImporter.RunAsync(
            api,
            source.ConnectionString,
            visaIdMapPath,
            dryRun ? null : documentIdMapPath,
            maxRows,
            dryRun,
            verbose);

        Console.WriteLine($"INF Visa id-map entries: {result.VisaIdMapEntries}");
        Console.WriteLine($"INF Rows with blob processed: {result.LegacyRowsWithBlob}");
        Console.WriteLine(
            $"INF Posted: {result.Posted}  Failed: {result.Failed}  " +
            $"No visa map: {result.SkippedNoVisaMap}  No blob: {result.SkippedNoBlob}  " +
            $"Oversize (>5MB): {result.SkippedOversize}  Already imported: {result.SkippedAlreadyImported}");
        if (result.DocumentIdMapPath != null)
            Console.WriteLine($"INF Document id-map: {result.DocumentIdMapPath}");

        foreach (var error in result.Errors.Take(20))
            Console.Error.WriteLine($"ERR {error}");
        if (result.Errors.Count > 20)
            Console.Error.WriteLine($"ERR ... and {result.Errors.Count - 20} more");

        return result.Failed > 0 ? 1 : 0;
    }

    private static async Task<int> RunEducationFileImportAsync(
        Visa2026.DataImporter.ApiClient api,
        Visa2014LegacySourceProfile source,
        string dataImporterRoot,
        IReadOnlyList<string> args,
        string property,
        int? maxRows,
        bool dryRun,
        bool verbose)
    {
        var isEducationDocument = string.Equals(property, "EducationDocument", StringComparison.OrdinalIgnoreCase)
            || string.Equals(property, "DiplomaCopy", StringComparison.OrdinalIgnoreCase)
            || string.Equals(property, "Diploma", StringComparison.OrdinalIgnoreCase);
        if (!isEducationDocument)
        {
            Console.Error.WriteLine($"ERR Property '{property}' is not supported for Education. Supported: EducationDocument (DiplomaCopy).");
            return 1;
        }

        var educationIdMapPath = GetOptionValue(args, "--education-id-map")
            ?? GetOptionValue(args, "--id-map")
            ?? source.IdMapPath(dataImporterRoot, "Education");
        var documentIdMapPath = GetOptionValue(args, "--document-id-map-output")
            ?? source.IdMapPath(dataImporterRoot, "EducationDocument");

        Console.WriteLine($"INF Education id-map: {educationIdMapPath}");
        Console.WriteLine($"INF Document id-map: {documentIdMapPath}");

        var result = await Visa2014EducationDocumentImporter.RunAsync(
            api,
            source.ConnectionString,
            educationIdMapPath,
            dryRun ? null : documentIdMapPath,
            maxRows,
            dryRun,
            verbose);

        Console.WriteLine($"INF Education id-map entries: {result.EducationIdMapEntries}");
        Console.WriteLine($"INF Legacy diploma copy rows: {result.LegacyCopyRows}");
        Console.WriteLine(
            $"INF Posted: {result.Posted}  Failed: {result.Failed}  " +
            $"No education map: {result.SkippedNoEducationMap}  No blob: {result.SkippedNoBlob}  " +
            $"Oversize (>5MB): {result.SkippedOversize}  Already imported: {result.SkippedAlreadyImported}  " +
            $"Duplicate blob: {result.SkippedDuplicateBlob}");
        if (result.DocumentIdMapPath != null)
            Console.WriteLine($"INF Document id-map: {result.DocumentIdMapPath}");

        foreach (var error in result.Errors.Take(20))
            Console.Error.WriteLine($"ERR {error}");
        if (result.Errors.Count > 20)
            Console.Error.WriteLine($"ERR ... and {result.Errors.Count - 20} more");

        return result.Failed > 0 ? 1 : 0;
    }

    private static bool HasArg(IReadOnlyList<string> args, string flag) =>
        args.Any(a => string.Equals(a, flag, StringComparison.OrdinalIgnoreCase));

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
