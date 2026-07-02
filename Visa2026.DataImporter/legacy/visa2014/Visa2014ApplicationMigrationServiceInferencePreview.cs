namespace Visa2026.DataImporter.Legacy.Visa2014;

internal static class Visa2014ApplicationMigrationServiceInferencePreview
{
    private const string SeherEtrap = "\u015E\u00E4herEtrap";

    internal static readonly string[] MainColumnOrder =
    [
        "ManualApplicationNumber", "_legacyApplicationOid", "_legacyPersonOid",
        "ApplicationDate", "RegionMgCode", "RegionName", "CityMgCode", "CityName",
        "ProposedMigrationService", "Confidence", "Reason",
        "_usedExpiredAddressFallback", "_addressCount",
    ];

    private const string ApplicationsSql = $"""
        SELECT
            CAST(a.Oid AS varchar(36)) AS LegacyApplicationOid,
            r.ManualApplicationNumber,
            CONVERT(varchar(10), r.ManualApplicationDate, 23) AS ApplicationDate,
            CASE
                WHEN ISNULL(a.ForEmployee, 0) = 1 THEN CAST(pia.Employee AS varchar(36))
                ELSE CAST(pia.FamilyMember AS varchar(36))
            END AS LegacyPersonOid
        FROM dbo.Application a
        INNER JOIN dbo.IRegistration_Data r ON r.Oid = a.IRegistration_Data
        INNER JOIN dbo.PersonInApplication pia ON pia.Application = a.Oid AND pia.GCRecord IS NULL
        LEFT JOIN dbo.ApplicationTypeForEmployee ate ON ate.Oid = a.ApplicationTypeForEmployee
        LEFT JOIN dbo.ApplicationTypeForFamilyMember atfm ON atfm.Oid = a.ApplicationTypeForFamilyMember
        WHERE a.GCRecord IS NULL
          AND a.DepartmentForRegistration IS NULL
          AND (
              (ISNULL(a.ForEmployee, 0) = 1 AND ate.TypeOfApplicationForEmployeeID = 2)
              OR (ISNULL(a.ForFamilyMember, 0) = 1 AND atfm.TypeOfApplicationForFamilyMemberID = 2)
          )
        """;

    private static string AddressesSql(IReadOnlyCollection<Guid> personOids)
    {
        var idList = string.Join(", ", personOids.Select(id => $"'{id:D}'"));
        return $"""
            SELECT
                CAST(aor.Oid AS varchar(36)) AS LegacyAddressOid,
                CAST(aor.Person AS varchar(36)) AS LegacyPersonOid,
                ISNULL(r.mgCode, '') AS RegionMgCode,
                r.NameOfRegion AS RegionName,
                ISNULL(se.mgCode, '') AS CityMgCode,
                se.[{SeherEtrap}L] AS CityName,
                CONVERT(varchar(10), addr.ExpiringDateOfAddressDocument, 23) AS ExpirationDate
            FROM dbo.AddressOfResidence aor
            INNER JOIN dbo.Address addr ON aor.Address = addr.Oid AND addr.GCRecord IS NULL
            LEFT JOIN dbo.Region r ON addr.Region = r.Oid
            LEFT JOIN dbo.[{SeherEtrap}] se ON addr.[{SeherEtrap}] = se.Oid
            WHERE aor.GCRecord IS NULL
              AND aor.Person IN ({idList})
            """;
    }

    public static Visa2014PreviewExportResult Export(
        string connectionString,
        string rulesYamlPath,
        string outputPath,
        int? maxRows,
        bool verbose,
        string? legacySourceId = null)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outputPath))!);

        var (exportRows, rules, confidenceCounts) = PrepareInferenceRows(
            connectionString, rulesYamlPath, maxRows, verbose);

        foreach (var pair in confidenceCounts.OrderBy(p => p.Key))
            Console.WriteLine($"INF Confidence {pair.Key}: {pair.Value}");

        var metaRows = new List<IReadOnlyDictionary<string, object?>>
        {
            Meta("_key", "exportedAt", DateTime.UtcNow.ToString("O")),
            Meta("_key", "entity", "ApplicationMigrationServiceInference"),
            Meta("_key", "database", GetDatabaseName(connectionString)),
            Meta("_key", "legacyRowCount", exportRows.Count),
            Meta("_key", "importRowCount", exportRows.Count),
            Meta("_key", "rulesYaml", rulesYamlPath),
            Meta("_key", "approvedForPatch", rules.ApprovedForPatch),
            Meta("_key", "confidenceHigh", confidenceCounts["high"]),
            Meta("_key", "confidenceMedium", confidenceCounts["medium"]),
            Meta("_key", "confidenceLow", confidenceCounts["low"]),
            Meta("_key", "confidenceNone", confidenceCounts["none"]),
        };
        if (!string.IsNullOrWhiteSpace(legacySourceId))
            metaRows.Add(Meta("_key", "legacySource", legacySourceId));

        var writtenPath = Visa2014MinimalXlsxWriter.WriteWorkbook(outputPath,
        [
            new Visa2014Worksheet
            {
                Name = "Inference",
                Columns = MainColumnOrder,
                Rows = exportRows,
            },
            new Visa2014Worksheet
            {
                Name = "_Meta",
                Columns = ["_key", "value"],
                Rows = metaRows,
            },
        ]);

        return new Visa2014PreviewExportResult
        {
            OutputPath = Path.GetFullPath(writtenPath),
            LegacyRowCount = exportRows.Count,
            ImportRowCount = exportRows.Count,
            SkippedRowCount = confidenceCounts["none"],
            DedupeMergedCount = 0,
            UnmappedLookupCount = confidenceCounts["none"],
        };
    }

    internal static (List<Dictionary<string, object?>> Rows, Visa2014MigrationServiceInferenceRules Rules, Dictionary<string, int> ConfidenceCounts)
        PrepareInferenceRows(
            string connectionString,
            string rulesYamlPath,
            int? maxRows,
            bool verbose)
    {
        var rules = Visa2014MigrationServiceInferenceRules.Load(rulesYamlPath);
        var appSql = maxRows is > 0
            ? $"SELECT TOP ({maxRows.Value}) * FROM ({ApplicationsSql}) AS q ORDER BY ManualApplicationNumber"
            : $"{ApplicationsSql} ORDER BY ManualApplicationNumber";

        var appRows = Visa2014SqlCmdReader.Query(connectionString, appSql, verbose);
        var applications = ParseApplications(appRows);

        var personOids = applications
            .Select(a => a.LegacyPersonOid)
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToList();

        var addressesByPerson = LoadAddressesByPerson(connectionString, personOids, verbose);

        var exportRows = new List<Dictionary<string, object?>>();
        var confidenceCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["high"] = 0,
            ["medium"] = 0,
            ["low"] = 0,
            ["none"] = 0,
        };

        foreach (var app in applications)
        {
            var row = BuildExportRow(app, addressesByPerson, rules);
            exportRows.Add(row);

            var confidence = row.GetValueOrDefault("Confidence") as string ?? "none";
            if (confidenceCounts.ContainsKey(confidence))
                confidenceCounts[confidence]++;
            else
                confidenceCounts["none"]++;
        }

        return (exportRows, rules, confidenceCounts);
    }

    private static Dictionary<string, object?> BuildExportRow(
        ApplicationRow app,
        IReadOnlyDictionary<Guid, List<Visa2014AddressForInference>> addressesByPerson,
        Visa2014MigrationServiceInferenceRules rules)
    {
        string? regionMgCode = null;
        string? regionName = null;
        string? cityMgCode = null;
        string? cityName = null;
        string confidence = "none";
        string reason;
        string? proposed = null;
        bool usedExpiredFallback = false;
        int addressCount = 0;

        if (!TryInferMigrationService(
                app.LegacyPersonOid,
                app.ApplicationDate,
                addressesByPerson,
                rules,
                out proposed,
                out confidence,
                out reason,
                out regionMgCode,
                out regionName,
                out cityMgCode,
                out cityName,
                out usedExpiredFallback,
                out addressCount))
        {
            // reason already set by TryInferMigrationService
        }

        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["ManualApplicationNumber"] = app.ManualApplicationNumber,
            ["_legacyApplicationOid"] = app.LegacyApplicationOid,
            ["_legacyPersonOid"] = app.LegacyPersonOid == Guid.Empty ? null : app.LegacyPersonOid,
            ["ApplicationDate"] = app.ApplicationDate?.ToString("yyyy-MM-dd"),
            ["RegionMgCode"] = regionMgCode,
            ["RegionName"] = regionName,
            ["CityMgCode"] = cityMgCode,
            ["CityName"] = cityName,
            ["ProposedMigrationService"] = proposed,
            ["Confidence"] = confidence,
            ["Reason"] = reason,
            ["_usedExpiredAddressFallback"] = usedExpiredFallback,
            ["_addressCount"] = addressCount,
        };
    }

    internal static bool TryInferMigrationService(
        Guid legacyPersonOid,
        DateTime? applicationDate,
        IReadOnlyDictionary<Guid, List<Visa2014AddressForInference>> addressesByPerson,
        Visa2014MigrationServiceInferenceRules rules,
        out string? migrationServiceNameTm,
        out string confidence,
        out string reason,
        out string? regionMgCode,
        out string? regionName,
        out string? cityMgCode,
        out string? cityName,
        out bool usedExpiredFallback,
        out int addressCount)
    {
        regionMgCode = null;
        regionName = null;
        cityMgCode = null;
        cityName = null;
        migrationServiceNameTm = null;
        confidence = "none";
        reason = "";
        usedExpiredFallback = false;
        addressCount = 0;

        if (legacyPersonOid == Guid.Empty)
        {
            reason = "No PersonInApplication person OID";
            return false;
        }

        if (!addressesByPerson.TryGetValue(legacyPersonOid, out var addresses) || addresses.Count == 0)
        {
            reason = "No AddressOfResidence for person";
            return false;
        }

        addressCount = addresses.Count;
        var current = Visa2014MigrationServiceAddressPicker.PickCurrent(
            addresses,
            applicationDate,
            out usedExpiredFallback);

        if (current == null)
        {
            reason = "Address picker returned null";
            return false;
        }

        regionMgCode = NullIfEmpty(current.RegionMgCode);
        regionName = current.RegionName;
        cityMgCode = NullIfEmpty(current.CityMgCode);
        cityName = current.CityName;

        var inference = rules.Infer(
            regionMgCode,
            regionName,
            cityMgCode,
            cityName,
            usedExpiredFallback);

        migrationServiceNameTm = inference.MigrationServiceNameTm;
        confidence = inference.Confidence;
        reason = inference.Reason;

        return !string.IsNullOrWhiteSpace(migrationServiceNameTm)
            && !string.Equals(confidence, "none", StringComparison.OrdinalIgnoreCase);
    }

    internal static Dictionary<Guid, List<Visa2014AddressForInference>> LoadAddressesByPerson(
        string connectionString,
        IReadOnlyList<Guid> personOids,
        bool verbose)
    {
        var result = new Dictionary<Guid, List<Visa2014AddressForInference>>();
        if (personOids.Count == 0)
            return result;

        const int batchSize = 200;
        for (int offset = 0; offset < personOids.Count; offset += batchSize)
        {
            var batch = personOids.Skip(offset).Take(batchSize).ToList();
            var sql = AddressesSql(batch);
            var rows = Visa2014SqlCmdReader.Query(connectionString, sql, verbose);

            foreach (var dict in rows)
            {
                if (!Guid.TryParse(dict.GetValueOrDefault("LegacyPersonOid"), out var personOid))
                    continue;
                if (!Guid.TryParse(dict.GetValueOrDefault("LegacyAddressOid"), out var addressOid))
                    continue;

                DateTime? expiration = DateTime.TryParse(dict.GetValueOrDefault("ExpirationDate"), out var exp)
                    ? exp
                    : null;

                var address = new Visa2014AddressForInference(
                    addressOid,
                    NullIfEmpty(dict.GetValueOrDefault("RegionMgCode")),
                    dict.GetValueOrDefault("RegionName"),
                    NullIfEmpty(dict.GetValueOrDefault("CityMgCode")),
                    dict.GetValueOrDefault("CityName"),
                    expiration);

                if (!result.TryGetValue(personOid, out var list))
                {
                    list = [];
                    result[personOid] = list;
                }

                list.Add(address);
            }
        }

        return result;
    }

    private static List<ApplicationRow> ParseApplications(
        IReadOnlyList<IReadOnlyDictionary<string, string?>> rows)
    {
        var apps = new List<ApplicationRow>();
        foreach (var row in rows)
        {
            if (!Guid.TryParse(row.GetValueOrDefault("LegacyApplicationOid"), out var appOid))
                continue;

            Guid.TryParse(row.GetValueOrDefault("LegacyPersonOid"), out var personOid);
            DateTime? appDate = DateTime.TryParse(row.GetValueOrDefault("ApplicationDate"), out var parsed)
                ? parsed
                : null;

            apps.Add(new ApplicationRow(
                appOid,
                row.GetValueOrDefault("ManualApplicationNumber"),
                appDate,
                personOid));
        }

        return apps;
    }

    private static string? NullIfEmpty(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static Dictionary<string, object?> Meta(string k1, string k2, object? v) =>
        new(StringComparer.Ordinal) { [k1] = k2, ["value"] = v };

    private static string GetDatabaseName(string connectionString)
    {
        foreach (var part in connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            if (part.StartsWith("Database=", StringComparison.OrdinalIgnoreCase))
                return part["Database=".Length..].Trim();
            if (part.StartsWith("Initial Catalog=", StringComparison.OrdinalIgnoreCase))
                return part["Initial Catalog=".Length..].Trim();
        }

        return "?";
    }

    private sealed record ApplicationRow(
        Guid LegacyApplicationOid,
        string? ManualApplicationNumber,
        DateTime? ApplicationDate,
        Guid LegacyPersonOid);
}
