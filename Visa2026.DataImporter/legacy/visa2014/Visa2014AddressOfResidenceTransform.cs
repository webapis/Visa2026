using System.Text.RegularExpressions;

namespace Visa2026.DataImporter.Legacy.Visa2014;

internal sealed record Visa2014AddressOfResidenceRawRow(
    Guid LegacyOid,
    Guid LegacyPersonOid,
    string? DocumentType,
    string? RegionMgCode,
    string? RegionName,
    string? CityMgCode,
    string? CityName,
    string? AddressLine,
    DateTime? ExpirationDate);

internal static class Visa2014AddressOfResidenceTransform
{
    // VISA2015 uses ŞäherEtrap (U+015E U+00E4), not ŞeherEtrap — verify via UNICODE(SUBSTRING(name,1,2)) on sys.tables.
    private const string SeherEtrap = "\u015E\u00E4herEtrap";

    internal const string ExtractSql = $"""
        SELECT
            CAST(aor.Oid AS varchar(36)) AS Oid,
            CAST(aor.Person AS varchar(36)) AS LegacyPersonOid,
            doa.TypeOfDocument,
            ISNULL(r.mgCode, '') AS RegionMgCode,
            r.NameOfRegion AS RegionName,
            ISNULL(se.mgCode, '') AS CityMgCode,
            se.[{SeherEtrap}L] AS CityName,
            a.AddressLine,
            CONVERT(varchar(10), a.ExpiringDateOfAddressDocument, 23) AS ExpirationDate
        FROM dbo.AddressOfResidence aor
        INNER JOIN dbo.Person p ON aor.Person = p.Oid AND p.GCRecord IS NULL
        INNER JOIN dbo.Address a ON aor.Address = a.Oid AND a.GCRecord IS NULL
        LEFT JOIN dbo.Region r ON a.Region = r.Oid
        LEFT JOIN dbo.[{SeherEtrap}] se ON a.[{SeherEtrap}] = se.Oid
        LEFT JOIN dbo.DocumentOfAddress doa ON a.DocumentOfAddress = doa.Oid
        WHERE aor.GCRecord IS NULL
        """;

    internal static readonly string[] AddressOfResidenceMainColumnOrder =
    [
        "_legacyRowId", "_legacyTable", "_importAction",
        "Person", "Type", "Region", "City", "FullAddress", "Lodging", "Hotel", "Hospital", "OtherSite", "ExpirationDate",
        "_legacy_AddressLine", "_legacy_RegionMgCode", "_legacy_CityMgCode", "_legacy_PersonOid",
    ];

    private static readonly Dictionary<string, string> RegionMgCodeToNameTm = new(StringComparer.OrdinalIgnoreCase)
    {
        ["BN"] = "Balkan welaýaty",
        ["MR"] = "Mary welaýaty",
        ["AH"] = "Ahal welaýaty",
        ["AS"] = "Aşgabat şäheri",
        ["LB"] = "Lebap welaýaty",
        ["DZ"] = "Daşoguz welaýaty",
    };

    public static Visa2014PersonImportBatch PrepareImportBatch(
        string connectionString,
        IReadOnlyList<string> lookupTranslationPaths,
        int? maxRows,
        bool verbose)
    {
        var catalogs = Visa2014LookupTranslator.Load(lookupTranslationPaths);
        var sql = maxRows is > 0
            ? $"SELECT TOP ({maxRows.Value}) * FROM ({ExtractSql}) AS q"
            : ExtractSql;

        var dictRows = Visa2014SqlCmdReader.Query(connectionString, sql, verbose);
        var rawRows = new List<Visa2014AddressOfResidenceRawRow>();
        int parseSkipped = 0;
        foreach (var dict in dictRows)
        {
            if (TryParseRawRow(dict, out var parsed))
                rawRows.Add(parsed);
            else
                parseSkipped++;
        }

        if (verbose && parseSkipped > 0)
            Console.WriteLine($"  Skipped {parseSkipped} sqlcmd row(s) with invalid shape.");

        return TransformRows(rawRows, catalogs, out var skipped, out var unmappedDistinct, out _);
    }

    internal static bool TryParseRawRow(IReadOnlyDictionary<string, string?> row, out Visa2014AddressOfResidenceRawRow parsed)
    {
        parsed = null!;
        if (!row.TryGetValue("Oid", out var oidText) || !Guid.TryParse(oidText?.Trim(), out var legacyOid))
            return false;
        if (!row.TryGetValue("LegacyPersonOid", out var personText) || !Guid.TryParse(personText?.Trim(), out var legacyPersonOid))
            return false;

        DateTime? expiration = DateTime.TryParse(row.GetValueOrDefault("ExpirationDate"), out var exp) ? exp : null;

        parsed = new Visa2014AddressOfResidenceRawRow(
            LegacyOid: legacyOid,
            LegacyPersonOid: legacyPersonOid,
            DocumentType: row.GetValueOrDefault("TypeOfDocument"),
            RegionMgCode: NullIfEmpty(row.GetValueOrDefault("RegionMgCode")),
            RegionName: row.GetValueOrDefault("RegionName"),
            CityMgCode: NullIfEmpty(row.GetValueOrDefault("CityMgCode")),
            CityName: row.GetValueOrDefault("CityName"),
            AddressLine: row.GetValueOrDefault("AddressLine"),
            ExpirationDate: expiration);
        return true;
    }

    private static Visa2014PersonImportBatch TransformRows(
        IReadOnlyList<Visa2014AddressOfResidenceRawRow> rawRows,
        IReadOnlyDictionary<string, Visa2014LookupCatalog> catalogs,
        out List<Dictionary<string, object?>> skipped,
        out List<Dictionary<string, object?>> unmappedDistinct,
        out List<Dictionary<string, object?>> dedupeSummary)
    {
        skipped = [];
        dedupeSummary = [];
        var unmappedSet = new HashSet<string>(StringComparer.Ordinal);
        var importRows = new List<Dictionary<string, object?>>();

        foreach (var row in rawRows)
        {
            var unmapped = new List<string>();

            if (!TryResolveRegion(row, catalogs, out var regionNameTm, out var regionReason))
                unmapped.Add(regionReason ?? "Region");

            if (!TryResolveCity(row, catalogs, regionNameTm, out var cityNameTm, out var cityReason))
                unmapped.Add(cityReason ?? "City");

            if (unmapped.Count > 0)
            {
                skipped.Add(BuildSkippedRow(row, string.Join("; ", unmapped)));
                foreach (var u in unmapped)
                    unmappedSet.Add(u);
                continue;
            }

            var type = MapResidenceType(row.DocumentType, row.AddressLine);
            var stripped = Visa2014AddressLineNormalizer.StripRegionAndCityPrefixes(
                row.AddressLine, regionNameTm, cityNameTm);
            if (string.IsNullOrWhiteSpace(stripped))
                stripped = row.AddressLine?.Trim() ?? string.Empty;

            string? lodgingName = null;
            string? hotelName = null;
            string? hospitalName = null;
            string? otherSiteName = null;
            if (type == "Lodging")
                lodgingName = Visa2014AddressLineNormalizer.NormalizeLodgingCatalogAddress(
                    row.AddressLine, regionNameTm, cityNameTm);
            else if (type == "Hotel")
                hotelName = Visa2014AddressLineNormalizer.NormalizeHotelCatalogName(
                    row.AddressLine, regionNameTm, cityNameTm);
            else if (type == "Hospital")
                hospitalName = Visa2014AddressLineNormalizer.NormalizeHospitalCatalogName(
                    row.AddressLine, regionNameTm, cityNameTm);
            else if (type == "Other")
                otherSiteName = Visa2014AddressLineNormalizer.NormalizeLodgingCatalogAddress(
                    row.AddressLine, regionNameTm, cityNameTm);

            importRows.Add(new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["_legacyRowId"] = row.LegacyOid.ToString(),
                ["_legacyTable"] = "AddressOfResidence",
                ["_importAction"] = "import",
                ["Person"] = row.LegacyPersonOid.ToString(),
                ["Type"] = type,
                ["Region"] = regionNameTm,
                ["City"] = cityNameTm,
                ["FullAddress"] = type == "PrivateHouse" ? stripped : null,
                ["Lodging"] = lodgingName,
                ["Hotel"] = hotelName,
                ["Hospital"] = hospitalName,
                ["OtherSite"] = otherSiteName,
                ["ExpirationDate"] = type == "PrivateHouse" ? row.ExpirationDate?.ToString("yyyy-MM-dd") : null,
                ["_legacy_AddressLine"] = row.AddressLine,
                ["_legacy_RegionMgCode"] = row.RegionMgCode,
                ["_legacy_CityMgCode"] = row.CityMgCode,
                ["_legacy_PersonOid"] = row.LegacyPersonOid.ToString(),
            });
        }

        unmappedDistinct = unmappedSet
            .Select(u => new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["catalog"] = u.Split(':')[0],
                ["legacyValue"] = u.Contains(':') ? u[(u.IndexOf(':') + 1)..] : u,
                ["reason"] = "unmapped",
            })
            .OrderBy(r => r["catalog"]?.ToString(), StringComparer.Ordinal)
            .ThenBy(r => r["legacyValue"]?.ToString(), StringComparer.Ordinal)
            .ToList();

        return new Visa2014PersonImportBatch
        {
            LegacyRowCount = rawRows.Count,
            ImportRows = importRows,
            Skipped = skipped,
            UnmappedLookups = unmappedDistinct,
            DedupeMergedCount = 0,
            DedupeSummary = dedupeSummary,
        };
    }

    private static bool TryResolveRegion(
        Visa2014AddressOfResidenceRawRow row,
        IReadOnlyDictionary<string, Visa2014LookupCatalog> catalogs,
        out string? regionNameTm,
        out string? unmappedReason)
    {
        regionNameTm = null;
        unmappedReason = null;

        var mgCode = row.RegionMgCode;
        if (string.IsNullOrWhiteSpace(mgCode))
            mgCode = InferRegionMgCode(row.AddressLine, row.RegionName);

        if (!string.IsNullOrWhiteSpace(mgCode) &&
            Visa2014LookupTranslator.TryTranslate(catalogs, "Region", mgCode, out var target, out _))
        {
            regionNameTm = RegionMgCodeToNameTm.GetValueOrDefault(target ?? mgCode) ?? target;
            return regionNameTm != null;
        }

        unmappedReason = $"Region:{row.RegionMgCode ?? row.RegionName ?? row.AddressLine?[..Math.Min(40, row.AddressLine?.Length ?? 0)]}";
        return false;
    }

    private static string? InferRegionMgCode(string? addressLine, string? regionName)
    {
        if (!string.IsNullOrWhiteSpace(regionName))
        {
            var folded = Visa2014AddressLineNormalizer.NormalizeMatchKey(regionName);
            if (folded.Contains("balkan", StringComparison.Ordinal)) return "BN";
            if (folded.Contains("mary", StringComparison.Ordinal)) return "MR";
            if (folded.Contains("ahal", StringComparison.Ordinal)) return "AH";
            if (folded.Contains("asgabat", StringComparison.Ordinal) || folded.Contains("ashgabat", StringComparison.Ordinal)) return "AS";
            if (folded.Contains("lebap", StringComparison.Ordinal)) return "LB";
            if (folded.Contains("turkmenabat", StringComparison.Ordinal)) return "LB";
            if (folded.Contains("dasoguz", StringComparison.Ordinal) || folded.Contains("dashoguz", StringComparison.Ordinal)) return "DZ";
        }

        if (string.IsNullOrWhiteSpace(addressLine))
            return null;

        var line = Visa2014AddressLineNormalizer.NormalizeMatchKey(addressLine);
        if (IsAsgabatRegionLine(line))
            return "AS";
        if (StartsTurkmenabatCityPrefix(line))
            return "LB";
        if (line.StartsWith("balkan", StringComparison.Ordinal) || line.Contains(" wel balkan", StringComparison.Ordinal) || line.Contains("balkan wel", StringComparison.Ordinal))
            return "BN";
        if (line.StartsWith("mary", StringComparison.Ordinal) || line.Contains(" wel mary", StringComparison.Ordinal) || line.Contains("mary wel", StringComparison.Ordinal))
            return "MR";
        if (line.StartsWith("ahal", StringComparison.Ordinal) || line.Contains(" wel ahal", StringComparison.Ordinal) || line.Contains("ahal wel", StringComparison.Ordinal))
            return "AH";
        if (line.StartsWith("lebap", StringComparison.Ordinal) || line.Contains("lebap wel", StringComparison.Ordinal) || line.Contains("lebap w ", StringComparison.Ordinal))
            return "LB";
        if (line.StartsWith("dasoguz", StringComparison.Ordinal) || line.StartsWith("dashoguz", StringComparison.Ordinal) || line.Contains("dasoguz wel", StringComparison.Ordinal))
            return "DZ";
        if (Regex.IsMatch(line, @"^dasoguz\s+(s|seheri|sh)\b") || Regex.IsMatch(line, @"^dashoguz\s+(s|seheri|sh)\b"))
            return "DZ";
        if (Regex.IsMatch(line, @"^turkmenbasy\s+(s|seheri|sh)\b"))
            return "BN";
        return null;
    }

    private static bool IsAsgabatRegionLine(string line) =>
        Regex.IsMatch(line, @"^s[\.,]?\s*asgabat")
        || Regex.IsMatch(line, @"^s[\.,]?\s*askabat")
        || line.StartsWith("asgabat", StringComparison.Ordinal)
        || line.StartsWith("askabat", StringComparison.Ordinal)
        || Regex.IsMatch(line, @"^asgabat\s+(s|seheri|sh)\b")
        || line.StartsWith("saher asgabat", StringComparison.Ordinal);

    private static bool StartsTurkmenabatCityPrefix(string line) =>
        Regex.IsMatch(line, @"^turkmenabat\s+(s|seheri|sh)\b")
        || Regex.IsMatch(line, @"^turkmenabat\s*;")
        || line.StartsWith("turkmenabat seheri", StringComparison.Ordinal);

    private static bool TryResolveCity(
        Visa2014AddressOfResidenceRawRow row,
        IReadOnlyDictionary<string, Visa2014LookupCatalog> catalogs,
        string? regionNameTm,
        out string? cityNameTm,
        out string? unmappedReason)
    {
        cityNameTm = null;
        unmappedReason = null;

        if (!string.IsNullOrWhiteSpace(row.CityMgCode))
        {
            if (CityNameByPdfCode.TryGetValue(row.CityMgCode, out var byCode))
            {
                cityNameTm = byCode;
                return true;
            }

            if (Visa2014LookupTranslator.TryTranslate(catalogs, "City", row.CityMgCode, out var pdfCode, out _))
            {
                cityNameTm = CityNameByPdfCode.GetValueOrDefault(pdfCode ?? row.CityMgCode);
                if (cityNameTm != null)
                    return true;
            }
        }

        if (!string.IsNullOrWhiteSpace(row.CityName))
        {
            if (Visa2014LookupTranslator.TryTranslate(catalogs, "CityByName", row.CityName, out var byName, out _))
            {
                cityNameTm = byName;
                return true;
            }

            cityNameTm = NormalizeLegacyCityName(row.CityName);
            if (CityExistsInRegion(cityNameTm, regionNameTm))
                return true;
        }

        if (!string.IsNullOrWhiteSpace(row.AddressLine))
        {
            var inferred = InferCityFromAddressLine(row.AddressLine, regionNameTm);
            if (inferred != null)
            {
                cityNameTm = inferred;
                return true;
            }
        }

        unmappedReason = $"City:{row.CityMgCode ?? row.CityName ?? "(null)"}";
        return false;
    }

    private static string NormalizeLegacyCityName(string legacyCity) =>
        legacyCity
            .Replace("Asgabat", "Aşgabat", StringComparison.OrdinalIgnoreCase)
            .Replace("Dasoguz", "Daşoguz", StringComparison.OrdinalIgnoreCase)
            .Replace("Yoloten", "Ýolöten", StringComparison.OrdinalIgnoreCase)
            .Trim();

    private static bool CityExistsInRegion(string? cityNameTm, string? regionNameTm)
    {
        if (string.IsNullOrWhiteSpace(cityNameTm))
            return false;
        return true;
    }

    private static string? InferCityFromAddressLine(string addressLine, string? regionNameTm)
    {
        var line = Visa2014AddressLineNormalizer.NormalizeMatchKey(addressLine);
        if (string.IsNullOrEmpty(line))
            return null;

        var regionCode = RegionNameTmToMgCode(regionNameTm) ?? InferRegionMgCode(addressLine, null);

        if (regionCode == "AS" || IsAsgabatRegionLine(line) || Regex.IsMatch(line, @"^asgabat\s+(s|seheri|sh)\b"))
            return "Aşgabat şäheri";

        if (line.Contains("myhmanhan", StringComparison.Ordinal))
        {
            if (line.Contains("turkmenabat", StringComparison.Ordinal) || regionCode == "LB")
                return "Türkmenabat şäheri";
            if (line.Contains("asgabat", StringComparison.Ordinal) || regionCode == "AS")
                return "Aşgabat şäheri";
        }

        if (regionCode == "LB" || StartsTurkmenabatCityPrefix(line))
        {
            if (StartsTurkmenabatCityPrefix(line) || line.Contains("turkmenabat seheri", StringComparison.Ordinal))
                return "Türkmenabat şäheri";
            if (line.Contains("dowletli etr", StringComparison.Ordinal))
                return "Döwletli etraby";
            if (line.Contains("serdarabat", StringComparison.Ordinal) || line.Contains("serdar etr", StringComparison.Ordinal))
                return "Serdarabat etraby";
            if (line.Contains("gurbansoltan", StringComparison.Ordinal))
                return "Gurbansoltan-eje ad. etraby";
            if (line.Contains("saparmyrat turkmenbasy", StringComparison.Ordinal) || line.Contains("beyik saparmyrat", StringComparison.Ordinal))
                return "Beýik Saparmyrat Türkmenbaşy ad. etraby";
        }

        if (regionCode == "MR" || line.StartsWith("mary", StringComparison.Ordinal))
        {
            if (line.Contains("turkmenbasy sehercesi", StringComparison.Ordinal))
                return "Mary etrabynyň S.Türkmenbaşy şäherçesi";
            if (line.Contains("serhetabat seheri", StringComparison.Ordinal))
                return "Serhetabat şäheri";
            if (line.Contains("serhetabat", StringComparison.Ordinal))
                return "Serhetabat etraby";
            if (line.Contains("yoloten", StringComparison.Ordinal))
                return "Ýolöten şäheri";
            if (Regex.IsMatch(line, @"^mary\s+(s|seheri|sh)[\.,;\s]"))
                return "Mary şäheri";
            if (line.Contains("mary etr", StringComparison.Ordinal) || line.Contains("mary etrap", StringComparison.Ordinal) || line.Contains("mary-2", StringComparison.Ordinal))
                return "Mary etraby";
            return "Mary etraby";
        }

        if (regionCode == "BN" || line.StartsWith("balkan", StringComparison.Ordinal))
        {
            if (Regex.IsMatch(line, @"^turkmenbasy\s+(s|seheri|sh)\b"))
                return "Türkmenbaşy şäheri";
            if (line.Contains("turkmenbasy etr", StringComparison.Ordinal) || line.Contains("turkmenbasy etrap", StringComparison.Ordinal))
                return "Türkmenbaşy etraby";
            if (line.Contains("balkanabat", StringComparison.Ordinal))
                return "Balkanabat şäheri";
            if (line.Contains("serdar etr", StringComparison.Ordinal))
                return "Serdar etraby";
            if (line.Contains("garabogaz", StringComparison.Ordinal))
                return "Garabogaz şäheri";
        }

        if (regionCode == "AH" || line.StartsWith("ahal", StringComparison.Ordinal))
            return "Akbugdaý etraby";

        if (regionCode == "DZ" || Regex.IsMatch(line, @"^dasoguz\s+(s|seheri|sh)\b") || Regex.IsMatch(line, @"^dashoguz\s+(s|seheri|sh)\b"))
            return "Daşoguz şäheri";

        return null;
    }

    private static string? RegionNameTmToMgCode(string? regionNameTm)
    {
        if (string.IsNullOrWhiteSpace(regionNameTm))
            return null;

        foreach (var pair in RegionMgCodeToNameTm)
        {
            if (string.Equals(pair.Value, regionNameTm, StringComparison.OrdinalIgnoreCase))
                return pair.Key;
        }

        var folded = Visa2014AddressLineNormalizer.NormalizeMatchKey(regionNameTm);
        if (folded.Contains("mary", StringComparison.Ordinal)) return "MR";
        if (folded.Contains("lebap", StringComparison.Ordinal)) return "LB";
        if (folded.Contains("balkan", StringComparison.Ordinal)) return "BN";
        if (folded.Contains("ahal", StringComparison.Ordinal)) return "AH";
        if (folded.Contains("asgabat", StringComparison.Ordinal)) return "AS";
        if (folded.Contains("dasoguz", StringComparison.Ordinal) || folded.Contains("dashoguz", StringComparison.Ordinal)) return "DZ";
        return null;
    }

    private static readonly Dictionary<string, string> CityNameByPdfCode = new(StringComparer.OrdinalIgnoreCase)
    {
        ["BN15"] = "Türkmenbaşy etraby",
        ["AH48"] = "Akbugdaý etraby",
        ["MR36"] = "Mary etraby",
        ["AS69"] = "Aşgabat şäheri",
        ["MR19"] = "Mary şäheri",
        ["LB18"] = "Türkmenabat şäheri",
        ["DZ56"] = "Daşoguz şäheri",
        ["BN63"] = "Balkanabat şäheri",
        ["BN9"] = "Gumdag şäheri",
        ["BN10"] = "Garabogaz şäheri",
        ["MR23"] = "Serhetabat etraby",
        ["MR11"] = "Ýolöten şäheri",
        ["MR2"] = "Serhetabat şäheri",
        ["AH41"] = "Kaka etraby",
        ["AS57"] = "Köpetdag etraby",
    };

    private static string MapResidenceType(string? documentType, string? addressLine) => documentType?.Trim() switch
    {
        "Lojman" => Visa2014ResidenceClassifier.MapLojmanResidenceType(addressLine),
        "Patent" => Visa2014ResidenceClassifier.MapPatentResidenceType(addressLine),
        "myhmanhana" => Visa2014ResidenceClassifier.IsHospitalAddressLine(addressLine) ? "Hospital" : "Hotel",
        _ => "Other",
    };

    private static Dictionary<string, object?> BuildSkippedRow(Visa2014AddressOfResidenceRawRow row, string reason) =>
        new(StringComparer.Ordinal)
        {
            ["_legacyRowId"] = row.LegacyOid.ToString(),
            ["reason"] = reason,
            ["_legacy_AddressLine"] = row.AddressLine,
            ["_legacy_PersonOid"] = row.LegacyPersonOid.ToString(),
        };

    private static string? NullIfEmpty(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    internal static bool TryBuildLodgingSiteAddress(
        string? addressLine,
        string? regionMgCode,
        string? regionName,
        string? cityMgCode,
        string? cityName,
        IReadOnlyDictionary<string, Visa2014LookupCatalog> catalogs,
        out string? fullAddress,
        out string? regionNameTm,
        out string? cityNameTm,
        out string? unmappedReason)
    {
        fullAddress = null;
        regionNameTm = null;
        cityNameTm = null;
        unmappedReason = null;

        var row = new Visa2014AddressOfResidenceRawRow(
            LegacyOid: Guid.Empty,
            LegacyPersonOid: Guid.Empty,
            DocumentType: "Lojman",
            RegionMgCode: NullIfEmpty(regionMgCode),
            RegionName: regionName,
            CityMgCode: NullIfEmpty(cityMgCode),
            CityName: cityName,
            AddressLine: addressLine,
            ExpirationDate: null);

        if (!TryResolveRegion(row, catalogs, out regionNameTm, out var regionReason))
        {
            unmappedReason = regionReason;
            return false;
        }

        if (!TryResolveCity(row, catalogs, regionNameTm, out cityNameTm, out var cityReason))
        {
            unmappedReason = cityReason;
            return false;
        }

        fullAddress = Visa2014AddressLineNormalizer.NormalizeLodgingCatalogAddress(
            addressLine, regionNameTm, cityNameTm);
        if (string.IsNullOrWhiteSpace(fullAddress))
            fullAddress = addressLine?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(fullAddress))
        {
            unmappedReason = "empty after strip";
            return false;
        }

        return true;
    }

    internal static bool TryBuildOtherSiteAddress(
        string? addressLine,
        string? regionMgCode,
        string? regionName,
        string? cityMgCode,
        string? cityName,
        IReadOnlyDictionary<string, Visa2014LookupCatalog> catalogs,
        out string? fullAddress,
        out string? regionNameTm,
        out string? cityNameTm,
        out string? unmappedReason) =>
        TryBuildLodgingSiteAddress(
            addressLine, regionMgCode, regionName, cityMgCode, cityName, catalogs,
            out fullAddress, out regionNameTm, out cityNameTm, out unmappedReason);

    internal static bool TryBuildHotelSiteAddress(
        string? addressLine,
        string? regionMgCode,
        string? regionName,
        string? cityMgCode,
        string? cityName,
        IReadOnlyDictionary<string, Visa2014LookupCatalog> catalogs,
        out string? hotelName,
        out string? regionNameTm,
        out string? cityNameTm,
        out string? unmappedReason)
    {
        hotelName = null;
        regionNameTm = null;
        cityNameTm = null;
        unmappedReason = null;

        var row = new Visa2014AddressOfResidenceRawRow(
            LegacyOid: Guid.Empty,
            LegacyPersonOid: Guid.Empty,
            DocumentType: "myhmanhana",
            RegionMgCode: NullIfEmpty(regionMgCode),
            RegionName: regionName,
            CityMgCode: NullIfEmpty(cityMgCode),
            CityName: cityName,
            AddressLine: addressLine,
            ExpirationDate: null);

        if (!TryResolveRegion(row, catalogs, out regionNameTm, out var regionReason))
        {
            unmappedReason = regionReason;
            return false;
        }

        if (!TryResolveCity(row, catalogs, regionNameTm, out cityNameTm, out var cityReason))
        {
            unmappedReason = cityReason;
            return false;
        }

        hotelName = Visa2014AddressLineNormalizer.NormalizeHotelCatalogName(
            addressLine, regionNameTm, cityNameTm);
        if (string.IsNullOrWhiteSpace(hotelName))
            hotelName = addressLine?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(hotelName))
        {
            unmappedReason = "empty after strip";
            return false;
        }

        return true;
    }

    internal static bool TryBuildHospitalSiteAddress(
        string? addressLine,
        string? regionMgCode,
        string? regionName,
        string? cityMgCode,
        string? cityName,
        IReadOnlyDictionary<string, Visa2014LookupCatalog> catalogs,
        out string? hospitalName,
        out string? regionNameTm,
        out string? cityNameTm,
        out string? unmappedReason)
    {
        hospitalName = null;
        regionNameTm = null;
        cityNameTm = null;
        unmappedReason = null;

        var row = new Visa2014AddressOfResidenceRawRow(
            LegacyOid: Guid.Empty,
            LegacyPersonOid: Guid.Empty,
            DocumentType: "myhmanhana",
            RegionMgCode: NullIfEmpty(regionMgCode),
            RegionName: regionName,
            CityMgCode: NullIfEmpty(cityMgCode),
            CityName: cityName,
            AddressLine: addressLine,
            ExpirationDate: null);

        if (!TryResolveRegion(row, catalogs, out regionNameTm, out var regionReason))
        {
            unmappedReason = regionReason;
            return false;
        }

        if (!TryResolveCity(row, catalogs, regionNameTm, out cityNameTm, out var cityReason))
        {
            unmappedReason = cityReason;
            return false;
        }

        hospitalName = Visa2014AddressLineNormalizer.NormalizeHospitalCatalogName(
            addressLine, regionNameTm, cityNameTm);
        if (string.IsNullOrWhiteSpace(hospitalName))
            hospitalName = addressLine?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(hospitalName))
        {
            unmappedReason = "empty after strip";
            return false;
        }

        return true;
    }
}
