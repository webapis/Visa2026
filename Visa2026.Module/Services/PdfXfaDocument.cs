using System;
using System.Text;

namespace Visa2026.Module.Services;

/// <summary>
/// Chrome/PDFium cannot paint XFA in an iframe. Detect the packet so preview can use pdf.js.
/// </summary>
public static class PdfXfaDocument
{
    private static readonly byte[] XfaMarker = Encoding.ASCII.GetBytes("/XFA");

    public static bool ContainsXfa(byte[]? pdf)
    {
        if (pdf == null || pdf.Length < 8)
            return false;

        return pdf.AsSpan().IndexOf(XfaMarker) >= 0;
    }
}