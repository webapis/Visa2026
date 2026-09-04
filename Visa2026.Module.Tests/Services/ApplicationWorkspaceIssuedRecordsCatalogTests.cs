using System;
using System.Collections.ObjectModel;
using DevExpress.Persistent.BaseImpl.EF;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ApplicationWorkspace;
using Visa2026.Module.Services.HeaderLinkedDocuments;
using Visa2026.Module.Services.PreviewSlot;
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

    [Fact]
    public void TryGetDocumentCopiesFamily_MapsIssuedVisaToVisaCopies()
    {
        Assert.True(ApplicationWorkspaceIssuedRecordsCatalog.TryGetDocumentCopiesFamily(
            ApplicationWorkspaceIssuedRecordsCatalog.IssuedVisa,
            out var family));
        Assert.Equal(HeaderDocumentCopiesFamily.Visa, family);

        var visaId = Guid.Parse("11111111-2222-3333-4444-555555555555");
        Assert.Equal(
            $"visa-document-copies:visa:{visaId:N}|preview:first",
            VisaPreviewSlotOccupantKeys.ForHeaderDocumentCopies(new HeaderDocumentCopiesSlotRequest
            {
                Family = HeaderDocumentCopiesFamily.Visa,
                ParentId = visaId,
                OpenPreviewOnly = true,
            }));
    }

    [Fact]
    public void BuildIssuedTiles_IssuedVisaHasCopy_WhenVisaDocumentHasFile()
    {
        var visaWithCopy = new Visa
        {
            ID = Guid.NewGuid(),
            VisaNumber = "a0002",
            IssueDate = new DateTime(2023, 8, 25),
            Documents = new ObservableCollection<VisaDocument>(),
        };
        visaWithCopy.Documents.Add(new VisaDocument
        {
            Visa = visaWithCopy,
            File = new FileData { Content = [1, 2, 3], Size = 3, FileName = "visa.pdf" },
        });
        var visaWithoutCopy = new Visa
        {
            ID = Guid.NewGuid(),
            VisaNumber = "a0001",
            IssueDate = new DateTime(2023, 8, 24),
        };

        var app = new ApplicationProfileInstance
        {
            ApplicationProfile = new ApplicationProfile
            {
                ProduceInvitation = true,
                ProduceVisa = true,
            },
            IssuedVisas = new ObservableCollection<Visa> { visaWithCopy, visaWithoutCopy },
        };

        var view = ApplicationWorkspaceCaseBuilder.Build(
            app,
            app.ApplicationProfile,
            Array.Empty<ApplicationWorkspaceTab>(),
            default,
            new ApplicationWorkspaceCaseChrome());

        var tile = Assert.Single(view.IssuedRecordTiles, t => t.Key == ApplicationWorkspaceIssuedRecordsCatalog.IssuedVisa);
        Assert.Equal(2, tile.Count);
        Assert.True(Assert.Single(tile.Rows, r => r.Title == "a0002").HasCopy);
        Assert.False(Assert.Single(tile.Rows, r => r.Title == "a0001").HasCopy);
    }

    [Fact]
    public void Build_Activities_KeepSubmittedAfterLaterApprovals()
    {
        var profile = new ApplicationProfile
        {
            ProgressRoute = ApplicationProfileInstanceProgressRouteKind.ViaMinistries,
        };
        var application = new ApplicationProfileInstance
        {
            ApplicationProfile = profile,
            ApplicationDate = new DateTime(2024, 8, 17),
            ProgressHistory = new ObservableCollection<ApplicationProfileInstanceProgress>(),
        };
        application.ProgressHistory.Add(new ApplicationProfileInstanceProgress
        {
            ID = Guid.NewGuid(),
            ApplicationProfileInstance = application,
            Order = 1,
            Date = new DateTime(2024, 8, 18),
            State = new ApplicationState
            {
                Code = ApplicationProfileInstanceProgressStateCodes.Review1Started,
                NameTm = "Sent for agreement",
            },
        });
        application.ProgressHistory.Add(new ApplicationProfileInstanceProgress
        {
            ID = Guid.NewGuid(),
            ApplicationProfileInstance = application,
            Order = 2,
            Date = new DateTime(2024, 8, 19),
            State = new ApplicationState
            {
                Code = ApplicationProfileInstanceProgressStateCodes.Review1Approved,
                NameTm = "Closed agreement",
            },
        });

        var view = ApplicationWorkspaceCaseBuilder.Build(
            application,
            profile,
            Array.Empty<ApplicationWorkspaceTab>(),
            default,
            new ApplicationWorkspaceCaseChrome { StartedOn = "17 Aug 2024" });

        Assert.Equal(2, view.Activities.Count);
        Assert.Equal("Approved", view.Activities[0].Title);
        Assert.Equal("19 Aug 2024", view.Activities[0].Subtitle);
        Assert.Equal("Submitted", view.Activities[1].Title);
        Assert.Equal("18 Aug 2024", view.Activities[1].Subtitle);
    }
}
