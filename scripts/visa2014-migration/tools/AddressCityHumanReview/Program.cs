using System.Globalization;
using System.Text;
using System.Text.Json;
using ClosedXML.Excel;

static string Norm(string? s)
{
    if (string.IsNullOrWhiteSpace(s)) return string.Empty;
    var formD = s.Trim().Normalize(NormalizationForm.FormD);
    var sb = new StringBuilder(formD.Length);
    foreach (var ch in formD)
    {
        var cat = CharUnicodeInfo.GetUnicodeCategory(ch);
        if (cat == UnicodeCategory.NonSpacingMark) continue;
        var c = char.ToLowerInvariant(ch);
        c = c switch
        {
            'ý' or 'ÿ' => 'y',
            'ş' or 'ș' => 's',
            'ç' => 'c',
            'ň' or 'ñ' => 'n',
            'ž' or 'ż' => 'z',
            'ä' or 'á' or 'à' => 'a',
            'ö' or 'ó' => 'o',
            'ü' or 'ú' => 'u',
            'ı' => 'i',
            _ => c
        };
        if (c is >= 'a' and <= 'z' or >= '0' and <= '9')
            sb.Append(c);
    }
    var n = sb.ToString();
    foreach (var suffix in new[] { "etraby", "saheri", "obasy", "sehercesi" })
    {
        if (n.EndsWith(suffix, StringComparison.Ordinal) && n.Length > suffix.Length + 2)
            n = n[..^suffix.Length];
    }
    return n;
}

static int Levenshtein(string a, string b)
{
    var n = a.Length;
    var m = b.Length;
    var d = new int[n + 1, m + 1];
    for (var i = 0; i <= n; i++) d[i, 0] = i;
    for (var j = 0; j <= m; j++) d[0, j] = j;
    for (var i = 1; i <= n; i++)
    for (var j = 1; j <= m; j++)
    {
        var cost = a[i - 1] == b[j - 1] ? 0 : 1;
        d[i, j] = Math.Min(Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + cost);
    }
    return d[n, m];
}

static bool IsNearDuplicate(string na, string nb, out string reason)
{
    reason = string.Empty;
    if (na.Length < 3 || nb.Length < 3) return false;
    if (na == nb)
    {
        reason = "same_normalized";
        return true;
    }
    var shorter = na.Length <= nb.Length ? na : nb;
    var longer = na.Length <= nb.Length ? nb : na;
    if (longer.Contains(shorter, StringComparison.Ordinal) && shorter.Length >= 4)
    {
        reason = "containment";
        return true;
    }
    var maxLen = Math.Max(na.Length, nb.Length);
    var dist = Levenshtein(na, nb);
    if (maxLen >= 6 && dist <= Math.Max(2, maxLen / 5))
    {
        reason = $"levenshtein:{dist}";
        return true;
    }
    return false;
}

static Dictionary<string, JsonElement>[] LoadRows(string path)
{
    using var doc = JsonDocument.Parse(File.ReadAllText(path));
    if (!doc.RootElement.TryGetProperty("rows", out var rows))
        return Array.Empty<Dictionary<string, JsonElement>>();
    return rows.EnumerateArray()
        .Select(e => e.EnumerateObject().ToDictionary(p => p.Name, p => p.Value.Clone()))
        .ToArray();
}

static string? GetStr(Dictionary<string, JsonElement> row, string key) =>
    row.TryGetValue(key, out var el) && el.ValueKind == JsonValueKind.String ? el.GetString() : null;

var cli = CliArgs.Parse(Environment.GetCommandLineArgs().Skip(1).ToArray());
var cityPath = cli.Require("--city-json");
var lodgingPath = cli.Get("--lodging-json");
var hotelPath = cli.Get("--hotel-json");
var hospitalPath = cli.Get("--hospital-json");
var otherPath = cli.Get("--other-site-json");
var usagePath = cli.Get("--prod-usage-csv");
var outputPath = cli.Require("--output");

var cities = LoadRows(cityPath)
    .Select((r, i) => new CityRow(
        i,
        GetStr(r, "NameTm") ?? string.Empty,
        GetStr(r, "Region") ?? string.Empty,
        GetStr(r, "PdfForm_Code"),
        Norm(GetStr(r, "NameTm"))))
    .Where(c => c.NameTm.Length > 0)
    .ToList();

var usage = new Dictionary<(string Region, string NameTm), UsageRow>(new RegionNameComparer());
if (!string.IsNullOrWhiteSpace(usagePath) && File.Exists(usagePath))
{
    foreach (var line in File.ReadLines(usagePath).Skip(1))
    {
        if (string.IsNullOrWhiteSpace(line)) continue;
        var p = SplitCsv(line);
        if (p.Length < 7) continue;
        usage[(p[0], p[1])] = new UsageRow(
            ParseInt(p[2]), ParseInt(p[3]), ParseInt(p[4]), ParseInt(p[5]),
            string.Equals(p[6], "1", StringComparison.OrdinalIgnoreCase)
            || string.Equals(p[6], "true", StringComparison.OrdinalIgnoreCase)
            || string.Equals(p[6], "Y", StringComparison.OrdinalIgnoreCase));
    }
}

UsageRow U(CityRow c) =>
    usage.TryGetValue((c.Region, c.NameTm), out var u) ? u : UsageRow.Zero;

var lodgingRows = string.IsNullOrWhiteSpace(lodgingPath) || !File.Exists(lodgingPath)
    ? Array.Empty<Dictionary<string, JsonElement>>()
    : LoadRows(lodgingPath);
var hotelRows = string.IsNullOrWhiteSpace(hotelPath) || !File.Exists(hotelPath)
    ? Array.Empty<Dictionary<string, JsonElement>>()
    : LoadRows(hotelPath);
var hospitalRows = string.IsNullOrWhiteSpace(hospitalPath) || !File.Exists(hospitalPath)
    ? Array.Empty<Dictionary<string, JsonElement>>()
    : LoadRows(hospitalPath);
var otherRows = string.IsNullOrWhiteSpace(otherPath) || !File.Exists(otherPath)
    ? Array.Empty<Dictionary<string, JsonElement>>()
    : LoadRows(otherPath);

int CountSite(IEnumerable<Dictionary<string, JsonElement>> rows, string cityName, string region) =>
    rows.Count(r =>
        string.Equals(GetStr(r, "City"), cityName, StringComparison.Ordinal)
        && string.Equals(GetStr(r, "Region"), region, StringComparison.Ordinal));

var pairs = new List<NearDup>();
foreach (var group in cities.GroupBy(c => c.Region, StringComparer.Ordinal))
{
    var list = group.ToList();
    for (var i = 0; i < list.Count; i++)
    for (var j = i + 1; j < list.Count; j++)
    {
        var a = list[i];
        var b = list[j];
        if (!IsNearDuplicate(a.Norm, b.Norm, out var reason)) continue;

        var ua = U(a);
        var ub = U(b);
        var scoreA = ScoreKeeper(a, ua);
        var scoreB = ScoreKeeper(b, ub);
        var keeperIsA = scoreA >= scoreB;
        var suggestedKeeper = keeperIsA ? a.NameTm : b.NameTm;
        var suggestedDecision = keeperIsA ? "MergeBintoA" : "MergeAintoB";
        var confidence = Confidence(a, b, reason, ua, ub);

        // High confidence: one has PdfForm_Code, other does not, containment/same stem
        var decision = confidence == "High" ? suggestedDecision : string.Empty;

        pairs.Add(new NearDup(
            a.Region,
            a.NameTm,
            b.NameTm,
            a.PdfFormCode ?? string.Empty,
            b.PdfFormCode ?? string.Empty,
            ua.Lodging + CountSite(lodgingRows, a.NameTm, a.Region),
            ub.Lodging + CountSite(lodgingRows, b.NameTm, b.Region),
            ua.Hotel + CountSite(hotelRows, a.NameTm, a.Region),
            ub.Hotel + CountSite(hotelRows, b.NameTm, b.Region),
            ua.AoR,
            ub.AoR,
            suggestedKeeper,
            suggestedDecision,
            confidence,
            decision,
            reason));
    }
}

Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outputPath))!);
using var wb = new XLWorkbook();

var wsDup = wb.Worksheets.Add("NearDuplicates");
WriteHeader(wsDup, new[]
{
    "Region", "NameTm_A", "NameTm_B", "PdfForm_Code_A", "PdfForm_Code_B",
    "LodgingCount_A", "LodgingCount_B", "HotelCount_A", "HotelCount_B",
    "AoRCount_A", "AoRCount_B", "SuggestedKeeper", "SuggestedDecision", "Confidence",
    "Decision", "MatchReason", "Notes"
});
var r = 2;
foreach (var p in pairs.OrderBy(p => p.Region).ThenBy(p => p.NameTmA))
{
    wsDup.Cell(r, 1).Value = p.Region;
    wsDup.Cell(r, 2).Value = p.NameTmA;
    wsDup.Cell(r, 3).Value = p.NameTmB;
    wsDup.Cell(r, 4).Value = p.CodeA;
    wsDup.Cell(r, 5).Value = p.CodeB;
    wsDup.Cell(r, 6).Value = p.LodgingA;
    wsDup.Cell(r, 7).Value = p.LodgingB;
    wsDup.Cell(r, 8).Value = p.HotelA;
    wsDup.Cell(r, 9).Value = p.HotelB;
    wsDup.Cell(r, 10).Value = p.AoRA;
    wsDup.Cell(r, 11).Value = p.AoRB;
    wsDup.Cell(r, 12).Value = p.SuggestedKeeper;
    wsDup.Cell(r, 13).Value = p.SuggestedDecision;
    wsDup.Cell(r, 14).Value = p.Confidence;
    wsDup.Cell(r, 15).Value = p.Decision;
    wsDup.Cell(r, 16).Value = p.Reason;
    wsDup.Cell(r, 17).Value = p.Confidence == "High"
        ? "Auto-filled Decision (prefer PdfForm_Code keeper). Change to KeepBoth if wrong."
        : "Fill Decision: KeepBoth | MergeAintoB | MergeBintoA";
    if (p.Confidence == "High")
        wsDup.Range(r, 1, r, 17).Style.Fill.BackgroundColor = XLColor.FromHtml("#E8F5E9");
    else if (p.Confidence == "Medium")
        wsDup.Range(r, 1, r, 17).Style.Fill.BackgroundColor = XLColor.FromHtml("#FFF8E1");
    r++;
}
wsDup.SheetView.FreezeRows(1);
wsDup.Columns().AdjustToContents(1, 40);

var wsCity = wb.Worksheets.Add("CityCatalog");
WriteHeader(wsCity, new[]
{
    "Region", "NameTm", "PdfForm_Code", "Normalized", "ProdRegionLinked",
    "AoRCount", "LodgingCount", "HotelCount", "HospitalCount",
    "CatalogLodgingRefs", "CatalogHotelRefs", "CatalogHospitalRefs", "CatalogOtherSiteRefs",
    "NearDupPartnerCount"
});
r = 2;
var partnerCounts = pairs
    .SelectMany(p => new[] { (p.Region, p.NameTmA), (p.Region, p.NameTmB) })
    .GroupBy(x => x)
    .ToDictionary(g => g.Key, g => g.Count());
foreach (var c in cities.OrderBy(c => c.Region).ThenBy(c => c.NameTm))
{
    var u = U(c);
    wsCity.Cell(r, 1).Value = c.Region;
    wsCity.Cell(r, 2).Value = c.NameTm;
    wsCity.Cell(r, 3).Value = c.PdfFormCode ?? string.Empty;
    wsCity.Cell(r, 4).Value = c.Norm;
    wsCity.Cell(r, 5).Value = u.RegionLinked ? "Y" : (usage.Count == 0 ? "" : "N");
    wsCity.Cell(r, 6).Value = u.AoR;
    wsCity.Cell(r, 7).Value = u.Lodging;
    wsCity.Cell(r, 8).Value = u.Hotel;
    wsCity.Cell(r, 9).Value = u.Hospital;
    wsCity.Cell(r, 10).Value = CountSite(lodgingRows, c.NameTm, c.Region);
    wsCity.Cell(r, 11).Value = CountSite(hotelRows, c.NameTm, c.Region);
    wsCity.Cell(r, 12).Value = CountSite(hospitalRows, c.NameTm, c.Region);
    wsCity.Cell(r, 13).Value = CountSite(otherRows, c.NameTm, c.Region);
    wsCity.Cell(r, 14).Value = partnerCounts.TryGetValue((c.Region, c.NameTm), out var pc) ? pc : 0;
    r++;
}
wsCity.SheetView.FreezeRows(1);
wsCity.Columns().AdjustToContents(1, 40);

var wsLodging = wb.Worksheets.Add("LodgingCityRefs");
WriteHeader(wsLodging, new[]
{
    "Region", "City", "FullAddress", "CityExactMatchCount", "CityNearDupInRegion", "Notes"
});
r = 2;
foreach (var row in lodgingRows)
{
    var region = GetStr(row, "Region") ?? string.Empty;
    var city = GetStr(row, "City") ?? string.Empty;
    var addr = GetStr(row, "FullAddress") ?? string.Empty;
    var exact = cities.Count(c => c.Region == region && c.NameTm == city);
    var near = pairs.Any(p => p.Region == region && (p.NameTmA == city || p.NameTmB == city));
    wsLodging.Cell(r, 1).Value = region;
    wsLodging.Cell(r, 2).Value = city;
    wsLodging.Cell(r, 3).Value = addr;
    wsLodging.Cell(r, 4).Value = exact;
    wsLodging.Cell(r, 5).Value = near ? "Y" : "N";
    wsLodging.Cell(r, 6).Value = exact == 0 ? "MISSING city.json row"
        : exact > 1 ? "DUPLICATE exact NameTm"
        : near ? "City is in a near-duplicate pair — verify keeper"
        : "OK";
    if (exact != 1 || near)
        wsLodging.Range(r, 1, r, 6).Style.Fill.BackgroundColor = XLColor.FromHtml("#FFEBEE");
    r++;
}
wsLodging.SheetView.FreezeRows(1);
wsLodging.Columns().AdjustToContents(1, 60);

var wsReadme = wb.Worksheets.Add("README");
wsReadme.Cell(1, 1).Value = "Address City human review";
wsReadme.Cell(2, 1).Value = "1. NearDuplicates: green=High confidence Decision auto-filled; yellow=Medium — fill Decision.";
wsReadme.Cell(3, 1).Value = "2. Decision values: KeepBoth | MergeAintoB | MergeBintoA";
wsReadme.Cell(4, 1).Value = "3. SuggestedKeeper prefers PdfForm_Code, then higher AoR/Lodging usage, then longer official name.";
wsReadme.Cell(5, 1).Value = "4. After marking, run Apply-AddressCityHumanReviewDecisions.ps1 (or ask agent to apply).";
wsReadme.Cell(6, 1).Value = $"GeneratedUtc: {DateTime.UtcNow:o}";
wsReadme.Cell(7, 1).Value = $"City rows: {cities.Count}; Near-dup pairs: {pairs.Count}; High auto Decision: {pairs.Count(p => p.Decision.Length > 0)}";
wsReadme.Column(1).Width = 120;

wb.SaveAs(outputPath);
Console.WriteLine($"OK wrote {outputPath}");
Console.WriteLine($"INF cities={cities.Count} nearDupPairs={pairs.Count} highAuto={pairs.Count(p => p.Decision.Length > 0)}");

static int ScoreKeeper(CityRow c, UsageRow u)
{
    var score = 0;
    if (!string.IsNullOrWhiteSpace(c.PdfFormCode)) score += 100;
    if (c.NameTm.Contains("etraby", StringComparison.OrdinalIgnoreCase)
        || c.NameTm.Contains("şäheri", StringComparison.OrdinalIgnoreCase)
        || c.NameTm.Contains("saheri", StringComparison.OrdinalIgnoreCase))
        score += 20;
    score += Math.Min(50, u.AoR / 10);
    score += Math.Min(20, u.Lodging * 2);
    score += c.NameTm.Length;
    return score;
}

static string Confidence(CityRow a, CityRow b, string reason, UsageRow ua, UsageRow ub)
{
    // Distinct district vs city seats sharing a stem (Baharly etraby vs Baharly şäheri) must stay KeepBoth.
    if (IsEtrabySaheriSiblingPair(a.NameTm, b.NameTm))
        return "Medium";

    var oneCode = string.IsNullOrWhiteSpace(a.PdfFormCode) ^ string.IsNullOrWhiteSpace(b.PdfFormCode);
    var longPrefixNoise = HasAdminPrefixNoise(a.NameTm) || HasAdminPrefixNoise(b.NameTm);
    var typoOrShort =
        reason is "same_normalized"
        || (reason is "containment" && (IsShortAlias(a.NameTm, b.NameTm) || IsShortAlias(b.NameTm, a.NameTm)))
        || (reason.StartsWith("levenshtein", StringComparison.Ordinal) && !IsEtrabySaheriSiblingPair(a.NameTm, b.NameTm));

    // Long admin-prefix labels are only Medium here; Apply skips losers with multiple keepers.
    if (oneCode && typoOrShort)
        return "High";
    if (reason is "containment" or "same_normalized" || longPrefixNoise || reason.StartsWith("levenshtein", StringComparison.Ordinal))
        return "Medium";
    return "Low";
}

static bool HasAdminPrefixNoise(string name) =>
    name.Contains("welaýatynyň", StringComparison.OrdinalIgnoreCase)
    || name.Contains("welayatynyn", StringComparison.OrdinalIgnoreCase)
    || name.Contains("etrabynyň", StringComparison.OrdinalIgnoreCase)
    || name.Contains("etrabynyn", StringComparison.OrdinalIgnoreCase);

static bool IsShortAlias(string shorter, string longer)
{
    var ns = Norm(shorter);
    var nl = Norm(longer);
    if (ns.Length < 4 || nl.Length < ns.Length) return false;
    if (!nl.Contains(ns, StringComparison.Ordinal)) return false;
    // short alias like "Ak bugdaý" vs "Akbugdaý etraby" (not etraby vs şäheri)
    var shortIsOfficialType = ContainsPlaceType(shorter);
    var longIsOfficialType = ContainsPlaceType(longer);
    return !shortIsOfficialType || !longIsOfficialType;
}

static bool ContainsPlaceType(string name) =>
    name.Contains("etraby", StringComparison.OrdinalIgnoreCase)
    || name.Contains("şäheri", StringComparison.OrdinalIgnoreCase)
    || name.Contains("saheri", StringComparison.OrdinalIgnoreCase)
    || name.Contains("obasy", StringComparison.OrdinalIgnoreCase);

static bool IsEtrabySaheriSiblingPair(string a, string b)
{
    var aE = a.Contains("etraby", StringComparison.OrdinalIgnoreCase);
    var bE = b.Contains("etraby", StringComparison.OrdinalIgnoreCase);
    var aS = a.Contains("şäheri", StringComparison.OrdinalIgnoreCase) || a.Contains("saheri", StringComparison.OrdinalIgnoreCase);
    var bS = b.Contains("şäheri", StringComparison.OrdinalIgnoreCase) || b.Contains("saheri", StringComparison.OrdinalIgnoreCase);
    if (!((aE && bS) || (aS && bE))) return false;
    return Norm(a) == Norm(b);
}

static void WriteHeader(IXLWorksheet ws, string[] headers)
{
    for (var i = 0; i < headers.Length; i++)
    {
        ws.Cell(1, i + 1).Value = headers[i];
        ws.Cell(1, i + 1).Style.Font.Bold = true;
    }
}

static string[] SplitCsv(string line)
{
    var list = new List<string>();
    var sb = new StringBuilder();
    var inQuotes = false;
    for (var i = 0; i < line.Length; i++)
    {
        var ch = line[i];
        if (ch == '"')
        {
            if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
            {
                sb.Append('"');
                i++;
            }
            else inQuotes = !inQuotes;
            continue;
        }
        if (ch == ',' && !inQuotes)
        {
            list.Add(sb.ToString());
            sb.Clear();
            continue;
        }
        sb.Append(ch);
    }
    list.Add(sb.ToString());
    return list.ToArray();
}

static int ParseInt(string s) => int.TryParse(s.Trim(), out var n) ? n : 0;

sealed record CityRow(int Index, string NameTm, string Region, string? PdfFormCode, string Norm);
sealed record UsageRow(int AoR, int Lodging, int Hotel, int Hospital, bool RegionLinked)
{
    public static UsageRow Zero { get; } = new(0, 0, 0, 0, false);
}
sealed record NearDup(
    string Region, string NameTmA, string NameTmB, string CodeA, string CodeB,
    int LodgingA, int LodgingB, int HotelA, int HotelB, int AoRA, int AoRB,
    string SuggestedKeeper, string SuggestedDecision, string Confidence, string Decision, string Reason);

sealed class RegionNameComparer : IEqualityComparer<(string Region, string NameTm)>
{
    public bool Equals((string Region, string NameTm) x, (string Region, string NameTm) y) =>
        string.Equals(x.Region, y.Region, StringComparison.Ordinal)
        && string.Equals(x.NameTm, y.NameTm, StringComparison.Ordinal);

    public int GetHashCode((string Region, string NameTm) obj) =>
        HashCode.Combine(obj.Region, obj.NameTm);
}

static class CliArgs
{
    public static Dictionary<string, string> Parse(string[] argv)
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

    public static string Require(this Dictionary<string, string> d, string key) =>
        d.TryGetValue(key, out var v) && !string.IsNullOrWhiteSpace(v)
            ? v
            : throw new ArgumentException($"Missing {key}");

    public static string? Get(this Dictionary<string, string> d, string key) =>
        d.TryGetValue(key, out var v) ? v : null;
}
