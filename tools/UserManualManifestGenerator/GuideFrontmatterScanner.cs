using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using UserManualManifestGenerator.Models;

namespace UserManualManifestGenerator;

public static class GuideFrontmatterScanner
{
    private static readonly Regex FrontmatterRegex = new(
        @"^---\s*\r?\n(?<body>.*?)\r?\n---",
        RegexOptions.Singleline | RegexOptions.Compiled);

    public static IReadOnlyList<GuideFrontmatter> Scan(string guidesRoot)
    {
        if (!Directory.Exists(guidesRoot))
            return Array.Empty<GuideFrontmatter>();

        var results = new List<GuideFrontmatter>();
        foreach (var file in Directory.EnumerateFiles(guidesRoot, "*.md", SearchOption.AllDirectories))
        {
            if (!file.Contains($"{Path.DirectorySeparatorChar}guides{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
                continue;

            if (Path.GetFileName(file).StartsWith("_", StringComparison.Ordinal))
                continue;

            var locale = TryGetLocaleFromPath(guidesRoot, file);
            var text = File.ReadAllText(file, Encoding.UTF8);
            var match = FrontmatterRegex.Match(text);
            if (!match.Success)
                continue;

            var yaml = match.Groups["body"].Value;
            results.Add(new GuideFrontmatter
            {
                FilePath = file,
                Locale = locale,
                Slug = ReadYamlScalar(yaml, "slug"),
                Bo = ReadYamlScalar(yaml, "bo"),
                Status = ReadYamlScalar(yaml, "status"),
            });
        }

        return results;
    }

    public static IReadOnlyDictionary<string, IReadOnlyList<string>> BuildGuideSlugsByBo(
        IReadOnlyList<GuideFrontmatter> guides)
    {
        var map = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        foreach (var guide in guides)
        {
            if (string.IsNullOrWhiteSpace(guide.Bo) || string.IsNullOrWhiteSpace(guide.Slug))
                continue;

            if (!map.TryGetValue(guide.Bo, out var slugs))
            {
                slugs = new HashSet<string>(StringComparer.Ordinal);
                map[guide.Bo] = slugs;
            }

            slugs.Add(guide.Slug);
        }

        return map.ToDictionary(
            pair => pair.Key,
            pair => (IReadOnlyList<string>)pair.Value.OrderBy(s => s, StringComparer.Ordinal).ToArray(),
            StringComparer.Ordinal);
    }

    private static string TryGetLocaleFromPath(string guidesRoot, string filePath)
    {
        var relative = Path.GetRelativePath(guidesRoot, filePath);
        var firstSegment = relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).FirstOrDefault();
        return string.IsNullOrWhiteSpace(firstSegment) ? "en" : firstSegment;
    }

    private static string? ReadYamlScalar(string yaml, string key)
    {
        var pattern = $@"^{Regex.Escape(key)}\s*:\s*(.+)$";
        foreach (var line in yaml.Split('\n'))
        {
            var trimmed = line.TrimEnd('\r');
            var match = Regex.Match(trimmed, pattern, RegexOptions.IgnoreCase);
            if (!match.Success)
                continue;

            var value = match.Groups[1].Value.Trim();
            if (value.StartsWith('"') && value.EndsWith('"') && value.Length >= 2)
                value = value[1..^1];
            return value;
        }

        return null;
    }
}
