using System.Data;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Data.SqlClient;
using Bo = Visa2026.Module.BusinessObjects;

namespace Visa2026.DataImporter.Legacy.Visa2014;

internal sealed class Visa2014VisaDocumentImportResult
{
    public int VisaIdMapEntries { get; init; }
    public int LegacyRowsWithBlob { get; init; }
    public int Posted { get; init; }
    public int SkippedNoVisaMap { get; init; }
    public int SkippedNoBlob { get; init; }
    public int SkippedOversize { get; init; }
    public int SkippedAlreadyImported { get; init; }
    public int Failed { get; init; }
    public string? DocumentIdMapPath { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = [];
}

internal static class Visa2014VisaDocumentImporter
{
    private const int MaxDocumentBytes = 5 * 1024 * 1024;

    private const string ListVisasWithBlobSql = """
        SELECT
            CAST(v.Oid AS varchar(36)) AS LegacyVisaOid,
            v.VisaNumber
        FROM dbo.Visa v
        INNER JOIN dbo.Passport p ON v.Passport = p.Oid AND p.GCRecord IS NULL
        WHERE v.GCRecord IS NULL
        ORDER BY v.Oid
        """;

    public static async Task<Visa2014VisaDocumentImportResult> RunAsync(
        IVisa2014ImportTarget target,
        string legacyConnectionString,
        string visaIdMapPath,
        string? documentIdMapOutputPath,
        int? maxRows,
        bool dryRun,
        bool verbose)
    {
        var visaIdMap = Visa2014IdMapHelper.Load(visaIdMapPath);
        var existingDocMap = LoadOptionalDocumentIdMap(documentIdMapOutputPath);

        await using var connection = new SqlConnection(legacyConnectionString);
        await connection.OpenAsync();
        var blobColumn = Visa2014LegacyBlobColumnResolver.GetVarbinaryColumnName(connection, "dbo.Visa");

        var legacyVisaRows = await ListLegacyVisaRowsAsync(connection, maxRows);
        var errors = new List<string>();
        var newDocMap = new Dictionary<Guid, Guid>(existingDocMap);
        int posted = 0;
        int failed = 0;
        int skippedNoVisaMap = 0;
        int skippedNoBlob = 0;
        int skippedOversize = 0;
        int skippedAlreadyImported = 0;
        int rowsWithBlob = 0;

        foreach (var (legacyVisaOid, visaNumber) in legacyVisaRows)
        {
            if (existingDocMap.ContainsKey(legacyVisaOid))
            {
                skippedAlreadyImported++;
                continue;
            }

            if (!visaIdMap.TryGetValue(legacyVisaOid, out var targetVisaId))
            {
                skippedNoVisaMap++;
                if (verbose)
                    Console.WriteLine($"  SKIP visa {legacyVisaOid}: not in Visa id-map");
                continue;
            }

            byte[]? blob;
            try
            {
                blob = await ReadVisaBlobAsync(connection, legacyConnectionString, blobColumn, legacyVisaOid);
            }
            catch (Exception ex)
            {
                failed++;
                errors.Add($"{legacyVisaOid}: SQL read failed — {ex.Message}");
                continue;
            }

            if (blob == null || blob.Length == 0)
            {
                skippedNoBlob++;
                continue;
            }

            rowsWithBlob++;

            if (blob.Length > MaxDocumentBytes)
            {
                skippedOversize++;
                if (verbose)
                    Console.WriteLine($"  SKIP visa {legacyVisaOid}: {blob.Length} bytes exceeds {MaxDocumentBytes} limit");
                continue;
            }

            var fileName = Visa2014LegacyFileNameHelper.BuildVisaCopyFileName(visaNumber, blob);

            if (dryRun)
            {
                Console.WriteLine(
                    $"DRY RUN: POST VisaDocument ← visa {legacyVisaOid} → {targetVisaId} ({blob.Length} bytes, {fileName})");
                posted++;
                continue;
            }

            try
            {
                var payload = Visa2014DocumentImportPayload.WithNestedFile(
                    "Visa", targetVisaId, fileName, blob);

                var createdId = await target.CreateAsync(typeof(Bo.VisaDocument), payload);
                if (createdId == null)
                {
                    failed++;
                    errors.Add($"{legacyVisaOid}: VisaDocument create returned null");
                    continue;
                }

                await target.FlushAsync();
                newDocMap[legacyVisaOid] = createdId.Value;
                posted++;
                if (posted % 100 == 0)
                    Console.WriteLine($"INF Progress: {posted} posted, {failed} failed, {skippedNoVisaMap} no visa map...");
                if (verbose)
                    Console.WriteLine($"  POST VisaDocument {createdId} ← visa {legacyVisaOid}");
            }
            catch (Exception ex)
            {
                failed++;
                errors.Add($"{legacyVisaOid}: {ex.Message}");
            }
        }

        string? documentIdMapPath = null;
        if (!dryRun && newDocMap.Count > existingDocMap.Count && !string.IsNullOrWhiteSpace(documentIdMapOutputPath))
        {
            documentIdMapPath = Path.GetFullPath(documentIdMapOutputPath);
            Directory.CreateDirectory(Path.GetDirectoryName(documentIdMapPath)!);
            var serializable = newDocMap.ToDictionary(
                kvp => kvp.Key.ToString(),
                kvp => kvp.Value.ToString());
            await File.WriteAllTextAsync(
                documentIdMapPath,
                JsonSerializer.Serialize(serializable, new JsonSerializerOptions { WriteIndented = true }));
        }

        return new Visa2014VisaDocumentImportResult
        {
            VisaIdMapEntries = visaIdMap.Count,
            LegacyRowsWithBlob = rowsWithBlob,
            Posted = posted,
            SkippedNoVisaMap = skippedNoVisaMap,
            SkippedNoBlob = skippedNoBlob,
            SkippedOversize = skippedOversize,
            SkippedAlreadyImported = skippedAlreadyImported,
            Failed = failed,
            DocumentIdMapPath = documentIdMapPath,
            Errors = errors,
        };
    }

    private static async Task<List<(Guid LegacyVisaOid, string? VisaNumber)>> ListLegacyVisaRowsAsync(
        SqlConnection connection,
        int? maxRows)
    {
        var sql = maxRows is > 0
            ? ListVisasWithBlobSql.Replace("SELECT", $"SELECT TOP ({maxRows.Value})", StringComparison.Ordinal)
            : ListVisasWithBlobSql;

        var result = new List<(Guid, string?)>();
        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            if (Guid.TryParse(reader.GetString(0), out var oid))
            {
                var visaNumber = reader.IsDBNull(1) ? null : reader.GetString(1);
                result.Add((oid, visaNumber));
            }
        }

        return result;
    }

    private static async Task<byte[]?> ReadVisaBlobAsync(
        SqlConnection connection,
        string legacyConnectionString,
        string blobColumn,
        Guid legacyVisaOid)
    {
        try
        {
            await EnsureOpenAsync(connection);
            return await ReadVisaBlobOnceAsync(connection, blobColumn, legacyVisaOid);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"WRN VisaDocument blob {legacyVisaOid}: {ex.Message} — retrying on a new connection");
            await using var retry = new SqlConnection(legacyConnectionString);
            await retry.OpenAsync();
            var blob = await ReadVisaBlobOnceAsync(retry, blobColumn, legacyVisaOid);
            try { connection.Close(); } catch { /* original connection is broken */ }
            return blob;
        }
    }

    private static async Task<byte[]?> ReadVisaBlobOnceAsync(
        SqlConnection connection,
        string blobColumn,
        Guid legacyVisaOid)
    {
        var sql = $"SELECT [{blobColumn.Replace("]", "]]")}] FROM dbo.Visa WHERE Oid = @oid";
        await using var command = new SqlCommand(sql, connection) { CommandTimeout = 180 };
        command.Parameters.AddWithValue("@oid", legacyVisaOid);
        var scalar = await command.ExecuteScalarAsync();
        return scalar is byte[] bytes && bytes.Length > 0 ? bytes : null;
    }

    private static async Task EnsureOpenAsync(SqlConnection connection)
    {
        if (connection.State == ConnectionState.Open)
            return;
        if (connection.State != ConnectionState.Closed)
        {
            try { connection.Close(); } catch { /* ignore broken close */ }
        }

        await connection.OpenAsync();
    }

    private static Dictionary<Guid, Guid> LoadOptionalDocumentIdMap(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            return new Dictionary<Guid, Guid>();

        return Visa2014IdMapHelper.Load(path);
    }
}

internal sealed class VisaDocumentImportRow
{
    [JsonPropertyName("ID")]
    public Guid Id { get; set; }
}
