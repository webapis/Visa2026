namespace Visa2026.DataImporter.Legacy.Visa2014;

internal static class Visa2014ImportCommand
{
    public static async Task<int> RunAsync(IReadOnlyList<string> args, bool verbose)
    {
        var entity = GetOptionValue(args, "--entity");
        if (string.IsNullOrWhiteSpace(entity))
        {
            Console.Error.WriteLine("ERR --import-visa2014 requires --entity <Name> (e.g. Person).");
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

        var apiBaseUrl = GetOptionValue(args, "--api-base-url")
            ?? Environment.GetEnvironmentVariable("ApiOptions__BaseUrl")
            ?? Environment.GetEnvironmentVariable("API_BASE_URL")
            ?? "https://localhost:5001";
        var userName = GetOptionValue(args, "--user") ?? "Admin";
        var password = GetOptionValue(args, "--password") ?? "";

        var legacyConnection = Visa2014ContentRoot.ResolveConnectionString(GetOptionValue(args, "--connection"));
        var idMapPath = GetOptionValue(args, "--id-map-output")
            ?? Path.Combine(Visa2014ContentRoot.LegacyRoot(dataImporterRoot), "id-maps", "Person.json");

        int? maxRows = null;
        var maxRowsText = GetOptionValue(args, "--max-rows");
        if (int.TryParse(maxRowsText, out var parsedMax) && parsedMax > 0)
            maxRows = parsedMax;

        bool dryRun = HasArg(args, "--dry-run");
        bool noWait = HasArg(args, "--no-wait");

        Console.WriteLine($"=== VISA2014 OData import — {entity}");
        Console.WriteLine($"INF Legacy SQL: {MaskConnectionForLog(legacyConnection)}");
        Console.WriteLine($"INF Target API: {apiBaseUrl}");
        Console.WriteLine($"INF Lookup translations: {Path.GetFullPath(lookupPath)}");
        if (maxRows.HasValue)
            Console.WriteLine($"INF Max rows: {maxRows.Value}");
        if (dryRun)
            Console.WriteLine("INF Mode: dry-run (no POST)");

        var api = new Visa2026.DataImporter.ApiClient(apiBaseUrl, userName, password) { Verbose = verbose };

        if (!dryRun)
        {
            if (!noWait)
                await api.WaitForServerAsync();
            await api.LoginAsync();
        }

        try
        {
            var result = await Visa2014PersonODataImporter.RunAsync(
                api,
                legacyConnection,
                lookupPath,
                dryRun ? null : idMapPath,
                maxRows,
                dryRun,
                verbose);

            Console.WriteLine($"INF Legacy SQL rows: {result.LegacyRowCount}");
            Console.WriteLine($"INF Prepared: {result.PreparedCount}  Skipped: {result.SkippedCount}  Dedupe merged: {result.DedupeMergedCount}");
            if (!dryRun)
            {
                Console.WriteLine($"INF Posted: {result.PostedCount}  Failed: {result.FailedCount}");
                if (result.IdMapPath != null)
                    Console.WriteLine($"INF Id-map: {result.IdMapPath}");
            }

            return result.FailedCount > 0 ? 1 : 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ERR Import failed: {ex.Message}");
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
