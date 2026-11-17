using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ApplicationProfilePicker;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public class ApplicationProfilePickerRowTests
{
    [Fact]
    public void RequiresApprovalLegVersion_TrueForViaMinistryEvenWhenEmpty()
    {
        var row = new ApplicationProfilePickerRow
        {
            ProgressRoute = ApplicationProfileInstanceProgressRouteKind.ViaMinistries,
        };

        Assert.True(row.RequiresApprovalLegVersion);
        Assert.Empty(row.ApprovalLegVersions);
    }

    [Fact]
    public void RequiresApprovalLegVersion_FalseForDirectMigration()
    {
        var row = new ApplicationProfilePickerRow
        {
            ProgressRoute = ApplicationProfileInstanceProgressRouteKind.DirectToMigrationService,
        };

        Assert.False(row.RequiresApprovalLegVersion);
    }

    [Fact]
    public void RequiresApprovalLegVersion_TrueWhenCreateListOverridesToVia()
    {
        // ProgressRoute comes from the create-list filter when New is opened from Via/Direct.
        var row = new ApplicationProfilePickerRow
        {
            ProgressRoute = ApplicationProfileInstanceProgressRouteKind.ViaMinistries,
            Code = "seretmezlik",
        };

        Assert.True(row.RequiresApprovalLegVersion);
    }
}