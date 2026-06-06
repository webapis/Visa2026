using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.UserReports;

namespace Visa2026.Module.Services.WordReports;

/// <summary>Scope and optional selected application lines for Resminamalar generation.</summary>
public sealed class WordReportGenerationContext
{
    public WordReportPackageScope Scope { get; init; } = WordReportPackageScope.Application;

    public IReadOnlyList<Guid> SelectedApplicationItemIds { get; init; } = Array.Empty<Guid>();

    public static WordReportGenerationContext ForApplication() =>
        new() { Scope = WordReportPackageScope.Application };

    public static WordReportGenerationContext ForApplicationItems(IReadOnlyList<Guid> applicationItemIds) =>
        new()
        {
            Scope = WordReportPackageScope.ApplicationItem,
            SelectedApplicationItemIds = applicationItemIds ?? Array.Empty<Guid>()
        };

    public IList<ApplicationItem> ResolveApplicationItems(IObjectSpace objectSpace, Application application)
    {
        var activeItems = UserReportMergeDataHelper.GetActiveApplicationItems(objectSpace, application);
        if (Scope != WordReportPackageScope.ApplicationItem || SelectedApplicationItemIds.Count == 0)
            return activeItems;

        var selectedIds = SelectedApplicationItemIds.ToHashSet();
        var byId = activeItems
            .Where(item => selectedIds.Contains(item.ID))
            .ToDictionary(item => item.ID);

        return SelectedApplicationItemIds
            .Where(byId.ContainsKey)
            .Select(id => byId[id])
            .ToList();
    }
}
