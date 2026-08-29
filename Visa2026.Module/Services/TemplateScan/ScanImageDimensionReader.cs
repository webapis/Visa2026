#nullable enable

namespace Visa2026.Module.Services.TemplateScan;

internal static class ScanImageDimensionReader
{
    internal static bool TryReadImageDimensions(byte[] content, out int width, out int height)
    {
        width = 0;
        height = 0;

        if (content.Length >= 24 && content[0] == 0x89 && content[1] == 0x50 && content[2] == 0x4E && content[3] == 0x47)
            return TryReadPngDimensions(content, out width, out height);

        if (content.Length >= 4 && content[0] == 0xFF && content[1] == 0xD8)
            return TryReadJpegDimensions(content, out width, out height);

        return false;
    }

    private static bool TryReadPngDimensions(byte[] content, out int width, out int height)
    {
        width = (content[16] << 24) | (content[17] << 16) | (content[18] << 8) | content[19];
        height = (content[20] << 24) | (content[21] << 16) | (content[22] << 8) | content[23];
        return width > 0 && height > 0;
    }

    private static bool TryReadJpegDimensions(byte[] content, out int width, out int height)
    {
        width = 0;
        height = 0;
        var index = 2;

        while (index + 9 < content.Length)
        {
            if (content[index] != 0xFF)
            {
                index++;
                continue;
            }

            var marker = content[index + 1];
            if (marker is 0xD8 or 0x01)
            {
                index += 2;
                continue;
            }

            if (marker is 0xD9 or 0xDA)
                break;

            var segmentLength = (content[index + 2] << 8) | content[index + 3];
            if (segmentLength < 2)
                break;

            var isSof = marker is 0xC0 or 0xC1 or 0xC2 or 0xC3 or 0xC5 or 0xC6 or 0xC7 or 0xC9 or 0xCA or 0xCB or 0xCD or 0xCE or 0xCF;
            if (isSof && index + 7 < content.Length)
            {
                height = (content[index + 5] << 8) | content[index + 6];
                width = (content[index + 7] << 8) | content[index + 8];
                return width > 0 && height > 0;
            }

            index += 2 + segmentLength;
        }

        return false;
    }
}
