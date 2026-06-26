namespace Visa2026.DataImporter.Legacy.Visa2014;

internal static class Visa2014LegacyFileNameHelper
{
    public static string BuildFileName(Guid legacyCopyOid, byte[] content)
    {
        var ext = GuessExtension(content);
        return $"passport-copy-{legacyCopyOid:N}{ext}";
    }

    private static string GuessExtension(byte[] content)
    {
        if (content.Length >= 5
            && content[0] == (byte)'%'
            && content[1] == (byte)'P'
            && content[2] == (byte)'D'
            && content[3] == (byte)'F')
            return ".pdf";

        if (content.Length >= 3
            && content[0] == 0xFF
            && content[1] == 0xD8
            && content[2] == 0xFF)
            return ".jpg";

        if (content.Length >= 8
            && content[0] == 0x89
            && content[1] == (byte)'P'
            && content[2] == (byte)'N'
            && content[3] == (byte)'G')
            return ".png";

        if (content.Length >= 6
            && content[0] == (byte)'G'
            && content[1] == (byte)'I'
            && content[2] == (byte)'F')
            return ".gif";

        if (content.Length >= 2
            && content[0] == (byte)'B'
            && content[1] == (byte)'M')
            return ".bmp";

        if (content.Length >= 4
            && ((content[0] == 0x49 && content[1] == 0x49)
                || (content[0] == 0x4D && content[1] == 0x4D)))
            return ".tif";

        return ".pdf";
    }
}
