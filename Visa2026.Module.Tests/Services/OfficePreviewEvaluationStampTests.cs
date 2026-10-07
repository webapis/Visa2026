using System.IO.Compression;
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

    [Fact]
    public void ContainsStamp_detects_banner_inside_a_flate_stream()
    {
        var pdf = PdfWithFlateStream("For evaluation purposes only. Please register an existing license.");
        Assert.True(OfficePreviewEvaluationStamp.ContainsStamp(pdf));
    }

    [Fact]
    public void ContainsStamp_ignores_a_flate_stream_without_the_banner()
    {
        var pdf = PdfWithFlateStream("Dasary yurt rayatlarynyn sanawy");
        Assert.False(OfficePreviewEvaluationStamp.ContainsStamp(pdf));
    }

    private static byte[] PdfWithFlateStream(string pageText)
    {
        using var compressed = new MemoryStream();
        using (var zlib = new ZLibStream(compressed, CompressionLevel.SmallestSize, leaveOpen: true))
        {
            var payload = Encoding.ASCII.GetBytes(pageText);
            zlib.Write(payload, 0, payload.Length);
        }

        var body = compressed.ToArray();
        using var pdf = new MemoryStream();
        var head = Encoding.ASCII.GetBytes("%PDF-1.4\n1 0 obj\n<</Filter /FlateDecode /Length " + body.Length + ">>\nstream\n");
        var tail = Encoding.ASCII.GetBytes("\nendstream\nendobj\n%%EOF");
        pdf.Write(head, 0, head.Length);
        pdf.Write(body, 0, body.Length);
        pdf.Write(tail, 0, tail.Length);
        return pdf.ToArray();
    }
}
