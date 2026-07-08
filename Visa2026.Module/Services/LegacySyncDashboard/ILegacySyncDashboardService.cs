namespace Visa2026.Module.Services.LegacySyncDashboard;

public interface ILegacySyncDashboardService
{
    LegacySyncDashboardSnapshot GetSnapshot();

    LegacySyncDashboardRefreshResult RefreshSnapshot();

    LegacySyncDashboardFileContent GetDashboardHtmlFile();

    LegacySyncDashboardFileContent GetDashboardJsonFile();
}