namespace Visa2026.Module.Services.UserReports;

/// <summary>
/// FM invitation letter phrase: joined dependent relationships (genitive) plus one sponsor
/// name and position — e.g. <c>aýalynyň we çagasynyň (İzzet Taşdelen-İşe goýberiş…)</c>.
/// One sponsor per letter.
/// </summary>
public static class FamilyMemberSponsorPhrase
{
    public static string Format(string? relationshipsGenitive, string? sponsorFullName, string? sponsorPositionTm)
    {
        var rel = relationshipsGenitive?.Trim() ?? string.Empty;
        var name = sponsorFullName?.Trim() ?? string.Empty;
        var pos = sponsorPositionTm?.Trim() ?? string.Empty;

        if (rel.Length == 0 && name.Length == 0 && pos.Length == 0)
            return string.Empty;

        string? inner = null;
        if (name.Length > 0 && pos.Length > 0)
            inner = name + "-" + pos;
        else if (name.Length > 0)
            inner = name;
        else if (pos.Length > 0)
            inner = pos;

        if (inner == null)
            return rel;

        if (rel.Length == 0)
            return "(" + inner + ")";

        return rel + " (" + inner + ")";
    }
}