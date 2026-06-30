using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Visa2026.DataImporter.Legacy.Visa2014;

internal sealed record Visa2014MigrationServiceInferenceResult(
    string? MigrationServiceNameTm,
    string Confidence,
    string Reason,
    bool UsedExpiredAddressFallback);

internal sealed class Visa2014MigrationServiceInferenceRules
{
    public string Version { get; set; } = "1";
    public bool ApprovedForPatch { get; set; }
    public List<RegionRule> RegionRules { get; set; } = [];
    public List<CityOverride> CityOverrides { get; set; } = [];

    public sealed class RegionRule
    {
        public string RegionMgCode { get; set; } = "";
        public string? MigrationServiceNameTm { get; set; }
        public string? LocalizationKey { get; set; }
        public string Confidence { get; set; } = "medium";
        public string? Reason { get; set; }
    }

    public sealed class CityOverride
    {
        public List<string>? CityMgCodes { get; set; }
        public string? CityNameContains { get; set; }
        public string MigrationServiceNameTm { get; set; } = "";
        public string? LocalizationKey { get; set; }
        public string Confidence { get; set; } = "high";
        public string? Notes { get; set; }
    }

    public static Visa2014MigrationServiceInferenceRules Load(string yamlPath)
    {
        if (!File.Exists(yamlPath))
            throw new FileNotFoundException("migration-service-inference.yaml not found.", yamlPath);

        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();

        return deserializer.Deserialize<Visa2014MigrationServiceInferenceRules>(File.ReadAllText(yamlPath))
               ?? throw new InvalidOperationException("migration-service-inference.yaml deserialized to null.");
    }

    public static string ResolveRulesPath(string? solutionRoot)
    {
        if (solutionRoot == null)
            throw new InvalidOperationException("Could not locate solution root for migration-service-inference.yaml.");

        return Path.Combine(
            solutionRoot,
            "docs",
            "VISA2014_MIGRATION",
            "migration-service-inference.yaml");
    }

    public Visa2014MigrationServiceInferenceResult Infer(
        string? regionMgCode,
        string? regionName,
        string? cityMgCode,
        string? cityName,
        bool usedExpiredAddressFallback)
    {
        if (string.IsNullOrWhiteSpace(regionMgCode))
        {
            return new Visa2014MigrationServiceInferenceResult(
                null,
                "none",
                string.IsNullOrWhiteSpace(cityMgCode) && string.IsNullOrWhiteSpace(cityName)
                    ? "No region on current address"
                    : "Region mgCode missing on current address",
                usedExpiredAddressFallback);
        }

        var regionCode = regionMgCode.Trim();

        foreach (var cityOverride in CityOverrides ?? [])
        {
            if (cityOverride.CityMgCodes is { Count: > 0 } mgCodes &&
                !string.IsNullOrWhiteSpace(cityMgCode) &&
                mgCodes.Any(code => code.Equals(cityMgCode.Trim(), StringComparison.OrdinalIgnoreCase)))
            {
                return Finish(cityOverride.MigrationServiceNameTm, cityOverride.Confidence,
                    $"City override ({cityMgCode.Trim()})", usedExpiredAddressFallback);
            }

            if (!string.IsNullOrWhiteSpace(cityOverride.CityNameContains) &&
                !string.IsNullOrWhiteSpace(cityName) &&
                Visa2014CatalogMatchHelper.NormalizeKey(cityName)
                    .Contains(cityOverride.CityNameContains.Trim(), StringComparison.Ordinal))
            {
                return Finish(cityOverride.MigrationServiceNameTm, cityOverride.Confidence,
                    $"City name contains '{cityOverride.CityNameContains}'", usedExpiredAddressFallback);
            }
        }

        var regionRule = (RegionRules ?? []).FirstOrDefault(r =>
            r.RegionMgCode.Equals(regionCode, StringComparison.OrdinalIgnoreCase));

        if (regionRule == null)
        {
            return new Visa2014MigrationServiceInferenceResult(
                null,
                "none",
                $"Unknown region mgCode '{regionCode}'",
                usedExpiredAddressFallback);
        }

        if (string.IsNullOrWhiteSpace(regionRule.MigrationServiceNameTm))
        {
            return new Visa2014MigrationServiceInferenceResult(
                null,
                regionRule.Confidence,
                regionRule.Reason ?? $"Region {regionCode} has no MigrationService catalog row",
                usedExpiredAddressFallback);
        }

        var regionLabel = string.IsNullOrWhiteSpace(regionName) ? regionCode : regionName.Trim();
        var citySuffix = string.IsNullOrWhiteSpace(cityMgCode) && string.IsNullOrWhiteSpace(cityName)
            ? "region-only"
            : $"city={cityMgCode ?? cityName?.Trim() ?? "?"}";
        return Finish(
            regionRule.MigrationServiceNameTm,
            regionRule.Confidence,
            $"Regional office from {regionLabel} ({citySuffix})",
            usedExpiredAddressFallback);
    }

    private static Visa2014MigrationServiceInferenceResult Finish(
        string migrationServiceNameTm,
        string confidence,
        string reason,
        bool usedExpiredAddressFallback)
    {
        var effectiveConfidence = confidence;
        if (usedExpiredAddressFallback && !string.Equals(confidence, "none", StringComparison.OrdinalIgnoreCase))
        {
            effectiveConfidence = confidence.Equals("high", StringComparison.OrdinalIgnoreCase)
                ? "medium"
                : "low";
            reason += "; expired-only address fallback";
        }

        return new Visa2014MigrationServiceInferenceResult(
            migrationServiceNameTm,
            effectiveConfidence,
            reason,
            usedExpiredAddressFallback);
    }
}

internal sealed record Visa2014AddressForInference(
    Guid LegacyOid,
    string? RegionMgCode,
    string? RegionName,
    string? CityMgCode,
    string? CityName,
    DateTime? ExpirationDate);

internal static class Visa2014MigrationServiceAddressPicker
{
    /// <summary>
    /// Mirrors <see cref="Visa2026.Module.BusinessObjects.PersonCurrentItems.GetCurrentAddressOfResidence"/>.
    /// </summary>
    public static Visa2014AddressForInference? PickCurrent(
        IReadOnlyList<Visa2014AddressForInference> addresses,
        DateTime? asOf,
        out bool usedExpiredFallback)
    {
        usedExpiredFallback = false;
        if (addresses.Count == 0)
            return null;

        var asOfDate = (asOf ?? DateTime.Today).Date;
        var stillValid = addresses
            .Where(a => !a.ExpirationDate.HasValue || a.ExpirationDate.Value.Date >= asOfDate)
            .ToList();

        if (stillValid.Count > 0)
        {
            return stillValid
                .OrderByDescending(a => a.ExpirationDate?.Date ?? DateTime.MaxValue)
                .ThenByDescending(a => a.LegacyOid)
                .FirstOrDefault();
        }

        usedExpiredFallback = true;
        return addresses
            .OrderByDescending(a => a.ExpirationDate?.Date ?? DateTime.MinValue)
            .ThenByDescending(a => a.LegacyOid)
            .FirstOrDefault();
    }
}
