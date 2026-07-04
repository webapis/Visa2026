using Microsoft.Data.SqlClient;

namespace Visa2026.DataImporter.Legacy.Visa2014;

internal sealed class Visa2014EducationDocumentPurgeResult
{
    public int TotalDocuments { get; init; }
    public int Deleted { get; init; }
    public int Failed { get; init; }
    public string? DocumentIdMapPath { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = [];
}

/// <summary>
/// Deletes all active EducationDocument rows (diploma copies) via OData and clears the import id-map.
/// </summary>
internal static class Visa2014EducationDocumentPurge
{
    private const string ListDocumentIdsSql = """
        SELECT CAST(ed.ID AS varchar(36)) AS DocId
        FROM EducationDocument ed
        WHERE ed.GCRecord = 0
        ORDER BY ed.ID
        """;

    public static async Task<int> RunCommandAsync(IReadOnlyList<string> args, bool verbose)
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

        var apiBaseUrl = GetOptionValue(args, "--api-base-url")
            ?? Environment.GetEnvironmentVariable("ApiOptions__BaseUrl")
            ?? Environment.GetEnvironmentVariable("API_BASE_URL")
            ?? "https://localhost:5001";
        var userName = GetOptionValue(args, "--user") ?? "Admin";
        var password = GetOptionValue(args, "--password") ?? "";
        var targetConnection = GetOptionValue(args, "--target-connection")
            ?? Environment.GetEnvironmentVariable("VISA2026_SQL_CONNECTION")
            ?? "Server=(localdb)\\mssqllocaldb;Database=Visa2026;Trusted_Connection=True;TrustServerCertificate=True";
        var documentIdMapPath = GetOptionValue(args, "--document-id-map-output")
            ?? source.IdMapPath(dataImporterRoot, "EducationDocument");

        bool dryRun = HasArg(args, "--dry-run");
        bool noWait = HasArg(args, "--no-wait");

        Console.WriteLine("=== VISA2014 EducationDocument purge (delete all diploma copies)");
        Console.WriteLine($"INF Legacy source: {source.Id} ({source.Label})");
        Console.WriteLine($"INF Target SQL: {MaskConnectionForLog(targetConnection)}");
        Console.WriteLine($"INF Target API: {apiBaseUrl}");
        Console.WriteLine($"INF Document id-map: {documentIdMapPath}");
        if (dryRun)
            Console.WriteLine("INF Mode: dry-run (no DELETE)");

        var api = new Visa2026.DataImporter.ApiClient(apiBaseUrl, userName, password) { Verbose = verbose };

        if (!dryRun)
        {
            if (!noWait)
                await api.WaitForServerAsync();
            await api.LoginAsync();
        }

        try
        {
            var result = await RunAsync(
                api,
                targetConnection,
                documentIdMapPath,
                dryRun,
                verbose);

            Console.WriteLine($"INF Total documents: {result.TotalDocuments}");
            Console.WriteLine($"INF Deleted: {result.Deleted}  Failed: {result.Failed}");
            if (result.DocumentIdMapPath != null)
                Console.WriteLine($"INF Document id-map cleared: {result.DocumentIdMapPath}");

            foreach (var error in result.Errors.Take(20))
                Console.Error.WriteLine($"ERR {error}");
            if (result.Errors.Count > 20)
                Console.Error.WriteLine($"ERR ... and {result.Errors.Count - 20} more");

            return result.Failed > 0 ? 1 : 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ERR Purge failed: {ex.Message}");
            if (verbose)
                Console.Error.WriteLine(ex);
            return 1;
        }
    }

    public static async Task<Visa2014EducationDocumentPurgeResult> RunAsync(
        Visa2026.DataImporter.ApiClient api,
        string targetConnectionString,
        string documentIdMapPath,
        bool dryRun,
        bool verbose)
    {
        var docIds = await ListDocumentIdsAsync(targetConnectionString);
        Console.WriteLine($"INF Found {docIds.Count} active EducationDocument row(s) to delete");

        if (dryRun)
        {
            return new Visa2014EducationDocumentPurgeResult
            {
                TotalDocuments = docIds.Count,
                Deleted = docIds.Count,
            };
        }

        var errors = new List<string>();
        int deleted = 0;
        int failed = 0;

        foreach (var docId in docIds)
        {
            try
            {
                await api.DeleteAsync("EducationDocument", docId);
                deleted++;
                if (deleted % 200 == 0)
                    Console.WriteLine($"INF Progress: {deleted}/{docIds.Count} deleted...");
                if (verbose)
                    Console.WriteLine($"  DELETE EducationDocument {docId}");
            }
            catch (Exception ex)
            {
                failed++;
                errors.Add($"DELETE EducationDocument {docId}: {ex.Message}");
            }
        }

        string? savedMapPath = null;
        if (deleted > 0 && !string.IsNullOrWhiteSpace(documentIdMapPath))
        {
            savedMapPath = Path.GetFullPath(documentIdMapPath);
            await Visa2014IdMapHelper.SaveAsync(savedMapPath, new Dictionary<Guid, Guid>());
        }

        return new Visa2014EducationDocumentPurgeResult
        {
            TotalDocuments = docIds.Count,
            Deleted = deleted,
            Failed = failed,
            DocumentIdMapPath = savedMapPath,
            Errors = errors,
        };
    }

    private static async Task<List<Guid>> ListDocumentIdsAsync(string targetConnectionString)
    {
        var ids = new List<Guid>();
        await using var connection = new SqlConnection(targetConnectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand(ListDocumentIdsSql, connection);
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            if (Guid.TryParse(reader.GetString(0), out var docId))
                ids.Add(docId);
        }

        return ids;
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
        if (string.IsNullOrWhiteSpace(connectionString))
            return "(empty)";

        var parts = connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return string.Join(";", parts.Where(p =>
            !p.StartsWith("Password=", StringComparison.OrdinalIgnoreCase)
            && !p.StartsWith("Pwd=", StringComparison.OrdinalIgnoreCase)));
    }
}
