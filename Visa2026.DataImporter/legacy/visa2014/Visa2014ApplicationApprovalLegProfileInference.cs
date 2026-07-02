using Visa2026.Module.BusinessObjects;

namespace Visa2026.DataImporter.Legacy.Visa2014;

/// <summary>
/// Per-application <see cref="ApprovalLegProfile.Code"/> from legacy ministry routing fields.
/// Aligns with <c>Generate-ProjectContractCalikEnergiCatalog.ps1</c> and tenant profile seeds.
/// </summary>
internal static class Visa2014ApplicationApprovalLegProfileInference
{
    private const string MinistryTe = "T\u00FCrkmenenergo";
    private const string MinistryEn = "Energetika";
    private const string MinistryGu = "Gurlu\u015Fyk";

    public static string? ResolveProfileCode(Visa2014ApplicationRawRow raw)
    {
        var hasLeg2 = HasConstructionForward(raw.DateForwardedToMinConstruction, raw.DocNumberForwardedToMinConstruction);
        var hasMinistryForward = IsLegacyDateSet(raw.DateForwardedToMinistry);
        var isTurkmenenergo = !string.IsNullOrWhiteSpace(raw.AppliedMinistryTitleL)
            && raw.AppliedMinistryTitleL.Contains("energo", StringComparison.OrdinalIgnoreCase);

        var leg1Short = Visa2014ProjectContractMinistryLegPreviewExporter.MapMinistryShortName(raw.AppliedMinistryTitle);
        var isEnergoFlow = false;

        if (hasMinistryForward && (isTurkmenenergo || !string.IsNullOrWhiteSpace(leg1Short)))
        {
            isEnergoFlow = isTurkmenenergo;
        }
        else
        {
            var combined = $"{raw.ContractMinistryTitle} {raw.ContractMinistryTitleL}";
            if (combined.Contains("energo", StringComparison.OrdinalIgnoreCase))
            {
                isEnergoFlow = true;
            }
            else
            {
                leg1Short = Visa2014ProjectContractMinistryLegPreviewExporter.MapMinistryShortName(raw.ContractMinistryTitle);
                if (string.IsNullOrWhiteSpace(leg1Short))
                    leg1Short = Visa2014ProjectContractMinistryLegPreviewExporter.MapMinistryShortName(raw.ContractMinistryTitleL);
            }
        }

        if (string.IsNullOrWhiteSpace(leg1Short))
            leg1Short = MinistryEn;

        IReadOnlyList<string> legShortNames = isEnergoFlow || string.Equals(leg1Short, MinistryEn, StringComparison.Ordinal)
            ? hasLeg2 ? [MinistryTe, MinistryEn, MinistryGu] : [MinistryTe, MinistryEn]
            : hasLeg2 ? [leg1Short, MinistryGu] : [leg1Short];

        return ApprovalLegProfileCodeHelper.ResolveCodeFromLegShortNames(legShortNames);
    }

    private static bool HasConstructionForward(DateTime? date, string? docNumber) =>
        IsLegacyDateSet(date)
        || !string.IsNullOrWhiteSpace(docNumber);

    private static bool IsLegacyDateSet(DateTime? date) =>
        date is { Year: >= 2000 };
}
