using Microsoft.Data.SqlClient;
using Bo = Visa2026.Module.BusinessObjects;

namespace Visa2026.DataImporter.Legacy.Visa2014;

internal sealed class Visa2014FamilyProofDocumentImportResult
{
    public int PersonIdMapEntries { get; init; }
    public int LegacyRows { get; init; }
    public int PostedPersonDocument { get; init; }
    public int PostedFamilyRelationDocument { get; init; }
    public int SkippedNoPersonMap { get; init; }
    public int SkippedNoBlob { get; init; }
    public int SkippedOversize { get; init; }
    public int SkippedAlreadyImported { get; init; }
    public int SkippedDuplicateBlob { get; init; }
    public int Failed { get; init; }
    public string? DocumentIdMapPath { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = [];
}

internal static class Visa2014FamilyProofDocumentImporter
{
    private const int MaxDocumentBytes = 5 * 1024 * 1024;

    public static async Task<Visa2014FamilyProofDocumentImportResult> RunAsync(
        IVisa2014ImportTarget target,
        string legacyConnectionString,
        string personIdMapPath,
        string? documentIdMapOutputPath,
        int? maxRows,
        bool dryRun,
        bool verbose)
    {
        var personIdMap = Visa2014IdMapHelper.Load(personIdMapPath);
        var docMap = LoadOptionalDocumentIdMap(documentIdMapOutputPath);
        int mapEntriesAtStart = docMap.Count;

        await using var connection = new SqlConnection(legacyConnectionString);
        await connection.OpenAsync();

        var personLinkColumn = await Visa2014LegacyTableColumnResolver.FindColumnNameAsync(
            connection, "dbo.FamilyProofDocument", "Implicit_IPersonn_");
        if (string.IsNullOrWhiteSpace(personLinkColumn))
            throw new InvalidOperationException("FamilyProofDocument Person link column not found.");

        var legacyRows = await ListLegacyRowsAsync(connection, personLinkColumn, maxRows);
        var errors = new List<string>();
        var importedBlobKeys = new HashSet<string>(StringComparer.Ordinal);
        var copyIndexByPerson = new Dictionary<Guid, int>();
        int postedPersonDocument = 0;
        int postedFamilyRelationDocument = 0;
        int failed = 0;
        int skippedNoPersonMap = 0;
        int skippedNoBlob = 0;
        int skippedOversize = 0;
        int skippedAlreadyImported = 0;
        int skippedDuplicateBlob = 0;
        int postedSinceLastSave = 0;

        foreach (var row in legacyRows)
        {
            if (!personIdMap.TryGetValue(row.LegacyPersonOid, out var targetPersonId))
            {
                skippedNoPersonMap++;
                continue;
            }

            byte[]? blob;
            try
            {
                blob = await ReadInlineCopyBlobAsync(connection, row.LegacyDocumentOid);
            }
            catch (Exception ex)
            {
                failed++;
                errors.Add($"{row.LegacyDocumentOid}: SQL read failed — {ex.Message}");
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

            if (docMap.ContainsKey(row.LegacyDocumentOid))
            {
                skippedAlreadyImported++;
                Visa2014LegacyBlobDedupeHelper.RegisterExistingBlob(
                    importedBlobKeys, copyIndexByPerson, targetPersonId, blob);
                continue;
            }

            if (!Visa2014LegacyBlobDedupeHelper.TryRegisterDistinctBlob(
                    importedBlobKeys, copyIndexByPerson, targetPersonId, blob, out var copyIndex))
            {
                skippedDuplicateBlob++;
                continue;
            }

            var fileName = Visa2014LegacyFileNameHelper.BuildFamilyProofCopyFileName(
                row.PersonFullName, blob, copyIndex);
            var targetType = row.IsFamilyMemberPerson
                ? typeof(Bo.PersonFamilyRelationDocument)
                : typeof(Bo.PersonDocument);

            if (dryRun)
            {
                if (row.IsFamilyMemberPerson)
                    postedFamilyRelationDocument++;
                else
                    postedPersonDocument++;
                continue;
            }

            try
            {
                var payload = Visa2014DocumentImportPayload.WithNestedFile(
                    "Person", targetPersonId, fileName, blob);

                var createdId = await target.CreateAsync(targetType, payload);
                if (createdId == null)
                {
                    failed++;
                    errors.Add($"{row.LegacyDocumentOid}: {targetType.Name} create returned null");
                    continue;
                }

                await target.FlushAsync();
                docMap[row.LegacyDocumentOid] = createdId.Value;
                if (row.IsFamilyMemberPerson)
                    postedFamilyRelationDocument++;
                else
                    postedPersonDocument++;
                postedSinceLastSave++;

                if (postedSinceLastSave >= 100 && !string.IsNullOrWhiteSpace(documentIdMapOutputPath))
                {
                    await Visa2014IdMapHelper.SaveAsync(documentIdMapOutputPath, docMap);
                    postedSinceLastSave = 0;
                }
            }
            catch (Exception ex)
            {
                failed++;
                errors.Add($"{row.LegacyDocumentOid}: {ex.Message}");
            }
        }

        string? documentIdMapPath = null;
        if (!dryRun && docMap.Count > mapEntriesAtStart && !string.IsNullOrWhiteSpace(documentIdMapOutputPath))
        {
            documentIdMapPath = Path.GetFullPath(documentIdMapOutputPath);
            await Visa2014IdMapHelper.SaveAsync(documentIdMapPath, docMap);
        }

        return new Visa2014FamilyProofDocumentImportResult
        {
            PersonIdMapEntries = personIdMap.Count,
            LegacyRows = legacyRows.Count,
            PostedPersonDocument = postedPersonDocument,
            PostedFamilyRelationDocument = postedFamilyRelationDocument,
            SkippedNoPersonMap = skippedNoPersonMap,
            SkippedNoBlob = skippedNoBlob,
            SkippedOversize = skippedOversize,
            SkippedAlreadyImported = skippedAlreadyImported,
            SkippedDuplicateBlob = skippedDuplicateBlob,
            Failed = failed,
            DocumentIdMapPath = documentIdMapPath,
            Errors = errors,
        };
    }

    private sealed record LegacyFamilyProofRow(
        Guid LegacyDocumentOid,
        Guid LegacyPersonOid,
        string? PersonFullName,
        bool IsFamilyMemberPerson);

    private static async Task<List<LegacyFamilyProofRow>> ListLegacyRowsAsync(
        SqlConnection connection,
        string personLinkColumn,
        int? maxRows)
    {
        var personCol = Visa2014LegacyTableColumnResolver.Bracket(personLinkColumn);
        var sql = $"""
            SELECT
                CAST(fpd.Oid AS varchar(36)) AS LegacyDocumentOid,
                CAST(fpd.{personCol} AS varchar(36)) AS LegacyPersonOid,
                LEFT(LTRIM(RTRIM(
                    COALESCE(per.FirstName, N'') + N' ' + COALESCE(per.LastName, N'')
                )), 200) AS PersonFullName,
                CASE WHEN e.Oid IS NULL AND fm.Oid IS NOT NULL THEN 1 ELSE 0 END AS IsFamilyMemberPerson
            FROM dbo.FamilyProofDocument fpd
            INNER JOIN dbo.Person per ON per.Oid = fpd.{personCol} AND per.GCRecord IS NULL
            LEFT JOIN dbo.Employee e ON e.Oid = per.Oid
            LEFT JOIN dbo.FamilyMember fm ON fm.Oid = per.Oid
            WHERE fpd.GCRecord IS NULL
              AND fpd.{personCol} IS NOT NULL
              AND fpd.CopyOfDocument IS NOT NULL
              AND DATALENGTH(fpd.CopyOfDocument) > 0
            ORDER BY fpd.{personCol}, fpd.Oid
            """;

        if (maxRows is > 0)
            sql = sql.Replace("SELECT", $"SELECT TOP ({maxRows.Value})", StringComparison.Ordinal);

        var rows = new List<LegacyFamilyProofRow>();
        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            if (!Guid.TryParse(reader.GetString(0), out var documentOid))
                continue;
            if (!Guid.TryParse(reader.GetString(1), out var personOid))
                continue;
            var personFullName = reader.IsDBNull(2) ? null : reader.GetString(2);
            var isFamilyMember = !reader.IsDBNull(3) && reader.GetInt32(3) == 1;
            rows.Add(new LegacyFamilyProofRow(documentOid, personOid, personFullName, isFamilyMember));
        }

        return rows;
    }

    private static async Task<byte[]?> ReadInlineCopyBlobAsync(SqlConnection connection, Guid documentOid)
    {
        const string sql = """
            SELECT CopyOfDocument FROM dbo.FamilyProofDocument WHERE Oid = @oid AND GCRecord IS NULL
            """;
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@oid", documentOid);
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
