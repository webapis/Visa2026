using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Visa2026.DataImporter.Legacy.Visa2014;

internal sealed class Visa2014LegacySourceProfile
{
    public string Id { get; init; } = "";
    public string Label { get; init; } = "";
    public string LegacyDatabase { get; init; } = "";
    public string ConnectionString { get; init; } = "";
    public IReadOnlyList<string> LookupTranslationPaths { get; init; } = [];
    public string IdMapDirectory { get; init; } = "";
    private readonly IReadOnlyDictionary<string, string> _previewOutputs;

    public Visa2014LegacySourceProfile(
        string id,
        string label,
        string legacyDatabase,
        string connectionString,
        IReadOnlyList<string> lookupTranslationPaths,
        string idMapDirectory,
        IReadOnlyDictionary<string, string> previewOutputs)
    {
        Id = id;
        Label = label;
        LegacyDatabase = legacyDatabase;
        ConnectionString = connectionString;
        LookupTranslationPaths = lookupTranslationPaths;
        IdMapDirectory = idMapDirectory;
        _previewOutputs = previewOutputs;
    }

    public string PreviewOutputPath(string dataImporterRoot, string entity)
    {
        if (_previewOutputs.TryGetValue(entity, out var relative) && !string.IsNullOrWhiteSpace(relative))
            return Path.Combine(Visa2014ContentRoot.LegacyRoot(dataImporterRoot), relative.Replace('/', Path.DirectorySeparatorChar));

        return Visa2014ContentRoot.DefaultPreviewOutputPath(dataImporterRoot, entity);
    }

    public string IdMapPath(string dataImporterRoot, string entity) =>
        Path.Combine(Visa2014ContentRoot.LegacyRoot(dataImporterRoot), IdMapDirectory, $"{entity}.json");
}

internal static class Visa2014LegacySource
{
    public static Visa2014LegacySourceProfile Resolve(
        string dataImporterRoot,
        string? solutionRoot,
        IReadOnlyList<string> args)
    {
        var sourcesPath = Path.Combine(Visa2014ContentRoot.LegacyRoot(dataImporterRoot), "legacy-sources.yaml");
        if (!File.Exists(sourcesPath))
            throw new FileNotFoundException("legacy-sources.yaml not found.", sourcesPath);

        var yaml = File.ReadAllText(sourcesPath);
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();

        var root = deserializer.Deserialize<LegacySourcesRoot>(yaml);
        var sourceId = GetOptionValue(args, "--legacy-source")
            ?? Environment.GetEnvironmentVariable("VISA2014_LEGACY_SOURCE")
            ?? root.DefaultSource
            ?? "calik-energi";

        var node = (root.Sources ?? []).FirstOrDefault(s =>
            string.Equals(s.Id, sourceId, StringComparison.OrdinalIgnoreCase));

        if (node == null || string.IsNullOrWhiteSpace(node.Id))
            throw new InvalidOperationException($"Unknown --legacy-source '{sourceId}'. See legacy-sources.yaml.");

        if (solutionRoot == null)
            throw new InvalidOperationException("Could not locate solution root for lookup translation paths.");

        var lookupPaths = new List<string>();
        foreach (var relative in node.LookupTranslations ?? [])
        {
            if (string.IsNullOrWhiteSpace(relative))
                continue;

            var full = Path.IsPathRooted(relative)
                ? relative
                : Path.Combine(solutionRoot, relative.Replace('/', Path.DirectorySeparatorChar));

            if (!File.Exists(full))
                throw new FileNotFoundException($"Lookup translations file not found for source '{node.Id}'.", full);

            lookupPaths.Add(full);
        }

        if (lookupPaths.Count == 0)
            throw new InvalidOperationException($"Source '{node.Id}' has no lookupTranslations entries.");

        var connection = Visa2014ContentRoot.ApplySqlPasswordFromEnvironment(
            Visa2014ContentRoot.ResolveConnectionString(
                GetOptionValue(args, "--connection"),
                node.ConnectionString));

        return new Visa2014LegacySourceProfile(
            node.Id,
            node.Label ?? node.Id,
            node.LegacyDatabase ?? "?",
            connection,
            lookupPaths,
            node.IdMapDir ?? $"id-maps/{node.Id}",
            node.PreviewOutput ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));
    }

    private static string? GetOptionValue(IReadOnlyList<string> args, string optionName)
    {
        for (int i = 0; i < args.Count; i++)
        {
            if (!string.Equals(args[i], optionName, StringComparison.OrdinalIgnoreCase))
                continue;

            if (i + 1 < args.Count && !args[i + 1].StartsWith('-'))
                return args[i + 1];
            return null;
        }

        return null;
    }

    private sealed class LegacySourcesRoot
    {
        public string? DefaultSource { get; set; }
        public List<SourceNode>? Sources { get; set; }
    }

    private sealed class SourceNode
    {
        public string? Id { get; set; }
        public string? Label { get; set; }
        public string? LegacyDatabase { get; set; }
        public string? ConnectionString { get; set; }
        public List<string>? LookupTranslations { get; set; }
        public Dictionary<string, string>? PreviewOutput { get; set; }
        public string? IdMapDir { get; set; }
    }
}
