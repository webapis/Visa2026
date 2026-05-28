using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Visa2026.DataImporter;

/// <summary>
/// Loads scenario seed data from <c>seed/scenarios.index.yaml</c> and <c>seed/scenarios/*.yaml</c>,
/// or a legacy monolithic <c>data.yaml</c> file.
/// </summary>
public static class YamlSeedCatalog
{
    public const string DefaultIndexRelativePath = "seed/scenarios.index.yaml";
    public const string LegacyMonolithicFileName = "data.yaml";

    private static readonly IDeserializer Deserializer = new DeserializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .IgnoreUnmatchedProperties()
        .Build();

    private static readonly ISerializer Serializer = new SerializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .Build();

    /// <summary>
    /// Resolves a user-supplied path to an existing seed source (index, directory, or legacy file).
    /// </summary>
    public static string? ResolveExistingSeedPath(string? spec, string? baseDirectory = null)
    {
        baseDirectory ??= AppContext.BaseDirectory;

        if (!string.IsNullOrWhiteSpace(spec))
        {
            var resolved = ResolvePath(spec, baseDirectory);
            if (resolved != null)
                return resolved;
        }

        foreach (var candidate in new[]
                 {
                     DefaultIndexRelativePath,
                     Path.Combine("seed", "scenarios.index.yaml"),
                     LegacyMonolithicFileName,
                 })
        {
            var resolved = ResolvePath(candidate, baseDirectory);
            if (resolved != null)
                return resolved;
        }

        return null;
    }

    public static bool IsSeedIndexPath(string path) =>
        path.EndsWith("scenarios.index.yaml", StringComparison.OrdinalIgnoreCase)
        || (Directory.Exists(path) && File.Exists(Path.Combine(path, "scenarios.index.yaml")));

    public static bool IsSeedDirectory(string path) =>
        Directory.Exists(path) && File.Exists(Path.Combine(path, "scenarios.index.yaml"));

    public static List<YamlScenario> LoadScenarios(string seedPath)
    {
        if (IsSeedIndexPath(seedPath) || IsSeedDirectory(seedPath))
            return LoadFromIndex(seedPath);

        if (!File.Exists(seedPath))
            throw new FileNotFoundException($"Seed file not found: {seedPath}");

        return LoadMonolithic(seedPath);
    }

    public static void ExportMonolithicToSeedLayout(string monolithicPath, string seedRootDirectory)
    {
        var scenarios = LoadMonolithic(monolithicPath);
        if (scenarios.Count == 0)
            throw new InvalidOperationException($"No scenarios found in {monolithicPath}.");

        var scenariosDir = Path.Combine(seedRootDirectory, "scenarios");
        Directory.CreateDirectory(scenariosDir);

        var indexEntries = new List<string>();
        foreach (var scenario in scenarios.OrderBy(s => s.Order).ThenBy(s => s.Name, StringComparer.OrdinalIgnoreCase))
        {
            string fileName = BuildScenarioFileName(scenario);
            string filePath = Path.Combine(scenariosDir, fileName);
            File.WriteAllText(filePath, Serializer.Serialize(scenario));
            indexEntries.Add(fileName);
        }

        var index = new YamlSeedIndex
        {
            Version = 1,
            Scenarios = indexEntries,
        };

        Directory.CreateDirectory(seedRootDirectory);
        File.WriteAllText(
            Path.Combine(seedRootDirectory, "scenarios.index.yaml"),
            Serializer.Serialize(index));
    }

    private static List<YamlScenario> LoadMonolithic(string filePath)
    {
        var yaml = File.ReadAllText(filePath);
        var root = Deserializer.Deserialize<YamlRoot>(yaml);
        var scenarios = root?.Scenarios ?? new List<YamlScenario>();
        scenarios.Sort((a, b) => a.Order.CompareTo(b.Order));
        return scenarios;
    }

    private static List<YamlScenario> LoadFromIndex(string indexOrDirectoryPath)
    {
        string seedRoot = IsSeedDirectory(indexOrDirectoryPath)
            ? indexOrDirectoryPath
            : Path.GetDirectoryName(Path.GetFullPath(indexOrDirectoryPath))
              ?? throw new InvalidOperationException($"Invalid seed index path: {indexOrDirectoryPath}");

        string indexPath = IsSeedDirectory(indexOrDirectoryPath)
            ? Path.Combine(seedRoot, "scenarios.index.yaml")
            : indexOrDirectoryPath;

        var index = Deserializer.Deserialize<YamlSeedIndex>(File.ReadAllText(indexPath))
                    ?? new YamlSeedIndex();

        if (index.Scenarios == null || index.Scenarios.Count == 0)
            throw new InvalidOperationException($"No scenarios listed in {indexPath}.");

        var scenariosDir = Path.Combine(seedRoot, "scenarios");
        var loaded = new List<YamlScenario>();

        foreach (var entry in index.Scenarios)
        {
            if (string.IsNullOrWhiteSpace(entry))
                continue;

            string fileName = entry.Trim().Replace('\\', '/');
            if (fileName.Contains('/'))
                fileName = Path.GetFileName(fileName);

            string scenarioPath = Path.Combine(scenariosDir, fileName);
            if (!File.Exists(scenarioPath))
                throw new FileNotFoundException($"Scenario file listed in index not found: {scenarioPath}");

            var scenario = Deserializer.Deserialize<YamlScenario>(File.ReadAllText(scenarioPath))
                           ?? throw new InvalidOperationException($"Empty scenario file: {scenarioPath}");

            if (string.IsNullOrWhiteSpace(scenario.Name))
                throw new InvalidOperationException($"Scenario file missing name: {scenarioPath}");

            scenario.SourceFile = fileName;
            loaded.Add(scenario);
        }

        loaded.Sort((a, b) => a.Order.CompareTo(b.Order));
        return loaded;
    }

    private static string BuildScenarioFileName(YamlScenario scenario)
    {
        string slug = Slugify(scenario.Name);
        int order = scenario.Order;
        return $"{order:D2}-{slug}.yaml";
    }

    private static string Slugify(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "scenario";

        var chars = name.Trim().Select(c =>
            char.IsLetterOrDigit(c) ? char.ToLowerInvariant(c) : '-').ToArray();

        string slug = new string(chars).Trim('-');
        while (slug.Contains("--", StringComparison.Ordinal))
            slug = slug.Replace("--", "-", StringComparison.Ordinal);

        return string.IsNullOrEmpty(slug) ? "scenario" : slug;
    }

    private static string? ResolvePath(string path, string baseDirectory)
    {
        if (File.Exists(path))
            return Path.GetFullPath(path);

        if (Directory.Exists(path))
            return Path.GetFullPath(path);

        string fromBase = Path.Combine(baseDirectory, path);
        if (File.Exists(fromBase))
            return Path.GetFullPath(fromBase);

        if (Directory.Exists(fromBase))
            return Path.GetFullPath(fromBase);

        string? dir = Path.GetDirectoryName(path);
        string? file = Path.GetFileName(path);
        if (!string.IsNullOrEmpty(dir) && !string.IsNullOrEmpty(file))
        {
            string nested = Path.Combine(baseDirectory, dir, file);
            if (File.Exists(nested))
                return Path.GetFullPath(nested);
        }

        return null;
    }
}

/// <summary>Index for <c>seed/scenarios.index.yaml</c> — ordered list of scenario fragment files.</summary>
public class YamlSeedIndex
{
    public int Version { get; set; } = 1;
    public List<string> Scenarios { get; set; } = new();
}
