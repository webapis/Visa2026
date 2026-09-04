#nullable enable

using Visa2026.Module.Services.TemplateScan;
using Xunit;

namespace Visa2026.Module.Tests.TemplateScan;

public class ScanPlaceholderRoleCatalogTests
{
    [Theory]
    [InlineData("PFN", ScanPlaceholderRole.Applicant)]
    [InlineData("RPFN", ScanPlaceholderRole.Wekil)]
    [InlineData("RPCL", ScanPlaceholderRole.Wekil)]
    [InlineData("CHFN", ScanPlaceholderRole.Signatory)]
    [InlineData("ACFNM", ScanPlaceholderRole.Signatory)]
    [InlineData("ASPN", ScanPlaceholderRole.Company)]
    [InlineData("AFNUM", ScanPlaceholderRole.Case)]
    [InlineData("RNUM", ScanPlaceholderRole.Applicant)]
    public void Resolve_maps_known_short_codes(string shortCode, ScanPlaceholderRole expected)
    {
        Assert.Equal(expected, ScanPlaceholderRoleCatalog.Resolve(shortCode));
    }

    [Fact]
    public void Describe_wekil_warns_not_applicant()
    {
        var text = ScanPlaceholderRoleCatalog.Describe("RPFN", "Representative full name");
        Assert.Contains("wekil", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("never a visa applicant", text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Describe_applicant_warns_not_wekil()
    {
        var text = ScanPlaceholderRoleCatalog.Describe("PFN", "Person full name");
        Assert.Contains("roster / applicant", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("not the Configuration wekil", text, StringComparison.OrdinalIgnoreCase);
    }
}
