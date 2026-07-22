namespace Visa2026.DataImporter.Legacy.Visa2014;

/// <summary>
/// Classifies how a legacy lookup value resolves against layer-3 catalogs
/// (same order as <see cref="Visa2014LookupTranslator.TryTranslate"/>).
/// </summary>
internal enum Visa2014LookupResolveKind
{
    Empty,
    ExactYaml,
    NormalizedYaml,
    IdentityPassThrough,
    Unmapped,
}

internal static class Visa2014LookupOutcomeClassifier
{
    public static Visa2014LookupResolveKind Classify(
        IReadOnlyDictionary<string, Visa2014LookupCatalog> catalogs,
        string catalogName,
        string? legacyValue,
        out string? translatedTarget)
    {
        translatedTarget = null;
        if (string.IsNullOrWhiteSpace(legacyValue))
            return Visa2014LookupResolveKind.Empty;

        var trimmed = legacyValue.Trim();
        if (!catalogs.TryGetValue(catalogName, out var catalog))
            return Visa2014LookupResolveKind.Unmapped;

        if (catalog.LegacyToTarget.TryGetValue(trimmed, out var exact))
        {
            translatedTarget = exact;
            return Visa2014LookupResolveKind.ExactYaml;
        }

        foreach (var (legacy, target) in catalog.LegacyToTarget)
        {
            if (Visa2014CatalogMatchHelper.KeysEqual(legacy, trimmed))
            {
                translatedTarget = target;
                return Visa2014LookupResolveKind.NormalizedYaml;
            }
        }

        foreach (var target in catalog.LegacyToTarget.Values.Distinct(StringComparer.Ordinal))
        {
            if (Visa2014CatalogMatchHelper.KeysEqual(target, trimmed))
            {
                translatedTarget = target;
                return Visa2014LookupResolveKind.NormalizedYaml;
            }
        }

        if (catalog.IdentityPassThrough)
        {
            translatedTarget = trimmed;
            return Visa2014LookupResolveKind.IdentityPassThrough;
        }

        return Visa2014LookupResolveKind.Unmapped;
    }

    public static string ToSilentBucket(
        Visa2014LookupResolveKind kind,
        string? expectedTarget,
        string? documentedDefault)
    {
        return kind switch
        {
            Visa2014LookupResolveKind.Empty =>
                string.IsNullOrEmpty(expectedTarget) ? SilentBuckets.NullAllowed : SilentBuckets.DefaultApplied,
            Visa2014LookupResolveKind.ExactYaml => SilentBuckets.ExplicitYaml,
            Visa2014LookupResolveKind.NormalizedYaml => SilentBuckets.NormalizedYaml,
            Visa2014LookupResolveKind.IdentityPassThrough => SilentBuckets.IdentityPassthrough,
            Visa2014LookupResolveKind.Unmapped when
                !string.IsNullOrEmpty(documentedDefault) &&
                Visa2014CatalogMatchHelper.KeysEqual(expectedTarget, documentedDefault) =>
                SilentBuckets.DefaultApplied,
            Visa2014LookupResolveKind.Unmapped when !string.IsNullOrEmpty(expectedTarget) =>
                SilentBuckets.DefaultApplied,
            _ => SilentBuckets.NullAllowed,
        };
    }
}

internal static class SilentBuckets
{
    public const string ExplicitYaml = "explicit_yaml";
    public const string NormalizedYaml = "normalized_yaml";
    public const string IdentityPassthrough = "identity_passthrough";
    public const string DefaultApplied = "default_applied";
    public const string NullAllowed = "null_allowed";
    public const string ActualDefaultTolerated = "actual_default_tolerated";
    public const string ActualWithoutExpected = "actual_without_expected";
    public const string SkippedUnmapped = "skipped_unmapped";
    public const string Mismatch = "mismatch";

    public static bool IsUnexpectedFail(string bucket) =>
        string.Equals(bucket, ActualWithoutExpected, StringComparison.Ordinal);
}