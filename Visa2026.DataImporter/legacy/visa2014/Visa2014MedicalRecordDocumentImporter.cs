using System.Text.Json.Serialization;
using Microsoft.Data.SqlClient;
using Visa2026.DataImporter;

namespace Visa2026.DataImporter.Legacy.Visa2014;

internal sealed class Visa2014MedicalRecordDocumentImportResult
{
    public int PersonIdMapEntries { get; init; }
    public int LegacySpidLinkRows { get; init; }
    public int LegacyImportableRows { get; init; }
    public int Posted { get; init; }
    public int SkippedNoPersonMap { get; init; }
    public int SkippedOrphanCopy { get; init; }
    public int SkippedNoBlob { get; init; }
    public int SkippedNoAudit { get; init; }
    public int SkippedOversize { get; init; }
    public int SkippedAlreadyImported { get; init; }
    public int SkippedDuplicateBlob { get; init; }
    public int Failed { get; init; }
    public string? MedicalRecordIdMapPath { get; init; }
    public string? DocumentIdMapPath { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = [];
}

/// <summary>
/// Legacy spid kepilnama scans: <c>IPersonn_SpidKepilnama</c> → <c>Copy</c> → <c>FileData</c>.
/// Creates synthetic <c>MedicalRecord</c> parent per link, then posts <c>MedicalRecordDocument</c>.
/// </summary>
internal static class Visa2014MedicalRecordDocumentImporter
{
    private const int MaxDocumentBytes = 5 * 1024 * 1024;
    private const string DocumentNumber = "0";

    private const string ListImportableSql = """
        SELECT
            CAST(l.ICopy_Implicit_IPersonn_SpidKepilnama_List_Link AS varchar(36)) AS LegacyCopyOid,
            CAST(l.IPersonn_SpidKepilnama_Link AS varchar(36)) AS LegacyPersonOid,
            CAST(c.CopyOfDocument AS varchar(36)) AS FileDataOid,
            LTRIM(RTRIM(
                COALESCE(per.FirstName, N'') + N' ' + COALESCE(per.LastName, N'')
            )) AS PersonFullName,
            f.FileName AS LegacyFileName
        FROM dbo.IPersonn_SpidKepilnama l
        INNER JOIN dbo.Person per ON per.Oid = l.IPersonn_SpidKepilnama_Link AND per.GCRecord IS NULL
        INNER JOIN dbo.Copy c ON c.Oid = l.ICopy_Implicit_IPersonn_SpidKepilnama_List_Link AND c.GCRecord IS NULL
        INNER JOIN dbo.FileData f ON f.Oid = c.CopyOfDocument AND f.GCRecord IS NULL
        WHERE l.GCRecord IS NULL
        ORDER BY l.IPersonn_SpidKepilnama_Link, c.Oid
        """;

    private const string CountSpidLinksSql = """
        SELECT COUNT(*) FROM dbo.IPersonn_SpidKepilnama WHERE GCRecord IS NULL
        """;

    private const string CountOrphanLinksSql = """
        SELECT COUNT(*)
        FROM dbo.IPersonn_SpidKepilnama l
        LEFT JOIN dbo.Copy c
            ON c.Oid = l.ICopy_Implicit_IPersonn_SpidKepilnama_List_Link AND c.GCRecord IS NULL
        WHERE l.GCRecord IS NULL AND c.Oid IS NULL
        """;

    public static async Task<Visa2014MedicalRecordDocumentImportResult> RunAsync(
        ApiClient api,
        string legacyConnectionString,
        string personIdMapPath,
        string? medicalRecordIdMapOutputPath,
        string? documentIdMapOutputPath,
        int? maxRows,
        bool dryRun,
        bool verbose)
    {
        var personIdMap = Visa2014IdMapHelper.Load(personIdMapPath);
        var medicalRecordMap = LoadOptionalIdMap(medicalRecordIdMapOutputPath);
        var documentMap = LoadOptionalIdMap(documentIdMapOutputPath);
        int medicalMapAtStart = medicalRecordMap.Count;
        int documentMapAtStart = documentMap.Count;

        Guid validityDurationId = Guid.Empty;
        if (!dryRun)
            validityDurationId = await ResolveMonth3ValidityDurationIdAsync(api);

        await using var connection = new SqlConnection(legacyConnectionString);
        await connection.OpenAsync();

        var spidLinkRows = await ScalarCountAsync(connection, CountSpidLinksSql);
        var orphanLinks = await ScalarCountAsync(connection, CountOrphanLinksSql);
        var importRows = await ListImportableRowsAsync(connection, maxRows);

        var errors = new List<string>();
        var importedBlobKeys = new HashSet<string>(StringComparer.Ordinal);
        var copyIndexByPerson = new Dictionary<Guid, int>();
        int posted = 0;
        int failed = 0;
        int skippedNoPersonMap = 0;
        int skippedNoBlob = 0;
        int skippedNoAudit = 0;
        int skippedOversize = 0;
        int skippedAlreadyImported = 0;
        int skippedDuplicateBlob = 0;
        int postedSinceLastSave = 0;

        foreach (var row in importRows)
        {
            if (!personIdMap.TryGetValue(row.LegacyPersonOid, out var targetPersonId))
            {
                skippedNoPersonMap++;
                if (verbose)
                    Console.WriteLine($"  SKIP copy {row.LegacyCopyOid}: Person {row.LegacyPersonOid} not in id-map");
                continue;
            }

            byte[]? blob;
            try
            {
                blob = await ReadFileDataBlobAsync(connection, row.FileDataOid);
            }
            catch (Exception ex)
            {
                failed++;
                errors.Add($"{row.LegacyCopyOid}: SQL read failed — {ex.Message}");
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
                    Console.WriteLine($"  SKIP copy {row.LegacyCopyOid}: {blob.Length} bytes exceeds {MaxDocumentBytes} limit");
                continue;
            }

            DateTime? issueDate;
            try
            {
                issueDate = await Visa2014LegacyAuditIssueDateHelper.TryGetUploadIssueDateAsync(
                    connection, row.LegacyCopyOid, row.FileDataOid);
            }
            catch (Exception ex)
            {
                failed++;
                errors.Add($"{row.LegacyCopyOid}: audit lookup failed — {ex.Message}");
                continue;
            }

            if (!issueDate.HasValue)
            {
                skippedNoAudit++;
                if (verbose)
                    Console.WriteLine($"  SKIP copy {row.LegacyCopyOid}: no ObjectCreated audit for Copy/FileData");
                continue;
            }

            if (documentMap.ContainsKey(row.LegacyCopyOid))
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
                if (verbose)
                    Console.WriteLine($"  SKIP copy {row.LegacyCopyOid}: duplicate blob for Person {targetPersonId}");
                continue;
            }

            var fileName = ResolveFileName(row, blob, copyIndex);

            if (dryRun)
            {
                Console.WriteLine(
                    $"DRY RUN: POST MedicalRecord + MedicalRecordDocument ← copy {row.LegacyCopyOid} " +
                    $"person {targetPersonId} issue {issueDate:yyyy-MM-dd} ({blob.Length} bytes, {fileName})");
                posted++;
                continue;
            }

            try
            {
                var medicalRecordId = medicalRecordMap.GetValueOrDefault(row.LegacyCopyOid);
                if (medicalRecordId == Guid.Empty)
                {
                    var medicalPayload = new Dictionary<string, object?>
                    {
                        ["Person"] = new { ID = targetPersonId },
                        ["DocumentNumber"] = DocumentNumber,
                        ["IssueDate"] = DateTime.SpecifyKind(issueDate.Value, DateTimeKind.Utc),
                        ["ValidityDuration"] = new { ID = validityDurationId },
                    };

                    var medicalCreated = await api.CreateAsync<MedicalRecordImportRow>("MedicalRecord", medicalPayload);
                    if (medicalCreated == null)
                    {
                        failed++;
                        errors.Add($"{row.LegacyCopyOid}: MedicalRecord POST returned null");
                        continue;
                    }

                    medicalRecordId = medicalCreated.Id;
                    medicalRecordMap[row.LegacyCopyOid] = medicalRecordId;
                }

                var fileCreated = await api.CreateAsync<FileDataImportRow>("FileData", new Dictionary<string, object?>
                {
                    ["FileName"] = fileName,
                    ["Content"] = blob,
                });
                if (fileCreated == null)
                {
                    failed++;
                    errors.Add($"{row.LegacyCopyOid}: FileData POST returned null");
                    continue;
                }

                var docPayload = new Dictionary<string, object?>
                {
                    ["MedicalRecord"] = new { ID = medicalRecordId },
                    ["File"] = new { ID = fileCreated.Id },
                };

                var docCreated = await api.CreateAsync<MedicalRecordDocumentImportRow>("MedicalRecordDocument", docPayload);
                if (docCreated == null)
                {
                    failed++;
                    errors.Add($"{row.LegacyCopyOid}: MedicalRecordDocument POST returned null");
                    continue;
                }

                documentMap[row.LegacyCopyOid] = docCreated.Id;
                posted++;
                postedSinceLastSave++;
                if (posted % 50 == 0)
                    Console.WriteLine(
                        $"INF Progress: {posted} posted, {failed} failed, {skippedAlreadyImported} already imported, " +
                        $"{skippedNoPersonMap} no person map...");
                if (verbose)
                    Console.WriteLine($"  POST MedicalRecordDocument {docCreated.Id} ← copy {row.LegacyCopyOid}");

                if (postedSinceLastSave >= 50)
                {
                    await SaveMapsIfConfiguredAsync(medicalRecordIdMapOutputPath, medicalRecordMap);
                    await SaveMapsIfConfiguredAsync(documentIdMapOutputPath, documentMap);
                    postedSinceLastSave = 0;
                }
            }
            catch (Exception ex)
            {
                failed++;
                errors.Add($"{row.LegacyCopyOid}: {ex.Message}");
            }
        }

        string? medicalRecordIdMapPath = null;
        string? documentIdMapPath = null;
        if (!dryRun)
        {
            if (medicalRecordMap.Count > medicalMapAtStart && !string.IsNullOrWhiteSpace(medicalRecordIdMapOutputPath))
            {
                medicalRecordIdMapPath = Path.GetFullPath(medicalRecordIdMapOutputPath);
                await Visa2014IdMapHelper.SaveAsync(medicalRecordIdMapPath, medicalRecordMap);
            }

            if (documentMap.Count > documentMapAtStart && !string.IsNullOrWhiteSpace(documentIdMapOutputPath))
            {
                documentIdMapPath = Path.GetFullPath(documentIdMapOutputPath);
                await Visa2014IdMapHelper.SaveAsync(documentIdMapPath, documentMap);
            }
        }

        return new Visa2014MedicalRecordDocumentImportResult
        {
            PersonIdMapEntries = personIdMap.Count,
            LegacySpidLinkRows = spidLinkRows,
            LegacyImportableRows = importRows.Count,
            Posted = posted,
            SkippedNoPersonMap = skippedNoPersonMap,
            SkippedOrphanCopy = orphanLinks,
            SkippedNoBlob = skippedNoBlob,
            SkippedNoAudit = skippedNoAudit,
            SkippedOversize = skippedOversize,
            SkippedAlreadyImported = skippedAlreadyImported,
            SkippedDuplicateBlob = skippedDuplicateBlob,
            Failed = failed,
            MedicalRecordIdMapPath = medicalRecordIdMapPath,
            DocumentIdMapPath = documentIdMapPath,
            Errors = errors,
        };
    }

    private static async Task<Guid> ResolveMonth3ValidityDurationIdAsync(ApiClient api)
    {
        var durations = await api.GetAllAsync<ValidityDuration>("ValidityDuration");
        var month3 = durations.FirstOrDefault(d =>
            string.Equals(d.LocalizationKey, "Month3", StringComparison.OrdinalIgnoreCase)
            || d.NumberOfDays == 90);
        if (month3 != null)
            return month3.Id;

        var defaultDuration = durations.FirstOrDefault(d => d.IsDefault);
        if (defaultDuration != null)
            return defaultDuration.Id;

        throw new InvalidOperationException(
            "Could not resolve ValidityDuration Month3 (90 days) from OData — ensure lookup catalogs are seeded.");
    }

    private static string ResolveFileName(LegacySpidRow row, byte[] blob, int copyIndex)
    {
        var legacyName = row.LegacyFileName?.Trim();
        if (!string.IsNullOrWhiteSpace(legacyName))
            return legacyName;

        return Visa2014LegacyFileNameHelper.BuildMedicalCopyFileName(row.PersonFullName, blob, copyIndex);
    }

    private static async Task<List<LegacySpidRow>> ListImportableRowsAsync(SqlConnection connection, int? maxRows)
    {
        var sql = maxRows is > 0
            ? ListImportableSql.Replace("SELECT", $"SELECT TOP ({maxRows.Value})", StringComparison.Ordinal)
            : ListImportableSql;

        var rows = new List<LegacySpidRow>();
        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            if (!Guid.TryParse(reader.GetString(0), out var copyOid))
                continue;
            if (!Guid.TryParse(reader.GetString(1), out var personOid))
                continue;
            if (!Guid.TryParse(reader.GetString(2), out var fileDataOid))
                continue;

            var personFullName = reader.IsDBNull(3) ? null : reader.GetString(3);
            var legacyFileName = reader.IsDBNull(4) ? null : reader.GetString(4);
            rows.Add(new LegacySpidRow(copyOid, personOid, fileDataOid, personFullName, legacyFileName));
        }

        return rows;
    }

    private static async Task<byte[]?> ReadFileDataBlobAsync(SqlConnection connection, Guid fileDataOid)
    {
        const string sql = """
            SELECT Content
            FROM dbo.FileData
            WHERE Oid = @oid AND GCRecord IS NULL
            """;
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@oid", fileDataOid);

        var value = await command.ExecuteScalarAsync();
        return value is DBNull or null ? null : (byte[])value;
    }

    private static async Task<int> ScalarCountAsync(SqlConnection connection, string sql)
    {
        await using var command = new SqlCommand(sql, connection);
        var value = await command.ExecuteScalarAsync();
        return value is int i ? i : Convert.ToInt32(value);
    }

    private static Dictionary<Guid, Guid> LoadOptionalIdMap(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            return new Dictionary<Guid, Guid>();

        return Visa2014IdMapHelper.Load(path);
    }

    private static async Task SaveMapsIfConfiguredAsync(string? path, Dictionary<Guid, Guid> map)
    {
        if (!string.IsNullOrWhiteSpace(path))
            await Visa2014IdMapHelper.SaveAsync(path, map);
    }

    private sealed record LegacySpidRow(
        Guid LegacyCopyOid,
        Guid LegacyPersonOid,
        Guid FileDataOid,
        string? PersonFullName,
        string? LegacyFileName);
}

internal sealed class MedicalRecordImportRow
{
    [JsonPropertyName("ID")]
    public Guid Id { get; set; }
}

internal sealed class MedicalRecordDocumentImportRow
{
    [JsonPropertyName("ID")]
    public Guid Id { get; set; }
}
