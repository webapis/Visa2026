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
    public void ShowSubmitToOrganization_ViaCreateList_OrSeretmezlikOnDirect()
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
        var seretmezlikDirect = new ApplicationProfileInstance
        {
            ApplicationProfile = new ApplicationProfile
            {
                RequireSubmitToOrganization = true,
                RequireTargetApplicationProfileInstance = true,
            },
            CreationProgressRoute = ApplicationProfileInstanceProgressRouteKind.DirectToMigrationService,
        };

        Assert.True(ApplicationProfileConfigurationResolver.ShowSubmitToOrganization(via));
        Assert.False(ApplicationProfileConfigurationResolver.ShowSubmitToOrganization(direct));
        Assert.True(ApplicationProfileConfigurationResolver.ShowSubmitToOrganization(seretmezlikDirect));
    }

    [Fact]
    public void TimelineSubmissionLabel_DirectWithoutDestination_UsesGenericMigrationLabel()
    {
        var app = new ApplicationProfileInstance();
        Assert.Equal(
            ApplicationMigrationSubmission.TimelineLabel,
            ApplicationProfileSubmitToOrganizationHelper.TimelineSubmissionLabel(app));
    }

    [Fact]
    public void ListLookupOptions_NullObjectSpace_ReturnsEmpty()
    {
        Assert.Empty(ApplicationProfileSubmitToOrganizationHelper.ListLookupOptions(null!));
    }

    [Fact]
    public void EncodeValue_UsesMinistryPrefixAndGenericMigrationKey()
    {
        var ministryId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var migrationId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var withMinistry = new ApplicationProfileInstance { SubmitToApprovingMinistryId = ministryId };
        var withMigration = new ApplicationProfileInstance { SubmitToMigrationServiceId = migrationId };

        Assert.Equal(
            ApplicationProfileSubmitToOrganizationHelper.MinistryValuePrefix + ministryId.ToString("D"),
            ApplicationProfileSubmitToOrganizationHelper.EncodeValue(withMinistry));
        Assert.Equal(
            ApplicationProfileSubmitToOrganizationHelper.MigrationGenericValue,
            ApplicationProfileSubmitToOrganizationHelper.EncodeValue(withMigration));
    }

    [Fact]
    public void FormatDestinationLabel_Migration_UsesStaticLocalizedName()
    {
        var app = new ApplicationProfileInstance
        {
            SubmitToMigrationService = new MigrationService
            {
                NameTm = "Türkmenistanyň Döwlet migrasiýa gullugynyň Ahal welaýaty boýunça müdirliginiň müdirine",
            },
        };

        Assert.Equal(
            ApplicationProfileSubmitToOrganizationHelper.StaticMigrationServiceDisplayName(),
            ApplicationProfileSubmitToOrganizationHelper.FormatDestinationLabel(app));
    }

    [Fact]
    public void TryParseValue_AcceptsGenericMigrationToken()
    {
        Assert.True(ApplicationProfileSubmitToOrganizationHelper.TryParseValue(
            ApplicationProfileSubmitToOrganizationHelper.MigrationGenericValue,
            out var kind,
            out var id));
        Assert.Equal("migration", kind);
        Assert.Equal(Guid.Empty, id);
    }
}