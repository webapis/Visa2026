using System;
using System.Collections.Generic;
using System.Linq;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>Resolves migration SLA profiles and application types for deploy link sync.</summary>
internal static class ApplicationMigrationSlaProfileTypeLinkResolver
{
    internal sealed class ProfileIndex
    {
        public Dictionary<string, ApplicationMigrationSlaProfile> ByExactCode { get; init; } =
            new(StringComparer.OrdinalIgnoreCase);

        public Dictionary<string, ApplicationMigrationSlaProfile> ByNormalizedCode { get; init; } =
            new(StringComparer.OrdinalIgnoreCase);
    }

    internal static ProfileIndex BuildProfileIndex(IEnumerable<ApplicationMigrationSlaProfile> profiles)
    {
        var index = new ProfileIndex();
        foreach (var profile in profiles)
        {
            if (string.IsNullOrWhiteSpace(profile.Code))
                continue;

            if (!index.ByExactCode.ContainsKey(profile.Code))
                index.ByExactCode[profile.Code] = profile;

            var normalized = NormalizeProfileCode(profile.Code);
            if (normalized.Length > 0 && !index.ByNormalizedCode.ContainsKey(normalized))
                index.ByNormalizedCode[normalized] = profile;
        }

        return index;
    }

    internal static ApplicationMigrationSlaProfile? TryResolveProfile(ProfileIndex index, string? seedCode)
    {
        if (string.IsNullOrWhiteSpace(seedCode))
            return null;

        if (index.ByExactCode.TryGetValue(seedCode, out var profile))
            return profile;

        var normalized = NormalizeProfileCode(seedCode);
        if (normalized.Length > 0 && index.ByNormalizedCode.TryGetValue(normalized, out profile))
            return profile;

        return null;
    }

    internal static ApplicationType? TryResolveApplicationType(
        IReadOnlyList<ApplicationType> applicationTypes,
        ApplicationTypeConfigurationRow row) =>
        TryResolveApplicationType(applicationTypes, row.Name);

    internal static ApplicationType? TryResolveApplicationType(
        IReadOnlyList<ApplicationType> applicationTypes,
        string seedName)
    {
        if (applicationTypes.Count == 0 || string.IsNullOrWhiteSpace(seedName))
            return null;

        return applicationTypes.FirstOrDefault(t => KeyEquals(t.Name, seedName))
            ?? applicationTypes.FirstOrDefault(t => KeyEquals(t.LocalizationKey, seedName))
            ?? applicationTypes.FirstOrDefault(t => KeyEquals(t.Code, seedName));
    }

    internal static string NormalizeProfileCode(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return string.Empty;

        return string.Concat(code.Where(char.IsLetterOrDigit)).ToUpperInvariant();
    }

    private static bool KeyEquals(string? left, string? right) =>
        !string.IsNullOrWhiteSpace(left)
        && !string.IsNullOrWhiteSpace(right)
        && string.Equals(left.Trim(), right.Trim(), StringComparison.OrdinalIgnoreCase);
}
