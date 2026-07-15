using DevExpress.ExpressApp;

namespace Visa2026.Module.Services.ReportDashboard;

public interface IReportDashboardQueryService
{
    ReportDashboardSnapshot LoadSnapshot(IObjectSpace objectSpace, int dateRangeMonths = 6);

    ReportDashboardPanelData LoadPanel(
        IObjectSpace objectSpace,
        ReportDashboardPersonType personType,
        ReportDashboardCategory category,
        string projectKey,
        int dateRangeMonths = 6,
        string subReport = "default",
        bool includeArchivedPersons = false);
}