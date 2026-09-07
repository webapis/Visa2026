using Visa2026.Module.DatabaseUpdate;
using Xunit;

namespace Visa2026.Module.Tests.DatabaseUpdate;

public class ApplicationProfileImportCodeResolverTests
{
    [Theory]
    [InlineData("App_Inv", "get_invitation")]
    [InlineData("App_Inv_FM", "get_invitation_fm")]
    [InlineData("App_Inv_And_WP", "get_invitation_wp")]
    [InlineData("App_Inv_According_to_WP", "get_invitation_according_to_wp")]
    public void ResolveImportProfileCode_TenantCalikCatalog_UsesUniqueProfileCode(string typeName, string expectedCode)
    {
        Assert.Equal(expectedCode, ApplicationProfileCatalogPreviewHelper.ResolveImportProfileCode(typeName));
    }
}