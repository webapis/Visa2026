using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.DatabaseUpdate;

public enum ApplicationProfileCatalogGranularity
{
    TypeOnly,
    TypeAndContract,
}

/// <summary>
/// Wave 0b — profile catalog key: ApplicationType only (direct migration / via-ministry without contract)
/// or ApplicationType + ProjectContract (via-ministry with legacy contract FK).
/// </summary>
public readonly record struct ApplicationProfileCatalogGroupKey(
    string ApplicationTypeName,
    string? ProjectContractCode,
    ApplicationProfileCatalogGranularity Granularity)
{
    public string CatalogKey => BuildCatalogKey(ApplicationTypeName, ProjectContractCode);

    public static string BuildCatalogKey(string applicationTypeName, string? projectContractCode) =>
        string.IsNullOrWhiteSpace(projectContractCode)
            ? applicationTypeName.Trim()
            : $"{applicationTypeName.Trim()}|{projectContractCode.Trim()}";

    public static string BuildMatchKey(string profileCode, string? projectContractCode) =>
        string.IsNullOrWhiteSpace(projectContractCode)
            ? profileCode.Trim()
            : $"{profileCode.Trim()}|{projectContractCode.Trim()}";

    public static string BuildContractVariantDisplaySuffix(string legacyContractCode) =>
        $"({legacyContractCode.Trim()})";

    public static bool ProfileMatchesLegacyContract(
        ApplicationProfile profile,
        Guid? resolvedContractId,
        string? legacyContractCode)
    {
        if (string.IsNullOrWhiteSpace(legacyContractCode))
        {
            return profile.DefaultProjectContractId == null
                   && !NameLooksLikeContractVariant(profile.Name);
        }

        if (resolvedContractId.HasValue && profile.DefaultProjectContractId == resolvedContractId)
            return true;

        if (profile.DefaultProjectContract != null
            && ProjectContractTitleMatches(profile.DefaultProjectContract.NameTm, legacyContractCode))
            return true;

        var suffix = BuildContractVariantDisplaySuffix(legacyContractCode);
        return profile.Name?.Contains(suffix, StringComparison.OrdinalIgnoreCase) == true;
    }

    public static bool NameLooksLikeContractVariant(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return false;

        var trimmed = name.TrimEnd();
        if (!trimmed.EndsWith(")", StringComparison.Ordinal))
            return false;

        var open = trimmed.LastIndexOf('(');
        if (open < 0 || open >= trimmed.Length - 2)
            return false;

        var inner = trimmed.Substring(open + 1, trimmed.Length - open - 2).Trim();
        if (inner.Length == 0)
            return false;

        // Contract suffix from Wave 0b is a short code (e.g. "Şatlyk-1"), not a Turkmen phrase with spaces.
        return inner.Length <= 48 && inner.IndexOf(' ') < 0;
    }

    public static ApplicationProfile? FindProfile(
        IEnumerable<ApplicationProfile> profiles,
        string profileCode,
        Guid? resolvedContractId,
        string? legacyContractCode)
    {
        var code = profileCode.Trim();
        var list = profiles as IList<ApplicationProfile> ?? profiles.ToList();
        var exact = list.FirstOrDefault(p =>
            string.Equals(p.Code, code, StringComparison.OrdinalIgnoreCase)
            && ProfileMatchesLegacyContract(p, resolvedContractId, legacyContractCode));
        if (exact != null)
            return exact;

        // Type-only tenant catalogs (no Wave 0b contract clones): bind the shared type profile
        // when a legacy via-ministry row has a ProjectContract but no matching variant exists.
        if (string.IsNullOrWhiteSpace(legacyContractCode))
            return null;

        return list.FirstOrDefault(p =>
            string.Equals(p.Code, code, StringComparison.OrdinalIgnoreCase)
            && ProfileMatchesLegacyContract(p, resolvedContractId: null, legacyContractCode: null));
    }

    private static bool ProjectContractTitleMatches(string? nameTm, string legacyCode)
    {
        if (string.IsNullOrWhiteSpace(nameTm))
            return false;

        var title = nameTm.Trim();
        var code = legacyCode.Trim();
        return title.StartsWith(code, StringComparison.OrdinalIgnoreCase)
               || string.Equals(title, code, StringComparison.OrdinalIgnoreCase);
    }
}

public static class ApplicationProfileCatalogGrouping
{
    public static bool TryResolveGroupKey(
        string? applicationTypeName,
        string? projectContractCode,
        out ApplicationProfileCatalogGroupKey groupKey)
    {
        groupKey = default;
        if (string.IsNullOrWhiteSpace(applicationTypeName))
            return false;

        if (!ApplicationProfileCatalogPreviewHelper.TryBuildProfileEntity(
                applicationTypeName.Trim(),
                out var profile,
                out _)
            || profile == null)
            return false;

        var hasContract = !string.IsNullOrWhiteSpace(projectContractCode);
        if (profile.ProgressRoute == ApplicationProfileInstanceProgressRouteKind.ViaMinistries && hasContract)
        {
            groupKey = new ApplicationProfileCatalogGroupKey(
                applicationTypeName.Trim(),
                projectContractCode!.Trim(),
                ApplicationProfileCatalogGranularity.TypeAndContract);
        }
        else
        {
            groupKey = new ApplicationProfileCatalogGroupKey(
                applicationTypeName.Trim(),
                null,
                ApplicationProfileCatalogGranularity.TypeOnly);
        }

        return true;
    }

    public static IReadOnlyList<ApplicationProfileCatalogGroupKey> DistinctGroupKeysFromImportRows(
        IEnumerable<IReadOnlyDictionary<string, object?>> importRows)
    {
        var keys = new Dictionary<string, ApplicationProfileCatalogGroupKey>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in importRows)
        {
            var typeName = row.GetValueOrDefault("ApplicationType") as string;
            var contractCode = row.GetValueOrDefault("ProjectContract") as string;
            if (!TryResolveGroupKey(typeName, contractCode, out var key))
                continue;

            keys.TryAdd(key.CatalogKey, key);
        }

        return keys.Values
            .OrderBy(k => k.ApplicationTypeName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(k => k.ProjectContractCode ?? string.Empty, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
