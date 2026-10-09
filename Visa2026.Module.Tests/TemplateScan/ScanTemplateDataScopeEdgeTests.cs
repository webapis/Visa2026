#nullable enable

using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.TemplateScan;
using Xunit;

namespace Visa2026.Module.Tests.TemplateScan;

/// <summary>
/// Edges for yellow-marks DataScope inference (people-only, empty → fallback, null plan).
/// </summary>
public class ScanTemplateDataScopeEdgeTests
{
    [Fact]
    public void Row_only_tokens_save_as_people_m2m()
    {
        var scope = ScanTemplateDataScope.FromProposedTokens(
            ["{{.PFN}}", "{{.RNUM}}", ".WDUT"],
            ApplicationProfileTemplateDataScope.Both);

        Assert.Equal(ApplicationProfileTemplateDataScope.PeopleM2M, scope);
    }

    [Fact]
    public void Empty_or_whitespace_or_image_only_returns_fallback()
    {
        Assert.Equal(
            ApplicationProfileTemplateDataScope.Both,
            ScanTemplateDataScope.FromProposedTokens(
                null,
                ApplicationProfileTemplateDataScope.Both));

        Assert.Equal(
            ApplicationProfileTemplateDataScope.ApplicationHeader,
            ScanTemplateDataScope.FromProposedTokens(
                ["  ", null, "{{IMAGE:PPH}}"],
                ApplicationProfileTemplateDataScope.ApplicationHeader));
    }

    [Fact]
    public void FromFieldPlan_null_returns_fallback()
    {
        Assert.Equal(
            ApplicationProfileTemplateDataScope.PeopleM2M,
            ScanTemplateDataScope.FromFieldPlan(null, ApplicationProfileTemplateDataScope.PeopleM2M));
    }
}
