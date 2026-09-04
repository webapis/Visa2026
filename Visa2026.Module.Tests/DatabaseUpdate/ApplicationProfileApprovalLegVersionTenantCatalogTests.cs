using Visa2026.Module.DatabaseUpdate.LookupCatalogs;
using Xunit;

namespace Visa2026.Module.Tests.DatabaseUpdate;

public class ApplicationProfileApprovalLegVersionTenantCatalogTests
{
    [Theory]
    [InlineData("approved", true)]
    [InlineData("Approved", true)]
    [InlineData("", false)]
    [InlineData("draft", false)]
    public void IsApproved_requires_literal_approved(string signOff, bool expected) =>
        Assert.Equal(expected, ApplicationProfileApprovalLegVersionTenantCatalogLoader.IsApproved(signOff));
}