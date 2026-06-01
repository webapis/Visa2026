using Visa2026.Module.BusinessObjects;
using Xunit;

namespace Visa2026.Module.Tests.BusinessObjects;

public class ApplicationProgressRouteHelperTests
{
    [Fact]
    public void DirectRoute_ExcludesMinistryStateCodes()
    {
        var states = ApplicationProgressRouteHelper.GetAllowedStateCodes(
            ApplicationProgressRouteKind.DirectToMigrationService,
            MinistryReviewDepth.None);

        Assert.Contains(ApplicationProgressStateCodes.IsBeingPrepared, states);
        Assert.Contains(ApplicationProgressStateCodes.ProcessStarted, states);
        Assert.DoesNotContain(ApplicationProgressStateCodes.Review1Started, states);
        Assert.DoesNotContain(ApplicationProgressStateCodes.Review2Started, states);
    }

    [Fact]
    public void ViaMinistries_FirstOnly_IncludesFirstNotSecondMinistryStates()
    {
        var states = ApplicationProgressRouteHelper.GetAllowedStateCodes(
            ApplicationProgressRouteKind.ViaMinistries,
            MinistryReviewDepth.FirstMinistryOnly);

        Assert.Contains(ApplicationProgressStateCodes.Review1Started, states);
        Assert.DoesNotContain(ApplicationProgressStateCodes.Review2Started, states);
    }

    [Fact]
    public void ViaMinistries_FirstAndSecond_IncludesSecondMinistryStates()
    {
        var states = ApplicationProgressRouteHelper.GetAllowedStateCodes(
            ApplicationProgressRouteKind.ViaMinistries,
            MinistryReviewDepth.FirstAndSecondMinistry);

        Assert.Contains(ApplicationProgressStateCodes.Review2Approved, states);
    }

    [Fact]
    public void DirectRoute_ExcludesMinistryLocations()
    {
        var locations = ApplicationProgressRouteHelper.GetAllowedLocationCodes(
            ApplicationProgressRouteKind.DirectToMigrationService,
            MinistryReviewDepth.None);

        Assert.Contains(ApplicationProgressLocationCodes.AtOffice, locations);
        Assert.Contains(ApplicationProgressLocationCodes.AtMigrationService, locations);
        Assert.DoesNotContain(ApplicationProgressLocationCodes.AtMinistry1, locations);
    }

    [Fact]
    public void ViaMinistries_FirstAndSecond_IncludesBothMinistryLocations()
    {
        var locations = ApplicationProgressRouteHelper.GetAllowedLocationCodes(
            ApplicationProgressRouteKind.ViaMinistries,
            MinistryReviewDepth.FirstAndSecondMinistry);

        Assert.Contains(ApplicationProgressLocationCodes.AtMinistry1, locations);
        Assert.Contains(ApplicationProgressLocationCodes.AtMinistry2, locations);
    }

    [Fact]
    public void Normalize_DirectRoute_ForcesNoneDepth()
    {
        var depth = ApplicationProgressRouteHelper.NormalizeMinistryReviewDepth(
            ApplicationProgressRouteKind.DirectToMigrationService,
            MinistryReviewDepth.FirstAndSecondMinistry);

        Assert.Equal(MinistryReviewDepth.None, depth);
    }

    [Fact]
    public void SuggestedNext_AfterOfficePreparation_DirectGoesToMigration()
    {
        var type = new ApplicationType
        {
            ApplicationProgressRoute = ApplicationProgressRouteKind.DirectToMigrationService,
            MinistryReviewDepth = MinistryReviewDepth.None
        };

        var next = ApplicationProgressRouteHelper.GetSuggestedNextAfterOfficePreparation(type);

        Assert.NotNull(next);
        Assert.Equal(ApplicationProgressStateCodes.ProcessStarted, next.Value.StateCode);
        Assert.Equal(ApplicationProgressLocationCodes.AtMigrationService, next.Value.LocationCode);
    }

    [Fact]
    public void SuggestedNext_AfterOfficePreparation_ViaMinistriesGoesToFirstReview()
    {
        var type = new ApplicationType
        {
            ApplicationProgressRoute = ApplicationProgressRouteKind.ViaMinistries,
            MinistryReviewDepth = MinistryReviewDepth.FirstMinistryOnly
        };

        var next = ApplicationProgressRouteHelper.GetSuggestedNextAfterOfficePreparation(type);

        Assert.NotNull(next);
        Assert.Equal(ApplicationProgressStateCodes.Review1Started, next.Value.StateCode);
        Assert.Equal(ApplicationProgressLocationCodes.AtMinistry1, next.Value.LocationCode);
    }

    [Fact]
    public void IsStateCodeAllowed_UsesApplicationTypeRoute()
    {
        var type = new ApplicationType
        {
            ApplicationProgressRoute = ApplicationProgressRouteKind.DirectToMigrationService,
            MinistryReviewDepth = MinistryReviewDepth.None
        };

        Assert.True(ApplicationProgressRouteHelper.IsStateCodeAllowed(type, ApplicationProgressStateCodes.ProcessStarted));
        Assert.False(ApplicationProgressRouteHelper.IsStateCodeAllowed(type, ApplicationProgressStateCodes.Review1Started));
    }

    [Fact]
    public void GetTypePickerRouteFilter_UsesCreationRouteForNewApplication()
    {
        var app = new Application
        {
            CreationProgressRoute = ApplicationProgressRouteKind.ViaMinistries
        };

        Assert.Equal(
            ApplicationProgressRouteKind.ViaMinistries,
            ApplicationProgressRouteHelper.GetTypePickerRouteFilter(app));
    }

    [Fact]
    public void GetTypePickerRouteFilter_UsesApplicationTypeWhenNoCreationRoute()
    {
        var app = new Application
        {
            ApplicationType = new ApplicationType
            {
                ApplicationProgressRoute = ApplicationProgressRouteKind.DirectToMigrationService
            }
        };

        Assert.Equal(
            ApplicationProgressRouteKind.DirectToMigrationService,
            ApplicationProgressRouteHelper.GetTypePickerRouteFilter(app));
    }

    [Fact]
    public void GetTypePickerRouteFilter_CreationRouteWinsOverApplicationType()
    {
        var app = new Application
        {
            CreationProgressRoute = ApplicationProgressRouteKind.DirectToMigrationService,
            ApplicationType = new ApplicationType
            {
                ApplicationProgressRoute = ApplicationProgressRouteKind.ViaMinistries
            }
        };

        Assert.Equal(
            ApplicationProgressRouteKind.DirectToMigrationService,
            ApplicationProgressRouteHelper.GetTypePickerRouteFilter(app));
    }
}
