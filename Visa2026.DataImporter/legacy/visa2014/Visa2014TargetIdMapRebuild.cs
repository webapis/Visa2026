using System.Text.Json;
using Microsoft.Data.SqlClient;

namespace Visa2026.DataImporter.Legacy.Visa2014;

/// <summary>
/// Used when id-map files were lost but the target DB still has imported data.
/// </summary>
internal static class Visa2014TargetIdMapRebuild
{
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
        try { source = Visa2014LegacySource.Resolve(dataImporterRoot, solutionRoot, args); }
        catch (Exception ex) { Console.Error.WriteLine($"ERR {ex.Message}"); return 1; }

        var targetConnection = GetTargetConnection(args);
        var mapDir = Path.Combine(Visa2014ContentRoot.LegacyRoot(dataImporterRoot), source.IdMapDirectory.TrimStart('/', '\\'));
        Directory.CreateDirectory(mapDir);

        var entities = ParseEntities(args);
        if (entities.Count == 0)
            entities = ["Person", "Application", "Passport", "Visa", "Education", "EmployeePositionHistory", "EmployeeSalary", "AddressOfResidence"];

        Console.WriteLine("=== VISA2014 id-map rebuild from target");
        Console.WriteLine($"INF Legacy source: {source.Id}");
        Console.WriteLine($"INF Map dir: {mapDir}");
        Console.WriteLine($"INF Entities: {string.Join(", ", entities)}");

        foreach (var entity in entities)
        {
            var code = await RebuildEntityAsync(
                entity,
                source,
                dataImporterRoot,
                mapDir,
                targetConnection,
                verbose);
            if (code != 0)
                return code;
        }

        return 0;
    }

    private static async Task<int> RebuildEntityAsync(
        string entity,
        Visa2014LegacySourceProfile source,
        string dataImporterRoot,
        string mapDir,
        string targetConnection,
        bool verbose)
    {
        var mapPath = Path.Combine(mapDir, $"{entity}.json");

        if (string.Equals(entity, "Person", StringComparison.OrdinalIgnoreCase))
        {
            if (!File.Exists(mapPath))
                await File.WriteAllTextAsync(mapPath, "{}");
            return await Visa2014PersonIdMapExpander.ExpandAsync(
                source.ConnectionString,
                source.LookupTranslationPaths,
                mapPath,
                targetConnection,
                verbose);
        }

        var map = new Dictionary<Guid, Guid>();
        int matched = 0;
        int skipped = 0;

        await using var conn = new SqlConnection(targetConnection);
        await conn.OpenAsync();

        if (string.Equals(entity, "Application", StringComparison.OrdinalIgnoreCase))
        {
            var batch = Visa2014ApplicationTransform.PrepareImportBatch(
                source.ConnectionString, source.LookupTranslationPaths, maxRows: null, verbose: false);
            foreach (var row in batch.ImportRows)
            {
                if (row.GetValueOrDefault("_importAction") as string == "skip")
                {
                    skipped++;
                    continue;
                }

                var legacyOid = (Guid)row["_legacyRowId"]!;
                var fullNumber = row.GetValueOrDefault("FullApplicationNumber") as string;
                if (string.IsNullOrWhiteSpace(fullNumber))
                {
                    skipped++;
                    continue;
                }

                var targetId = await ScalarGuidAsync(conn,
                    """
                    SELECT TOP 1 CAST(ID AS varchar(36))
                    FROM Applications
                    WHERE (GCRecord IS NULL OR GCRecord = 0)
                      AND IsManualEntry = 1
                      AND FullApplicationNumber = @key
                    ORDER BY ID
                    """,
                    ("@key", fullNumber.Trim()));
                if (!targetId.HasValue)
                {
                    skipped++;
                    continue;
                }

                map[legacyOid] = targetId.Value;
                matched++;
            }
        }
        else if (string.Equals(entity, "Passport", StringComparison.OrdinalIgnoreCase))
        {
            var personMap = LoadMap(Path.Combine(mapDir, "Person.json"));
            var batch = Visa2014PassportTransform.PrepareImportBatch(
                source.ConnectionString, source.LookupTranslationPaths, maxRows: null, verbose: false);
            foreach (var row in batch.ImportRows)
            {
                if (row.GetValueOrDefault("_importAction") as string == "skip")
                {
                    skipped++;
                    continue;
                }

                var legacyOid = (Guid)row["_legacyRowId"]!;
                var passportNumber = row.GetValueOrDefault("PassportNumber") as string;
                if (!TryParseLegacyGuid(row, "Person", out var legacyPersonOid) ||
                    string.IsNullOrWhiteSpace(passportNumber) ||
                    !personMap.TryGetValue(legacyPersonOid, out var personId))
                {
                    skipped++;
                    continue;
                }

                var targetId = await ScalarGuidAsync(conn,
                    """
                    SELECT TOP 1 CAST(ID AS varchar(36))
                    FROM Passports
                    WHERE (GCRecord IS NULL OR GCRecord = 0)
                      AND PersonID = @personId
                      AND PassportNumber = @passportNumber
                    ORDER BY ID
                    """,
                    ("@personId", personId), ("@passportNumber", passportNumber.Trim()));
                if (!targetId.HasValue)
                {
                    skipped++;
                    continue;
                }

                map[legacyOid] = targetId.Value;
                matched++;
            }
        }
        else if (string.Equals(entity, "Visa", StringComparison.OrdinalIgnoreCase))
        {
            var passportMap = LoadMap(Path.Combine(mapDir, "Passport.json"));
            var batch = Visa2014VisaTransform.PrepareImportBatch(
                source.ConnectionString, source.LookupTranslationPaths, maxRows: null, verbose: false);
            foreach (var row in batch.ImportRows)
            {
                if (row.GetValueOrDefault("_importAction") as string == "skip")
                {
                    skipped++;
                    continue;
                }

                var legacyOid = (Guid)row["_legacyRowId"]!;
                var visaNumber = row.GetValueOrDefault("VisaNumber") as string;
                if (!TryParseLegacyGuid(row, "Passport", out var legacyPassportOid) ||
                    string.IsNullOrWhiteSpace(visaNumber) ||
                    !passportMap.TryGetValue(legacyPassportOid, out var passportId))
                {
                    skipped++;
                    continue;
                }

                var targetId = await ScalarGuidAsync(conn,
                    """
                    SELECT TOP 1 CAST(ID AS varchar(36))
                    FROM Visas
                    WHERE (GCRecord IS NULL OR GCRecord = 0)
                      AND PassportID = @passportId
                      AND VisaNumber = @visaNumber
                    ORDER BY ID
                    """,
                    ("@passportId", passportId), ("@visaNumber", visaNumber.Trim()));
                if (!targetId.HasValue)
                {
                    skipped++;
                    continue;
                }

                map[legacyOid] = targetId.Value;
                matched++;
            }
        }
        else if (string.Equals(entity, "Education", StringComparison.OrdinalIgnoreCase))
        {
            var personMap = LoadMap(Path.Combine(mapDir, "Person.json"));
            var batch = Visa2014EducationTransform.PrepareImportBatch(
                source.ConnectionString, source.LookupTranslationPaths, maxRows: null, verbose: false);
            foreach (var row in batch.ImportRows)
            {
                if (row.GetValueOrDefault("_importAction") as string == "skip")
                {
                    skipped++;
                    continue;
                }

                var legacyOid = (Guid)row["_legacyRowId"]!;
                var graduationYear = row.GetValueOrDefault("GraduationYear") as string;
                if (!TryParseLegacyGuid(row, "Person", out var legacyPersonOid) ||
                    !personMap.TryGetValue(legacyPersonOid, out var personId))
                {
                    skipped++;
                    continue;
                }

                var targetId = await ScalarGuidAsync(conn,
                    """
                    SELECT TOP 1 CAST(ID AS varchar(36))
                    FROM Educations
                    WHERE (GCRecord IS NULL OR GCRecord = 0)
                      AND PersonID = @personId
                      AND (GraduationYear = @gradYear OR (@gradYear IS NULL AND GraduationYear IS NULL))
                    ORDER BY ID DESC
                    """,
                    ("@personId", personId),
                    ("@gradYear", string.IsNullOrWhiteSpace(graduationYear) ? DBNull.Value : graduationYear.Trim()));
                if (!targetId.HasValue)
                {
                    skipped++;
                    continue;
                }

                map[legacyOid] = targetId.Value;
                matched++;
            }
        }
        else if (string.Equals(entity, "EmployeePositionHistory", StringComparison.OrdinalIgnoreCase))
        {
            var personMap = LoadMap(Path.Combine(mapDir, "Person.json"));
            var batch = Visa2014EmployeePositionHistoryTransform.PrepareImportBatch(
                source.ConnectionString, source.LookupTranslationPaths, maxRows: null, verbose: false);
            foreach (var row in batch.ImportRows)
            {
                if (row.GetValueOrDefault("_importAction") as string == "skip")
                {
                    skipped++;
                    continue;
                }

                var legacyOid = (Guid)row["_legacyRowId"]!;
                var startDateText = row.GetValueOrDefault("StartDate") as string;
                if (!TryParseLegacyGuid(row, "Person", out var legacyPersonOid) ||
                    !personMap.TryGetValue(legacyPersonOid, out var personId) ||
                    !DateTime.TryParse(startDateText, out var startDate))
                {
                    skipped++;
                    continue;
                }

                var targetId = await ScalarGuidAsync(conn,
                    """
                    SELECT TOP 1 CAST(ID AS varchar(36))
                    FROM EmployeePositionHistories
                    WHERE (GCRecord IS NULL OR GCRecord = 0)
                      AND PersonID = @personId
                      AND CAST(StartDate AS date) = @startDate
                    ORDER BY ID
                    """,
                    ("@personId", personId), ("@startDate", startDate.Date));
                if (!targetId.HasValue)
                {
                    skipped++;
                    continue;
                }

                map[legacyOid] = targetId.Value;
                matched++;
            }
        }
        else if (string.Equals(entity, "EmployeeSalary", StringComparison.OrdinalIgnoreCase))
        {
            var personMap = LoadMap(Path.Combine(mapDir, "Person.json"));
            var batch = Visa2014EmployeeSalaryTransform.PrepareImportBatch(
                source.ConnectionString, source.LookupTranslationPaths, maxRows: null, verbose: false);
            foreach (var row in batch.ImportRows)
            {
                if (row.GetValueOrDefault("_importAction") as string == "skip")
                {
                    skipped++;
                    continue;
                }

                var legacyPersonOid = (Guid)row["_legacyRowId"]!;
                if (!personMap.TryGetValue(legacyPersonOid, out var personId))
                {
                    skipped++;
                    continue;
                }

                var targetId = await ScalarGuidAsync(conn,
                    """
                    SELECT TOP 1 CAST(ID AS varchar(36))
                    FROM EmployeeSalaries
                    WHERE (GCRecord IS NULL OR GCRecord = 0)
                      AND PersonID = @personId
                    ORDER BY StartDate DESC, ID DESC
                    """,
                    ("@personId", personId));
                if (!targetId.HasValue)
                {
                    skipped++;
                    continue;
                }

                map[legacyPersonOid] = targetId.Value;
                matched++;
            }
        }
        else if (string.Equals(entity, "AddressOfResidence", StringComparison.OrdinalIgnoreCase))
        {
            var personMap = LoadMap(Path.Combine(mapDir, "Person.json"));
            var batch = Visa2014AddressOfResidenceTransform.PrepareImportBatch(
                source.ConnectionString, source.LookupTranslationPaths, maxRows: null, verbose: false);
            foreach (var row in batch.ImportRows)
            {
                if (row.GetValueOrDefault("_importAction") as string == "skip")
                {
                    skipped++;
                    continue;
                }

                if (!TryParseLegacyRowId(row, out var legacyOid))
                {
                    skipped++;
                    continue;
                }

                var fullAddress = row.GetValueOrDefault("FullAddress") as string;
                var expirationText = row.GetValueOrDefault("ExpirationDate") as string;
                if (!TryParseLegacyGuid(row, "Person", out var legacyPersonOid) ||
                    !personMap.TryGetValue(legacyPersonOid, out var personId) ||
                    string.IsNullOrWhiteSpace(fullAddress))
                {
                    skipped++;
                    continue;
                }

                DateTime? expiration = DateTime.TryParse(expirationText, out var exp) ? exp.Date : null;
                var targetId = await ScalarGuidAsync(conn,
                    """
                    SELECT TOP 1 CAST(ID AS varchar(36))
                    FROM AddressesOfResidence
                    WHERE (GCRecord IS NULL OR GCRecord = 0)
                      AND PersonID = @personId
                      AND FullAddress = @fullAddress
                      AND (
                            (@expiration IS NULL AND ExpirationDate IS NULL)
                         OR (ExpirationDate IS NOT NULL AND CAST(ExpirationDate AS date) = @expiration)
                      )
                    ORDER BY ID
                    """,
                    ("@personId", personId),
                    ("@fullAddress", fullAddress.Trim()),
                    ("@expiration", expiration.HasValue ? expiration.Value : DBNull.Value));
                if (!targetId.HasValue)
                {
                    skipped++;
                    continue;
                }

                map[legacyOid] = targetId.Value;
                matched++;
            }
        }
        else
        {
            Console.Error.WriteLine($"ERR Unsupported entity for rebuild: {entity}");
            return 1;
        }

        await Visa2014IdMapHelper.SaveAsync(mapPath, map);
        Console.WriteLine($"INF {entity} id-map: {matched} matched, {skipped} skipped -> {mapPath}");
        return 0;
    }

    private static Dictionary<Guid, Guid> LoadMap(string path)
    {
        if (!File.Exists(path))
            return new Dictionary<Guid, Guid>();
        return Visa2014IdMapHelper.Load(path);
    }


    private static bool TryParseLegacyRowId(Dictionary<string, object?> row, out Guid legacyOid)
    {
        legacyOid = Guid.Empty;
        var value = row.GetValueOrDefault("_legacyRowId");
        switch (value)
        {
            case Guid g:
                legacyOid = g;
                return true;
            case string s when Guid.TryParse(s, out var parsed):
                legacyOid = parsed;
                return true;
            default:
                return false;
        }
    }    private static bool TryParseLegacyGuid(Dictionary<string, object?> row, string field, out Guid legacyOid)
    {
        legacyOid = Guid.Empty;
        var text = row.GetValueOrDefault(field) as string;
        return !string.IsNullOrWhiteSpace(text) && Guid.TryParse(text, out legacyOid);
    }

    private static async Task<Guid?> ScalarGuidAsync(
        SqlConnection conn,
        string sql,
        params (string Name, object Value)[] parameters)
    {
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        foreach (var (name, value) in parameters)
            cmd.Parameters.AddWithValue(name, value);

        var text = await cmd.ExecuteScalarAsync() as string;
        return Guid.TryParse(text, out var id) ? id : null;
    }

    private static List<string> ParseEntities(IReadOnlyList<string> args)
    {
        var raw = GetOptionValue(args, "--entities");
        if (string.IsNullOrWhiteSpace(raw))
            return [];
        return raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
    }

    private static string GetTargetConnection(IReadOnlyList<string> args) =>
        GetOptionValue(args, "--target-connection")
        ?? Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
        ?? "Server=(localdb)\\mssqllocaldb;Database=Visa2026;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true";

    private static string? GetOptionValue(IReadOnlyList<string> args, string optionName)
    {
        for (int i = 0; i < args.Count - 1; i++)
        {
            if (string.Equals(args[i], optionName, StringComparison.OrdinalIgnoreCase))
                return args[i + 1];
        }

        return null;
    }
}