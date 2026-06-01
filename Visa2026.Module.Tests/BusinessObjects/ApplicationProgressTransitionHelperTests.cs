using Visa2026.Module.BusinessObjects;
using Xunit;

namespace Visa2026.Module.Tests.BusinessObjects;

public class ApplicationProgressTransitionHelperTests
{
    [Fact]
    public void DirectRoute_AllowedNextAfterOfficePreparation_IsProcessStarted()
    {
        var app = CreateDirectApplication();
        var officePrep = OfficePreparationRow(app);

        var next = ApplicationProgressTransitionHelper.GetAllowedNextStateCodes(app, officePrep);

        Assert.Single(next);
        Assert.Equal(ApplicationProgressStateCodes.ProcessStarted, next[0]);
    }

    [Fact]
    public void ViaMinistries_FirstOnly_AllowedNextAfterOfficePreparation_IsFirstReview()
    {
        var app = CreateViaMinistriesApplication(MinistryReviewDepth.FirstMinistryOnly);
        var officePrep = OfficePreparationRow(app);

        var next = ApplicationProgressTransitionHelper.GetAllowedNextStateCodes(app, officePrep);

        Assert.Contains(ApplicationProgressStateCodes.Review1Started, next);
        Assert.DoesNotContain(ApplicationProgressStateCodes.Review2Started, next);
    }

    [Fact]
    public void ViaMinistries_FirstAndSecond_AllowedNextAfterFirstApproval_IsSecondReview()
    {
        var app = CreateViaMinistriesApplication(MinistryReviewDepth.FirstAndSecondMinistry);
        var approved = new ApplicationProgress
        {
            Application = app,
            State = new ApplicationState { Code = ApplicationProgressStateCodes.Review1Approved },
            Location = new ApplicationLocation { Code = ApplicationProgressLocationCodes.AtMinistry1 }
        };

        var next = ApplicationProgressTransitionHelper.GetAllowedNextStateCodes(app, approved);

        Assert.Contains(ApplicationProgressStateCodes.Review2Started, next);
        Assert.DoesNotContain(ApplicationProgressStateCodes.ProcessStarted, next);
    }

    [Fact]
    public void IsCanonicalStateLocationPair_RejectsProcessStartedAtOffice()
    {
        Assert.False(ApplicationProgressTransitionHelper.IsCanonicalStateLocationPair(
            ApplicationProgressStateCodes.ProcessStarted,
            ApplicationProgressLocationCodes.AtOffice));
    }

    [Fact]
    public void IsCanonicalStateLocationPair_AcceptsProcessStartedAtMigration()
    {
        Assert.True(ApplicationProgressTransitionHelper.IsCanonicalStateLocationPair(
            ApplicationProgressStateCodes.ProcessStarted,
            ApplicationProgressLocationCodes.AtMigrationService));
    }

    [Fact]
    public void TryValidateProgressStep_RejectsIllegalDirectJump()
    {
        var app = CreateDirectApplication();
        var progress = new ApplicationProgress
        {
            Application = app,
            State = new ApplicationState { Code = ApplicationProgressStateCodes.ProcessIssued },
            Location = new ApplicationLocation { Code = ApplicationProgressLocationCodes.AtMigrationService },
            Date = DateTime.Today
        };
        app.ProgressHistory = new List<ApplicationProgress> { OfficePreparationRow(app), progress };

        Assert.False(ApplicationProgressTransitionHelper.TryValidateProgressStep(progress, null, out var message));
        Assert.False(string.IsNullOrWhiteSpace(message));
    }

    [Fact]
    public void TryValidateProgressStep_AcceptsDirectOfficeToMigration()
    {
        var app = CreateDirectApplication();
        var officePrep = OfficePreparationRow(app);
        var started = new ApplicationProgress
        {
            Application = app,
            State = new ApplicationState { Code = ApplicationProgressStateCodes.ProcessStarted },
            Location = new ApplicationLocation { Code = ApplicationProgressLocationCodes.AtMigrationService },
            Date = DateTime.Today
        };
        app.ProgressHistory = new List<ApplicationProgress> { officePrep, started };

        Assert.True(ApplicationProgressTransitionHelper.TryValidateProgressStep(started, null, out _));
    }

    [Fact]
    public void TryValidateProgressStep_RejectsAdvanceAfterTerminal()
    {
        var app = CreateDirectApplication();
        var officePrep = OfficePreparationRow(app);
        var terminal = new ApplicationProgress
        {
            Application = app,
            State = new ApplicationState { Code = ApplicationProgressStateCodes.ProcessIssued },
            Location = new ApplicationLocation { Code = ApplicationProgressLocationCodes.AtMigrationService },
            Date = DateTime.Today.AddDays(-1)
        };
        officePrep.Date = DateTime.Today.AddDays(-2);
        var next = new ApplicationProgress
        {
            Application = app,
            State = new ApplicationState { Code = ApplicationProgressStateCodes.ProcessCancelled },
            Location = new ApplicationLocation { Code = ApplicationProgressLocationCodes.AtOffice },
            Date = DateTime.Today
        };
        app.ProgressHistory = new List<ApplicationProgress> { officePrep, terminal, next };

        Assert.False(ApplicationProgressTransitionHelper.TryValidateProgressStep(next, null, out var message));
        Assert.False(string.IsNullOrWhiteSpace(message));
    }

    [Fact]
    public void GetSuggestedNextStep_AfterOfficePreparation_DirectMatchesLegacyHelper()
    {
        var app = CreateDirectApplication();
        var suggested = ApplicationProgressTransitionHelper.GetSuggestedNextStep(app, OfficePreparationRow(app));

        Assert.NotNull(suggested);
        Assert.Equal(ApplicationProgressStateCodes.ProcessStarted, suggested.Value.StateCode);
        Assert.Equal(ApplicationProgressLocationCodes.AtMigrationService, suggested.Value.LocationCode);
    }

    [Fact]
    public void IsTerminalStateCode_IncludesReviewRejections()
    {
        Assert.True(ApplicationProgressTransitionHelper.IsTerminalStateCode(
            ApplicationProgressStateCodes.Review1Rejected));
    }

    [Fact]
    public void GetAllowedLocationCodesForState_ProcessStarted_DirectRoute_ReturnsMigrationService()
    {
        var app = CreateDirectApplication();
        var locations = ApplicationProgressTransitionHelper.GetAllowedLocationCodesForState(
            app,
            ApplicationProgressStateCodes.ProcessStarted);

        Assert.Single(locations);
        Assert.Equal(ApplicationProgressLocationCodes.AtMigrationService, locations[0]);
    }

    [Fact]
    public void GetAllowedLocationCodesForState_ProcessStarted_WithoutApplication_ReturnsCanonical()
    {
        var locations = ApplicationProgressTransitionHelper.GetAllowedLocationCodesForState(
            null,
            ApplicationProgressStateCodes.ProcessStarted);

        Assert.Contains(ApplicationProgressLocationCodes.AtMigrationService, locations);
    }

    private static Application CreateDirectApplication() =>
        new()
        {
            ApplicationType = new ApplicationType
            {
                ApplicationProgressRoute = ApplicationProgressRouteKind.DirectToMigrationService,
                MinistryReviewDepth = MinistryReviewDepth.None
            },
            ProgressHistory = new List<ApplicationProgress>()
        };

    private static Application CreateViaMinistriesApplication(MinistryReviewDepth depth) =>
        new()
        {
            ApplicationType = new ApplicationType
            {
                ApplicationProgressRoute = ApplicationProgressRouteKind.ViaMinistries,
                MinistryReviewDepth = depth
            },
            ProgressHistory = new List<ApplicationProgress>()
        };

    private static ApplicationProgress OfficePreparationRow(Application app) =>
        new()
        {
            Application = app,
            State = new ApplicationState { Code = ApplicationProgressDefaults.InitialStateCode },
            Location = new ApplicationLocation { Code = ApplicationProgressDefaults.InitialLocationCode },
            Date = DateTime.Today.AddDays(-1)
        };
}
