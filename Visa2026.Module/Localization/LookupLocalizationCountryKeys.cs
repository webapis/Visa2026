using System;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Localization;

internal static partial class LookupLocalizationKeys
{
    private static string ResolveCountry(LookupBase row)
    {
        if (!string.IsNullOrWhiteSpace(row.Code) && row.Code.Trim().Length == 3)
            return row.Code.Trim().ToUpperInvariant();

        if (!string.IsNullOrWhiteSpace(row.LocalizationKey) && row.LocalizationKey.Trim().Length == 3)
            return row.LocalizationKey.Trim().ToUpperInvariant();

        return string.Empty;
    }
}
