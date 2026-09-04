using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.UserReports;

namespace Visa2026.Module.Services.WordReports;

/// <summary>Scope and optional selected roster person rows for Resminamalar generation.</summary>
public sealed class WordReportGenerationContext
{
    public WordReportPackageScope Scope { get; init; } = WordReportPackageScope.ApplicationProfileInstance;

    /// <summary>Selected Person IDs on the application (merge-line projection IDs).</summary>
    public IReadOnlyList<Guid> SelectedRosterPersonIds { get; init; } = Array.Empty<Guid>();

    /// <summary>Obsolete alias for <see cref="SelectedRosterPersonIds"/>.</summary>
    public IReadOnlyList<Guid> SelectedApplicationItemIds
    {
        get => SelectedRosterPersonIds;
        init => SelectedRosterPersonIds = value ?? Array.Empty<Guid>();
    }

    public static WordReportGenerationContext ForApplication() =>
        new() { Scope = WordReportPackageScope.ApplicationProfileInstance };

    public static WordReportGenerationContext ForRosterPersons(IReadOnlyList<Guid> rosterPersonIds) =>
        new()
        {
            Scope = WordReportPackageScope.RosterPerson,
            SelectedRosterPersonIds = rosterPersonIds ?? Array.Empty<Guid>()
        };

    /// <summary>Obsolete alias for <see cref="ForRosterPersons"/>.</summary>
    public static WordReportGenerationContext ForApplicationItems(IReadOnlyList<Guid> applicationItemIds) =>
        ForRosterPersons(applicationItemIds);

    public IList<ApplicationRosterMergeLine> ResolveApplicationItems(IObjectSpace objectSpace, ApplicationProfileInstance application)
    {
        var activeItems = UserReportMergeDataHelper.GetActiveApplicationItems(objectSpace, application);
        if (Scope != WordReportPackageScope.RosterPerson || SelectedRosterPersonIds.Count == 0)
            return activeItems;

        var selectedIds = SelectedRosterPersonIds.ToHashSet();
        var byId = activeItems
            .Where(item => selectedIds.Contains(item.ID))
            .ToDictionary(item => item.ID);

        return SelectedRosterPersonIds
            .Where(byId.ContainsKey)
            .Select(id => byId[id])
            .ToList();
    }
}