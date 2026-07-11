using System.Globalization;
using System.Text;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Visa2026.DataImporter.Legacy.Visa2014;

internal sealed class Visa2014LookupCatalog
{
    public required string TargetCatalog { get; init; }
    public required string TargetMatchProperty { get; init; }
    public required string UnmappedPolicy { get; init; }
    public bool IdentityPassThrough { get; init; }
    public Dictionary<string, string> LegacyToTarget { get; init; } = new(StringComparer.Ordinal);
}

internal static class Visa2014LookupTranslator
{
    public static IReadOnlyDictionary<string, Visa2014LookupCatalog> Load(string yamlPath) =>
        Load(new[] { yamlPath });

    public static IReadOnlyDictionary<string, Visa2014LookupCatalog> Load(IReadOnlyList<string> yamlPaths)
    {
        if (yamlPaths.Count == 0)
            throw new ArgumentException("At least one lookup translations path is required.", nameof(yamlPaths));

        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();

        var result = new Dictionary<string, Visa2014LookupCatalog>(StringComparer.Ordinal);

        foreach (var yamlPath in yamlPaths)
        {
            if (!File.Exists(yamlPath))
                throw new FileNotFoundException("Lookup translations file not found.", yamlPath);

            var yaml = File.ReadAllText(yamlPath);
            var root = deserializer.Deserialize<LookupRoot>(yaml);

            foreach (var catalog in root.Catalogs ?? [])
            {
                if (string.IsNullOrWhiteSpace(catalog.TargetCatalog))
                    continue;

                var map = new Dictionary<string, string>(StringComparer.Ordinal);
                // Later YAML files (tenant overlays) merge into the same catalog: keep base
                // values[] and let overlay keys override. Replacing wholesale wiped Person-wave
                // Country aliases (e.g. UAE→ARE) when calik-energi set identityPassThrough + values:[].
                if (result.TryGetValue(catalog.TargetCatalog, out var existing))
                {
                    foreach (var (legacy, target) in existing.LegacyToTarget)
                        map[legacy] = target;
                }

                foreach (var row in catalog.Values ?? [])
                {
                    if (string.IsNullOrWhiteSpace(row.Legacy) || string.IsNullOrWhiteSpace(row.Target))
                        continue;
                    map[row.Legacy.Trim()] = row.Target.Trim();
                }

                result[catalog.TargetCatalog] = new Visa2014LookupCatalog
                {
                    TargetCatalog = catalog.TargetCatalog,
                    TargetMatchProperty = catalog.TargetMatchProperty
                        ?? existing?.TargetMatchProperty
                        ?? "Name",
                    UnmappedPolicy = catalog.UnmappedPolicy
                        ?? existing?.UnmappedPolicy
                        ?? "block_row",
                    // Overlay may enable identityPassThrough; once true in any file, keep it
                    // unless this node explicitly sets false after a prior true (rare).
                    IdentityPassThrough = catalog.IdentityPassThrough || (existing?.IdentityPassThrough ?? false),
                    LegacyToTarget = map,
                };
            }
        }

        return result;
    }

    public static bool TryTranslate(
        IReadOnlyDictionary<string, Visa2014LookupCatalog> catalogs,
        string catalogName,
        string? legacyValue,
        out string? targetValue,
        out string? unmappedReason)
    {
        targetValue = null;
        unmappedReason = null;

        if (string.IsNullOrWhiteSpace(legacyValue))
            return true;

        var trimmed = legacyValue.Trim();
        if (!catalogs.TryGetValue(catalogName, out var catalog))
        {
            unmappedReason = $"unknown_catalog:{catalogName}";
            return false;
        }

        if (catalog.LegacyToTarget.TryGetValue(trimmed, out var exact))
        {
            targetValue = exact;
            return true;
        }

        foreach (var (legacy, target) in catalog.LegacyToTarget)
        {
            if (Visa2014CatalogMatchHelper.KeysEqual(legacy, trimmed))
            {
                targetValue = target;
                return true;
            }
        }

        if (catalog.IdentityPassThrough)
        {
            targetValue = trimmed;
            return true;
        }

        unmappedReason = $"unmapped_lookup:{catalogName}:{trimmed}";
        return catalog.UnmappedPolicy is "allow_null" or "skip_row";
    }

    private sealed class LookupRoot
    {
        public List<CatalogNode>? Catalogs { get; set; }
    }

    private sealed class CatalogNode
    {
        public string? TargetCatalog { get; set; }
        public string? TargetMatchProperty { get; set; }
        public string? UnmappedPolicy { get; set; }
        public bool IdentityPassThrough { get; set; }
        public List<ValueNode>? Values { get; set; }
    }

    private sealed class ValueNode
    {
        public string? Legacy { get; set; }
        public string? Target { get; set; }
    }
}

internal static class Visa2014CatalogMatchHelper
{
    public static bool KeysEqual(string? left, string? right)
    {
        if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right))
            return false;

        return string.Equals(NormalizeKey(left), NormalizeKey(right), StringComparison.Ordinal);
    }

    public static string NormalizeKey(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var folded = FoldTurkmenChars(value.Trim());
        var decomposed = folded.Normalize(NormalizationForm.FormD);
        var buffer = new StringBuilder(decomposed.Length);

        foreach (var ch in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark)
                continue;

            buffer.Append(char.ToLowerInvariant(ch));
        }

        return buffer.ToString();
    }

    private static string FoldTurkmenChars(string value)
    {
        var buffer = new StringBuilder(value.Length);
        foreach (var ch in value)
            buffer.Append(FoldTurkmenChar(ch));
        return buffer.ToString();
    }

    private static char FoldTurkmenChar(char ch) => ch switch
    {
        'Ä' or 'ä' => 'a',
        'Ç' or 'ç' => 'c',
        'Ž' or 'ž' => 'z',
        'Ň' or 'ň' => 'n',
        'Ö' or 'ö' => 'o',
        'Ş' or 'ş' => 's',
        'Ü' or 'ü' => 'u',
        'Ý' or 'ý' => 'y',
        _ => ch,
    };
}
