using Visa2026.Module.BusinessObjects;
using Visa2026.Module.DatabaseUpdate;
using Xunit;

namespace Visa2026.Module.Tests.DatabaseUpdate;

public sealed class ApplicationTypeConfigurationSeedTests
{
    [Fact]
    public void Rows_loads_embedded_catalog()
    {
        Assert.True(ApplicationTypeConfigurationSeed.Rows.Count >= 30);
        Assert.Contains(ApplicationTypeConfigurationSeed.Rows, r => r.Name == "App_Inv");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Not_A_Real_Type")]
    public void TryGetByName_unknown_returns_false(string? name)
    {
        Assert.False(ApplicationTypeConfigurationSeed.TryGetByName(name, out _));
    }

    [Fact]
    public void TryGetByName_is_case_insensitive()
    {
        Assert.True(ApplicationTypeConfigurationSeed.TryGetByName("app_inv", out var row));
        Assert.Equal("App_Inv", row.Name);
    }

    [Fact]
    public void TryGetByName_App_Inv_infers_via_ministries_and_two_leg_depth()
    {
        Assert.True(ApplicationTypeConfigurationSeed.TryGetByName("App_Inv", out var row));

        Assert.Equal("get_invitation", row.Code);
        Assert.Equal(ApplicationLifecycleStage.Entry, row.LifecycleStage);
        Assert.Equal(ApplicationTypeCategory.Both, row.Category);
        Assert.True(row.CanIssueVisa);
        Assert.True(row.CanIssueInvitation);
        Assert.False(row.CanIssueWorkPermit);
        Assert.True(row.ShowProjectContract);
        Assert.True(row.ShowInvitations);
        Assert.Equal(ApplicationProgressRouteKind.ViaMinistries, row.ApplicationProgressRoute);
        Assert.Equal(MinistryReviewDepth.FirstAndSecondMinistry, row.MinistryReviewDepth);
        Assert.True(row.ShowApprovalLegProfile);
    }

    [Fact]
    public void TryGetByName_App_Cancel_App_infers_direct_migration_and_no_ministry_depth()
    {
        Assert.True(ApplicationTypeConfigurationSeed.TryGetByName("App_Cancel_App", out var row));

        Assert.False(row.ShowProjectContract);
        Assert.Equal(ApplicationProgressRouteKind.DirectToMigrationService, row.ApplicationProgressRoute);
        Assert.Equal(MinistryReviewDepth.None, row.MinistryReviewDepth);
        Assert.False(row.ShowApprovalLegProfile);
    }

    [Fact]
    public void TryGetByName_App_Border_Zone_Permission_infers_first_ministry_only()
    {
        Assert.True(ApplicationTypeConfigurationSeed.TryGetByName("App_Border_Zone_Permission", out var row));

        Assert.True(row.ShowProjectContract);
        Assert.False(row.ShowInvitations);
        Assert.False(row.ShowWorkPermits);
        Assert.Equal(ApplicationProgressRouteKind.ViaMinistries, row.ApplicationProgressRoute);
        Assert.Equal(MinistryReviewDepth.FirstMinistryOnly, row.MinistryReviewDepth);
    }
}
