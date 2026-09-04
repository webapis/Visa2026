using System.Globalization;
using System.Text.RegularExpressions;

namespace Visa2026.DataImporter.Legacy.Visa2014;

/// <summary>
/// Read-only preview: derives <c>ProjectContract</c> ministry legs from legacy ApplicationProfileInstance routing
/// (VISA2015) so they can be reviewed before the tenant seed is regenerated.
///
/// Extraction rule (confirmed against Çalik VISA2015):
///   Leg 1 = Application.AppliedMinistery (per-application; majority per contract for the template).
///   Leg 2 = "Gurluşyk" (Ministry of Construction) when a construction forward is present
///           (DateForwardedToMinConstruction set OR DocNumberForwardedToMinConstruction non-empty).
/// This writer never touches the target DB or the seed JSON — it only produces the evidence workbook.
/// </summary>
internal static class Visa2014ProjectContractMinistryLegPreviewExporter
{
    /// <summary>Second leg destination — legacy stores only a date/doc number, no ministry name.</summary>
    private const string Leg2MinistryShortName = "Gurluşyk";

    private static readonly string[] ProjectContractLegsColumnOrder =
    [
        "ContractCode", "AppsOnContract", "Leg1FwdApps", "ObservedLegCount",
        "Leg1MinistryTitle", "Leg1MinistryShortName", "Leg1MinistryApps", "Leg1MajorityShare",
        "DistinctLeg1Ministries", "IsMixed",
        "Leg2Apps", "HasLeg2", "Leg2MinistryShortName", "_leg1Unmapped",
    ];

    internal static string ExtractSql => """
        SELECT
            LTRIM(RTRIM(c.NumberOfContract)) AS ContractCode,
            LTRIM(RTRIM(ISNULL(am.TitleOfMinistery, ''))) AS Leg1MinistryTitle,
            COUNT(*) AS Apps,
            SUM(CASE WHEN a.DateForwardedToMonistery >= '2000-01-01' THEN 1 ELSE 0 END) AS Leg1FwdApps,
            SUM(CASE WHEN a.DateForwardedToMinConstruction >= '2000-01-01'
                      OR NULLIF(LTRIM(RTRIM(a.DocNumberForwardedToMinConstruction)), '') IS NOT NULL
                     THEN 1 ELSE 0 END) AS Leg2Apps
        FROM dbo.Application a
        INNER JOIN dbo.Contract c ON c.Oid = a.Contract
        LEFT JOIN dbo.AppliedMinistery am ON am.Oid = a.AppliedMinistery
        WHERE a.GCRecord IS NULL AND c.GCRecord IS NULL
        GROUP BY LTRIM(RTRIM(c.NumberOfContract)), LTRIM(RTRIM(ISNULL(am.TitleOfMinistery, '')))
        """;

    public static Visa2014PreviewExportResult Export(
        string connectionString,
        IReadOnlyList<string> lookupTranslationPaths,
        string outputPath,
        int? maxRows,
        bool verbose,
        string? legacySourceId = null)
    {
        _ = lookupTranslationPaths;
        _ = maxRows;
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outputPath))!);

        var rawRows = Visa2014SqlCmdReader.Query(connectionString, ExtractSql, verbose);
        var breakdown = ParseBreakdown(rawRows);
        var contractSpecs = AggregateContracts(breakdown);
        var distinctMinistries = AggregateDistinctMinistries(breakdown);

        var contractRows = contractSpecs.Select(BuildContractRow).ToList();
        var breakdownRows = breakdown
            .OrderBy(b => b.ContractCode, StringComparer.OrdinalIgnoreCase)
            .ThenByDescending(b => b.Leg1FwdApps)
            .Select(BuildBreakdownRow)
            .ToList();
        var distinctRows = distinctMinistries.Select(BuildDistinctRow).ToList();

        var mixedContractCount = contractSpecs.Count(c => c.IsMixed);
        var twoLegContractCount = contractSpecs.Count(c => c.HasLeg2);
        var unmappedContractCount = contractSpecs.Count(c => c.Leg1FwdApps > 0 && string.IsNullOrEmpty(c.Leg1MinistryShortName));

        var metaRows = new List<IReadOnlyDictionary<string, object?>>
        {
            Meta("exportedAt", DateTime.UtcNow.ToString("O")),
            Meta("entity", "ProjectContractMinistryLeg"),
            Meta("database", GetDatabaseName(connectionString)),
            Meta("contractCount", contractSpecs.Count),
            Meta("twoLegContractCount", twoLegContractCount),
            Meta("mixedLeg1MinistryContractCount", mixedContractCount),
            Meta("unmappedLeg1MinistryContractCount", unmappedContractCount),
            Meta("distinctLeg1MinistryTitles", distinctMinistries.Count),
            Meta("leg2MinistryShortName", Leg2MinistryShortName),
            Meta("rule", "Leg1=Application.AppliedMinistery (majority per contract); Leg2=Gurluşyk when construction forward present"),
        };
        if (!string.IsNullOrWhiteSpace(legacySourceId))
            metaRows.Add(Meta("legacySource", legacySourceId));

        var writtenPath = Visa2014MinimalXlsxWriter.WriteWorkbook(outputPath,
        [
            new Visa2014Worksheet
            {
                Name = "ProjectContractLegs",
                Columns = ProjectContractLegsColumnOrder,
                Rows = contractRows,
            },
            new Visa2014Worksheet
            {
                Name = "_MinistryByContract",
                Columns = ["ContractCode", "Leg1MinistryTitle", "Leg1MinistryShortName", "Apps", "Leg1FwdApps", "Leg2Apps"],
                Rows = breakdownRows,
            },
            new Visa2014Worksheet
            {
                Name = "_DistinctMinistries",
                Columns = ["Leg1MinistryTitle", "Leg1MinistryShortName", "Contracts", "TotalApps", "Leg1FwdApps", "_unmapped"],
                Rows = distinctRows,
            },
            new Visa2014Worksheet { Name = "_Meta", Columns = ["_key", "value"], Rows = metaRows },
        ]);

        if (verbose)
        {
            Console.WriteLine($"  Contracts: {contractSpecs.Count}  2-leg: {twoLegContractCount}  mixed leg-1: {mixedContractCount}  unmapped leg-1: {unmappedContractCount}");
        }

        return new Visa2014PreviewExportResult
        {
            OutputPath = Path.GetFullPath(writtenPath),
            LegacyRowCount = rawRows.Count,
            ImportRowCount = contractSpecs.Count,
            SkippedRowCount = 0,
            DedupeMergedCount = 0,
            UnmappedLookupCount = unmappedContractCount,
        };
    }

    private sealed record MinistryBreakdownRow(
        string ContractCode,
        string Leg1MinistryTitle,
        int Apps,
        int Leg1FwdApps,
        int Leg2Apps);

    private sealed record ContractLegSpec(
        string ContractCode,
        int AppsOnContract,
        int Leg1FwdApps,
        string Leg1MinistryTitle,
        string Leg1MinistryShortName,
        int Leg1MinistryApps,
        int DistinctLeg1Ministries,
        bool IsMixed,
        int Leg2Apps)
    {
        public bool HasLeg2 => Leg2Apps > 0;
        public int ObservedLegCount => (Leg1FwdApps > 0 ? 1 : 0) + (HasLeg2 ? 1 : 0);
        public double Leg1MajorityShare =>
            Leg1FwdApps > 0 ? Math.Round((double)Leg1MinistryApps / Leg1FwdApps, 3) : 0d;
    }

    private sealed record DistinctMinistrySpec(
        string Leg1MinistryTitle,
        string Leg1MinistryShortName,
        int Contracts,
        int TotalApps,
        int Leg1FwdApps);

    private static List<MinistryBreakdownRow> ParseBreakdown(
        IReadOnlyList<IReadOnlyDictionary<string, string?>> rawRows)
    {
        var result = new List<MinistryBreakdownRow>();
        foreach (var row in rawRows)
        {
            var contractCode = NormalizeWhitespace(row.GetValueOrDefault("ContractCode"));
            if (string.IsNullOrEmpty(contractCode))
                continue;

            result.Add(new MinistryBreakdownRow(
                ContractCode: contractCode,
                Leg1MinistryTitle: NormalizeWhitespace(row.GetValueOrDefault("Leg1MinistryTitle")),
                Apps: ParseInt(row.GetValueOrDefault("Apps")),
                Leg1FwdApps: ParseInt(row.GetValueOrDefault("Leg1FwdApps")),
                Leg2Apps: ParseInt(row.GetValueOrDefault("Leg2Apps"))));
        }

        return result;
    }

    private static List<ContractLegSpec> AggregateContracts(IReadOnlyList<MinistryBreakdownRow> breakdown)
    {
        var specs = new List<ContractLegSpec>();
        foreach (var group in breakdown.GroupBy(b => b.ContractCode, StringComparer.OrdinalIgnoreCase))
        {
            var appsOnContract = group.Sum(b => b.Apps);
            var leg1FwdApps = group.Sum(b => b.Leg1FwdApps);
            var leg2Apps = group.Sum(b => b.Leg2Apps);

            // Leg-1 ministry = the ministry most applications on this contract were forwarded to.
            var forwardedMinistries = group
                .Where(b => b.Leg1FwdApps > 0 && !string.IsNullOrEmpty(b.Leg1MinistryTitle))
                .ToList();

            var majority = forwardedMinistries
                .OrderByDescending(b => b.Leg1FwdApps)
                .ThenByDescending(b => b.Apps)
                .ThenBy(b => b.Leg1MinistryTitle, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault();

            var distinctLeg1 = forwardedMinistries.Count;

            specs.Add(new ContractLegSpec(
                ContractCode: group.Key,
                AppsOnContract: appsOnContract,
                Leg1FwdApps: leg1FwdApps,
                Leg1MinistryTitle: majority?.Leg1MinistryTitle ?? string.Empty,
                Leg1MinistryShortName: majority == null ? string.Empty : MapMinistryShortName(majority.Leg1MinistryTitle),
                Leg1MinistryApps: majority?.Leg1FwdApps ?? 0,
                DistinctLeg1Ministries: distinctLeg1,
                IsMixed: distinctLeg1 > 1,
                Leg2Apps: leg2Apps));
        }

        return specs
            .OrderByDescending(s => s.AppsOnContract)
            .ThenBy(s => s.ContractCode, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static List<DistinctMinistrySpec> AggregateDistinctMinistries(
        IReadOnlyList<MinistryBreakdownRow> breakdown)
    {
        return breakdown
            .Where(b => !string.IsNullOrEmpty(b.Leg1MinistryTitle))
            .GroupBy(b => b.Leg1MinistryTitle, StringComparer.OrdinalIgnoreCase)
            .Select(g => new DistinctMinistrySpec(
                Leg1MinistryTitle: g.Key,
                Leg1MinistryShortName: MapMinistryShortName(g.Key),
                Contracts: g.Select(b => b.ContractCode).Distinct(StringComparer.OrdinalIgnoreCase).Count(),
                TotalApps: g.Sum(b => b.Apps),
                Leg1FwdApps: g.Sum(b => b.Leg1FwdApps)))
            .OrderByDescending(m => m.Leg1FwdApps)
            .ThenByDescending(m => m.TotalApps)
            .ToList();
    }

    private static Dictionary<string, object?> BuildContractRow(ContractLegSpec spec) =>
        new(StringComparer.Ordinal)
        {
            ["ContractCode"] = spec.ContractCode,
            ["AppsOnContract"] = spec.AppsOnContract,
            ["Leg1FwdApps"] = spec.Leg1FwdApps,
            ["ObservedLegCount"] = spec.ObservedLegCount,
            ["Leg1MinistryTitle"] = spec.Leg1MinistryTitle,
            ["Leg1MinistryShortName"] = spec.Leg1MinistryShortName,
            ["Leg1MinistryApps"] = spec.Leg1MinistryApps,
            ["Leg1MajorityShare"] = spec.Leg1MajorityShare.ToString(CultureInfo.InvariantCulture),
            ["DistinctLeg1Ministries"] = spec.DistinctLeg1Ministries,
            ["IsMixed"] = spec.IsMixed ? "1" : "0",
            ["Leg2Apps"] = spec.Leg2Apps,
            ["HasLeg2"] = spec.HasLeg2 ? "1" : "0",
            ["Leg2MinistryShortName"] = spec.HasLeg2 ? Leg2MinistryShortName : string.Empty,
            ["_leg1Unmapped"] = spec.Leg1FwdApps > 0 && string.IsNullOrEmpty(spec.Leg1MinistryShortName) ? "1" : "0",
        };

    private static Dictionary<string, object?> BuildBreakdownRow(MinistryBreakdownRow row) =>
        new(StringComparer.Ordinal)
        {
            ["ContractCode"] = row.ContractCode,
            ["Leg1MinistryTitle"] = row.Leg1MinistryTitle,
            ["Leg1MinistryShortName"] = MapMinistryShortName(row.Leg1MinistryTitle),
            ["Apps"] = row.Apps,
            ["Leg1FwdApps"] = row.Leg1FwdApps,
            ["Leg2Apps"] = row.Leg2Apps,
        };

    private static Dictionary<string, object?> BuildDistinctRow(DistinctMinistrySpec spec) =>
        new(StringComparer.Ordinal)
        {
            ["Leg1MinistryTitle"] = spec.Leg1MinistryTitle,
            ["Leg1MinistryShortName"] = spec.Leg1MinistryShortName,
            ["Contracts"] = spec.Contracts,
            ["TotalApps"] = spec.TotalApps,
            ["Leg1FwdApps"] = spec.Leg1FwdApps,
            ["_unmapped"] = string.IsNullOrEmpty(spec.Leg1MinistryShortName) ? "1" : "0",
        };

    /// <summary>
    /// Best-effort mapping of a legacy <c>AppliedMinistery.TitleOfMinistery</c> to a Visa2026
    /// <c>ApprovingMinistry.ShortNameTm</c>. Unmapped titles return empty (flagged in the preview
    /// so the reviewer can extend the catalog / translation before seed regeneration).
    /// </summary>
    internal static string MapMinistryShortName(string? title)
    {
        if (string.IsNullOrWhiteSpace(title))
            return string.Empty;

        var upper = title.ToUpperInvariant();

        if (upper.Contains("ENERGETIKA"))
            return "Energetika";
        if (upper.Contains("GAZ"))
            return "Türkmengaz";
        if (upper.Contains("GABAT") || upper.Contains("HÄKIM") || upper.Contains("HAKIM"))
            return "Aşgabat häkimlik";
        if (upper.Contains("NGIZ") || upper.Contains("NEBITI GAÝTADAN") || upper.Contains("NEBITI GAYTADAN") || upper.Contains("TÜRKMENBAŞYDAKY") || upper.Contains("TURKMENBASYDAKY"))
            return "TNGIZ";
        if (upper.Contains("HIMI"))
            return "Türkmenhimiýa";
        if (upper.Contains("NEBIT"))
            return "Türkmennebit";

        return string.Empty;
    }

    private static string NormalizeWhitespace(string? value) =>
        string.IsNullOrWhiteSpace(value) ? string.Empty : Regex.Replace(value.Trim(), "\\s+", " ");

    private static int ParseInt(string? text) =>
        int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) ? value : 0;

    private static Dictionary<string, object?> Meta(string key, object? value) =>
        new(StringComparer.Ordinal) { ["_key"] = key, ["value"] = value };

    private static string GetDatabaseName(string connectionString)
    {
        foreach (var part in connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            if (part.StartsWith("Database=", StringComparison.OrdinalIgnoreCase))
                return part["Database=".Length..].Trim();
            if (part.StartsWith("Initial Catalog=", StringComparison.OrdinalIgnoreCase))
                return part["Initial Catalog=".Length..].Trim();
        }

        return "VISA2015";
    }
}
