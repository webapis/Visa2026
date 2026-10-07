using System;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace Visa2026.Module.Services.WordReports;

/// <summary>Detects the DevExpress Office File API evaluation banner in a preview PDF.</summary>
internal static class OfficePreviewEvaluationStamp
{
    private const int MaxInflatedStreamBytes = 8 * 1024 * 1024;

    private static readonly string[] Markers =
    {
        "for evaluation purposes only",
        "evaluation warning",
        "please register an existing license",
        "devexpress product libraries",
    };

    internal static bool ContainsStamp(byte[]? pdf)
    {
        if (pdf == null || pdf.Length < 8)
            return false;

        if (HasMarker(pdf))
            return true;

        return InflatedStreamsHaveMarker(pdf);
    }

    private static bool HasMarker(byte[] bytes)
    {
        var text = Encoding.Latin1.GetString(bytes);
        foreach (var marker in Markers)
        {
            if (text.Contains(marker, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Office File API writes the red banner into a FlateDecode page stream, so a raw-byte search misses it.
    /// </summary>
    private static bool InflatedStreamsHaveMarker(byte[] pdf)
    {
        var searchFrom = 0;
        while (searchFrom < pdf.Length)
        {
            var streamAt = IndexOfAscii(pdf, "stream", searchFrom);
            if (streamAt < 0)
                return false;

            var dataStart = streamAt + "stream".Length;
            if (dataStart < pdf.Length && pdf[dataStart] == (byte)'\r')
                dataStart++;
            if (dataStart < pdf.Length && pdf[dataStart] == (byte)'\n')
                dataStart++;

            var endAt = IndexOfAscii(pdf, "endstream", dataStart);
            if (endAt < 0)
                return false;

            var dataEnd = endAt;
            if (dataEnd > dataStart && pdf[dataEnd - 1] == (byte)'\n')
                dataEnd--;
            if (dataEnd > dataStart && pdf[dataEnd - 1] == (byte)'\r')
                dataEnd--;

            if (dataEnd > dataStart
                && TryInflate(pdf, dataStart, dataEnd - dataStart, out var inflated)
                && HasMarker(inflated))
                return true;

            searchFrom = endAt + "endstream".Length;
        }

        return false;
    }

    private static int IndexOfAscii(byte[] pdf, string ascii, int start)
    {
        var needle = Encoding.ASCII.GetBytes(ascii);
        var last = pdf.Length - needle.Length;
        for (var i = start; i <= last; i++)
        {
            var match = true;
            for (var j = 0; j < needle.Length; j++)
            {
                if (pdf[i + j] != needle[j])
                {
                    match = false;
                    break;
                }
            }

            if (match)
                return i;
        }

        return -1;
    }

    private static bool TryInflate(byte[] pdf, int offset, int count, out byte[] inflated)
    {
        inflated = Array.Empty<byte>();
        if (count < 2)
            return false;

        try
        {
            using var input = new MemoryStream(pdf, offset, count, writable: false);
            using var zlib = new ZLibStream(input, CompressionMode.Decompress);
            using var output = new MemoryStream();
            var buffer = new byte[8192];
            int read;
            while ((read = zlib.Read(buffer, 0, buffer.Length)) > 0)
            {
                if (output.Length + read > MaxInflatedStreamBytes)
                    return false;

                output.Write(buffer, 0, read);
            }

            inflated = output.ToArray();
            return inflated.Length > 0;
        }
        catch (InvalidDataException)
        {
            return false;
        }
        catch (IOException)
        {
            return false;
        }
    }
}
