using Visa2026.Module.Services.ReportDashboard;
using Xunit;

namespace Visa2026.Module.Tests.Services;

/// <summary>
/// Save() permission and early validation gates for company invitation Excel templates.
/// Outside an XAF security ValueManager, administrator check fails closed (AdminOnly).
/// </summary>
public class ReportDashboardExcelTemplateServiceSaveGateTests
{
    [Fact]
    public void Save_outside_security_context_returns_AdminOnly()
    {
        // Non-admin / missing SecuritySystem must never store a template.
        var key = ReportDashboardExcelTemplateService.Save(
            objectSpace: null!,
            category: ReportDashboardCategory.Invitation,
            subReport: "ready-by-project",
            fileName: "ready.xlsx",
            content: new byte[] { 0x50, 0x4B },
            expectedColumns: 8);

        Assert.Equal(ReportDashboardExcelTemplateService.AdminOnlyKey, key);
    }
}
