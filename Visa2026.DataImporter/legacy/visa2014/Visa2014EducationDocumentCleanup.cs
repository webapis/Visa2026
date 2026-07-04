using Microsoft.Data.SqlClient;

namespace Visa2026.DataImporter.Legacy.Visa2014;

internal sealed class Visa2014EducationDocumentCleanupResult
{
    public int TotalDocuments { get; init; }
    public int DuplicatesRemoved { get; init; }
    public int Renamed { get; init; }
    public int AlreadyCorrectlyNamed { get; init; }
    public int Failed { get; init; }
    public string? DocumentIdMapPath { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = [];
}

/// <summary>
/// Removes duplicate diploma blobs per Education and renames legacy passport-copy-* FileData rows to diploma-*.
/// </summary>
internal static class Visa2014EducationDocumentCleanup
{
    private const string LoadDocumentsSql = """
        SELECT
            CAST(ed.ID AS varchar(36)) AS DocId,
            CAST(ed.EducationID AS varchar(36)) AS EducationId,
            CAST(ed.FileID AS varchar(36)) AS FileId,
            fd.FileName,
            fd.Content,
            LTRIM(RTRIM(COALESCE(p.FirstName, N'') + N' ' + COALESCE(p.LastName, N''))) AS PersonFullName
        FROM EducationDocument ed
        INNER JOIN FileData fd ON ed.FileID = fd.ID
        INNER JOIN Educations e ON ed.EducationID = e.ID
        INNER JOIN People p ON e.PersonID = p.ID
        WHERE ed.GCRecord = 0
        ORDER BY ed.EducationID, ed.ID
        """;

    private sealed record ExistingDoc(
        Guid DocId,
        Guid EducationId,
        Guid FileId,
        string FileName,
        byte[] Content,
        string? PersonFullName);

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

        Console.WriteLine("=== VISA2014 EducationDocument cleanup");
        Console.WriteLine($"INF Legacy source: {source.Id} ({source.Label})");
        Console.WriteLine($"INF Target SQL: {MaskConnectionForLog(targetConnection)}");
        Console.WriteLine($"INF Target API: {apiBaseUrl}");
        Console.WriteLine($"INF Document id-map: {documentIdMapPath}");
        if (dryRun)
            Console.WriteLine("INF Mode: dry-run (no DELETE/PATCH)");

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

            Console.WriteLine($"INF Total documents scanned: {result.TotalDocuments}");
            Console.WriteLine(
                $"INF Duplicates removed: {result.DuplicatesRemoved}  Renamed: {result.Renamed}  " +
                $"Already correctly named: {result.AlreadyCorrectlyNamed}  Failed: {result.Failed}");
            if (result.DocumentIdMapPath != null)
                Console.WriteLine($"INF Document id-map: {result.DocumentIdMapPath}");

            foreach (var error in result.Errors.Take(20))
                Console.Error.WriteLine($"ERR {error}");
            if (result.Errors.Count > 20)
                Console.Error.WriteLine($"ERR ... and {result.Errors.Count - 20} more");

            return result.Failed > 0 ? 1 : 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ERR Cleanup failed: {ex.Message}");
            if (verbose)
                Console.Error.WriteLine(ex);
            return 1;
        }
    }

    public static async Task<Visa2014EducationDocumentCleanupResult> RunAsync(
        Visa2026.DataImporter.ApiClient api,
        string targetConnectionString,
        string documentIdMapPath,
        bool dryRun,
        bool verbose)
    {
        var docs = await LoadExistingDocumentsAsync(targetConnectionString);
        var docMap = File.Exists(documentIdMapPath)
            ? Visa2014IdMapHelper.Load(documentIdMapPath)
            : new Dictionary<Guid, Guid>();

        var duplicatesToDelete = new List<Guid>();
        var renames = new List<(Guid FileId, string NewFileName, Guid DocId)>();
        int alreadyCorrectlyNamed = 0;

        foreach (var group in docs.GroupBy(d => d.EducationId))
        {
            var importedBlobKeys = new HashSet<string>(StringComparer.Ordinal);
            var copyIndexByEducation = new Dictionary<Guid, int>();

            foreach (var doc in group.OrderBy(d => d.DocId))
            {
                if (!Visa2014LegacyBlobDedupeHelper.TryRegisterDistinctBlob(
                        importedBlobKeys,
                        copyIndexByEducation,
                        doc.EducationId,
                        doc.Content,
                        out var copyIndex))
                {
                    duplicatesToDelete.Add(doc.DocId);
                    if (verbose)
                        Console.WriteLine($"  DELETE duplicate EducationDocument {doc.DocId} (Education {doc.EducationId})");
                    continue;
                }

                var targetFileName = Visa2014LegacyFileNameHelper.BuildDiplomaCopyFileName(
                    doc.PersonFullName,
                    doc.Content,
                    copyIndex);

                if (string.Equals(doc.FileName, targetFileName, StringComparison.Ordinal))
                {
                    alreadyCorrectlyNamed++;
                    continue;
                }

                renames.Add((doc.FileId, targetFileName, doc.DocId));
                if (verbose)
                    Console.WriteLine($"  RENAME {doc.FileId}: {doc.FileName} -> {targetFileName}");
            }
        }

        Console.WriteLine(
            $"INF Plan: delete {duplicatesToDelete.Count} duplicate(s), rename {renames.Count}, keep {alreadyCorrectlyNamed} unchanged");

        if (dryRun)
        {
            return new Visa2014EducationDocumentCleanupResult
            {
                TotalDocuments = docs.Count,
                DuplicatesRemoved = duplicatesToDelete.Count,
                Renamed = renames.Count,
                AlreadyCorrectlyNamed = alreadyCorrectlyNamed,
            };
        }

        var errors = new List<string>();
        int failed = 0;
        int removed = 0;
        int renamed = 0;

        foreach (var docId in duplicatesToDelete)
        {
            try
            {
                await api.DeleteAsync("EducationDocument", docId);
                removed++;
                if (removed % 50 == 0)
                    Console.WriteLine($"INF Progress: {removed} duplicate(s) deleted...");
            }
            catch (Exception ex)
            {
                failed++;
                errors.Add($"DELETE EducationDocument {docId}: {ex.Message}");
            }
        }

        var deletedDocIds = duplicatesToDelete.ToHashSet();
        if (deletedDocIds.Count > 0 && docMap.Count > 0)
        {
            var legacyKeysToRemove = docMap
                .Where(kvp => deletedDocIds.Contains(kvp.Value))
                .Select(kvp => kvp.Key)
                .ToList();
            foreach (var legacyKey in legacyKeysToRemove)
                docMap.Remove(legacyKey);
        }

        foreach (var (fileId, newFileName, docId) in renames)
        {
            try
            {
                await api.UpdateAsync("FileData", fileId, new Dictionary<string, object?>
                {
                    ["FileName"] = newFileName,
                });
                renamed++;
                if (renamed % 200 == 0)
                    Console.WriteLine($"INF Progress: {renamed} file(s) renamed...");
            }
            catch (Exception ex)
            {
                failed++;
                errors.Add($"PATCH FileData {fileId} (EducationDocument {docId}): {ex.Message}");
            }
        }

        string? savedMapPath = null;
        if (deletedDocIds.Count > 0 && !string.IsNullOrWhiteSpace(documentIdMapPath))
        {
            savedMapPath = Path.GetFullPath(documentIdMapPath);
            await Visa2014IdMapHelper.SaveAsync(savedMapPath, docMap);
        }

        return new Visa2014EducationDocumentCleanupResult
        {
            TotalDocuments = docs.Count,
            DuplicatesRemoved = removed,
            Renamed = renamed,
            AlreadyCorrectlyNamed = alreadyCorrectlyNamed,
            Failed = failed,
            DocumentIdMapPath = savedMapPath,
            Errors = errors,
        };
    }

    private static async Task<List<ExistingDoc>> LoadExistingDocumentsAsync(string targetConnectionString)
    {
        var docs = new List<ExistingDoc>();
        await using var connection = new SqlConnection(targetConnectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand(LoadDocumentsSql, connection);
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            if (!Guid.TryParse(reader.GetString(0), out var docId))
                continue;
            if (!Guid.TryParse(reader.GetString(1), out var educationId))
                continue;
            if (!Guid.TryParse(reader.GetString(2), out var fileId))
                continue;

            var fileName = reader.IsDBNull(3) ? "" : reader.GetString(3);
            if (reader.IsDBNull(4))
                continue;

            var content = (byte[])reader.GetValue(4);
            var personFullName = reader.IsDBNull(5) ? null : reader.GetString(5);
            docs.Add(new ExistingDoc(docId, educationId, fileId, fileName, content, personFullName));
        }

        return docs;
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
