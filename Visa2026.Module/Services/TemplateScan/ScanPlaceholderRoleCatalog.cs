#nullable enable

using Visa2026.Module.Services.UserReports;

namespace Visa2026.Module.Services.TemplateScan;

/// <summary>
/// Role + one-line description for Azure (and Review hints).
/// Short codes stay the reply key; this is extra context, not a replacement.
/// </summary>
public enum ScanPlaceholderRole
{
    Applicant = 0,
    Signatory = 1,
    Wekil = 2,
    Company = 3,
    Case = 4,
}

public static class ScanPlaceholderRoleCatalog
{
    private static readonly HashSet<string> Wekil = new(StringComparer.OrdinalIgnoreCase)
    {
        "RPFN", "RPOS", "RPPH", "RPPL", "RPPN", "RPPA", "RPPD", "RPCL",
    };

    private static readonly HashSet<string> Signatory = new(StringComparer.OrdinalIgnoreCase)
    {
        "ACFNM", "ACPOS", "CHFN", "CHPL", "CHPN", "CHPA", "CHPD",
    };

    private static readonly HashSet<string> Company = new(StringComparer.OrdinalIgnoreCase)
    {
        "ACODE", "ACNAM", "ACADR", "ACPHN", "ACEML", "ACTAX", "ACRGL", "ACRDT", "ASPN",
    };

    private static readonly HashSet<string> CaseHeader = new(StringComparer.OrdinalIgnoreCase)
    {
        "AFNUM", "ADAT", "Urgency_NameTm", "VPER", "VCAT", "MSRV", "SPFNM", "ABZLN",
    };

    public static ScanPlaceholderRole Resolve(string? shortCode)
    {
        if (string.IsNullOrWhiteSpace(shortCode))
            return ScanPlaceholderRole.Case;

        if (Wekil.Contains(shortCode))
            return ScanPlaceholderRole.Wekil;
        if (Signatory.Contains(shortCode))
            return ScanPlaceholderRole.Signatory;
        if (Company.Contains(shortCode))
            return ScanPlaceholderRole.Company;
        if (CaseHeader.Contains(shortCode))
            return ScanPlaceholderRole.Case;

        return ScanPlaceholderRole.Applicant;
    }

    public static string Describe(UserReportPlaceholderCatalogEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        return Describe(entry.ShortCode, entry.LabelEn);
    }

    public static string Describe(string? shortCode, string? labelEn)
    {
        var label = string.IsNullOrWhiteSpace(labelEn) ? (shortCode ?? "placeholder") : labelEn.Trim();
        return Resolve(shortCode) switch
        {
            ScanPlaceholderRole.Wekil =>
                label + " — tenant Authorized Representative (wekil) from Company / Signatories; never a visa applicant or roster person",
            ScanPlaceholderRole.Signatory =>
                label + " — Authorized Signatory (gol çekiji); not the wekil or applicant",
            ScanPlaceholderRole.Company =>
                label + " — tenant company (Configuration)",
            ScanPlaceholderRole.Case =>
                label + " — application / case header",
            _ =>
                label + " — roster / applicant person; not the Configuration wekil",
        };
    }
}
