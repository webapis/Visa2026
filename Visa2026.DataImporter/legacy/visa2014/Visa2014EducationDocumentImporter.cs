using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Data.SqlClient;

namespace Visa2026.DataImporter.Legacy.Visa2014;

internal sealed class Visa2014EducationDocumentImportResult
{
    public int EducationIdMapEntries { get; init; }
    public int LegacyCopyRows { get; init; }
    public int Posted { get; init; }
    public int SkippedNoEducationMap { get; init; }
    public int SkippedNoBlob { get; init; }
    public int SkippedOversize { get; init; }
    public int SkippedAlreadyImported { get; init; }
    public int SkippedDuplicateBlob { get; init; }
    public int Failed { get; init; }
    public string? DocumentIdMapPath { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = [];
}

/// <summary>
/// Legacy diploma scans live on <c>dbo.PassportCopy</c> rows with <c>Education</c> FK (not on dbo.Education).
/// </summary>
internal static class Visa2014EducationDocumentImporter
{
    private const int MaxDocumentBytes = 5 * 1024 * 1024;

    private const string ListEducationCopiesSql = """
        SELECT
            CAST(pc.Oid AS varchar(36)) AS LegacyCopyOid,
            CAST(pc.Education AS varchar(36)) AS LegacyEducationOid,
            LTRIM(RTRIM(
                COALESCE(per.FirstName, N'') + N' ' + COALESCE(per.LastName, N'')
            )) AS PersonFullName
        FROM dbo.PassportCopy pc
        INNER JOIN dbo.Education e ON pc.Education = e.Oid AND e.GCRecord IS NULL
        INNER JOIN dbo.Person per ON e.Person = per.Oid AND per.GCRecord IS NULL
        WHERE pc.GCRecord IS NULL
          AND pc.Education IS NOT NULL
        ORDER BY pc.Education, pc.Oid
        """;

    public static async Task<Visa2014EducationDocumentImportResult> RunAsync(
        ApiClient api,
        string legacyConnectionString,
        string educationIdMapPath,
        string? documentIdMapOutputPath,
        int? maxRows,
        bool dryRun,
        bool verbose)
    {
        var educationIdMap = Visa2014IdMapHelper.Load(educationIdMapPath);
        var docMap = LoadOptionalDocumentIdMap(documentIdMapOutputPath);
        int mapEntriesAtStart = docMap.Count;

        await using var connection = new SqlConnection(legacyConnectionString);
        await connection.OpenAsync();
        var blobColumn = Visa2014LegacyBlobColumnResolver.GetVarbinaryColumnName(connection, "dbo.PassportCopy");

        var copyRows = await ListLegacyCopyRowsAsync(connection, maxRows);
        var errors = new List<string>();
        var importedBlobKeys = new HashSet<string>(StringComparer.Ordinal);
        var copyIndexByEducation = new Dictionary<Guid, int>();
        int posted = 0;
        int failed = 0;
        int skippedNoEducationMap = 0;
        int skippedNoBlob = 0;
        int skippedOversize = 0;
        int skippedAlreadyImported = 0;
        int skippedDuplicateBlob = 0;
        int postedSinceLastSave = 0;

        foreach (var (legacyCopyOid, legacyEducationOid, personFullName) in copyRows)
        {
            if (!educationIdMap.TryGetValue(legacyEducationOid, out var targetEducationId))
            {
                skippedNoEducationMap++;
                if (verbose)
                    Console.WriteLine($"  SKIP copy {legacyCopyOid}: Education {legacyEducationOid} not in id-map");
                continue;
            }

            byte[]? blob;
            try
            {
                blob = await ReadCopyBlobAsync(connection, blobColumn, legacyCopyOid);
            }
            catch (Exception ex)
            {
                failed++;
                errors.Add($"{legacyCopyOid}: SQL read failed — {ex.Message}");
                continue;
            }

            if (blob == null || blob.Length == 0)
            {
                skippedNoBlob++;
                continue;
            }

            if (blob.Length > MaxDocumentBytes)
            {
                skippedOversize++;
                if (verbose)
                    Console.WriteLine($"  SKIP copy {legacyCopyOid}: {blob.Length} bytes exceeds {MaxDocumentBytes} limit");
                continue;
            }

            if (docMap.ContainsKey(legacyCopyOid))
            {
                skippedAlreadyImported++;
                Visa2014LegacyBlobDedupeHelper.RegisterExistingBlob(
                    importedBlobKeys, copyIndexByEducation, targetEducationId, blob);
                continue;
            }

            if (!Visa2014LegacyBlobDedupeHelper.TryRegisterDistinctBlob(
                    importedBlobKeys, copyIndexByEducation, targetEducationId, blob, out var copyIndex))
            {
                skippedDuplicateBlob++;
                if (verbose)
                    Console.WriteLine($"  SKIP copy {legacyCopyOid}: duplicate blob for Education {targetEducationId} ({blob.Length} bytes)");
                continue;
            }

            var fileName = Visa2014LegacyFileNameHelper.BuildDiplomaCopyFileName(personFullName, blob, copyIndex);

            if (dryRun)
            {
                Console.WriteLine(
                    $"DRY RUN: POST EducationDocument ← copy {legacyCopyOid} education {targetEducationId} ({blob.Length} bytes, {fileName})");
                posted++;
                continue;
            }

            try
            {
                var fileCreated = await api.CreateAsync<FileDataImportRow>("FileData", new Dictionary<string, object?>
                {
                    ["FileName"] = fileName,
                    ["Content"] = blob,
                });
                if (fileCreated == null)
                {
                    failed++;
                    errors.Add($"{legacyCopyOid}: FileData POST returned null");
                    continue;
                }

                var payload = new Dictionary<string, object?>
                {
                    ["Education"] = new { ID = targetEducationId },
                    ["File"] = new { ID = fileCreated.Id },
                };

                var created = await api.CreateAsync<EducationDocumentImportRow>("EducationDocument", payload);
                if (created == null)
                {
                    failed++;
                    errors.Add($"{legacyCopyOid}: EducationDocument POST returned null");
                    continue;
                }

                docMap[legacyCopyOid] = created.Id;
                posted++;
                postedSinceLastSave++;
                if (posted % 100 == 0)
                    Console.WriteLine($"INF Progress: {posted} posted, {failed} failed, {skippedAlreadyImported} already imported, {skippedNoEducationMap} no education map...");
                if (verbose)
                    Console.WriteLine($"  POST EducationDocument {created.Id} ← copy {legacyCopyOid}");

                if (postedSinceLastSave >= 100 &&
                    !string.IsNullOrWhiteSpace(documentIdMapOutputPath))
                {
                    await Visa2014IdMapHelper.SaveAsync(documentIdMapOutputPath, docMap);
                    postedSinceLastSave = 0;
                }
            }
            catch (Exception ex)
            {
                failed++;
                errors.Add($"{legacyCopyOid}: {ex.Message}");
            }
        }

        string? documentIdMapPath = null;
        if (!dryRun && docMap.Count > mapEntriesAtStart && !string.IsNullOrWhiteSpace(documentIdMapOutputPath))
        {
            documentIdMapPath = Path.GetFullPath(documentIdMapOutputPath);
            await Visa2014IdMapHelper.SaveAsync(documentIdMapPath, docMap);
        }

        return new Visa2014EducationDocumentImportResult
        {
            EducationIdMapEntries = educationIdMap.Count,
            LegacyCopyRows = copyRows.Count,
            Posted = posted,
            SkippedNoEducationMap = skippedNoEducationMap,
            SkippedNoBlob = skippedNoBlob,
            SkippedOversize = skippedOversize,
            SkippedAlreadyImported = skippedAlreadyImported,
            SkippedDuplicateBlob = skippedDuplicateBlob,
            Failed = failed,
            DocumentIdMapPath = documentIdMapPath,
            Errors = errors,
        };
    }

    private static async Task<List<(Guid LegacyCopyOid, Guid LegacyEducationOid, string? PersonFullName)>> ListLegacyCopyRowsAsync(
        SqlConnection connection,
        int? maxRows)
    {
        var sql = maxRows is > 0
            ? ListEducationCopiesSql.Replace("SELECT", $"SELECT TOP ({maxRows.Value})", StringComparison.Ordinal)
            : ListEducationCopiesSql;

        var rows = new List<(Guid, Guid, string?)>();
        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            if (!Guid.TryParse(reader.GetString(0), out var copyOid))
                continue;
            if (!Guid.TryParse(reader.GetString(1), out var educationOid))
                continue;
            var personFullName = reader.IsDBNull(2) ? null : reader.GetString(2);
            rows.Add((copyOid, educationOid, personFullName));
        }

        return rows;
    }

    private static async Task<byte[]?> ReadCopyBlobAsync(
        SqlConnection connection,
        string blobColumn,
        Guid legacyCopyOid)
    {
        var sql = $"SELECT [{blobColumn.Replace("]", "]]")}] FROM dbo.PassportCopy WHERE Oid = @oid AND GCRecord IS NULL";
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@oid", legacyCopyOid);

        var value = await command.ExecuteScalarAsync();
        return value is DBNull or null ? null : (byte[])value;
    }

    private static Dictionary<Guid, Guid> LoadOptionalDocumentIdMap(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            return new Dictionary<Guid, Guid>();

        return Visa2014IdMapHelper.Load(path);
    }
}

internal sealed class EducationDocumentImportRow
{
    [JsonPropertyName("ID")]
    public Guid Id { get; set; }
}
