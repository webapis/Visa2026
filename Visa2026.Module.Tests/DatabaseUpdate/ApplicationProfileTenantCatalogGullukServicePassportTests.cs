using Visa2026.Module.DatabaseUpdate.LookupCatalogs;
using Xunit;

namespace Visa2026.Module.Tests.DatabaseUpdate;

/// <summary>
/// Gulluk service-passport seed must produce invitations (and require invitation address/place)
/// so deploy sync turns May produce Invitation on for officers.
/// </summary>
public sealed class ApplicationProfileTenantCatalogGullukServicePassportTests
{
    [Fact]
    public void Calik_get_invitation_service_passport_ProducesInvitation_WithAddressPlace()
    {
        Assert.True(ApplicationProfileTenantCatalogLoader.TryLoadRows(out var rows));
        var gulluk = Assert.Single(rows, r => r.Code == "get_invitation_service_passport");

        Assert.True(gulluk.ProduceInvitation);
        Assert.False(gulluk.ProduceVisa);
        Assert.False(gulluk.ProduceWorkPermit);
        Assert.True(gulluk.RequireInvitationAddress);
        Assert.True(gulluk.RequireInvitationPlace);
        Assert.Equal("Issuance", gulluk.ActionFamily);
        Assert.Equal("ViaMinistries", gulluk.ProgressRoute);
    }

    [Fact]
    public void Calik_ordinary_get_invitation_StillProducesInvitation()
    {
        Assert.True(ApplicationProfileTenantCatalogLoader.TryLoadRows(out var rows));
        var invitation = Assert.Single(rows, r => r.Code == "get_invitation");

        Assert.True(invitation.ProduceInvitation);
        Assert.True(invitation.RequireInvitationAddress);
        Assert.True(invitation.RequireInvitationPlace);
    }
}
