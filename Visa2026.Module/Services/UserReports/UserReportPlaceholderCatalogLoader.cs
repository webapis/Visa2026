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

        SyncRowNumberKeys(data);

        foreach (var pair in data.ToList())
        {
            foreach (var alias in ShortCodesForKey(pair.Key))
            {
                if (!HasPresentValue(data, alias))
                    data[alias] = pair.Value;
            }

            if (Maps.Value.ShortToCanonical.TryGetValue(pair.Key, out var canonical)
                && !string.Equals(canonical, pair.Key, StringComparison.OrdinalIgnoreCase)
                && !HasPresentValue(data, canonical))
            {
                data[canonical] = pair.Value;
                foreach (var alias in ShortCodesForCanonical(canonical))
                {
                    if (!HasPresentValue(data, alias))
                        data[alias] = pair.Value;
                }
            }
        }
    }

    /// <summary>
    /// Yellow-marks sanaws use <c>{{.RNUM}}</c> (catalog <c>RowNumber</c>).
    /// Seeded Word lists store the same value as <c>RowNo</c>.
    /// </summary>
    private static void SyncRowNumberKeys(IDictionary<string, object> data)
    {
        if (!TryGetPresentValue(data, "RowNumber", out var number)
            && !TryGetPresentValue(data, "RowNo", out number)
            && !TryGetPresentValue(data, "RNUM", out number))
        {
            return;
        }

        data["RowNumber"] = number;
        data["RowNo"] = number;
        data["RNUM"] = number;
    }

    private static IEnumerable<string> ShortCodesForKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            yield break;

        if (TryGetShortCode(key, out var primary))
            yield return primary;

        foreach (var alias in ShortCodesForCanonical(key))
            yield return alias;
    }

    private static IEnumerable<string> ShortCodesForCanonical(string canonical)
    {
        if (string.IsNullOrWhiteSpace(canonical))
            yield break;

        var path = canonical.Trim();
        foreach (var pair in Maps.Value.ShortToCanonical)
        {
            if (string.Equals(pair.Value, path, StringComparison.OrdinalIgnoreCase))
                yield return pair.Key;
        }
    }

    private static bool HasPresentValue(IDictionary<string, object> data, string key) =>
        TryGetPresentValue(data, key, out _);

    private static bool TryGetPresentValue(IDictionary<string, object> data, string key, out object value)
    {
        if (data.TryGetValue(key, out value)
            && value != null
            && (value is not string text || !string.IsNullOrWhiteSpace(text)))
        {
            return true;
        }

        value = null!;
        return false;
    }
}