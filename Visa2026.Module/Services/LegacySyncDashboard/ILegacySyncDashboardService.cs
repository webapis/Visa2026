namespace Visa2026.Module.Services.LegacySyncDashboard;

public interface ILegacySyncDashboardService
{
    LegacySyncDashboardSnapshot GetSnapshot();

    LegacySyncDashboardFileContent GetDashboardHtmlFile();

    LegacySyncDashboardFileContent GetDashboardJsonFile();
}
