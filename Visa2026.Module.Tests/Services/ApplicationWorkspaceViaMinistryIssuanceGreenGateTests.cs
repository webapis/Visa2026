using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ApplicationWorkspace;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public class ApplicationWorkspaceViaMinistryIssuanceGreenGateTests
{
    [Fact]
    public void RequiredChipKeys_invitation_getting_ignores_visa()
    {
        var app = ViaMinistryApp(produceInvitation: true, produceVisa: true, produceWorkPermit: false);

        var keys = ApplicationWorkspaceViaMinistryIssuanceGreenGate.RequiredChipKeys(app);

        Assert.Equal(new[] { ApplicationWorkspaceIssuedRecordsCatalog.Invitation }, keys);
    }

    [Fact]
    public void RequiredChipKeys_invitation_and_work_permit_ignores_visa()
    {
        var app = ViaMinistryApp(produceInvitation: true, produceVisa: true, produceWorkPermit: true);

        var keys = ApplicationWorkspaceViaMinistryIssuanceGreenGate.RequiredChipKeys(app);

        Assert.Equal(
            new[]
            {
                ApplicationWorkspaceIssuedRecordsCatalog.Invitation,
                ApplicationWorkspaceIssuedRecordsCatalog.WorkPermit,
            },
            keys);
    }

    [Fact]
    public void RequiredChipKeys_visa_extension_requires_visa()
    {
        var app = ViaMinistryApp(produceInvitation: false, produceVisa: true, produceWorkPermit: false);

        var keys = ApplicationWorkspaceViaMinistryIssuanceGreenGate.RequiredChipKeys(app);

        Assert.Equal(new[] { ApplicationWorkspaceIssuedRecordsCatalog.IssuedVisa }, keys);
    }

    [Fact]
    public void RequiredChipKeys_visa_and_work_permit_extension()
    {
        var app = ViaMinistryApp(produceInvitation: false, produceVisa: true, produceWorkPermit: true);

        var keys = ApplicationWorkspaceViaMinistryIssuanceGreenGate.RequiredChipKeys(app);

        Assert.Equal(
            new[]
            {
                ApplicationWorkspaceIssuedRecordsCatalog.IssuedVisa,
                ApplicationWorkspaceIssuedRecordsCatalog.WorkPermit,
            },
            keys);
    }

    [Fact]
    public void RequiredChipKeys_work_permit_only()
    {
        var app = ViaMinistryApp(produceInvitation: false, produceVisa: false, produceWorkPermit: true);

        var keys = ApplicationWorkspaceViaMinistryIssuanceGreenGate.RequiredChipKeys(app);

        Assert.Equal(new[] { ApplicationWorkspaceIssuedRecordsCatalog.WorkPermit }, keys);
    }

    [Fact]
    public void RequiredChipKeys_border_zone()
    {
        var app = ViaMinistryApp(
            produceInvitation: false,
            produceVisa: false,
            produceWorkPermit: false,
            produceBorderZone: true);

        var keys = ApplicationWorkspaceViaMinistryIssuanceGreenGate.RequiredChipKeys(app);

        Assert.Equal(new[] { ApplicationWorkspaceIssuedRecordsCatalog.BorderZone }, keys);
    }

    [Fact]
    public void RequiredChipKeys_work_location_grant()
    {
        var app = ViaMinistryApp(
            produceInvitation: false,
            produceVisa: false,
            produceWorkPermit: false,
            produceWorkLocation: true);

        var keys = ApplicationWorkspaceViaMinistryIssuanceGreenGate.RequiredChipKeys(app);

        Assert.Equal(new[] { ApplicationWorkspaceIssuedRecordsCatalog.WorkPermitLocationGrant }, keys);
    }

    [Fact]
    public void IsGreen_true_when_as_co_received_and_invitation_complete_even_if_visa_zero()
    {
        var app = ViaMinistryApp(produceInvitation: true, produceVisa: true, produceWorkPermit: false);
        app.SetListViewResultCoverage(
        [
            Chip(ApplicationWorkspaceIssuedRecordsCatalog.Invitation, 2, 2),
            Chip(ApplicationWorkspaceIssuedRecordsCatalog.IssuedVisa, 0, 2),
        ]);

        Assert.True(ApplicationWorkspaceViaMinistryIssuanceGreenGate.IsGreen(
            app,
            ApplicationProfileInstanceProgressStateCodes.ProcessStarted));
    }

    [Fact]
    public void IsGreen_false_when_latest_is_not_as_co_received()
    {
        var app = ViaMinistryApp(produceInvitation: true, produceVisa: true, produceWorkPermit: false);
        app.SetListViewResultCoverage(
        [
            Chip(ApplicationWorkspaceIssuedRecordsCatalog.Invitation, 2, 2),
        ]);

        Assert.False(ApplicationWorkspaceViaMinistryIssuanceGreenGate.IsGreen(
            app,
            ApplicationProfileInstanceProgressStateCodes.SubmittedToMigration));
    }

    [Fact]
    public void IsGreen_false_when_invitation_incomplete()
    {
        var app = ViaMinistryApp(produceInvitation: true, produceVisa: true, produceWorkPermit: false);
        app.SetListViewResultCoverage(
        [
            Chip(ApplicationWorkspaceIssuedRecordsCatalog.Invitation, 1, 2),
            Chip(ApplicationWorkspaceIssuedRecordsCatalog.IssuedVisa, 2, 2),
        ]);

        Assert.False(ApplicationWorkspaceViaMinistryIssuanceGreenGate.IsGreen(
            app,
            ApplicationProfileInstanceProgressStateCodes.ProcessStarted));
    }

    [Fact]
    public void IsGreen_false_for_direct_route_even_when_complete()
    {
        var app = ViaMinistryApp(produceInvitation: true, produceVisa: true, produceWorkPermit: false);
        app.ApplicationProfile!.ProgressRoute = ApplicationProfileInstanceProgressRouteKind.DirectToMigrationService;
        app.SetListViewResultCoverage(
        [
            Chip(ApplicationWorkspaceIssuedRecordsCatalog.Invitation, 2, 2),
        ]);

        Assert.False(ApplicationWorkspaceViaMinistryIssuanceGreenGate.IsGreen(
            app,
            ApplicationProfileInstanceProgressStateCodes.ProcessStarted));
    }

    [Fact]
    public void IsGreen_true_for_invitation_and_wp_when_both_complete()
    {
        var app = ViaMinistryApp(produceInvitation: true, produceVisa: true, produceWorkPermit: true);
        app.SetListViewResultCoverage(
        [
            Chip(ApplicationWorkspaceIssuedRecordsCatalog.Invitation, 2, 2),
            Chip(ApplicationWorkspaceIssuedRecordsCatalog.WorkPermit, 2, 2),
            Chip(ApplicationWorkspaceIssuedRecordsCatalog.IssuedVisa, 0, 2),
        ]);

        Assert.True(ApplicationWorkspaceViaMinistryIssuanceGreenGate.IsGreen(
            app,
            ApplicationProfileInstanceProgressStateCodes.ProcessStarted));
    }

    [Fact]
    public void IsGreen_false_for_invitation_and_wp_when_wp_incomplete()
    {
        var app = ViaMinistryApp(produceInvitation: true, produceVisa: true, produceWorkPermit: true);
        app.SetListViewResultCoverage(
        [
            Chip(ApplicationWorkspaceIssuedRecordsCatalog.Invitation, 2, 2),
            Chip(ApplicationWorkspaceIssuedRecordsCatalog.WorkPermit, 1, 2),
            Chip(ApplicationWorkspaceIssuedRecordsCatalog.IssuedVisa, 2, 2),
        ]);

        Assert.False(ApplicationWorkspaceViaMinistryIssuanceGreenGate.IsGreen(
            app,
            ApplicationProfileInstanceProgressStateCodes.ProcessStarted));
    }

    private static ApplicationProfileInstance ViaMinistryApp(
        bool produceInvitation,
        bool produceVisa,
        bool produceWorkPermit,
        bool produceBorderZone = false,
        bool produceWorkLocation = false) =>
        new()
        {
            ApplicationProfile = new ApplicationProfile
            {
                ProgressRoute = ApplicationProfileInstanceProgressRouteKind.ViaMinistries,
                ProduceInvitation = produceInvitation,
                ProduceVisa = produceVisa,
                ProduceWorkPermit = produceWorkPermit,
                ProduceBorderZone = produceBorderZone,
                ProduceWorkLocation = produceWorkLocation,
            },
        };

    private static ApplicationWorkspaceIssuedResultListCoverage.Chip Chip(
        string key,
        int coverage,
        int expected) =>
        new(key, key, coverage, expected, IsOptional: false);
}