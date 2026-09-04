using System;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace Visa2026.Module.Services;

/// <summary>
/// Chrome/PDFium cannot paint XFA in an iframe. Detect the packet so preview can use pdf.js.
/// Spire-filled forms often keep <c>/XFA</c> only inside Flate object streams.
/// </summary>
public static class PdfXfaDocument
{
    private static readonly byte[][] Markers =
    {
        Encoding.ASCII.GetBytes("/XFA"),
        Encoding.ASCII.GetBytes("/NeedsRendering"),
        Encoding.ASCII.GetBytes("xdp:xdp"),
        Encoding.ASCII.GetBytes("xmlns:xfa"),
        Encoding.ASCII.GetBytes("xfa:datasets"),
    };

    private static readonly byte[] StreamToken = Encoding.ASCII.GetBytes("stream");
    private static readonly byte[] EndStreamToken = Encoding.ASCII.GetBytes("endstream");

    public static bool ContainsXfa(byte[]? pdf)
    {
        if (pdf == null || pdf.Length < 8)
            return false;

        var span = pdf.AsSpan();
        if (ContainsAnyMarker(span))
            return true;

        return ContainsMarkerInFlateStreams(pdf);
    }

    private static bool ContainsAnyMarker(ReadOnlySpan<byte> bytes)
    {
        foreach (var marker in Markers)
        {
            if (bytes.IndexOf(marker) >= 0)
                return true;
        }

        return false;
    }

    private static bool ContainsMarkerInFlateStreams(byte[] pdf)
    {
        var span = pdf.AsSpan();
        var offset = 0;
        for (var n = 0; n < 80 && offset < pdf.Length; n++)
        {
            var rel = span.Slice(offset).IndexOf(StreamToken);
            if (rel < 0)
                return false;

            var dataStart = offset + rel + StreamToken.Length;
            if (dataStart < pdf.Length && pdf[dataStart] == (byte)'\r')
                dataStart++;
            if (dataStart < pdf.Length && pdf[dataStart] == (byte)'\n')
                dataStart++;

            var endRel = span.Slice(dataStart).IndexOf(EndStreamToken);
            if (endRel < 0)
                return false;

            var length = endRel;
            while (length > 0 && (pdf[dataStart + length - 1] == (byte)'\n' || pdf[dataStart + length - 1] == (byte)'\r'))
                length--;

            if (length is > 8 and < 2_000_000 && TryInflateContainsMarker(pdf.AsSpan(dataStart, length)))
                return true;

            offset = dataStart + endRel + EndStreamToken.Length;
        }

        return false;
    }

    private static bool TryInflateContainsMarker(ReadOnlySpan<byte> compressed)
    {
        return TryInflateContainsMarker(compressed, zlib: true)
            || TryInflateContainsMarker(compressed, zlib: false);
    }

    private static bool TryInflateContainsMarker(ReadOnlySpan<byte> compressed, bool zlib)
    {
        try
        {
            using var input = new MemoryStream(compressed.ToArray(), writable: false);
            Stream decoder = zlib
                ? new ZLibStream(input, CompressionMode.Decompress)
                : new DeflateStream(input, CompressionMode.Decompress);
            using (decoder)
            using (var output = new MemoryStream())
            {
                decoder.CopyTo(output);
                if (output.Length is <= 0 or > 4_000_000)
                    return false;

                return ContainsAnyMarker(output.ToArray());
            }
        }
        catch (InvalidDataException)
        {
            return false;
        }
        catch (NotSupportedException)
        {
            return false;
        }
    }
}