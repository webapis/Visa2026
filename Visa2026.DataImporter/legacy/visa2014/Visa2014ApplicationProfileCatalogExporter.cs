using Visa2026.Module.DatabaseUpdate;

namespace Visa2026.DataImporter.Legacy.Visa2014;

/// <summary>
/// Wave 0b — proposed tenant <see cref="BusinessObjects.ApplicationProfile"/> catalog from legacy VISA2015 ApplicationProfileInstance rows.
/// Via-ministry types: one profile per (ApplicationType, ProjectContract) when contract is set; otherwise type-only.
/// Direct migration: one profile per ApplicationType.
/// </summary>
internal static class Visa2014ApplicationProfileCatalogExporter
{
    private static readonly string[] ProfileColumnOrder =
    [
        "ApplicationTypeName", "ProfileCatalogKey", "ProfileGranularity", "DefaultProjectContractCode",
        "ProfileCode", "ProfileName", "ProfileDescription", "SelectionCode",
        "ProgressRoute", "ActionFamily",
        "ForEmployee", "ForFamilyMember", "ForTemporaryVisitor",
        "ProduceInvitation", "ProduceWorkPermit", "ProduceVisa", "ProduceBorderZone", "ProduceRejection", "ProduceWorkLocation",
        "MinistrySlaDays", "MigrationSlaDays", "MigrationSlaProfileCode",
        "RequirePersonPassport", "RequirePersonEducation", "RequirePersonPosition", "RequirePersonAddressOfResidence",
        "ApplicationCount", "DistinctCompositeCount", "FirstApplicationDate", "LastApplicationDate",
        "WithProjectContractCount", "DistinctApprovalLegProfileCount", "TopApprovalLegProfile",
        "Decision", "SignOff",
    ];

    public static Visa2014PreviewExportResult Export(
        string connectionString,
        IReadOnlyList<string> lookupTranslationPaths,
        string outputPath,
        int? maxRows,
        bool verbose,
        string? legacySourceId = null)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outputPath))!);

        var batch = Visa2014ApplicationPreviewExporter.PrepareImportBatch(
            connectionString,
            lookupTranslationPaths,
            maxRows,
            verbose);

        var importRows = batch.ImportRows;
        var profileRows = BuildProfileRows(importRows);
        var compositeRows = BuildCompositeRows(importRows, batch.Skipped);
        var duplicateRows = BuildDuplicateNumberRows(importRows);
        var missingCatalogRows = BuildMissingCatalogRows(importRows);
        var unmappedTypeRows = BuildUnmappedTypeRows(batch.Skipped);

        var metaRows = new List<IReadOnlyDictionary<string, object?>>
        {
            Meta("exportedAt", DateTime.UtcNow.ToString("O")),
            Meta("entity", "ApplicationProfileCatalog"),
            Meta("wave", "0b-proposal"),
            Meta("database", GetDatabaseName(connectionString)),
            Meta("legacyRowCount", batch.LegacyRowCount),
            Meta("importRowCount", importRows.Count),
            Meta("skippedRowCount", batch.Skipped.Count),
            Meta("proposedProfileCount", profileRows.Count),
            Meta("granularity", "via_ministry_type_plus_contract_else_type_only"),
            Meta("recency", "full_history"),
            Meta("profileFkRule", "per_legacy_oid_application_type_not_manual_number"),
        };
        if (!string.IsNullOrWhiteSpace(legacySourceId))
            metaRows.Add(Meta("legacySource", legacySourceId));

        var writtenPath = Visa2014MinimalXlsxWriter.WriteWorkbook(outputPath,
        [
            new Visa2014Worksheet { Name = "ApplicationProfiles", Columns = ProfileColumnOrder, Rows = profileRows },
            new Visa2014Worksheet
            {
                Name = "_ByComposite",
                Columns =
                [
                    "_legacy_ApplicationTypeComposite", "ApplicationType", "ApplicationCount",
                    "FirstApplicationDate", "LastApplicationDate", "ImportAction",
                ],
                Rows = compositeRows,
            },
            new Visa2014Worksheet
            {
                Name = "_DuplicateNumbers",
                Columns =
                [
                    "FullApplicationNumber", "DistinctLegacyOidCount", "DistinctCompositeCount",
                    "DistinctApplicationTypeCount", "SampleLegacyOids", "Note",
                ],
                Rows = duplicateRows,
            },
            new Visa2014Worksheet
            {
                Name = "_MissingTypeCatalog",
                Columns = ["ApplicationType", "ApplicationCount", "Note"],
                Rows = missingCatalogRows,
            },
            new Visa2014Worksheet
            {
                Name = "_SkippedComposites",
                Columns = ["_legacy_ApplicationTypeComposite", "SkippedCount", "_skipReason", "Note"],
                Rows = unmappedTypeRows,
            },
            new Visa2014Worksheet
            {
                Name = "_UnmappedLookups",
                Columns = ["catalog", "legacyValue", "reason"],
                Rows = batch.UnmappedLookups.ToList(),
            },
            new Visa2014Worksheet { Name = "_Meta", Columns = ["_key", "value"], Rows = metaRows },
        ]);

        return new Visa2014PreviewExportResult
        {
            OutputPath = Path.GetFullPath(writtenPath),
            LegacyRowCount = batch.LegacyRowCount,
            ImportRowCount = profileRows.Count,
            SkippedRowCount = batch.Skipped.Count,
            DedupeMergedCount = batch.DedupeMergedCount,
            UnmappedLookupCount = batch.UnmappedLookups.Count,
        };
    }

    private static List<Dictionary<string, object?>> BuildProfileRows(
        IReadOnlyList<Dictionary<string, object?>> importRows)
    {
        var groups = importRows
            .Where(r => r.GetValueOrDefault("ApplicationType") is string type && !string.IsNullOrWhiteSpace(type))
            .Select(r => (
                Row: r,
                TypeName: (string)r["ApplicationType"]!,
                ContractCode: r.GetValueOrDefault("ProjectContract") as string))
            .Where(x => ApplicationProfileCatalogGrouping.TryResolveGroupKey(
                x.TypeName,
                x.ContractCode,
                out _))
            .GroupBy(
                x =>
                {
                    ApplicationProfileCatalogGrouping.TryResolveGroupKey(
                        x.TypeName,
                        x.ContractCode,
                        out var key);
                    return key.CatalogKey;
                },
                StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(g => g.Count())
            .ThenBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var rows = new List<Dictionary<string, object?>>();
        foreach (var group in groups)
        {
            var first = group.First();

            if (!ApplicationProfileCatalogPreviewHelper.TryBuild(
                    first.TypeName,
                    first.ContractCode,
                    out var preview))
                continue;

            var groupRows = group.Select(x => x.Row).ToList();
            var dates = groupRows
                .Select(ParseApplicationDate)
                .Where(d => d.HasValue)
                .Select(d => d!.Value)
                .ToList();

            var composites = groupRows
                .Select(r => r.GetValueOrDefault("_legacy_ApplicationTypeComposite") as string)
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Distinct(StringComparer.Ordinal)
                .Count();

            var withContract = groupRows.Count(r => HasNonEmpty(r, "ProjectContract"));
            var legProfiles = groupRows
                .Select(r => r.GetValueOrDefault("ApprovalLegProfile") as string)
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .GroupBy(v => v!, StringComparer.OrdinalIgnoreCase)
                .OrderByDescending(g => g.Count())
                .ToList();

            var topLeg = legProfiles.FirstOrDefault()?.Key;

            rows.Add(preview.ToExportDictionary(
                applicationCount: groupRows.Count,
                compositeCount: composites,
                firstApplicationDate: dates.Count > 0 ? dates.Min() : null,
                lastApplicationDate: dates.Count > 0 ? dates.Max() : null,
                withProjectContractCount: withContract,
                distinctApprovalLegProfileCount: legProfiles.Count,
                topApprovalLegProfile: topLeg));
        }

        return rows;
    }

    private static List<Dictionary<string, object?>> BuildCompositeRows(
        IReadOnlyList<Dictionary<string, object?>> importRows,
        IReadOnlyList<Dictionary<string, object?>> skippedRows)
    {
        var all = importRows
            .Select(r => (Row: r, Action: "import"))
            .Concat(skippedRows.Select(r => (Row: r, Action: "skip")));

        return all
            .Where(x => x.Row.GetValueOrDefault("_legacy_ApplicationTypeComposite") is string c && !string.IsNullOrWhiteSpace(c))
            .GroupBy(x => (
                    Composite: (string)x.Row["_legacy_ApplicationTypeComposite"]!,
                    Type: x.Row.GetValueOrDefault("ApplicationType") as string ?? string.Empty,
                    Action: x.Action))
            .Select(g =>
            {
                var dates = g.Select(x => ParseApplicationDate(x.Row)).Where(d => d.HasValue).Select(d => d!.Value).ToList();
                return new Dictionary<string, object?>(StringComparer.Ordinal)
                {
                    ["_legacy_ApplicationTypeComposite"] = g.Key.Composite,
                    ["ApplicationType"] = g.Key.Type,
                    ["ApplicationCount"] = g.Count(),
                    ["FirstApplicationDate"] = dates.Count > 0 ? dates.Min().ToString("yyyy-MM-dd") : string.Empty,
                    ["LastApplicationDate"] = dates.Count > 0 ? dates.Max().ToString("yyyy-MM-dd") : string.Empty,
                    ["ImportAction"] = g.Key.Action,
                };
            })
            .OrderByDescending(r => (int)(r["ApplicationCount"] ?? 0))
            .ThenBy(r => r["_legacy_ApplicationTypeComposite"] as string, StringComparer.Ordinal)
            .ToList();
    }

    private static List<Dictionary<string, object?>> BuildDuplicateNumberRows(
        IReadOnlyList<Dictionary<string, object?>> importRows)
    {
        return importRows
            .Where(r => r.GetValueOrDefault("FullApplicationNumber") is string n && !string.IsNullOrWhiteSpace(n))
            .GroupBy(r => (string)r["FullApplicationNumber"]!, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Select(r => r.GetValueOrDefault("_legacyRowId")).Distinct().Count() > 1)
            .Select(g =>
            {
                var composites = g
                    .Select(r => r.GetValueOrDefault("_legacy_ApplicationTypeComposite") as string ?? string.Empty)
                    .Distinct(StringComparer.Ordinal)
                    .ToList();
                var types = g
                    .Select(r => r.GetValueOrDefault("ApplicationType") as string ?? string.Empty)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
                var oids = g
                    .Select(r => r.GetValueOrDefault("_legacyRowId") as string)
                    .Where(v => !string.IsNullOrWhiteSpace(v))
                    .Take(5)
                    .ToList();

                var note = types.Count > 1
                    ? "Same number, different ApplicationType — profile follows each legacy Oid row."
                    : "Same number, multiple legacy Oids — profile follows each legacy Oid row.";

                return new Dictionary<string, object?>(StringComparer.Ordinal)
                {
                    ["FullApplicationNumber"] = g.Key,
                    ["DistinctLegacyOidCount"] = g.Select(r => r.GetValueOrDefault("_legacyRowId")).Distinct().Count(),
                    ["DistinctCompositeCount"] = composites.Count,
                    ["DistinctApplicationTypeCount"] = types.Count(t => !string.IsNullOrWhiteSpace(t)),
                    ["SampleLegacyOids"] = string.Join("; ", oids),
                    ["Note"] = note,
                };
            })
            .OrderByDescending(r => (int)(r["DistinctApplicationTypeCount"] ?? 0))
            .ThenByDescending(r => (int)(r["DistinctLegacyOidCount"] ?? 0))
            .ToList();
    }

    private static List<Dictionary<string, object?>> BuildMissingCatalogRows(
        IReadOnlyList<Dictionary<string, object?>> importRows) =>
        importRows
            .Where(r => r.GetValueOrDefault("ApplicationType") is string type && !string.IsNullOrWhiteSpace(type))
            .GroupBy(r => (string)r["ApplicationType"]!, StringComparer.OrdinalIgnoreCase)
            .Where(g => !ApplicationProfileCatalogPreviewHelper.TryBuild(g.Key, out _))
            .Select(g => new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["ApplicationType"] = g.Key,
                ["ApplicationCount"] = g.Count(),
                ["Note"] = "ApplicationType not in ApplicationTypeConfigurationCatalog.json — fix before tenant JSON.",
            })
            .OrderByDescending(r => (int)(r["ApplicationCount"] ?? 0))
            .ToList();

    private static List<Dictionary<string, object?>> BuildUnmappedTypeRows(
        IReadOnlyList<Dictionary<string, object?>> skippedRows) =>
        skippedRows
            .Where(r => r.GetValueOrDefault("_legacy_ApplicationTypeComposite") is string c && !string.IsNullOrWhiteSpace(c))
            .GroupBy(
                r => (
                    Composite: (string)r["_legacy_ApplicationTypeComposite"]!,
                    Reason: r.GetValueOrDefault("_skipReason") as string ?? string.Empty))
            .Select(g => new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["_legacy_ApplicationTypeComposite"] = g.Key.Composite,
                ["SkippedCount"] = g.Count(),
                ["_skipReason"] = g.Key.Reason,
                ["Note"] = Visa2014ApplicationTransform.IsSkippedApplicationTypeComposite(g.Key.Composite)
                    ? "Known import skip composite (no ApplicationProfile for import wave)."
                    : "Review lookup translation / skip reason.",
            })
            .OrderByDescending(r => (int)(r["SkippedCount"] ?? 0))
            .ToList();

    private static DateTime? ParseApplicationDate(Dictionary<string, object?> row)
    {
        if (row.GetValueOrDefault("ApplicationDate") is not string text || string.IsNullOrWhiteSpace(text))
            return null;

        return DateTime.TryParse(text, out var date) ? date.Date : null;
    }

    private static bool HasNonEmpty(Dictionary<string, object?> row, string key) =>
        row.GetValueOrDefault(key) is string value && !string.IsNullOrWhiteSpace(value);

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
