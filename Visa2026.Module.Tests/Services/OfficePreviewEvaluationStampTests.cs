using System.Text;
using Visa2026.Module.Services.WordReports;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public class OfficePreviewEvaluationStampTests
{
    [Fact]
    public void ContainsStamp_detects_office_file_api_banner()
    {
        var pdf = Encoding.Latin1.GetBytes("%PDF-1.7\nFor evaluation purposes only. Please register an existing license.");
        Assert.True(OfficePreviewEvaluationStamp.ContainsStamp(pdf));
    }

    [Fact]
    public void ContainsStamp_ignores_clean_pdf()
    {
        var pdf = Encoding.Latin1.GetBytes("%PDF-1.7\nSAHSY KAGYZY");
        Assert.False(OfficePreviewEvaluationStamp.ContainsStamp(pdf));
    }
}
