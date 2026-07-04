using Microsoft.Data.SqlClient;

namespace Visa2026.DataImporter.Legacy.Visa2014;

internal sealed class Visa2014AddressOfResidencePurgeResult
{
    public int TotalRows { get; init; }
    public int Deleted { get; init; }
    public int Failed { get; init; }
    public string? IdMapPath { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = [];
}

/// <summary>
/// Deletes all active AddressOfResidence rows via OData and clears the import id-map (re-import after classifier fix).
/// </summary>
internal static class Visa2014AddressOfResidencePurge
{
    private const string ListIdsSql = """
        SELECT CAST(ID AS varchar(36)) AS RowId
        FROM AddressOfResidence
        WHERE GCRecord IS NULL OR GCRecord = 0
        ORDER BY ID
        """;

    private sealed class AddressOfResidenceIdRow
    {
        [System.Text.Json.Serialization.JsonPropertyName("ID")]
        public Guid Id { get; set; }
    }

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
        var idMapPath = GetOptionValue(args, "--id-map-output")
            ?? source.IdMapPath(dataImporterRoot, "AddressOfResidence");

        bool dryRun = HasArg(args, "--dry-run");
        bool noWait = HasArg(args, "--no-wait");

        Console.WriteLine("=== VISA2014 AddressOfResidence purge (delete all + clear id-map)");
        Console.WriteLine($"INF Legacy source: {source.Id} ({source.Label})");
        Console.WriteLine($"INF Target SQL: {MaskConnectionForLog(targetConnection)}");
        Console.WriteLine($"INF Target API: {apiBaseUrl}");
        Console.WriteLine($"INF AddressOfResidence id-map: {idMapPath}");
        if (dryRun)
            Console.WriteLine("INF Mode: dry-run (no DELETE)");

        var api = new Visa2026.DataImporter.ApiClient(apiBaseUrl, userName, password) { Verbose = verbose };

        if (!noWait)
            await api.WaitForServerAsync();
        await api.LoginAsync();

        try
        {
            var result = await RunAsync(
                api,
                targetConnection,
                idMapPath,
                dryRun,
                verbose);

            Console.WriteLine($"INF Total rows: {result.TotalRows}");
            Console.WriteLine($"INF Deleted: {result.Deleted}  Failed: {result.Failed}");
            if (result.IdMapPath != null)
                Console.WriteLine($"INF AddressOfResidence id-map cleared: {result.IdMapPath}");

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

    public static async Task<Visa2014AddressOfResidencePurgeResult> RunAsync(
        Visa2026.DataImporter.ApiClient api,
        string? targetConnectionString,
        string idMapPath,
        bool dryRun,
        bool verbose)
    {
        var rowIds = await ListRowIdsAsync(api, targetConnectionString, verbose);
        Console.WriteLine($"INF Found {rowIds.Count} active AddressOfResidence row(s) to delete");

        if (dryRun)
        {
            return new Visa2014AddressOfResidencePurgeResult
            {
                TotalRows = rowIds.Count,
                Deleted = rowIds.Count,
            };
        }

        var errors = new List<string>();
        int deleted = 0;
        int failed = 0;

        foreach (var rowId in rowIds)
        {
            try
            {
                await api.DeleteAsync("AddressOfResidence", rowId);
                deleted++;
                if (deleted % 250 == 0)
                    Console.WriteLine($"INF Progress: {deleted}/{rowIds.Count} deleted...");
                if (verbose)
                    Console.WriteLine($"  DELETE AddressOfResidence {rowId}");
            }
            catch (Exception ex)
            {
                failed++;
                errors.Add($"DELETE AddressOfResidence {rowId}: {ex.Message}");
            }
        }

        string? savedMapPath = null;
        if (failed == 0 && !string.IsNullOrWhiteSpace(idMapPath))
        {
            savedMapPath = Path.GetFullPath(idMapPath);
            Directory.CreateDirectory(Path.GetDirectoryName(savedMapPath)!);
            await Visa2014IdMapHelper.SaveAsync(savedMapPath, new Dictionary<Guid, Guid>());
        }

        return new Visa2014AddressOfResidencePurgeResult
        {
            TotalRows = rowIds.Count,
            Deleted = deleted,
            Failed = failed,
            IdMapPath = savedMapPath,
            Errors = errors,
        };
    }

    private static async Task<List<Guid>> ListRowIdsAsync(
        Visa2026.DataImporter.ApiClient api,
        string? targetConnectionString,
        bool verbose)
    {
        if (!string.IsNullOrWhiteSpace(targetConnectionString))
        {
            try
            {
                var sqlIds = await ListRowIdsFromSqlAsync(targetConnectionString);
                if (sqlIds.Count > 0)
                    return sqlIds;
            }
            catch (Exception ex)
            {
                if (verbose)
                    Console.WriteLine($"INF SQL row list failed ({ex.Message}); falling back to OData.");
            }
        }

        var rows = await api.GetAllAsync<AddressOfResidenceIdRow>("AddressOfResidence", "$select=ID");
        return rows.Where(r => r.Id != Guid.Empty).Select(r => r.Id).ToList();
    }

    private static async Task<List<Guid>> ListRowIdsFromSqlAsync(string targetConnectionString)
    {
        var ids = new List<Guid>();
        await using var connection = new SqlConnection(targetConnectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand(ListIdsSql, connection);
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            if (Guid.TryParse(reader.GetString(0), out var rowId))
                ids.Add(rowId);
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
