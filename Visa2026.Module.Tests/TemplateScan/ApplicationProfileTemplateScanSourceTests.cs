#nullable enable

using DevExpress.Persistent.BaseImpl.EF;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.TemplateScan;
using Xunit;

namespace Visa2026.Module.Tests.TemplateScan;

public class ApplicationProfileTemplateScanSourceTests
{
    [Fact]
    public void TryReadReviewBytes_prefers_yellow_source_over_mapped_template()
    {
        var template = new ApplicationProfileTemplate
        {
            SourceFile = new FileData { FileName = "Forma_16_yellow.docx", Content = [1, 2, 3] },
            TemplateFile = new FileData { FileName = "Forma_16.docx", Content = [9, 9] },
        };

        Assert.True(ApplicationProfileTemplateScanSource.TryReadReviewBytes(
            objectSpace: null,
            template,
            out var content,
            out var fileName,
            out var fromYellowSource));

        Assert.False(fromYellowSource);
        Assert.Equal("Forma_16.docx", fileName);
        Assert.Equal(new byte[] { 9, 9 }, content);
    }

    [Fact]
    public void TryReadReviewBytes_falls_back_to_mapped_template_when_source_empty()
    {
        var template = new ApplicationProfileTemplate
        {
            SourceFile = new FileData { FileName = "missing.docx", Content = Array.Empty<byte>() },
            TemplateFile = new FileData { FileName = "Forma_16.docx", Content = [7, 8] },
        };

        Assert.True(ApplicationProfileTemplateScanSource.TryReadReviewBytes(
            objectSpace: null,
            template,
            out var content,
            out var fileName,
            out var fromYellowSource));

        Assert.False(fromYellowSource);
        Assert.Equal("Forma_16.docx", fileName);
        Assert.Equal(new byte[] { 7, 8 }, content);
    }

    [Fact]
    public void TryReadReviewBytes_prefers_office_source_only_when_it_still_has_yellow()
    {
        var yellow = ScanOfficeYellowExtractorTests.CreateWordFixture("Yerkin Didem");
        var mapped = ScanOfficeLibraryTokenExtractorTests.CreateWordTokenFixture("{{.PFN}}");
        var template = new ApplicationProfileTemplate
        {
            SourceFile = new FileData { FileName = "F16_yellow.docx", Content = yellow },
            TemplateFile = new FileData { FileName = "F16.docx", Content = mapped },
        };

        Assert.True(ApplicationProfileTemplateScanSource.TryReadReviewBytes(
            objectSpace: null,
            template,
            out var content,
            out var fileName,
            out var fromYellowSource));

        Assert.True(fromYellowSource);
        Assert.Equal("F16_yellow.docx", fileName);
        Assert.Equal(yellow, content);
    }

    [Fact]
    public void TryReadReviewBytes_skips_source_that_is_already_mapped_tokens()
    {
        var mapped = ScanOfficeLibraryTokenExtractorTests.CreateWordTokenFixture("{{.PFN}}");
        var template = new ApplicationProfileTemplate
        {
            SourceFile = new FileData { FileName = "F16_source.docx", Content = mapped },
            TemplateFile = new FileData { FileName = "F16.docx", Content = mapped },
        };

        Assert.True(ApplicationProfileTemplateScanSource.TryReadReviewBytes(
            objectSpace: null,
            template,
            out _,
            out _,
            out var fromYellowSource));

        Assert.False(fromYellowSource);
    }

    [Fact]
    public void TryReadReviewBytes_false_when_both_files_missing()
    {
        Assert.False(ApplicationProfileTemplateScanSource.TryReadReviewBytes(
            objectSpace: null,
            new ApplicationProfileTemplate(),
            out var content,
            out var fileName,
            out var fromYellowSource));

        Assert.False(fromYellowSource);
        Assert.Empty(content);
        Assert.Equal(string.Empty, fileName);
    }
}