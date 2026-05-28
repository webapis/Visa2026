using System.Text.Json;
using System.Text.Json.Serialization;

namespace Visa2026.DataImporter;

/// <summary>
/// Loads ApplicationType <c>Show*</c> flags from
/// <c>Visa2026.Module/DatabaseUpdate/LookupCatalogs/ApplicationTypeConfigurationCatalog.json</c>.
/// </summary>
internal sealed class ApplicationTypeVisibilityCatalog
{
    private const string CatalogRelativeFromSolutionRoot =
        "Visa2026.Module/DatabaseUpdate/LookupCatalogs/ApplicationTypeConfigurationCatalog.json";

    private const string CatalogRelativeFromOutput =
        "LookupCatalogs/ApplicationTypeConfigurationCatalog.json";

    private readonly Dictionary<string, Dictionary<string, bool>> _byType =
        new(StringComparer.OrdinalIgnoreCase);

    public static ApplicationTypeVisibilityCatalog Load()
    {
        string path = ResolveCatalogPath()
            ?? throw new FileNotFoundException(
                $"ApplicationType configuration catalog not found. Expected at solution path '{CatalogRelativeFromSolutionRoot}' " +
                $"or output path '{CatalogRelativeFromOutput}' (linked copy from Module).");

        var json = File.ReadAllText(path);
        var catalog = JsonSerializer.Deserialize<CatalogRoot>(json, JsonOptions);
        if (catalog?.Rows == null || catalog.Rows.Count == 0)
            throw new InvalidOperationException($"ApplicationType configuration catalog has no rows: {path}");

        var result = new ApplicationTypeVisibilityCatalog();
        foreach (var row in catalog.Rows)
        {
            if (string.IsNullOrWhiteSpace(row.Name))
                continue;

            var flags = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            if (row.Flags != null)
            {
                foreach (var (key, value) in row.Flags)
                    flags[key] = value;
            }

            result._byType[row.Name.Trim()] = flags;
        }

        return result;
    }

    public static string? ResolveCatalogPath()
    {
        if (LookupDumper.FindSolutionRoot(AppContext.BaseDirectory) is string solutionRoot)
        {
            string fromSolution = Path.Combine(
                solutionRoot,
                "Visa2026.Module",
                "DatabaseUpdate",
                "LookupCatalogs",
                "ApplicationTypeConfigurationCatalog.json");
            if (File.Exists(fromSolution))
                return fromSolution;
        }

        string fromOutput = Path.Combine(AppContext.BaseDirectory, CatalogRelativeFromOutput);
        if (File.Exists(fromOutput))
            return fromOutput;

        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            string candidate = Path.Combine(dir.FullName, CatalogRelativeFromSolutionRoot.Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(candidate))
                return candidate;

            dir = dir.Parent;
        }

        return null;
    }

    public bool TryGetFlags(string applicationTypeName, out IReadOnlyDictionary<string, bool> flags)
    {
        if (_byType.TryGetValue(applicationTypeName, out var dict))
        {
            flags = dict;
            return true;
        }

        flags = EmptyFlags;
        return false;
    }

    public IReadOnlyCollection<string> ApplicationTypeNames => _byType.Keys;

    private static readonly IReadOnlyDictionary<string, bool> EmptyFlags =
        new Dictionary<string, bool>();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    private sealed class CatalogRoot
    {
        public List<CatalogRow>? Rows { get; set; }
    }

    private sealed class CatalogRow
    {
        public string? Name { get; set; }

        [JsonPropertyName("Flags")]
        public Dictionary<string, bool>? Flags { get; set; }
    }
}
