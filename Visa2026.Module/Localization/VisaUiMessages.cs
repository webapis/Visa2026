using System.Globalization;
using Visa2026.Module.Localization.Generated;

namespace Visa2026.Module.Localization;

/// <summary>Layer A runtime UI strings (controllers, confirmations). Regenerate catalog via GenerateModelLocalization.</summary>
public static class VisaUiMessages
{
    public const string DefaultCultureName = "en-US";

    public static string Get(string key) => Get(key, null);

    public static string Get(string key, string? cultureName)
    {
        string culture = NormalizeCulture(cultureName ?? CultureInfo.CurrentUICulture.Name);
        if (VisaUiMessageCatalog.TryGet(culture, key, out string? message))
        {
            return message!;
        }

        return key;
    }

    public static string Format(string key, params object[] args) =>
        FormatForCulture(null, key, args);

    /// <summary>Formats using an explicit culture (not the second argument — avoids ambiguity when {0} is a string).</summary>
    public static string FormatForCulture(string? cultureName, string key, params object[] args)
    {
        string culture = NormalizeCulture(cultureName ?? CultureInfo.CurrentUICulture.Name);
        return string.Format(CultureInfo.GetCultureInfo(culture), Get(key, culture), args);
    }

    public static string NormalizeCultureName(string? cultureName) => NormalizeCulture(cultureName);

    private static string NormalizeCulture(string? cultureName)
    {
        if (string.IsNullOrWhiteSpace(cultureName))
        {
            return DefaultCultureName;
        }

        string[] supported = [DefaultCultureName, "tr-TR", "tk-TM", "ru-RU"];
        foreach (string supportedCulture in supported)
        {
            if (string.Equals(cultureName, supportedCulture, StringComparison.OrdinalIgnoreCase))
            {
                return supportedCulture;
            }
        }

        try
        {
            var culture = CultureInfo.GetCultureInfo(cultureName);
            foreach (string supportedCulture in supported)
            {
                var supportedInfo = CultureInfo.GetCultureInfo(supportedCulture);
                if (culture.Equals(supportedInfo)
                    || culture.TwoLetterISOLanguageName.Equals(
                        supportedInfo.TwoLetterISOLanguageName,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return supportedCulture;
                }
            }
        }
        catch (CultureNotFoundException)
        {
            // fall through
        }

        return DefaultCultureName;
    }
}
