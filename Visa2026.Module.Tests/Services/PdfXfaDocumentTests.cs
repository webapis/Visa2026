using System.IO;
using System.IO.Compression;
using System.Text;
using Visa2026.Module.Services;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public class PdfXfaDocumentTests
{
    [Fact]
    public void ContainsXfa_Empty_IsFalse()
    {
        Assert.False(PdfXfaDocument.ContainsXfa(null));
        Assert.False(PdfXfaDocument.ContainsXfa([]));
        Assert.False(PdfXfaDocument.ContainsXfa(Encoding.ASCII.GetBytes("%PDF-1.7")));
    }

    [Fact]
    public void ContainsXfa_Marker_IsTrue()
    {
        var pdf = Encoding.ASCII.GetBytes("%PDF-1.7\n1 0 obj\n<< /XFA [ 2 0 R ] >>\nendobj");
        Assert.True(PdfXfaDocument.ContainsXfa(pdf));
    }

    [Fact]
    public void ContainsXfa_XdpPacket_IsTrue()
    {
        var pdf = Encoding.ASCII.GetBytes("%PDF-1.7\n<xdp:xdp xmlns:xfa=\"http://www.xfa.org/schema/xfa-data/1.0/\">");
        Assert.True(PdfXfaDocument.ContainsXfa(pdf));
    }

    [Fact]
    public void ContainsXfa_FlateStream_IsTrue()
    {
        var inner = Encoding.ASCII.GetBytes("<< /XFA [ 2 0 R ] /NeedsRendering true >>");
        using var compressed = new MemoryStream();
        using (var zlib = new ZLibStream(compressed, CompressionLevel.SmallestSize, leaveOpen: true))
            zlib.Write(inner, 0, inner.Length);

        var header = Encoding.ASCII.GetBytes("%PDF-1.7\n1 0 obj\n<< /Length " + compressed.Length + " /Filter /FlateDecode >>\nstream\n");
        var footer = Encoding.ASCII.GetBytes("\nendstream\nendobj");
        var pdf = new byte[header.Length + compressed.Length + footer.Length];
        Buffer.BlockCopy(header, 0, pdf, 0, header.Length);
        Buffer.BlockCopy(compressed.ToArray(), 0, pdf, header.Length, (int)compressed.Length);
        Buffer.BlockCopy(footer, 0, pdf, header.Length + (int)compressed.Length, footer.Length);

        Assert.True(PdfXfaDocument.ContainsXfa(pdf));
    }
}