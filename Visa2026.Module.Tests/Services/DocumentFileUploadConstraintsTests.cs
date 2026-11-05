using System.Text;
using DevExpress.Persistent.BaseImpl.EF;
using Visa2026.Module.Services;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public class DocumentFileUploadConstraintsTests
{
    [Fact]
    public void TryValidate_null_file_is_allowed()
    {
        Assert.True(DocumentFileUploadConstraints.TryValidate(null, out var error));
        Assert.Null(error);
    }

    [Fact]
    public void TryValidate_rejects_empty_size()
    {
        var file = new FileData { FileName = "scan.pdf", Size = 0, Content = [] };
        Assert.False(DocumentFileUploadConstraints.TryValidate(file, out var error));
        Assert.Equal("The file is empty.", error);
    }

    [Theory]
    [InlineData("notes.docx")]
    [InlineData("notes.exe")]
    [InlineData("notes")]
    public void TryValidate_rejects_disallowed_extensions(string fileName)
    {
        var file = new FileData
        {
            FileName = fileName,
            Size = 16,
            Content = Encoding.ASCII.GetBytes("%PDF-1.4 placeholder"),
        };

        Assert.False(DocumentFileUploadConstraints.TryValidate(file, out var error));
        Assert.NotNull(error);
        Assert.Contains("not allowed", error, System.StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TryValidate_allows_pdf_when_content_matches()
    {
        var content = Encoding.ASCII.GetBytes("%PDF-1.7\n%....");
        var file = new FileData { FileName = "passport.pdf", Size = content.Length, Content = content };

        Assert.True(DocumentFileUploadConstraints.TryValidate(file, out var error));
        Assert.Null(error);
    }

    [Fact]
    public void TryValidate_rejects_mismatched_pdf_extension()
    {
        var content = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46 };
        var file = new FileData { FileName = "fake.pdf", Size = content.Length, Content = content };

        Assert.False(DocumentFileUploadConstraints.TryValidate(file, out var error));
        Assert.NotNull(error);
        Assert.Contains("does not match", error, System.StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TryValidate_allows_when_content_too_short_to_sniff()
    {
        var file = new FileData
        {
            FileName = "tiny.pdf",
            Size = 4,
            Content = Encoding.ASCII.GetBytes("%PD"),
        };

        Assert.True(DocumentFileUploadConstraints.TryValidate(file, out var error));
        Assert.Null(error);
    }

    [Fact]
    public void IsLikelyPdf_requires_percent_pdf_prefix()
    {
        Assert.True(DocumentFileUploadConstraints.IsLikelyPdf(Encoding.ASCII.GetBytes("%PDF-")));
        Assert.False(DocumentFileUploadConstraints.IsLikelyPdf(Encoding.ASCII.GetBytes("%PD")));
        Assert.False(DocumentFileUploadConstraints.IsLikelyPdf(Encoding.ASCII.GetBytes("PDF-1")));
    }
}
