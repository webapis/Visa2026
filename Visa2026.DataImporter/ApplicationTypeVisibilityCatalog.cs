using System.Text.Json;
using System.Text.Json.Serialization;

namespace Visa2026.DataImporter;

/// <summary>
/// Loads <c>seed/application-type-visibility.json</c> (generated from Module ApplicationType seed).
/// </summary>
internal sealed class ApplicationTypeVisibilityCatalog
{
    private readonly Dictionary<string, Dictionary<string, bool>> _byType =
        new(StringComparer.OrdinalIgnoreCase);

    public static ApplicationTypeVisibilityCatalog Load(string? baseDirectory = null)
    {
        baseDirectory ??= AppContext.BaseDirectory;
        var catalog = new ApplicationTypeVisibilityCatalog();

        foreach (string relative in new[]
                 {
                     Path.Combine("seed", "application-type-visibility.json"),
                     "application-type-visibility.json",
                 })
        {
            string path = Path.Combine(baseDirectory, relative);
            if (!File.Exists(path))
                continue;

            var json = File.ReadAllText(path);
            var root = JsonSerializer.Deserialize<VisibilityRoot>(json, JsonOptions);
            if (root?.ApplicationTypes == null)
                break;

            foreach (var (typeName, flags) in root.ApplicationTypes)
            {
                var dict = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
                foreach (var prop in flags)
                    dict[prop.Key] = prop.Value;

                catalog._byType[typeName] = dict;
            }

            return catalog;
        }

        throw new FileNotFoundException(
            "application-type-visibility.json not found. Run scripts/local/Export-ApplicationTypeSeedVisibility.ps1.");
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

    private sealed class VisibilityRoot
    {
        [JsonPropertyName("applicationTypes")]
        public Dictionary<string, Dictionary<string, bool>>? ApplicationTypes { get; set; }
    }
}
