#nullable enable

using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.TemplateConvert;
using Visa2026.Module.Services.TemplateScan;
using Visa2026.Module.Services.UserReports;
using Xunit;

namespace Visa2026.Module.Tests.TemplateScan;

public class ScanLibraryTokenRewriterTests
{
    private static ApplicationProfilePlaceholderSet Set() =>
        new ApplicationProfilePlaceholderSetService(new UserReportPlaceholderCatalogService()).GetSet(
            new ApplicationProfilePlaceholderSetQuery
            {
                Profile = new ApplicationProfile(),
                DataScope = ApplicationProfileTemplateDataScope.Both,
                TemplateKind = ApplicationProfileTemplateKind.Word,
            });

    [Fact]
    public void Row_only_header_shaped_tokens_become_row_tokens()
    {
        var set = Set();
        Assert.Equal("{{.PVFM}}", ScanLibraryTokenRewriter.Rewrite("{{ds.PVFM}}", set));
        Assert.Equal(
            "{{.PDBT}}, {{.PCBT}}, {{.PBPL}}",
            ScanLibraryTokenRewriter.Rewrite("{{ds.PDBT}}, {{ds.PCBT}}, {{ds.PBPL}}", set));
        Assert.Equal("{{.PFWC}}", ScanLibraryTokenRewriter.Rewrite("{{ds.PFWC}}", set));
    }

    [Fact]
    public void Header_only_tokens_stay_on_ds()
    {
        var set = Set();
        Assert.Equal("{{ds.AFNUM}}", ScanLibraryTokenRewriter.Rewrite("{{ds.AFNUM}}", set));
        Assert.Equal("{{ds.AFNUM}}", ScanLibraryTokenRewriter.Rewrite("{{.AFNUM}}", set));
    }

    [Fact]
    public void Row_tokens_already_in_row_shape_are_unchanged()
    {
        var set = Set();
        Assert.Equal("{{.PFN}}", ScanLibraryTokenRewriter.Rewrite("{{.PFN}}", set));
    }

    [Fact]
    public void Catalog_row_entry_ignores_header_usage()
    {
        var entry = new UserReportPlaceholderCatalogService().GetEntries().Single(e =>
            string.Equals(e.ShortCode, "PVFM", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(UserReportPlaceholderScope.Row, entry.Scope);
        Assert.Equal("{{.PVFM}}", entry.BuildWordToken(UserReportPlaceholderScope.Header));
        Assert.Equal("{{.PVFM}}", entry.BuildWordToken(UserReportPlaceholderScope.Row));
    }

    [Fact]
    public void Image_tokens_use_short_code_so_photo_cells_do_not_wrap()
    {
        var set = Set();
        var entry = new UserReportPlaceholderCatalogService().GetEntries().Single(e =>
            string.Equals(e.ShortCode, "PPH", StringComparison.OrdinalIgnoreCase));
        Assert.Equal("{{IMAGE:PPH}}", entry.BuildWordToken(UserReportPlaceholderScope.Row));
        Assert.Equal("{{IMAGE:PPH}}", ScanLibraryTokenRewriter.Rewrite("{{IMAGE:PPH}}", set));
        Assert.Equal("{{IMAGE:PPH}}", ScanLibraryTokenRewriter.Rewrite("{{IMAGE:Person_Photo}}", set));
    }
}