using System.Globalization;
using Visa2026.Module.Localization.Generated;

namespace Visa2026.Module.Localization;

/// <summary>Layer A runtime UI strings (controllers, confirmations). Regenerate catalog via GenerateModelLocalization.</summary>
public static class VisaUiMessages
{
    public static string Get(string key)
    {
        string culture = NormalizeCulture(CultureInfo.CurrentUICulture.Name);
        if (VisaUiMessageCatalog.TryGet(culture, key, out string? message))
        {
            return message!;
        }

        return key;
    }

    public static string Format(string key, params object[] args) =>
        string.Format(CultureInfo.CurrentUICulture, Get(key), args);

    private static string NormalizeCulture(string? cultureName)
    {
        if (string.IsNullOrWhiteSpace(cultureName))
        {
            return "en-US";
        }

        string[] supported = ["en-US", "tr-TR", "tk-TM", "ru-RU"];
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

        return "en-US";
    }
}
