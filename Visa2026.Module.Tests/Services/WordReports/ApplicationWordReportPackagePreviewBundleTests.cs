using Visa2026.Module.Services.WordReports;
using Xunit;

namespace Visa2026.Module.Tests.Services.WordReports;

/// <summary>
/// Preview download naming for single vs multi-file Resminamalar office bundles.
/// </summary>
public class ApplicationWordReportPackagePreviewBundleTests
{
    [Fact]
    public void PdfFileName_SingleOriginal_ChangesExtensionToPdf()
    {
        var bundle = new ApplicationWordReportPackagePreviewBundle
        {
            Originals =
            [
                new ApplicationWordReportGeneratedFile
                {
                    FileName = "Borcnama.docx",
                    Content = [1],
                    ContentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                },
            ],
        };

        Assert.Equal("Borcnama.pdf", bundle.PdfFileName);
        Assert.Equal("Borcnama.docx", bundle.Original.FileName);
    }

    [Fact]
    public void PdfFileName_BlankOriginalName_UsesDefault()
    {
        var bundle = new ApplicationWordReportPackagePreviewBundle
        {
            Originals =
            [
                new ApplicationWordReportGeneratedFile
                {
                    FileName = "  ",
                    Content = [1],
                    ContentType = "application/octet-stream",
                },
            ],
        };

        Assert.Equal("report-preview.pdf", bundle.PdfFileName);
    }

    [Fact]
    public void PdfFileName_MultipleOriginals_UsesStableMergedName()
    {
        var bundle = new ApplicationWordReportPackagePreviewBundle
        {
            Originals =
            [
                new ApplicationWordReportGeneratedFile
                {
                    FileName = "one.docx",
                    Content = [1],
                    ContentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                },
                new ApplicationWordReportGeneratedFile
                {
                    FileName = "two.xlsx",
                    Content = [2],
                    ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                },
            ],
        };

        Assert.Equal("report-preview.pdf", bundle.PdfFileName);
        Assert.Equal("one.docx", bundle.Original.FileName);
    }
}
