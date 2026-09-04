using System.Data;
using Microsoft.Data.SqlClient;
using Bo = Visa2026.Module.BusinessObjects;

namespace Visa2026.DataImporter.Legacy.Visa2014;

internal sealed class Visa2014PassportCopyLinkedDocumentImportResult
{
    public int ParentIdMapEntries { get; init; }
    public int LegacyCopyRows { get; init; }
    public int Posted { get; init; }
    public int SkippedNoParentMap { get; init; }
    public int SkippedNoBlob { get; init; }
    public int SkippedOversize { get; init; }
    public int SkippedAlreadyImported { get; init; }
    public int SkippedDuplicateBlob { get; init; }
    public int Failed { get; init; }
    public string? DocumentIdMapPath { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = [];
}

internal sealed class Visa2014PassportCopyLinkSpec
{
    public required string LegacyParentFkColumnPrefix { get; init; }
    public required string LegacyParentTable { get; init; }
    public required string LegacyParentNumberColumn { get; init; }
    public required Type TargetDocumentType { get; init; }
    public required string TargetParentNavigationProperty { get; init; }
    public required Func<string?, byte[], int, string> BuildFileName { get; init; }
}

internal static class Visa2014PassportCopyLinkedDocumentImporter
{
    private const int MaxDocumentBytes = 5 * 1024 * 1024;

    public static async Task<Visa2014PassportCopyLinkedDocumentImportResult> RunAsync(
        IVisa2014ImportTarget target,
        string legacyConnectionString,
        string parentIdMapPath,
        string? documentIdMapOutputPath,
        Visa2014PassportCopyLinkSpec spec,
        int? maxRows,
        bool dryRun,
        bool verbose)
    {
        var parentIdMap = Visa2014IdMapHelper.Load(parentIdMapPath);
        var docMap = LoadOptionalDocumentIdMap(documentIdMapOutputPath);
        int mapEntriesAtStart = docMap.Count;

        await using var connection = new SqlConnection(legacyConnectionString);
        await connection.OpenAsync();

        var blobColumn = Visa2014LegacyBlobColumnResolver.GetVarbinaryColumnName(connection, "dbo.PassportCopy");
        var parentFkColumn = await Visa2014LegacyTableColumnResolver.FindColumnNameAsync(
            connection, "dbo.PassportCopy", spec.LegacyParentFkColumnPrefix);
        if (string.IsNullOrWhiteSpace(parentFkColumn))
            throw new InvalidOperationException(
                $"No PassportCopy column matching prefix '{spec.LegacyParentFkColumnPrefix}'.");

        var copyRows = await ListLegacyCopyRowsAsync(connection, parentFkColumn, spec, maxRows);
        var errors = new List<string>();
        var importedBlobKeys = new HashSet<string>(StringComparer.Ordinal);
        var copyIndexByParent = new Dictionary<Guid, int>();
        int posted = 0;
        int failed = 0;
        int skippedNoParentMap = 0;
        int skippedNoBlob = 0;
        int skippedOversize = 0;
        int skippedAlreadyImported = 0;
        int skippedDuplicateBlob = 0;
        int postedSinceLastSave = 0;

        foreach (var (legacyCopyOid, legacyParentOid, parentNumber) in copyRows)
        {
            if (!parentIdMap.TryGetValue(legacyParentOid, out var targetParentId))
            {
                skippedNoParentMap++;
                if (verbose)
                    Console.WriteLine($"  SKIP copy {legacyCopyOid}: parent {legacyParentOid} not in id-map");
                continue;
            }

            if (docMap.ContainsKey(legacyCopyOid))
            {
                skippedAlreadyImported++;
                continue;
            }

            byte[]? blob;
            try
            {
                blob = await ReadCopyBlobAsync(connection, legacyConnectionString, blobColumn, legacyCopyOid);
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
                continue;
            }

            if (!Visa2014LegacyBlobDedupeHelper.TryRegisterDistinctBlob(
                    importedBlobKeys, copyIndexByParent, targetParentId, blob, out var copyIndex))
            {
                skippedDuplicateBlob++;
                continue;
            }

            var fileName = spec.BuildFileName(parentNumber, blob, copyIndex);

            if (dryRun)
            {
                posted++;
                continue;
            }

            try
            {
                var payload = Visa2014DocumentImportPayload.WithNestedFile(
                    spec.TargetParentNavigationProperty, targetParentId, fileName, blob);

                var createdId = await target.CreateAsync(spec.TargetDocumentType, payload);
                if (createdId == null)
                {
                    failed++;
                    errors.Add($"{legacyCopyOid}: {spec.TargetDocumentType.Name} create returned null");
                    continue;
                }

                await target.FlushAsync();
                docMap[legacyCopyOid] = createdId.Value;
                posted++;
                postedSinceLastSave++;
                if (posted % 100 == 0)
                    Console.WriteLine($"INF Progress: {posted} posted, {failed} failed, {skippedAlreadyImported} already imported, {skippedNoParentMap} no parent map...");

                if (postedSinceLastSave >= 100 && !string.IsNullOrWhiteSpace(documentIdMapOutputPath))
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

        return new Visa2014PassportCopyLinkedDocumentImportResult
        {
            ParentIdMapEntries = parentIdMap.Count,
            LegacyCopyRows = copyRows.Count,
            Posted = posted,
            SkippedNoParentMap = skippedNoParentMap,
            SkippedNoBlob = skippedNoBlob,
            SkippedOversize = skippedOversize,
            SkippedAlreadyImported = skippedAlreadyImported,
            SkippedDuplicateBlob = skippedDuplicateBlob,
            Failed = failed,
            DocumentIdMapPath = documentIdMapPath,
            Errors = errors,
        };
    }

    private static async Task<List<(Guid LegacyCopyOid, Guid LegacyParentOid, string? ParentNumber)>> ListLegacyCopyRowsAsync(
        SqlConnection connection,
        string parentFkColumn,
        Visa2014PassportCopyLinkSpec spec,
        int? maxRows)
    {
        var fkBracket = Visa2014LegacyTableColumnResolver.Bracket(parentFkColumn);
        var parentNumberBracket = Visa2014LegacyTableColumnResolver.Bracket(spec.LegacyParentNumberColumn);
        var sql = $"""
            SELECT
                CAST(pc.Oid AS varchar(36)) AS LegacyCopyOid,
                CAST(pc.{fkBracket} AS varchar(36)) AS LegacyParentOid,
                parent.{parentNumberBracket} AS ParentNumber
            FROM dbo.PassportCopy pc
            INNER JOIN {spec.LegacyParentTable} parent
                ON pc.{fkBracket} = parent.Oid AND parent.GCRecord IS NULL
            WHERE pc.GCRecord IS NULL
              AND pc.{fkBracket} IS NOT NULL
            ORDER BY pc.{fkBracket}, pc.Oid
            """;

        if (maxRows is > 0)
            sql = sql.Replace("SELECT", $"SELECT TOP ({maxRows.Value})", StringComparison.Ordinal);

        var rows = new List<(Guid, Guid, string?)>();
        await using var command = new SqlCommand(sql, connection) { CommandTimeout = 180 };
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            if (!Guid.TryParse(reader.GetString(0), out var copyOid))
                continue;
            if (!Guid.TryParse(reader.GetString(1), out var parentOid))
                continue;
            var parentNumber = reader.IsDBNull(2) ? null : reader.GetString(2);
            rows.Add((copyOid, parentOid, parentNumber));
        }

        return rows;
    }

    private static async Task<byte[]?> ReadCopyBlobAsync(
        SqlConnection connection,
        string legacyConnectionString,
        string blobColumn,
        Guid legacyCopyOid)
    {
        try
        {
            await EnsureOpenAsync(connection);
            return await ReadCopyBlobOnceAsync(connection, blobColumn, legacyCopyOid);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"WRN PassportCopy-linked blob {legacyCopyOid}: {ex.Message} — retrying on a new connection");
            await using var retry = new SqlConnection(legacyConnectionString);
            await retry.OpenAsync();
            var blob = await ReadCopyBlobOnceAsync(retry, blobColumn, legacyCopyOid);
            try { connection.Close(); } catch { /* original connection is broken */ }
            return blob;
        }
    }

    private static async Task<byte[]?> ReadCopyBlobOnceAsync(
        SqlConnection connection,
        string blobColumn,
        Guid legacyCopyOid)
    {
        var blobBracket = Visa2014LegacyTableColumnResolver.Bracket(blobColumn);
        var sql = $"SELECT {blobBracket} FROM dbo.PassportCopy WHERE Oid = @oid AND GCRecord IS NULL";
        await using var command = new SqlCommand(sql, connection) { CommandTimeout = 180 };
        command.Parameters.AddWithValue("@oid", legacyCopyOid);
        var value = await command.ExecuteScalarAsync();
        return value is DBNull or null ? null : (byte[])value;
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
