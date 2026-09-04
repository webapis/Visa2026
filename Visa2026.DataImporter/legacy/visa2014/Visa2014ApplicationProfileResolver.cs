using Bo = Visa2026.Module.BusinessObjects;
using Visa2026.Module.DatabaseUpdate;

namespace Visa2026.DataImporter.Legacy.Visa2014;

internal static class Visa2014ApplicationProfileResolver
{
    public static string? ResolveProfileCode(string? applicationTypeName, IReadOnlyList<ApplicationType> applicationTypes)
    {
        if (string.IsNullOrWhiteSpace(applicationTypeName))
            return null;

        var type = applicationTypes.FirstOrDefault(t =>
            string.Equals(t.Name, applicationTypeName.Trim(), StringComparison.OrdinalIgnoreCase));
        if (type == null)
            return null;

        var boType = new Bo.ApplicationType
        {
            Name = type.Name,
            Code = type.Code ?? string.Empty,
        };
        return ApplicationProfileFromApplicationTypeMapper.ResolveProfileCode(boType);
    }

    public static string? ResolveProfileCode(ApplicationType applicationType)
    {
        if (applicationType == null)
            return null;

        var boType = new Bo.ApplicationType
        {
            Name = applicationType.Name,
            Code = applicationType.Code ?? string.Empty,
        };
        return ApplicationProfileFromApplicationTypeMapper.ResolveProfileCode(boType);
    }

    public static string? ResolveProfileCode(Bo.ApplicationType applicationType) =>
        applicationType == null ? null : ApplicationProfileFromApplicationTypeMapper.ResolveProfileCode(applicationType);

    public static string? ResolveProfileCodeByTypeName(string? applicationTypeName)
    {
        if (!ApplicationProfileCatalogPreviewHelper.TryBuild(applicationTypeName, out var preview))
            return null;

        return preview.ProfileCode;
    }

    public static Guid? FindProfileId(
        IReadOnlyList<ApplicationProfile> profiles,
        IReadOnlyList<ProjectContract> contracts,
        string? applicationTypeName,
        string? projectContractCode)
    {
        if (string.IsNullOrWhiteSpace(applicationTypeName))
            return null;

        if (!ApplicationProfileCatalogGrouping.TryResolveGroupKey(
                applicationTypeName,
                projectContractCode,
                out var groupKey))
            return null;

        var profileCode = ResolveProfileCodeByTypeName(applicationTypeName);
        if (string.IsNullOrWhiteSpace(profileCode))
            return null;

        var contract = ResolveProjectContractDto(contracts, groupKey.ProjectContractCode);
        var contractId = contract?.Id;
        var code = profileCode.Trim();

        foreach (var profile in profiles)
        {
            if (!string.Equals(profile.Code, code, StringComparison.OrdinalIgnoreCase))
                continue;

            if (ProfileDtoMatchesLegacyContract(profile, contracts, contractId, groupKey.ProjectContractCode))
                return profile.Id;
        }

        // Type-only tenant catalogs: fall back to the shared type profile (no DefaultProjectContract).
        if (string.IsNullOrWhiteSpace(groupKey.ProjectContractCode))
            return null;

        foreach (var profile in profiles)
        {
            if (!string.Equals(profile.Code, code, StringComparison.OrdinalIgnoreCase))
                continue;

            if (ProfileDtoMatchesLegacyContract(profile, contracts, resolvedContractId: null, legacyContractCode: null))
                return profile.Id;
        }

        return null;
    }

    public static Bo.ApplicationProfile? FindProfile(
        IEnumerable<Bo.ApplicationProfile> profiles,
        IReadOnlyList<Bo.ProjectContract> contracts,
        string? applicationTypeName,
        string? projectContractCode)
    {
        if (string.IsNullOrWhiteSpace(applicationTypeName))
            return null;

        if (!ApplicationProfileCatalogGrouping.TryResolveGroupKey(
                applicationTypeName,
                projectContractCode,
                out var groupKey))
            return null;

        var profileCode = ResolveProfileCodeByTypeName(applicationTypeName);
        if (string.IsNullOrWhiteSpace(profileCode))
            return null;

        var contract = ResolveProjectContract(contracts, groupKey.ProjectContractCode);
        var contractId = contract?.ID;

        return ApplicationProfileCatalogGroupKey.FindProfile(
            profiles,
            profileCode.Trim(),
            contractId,
            groupKey.ProjectContractCode);
    }

    private static bool ProfileDtoMatchesLegacyContract(
        ApplicationProfile profile,
        IReadOnlyList<ProjectContract> contracts,
        Guid? resolvedContractId,
        string? legacyContractCode)
    {
        if (string.IsNullOrWhiteSpace(legacyContractCode))
        {
            return profile.DefaultProjectContractId == null
                   && !ApplicationProfileCatalogGroupKey.NameLooksLikeContractVariant(profile.Name);
        }

        if (resolvedContractId.HasValue && profile.DefaultProjectContractId == resolvedContractId)
            return true;

        if (profile.DefaultProjectContractId.HasValue)
        {
            var linked = contracts.FirstOrDefault(c => c.Id == profile.DefaultProjectContractId.Value);
            if (linked != null
                && (ProjectContractTitleMatches(linked.NameTm, legacyContractCode)
                    || KeysEqual(linked.Code, legacyContractCode)))
                return true;
        }

        var suffix = ApplicationProfileCatalogGroupKey.BuildContractVariantDisplaySuffix(legacyContractCode);
        return profile.Name?.Contains(suffix, StringComparison.OrdinalIgnoreCase) == true;
    }

    private static ProjectContract? ResolveProjectContractDto(
        IReadOnlyList<ProjectContract> contracts,
        string? legacyContractCode)
    {
        if (string.IsNullOrWhiteSpace(legacyContractCode))
            return null;

        var code = legacyContractCode.Trim();
        var matches = contracts
            .Where(c => ProjectContractTitleMatches(c.NameTm, code)
                        || KeysEqual(c.Code, code))
            .ToList();

        if (matches.Count == 0)
            return null;

        if (matches.Count == 1)
            return matches[0];

        return matches.FirstOrDefault(c =>
                   c.NameTm.Contains("2 ylalaşyk", StringComparison.OrdinalIgnoreCase)
                   || c.NameTm.Contains("2 ylalasyk", StringComparison.OrdinalIgnoreCase))
               ?? matches[0];
    }

    private static Bo.ProjectContract? ResolveProjectContract(
        IReadOnlyList<Bo.ProjectContract> contracts,
        string? legacyContractCode)
    {
        if (string.IsNullOrWhiteSpace(legacyContractCode))
            return null;

        var code = legacyContractCode.Trim();
        var matches = contracts
            .Where(c => ProjectContractTitleMatches(c.NameTm, code)
                        || KeysEqual(c.Code, code))
            .ToList();

        if (matches.Count == 0)
            return null;

        if (matches.Count == 1)
            return matches[0];

        return matches.FirstOrDefault(c =>
                   c.NameTm.Contains("2 ylalaşyk", StringComparison.OrdinalIgnoreCase)
                   || c.NameTm.Contains("2 ylalasyk", StringComparison.OrdinalIgnoreCase))
               ?? matches[0];
    }

    private static bool ProjectContractTitleMatches(string? nameTm, string legacyCode)
    {
        if (string.IsNullOrWhiteSpace(nameTm))
            return false;

        var title = nameTm.Trim();
        var code = legacyCode.Trim();
        return title.StartsWith(code, StringComparison.OrdinalIgnoreCase)
               || KeysEqual(title, code);
    }

    private static bool KeysEqual(string? left, string? right) =>
        string.Equals(left?.Trim(), right?.Trim(), StringComparison.OrdinalIgnoreCase);
}
