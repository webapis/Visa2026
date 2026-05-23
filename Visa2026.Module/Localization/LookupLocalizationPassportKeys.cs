using System;
using System.Collections.Generic;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Localization;

internal static partial class LookupLocalizationKeys
{
    private static readonly HashSet<string> PassportTypeSemanticKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "APD", "AML", "SH", "DZ", "AGL", "YD", "EU", "BS", "US", "PD", "PT", "LBG", "YG", "P", "AUN", "UN", "PG",
    };

    private static readonly Dictionary<string, string> PassportTypeNameToKey =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["APD-AKRD.DIPLOMAT PASPORT"] = "APD",
            ["AML-AKRD.MILLI PASPORT"] = "AML",
            ["SH-ŞAHADATNAMA"] = "SH",
            ["DZ-DENIZJININ PASPORTY"] = "DZ",
            ["AGL-AKRD.GULLUK PASPORT"] = "AGL",
            ["YD-ÝURDUNA DOLANYŞ ŞAHADATNAMA"] = "YD",
            ["EU-ÝEWROSUÝUZ PASPORT"] = "EU",
            ["BS-BOSGUN ŞAHADATNAMASY"] = "BS",
            ["US-UÇUŞ ŞAHADATNAMA"] = "US",
            ["PD-DIPLOMAT PASPORTY"] = "PD",
            ["PT-ÝOL RESMINAMASI"] = "PT",
            ["LBG-RAÝATSYZ ŞAHSYÝET"] = "LBG",
            ["YG-HEM.YASM.YGTYÝARNAMA"] = "YG",
            ["P - MILLI PASPORT"] = "P",
            ["AUN-AKRD.UN PASPORT"] = "AUN",
            ["UN-BMG PASPORT"] = "UN",
            ["PG-GULLUK PASPORTY"] = "PG",
        };

    private static string ResolvePassportType(LookupBase row)
    {
        if (row is PassportType passportType && !string.IsNullOrWhiteSpace(passportType.PdfForm_Code))
            return passportType.PdfForm_Code.Trim();

        return ResolveCatalogKey(row, PassportTypeSemanticKeys, PassportTypeNameToKey);
    }
}
