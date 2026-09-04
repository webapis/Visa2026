using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.DataImporter.Legacy.Visa2014;

/// <summary>
/// Phase A — frequency matrix ApplicationType × ApprovalLegProfileCode from VISA2015,
/// then seed JSON for per-profile ApplicationProfileApprovalLegVersion copies.
/// </summary>
internal static class Visa2014ApplicationProfileApprovalLegVersionMatrixExporter
{
    private const string FallbackApprovalLegProfileCode = "TE-EN";

    private static readonly JsonSerializerOptions JsonWriteOptions = new()
    {
        WriteIndented = true,
    };

    public sealed record MatrixCell(
        string ApplicationTypeName,
        string ProfileCode,
        string ProfileName,
        string ApprovalLegProfileCode,
        string ApprovalLegProfileNameTm,
        int AppCount,
        bool IsDefault);

    public sealed record ExportResult(
        IReadOnlyList<MatrixCell> Cells,
        IReadOnlyList<string> ViaProfilesWithoutLegacyApps,
        int AppsScanned,
        int AppsMapped,
        int AppsSkippedType,
        int AppsNoProfileCode,
        string SeedJsonPath,
        string MatrixMarkdownPath);

    public static ExportResult Export(
        string connectionString,
        IReadOnlyList<string> lookupTranslationPaths,
        string applicationProfileCatalogPath,
        string approvalLegProfileCatalogPath,
        string seedJsonPath,
        string matrixMarkdownPath,
        int? maxRows,
        bool verbose)
    {
        var catalogs = Visa2014LookupTranslator.Load(lookupTranslationPaths);
        var viaProfiles = LoadViaMinistryProfiles(applicationProfileCatalogPath);
        var legProfiles = LoadApprovalLegProfiles(approvalLegProfileCatalogPath);

        var sql = maxRows is > 0
            ? $"SELECT TOP ({maxRows.Value}) * FROM ({Visa2014ApplicationTransform.ExtractSql}) AS q"
            : Visa2014ApplicationTransform.ExtractSql;

        var dictRows = Visa2014SqlCmdReader.Query(connectionString, sql, verbose);
        var counts = new Dictionary<(string TypeName, string LegCode), int>();
        var appsScanned = 0;
        var appsMapped = 0;
        var appsSkippedType = 0;
        var appsNoProfileCode = 0;

        foreach (var dict in dictRows)
        {
            appsScanned++;
            if (!Visa2014ApplicationTransform.TryParseRawRow(dict, out var raw))
                continue;

            var composite = Visa2014ApplicationTransform.BuildApplicationTypeComposite(raw);
            if (Visa2014ApplicationTransform.IsSkippedApplicationTypeComposite(composite))
            {
                appsSkippedType++;
                continue;
            }

            if (!Visa2014LookupTranslator.TryTranslate(
                    catalogs, "ApplicationType", composite, out var typeName, out _)
                || string.IsNullOrWhiteSpace(typeName))
            {
                appsSkippedType++;
                continue;
            }

            if (!viaProfiles.ContainsKey(typeName))
                continue;

            var legCode = Visa2014ApplicationApprovalLegProfileInference.ResolveProfileCode(raw);
            if (string.IsNullOrWhiteSpace(legCode))
            {
                appsNoProfileCode++;
                continue;
            }

            appsMapped++;
            var key = (typeName, legCode);
            counts[key] = counts.TryGetValue(key, out var n) ? n + 1 : 1;
        }

        var cells = new List<MatrixCell>();
        var viaWithoutLegacy = new List<string>();

        foreach (var profile in viaProfiles.Values.OrderBy(p => p.ApplicationTypeName, StringComparer.Ordinal))
        {
            var typeCounts = counts
                .Where(kv => string.Equals(kv.Key.TypeName, profile.ApplicationTypeName, StringComparison.Ordinal))
                .OrderByDescending(kv => kv.Value)
                .ThenBy(kv => kv.Key.LegCode, StringComparer.Ordinal)
                .ToList();

            if (typeCounts.Count == 0)
            {
                viaWithoutLegacy.Add(profile.ApplicationTypeName);
                if (!legProfiles.TryGetValue(FallbackApprovalLegProfileCode, out var fallback))
                    continue;

                cells.Add(new MatrixCell(
                    profile.ApplicationTypeName,
                    profile.Code,
                    profile.Name,
                    FallbackApprovalLegProfileCode,
                    fallback.NameTm,
                    AppCount: 0,
                    IsDefault: true));
                continue;
            }

            var defaultCode = typeCounts[0].Key.LegCode;
            foreach (var entry in typeCounts)
            {
                var legCode = entry.Key.LegCode;
                var appCount = entry.Value;
                var nameTm = legProfiles.TryGetValue(legCode, out var lp)
                    ? lp.NameTm
                    : legCode;
                cells.Add(new MatrixCell(
                    profile.ApplicationTypeName,
                    profile.Code,
                    profile.Name,
                    legCode,
                    nameTm,
                    appCount,
                    IsDefault: string.Equals(legCode, defaultCode, StringComparison.Ordinal)));
            }
        }

        WriteSeedJson(seedJsonPath, cells, viaProfiles);
        WriteMatrixMarkdown(
            matrixMarkdownPath,
            cells,
            viaWithoutLegacy,
            appsScanned,
            appsMapped,
            appsSkippedType,
            appsNoProfileCode);

        return new ExportResult(
            cells,
            viaWithoutLegacy,
            appsScanned,
            appsMapped,
            appsSkippedType,
            appsNoProfileCode,
            Path.GetFullPath(seedJsonPath),
            Path.GetFullPath(matrixMarkdownPath));
    }

        private static JsonArray RequireRowsArray(JsonObject root, string path)
    {
        var rows = root["Rows"] as JsonArray ?? root["rows"] as JsonArray;
        if (rows == null)
            throw new InvalidOperationException($"Expected Rows array in {path}");
        return rows;
    }
    private sealed class ProfileRow
    {
        public string ApplicationTypeName { get; init; } = "";
        public string Code { get; init; } = "";
        public string Name { get; init; } = "";
    }

    private sealed class LegProfileRow
    {
        public string Code { get; init; } = "";
        public string NameTm { get; init; } = "";
    }

    private static Dictionary<string, ProfileRow> LoadViaMinistryProfiles(string path)
    {
        var json = File.ReadAllText(path, Encoding.UTF8);
        var root = JsonNode.Parse(json) as JsonObject
            ?? throw new InvalidOperationException($"Expected object root in {path}");
        var rows = RequireRowsArray(root, path);

        var map = new Dictionary<string, ProfileRow>(StringComparer.Ordinal);
        foreach (var node in rows.OfType<JsonObject>())
        {
            var route = node["ProgressRoute"]?.GetValue<string>() ?? "";
            if (!string.Equals(
                    route,
                    nameof(ApplicationProfileInstanceProgressRouteKind.ViaMinistries),
                    StringComparison.OrdinalIgnoreCase))
                continue;

            var typeName = node["ApplicationTypeName"]?.GetValue<string>()?.Trim() ?? "";
            var code = node["Code"]?.GetValue<string>()?.Trim() ?? "";
            var name = node["Name"]?.GetValue<string>()?.Trim() ?? code;
            if (string.IsNullOrWhiteSpace(typeName) || string.IsNullOrWhiteSpace(code))
                continue;

            map[typeName] = new ProfileRow
            {
                ApplicationTypeName = typeName,
                Code = code,
                Name = name,
            };
        }

        return map;
    }

    private static Dictionary<string, LegProfileRow> LoadApprovalLegProfiles(string path)
    {
        var json = File.ReadAllText(path, Encoding.UTF8);
        var root = JsonNode.Parse(json) as JsonObject
            ?? throw new InvalidOperationException($"Expected object root in {path}");
        var rows = RequireRowsArray(root, path);

        var map = new Dictionary<string, LegProfileRow>(StringComparer.OrdinalIgnoreCase);
        foreach (var node in rows.OfType<JsonObject>())
        {
            var code = node["Code"]?.GetValue<string>()?.Trim() ?? "";
            if (string.IsNullOrWhiteSpace(code))
                continue;

            var nameTm = node["NameTm"]?.GetValue<string>()?.Trim() ?? code;
            map[code] = new LegProfileRow { Code = code, NameTm = nameTm };
        }

        return map;
    }

    private static void WriteSeedJson(
        string path,
        IReadOnlyList<MatrixCell> cells,
        IReadOnlyDictionary<string, ProfileRow> viaProfiles)
    {
        var rows = new JsonArray();
        foreach (var group in cells.GroupBy(c => c.ApplicationTypeName, StringComparer.Ordinal)
                     .OrderBy(g => g.Key, StringComparer.Ordinal))
        {
            if (!viaProfiles.TryGetValue(group.Key, out var profile))
                continue;

            var versions = new JsonArray();
            var seq = 1;
            foreach (var cell in group
                         .OrderByDescending(c => c.IsDefault)
                         .ThenByDescending(c => c.AppCount)
                         .ThenBy(c => c.ApprovalLegProfileCode, StringComparer.Ordinal))
            {
                versions.Add(new JsonObject
                {
                    ["Name"] = cell.ApprovalLegProfileNameTm,
                    ["ApprovalLegProfileCode"] = cell.ApprovalLegProfileCode,
                    ["IsDefault"] = cell.IsDefault,
                    ["Sequence"] = seq++,
                    ["SourceAppCount"] = cell.AppCount,
                });
            }

            rows.Add(new JsonObject
            {
                ["ApplicationTypeName"] = profile.ApplicationTypeName,
                ["ProfileCode"] = profile.Code,
                ["ProfileName"] = profile.Name,
                ["SignOff"] = "approved",
                ["Versions"] = versions,
            });
        }

        var output = new JsonObject { ["rows"] = rows };
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        File.WriteAllText(
            path,
            output.ToJsonString(JsonWriteOptions),
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    private static void WriteMatrixMarkdown(
        string path,
        IReadOnlyList<MatrixCell> cells,
        IReadOnlyList<string> viaWithoutLegacy,
        int appsScanned,
        int appsMapped,
        int appsSkippedType,
        int appsNoProfileCode)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Application Profile — Approval leg version frequency (Çalik Energi)");
        sb.AppendLine();
        sb.AppendLine("**Source:** VISA2015 via `Visa2014ApplicationApprovalLegProfileInference` + ApplicationType lookup-translations.");
        sb.AppendLine("**Scope:** Via-ministry type-only profiles in `application-profile.calik-energi.json`.");
        sb.AppendLine("**Seed:** `application-profile-approval-leg-versions.calik-energi.json` (copies legs from `approval-leg-profile.json`).");
        sb.AppendLine();
        sb.AppendLine($"Generated: {DateTime.UtcNow:yyyy-MM-dd} UTC");
        sb.AppendLine();
        sb.AppendLine("## Totals");
        sb.AppendLine();
        sb.AppendLine("| Metric | Count |");
        sb.AppendLine("|--------|------:|");
        sb.AppendLine($"| Apps scanned | {appsScanned} |");
        sb.AppendLine($"| Apps mapped (via-ministry type + leg code) | {appsMapped} |");
        sb.AppendLine($"| Apps skipped (ApplicationType) | {appsSkippedType} |");
        sb.AppendLine($"| Apps with no leg code | {appsNoProfileCode} |");
        sb.AppendLine();
        sb.AppendLine("## Matrix");
        sb.AppendLine();
        sb.AppendLine("| ApplicationType | Profile Code | ApprovalLegProfile | Apps | Default |");
        sb.AppendLine("|-----------------|--------------|--------------------|-----:|:-------:|");
        foreach (var cell in cells
                     .OrderBy(c => c.ApplicationTypeName, StringComparer.Ordinal)
                     .ThenByDescending(c => c.AppCount)
                     .ThenBy(c => c.ApprovalLegProfileCode, StringComparer.Ordinal))
        {
            var def = cell.IsDefault ? "yes" : "";
            sb.AppendLine(
                $"| `{cell.ApplicationTypeName}` | `{cell.ProfileCode}` | `{cell.ApprovalLegProfileCode}` ({cell.ApprovalLegProfileNameTm}) | {cell.AppCount} | {def} |");
        }

        if (viaWithoutLegacy.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("## Via-ministry profiles with no legacy apps");
            sb.AppendLine();
            sb.AppendLine("Seeded fallback **TE-EN** (Default). Officer should confirm or add versions.");
            sb.AppendLine();
            foreach (var name in viaWithoutLegacy.OrderBy(s => s, StringComparer.Ordinal))
                sb.AppendLine($"- `{name}`");
        }

        sb.AppendLine();
        sb.AppendLine("## Review");
        sb.AppendLine();
        sb.AppendLine("1. Confirm **Default** per profile (most frequent chain).");
        sb.AppendLine("2. F5 → Configure profile → Identity → Approval leg versions.");
        sb.AppendLine("3. Phase B: imported instances keep inferred ApprovalLegProfile; F5 / snapshot backfill fills snapshots.");
        sb.AppendLine();

        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        File.WriteAllText(path, sb.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }
}