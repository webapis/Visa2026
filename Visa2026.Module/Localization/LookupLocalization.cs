using System;
using System.Globalization;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Text.Json;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Localization;

/// <summary>
/// Resolves UI display text for global lookup rows (Layer B). Falls back to <see cref="LookupBase.Name"/> /
/// <see cref="LookupBase.NameTm"/> until <c>Localization/LookupStrings.json</c> has an entry.
/// </summary>
public static class LookupLocalization
{
    private static readonly Lazy<IReadOnlyDictionary<string, IReadOnlyDictionary<string, Dictionary<string, string>>>> CatalogRows =
        new(LoadLookupStrings);

    /// <summary>Culture-aware label for grids, combos, and reports that should follow UI language.</summary>
    public static string GetDisplayName(LookupBase? lookup, string? cultureName = null)
    {
        if (lookup == null)
            return string.Empty;

        cultureName = VisaUiMessages.NormalizeCultureName(cultureName ?? CultureInfo.CurrentUICulture.Name);

        if (TryGetLocalizedString(lookup, cultureName, out var localized))
            return localized;

        return cultureName.StartsWith("tk", StringComparison.OrdinalIgnoreCase)
            ? FirstNonEmpty(lookup.NameTm, lookup.Name)
            : FirstNonEmpty(lookup.Name, lookup.NameTm);
    }

    /// <summary>Stable string-table key for <c>LookupStrings.json</c> (semantic keys, not legacy Turkmen codes).</summary>
    public static string GetEffectiveKey(LookupBase? lookup) => LookupLocalizationKeys.Resolve(lookup);

    /// <summary>Culture-aware string from a Layer B catalog (e.g. application-type-group).</summary>
    public static string GetCatalogDisplayName(string catalogId, string key, string? cultureName = null)
    {
        if (string.IsNullOrWhiteSpace(catalogId) || string.IsNullOrWhiteSpace(key))
            return string.Empty;

        cultureName = VisaUiMessages.NormalizeCultureName(cultureName ?? CultureInfo.CurrentUICulture.Name);

        if (!CatalogRows.Value.TryGetValue(catalogId, out var rows)
            || !rows.TryGetValue(key.Trim(), out var cultures))
        {
            return string.Empty;
        }

        return TryResolveCulture(cultures, cultureName, out var value) ? value : string.Empty;
    }

    public static bool TryGetCatalogKind(Type lookupType, out GlobalLookupCatalogKind kind)
    {
        kind = default;
        if (lookupType == null)
            return false;

        var attribute = lookupType.GetCustomAttribute<GlobalLookupCatalogAttribute>();
        if (attribute == null)
            return false;

        kind = attribute.Kind;
        return true;
    }

    private static bool TryGetLocalizedString(LookupBase lookup, string cultureName, out string value)
    {
        value = string.Empty;
        var key = GetEffectiveKey(lookup);
        if (string.IsNullOrWhiteSpace(key))
            return false;

        if (!TryGetCatalogId(lookup, out var catalogId))
            return false;
        if (!CatalogRows.Value.TryGetValue(catalogId, out var rows))
            return false;

        if (!rows.TryGetValue(key, out var cultures))
            return false;

        return TryResolveCulture(cultures, cultureName, out value);
    }

    private static bool TryResolveCulture(IReadOnlyDictionary<string, string> cultures, string cultureName, out string value)
    {
        value = string.Empty;
        if (TryCulture(cultures, cultureName, out value))
            return true;

        if (cultureName.StartsWith("tk", StringComparison.OrdinalIgnoreCase)
            && TryCulture(cultures, "tk-TM", out value))
            return true;

        if (cultureName.StartsWith("tr", StringComparison.OrdinalIgnoreCase)
            && TryCulture(cultures, "tr-TR", out value))
            return true;

        if (cultureName.StartsWith("ru", StringComparison.OrdinalIgnoreCase)
            && TryCulture(cultures, "ru-RU", out value))
            return true;

        return TryCulture(cultures, "en-US", out value);
    }

    private static bool TryCulture(IReadOnlyDictionary<string, string> cultures, string culture, out string value)
    {
        value = string.Empty;
        if (cultures.TryGetValue(culture, out var exact) && !string.IsNullOrWhiteSpace(exact))
        {
            value = exact;
            return true;
        }

        return false;
    }

    private static bool TryGetCatalogId(LookupBase lookup, out string catalogId)
    {
        catalogId = string.Empty;
        if (lookup is ApplicationType)
        {
            catalogId = "application-type";
            return true;
        }

        if (!TryGetCatalogKind(lookup.GetType(), out var catalogKind))
            return false;

        catalogId = CatalogId(catalogKind);
        return true;
    }

    private static string CatalogId(GlobalLookupCatalogKind kind) =>
        kind switch
        {
            GlobalLookupCatalogKind.MaritalStatus => "marital-status",
            GlobalLookupCatalogKind.Country => "country",
            GlobalLookupCatalogKind.VisaCategory => "visa-category",
            GlobalLookupCatalogKind.VisaPeriod => "visa-period",
            GlobalLookupCatalogKind.VisaType => "visa-type",
            GlobalLookupCatalogKind.EducationLevel => "education-level",
            GlobalLookupCatalogKind.PurposeOfTravel => "purpose-of-travel",
            GlobalLookupCatalogKind.CheckPoint => "checkpoint",
            GlobalLookupCatalogKind.VisaIssuedPlace => "visa-issued-place",
            GlobalLookupCatalogKind.MigrationService => "migration-service",
            GlobalLookupCatalogKind.PassportType => "passport-type",
            GlobalLookupCatalogKind.ApplicationLocation => "application-location",
            GlobalLookupCatalogKind.BorderZoneLocation => "border-zone-location",
            GlobalLookupCatalogKind.ValidityDuration => "validity-duration",
            GlobalLookupCatalogKind.ApplicationState => "application-state",
            _ => kind.ToString().ToLowerInvariant(),
        };

    private static string FirstNonEmpty(string? a, string? b) =>
        !string.IsNullOrWhiteSpace(a) ? a! : (b ?? string.Empty);

    private static IReadOnlyDictionary<string, IReadOnlyDictionary<string, Dictionary<string, string>>> LoadLookupStrings()
    {
        var root = LoadJsonResource("Localization.LookupStrings.json");
        MergeJsonResource(root, "Localization.ApplicationTypeLookupStrings.json");
        MergeJsonResource(root, "Localization.VisaLookupStrings.json");
        MergeJsonResource(root, "Localization.CountryLookupStrings.json");
        MergeJsonResource(root, "Localization.RelationshipLookupStrings.json");
        MergeJsonResource(root, "Localization.LookupCatalogStrings.json");
        if (root == null || root.Count == 0)
            return new Dictionary<string, IReadOnlyDictionary<string, Dictionary<string, string>>>(StringComparer.OrdinalIgnoreCase);

        return root.ToDictionary(
            kv => kv.Key,
            kv => (IReadOnlyDictionary<string, Dictionary<string, string>>)kv.Value,
            StringComparer.OrdinalIgnoreCase);
    }

    private static Dictionary<string, Dictionary<string, Dictionary<string, string>>>? LoadJsonResource(string suffix)
    {
        var assembly = typeof(LookupLocalization).Assembly;
        var resourceName = assembly.GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith(suffix, StringComparison.OrdinalIgnoreCase));

        if (resourceName == null)
            return null;

        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
            return null;

        using var reader = new StreamReader(stream);
        var json = reader.ReadToEnd();
        return JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, Dictionary<string, string>>>>(json);
    }

    private static void MergeJsonResource(
        Dictionary<string, Dictionary<string, Dictionary<string, string>>>? target,
        string suffix)
    {
        if (target == null)
            return;

        var source = LoadJsonResource(suffix);
        if (source == null)
            return;

        foreach (var catalog in source)
            target[catalog.Key] = catalog.Value;
    }
}
