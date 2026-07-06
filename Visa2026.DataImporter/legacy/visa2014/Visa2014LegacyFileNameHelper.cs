using System.Text;
using System.Text.RegularExpressions;

namespace Visa2026.DataImporter.Legacy.Visa2014;

internal static class Visa2014LegacyFileNameHelper
{
    private static readonly Regex InvalidFileNameChars = new(@"[^\w\-]+", RegexOptions.Compiled);

    public static string BuildPassportCopyFileName(string? passportNumber, byte[] content, int copyIndex = 1) =>
        BuildCopyFileName("passport", SanitizeToken(passportNumber, "unknown"), content, copyIndex);

    public static string BuildVisaCopyFileName(string? visaNumber, byte[] content, int copyIndex = 1) =>
        BuildCopyFileName("visa", SanitizeToken(visaNumber, "unknown"), content, copyIndex);

    public static string BuildDiplomaCopyFileName(string? personFullName, byte[] content, int copyIndex = 1) =>
        BuildCopyFileName("diploma", SanitizeToken(personFullName, "unknown"), content, copyIndex);

    public static string BuildMedicalCopyFileName(string? personFullName, byte[] content, int copyIndex = 1) =>
        BuildCopyFileName("medical", SanitizeToken(personFullName, "unknown"), content, copyIndex);

    public static string BuildWorkPermitCopyFileName(string? workPermitNumber, byte[] content, int copyIndex = 1) =>
        BuildCopyFileName("work-permit", SanitizeToken(workPermitNumber, "unknown"), content, copyIndex);

    public static string BuildInvitationCopyFileName(string? invitationNumber, byte[] content, int copyIndex = 1) =>
        BuildCopyFileName("invitation", SanitizeToken(invitationNumber, "unknown"), content, copyIndex);

    public static string BuildFamilyProofCopyFileName(string? personFullName, byte[] content, int copyIndex = 1) =>
        BuildCopyFileName("family-proof", SanitizeToken(personFullName, "unknown"), content, copyIndex);

    private static string BuildCopyFileName(string prefix, string token, byte[] content, int copyIndex)
    {
        var ext = GuessExtension(content);
        var suffix = copyIndex <= 1 ? string.Empty : $"-{copyIndex}";
        return $"{prefix}-{token}-copy{suffix}{ext}";
    }

    internal static string SanitizeToken(string? value, string fallback)
    {
        var trimmed = value?.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
            return fallback;

        var normalized = trimmed.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);
        foreach (var ch in normalized)
        {
            if (char.GetUnicodeCategory(ch) == System.Globalization.UnicodeCategory.NonSpacingMark)
                continue;
            builder.Append(ch);
        }

        var ascii = builder.ToString().Normalize(NormalizationForm.FormC);
        ascii = InvalidFileNameChars.Replace(ascii, "-");
        ascii = Regex.Replace(ascii, "-{2,}", "-").Trim('-', '_');
        return string.IsNullOrWhiteSpace(ascii) ? fallback : ascii;
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
