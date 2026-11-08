#nullable enable

using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public class ApplicationProfileInvitationAddressPolicyTests
{
    [Theory]
    [InlineData("App_Inv", null, true)]
    [InlineData("app_inv", "other", true)]
    [InlineData(" App_Inv ", null, true)]
    [InlineData(null, "get_invitation", true)]
    [InlineData("x", "GET_INVITATION", true)]
    [InlineData("App_Inv_X", "get_invitation_x", false)]
    [InlineData(null, null, false)]
    [InlineData("", " ", false)]
    public void IsCaklykAlmak_matches_catalog_key_or_code(string? catalogKey, string? code, bool expected)
    {
        Assert.Equal(expected, ApplicationProfileInvitationAddressPolicy.IsCaklykAlmak(catalogKey, code));
    }

    [Fact]
    public void IsCaklykAlmakType_uses_ApplicationType_seed_Name_not_NameTm()
    {
#pragma warning disable CS0618
        var type = new ApplicationType
        {
            Name = ApplicationProfileInvitationAddressPolicy.ApplicationTypeName,
            NameTm = "Çakylyk almak",
        };
#pragma warning restore CS0618

        Assert.True(ApplicationProfileInvitationAddressPolicy.IsCaklykAlmakType(type));
    }

    [Fact]
    public void IsCaklykAlmakType_rejects_null_and_non_seed_names()
    {
        Assert.False(ApplicationProfileInvitationAddressPolicy.IsCaklykAlmakType(null));

#pragma warning disable CS0618
        var other = new ApplicationType { Name = "App_Visa", NameTm = "Çakylyk almak" };
#pragma warning restore CS0618
        Assert.False(ApplicationProfileInvitationAddressPolicy.IsCaklykAlmakType(other));
    }

    [Fact]
    public void Constants_match_Çakylyk_Almak_seed_keys()
    {
        Assert.Equal("App_Inv", ApplicationProfileInvitationAddressPolicy.ProfileCatalogKey);
        Assert.Equal("get_invitation", ApplicationProfileInvitationAddressPolicy.ProfileCode);
        Assert.Equal("App_Inv", ApplicationProfileInvitationAddressPolicy.ApplicationTypeName);
    }
}
