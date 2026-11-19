using Visa2026.Module.DatabaseUpdate;
using Visa2026.Module.DatabaseUpdate.LookupCatalogs;
using Xunit;

namespace Visa2026.Module.Tests.DatabaseUpdate;

public class ApplicationProfileCalikPersonLastCountSeedsTests
{
    [Fact]
    public void Apply_PasportChange_PassportTwoVisaOne()
    {
        var row = new ApplicationProfileTenantCatalogRow { Code = "pasport_change" };
        ApplicationProfileCalikPersonLastCountSeeds.Apply(row);
        Assert.Equal(2, row.PersonPassportLastCount);
        Assert.Equal(1, row.PersonVisaLastCount);
    }

    [Fact]
    public void Apply_CancelInvitationWp_InvitationAndWorkPermitTwo()
    {
        var row = new ApplicationProfileTenantCatalogRow { Code = "cancel_invitation_wp" };
        ApplicationProfileCalikPersonLastCountSeeds.Apply(row);
        Assert.Equal(2, row.PersonInvitationItemLastCount);
        Assert.Equal(2, row.PersonWorkPermitItemLastCount);
    }

    [Fact]
    public void Apply_CancelInvitation_InvitationTwo()
    {
        var row = new ApplicationProfileTenantCatalogRow { Code = "cancel_invitation" };
        ApplicationProfileCalikPersonLastCountSeeds.Apply(row);
        Assert.Equal(2, row.PersonInvitationItemLastCount);
    }

    [Fact]
    public void Apply_CancelVisa_VisaTwo()
    {
        var row = new ApplicationProfileTenantCatalogRow { Code = "cancel_visa" };
        ApplicationProfileCalikPersonLastCountSeeds.Apply(row);
        Assert.Equal(2, row.PersonVisaLastCount);
    }

    [Fact]
    public void Apply_CancelVisaWp_VisaAndWorkPermitTwo()
    {
        var row = new ApplicationProfileTenantCatalogRow { Code = "cancel_visa_wp" };
        ApplicationProfileCalikPersonLastCountSeeds.Apply(row);
        Assert.Equal(2, row.PersonVisaLastCount);
        Assert.Equal(2, row.PersonWorkPermitItemLastCount);
    }

    [Fact]
    public void Apply_CancelWorkPermit_WorkPermitTwo()
    {
        var row = new ApplicationProfileTenantCatalogRow { Code = "cancel_workpermit" };
        ApplicationProfileCalikPersonLastCountSeeds.Apply(row);
        Assert.Equal(2, row.PersonWorkPermitItemLastCount);
    }

    [Fact]
    public void Apply_RegInfoChangePassport_Unchanged()
    {
        var row = new ApplicationProfileTenantCatalogRow { Code = "reg_info_change_passport" };
        ApplicationProfileCalikPersonLastCountSeeds.Apply(row);
        Assert.Equal(1, row.PersonPassportLastCount);
        Assert.Equal(1, row.PersonVisaLastCount);
        Assert.Equal(1, row.PersonAddressOfResidenceMaxCount);
    }

    [Fact]
    public void Apply_GetInvitation_AddressMaxTwo()
    {
        var row = new ApplicationProfileTenantCatalogRow { Code = "get_invitation" };
        ApplicationProfileCalikPersonLastCountSeeds.Apply(row);
        Assert.Equal(2, row.PersonAddressOfResidenceMaxCount);
        Assert.Equal(1, row.PersonPassportLastCount);
    }

    [Fact]
    public void Apply_GetInvitationWp_AddressMaxStaysOne()
    {
        var row = new ApplicationProfileTenantCatalogRow { Code = "get_invitation_wp" };
        ApplicationProfileCalikPersonLastCountSeeds.Apply(row);
        Assert.Equal(1, row.PersonAddressOfResidenceMaxCount);
    }
}