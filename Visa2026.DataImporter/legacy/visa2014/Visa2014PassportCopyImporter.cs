using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Data.SqlClient;
using Visa2026.DataImporter;

namespace Visa2026.DataImporter.Legacy.Visa2014;

internal sealed class Visa2014PassportCopyImportResult
{
    public int PassportIdMapEntries { get; init; }
    public int LegacyCopyRows { get; init; }
    public int Processed { get; init; }
    public int Posted { get; init; }
    public int SkippedNoPassportMap { get; init; }
    public int SkippedNoBlob { get; init; }
    public int SkippedOversize { get; init; }
    public int SkippedAlreadyImported { get; init; }
    public int Failed { get; init; }
    public string? CopyIdMapPath { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = [];
}

internal static class Visa2014PassportCopyImporter
{
    private const int MaxDocumentBytes = 5 * 1024 * 1024;

    private const string ListCopiesSql = """
        SELECT
            CAST(pc.Oid AS varchar(36)) AS LegacyCopyOid,
            CAST(pc.Passport AS varchar(36)) AS LegacyPassportOid
        FROM dbo.PassportCopy pc
        INNER JOIN dbo.Passport p ON pc.Passport = p.Oid AND p.GCRecord IS NULL
        WHERE pc.GCRecord IS NULL
          AND pc.Passport IS NOT NULL
        ORDER BY pc.Passport, pc.Oid
        """;

    public static async Task<Visa2014PassportCopyImportResult> RunAsync(
        ApiClient api,
        string legacyConnectionString,
        string passportIdMapPath,
        string? copyIdMapOutputPath,
        int? maxRows,
        bool dryRun,
        bool verbose)
    {
        var passportIdMap = Visa2014IdMapHelper.Load(passportIdMapPath);
        var existingCopyMap = LoadOptionalCopyIdMap(copyIdMapOutputPath);

        await using var connection = new SqlConnection(legacyConnectionString);
        await connection.OpenAsync();
        var blobColumn = Visa2014LegacyBlobColumnResolver.GetVarbinaryColumnName(connection, "dbo.PassportCopy");

        var copyRows = await ListLegacyCopyRowsAsync(connection, maxRows);
        var errors = new List<string>();
        var newCopyMap = new Dictionary<Guid, Guid>(existingCopyMap);
        int posted = 0;
        int failed = 0;
        int skippedNoPassportMap = 0;
        int skippedNoBlob = 0;
        int skippedOversize = 0;
        int skippedAlreadyImported = 0;

        foreach (var (legacyCopyOid, legacyPassportOid) in copyRows)
        {
            if (existingCopyMap.ContainsKey(legacyCopyOid))
            {
                skippedAlreadyImported++;
                continue;
            }

            if (!passportIdMap.TryGetValue(legacyPassportOid, out var targetPassportId))
            {
                skippedNoPassportMap++;
                if (verbose)
                    Console.WriteLine($"  SKIP copy {legacyCopyOid}: Passport {legacyPassportOid} not in id-map");
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

            var fileName = Visa2014LegacyFileNameHelper.BuildFileName(legacyCopyOid, blob);

            if (dryRun)
            {
                Console.WriteLine(
                    $"DRY RUN: POST PassportDocument ← copy {legacyCopyOid} passport {targetPassportId} ({blob.Length} bytes, {fileName})");
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
                    ["Passport"] = new { ID = targetPassportId },
                    ["File"] = new { ID = fileCreated.Id },
                };

                var created = await api.CreateAsync<PassportDocumentImportRow>("PassportDocument", payload);
                if (created == null)
                {
                    failed++;
                    errors.Add($"{legacyCopyOid}: POST returned null");
                    continue;
                }

                newCopyMap[legacyCopyOid] = created.Id;
                posted++;
                if (verbose)
                    Console.WriteLine($"  POST PassportDocument {created.Id} ← copy {legacyCopyOid}");
            }
            catch (Exception ex)
            {
                failed++;
                errors.Add($"{legacyCopyOid}: {ex.Message}");
            }
        }

        string? copyIdMapPath = null;
        if (!dryRun && newCopyMap.Count > existingCopyMap.Count && !string.IsNullOrWhiteSpace(copyIdMapOutputPath))
        {
            copyIdMapPath = Path.GetFullPath(copyIdMapOutputPath);
            Directory.CreateDirectory(Path.GetDirectoryName(copyIdMapPath)!);
            var serializable = newCopyMap.ToDictionary(
                kvp => kvp.Key.ToString(),
                kvp => kvp.Value.ToString());
            await File.WriteAllTextAsync(
                copyIdMapPath,
                JsonSerializer.Serialize(serializable, new JsonSerializerOptions { WriteIndented = true }));
        }

        return new Visa2014PassportCopyImportResult
        {
            PassportIdMapEntries = passportIdMap.Count,
            LegacyCopyRows = copyRows.Count,
            Processed = copyRows.Count,
            Posted = posted,
            SkippedNoPassportMap = skippedNoPassportMap,
            SkippedNoBlob = skippedNoBlob,
            SkippedOversize = skippedOversize,
            SkippedAlreadyImported = skippedAlreadyImported,
            Failed = failed,
            CopyIdMapPath = copyIdMapPath,
            Errors = errors,
        };
    }

    private static async Task<List<(Guid LegacyCopyOid, Guid LegacyPassportOid)>> ListLegacyCopyRowsAsync(
        SqlConnection connection,
        int? maxRows)
    {
        var sql = maxRows is > 0
            ? ListCopiesSql.Replace(
                "SELECT",
                $"SELECT TOP ({maxRows.Value})",
                StringComparison.Ordinal)
            : ListCopiesSql;

        var rows = new List<(Guid, Guid)>();
        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            if (!Guid.TryParse(reader.GetString(0), out var copyOid))
                continue;
            if (!Guid.TryParse(reader.GetString(1), out var passportOid))
                continue;
            rows.Add((copyOid, passportOid));
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

    private static Dictionary<Guid, Guid> LoadOptionalCopyIdMap(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            return new Dictionary<Guid, Guid>();

        return Visa2014IdMapHelper.Load(path);
    }
}

internal sealed class PassportDocumentImportRow
{
    [JsonPropertyName("ID")]
    public Guid Id { get; set; }
}

internal sealed class FileDataImportRow
{
    [JsonPropertyName("ID")]
    public Guid Id { get; set; }
}
