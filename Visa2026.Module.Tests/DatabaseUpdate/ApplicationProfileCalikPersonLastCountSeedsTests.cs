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
    }
}