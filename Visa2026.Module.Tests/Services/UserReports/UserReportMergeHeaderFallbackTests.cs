#nullable enable

using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ApplicationPersonRoster;
using Visa2026.Module.Services.UserReports;
using Xunit;

namespace Visa2026.Module.Tests.Services.UserReports;

public class UserReportMergeHeaderFallbackTests
{
    [Fact]
    public void Item_root_reads_application_number_and_migration_service()
    {
        var application = new ApplicationProfileInstance
        {
            FullApplicationNumber = "10/-12521",
            MigrationService = new MigrationService { NameTm = "Dowlet migrasiya gullugy" },
        };
        var line = new ApplicationRosterMergeLine { ApplicationProfileInstance = application };

        Assert.Null(UserReportMergeDataHelper.GetPropertyValue(line, "AFNUM"));
        Assert.Equal("10/-12521", UserReportMergeDataHelper.GetPropertyValueFromItemOrApplication(line, "AFNUM"));
        Assert.Equal(
            "Dowlet migrasiya gullugy",
            UserReportMergeDataHelper.GetPropertyValueFromItemOrApplication(line, "MSRV"));
        Assert.Equal(0, Assert.IsType<int>(UserReportMergeDataHelper.GetPropertyValueFromItemOrApplication(line, "TPCNT")));
    }
}