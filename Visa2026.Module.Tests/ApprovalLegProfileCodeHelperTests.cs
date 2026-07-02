using Visa2026.Module.BusinessObjects;
using Xunit;

namespace Visa2026.Module.Tests;

public sealed class ApprovalLegProfileCodeHelperTests
{
    [Theory]
    [InlineData(new[] { "Türkmenenergo", "Energetika" }, "TE-EN")]
    [InlineData(new[] { "Türkmenenergo", "Energetika", "Gurluşyk" }, "TE-EN-GU")]
    [InlineData(new[] { "TNGIZ" }, "NG")]
    [InlineData(new[] { "TNGIZ", "Gurluşyk" }, "NG-GU")]
    [InlineData(new[] { "Aşgabat häkimlik" }, "AH")]
    public void ResolveCodeFromLegShortNames_maps_known_chains(string[] legs, string expected) =>
        Assert.Equal(expected, ApprovalLegProfileCodeHelper.ResolveCodeFromLegShortNames(legs));
}
