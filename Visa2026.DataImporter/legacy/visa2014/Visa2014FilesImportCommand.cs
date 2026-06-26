namespace Visa2026.DataImporter.Legacy.Visa2014;

internal static class Visa2014FilesImportCommand
{
    public static async Task<int> RunAsync(IReadOnlyList<string> args, bool verbose)
    {
        var entity = GetOptionValue(args, "--entity");
        if (string.IsNullOrWhiteSpace(entity))
        {
            Console.Error.WriteLine("ERR --import-visa2014-files requires --entity <Name> (e.g. Person).");
            return 1;
        }

        var property = GetOptionValue(args, "--property");
        if (string.IsNullOrWhiteSpace(property))
        {
            Console.Error.WriteLine("ERR --import-visa2014-files requires --property <Name> (e.g. Photo).");
            return 1;
        }

        if (!string.Equals(entity, "Person", StringComparison.OrdinalIgnoreCase))
        {
            Console.Error.WriteLine($"ERR Entity '{entity}' is not supported yet. Supported: Person.");
            return 1;
        }

        var isPhoto = string.Equals(property, "Photo", StringComparison.OrdinalIgnoreCase);
        var isFamilyText = string.Equals(property, "VisaApplicationFamilyMembersText", StringComparison.OrdinalIgnoreCase);
        if (!isPhoto && !isFamilyText)
        {
            Console.Error.WriteLine($"ERR Property '{property}' is not supported yet. Supported: Photo, VisaApplicationFamilyMembersText.");
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

        var idMapPath = GetOptionValue(args, "--id-map")
            ?? GetOptionValue(args, "--id-map-output")
            ?? source.IdMapPath(dataImporterRoot, entity);

        int? maxRows = null;
        var maxRowsText = GetOptionValue(args, "--max-rows");
        if (int.TryParse(maxRowsText, out var parsedMax) && parsedMax > 0)
            maxRows = parsedMax;

        bool dryRun = HasArg(args, "--dry-run");
        bool noWait = HasArg(args, "--no-wait");

        Console.WriteLine($"=== VISA2014 file import — {entity}.{property}");
        Console.WriteLine($"INF Legacy source: {source.Id} ({source.Label})");
        Console.WriteLine($"INF Legacy SQL: {MaskConnectionForLog(source.ConnectionString)}");
        Console.WriteLine($"INF Target API: {apiBaseUrl}");
        Console.WriteLine($"INF Id-map: {idMapPath}");
        if (maxRows.HasValue)
            Console.WriteLine($"INF Max rows: {maxRows.Value}");
        if (dryRun)
            Console.WriteLine("INF Mode: dry-run (no PATCH)");

        var api = new Visa2026.DataImporter.ApiClient(apiBaseUrl, userName, password) { Verbose = verbose };

        if (!dryRun)
        {
            if (!noWait)
                await api.WaitForServerAsync();
            await api.LoginAsync();
        }

        try
        {
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
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ERR File import failed: {ex.Message}");
            if (verbose)
                Console.Error.WriteLine(ex);
            return 1;
        }
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
