using System;
using System.Collections.ObjectModel;
using System.Linq;
using Visa2026.Module.BusinessObjects;
using Xunit;

namespace Visa2026.Module.Tests.BusinessObjects;

public class ApplicationProgressRouteHelperTests
{
    [Fact]
    public void NormalizeMinistryReviewDepth_DirectRoute_ForcesNone()
    {
        Assert.Equal(
            MinistryReviewDepth.None,
            ApplicationProgressRouteHelper.NormalizeMinistryReviewDepth(
                ApplicationProgressRouteKind.DirectToMigrationService,
                MinistryReviewDepth.FirstAndSecondMinistry));
    }

    [Fact]
    public void NormalizeMinistryReviewDepth_ViaMinistries_NoneBecomesFirstMinistryOnly()
    {
        Assert.Equal(
            MinistryReviewDepth.FirstMinistryOnly,
            ApplicationProgressRouteHelper.NormalizeMinistryReviewDepth(
                ApplicationProgressRouteKind.ViaMinistries,
                MinistryReviewDepth.None));
    }

    [Fact]
    public void GetAllowedStateCodes_DirectRoute_OnlySharedProcessCodes()
    {
        var codes = ApplicationProgressRouteHelper.GetAllowedStateCodes(
            ApplicationProgressRouteKind.DirectToMigrationService,
            ministryLegCount: 3);

        Assert.Equal(
            [
                ApplicationProgressStateCodes.ProcessStarted,
                ApplicationProgressStateCodes.ProcessIssued,
                ApplicationProgressStateCodes.ProcessRejected,
                ApplicationProgressStateCodes.ProcessCancelled
            ],
            codes);
        Assert.DoesNotContain(ApplicationProgressStateCodes.Review1Started, codes);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 1)]
    [InlineData(3, 3)]
    [InlineData(99, ApplicationProgressLegCodes.MaxLegCount)]
    public void GetAllowedStateCodes_ViaMinistries_ClampsLegCountAndIncludesReviewCodes(
        int requestedLegs,
        int expectedLegs)
    {
        var codes = ApplicationProgressRouteHelper.GetAllowedStateCodes(
            ApplicationProgressRouteKind.ViaMinistries,
            requestedLegs);

        Assert.Contains(ApplicationProgressStateCodes.ProcessStarted, codes);
        Assert.Contains(ApplicationProgressLegCodes.ReviewStarted(1), codes);
        Assert.Contains(ApplicationProgressLegCodes.ReviewApproved(expectedLegs), codes);
        Assert.Contains(ApplicationProgressLegCodes.ReviewRejected(expectedLegs), codes);

        if (expectedLegs < ApplicationProgressLegCodes.MaxLegCount)
        {
            Assert.DoesNotContain(
                ApplicationProgressLegCodes.ReviewApproved(expectedLegs + 1),
                codes);
        }
    }

    [Fact]
    public void GetTypePickerRouteFilter_CreationProgressRoute_OverridesApplicationType()
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
        Assert.Null(ApplicationProgressRouteHelper.GetTypePickerRouteFilter(null));
    }

    [Fact]
    public void GetTypePickerRouteFilter_FallsBackToApplicationTypeRoute()
    {
        var app = new Application
        {
            ApplicationType = new ApplicationType
            {
                ApplicationProgressRoute = ApplicationProgressRouteKind.ViaMinistries
            }
        };

        Assert.Equal(
            ApplicationProgressRouteKind.ViaMinistries,
            ApplicationProgressRouteHelper.GetTypePickerRouteFilter(app));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void IsStateCodeAllowed_Blank_ReturnsFalse(string? stateCode)
    {
        var type = new ApplicationType
        {
            ApplicationProgressRoute = ApplicationProgressRouteKind.DirectToMigrationService
        };

        Assert.False(ApplicationProgressRouteHelper.IsStateCodeAllowed(type, stateCode));
    }

    [Fact]
    public void IsStateCodeAllowed_IsCaseInsensitiveAndTrims()
    {
        var type = new ApplicationType
        {
            ApplicationProgressRoute = ApplicationProgressRouteKind.DirectToMigrationService
        };

        Assert.True(ApplicationProgressRouteHelper.IsStateCodeAllowed(type, " process_started "));
        Assert.False(ApplicationProgressRouteHelper.IsStateCodeAllowed(
            type,
            ApplicationProgressStateCodes.Review1Started));
    }

    [Fact]
    public void TryValidateProgressStep_DisallowedState_SetsError()
    {
        var app = new Application
        {
            ApplicationType = new ApplicationType
            {
                ApplicationProgressRoute = ApplicationProgressRouteKind.DirectToMigrationService
            }
        };
        var progress = new ApplicationProgress
        {
            Application = app,
            State = new ApplicationState { Code = ApplicationProgressStateCodes.Review1Started }
        };

        Assert.False(ApplicationProgressRouteHelper.TryValidateProgressStep(progress, out var error));
        Assert.False(string.IsNullOrWhiteSpace(error));
        Assert.Contains(ApplicationProgressStateCodes.Review1Started, error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TryValidateProgressStep_NullProgressOrApplication_IsValid()
    {
        Assert.True(ApplicationProgressRouteHelper.TryValidateProgressStep(null, out var error1));
        Assert.Null(error1);

        Assert.True(ApplicationProgressRouteHelper.TryValidateProgressStep(
            new ApplicationProgress(),
            out var error2));
        Assert.Null(error2);
    }

    [Fact]
    public void GetSuggestedNextStateAfterOfficePreparation_Direct_ReturnsProcessStarted()
    {
        var app = new Application
        {
            ApplicationType = new ApplicationType
            {
                ApplicationProgressRoute = ApplicationProgressRouteKind.DirectToMigrationService,
                ShowApprovalLegProfile = false,
                ShowProjectContract = false
            }
        };

        Assert.Equal(
            ApplicationProgressStateCodes.ProcessStarted,
            ApplicationProgressRouteHelper.GetSuggestedNextStateAfterOfficePreparation(app));
    }

    [Fact]
    public void GetSuggestedNextStateAfterOfficePreparation_ViaMinistries_ReturnsFirstReviewStarted()
    {
        var app = BuildViaMinistriesApplication(includeProfile: true, includeContract: true);

        Assert.Equal(
            ApplicationProgressLegCodes.ReviewStarted(1),
            ApplicationProgressRouteHelper.GetSuggestedNextStateAfterOfficePreparation(app));
    }

    [Fact]
    public void GetSuggestedNextStateAfterOfficePreparation_MissingRequiredProfile_ReturnsNull()
    {
        var app = BuildViaMinistriesApplication(includeProfile: false, includeContract: true);

        Assert.Null(ApplicationProgressRouteHelper.GetSuggestedNextStateAfterOfficePreparation(app));
    }

    [Fact]
    public void GetSuggestedNextStateAfterOfficePreparation_MissingRequiredContract_ReturnsNull()
    {
        var app = BuildViaMinistriesApplication(includeProfile: true, includeContract: false);

        Assert.Null(ApplicationProgressRouteHelper.GetSuggestedNextStateAfterOfficePreparation(app));
    }

    [Fact]
    public void GetSuggestedNextAfterOfficePreparation_LegacyTuple_MapsLocationCodes()
    {
        var direct = new Application
        {
            ApplicationType = new ApplicationType
            {
                ApplicationProgressRoute = ApplicationProgressRouteKind.DirectToMigrationService
            }
        };
        var via = BuildViaMinistriesApplication(includeProfile: true, includeContract: true);

        var directSuggestion = ApplicationProgressRouteHelper.GetSuggestedNextAfterOfficePreparation(direct);
        Assert.NotNull(directSuggestion);
        Assert.Equal(ApplicationProgressStateCodes.ProcessStarted, directSuggestion.Value.StateCode);
        Assert.Equal(ApplicationProgressLocationCodes.AtMigrationService, directSuggestion.Value.LocationCode);

        var viaSuggestion = ApplicationProgressRouteHelper.GetSuggestedNextAfterOfficePreparation(via);
        Assert.NotNull(viaSuggestion);
        Assert.Equal(ApplicationProgressLegCodes.ReviewStarted(1), viaSuggestion.Value.StateCode);
        Assert.Equal(ApplicationProgressLegCodes.AtMinistry(1), viaSuggestion.Value.LocationCode);
    }

    private static Application BuildViaMinistriesApplication(bool includeProfile, bool includeContract)
    {
        var type = new ApplicationType
        {
            ApplicationProgressRoute = ApplicationProgressRouteKind.ViaMinistries,
            ShowApprovalLegProfile = true,
            ShowProjectContract = true
        };

        var app = new Application
        {
            ApplicationType = type,
            ProgressHistory = new ObservableCollection<ApplicationProgress>()
        };

        if (includeProfile)
        {
            app.ApprovalLegProfile = new ApprovalLegProfile
            {
                MinistryLegs =
                [
                    new ApprovalLegProfileMinistryLeg
                    {
                        Sequence = 1,
                        ApprovingMinistry = new ApprovingMinistry()
                    }
                ]
            };
        }

        if (includeContract)
            app.ProjectContract = new ProjectContract();

        return app;
    }
}
