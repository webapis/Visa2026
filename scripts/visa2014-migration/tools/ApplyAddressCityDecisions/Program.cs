using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using ClosedXML.Excel;

var cli = Parse(Environment.GetCommandLineArgs().Skip(1).ToArray());
var workbook = Req(cli, "--workbook");
var cityJsonPath = Req(cli, "--city-json");
var tenantDir = Req(cli, "--tenant-dir");
var translationsPath = Req(cli, "--translations");
var manifestPath = Req(cli, "--manifest");
var healSqlPath = Req(cli, "--heal-sql");
var fillEmptyKeepBoth = cli.ContainsKey("--fill-empty-keep-both");
var writeWorkbook = cli.GetValueOrDefault("--write-workbook");

using var wb = new XLWorkbook(workbook);
var ws = wb.Worksheet("NearDuplicates");
var header = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
var headerRow = ws.Row(1);
foreach (var cell in headerRow.CellsUsed())
    header[cell.GetString()] = cell.Address.ColumnNumber;

int Col(string name) => header.TryGetValue(name, out var c) ? c : throw new InvalidOperationException($"Missing column {name}");

var merges = new List<Merge>();
var lastRow = ws.LastRowUsed()?.RowNumber() ?? 1;
for (var r = 2; r <= lastRow; r++)
{
    var region = ws.Cell(r, Col("Region")).GetString().Trim();
    var a = ws.Cell(r, Col("NameTm_A")).GetString().Trim();
    var b = ws.Cell(r, Col("NameTm_B")).GetString().Trim();
    var decision = ws.Cell(r, Col("Decision")).GetString().Trim();
    var suggested = ws.Cell(r, Col("SuggestedDecision")).GetString().Trim();
    var confidence = ws.Cell(r, Col("Confidence")).GetString().Trim();

    if (string.IsNullOrWhiteSpace(decision))
    {
        if (string.Equals(confidence, "High", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(suggested))
            decision = suggested;
        else if (fillEmptyKeepBoth)
            decision = "KeepBoth";
        else
            continue;
        ws.Cell(r, Col("Decision")).Value = decision;
        if (string.IsNullOrWhiteSpace(ws.Cell(r, Col("Notes")).GetString()))
            ws.Cell(r, Col("Notes")).Value = fillEmptyKeepBoth && !string.Equals(confidence, "High", StringComparison.OrdinalIgnoreCase)
                ? "Defaulted KeepBoth (non-High)."
                : "Accepted High SuggestedDecision.";
    }

    if (string.Equals(decision, "KeepBoth", StringComparison.OrdinalIgnoreCase))
        continue;

    string keeper, loser;
    if (string.Equals(decision, "MergeAintoB", StringComparison.OrdinalIgnoreCase))
    {
        keeper = b; loser = a;
    }
    else if (string.Equals(decision, "MergeBintoA", StringComparison.OrdinalIgnoreCase))
    {
        keeper = a; loser = b;
    }
    else
    {
        Console.Error.WriteLine($"WRN row {r}: unknown Decision '{decision}' — skipped");
        continue;
    }

    merges.Add(new Merge(region, keeper, loser));
}

// Same loser targeting two keepers (e.g. Gyýanly vs Türkmenbaşy şäheri/etraby) is ambiguous — skip.
var ambiguousLosers = merges
    .GroupBy(m => (m.Region, m.Loser), EqualityComparer<(string, string)>.Default)
    .Where(g => g.Select(x => x.Keeper).Distinct(StringComparer.Ordinal).Count() > 1)
    .Select(g => g.Key)
    .ToHashSet();
if (ambiguousLosers.Count > 0)
{
    foreach (var key in ambiguousLosers)
        Console.Error.WriteLine($"WRN ambiguous loser skipped: [{key.Item1}] '{key.Item2}' (multiple keepers)");
    merges = merges.Where(m => !ambiguousLosers.Contains((m.Region, m.Loser))).ToList();
}

if (!string.IsNullOrWhiteSpace(writeWorkbook))
    wb.SaveAs(writeWorkbook);
else
    wb.Save();

Console.WriteLine($"INF merges to apply: {merges.Count}");
foreach (var m in merges)
    Console.WriteLine($"  [{m.Region}] '{m.Loser}' -> '{m.Keeper}'");

// city.json — remove loser rows
var cityText = File.ReadAllText(cityJsonPath);
var cityDoc = JsonNode.Parse(cityText)!.AsObject();
var rows = cityDoc["rows"]!.AsArray();
var removed = 0;
for (var i = rows.Count - 1; i >= 0; i--)
{
    var name = rows[i]?["NameTm"]?.GetValue<string>() ?? string.Empty;
    var region = rows[i]?["Region"]?.GetValue<string>() ?? string.Empty;
    if (merges.Any(m => m.Loser == name && m.Region == region))
    {
        rows.RemoveAt(i);
        removed++;
    }
}
WriteJson(cityJsonPath, cityDoc);
Console.WriteLine($"INF city.json removed {removed} loser row(s); remaining {rows.Count}");

// tenant site catalogs
foreach (var file in new[]
         {
             "lodging.json", "lodging.calik-energi.json",
             "hotel.json", "hotel.calik-energi.json",
             "hospital.json", "hospital.calik-energi.json",
             "other-site.json", "other-site.calik-energi.json"
         })
{
    var path = Path.Combine(tenantDir, file);
    if (!File.Exists(path)) continue;
    var doc = JsonNode.Parse(File.ReadAllText(path))!.AsObject();
    var siteRows = doc["rows"]?.AsArray();
    if (siteRows is null) continue;
    var changed = 0;
    foreach (var row in siteRows)
    {
        if (row is null) continue;
        var city = row["City"]?.GetValue<string>();
        var region = row["Region"]?.GetValue<string>();
        if (city is null || region is null) continue;
        var m = merges.FirstOrDefault(x => x.Loser == city && x.Region == region);
        if (m is null) continue;
        row["City"] = m.Keeper;
        changed++;
    }
    if (changed > 0)
    {
        WriteJson(path, doc);
        Console.WriteLine($"INF {file}: retargeted City on {changed} row(s)");
    }
}

// CityByName translations — add loser -> keeper aliases
if (File.Exists(translationsPath))
{
    var yaml = File.ReadAllText(translationsPath);
    var blockMarker = "targetCatalog: CityByName";
    var idx = yaml.IndexOf(blockMarker, StringComparison.Ordinal);
    if (idx < 0)
        Console.Error.WriteLine("WRN CityByName block not found in translations");
    else
    {
        var insertAt = yaml.IndexOf("values:", idx, StringComparison.Ordinal);
        if (insertAt < 0)
            Console.Error.WriteLine("WRN CityByName values: not found");
        else
        {
            // find end of values list indentation - append after values: line
            var lineEnd = yaml.IndexOf('\n', insertAt);
            var sb = new StringBuilder();
            foreach (var m in merges)
            {
                // legacy often ASCII-ish; emit both loser NameTm and a folded form
                sb.AppendLine($"      - legacy: {YamlEscape(m.Loser)}");
                sb.AppendLine($"        target: {YamlEscape(m.Keeper)}");
            }
            yaml = yaml.Insert(lineEnd + 1, sb.ToString());
            File.WriteAllText(translationsPath, yaml, new UTF8Encoding(false));
            Console.WriteLine($"INF translations: appended {merges.Count} CityByName alias pair(s)");
        }
    }
}

// bump manifest version only when catalog/translations actually changed
if (merges.Count > 0 && File.Exists(manifestPath))
{
    var man = JsonNode.Parse(File.ReadAllText(manifestPath))!.AsObject();
    var ver = man["version"]?.GetValue<int>() ?? 0;
    man["version"] = ver + 1;
    WriteJson(manifestPath, man);
    Console.WriteLine($"INF manifest version {ver} -> {ver + 1}");
}
else if (merges.Count == 0)
{
    Console.WriteLine("INF manifest unchanged (0 merges)");
}

// heal SQL for prod
var sql = new StringBuilder();
sql.AppendLine("-- Address City near-duplicate heal (generated)");
sql.AppendLine("BEGIN;");
foreach (var m in merges)
{
    var k = Esc(m.Keeper);
    var l = Esc(m.Loser);
    var reg = Esc(m.Region);
    sql.AppendLine($@"
-- {m.Region}: {m.Loser} -> {m.Keeper}
-- Revive keeper if previously soft-deleted; match cities by NameTm + region (RegionID or RegionName)
UPDATE ""Cities"" k SET ""GCRecord"" = 0, ""RegionID"" = r.""ID"", ""RegionName"" = r.""NameTm""
FROM ""Regions"" r
WHERE k.""NameTm"" = '{k}' AND r.""NameTm"" = '{reg}'
  AND (k.""RegionID"" = r.""ID"" OR k.""RegionID"" IS NULL OR k.""RegionName"" = r.""NameTm"" OR k.""RegionName"" IS NULL OR k.""RegionName"" = '');

UPDATE ""Lodgings"" t SET ""CityID"" = k.""ID""
FROM ""Cities"" k, ""Cities"" l, ""Regions"" r
WHERE k.""NameTm"" = '{k}' AND l.""NameTm"" = '{l}' AND r.""NameTm"" = '{reg}'
  AND (k.""RegionID"" = r.""ID"" OR k.""RegionName"" = r.""NameTm"")
  AND (l.""RegionID"" = r.""ID"" OR l.""RegionName"" = r.""NameTm"" OR l.""RegionID"" IS NULL)
  AND t.""CityID"" = l.""ID"";
UPDATE ""Hotels"" t SET ""CityID"" = k.""ID""
FROM ""Cities"" k, ""Cities"" l, ""Regions"" r
WHERE k.""NameTm"" = '{k}' AND l.""NameTm"" = '{l}' AND r.""NameTm"" = '{reg}'
  AND (k.""RegionID"" = r.""ID"" OR k.""RegionName"" = r.""NameTm"")
  AND (l.""RegionID"" = r.""ID"" OR l.""RegionName"" = r.""NameTm"" OR l.""RegionID"" IS NULL)
  AND t.""CityID"" = l.""ID"";
UPDATE ""Hospitals"" t SET ""CityID"" = k.""ID""
FROM ""Cities"" k, ""Cities"" l, ""Regions"" r
WHERE k.""NameTm"" = '{k}' AND l.""NameTm"" = '{l}' AND r.""NameTm"" = '{reg}'
  AND (k.""RegionID"" = r.""ID"" OR k.""RegionName"" = r.""NameTm"")
  AND (l.""RegionID"" = r.""ID"" OR l.""RegionName"" = r.""NameTm"" OR l.""RegionID"" IS NULL)
  AND t.""CityID"" = l.""ID"";
UPDATE ""OtherSites"" t SET ""CityID"" = k.""ID""
FROM ""Cities"" k, ""Cities"" l, ""Regions"" r
WHERE k.""NameTm"" = '{k}' AND l.""NameTm"" = '{l}' AND r.""NameTm"" = '{reg}'
  AND (k.""RegionID"" = r.""ID"" OR k.""RegionName"" = r.""NameTm"")
  AND (l.""RegionID"" = r.""ID"" OR l.""RegionName"" = r.""NameTm"" OR l.""RegionID"" IS NULL)
  AND t.""CityID"" = l.""ID"";
UPDATE ""AddressesOfResidence"" t SET ""CityID"" = k.""ID""
FROM ""Cities"" k, ""Cities"" l, ""Regions"" r
WHERE k.""NameTm"" = '{k}' AND l.""NameTm"" = '{l}' AND r.""NameTm"" = '{reg}'
  AND (k.""RegionID"" = r.""ID"" OR k.""RegionName"" = r.""NameTm"")
  AND (l.""RegionID"" = r.""ID"" OR l.""RegionName"" = r.""NameTm"" OR l.""RegionID"" IS NULL)
  AND t.""CityID"" = l.""ID"";
UPDATE ""Cities"" l SET ""GCRecord"" = 999002
FROM ""Cities"" k, ""Regions"" r
WHERE k.""NameTm"" = '{k}' AND l.""NameTm"" = '{l}' AND r.""NameTm"" = '{reg}'
  AND (k.""RegionID"" = r.""ID"" OR k.""RegionName"" = r.""NameTm"")
  AND (l.""RegionID"" = r.""ID"" OR l.""RegionName"" = r.""NameTm"" OR l.""RegionID"" IS NULL)
  AND l.""ID"" <> k.""ID"";
");
}
sql.AppendLine("COMMIT;");
File.WriteAllText(healSqlPath, sql.ToString(), new UTF8Encoding(false));
Console.WriteLine($"OK heal SQL -> {healSqlPath}");
Console.WriteLine("APPLY_COMPLETE");

static void WriteJson(string path, JsonNode node)
{
    var opts = new JsonSerializerOptions { WriteIndented = true };
    File.WriteAllText(path, node.ToJsonString(opts) + Environment.NewLine, new UTF8Encoding(false));
}

static string Esc(string s) => s.Replace("'", "''");
static string YamlEscape(string s) =>
    s.Contains(':') || s.Contains('#') || s.Contains("'") || s.Contains('"')
        ? "\"" + s.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\""
        : s;

static Dictionary<string, string> Parse(string[] argv)
{
    var d = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    for (var i = 0; i < argv.Length; i++)
    {
        if (!argv[i].StartsWith("--", StringComparison.Ordinal)) continue;
        var key = argv[i];
        var val = i + 1 < argv.Length && !argv[i + 1].StartsWith("--", StringComparison.Ordinal) ? argv[++i] : "true";
        d[key] = val;
    }
    return d;
}

static string Req(Dictionary<string, string> d, string key) =>
    d.TryGetValue(key, out var v) && !string.IsNullOrWhiteSpace(v) ? v : throw new ArgumentException($"Missing {key}");

sealed record Merge(string Region, string Keeper, string Loser);
