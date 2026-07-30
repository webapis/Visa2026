using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Data.SqlClient;
using Bo = Visa2026.Module.BusinessObjects;

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
    public int SkippedDuplicateBlob { get; init; }
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
            CAST(pc.Passport AS varchar(36)) AS LegacyPassportOid,
            p.PassportNumber
        FROM dbo.PassportCopy pc
        INNER JOIN dbo.Passport p ON pc.Passport = p.Oid AND p.GCRecord IS NULL
        WHERE pc.GCRecord IS NULL
          AND pc.Passport IS NOT NULL
        ORDER BY pc.Passport, pc.Oid
        """;

    public static async Task<Visa2014PassportCopyImportResult> RunAsync(
        IVisa2014ImportTarget target,
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
        int skippedDuplicateBlob = 0;
        var importedBlobKeys = new HashSet<string>(StringComparer.Ordinal);
        var copyIndexByPassport = new Dictionary<Guid, int>();

        foreach (var (legacyCopyOid, legacyPassportOid, passportNumber) in copyRows)
        {
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

            if (existingCopyMap.ContainsKey(legacyCopyOid))
            {
                skippedAlreadyImported++;
                Visa2014LegacyBlobDedupeHelper.RegisterExistingBlob(
                    importedBlobKeys, copyIndexByPassport, targetPassportId, blob);
                continue;
            }

            if (!Visa2014LegacyBlobDedupeHelper.TryRegisterDistinctBlob(
                    importedBlobKeys, copyIndexByPassport, targetPassportId, blob, out var copyIndex))
            {
                skippedDuplicateBlob++;
                if (verbose)
                    Console.WriteLine($"  SKIP copy {legacyCopyOid}: duplicate blob for Passport {targetPassportId} ({blob.Length} bytes)");
                continue;
            }

            var fileName = Visa2014LegacyFileNameHelper.BuildPassportCopyFileName(passportNumber, blob, copyIndex);

            if (dryRun)
            {
                Console.WriteLine(
                    $"DRY RUN: POST PassportDocument ← copy {legacyCopyOid} passport {targetPassportId} ({blob.Length} bytes, {fileName})");
                posted++;
                continue;
            }

            try
            {
                var payload = Visa2014DocumentImportPayload.WithNestedFile(
                    "Passport", targetPassportId, fileName, blob);

                var createdId = await target.CreateAsync(typeof(Bo.PassportDocument), payload);
                if (createdId == null)
                {
                    failed++;
                    errors.Add($"{legacyCopyOid}: PassportDocument create returned null");
                    continue;
                }

                await target.FlushAsync();
                newCopyMap[legacyCopyOid] = createdId.Value;
                posted++;
                if (posted % 100 == 0)
                    Console.WriteLine(
                        $"INF Progress: {posted} posted, {failed} failed, {skippedNoPassportMap} no passport map...");
                if (verbose)
                    Console.WriteLine($"  POST PassportDocument {createdId} ← copy {legacyCopyOid}");
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
            SkippedDuplicateBlob = skippedDuplicateBlob,
            Failed = failed,
            CopyIdMapPath = copyIdMapPath,
            Errors = errors,
        };
    }

    private static async Task<List<(Guid LegacyCopyOid, Guid LegacyPassportOid, string? PassportNumber)>> ListLegacyCopyRowsAsync(
        SqlConnection connection,
        int? maxRows)
    {
        var sql = maxRows is > 0
            ? ListCopiesSql.Replace(
                "SELECT",
                $"SELECT TOP ({maxRows.Value})",
                StringComparison.Ordinal)
            : ListCopiesSql;

        var rows = new List<(Guid, Guid, string?)>();
        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            if (!Guid.TryParse(reader.GetString(0), out var copyOid))
                continue;
            if (!Guid.TryParse(reader.GetString(1), out var passportOid))
                continue;
            var passportNumber = reader.IsDBNull(2) ? null : reader.GetString(2);
            rows.Add((copyOid, passportOid, passportNumber));
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
