using System.Security.Cryptography;
using System.Text;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.UserReports;

#nullable enable

namespace Visa2026.Module.Services.TemplateConvert;

/// <summary>
/// The set of placeholders a given Application Profile may use, so template conversion can never
/// offer a token the profile cannot fill (L10, Q1, Q13).
/// </summary>
public interface IApplicationProfilePlaceholderSetService
{
    ApplicationProfilePlaceholderSet GetSet(ApplicationProfilePlaceholderSetQuery query);
}

/// <inheritdoc cref="IApplicationProfilePlaceholderSetService"/>
public sealed class ApplicationProfilePlaceholderSetService : IApplicationProfilePlaceholderSetService
{
    private readonly IUserReportPlaceholderCatalogService _catalog;

    public ApplicationProfilePlaceholderSetService(IUserReportPlaceholderCatalogService catalog)
    {
        _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
    }

    public ApplicationProfilePlaceholderSet GetSet(ApplicationProfilePlaceholderSetQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(query.Profile);

        var allowed = new List<UserReportPlaceholderCatalogEntry>();
        var excluded = new List<PlaceholderExclusion>();

        foreach (var entry in _catalog.GetEntries())
        {
            var reason = Evaluate(entry, query);
            if (reason == null)
                allowed.Add(entry);
            else
                excluded.Add(new PlaceholderExclusion(entry.ShortCode, reason.Value));
        }

        return new ApplicationProfilePlaceholderSet
        {
            ApplicationProfileId = query.Profile.ID,
            DataScope = query.DataScope,
            TemplateKind = query.TemplateKind,
            Allowed = allowed
                .OrderBy(static e => e.Scope)
                .ThenBy(static e => e.ShortCode, StringComparer.OrdinalIgnoreCase)
                .ToList(),
            Excluded = excluded
                .OrderBy(static e => e.ShortCode, StringComparer.OrdinalIgnoreCase)
                .ToList(),
            Fingerprint = ComputeFingerprint(allowed),
        };
    }

    private static PlaceholderExclusionReason? Evaluate(
        UserReportPlaceholderCatalogEntry entry,
        ApplicationProfilePlaceholderSetQuery query)
    {
        if (!IsInDataScope(entry.Scope, query.DataScope))
            return PlaceholderExclusionReason.OutOfDataScope;

        if (!IsSupportedForKind(entry, query.TemplateKind))
            return PlaceholderExclusionReason.StructuralUnsupportedForKind;

        if (entry.Pack == UserReportPlaceholderPack.Unknown)
            return PlaceholderExclusionReason.UnknownPack;

        if (!ApplicationProfilePlaceholderPackMap.IsEnabled(query.Profile, entry.Pack))
            return PlaceholderExclusionReason.PersonPackDisabled;

        return null;
    }

    private static bool IsInDataScope(UserReportPlaceholderScope entryScope, ApplicationProfileTemplateDataScope dataScope)
    {
        if (entryScope == UserReportPlaceholderScope.Both)
            return true;

        return dataScope switch
        {
            ApplicationProfileTemplateDataScope.ApplicationHeader => entryScope == UserReportPlaceholderScope.Header,
            ApplicationProfileTemplateDataScope.PeopleM2M => entryScope == UserReportPlaceholderScope.Row,
            ApplicationProfileTemplateDataScope.Both => true,
            _ => false,
        };
    }

    private static bool IsSupportedForKind(UserReportPlaceholderCatalogEntry entry, ApplicationProfileTemplateKind kind) =>
        kind switch
        {
            ApplicationProfileTemplateKind.Word => true,
            ApplicationProfileTemplateKind.Excel => !entry.IsImage,
            _ => false,
        };

    private static string ComputeFingerprint(IEnumerable<UserReportPlaceholderCatalogEntry> allowed)
    {
        var codes = allowed
            .Select(static e => e.ShortCode)
            .OrderBy(static c => c, StringComparer.Ordinal)
            .ToList();

        var payload = string.Join("\n", codes);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload)));
    }
}
