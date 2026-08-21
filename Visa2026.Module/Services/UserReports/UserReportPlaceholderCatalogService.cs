namespace Visa2026.Module.Services.UserReports;

using Visa2026.Module.BusinessObjects;

public interface IUserReportPlaceholderCatalogService
{
    IReadOnlyList<UserReportPlaceholderCatalogEntry> GetEntries(UserReportPlaceholderManualQuery? query = null);

    string ResolveCanonicalPropertyPath(string propertyPath);
}

public sealed class UserReportPlaceholderCatalogService : IUserReportPlaceholderCatalogService
{
    private readonly Lazy<IReadOnlyList<UserReportPlaceholderCatalogEntry>> _entries;

    public UserReportPlaceholderCatalogService()
    {
        _entries = new Lazy<IReadOnlyList<UserReportPlaceholderCatalogEntry>>(LoadEntries);
    }

    public IReadOnlyList<UserReportPlaceholderCatalogEntry> GetEntries(UserReportPlaceholderManualQuery? query = null)
    {
        var all = _entries.Value;
        if (query == null)
            return all;

        IEnumerable<UserReportPlaceholderCatalogEntry> filtered = all;

        if (query.RootBoType is UserReportBoType rootBoType)
            filtered = filtered.Where(e => e.RootBoTypes.Contains(rootBoType));

        if (query.Scope is UserReportPlaceholderScope scope)
        {
            filtered = filtered.Where(e =>
                e.Scope == UserReportPlaceholderScope.Both
                || e.Scope == scope);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            filtered = filtered.Where(e =>
                e.ShortCode.Contains(term, StringComparison.OrdinalIgnoreCase)
                || e.CanonicalPath.Contains(term, StringComparison.OrdinalIgnoreCase)
                || e.LabelEn.Contains(term, StringComparison.OrdinalIgnoreCase)
                || e.LabelTk.Contains(term, StringComparison.OrdinalIgnoreCase)
                || e.LabelRu.Contains(term, StringComparison.OrdinalIgnoreCase)
                || e.LabelTr.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        return filtered
            .OrderBy(e => e.Scope)
            .ThenBy(e => e.ShortCode, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public string ResolveCanonicalPropertyPath(string propertyPath) =>
        UserReportPlaceholderAliasRegistry.ResolveCanonicalPropertyPath(propertyPath);

    private static IReadOnlyList<UserReportPlaceholderCatalogEntry> LoadEntries()
    {
        var file = UserReportPlaceholderCatalogLoader.Load();
        var list = new List<UserReportPlaceholderCatalogEntry>(file.Entries.Count);

        foreach (var dto in file.Entries)
        {
            if (string.IsNullOrWhiteSpace(dto.ShortCode) || string.IsNullOrWhiteSpace(dto.CanonicalPath))
                continue;

            list.Add(new UserReportPlaceholderCatalogEntry
            {
                ShortCode = dto.ShortCode.Trim(),
                CanonicalPath = dto.CanonicalPath.Trim(),
                Scope = ParseScope(dto.Scopes),
                RootBoTypes = ParseRootBoTypes(dto.RootBoTypes),
                ExampleValue = dto.ExampleValue?.Trim() ?? string.Empty,
                LabelEn = GetLabel(dto.Labels, "en"),
                LabelTk = GetLabel(dto.Labels, "tk-TM"),
                LabelRu = GetLabel(dto.Labels, "ru-RU"),
                LabelTr = GetLabel(dto.Labels, "tr-TR"),
                IsImage = dto.IsImage,
                Pack = ParsePack(dto.PackKey),
            });
        }

        return list;
    }

    /// <summary>
    /// An unrecognised or missing <c>packKey</c> stays <see cref="UserReportPlaceholderPack.Unknown"/> so a typo
    /// excludes the token from profile-scoped sets rather than leaking it into every profile.
    /// </summary>
    private static UserReportPlaceholderPack ParsePack(string? packKey)
    {
        if (string.IsNullOrWhiteSpace(packKey))
            return UserReportPlaceholderPack.Unknown;

        return Enum.TryParse<UserReportPlaceholderPack>(packKey.Trim(), ignoreCase: true, out var parsed)
            ? parsed
            : UserReportPlaceholderPack.Unknown;
    }

    private static UserReportPlaceholderScope ParseScope(IReadOnlyList<string>? scopes)
    {
        if (scopes == null || scopes.Count == 0)
            return UserReportPlaceholderScope.Both;

        var hasHeader = scopes.Any(s => string.Equals(s, "Header", StringComparison.OrdinalIgnoreCase));
        var hasRow = scopes.Any(s => string.Equals(s, "Row", StringComparison.OrdinalIgnoreCase));
        if (hasHeader && hasRow)
            return UserReportPlaceholderScope.Both;
        if (hasRow)
            return UserReportPlaceholderScope.Row;
        return UserReportPlaceholderScope.Header;
    }

    private static IReadOnlyList<UserReportBoType> ParseRootBoTypes(IReadOnlyList<string>? rootBoTypes)
    {
        if (rootBoTypes == null || rootBoTypes.Count == 0)
            return [UserReportBoType.ApplicationProfileInstance, UserReportBoType.ApplicationItem];

        var list = new List<UserReportBoType>();
        foreach (var name in rootBoTypes)
        {
            if (Enum.TryParse<UserReportBoType>(name, ignoreCase: true, out var parsed))
                list.Add(parsed);
        }

        return list.Count > 0 ? list : [UserReportBoType.ApplicationProfileInstance, UserReportBoType.ApplicationItem];
    }

    private static string GetLabel(IReadOnlyDictionary<string, string>? labels, string key)
    {
        if (labels == null)
            return string.Empty;

        return labels.TryGetValue(key, out var value) ? value : string.Empty;
    }
}