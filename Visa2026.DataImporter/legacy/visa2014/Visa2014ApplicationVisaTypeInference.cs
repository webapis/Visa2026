namespace Visa2026.DataImporter.Legacy.Visa2014;

/// <summary>
/// Legacy ApplicationProfileInstance has no VisaType FK. Infer Visa2026 <c>Application.VisaType</c>
/// from the resolved <c>ApplicationType.Name</c> for types that use VisaPeriod.
/// Keys are <see cref="VisaType.LocalizationKey"/> from visa-type.json.
/// </summary>
internal static class Visa2014ApplicationVisaTypeInference
{
    /// <summary>ApplicationType.Name → VisaType.LocalizationKey.</summary>
    private static readonly Dictionary<string, string> ApplicationTypeNameToVisaTypeKey =
        new(StringComparer.Ordinal)
        {
            // WP-Işçi Wiza
            ["App_Inv_And_WP"] = "WP",
            ["App_Visa_and_WP_Ext"] = "WP",
            ["App_Inv_According_to_WP"] = "WP",
            ["App_Visa_Ext_According_to_WP"] = "WP",
            ["App_Visa_Ext"] = "WP",

            // BS1-İşerwürlik
            ["App_Inv"] = "BS1",

            // FM-Maşgala
            ["App_Inv_FM"] = "FM",
            ["App_Visa_Ext_FM"] = "FM",
            ["App_Visa_For_New_Born_FM"] = "FM",

            // EX-Çykyş
            ["App_Exit_Visa"] = "EX",

            // OF-Gulluk — ShowVisaPeriod types adjacent to legacy invitation family
            ["App_Sevice_Passport"] = "OF",
        };

    /// <summary>
    /// Returns VisaType LocalizationKey when <paramref name="applicationTypeName"/> has an
    /// inference rule; otherwise null (do not invent a type).
    /// </summary>
    public static string? TryGetVisaTypeLocalizationKey(string? applicationTypeName)
    {
        if (string.IsNullOrWhiteSpace(applicationTypeName))
            return null;

        return ApplicationTypeNameToVisaTypeKey.TryGetValue(applicationTypeName.Trim(), out var key)
            ? key
            : null;
    }

    public static bool TryInferVisaType(string? applicationTypeName, out string localizationKey)
    {
        localizationKey = TryGetVisaTypeLocalizationKey(applicationTypeName) ?? string.Empty;
        return !string.IsNullOrEmpty(localizationKey);
    }
}
