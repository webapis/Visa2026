using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ReportDashboard;
using Xunit;

namespace Visa2026.Module.Tests.Services.ReportDashboard;

/// <summary>
/// Dashboard list labels prefer Application Profile name/code; ApplicationType is dual-read fallback only.
/// </summary>
public sealed class ReportDashboardProfileInstanceQueryTests
{
    [Fact]
    public void ProfileLabelOrMissing_prefers_profile_name()
    {
        var application = new ApplicationProfileInstance
        {
            ApplicationProfile = new ApplicationProfile { Name = "  Check-in  ", Code = "check_in" },
            ApplicationType = new ApplicationType { NameTm = "Type TM", Name = "TypeEn" },
        };

        Assert.Equal(
            "Check-in",
            ReportDashboardProfileInstanceQuery.ProfileLabelOrMissing(application, "—"));
    }

    [Fact]
    public void ProfileLabelOrMissing_uses_profile_code_when_name_blank()
    {
        var application = new ApplicationProfileInstance
        {
            ApplicationProfile = new ApplicationProfile { Name = "  ", Code = " extend_visa " },
            ApplicationType = new ApplicationType { NameTm = "ShouldNotWin", Name = "AlsoNot" },
        };

        Assert.Equal(
            "extend_visa",
            ReportDashboardProfileInstanceQuery.ProfileLabelOrMissing(application, "—"));
    }

    [Fact]
    public void ProfileLabelOrMissing_falls_back_to_ApplicationType_NameTm()
    {
        var application = new ApplicationProfileInstance
        {
            ApplicationProfile = null,
            ApplicationType = new ApplicationType { NameTm = "  Çagyryş  ", Name = "Invitation" },
        };

        Assert.Equal(
            "Çagyryş",
            ReportDashboardProfileInstanceQuery.ProfileLabelOrMissing(application, "—"));
    }

    [Fact]
    public void ProfileLabelOrMissing_falls_back_to_ApplicationType_Name()
    {
        var application = new ApplicationProfileInstance
        {
            ApplicationType = new ApplicationType { NameTm = " ", Name = " LegacyType " },
        };

        Assert.Equal(
            "LegacyType",
            ReportDashboardProfileInstanceQuery.ProfileLabelOrMissing(application, "—"));
    }

    [Fact]
    public void ProfileLabelOrMissing_returns_missing_when_nothing_set()
    {
        var application = new ApplicationProfileInstance();
        Assert.Equal("missing", ReportDashboardProfileInstanceQuery.ProfileLabelOrMissing(application, "missing"));
    }
}
