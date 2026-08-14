using System;
using System.Collections.ObjectModel;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ApplicationWorkspace;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public class ApplicationWorkspaceIssuedRecordsCatalogTests
{
    [Fact]
    public void IsVisible_FollowsMayProduceFlags()
    {
        var app = new ApplicationProfileInstance
        {
            ApplicationProfile = new ApplicationProfile
            {
                ProduceInvitation = true,
                ProduceWorkPermit = false,
                ProduceBorderZone = true,
                ProduceRejection = false,
                ProduceVisa = false,
            },
        };

        Assert.True(ApplicationWorkspaceIssuedRecordsCatalog.IsVisible(app, ApplicationWorkspaceIssuedRecordsCatalog.Invitation));
        Assert.False(ApplicationWorkspaceIssuedRecordsCatalog.IsVisible(app, ApplicationWorkspaceIssuedRecordsCatalog.WorkPermit));
        Assert.True(ApplicationWorkspaceIssuedRecordsCatalog.IsVisible(app, ApplicationWorkspaceIssuedRecordsCatalog.BorderZone));
        Assert.False(ApplicationWorkspaceIssuedRecordsCatalog.IsVisible(app, ApplicationWorkspaceIssuedRecordsCatalog.Rejection));
        Assert.True(ApplicationWorkspaceIssuedRecordsCatalog.IsVisible(app, ApplicationWorkspaceIssuedRecordsCatalog.IssuedVisa));
    }

    [Fact]
    public void BuildIssuedTiles_OmitsHiddenTypes_AndCountsCollectionRows()
    {
        var app = new ApplicationProfileInstance
        {
            ApplicationProfile = new ApplicationProfile
            {
                ProduceInvitation = true,
                ProduceWorkPermit = true,
                ProduceBorderZone = false,
                ProduceRejection = false,
                ProduceVisa = false,
            },
            Invitations = new ObservableCollection<Invitation>
            {
                new() { InvitationNumber = "INV-1", IssuedDate = new DateTime(2026, 8, 10) },
            },
            WorkPermits = new ObservableCollection<WorkPermit>(),
        };

        var view = ApplicationWorkspaceCaseBuilder.Build(
            app,
            app.ApplicationProfile,
            Array.Empty<ApplicationWorkspaceTab>(),
            default,
            new ApplicationWorkspaceCaseChrome());

        Assert.Equal(3, view.IssuedRecordTiles.Count);
        var invitation = Assert.Single(view.IssuedRecordTiles, t => t.Key == ApplicationWorkspaceIssuedRecordsCatalog.Invitation);
        Assert.Equal(1, invitation.Count);
        Assert.Equal("INV-1", invitation.Rows[0].Title);
        var permit = Assert.Single(view.IssuedRecordTiles, t => t.Key == ApplicationWorkspaceIssuedRecordsCatalog.WorkPermit);
        Assert.Equal(0, permit.Count);
        Assert.DoesNotContain(view.IssuedRecordTiles, t => t.Key == ApplicationWorkspaceIssuedRecordsCatalog.BorderZone);
        Assert.Contains(view.IssuedRecordTiles, t => t.Key == ApplicationWorkspaceIssuedRecordsCatalog.IssuedVisa);
    }

    [Fact]
    public void ResolveHeaderType_MapsKeysToIssuedHeaderBos()
    {
        Assert.Equal(typeof(Invitation), ApplicationWorkspaceIssuedRecordsCatalog.ResolveHeaderType(ApplicationWorkspaceIssuedRecordsCatalog.Invitation));
        Assert.Equal(typeof(WorkPermit), ApplicationWorkspaceIssuedRecordsCatalog.ResolveHeaderType(ApplicationWorkspaceIssuedRecordsCatalog.WorkPermit));
        Assert.Equal(typeof(BorderZone), ApplicationWorkspaceIssuedRecordsCatalog.ResolveHeaderType(ApplicationWorkspaceIssuedRecordsCatalog.BorderZone));
        Assert.Equal(typeof(Rejection), ApplicationWorkspaceIssuedRecordsCatalog.ResolveHeaderType(ApplicationWorkspaceIssuedRecordsCatalog.Rejection));
        Assert.Equal(typeof(Visa), ApplicationWorkspaceIssuedRecordsCatalog.ResolveHeaderType(ApplicationWorkspaceIssuedRecordsCatalog.IssuedVisa));
        Assert.Null(ApplicationWorkspaceIssuedRecordsCatalog.ResolveHeaderType("inv"));
    }
}
