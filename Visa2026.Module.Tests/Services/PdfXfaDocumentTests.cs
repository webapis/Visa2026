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
}