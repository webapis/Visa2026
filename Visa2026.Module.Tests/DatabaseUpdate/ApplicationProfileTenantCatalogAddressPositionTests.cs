using Visa2026.Module.BusinessObjects;
using Visa2026.Module.DatabaseUpdate;
using Visa2026.Module.DatabaseUpdate.LookupCatalogs;
using Visa2026.Module.Services;
using Xunit;

namespace Visa2026.Module.Tests.DatabaseUpdate;

public class ApplicationProfileTenantCatalogAddressPositionTests
{
    [Fact]
    public void Calik_catalog_requires_address1_not_legacy_invitation_stay()
    {
        Assert.True(ApplicationProfileTenantCatalogLoader.TryLoadRows(out var rows));
        Assert.NotEmpty(rows);

        foreach (var row in rows)
        {
            Assert.True(row.RequirePersonPosition, row.Code);
            Assert.False(row.RequireInvitationAddress, row.Code);
            Assert.False(row.RequireInvitationPlace, row.Code);

            var isChangeInvitation =
                string.Equals(row.ProfileCatalogKey, "App_Change_Inv", StringComparison.OrdinalIgnoreCase)
                || string.Equals(row.Code, "change_invitation", StringComparison.OrdinalIgnoreCase);
            if (isChangeInvitation)
            {
                Assert.False(row.RequireAddress1, row.Code);
                continue;
            }

            var collectsStay = ApplicationProfileInvitationAddressPolicy.CollectsInvitationStay(
                row.ProfileCatalogKey, row.Code);
            if (collectsStay)
                Assert.True(row.RequireAddress1, row.Code);
            else
                Assert.False(row.RequireAddress1, row.Code);
        }
    }

    [Fact]
    public void Mapper_turns_on_address1_for_invitation_stay_not_legacy_fields()
    {
        var issuance = new ApplicationProfile();
        ApplicationProfileFromApplicationTypeMapper.Apply(issuance, new ApplicationType());
        Assert.True(issuance.RequirePersonPosition);

        var invitation = new ApplicationProfile();
        ApplicationProfileFromApplicationTypeMapper.Apply(invitation, new ApplicationType { Name = "App_Inv" });
        Assert.True(invitation.RequireAddress1);
#pragma warning disable CS0618
        Assert.False(invitation.RequireInvitationAddress);
        Assert.False(invitation.RequireInvitationPlace);
#pragma warning restore CS0618

        var changeInvitation = new ApplicationProfile();
        ApplicationProfileFromApplicationTypeMapper.Apply(
            changeInvitation,
            new ApplicationType { Name = "App_Change_Inv", Code = "change_invitation" });
#pragma warning disable CS0618
        Assert.False(changeInvitation.RequireInvitationAddress);
        Assert.False(changeInvitation.RequireInvitationPlace);
#pragma warning restore CS0618
        Assert.True(changeInvitation.RequirePersonPosition);
    }
}