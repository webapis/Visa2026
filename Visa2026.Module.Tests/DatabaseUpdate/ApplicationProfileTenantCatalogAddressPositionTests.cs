using Visa2026.Module.BusinessObjects;
using Visa2026.Module.DatabaseUpdate;
using Visa2026.Module.DatabaseUpdate.LookupCatalogs;
using Visa2026.Module.Services;
using Xunit;

namespace Visa2026.Module.Tests.DatabaseUpdate;

public class ApplicationProfileTenantCatalogAddressPositionTests
{
    [Fact]
    public void Calik_catalog_requires_address_and_position_except_change_invitation()
    {
        Assert.True(ApplicationProfileTenantCatalogLoader.TryLoadRows(out var rows));
        Assert.NotEmpty(rows);

        foreach (var row in rows)
        {
            Assert.True(row.RequirePersonPosition, row.Code);
            var isChangeInvitation =
                string.Equals(row.ProfileCatalogKey, "App_Change_Inv", StringComparison.OrdinalIgnoreCase)
                || string.Equals(row.Code, "change_invitation", StringComparison.OrdinalIgnoreCase);
            if (isChangeInvitation)
            {
                Assert.False(row.RequirePersonAddressOfResidence, row.Code);
                Assert.False(row.RequireInvitationAddress, row.Code);
                Assert.False(row.RequireInvitationPlace, row.Code);
                continue;
            }

            Assert.True(row.RequirePersonAddressOfResidence, row.Code);
            var collectsStay = ApplicationProfileInvitationAddressPolicy.CollectsInvitationStay(
                row.ProfileCatalogKey, row.Code);
            if (collectsStay)
            {
                Assert.True(row.RequireInvitationAddress, row.Code);
                Assert.True(row.RequireInvitationPlace, row.Code);
            }
        }
    }

    [Fact]
    public void Mapper_turns_on_address_and_position_for_all_families_except_change_invitation()
    {
        var issuance = new ApplicationProfile();
        ApplicationProfileFromApplicationTypeMapper.Apply(issuance, new ApplicationType());
        Assert.True(issuance.RequirePersonAddressOfResidence);
        Assert.True(issuance.RequirePersonPosition);

        var trip = new ApplicationProfile();
        ApplicationProfileFromApplicationTypeMapper.Apply(trip, new ApplicationType { ShowBusinessTrips = true });
        Assert.True(trip.RequirePersonAddressOfResidence);
        Assert.True(trip.RequirePersonPosition);

        var invitation = new ApplicationProfile();
        ApplicationProfileFromApplicationTypeMapper.Apply(invitation, new ApplicationType { Name = "App_Inv" });
        Assert.True(invitation.RequirePersonAddressOfResidence);
        Assert.True(invitation.RequireInvitationAddress);
        Assert.True(invitation.RequireInvitationPlace);

        var changeInvitation = new ApplicationProfile();
        ApplicationProfileFromApplicationTypeMapper.Apply(
            changeInvitation,
            new ApplicationType { Name = "App_Change_Inv", Code = "change_invitation" });
        Assert.False(changeInvitation.RequirePersonAddressOfResidence);
        Assert.False(changeInvitation.RequireInvitationAddress);
        Assert.False(changeInvitation.RequireInvitationPlace);
        Assert.True(changeInvitation.RequirePersonPosition);
    }
}