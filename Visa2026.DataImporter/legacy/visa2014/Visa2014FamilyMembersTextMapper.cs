using Visa2026.Module.Services;

namespace Visa2026.DataImporter.Legacy.Visa2014;

/// <summary>
/// Maps legacy <c>dbo.MaritalStatus.StatusL</c> narrative to <c>Person.VisaApplicationFamilyMembersText</c>.
/// </summary>
internal static class Visa2014FamilyMembersTextMapper
{
    private static readonly HashSet<string> IgnoredLiterals = new(StringComparer.OrdinalIgnoreCase)
    {
        ".",
        "-",
        "0",
        "Ýok",
        "Yok",
        "Sallah",
    };

    public static string? FromLegacyStatusL(string? statusL, string? legacyMaritalStatusStatus = null)
    {
        if (VisaFamilyMemberLinesHelper.IsLegacySingleMaritalStatus(legacyMaritalStatusStatus))
        {
            return VisaFamilyMemberLinesHelper.NoneValue;
        }

        if (string.IsNullOrWhiteSpace(statusL))
            return null;

        var trimmed = statusL.Trim();
        if (IgnoredLiterals.Contains(trimmed))
            return null;

        return LegacyMaritalStatusLParser.ToStorageText(trimmed);
    }
}
