#nullable enable

using System;
using System.Collections.ObjectModel;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ApplicationWorkspace;
using Xunit;

namespace Visa2026.Module.Tests.Services;

/// <summary>
/// Invitation issued-result columns must use ExpirationDate (91-day offset label),
/// not the retired Expiration key — regression for the case-workspace rename.
/// </summary>
public class ApplicationWorkspaceIssuedInvitationExpirationColumnTests
{
    [Fact]
    public void Invitation_tile_exposes_ExpirationDate_column_key()
    {
        var app = new ApplicationProfileInstance
        {
            ApplicationProfile = new ApplicationProfile
            {
                ProduceInvitation = true,
                ProduceWorkPermit = false,
                ProduceBorderZone = false,
                ProduceRejection = false,
                ProduceVisa = false,
            },
            Invitations = new ObservableCollection<Invitation>
            {
                new()
                {
                    InvitationNumber = "INV-91",
                    IssuedDate = new DateTime(2026, 10, 17),
                    ExpirationDate = Invitation.ExpirationFromIssued(new DateTime(2026, 10, 17)),
                },
            },
        };

        var view = ApplicationWorkspaceCaseBuilder.Build(
            app,
            app.ApplicationProfile,
            Array.Empty<ApplicationWorkspaceTab>(),
            default,
            new ApplicationWorkspaceCaseChrome());

        var invitation = Assert.Single(
            view.IssuedRecordTiles,
            t => t.Key == ApplicationWorkspaceIssuedRecordsCatalog.Invitation);

        Assert.Contains("ApplicationProfileInstance.Issued.ExpirationDate", invitation.ColumnKeys);
        Assert.DoesNotContain("ApplicationProfileInstance.Issued.Expiration", invitation.ColumnKeys);
        Assert.Equal(new DateTime(2027, 1, 16), Invitation.ExpirationFromIssued(new DateTime(2026, 10, 17)));
    }
}
