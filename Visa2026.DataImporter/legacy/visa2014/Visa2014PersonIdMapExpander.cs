using System.Data.Common;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Npgsql;
using Visa2026.Module.DatabaseUpdate;

namespace Visa2026.DataImporter.Legacy.Visa2014;

internal static class Visa2014PersonIdMapExpander
{
    public static async Task<int> ExpandAsync(
        string legacyConnectionString,
        IReadOnlyList<string> lookupTranslationPaths,
        string idMapPath,
        string targetConnectionString,
        bool verbose)
    {
        if (!File.Exists(idMapPath))
        {
            Console.WriteLine($"INF Person id-map not found yet (fresh import): {idMapPath}");
            return 0;
        }

        var idMap = JsonSerializer.Deserialize<Dictionary<string, string>>(
            await File.ReadAllTextAsync(idMapPath)) ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        var before = idMap.Count;
        var dedupeAliases = Visa2014PersonTransform.BuildDedupeLegacyAliases(
            legacyConnectionString,
            lookupTranslationPaths,
            maxRows: null,
            verbose);

        int addedFromDedupe = 0;
        foreach (var (mergedLegacyOid, canonicalLegacyOid) in dedupeAliases)
        {
            var canonicalKey = canonicalLegacyOid.ToString();
            if (!idMap.TryGetValue(canonicalKey, out var targetId))
                continue;

            var mergedKey = mergedLegacyOid.ToString();
            if (idMap.ContainsKey(mergedKey))
                continue;

            idMap[mergedKey] = targetId;
            addedFromDedupe++;
        }

        var batch = Visa2014PersonTransform.PrepareImportBatch(
            legacyConnectionString,
            lookupTranslationPaths,
            maxRows: null,
            verbose: false);

        var supplementBatch = Visa2014PersonTransform.PrepareSupplementPermitReferencedImportBatch(
            legacyConnectionString,
            lookupTranslationPaths,
            maxRows: null,
            verbose: false);

        var rowsToExpand = batch.ImportRows.Concat(supplementBatch.ImportRows).ToList();

        int addedFromPn = 0;
        if (DatabaseProviderDetector.IsPostgreSql(targetConnectionString))
        {
            await using var pg = new NpgsqlConnection(
                DatabaseProviderDetector.StripEfCoreProvider(targetConnectionString));
            await pg.OpenAsync();
            addedFromPn = await MatchPeopleByIdentityAsync(pg, rowsToExpand, idMap, postgres: true);
            await File.WriteAllTextAsync(
                idMapPath,
                JsonSerializer.Serialize(idMap, new JsonSerializerOptions { WriteIndented = true }));
            Console.WriteLine(
                $"INF Id-map expanded: {before} → {idMap.Count} (+{idMap.Count - before}; dedupe {addedFromDedupe}, PN collision {addedFromPn})");
            return 0;
        }

        await using (var conn = new SqlConnection(targetConnectionString))
        {
            await conn.OpenAsync();
            addedFromPn = await MatchPeopleByIdentityAsync(conn, rowsToExpand, idMap, postgres: false);
        }

        await File.WriteAllTextAsync(
            idMapPath,
            JsonSerializer.Serialize(idMap, new JsonSerializerOptions { WriteIndented = true }));

        Console.WriteLine($"INF Id-map expanded: {before} → {idMap.Count} (+{idMap.Count - before}; dedupe {addedFromDedupe}, PN collision {addedFromPn})");
        return 0;
    }

    private static async Task<int> MatchPeopleByIdentityAsync(
        DbConnection conn,
        IReadOnlyList<Dictionary<string, object?>> rowsToExpand,
        Dictionary<string, string> idMap,
        bool postgres)
    {
        int added = 0;
        foreach (var row in rowsToExpand)
        {
            var legacyKey = ((Guid)row["_legacyRowId"]!).ToString();
            if (idMap.ContainsKey(legacyKey))
                continue;

            var pn = row.GetValueOrDefault("PersonalNumber") as string;
            if (string.IsNullOrWhiteSpace(pn))
                continue;

            await using var cmd = conn.CreateCommand();
            if (Visa2014PersonTransform.IsSentinelPersonalNumber(pn))
            {
                if (row.GetValueOrDefault("FirstName") is not string firstName ||
                    row.GetValueOrDefault("LastName") is not string lastName ||
                    row.GetValueOrDefault("DateOfBirth") is not DateTime dateOfBirth)
                    continue;

                cmd.CommandText = postgres
                    ? """
                      SELECT "ID"::text
                      FROM "People"
                      WHERE COALESCE("GCRecord", 0) = 0
                        AND "PersonalNumber" = '0'
                        AND UPPER(BTRIM("FirstName")) = @fn
                        AND UPPER(BTRIM("LastName")) = @ln
                        AND "DateOfBirth"::date = @dob::date
                      ORDER BY "ID"
                      LIMIT 1
                      """
                    : """
                      SELECT TOP 1 CAST(ID AS varchar(36))
                      FROM People
                      WHERE (GCRecord IS NULL OR GCRecord = 0)
                        AND PersonalNumber = N'0'
                        AND UPPER(LTRIM(RTRIM(FirstName))) = @fn
                        AND UPPER(LTRIM(RTRIM(LastName))) = @ln
                        AND CAST(DateOfBirth AS date) = CAST(@dob AS date)
                      ORDER BY ID
                      """;
                cmd.Parameters.Add(CreateParameter(cmd, "@fn", firstName.Trim().ToUpperInvariant()));
                cmd.Parameters.Add(CreateParameter(cmd, "@ln", lastName.Trim().ToUpperInvariant()));
                cmd.Parameters.Add(CreateParameter(cmd, "@dob", dateOfBirth.Date));
            }
            else
            {
                cmd.CommandText = postgres
                    ? """
                      SELECT "ID"::text
                      FROM "People"
                      WHERE COALESCE("GCRecord", 0) = 0 AND "PersonalNumber" = @pn
                      ORDER BY "ID"
                      LIMIT 1
                      """
                    : """
                      SELECT TOP 1 CAST(ID AS varchar(36))
                      FROM People
                      WHERE (GCRecord IS NULL OR GCRecord = 0) AND PersonalNumber = @pn
                      ORDER BY ID
                      """;
                cmd.Parameters.Add(CreateParameter(cmd, "@pn", pn));
            }

            var existing = await cmd.ExecuteScalarAsync() as string;
            if (string.IsNullOrWhiteSpace(existing))
                continue;

            idMap[legacyKey] = existing;
            added++;
        }

        return added;
    }

    private static DbParameter CreateParameter(DbCommand cmd, string name, object value)
    {
        var parameter = cmd.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        return parameter;
    }
}
