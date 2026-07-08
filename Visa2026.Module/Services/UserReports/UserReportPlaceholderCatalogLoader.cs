using System.Reflection;
using System.Text.Json;

namespace Visa2026.Module.Services.UserReports;

internal static class UserReportPlaceholderCatalogLoader
{
    private const string ResourceName = "Visa2026.Module.Resources.UserReportPlaceholderCatalog.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    public static UserReportPlaceholderCatalogFile Load()
    {
        var assembly = typeof(UserReportPlaceholderCatalogLoader).Assembly;
        using var stream = assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException($"Embedded resource not found: {ResourceName}");

        using var reader = new StreamReader(stream);
        var json = reader.ReadToEnd();
        return JsonSerializer.Deserialize<UserReportPlaceholderCatalogFile>(json, JsonOptions)
            ?? throw new InvalidOperationException("Placeholder catalog JSON is empty.");
    }
}

internal sealed class UserReportPlaceholderAliasMaps
{
    public required IReadOnlyDictionary<string, string> ShortToCanonical { get; init; }

    public required IReadOnlyDictionary<string, string> CanonicalToShort { get; init; }

    public static UserReportPlaceholderAliasMaps FromCatalog(UserReportPlaceholderCatalogFile file)
    {
        var shortToCanonical = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var canonicalToShort = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var entry in file.Entries)
        {
            if (string.IsNullOrWhiteSpace(entry.ShortCode) || string.IsNullOrWhiteSpace(entry.CanonicalPath))
                continue;

            var shortCode = entry.ShortCode.Trim();
            var canonical = entry.CanonicalPath.Trim();
            shortToCanonical[shortCode] = canonical;
            canonicalToShort.TryAdd(canonical, shortCode);
        }

        return new UserReportPlaceholderAliasMaps
        {
            ShortToCanonical = shortToCanonical,
            CanonicalToShort = canonicalToShort,
        };
    }
}

/// <summary>Resolves short placeholder codes to canonical BO property paths.</summary>
public static class UserReportPlaceholderAliasRegistry
{
    private static readonly Lazy<UserReportPlaceholderAliasMaps> Maps = new(() =>
        UserReportPlaceholderAliasMaps.FromCatalog(UserReportPlaceholderCatalogLoader.Load()));

    public static string ResolveCanonicalPropertyPath(string propertyPath)
    {
        if (string.IsNullOrWhiteSpace(propertyPath))
            return propertyPath ?? string.Empty;

        var path = propertyPath.Trim();
        if (Maps.Value.ShortToCanonical.TryGetValue(path, out var canonical))
            return canonical;

        return path;
    }

    public static bool TryGetShortCode(string canonicalPath, out string shortCode) =>
        Maps.Value.CanonicalToShort.TryGetValue(canonicalPath.Trim(), out shortCode!);

    public static void EnrichDictionary(IDictionary<string, object> data)
    {
        if (data == null || data.Count == 0)
            return;

        foreach (var pair in data.ToList())
        {
            if (TryGetShortCode(pair.Key, out var shortCode) && !data.ContainsKey(shortCode))
                data[shortCode] = pair.Value;

            if (Maps.Value.ShortToCanonical.TryGetValue(pair.Key, out var canonical)
                && !string.Equals(canonical, pair.Key, StringComparison.OrdinalIgnoreCase)
                && !data.ContainsKey(canonical))
            {
                data[canonical] = pair.Value;
            }
        }
    }
}