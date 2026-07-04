using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

if (args.Length < 1)
{
    Console.Error.WriteLine("Usage: GenerateApprovalLegProfileCatalog <project-contract.json> [output.json] [--strip]");
    return 1;
}

var contractPath = Path.GetFullPath(args[0]);
var outPath = args.Length > 1 && !args[1].StartsWith("--", StringComparison.Ordinal)
    ? Path.GetFullPath(args[1])
    : Path.Combine(Path.GetDirectoryName(contractPath)!, "approval-leg-profile.json");
var strip = args.Any(a => string.Equals(a, "--strip", StringComparison.OrdinalIgnoreCase));

var json = await File.ReadAllTextAsync(contractPath, Encoding.UTF8);
var root = JsonNode.Parse(json) as JsonObject
    ?? throw new InvalidOperationException("Expected JSON object root.");
var rows = root["rows"] as JsonArray
    ?? throw new InvalidOperationException("Expected rows array.");

var unique = new Dictionary<string, JsonObject>(StringComparer.Ordinal);

foreach (var rowNode in rows)
{
    if (rowNode is not JsonObject row)
        continue;

    if (row["MinistryLegs"] is not JsonArray legs || legs.Count == 0)
        continue;

    var sorted = legs
        .OfType<JsonObject>()
        .OrderBy(l => l["Sequence"]?.GetValue<int>() ?? 0)
        .ToList();

    var key = string.Join('|', sorted.Select(l => l["ApprovingMinistryShortNameTm"]?.GetValue<string>()?.Trim() ?? ""));
    if (string.IsNullOrWhiteSpace(key) || unique.ContainsKey(key))
        continue;

    var tokens = sorted
        .Select(l => ToProfileToken(l["ApprovingMinistryShortNameTm"]?.GetValue<string>() ?? ""))
        .ToList();
    var code = string.Join('-', tokens);
    var nameTm = string.Join('-', sorted.Select(l => l["ApprovingMinistryShortNameTm"]?.GetValue<string>()?.Trim() ?? ""));

    var cleanLegs = new JsonArray();
    foreach (var leg in sorted)
    {
        cleanLegs.Add(new JsonObject
        {
            ["Sequence"] = leg["Sequence"]?.GetValue<int>() ?? 0,
            ["ApprovingMinistryShortNameTm"] = leg["ApprovingMinistryShortNameTm"]?.GetValue<string>()?.Trim(),
            ["MaxDaysInReview"] = leg["MaxDaysInReview"]?.GetValue<int?>() ?? 10,
            ["WarningDaysBeforeMax"] = leg["WarningDaysBeforeMax"]?.GetValue<int?>() ?? 8,
        });
    }

    unique[key] = new JsonObject
    {
        ["Code"] = code,
        ["NameTm"] = nameTm,
        ["LocalizationKey"] = ToLocalizationKey(code),
        ["IsActive"] = true,
        ["MinistryLegs"] = cleanLegs,
    };
}

var profileRows = unique.Values
    .OrderBy(p => p["Code"]?.GetValue<string>(), StringComparer.Ordinal)
    .ToArray();

var output = new JsonObject { ["rows"] = new JsonArray(profileRows) };
var options = new JsonSerializerOptions { WriteIndented = true };
await File.WriteAllTextAsync(outPath, output.ToJsonString(options), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

Console.WriteLine($"Wrote {profileRows.Length} profile(s) to {outPath}");
foreach (var profile in profileRows)
    Console.WriteLine($"  {profile["Code"]} — {profile["NameTm"]}");

if (strip)
{
    foreach (var rowNode in rows)
    {
        if (rowNode is JsonObject row)
            row.Remove("MinistryLegs");
    }

    await File.WriteAllTextAsync(contractPath, root.ToJsonString(options), new UTF8Encoding(false));
    Console.WriteLine($"Stripped MinistryLegs from {contractPath}");

    var calikPath = Path.Combine(Path.GetDirectoryName(contractPath)!, "project-contract.calik-energi.json");
    if (File.Exists(calikPath))
    {
        var calikJson = await File.ReadAllTextAsync(calikPath, Encoding.UTF8);
        var calikRoot = JsonNode.Parse(calikJson) as JsonObject;
        if (calikRoot?["rows"] is JsonArray calikRows)
        {
            foreach (var rowNode in calikRows)
            {
                if (rowNode is JsonObject row)
                    row.Remove("MinistryLegs");
            }

            await File.WriteAllTextAsync(calikPath, calikRoot.ToJsonString(options), new UTF8Encoding(false));
            Console.WriteLine($"Stripped MinistryLegs from {calikPath}");
        }
    }
}

return 0;

static string ToProfileToken(string shortNameTm)
{
    var folded = FoldTurkmen(shortNameTm).ToLowerInvariant().Trim();
    return folded switch
    {
        "turkmenenergo" => "TE",
        "energetika" => "EN",
        "gurlusyk" => "GU",
        "turkmengaz" => "TG",
        "asgabat hakimlik" => "AH",
        "tngiz" => "NG",
        "turkmenhimiya" => "TH",
        "turkmennebit" => "TN",
        _ => new string(folded.Where(char.IsLetterOrDigit).Take(4).ToArray()).ToUpperInvariant() is { Length: > 0 } s
            ? s
            : "DF",
    };
}

static string ToLocalizationKey(string value)
{
    var folded = FoldTurkmen(value).ToLowerInvariant();
    folded = string.Join('-', folded.Split([' ', '-', '_'], StringSplitOptions.RemoveEmptyEntries));
    folded = new string(folded.Where(c => char.IsLetterOrDigit(c) || c == '-').ToArray()).Trim('-');
    if (folded.Length <= 64)
        return folded;

    var hash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(folded)))
        .ToLowerInvariant()[..8];
    return folded[..(64 - 1 - hash.Length)] + '_' + hash;
}

static string FoldTurkmen(string value)
{
    if (string.IsNullOrWhiteSpace(value))
        return string.Empty;

    var map = new Dictionary<char, char>
    {
        ['ý'] = 'y', ['Ý'] = 'y',
        ['ä'] = 'a', ['Ä'] = 'a',
        ['ö'] = 'o', ['Ö'] = 'o',
        ['ü'] = 'u', ['Ü'] = 'u',
        ['ç'] = 'c', ['Ç'] = 'c',
        ['ş'] = 's', ['Ş'] = 's',
        ['ň'] = 'n', ['Ň'] = 'n',
        ['ž'] = 'z', ['Ž'] = 'z',
        ['î'] = 'i', ['Î'] = 'i',
    };

    var chars = value.ToCharArray();
    for (var i = 0; i < chars.Length; i++)
    {
        if (map.TryGetValue(chars[i], out var mapped))
            chars[i] = mapped;
    }

    return new string(chars);
}
