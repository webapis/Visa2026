#nullable enable

using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.TemplateScan;
using Xunit;

namespace Visa2026.Module.Tests.TemplateScan;

public class ScanTemplateDataScopeTests
{
    [Fact]
    public void Header_only_ds_tokens_save_as_application_header()
    {
        var scope = ScanTemplateDataScope.FromProposedTokens(
            ["{{ds.AFNUM}}", "{{ds.ADAT}}", "{{ds.MSRV}}", "{{ds.TPCNT}}", "{{ds.ACPOS}}", "{{ds.CHFN}}"],
            ApplicationProfileTemplateDataScope.Both);

        Assert.Equal(ApplicationProfileTemplateDataScope.ApplicationHeader, scope);
    }

    [Fact]
    public void Photo_token_does_not_force_per_person_scope()
    {
        var scope = ScanTemplateDataScope.FromProposedTokens(
            ["{{ds.AFNUM}}", "{{IMAGE:PPH}}"],
            ApplicationProfileTemplateDataScope.Both);

        Assert.Equal(ApplicationProfileTemplateDataScope.ApplicationHeader, scope);
    }

    [Fact]
    public void Mixed_header_and_row_tokens_stay_both()
    {
        var scope = ScanTemplateDataScope.FromProposedTokens(
            ["{{ds.AFNUM}}", "{{.PFN}}"],
            ApplicationProfileTemplateDataScope.ApplicationHeader);

        Assert.Equal(ApplicationProfileTemplateDataScope.Both, scope);
    }
}