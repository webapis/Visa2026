using System.Collections.ObjectModel;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ApplicationProfilePicker;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public class ApplicationProfileSubmitToOrganizationTests
{
    [Fact]
    public void GetProgressRoute_SubmitToOrganization_ForcesDirect()
    {
        var profile = new ApplicationProfile
        {
            ProgressRoute = ApplicationProfileInstanceProgressRouteKind.ViaMinistries,
            RequireSubmitToOrganization = true,
        };
        var app = new ApplicationProfileInstance
        {
            ApplicationProfile = profile,
            CreationProgressRoute = ApplicationProfileInstanceProgressRouteKind.ViaMinistries,
            ApprovalLegSnapshots = new ObservableCollection<ApplicationProfileInstanceApprovalLegSnapshot>
            {
                new() { MinistryShortName = "TNGIZ", Sequence = 1 },
            },
        };

        Assert.Equal(
            ApplicationProfileInstanceProgressRouteKind.DirectToMigrationService,
            ApplicationProfileConfigurationResolver.GetProgressRoute(app));
        Assert.True(ApplicationProfileConfigurationResolver.DirectEndsAtMigrationSubmission(app));
    }

    [Fact]
    public void TimelineSubmissionLabel_UsesMinistryShortName()
    {
        var app = new ApplicationProfileInstance
        {
            SubmitToApprovingMinistry = new ApprovingMinistry
            {
                ShortNameTm = "TNGIZ",
                NameTm = "Long name",
            },
        };

        Assert.Equal(
            "Submitted to TNGIZ",
            ApplicationProfileSubmitToOrganizationHelper.TimelineSubmissionLabel(app));
    }

    [Fact]
    public void RequiresApprovalLegVersion_FalseWhenSubmitToOrganization()
    {
        var row = new ApplicationProfilePickerRow
        {
            ProgressRoute = ApplicationProfileInstanceProgressRouteKind.ViaMinistries,
            RequiresSubmitToOrganization = true,
        };

        Assert.False(row.RequiresApprovalLegVersion);
    }

    [Fact]
    public void ShowSubmitToOrganization_OnlyWhenViaCreateList()
    {
        var profile = new ApplicationProfile { RequireSubmitToOrganization = true };
        var via = new ApplicationProfileInstance
        {
            ApplicationProfile = profile,
            CreationProgressRoute = ApplicationProfileInstanceProgressRouteKind.ViaMinistries,
        };
        var direct = new ApplicationProfileInstance
        {
            ApplicationProfile = profile,
            CreationProgressRoute = ApplicationProfileInstanceProgressRouteKind.DirectToMigrationService,
        };

        Assert.True(ApplicationProfileConfigurationResolver.ShowSubmitToOrganization(via));
        Assert.False(ApplicationProfileConfigurationResolver.ShowSubmitToOrganization(direct));
    }

    [Fact]
    public void TimelineSubmissionLabel_DirectWithoutDestination_UsesGenericMigrationLabel()
    {
        var app = new ApplicationProfileInstance();
        Assert.Equal(
            ApplicationMigrationSubmission.TimelineLabel,
            ApplicationProfileSubmitToOrganizationHelper.TimelineSubmissionLabel(app));
    }
}