using Visa2026.Module.BusinessObjects;
using Visa2026.Module.DatabaseUpdate;
using Xunit;

namespace Visa2026.Module.Tests.DatabaseUpdate;

public sealed class ApplicationTypeConfigurationApplierTests
{
    [Fact]
    public void ApplyShowFlags_copies_capability_and_show_flags()
    {
        var target = new ApplicationType
        {
            CanIssueVisa = false,
            ShowVisas = false,
            ShowProjectContract = true,
        };
        var source = new ApplicationTypeConfigurationRow
        {
            Name = "App_Inv",
            CanIssueVisa = true,
            CanIssueInvitation = true,
            ShowVisas = true,
            ShowInvitations = true,
            ShowProjectContract = false,
        };

        ApplicationTypeConfigurationApplier.ApplyShowFlags(target, source);

        Assert.True(target.CanIssueVisa);
        Assert.True(target.CanIssueInvitation);
        Assert.True(target.ShowVisas);
        Assert.True(target.ShowInvitations);
        Assert.False(target.ShowProjectContract);
    }

    [Fact]
    public void Apply_without_overwrite_keeps_existing_show_flags()
    {
        var target = new ApplicationType
        {
            ShowVisas = true,
            CanIssueVisa = true,
            NameTm = "old",
        };
        var source = new ApplicationTypeConfigurationRow
        {
            Name = "App_Inv",
            NameTm = "Täze",
            Code = "INV",
            ShowVisas = false,
            CanIssueVisa = false,
            ApplicationProgressRoute = ApplicationProgressRouteKind.ViaMinistries,
            MinistryReviewDepth = MinistryReviewDepth.FirstAndSecondMinistry,
        };

        ApplicationTypeConfigurationApplier.Apply(target, source, overwriteShowFlags: false);

        Assert.Equal("Täze", target.NameTm);
        Assert.Equal("INV", target.Code);
        Assert.Equal("App_Inv", target.LocalizationKey);
        Assert.True(target.ShowVisas);
        Assert.True(target.CanIssueVisa);
        Assert.Equal(MinistryReviewDepth.FirstAndSecondMinistry, target.MinistryReviewDepth);
    }

    [Fact]
    public void Apply_direct_migration_route_forces_ministry_depth_none()
    {
        var target = new ApplicationType();
        var source = new ApplicationTypeConfigurationRow
        {
            Name = "Direct",
            ApplicationProgressRoute = ApplicationProgressRouteKind.DirectToMigrationService,
            MinistryReviewDepth = MinistryReviewDepth.FirstAndSecondMinistry,
        };

        ApplicationTypeConfigurationApplier.Apply(target, source, overwriteShowFlags: false);

        Assert.Equal(ApplicationProgressRouteKind.DirectToMigrationService, target.ApplicationProgressRoute);
        Assert.Equal(MinistryReviewDepth.None, target.MinistryReviewDepth);
    }

    [Fact]
    public void Apply_via_ministries_with_none_depth_defaults_to_first_ministry()
    {
        var target = new ApplicationType();
        var source = new ApplicationTypeConfigurationRow
        {
            Name = "Via",
            ApplicationProgressRoute = ApplicationProgressRouteKind.ViaMinistries,
            MinistryReviewDepth = MinistryReviewDepth.None,
        };

        ApplicationTypeConfigurationApplier.Apply(target, source, overwriteShowFlags: false);

        Assert.Equal(MinistryReviewDepth.FirstMinistryOnly, target.MinistryReviewDepth);
    }

    [Fact]
    public void Apply_with_overwrite_replaces_show_flags()
    {
        var target = new ApplicationType { ShowVisas = true, CanIssueVisa = true };
        var source = new ApplicationTypeConfigurationRow
        {
            Name = "X",
            ShowVisas = false,
            CanIssueVisa = false,
            ApplicationProgressRoute = ApplicationProgressRouteKind.ViaMinistries,
            MinistryReviewDepth = MinistryReviewDepth.FirstMinistryOnly,
        };

        ApplicationTypeConfigurationApplier.Apply(target, source, overwriteShowFlags: true);

        Assert.False(target.ShowVisas);
        Assert.False(target.CanIssueVisa);
    }
}
