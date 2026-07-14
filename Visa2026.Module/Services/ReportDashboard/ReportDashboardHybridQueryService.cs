using System.Collections.Generic;
using DevExpress.ExpressApp;

namespace Visa2026.Module.Services.ReportDashboard;

/// <summary>
/// Delegates per-category to either the real EF query service or the mock.
/// Promote a category by adding it to <see cref="RealCategories"/> once its
/// queries are verified in the target environment.
/// </summary>
public sealed class ReportDashboardHybridQueryService : IReportDashboardQueryService
{
    /// <summary>
    /// Categories that have been promoted to real EF queries.
    /// Add each category here one at a time as it is verified.
    /// </summary>
    private static readonly HashSet<ReportDashboardCategory> RealCategories =
    [
        ReportDashboardCategory.Registration,
        ReportDashboardCategory.Passport,
        ReportDashboardCategory.WorkPermit,
        ReportDashboardCategory.BorderZone,
        ReportDashboardCategory.Invitation,
        ReportDashboardCategory.Travel,
        ReportDashboardCategory.VisaExtension,
    ];

    private readonly ReportDashboardQueryService _real;
    private readonly ReportDashboardMockQueryService _mock;

    public ReportDashboardHybridQueryService(
        ReportDashboardQueryService real,
        ReportDashboardMockQueryService mock)
    {
        _real = real;
        _mock = mock;
    }

    /// <summary>
    /// Snapshot always uses the real service so counts are accurate.
    /// </summary>
    public ReportDashboardSnapshot LoadSnapshot(IObjectSpace objectSpace, int dateRangeMonths = 6)
        => _real.LoadSnapshot(objectSpace, dateRangeMonths);

    public ReportDashboardPanelData LoadPanel(
        IObjectSpace objectSpace,
        ReportDashboardPersonType personType,
        ReportDashboardCategory category,
        string projectKey,
        int dateRangeMonths = 6,
        string subReport = "default")
    {
        IReportDashboardQueryService service = RealCategories.Contains(category) ? _real : _mock;
        return service.LoadPanel(objectSpace, personType, category, projectKey, dateRangeMonths, subReport);
    }
}