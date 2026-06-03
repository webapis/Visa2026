using System;
using System.Collections.Generic;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Localization;

internal static partial class LookupLocalizationKeys
{
    private static readonly HashSet<string> VisaTypeSemanticKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "BS1", "TR1", "TR2", "OF", "EX", "HM", "DP", "PR1", "PR2", "HL", "DR", "IN", "ST", "SP2", "BS2", "TU", "FM", "SP1", "WP",
    };

    private static readonly HashSet<string> VisaCategorySemanticKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "Single", "Double", "Multiple",
    };

    private static readonly HashSet<string> VisaPeriodSemanticKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "Month1", "Month2", "Month3", "Month4", "Month5", "Month6", "Month12", "Year1",
        "Day7", "Day10", "Day15", "Day20", "PerWorkPermit", "PerVisa",
    };

    private static readonly Dictionary<string, string> VisaTypeNameToKey =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["BS1-İşerwürlik"] = "BS1",
            ["TR1-Üstaşyr"] = "TR1",
            ["TR2-Üstaşyr"] = "TR2",
            ["OF-Gulluk"] = "OF",
            ["EX-Çykyş"] = "EX",
            ["HM-Ynsanperwelik"] = "HM",
            ["DP-Diplomat"] = "DP",
            ["PR1-Hususy(90)"] = "PR1",
            ["PR2-Hususy(365)"] = "PR2",
            ["HL-Saglyk"] = "HL",
            ["DR-Sürüji"] = "DR",
            ["IN-Maýa Goýum"] = "IN",
            ["ST-Talyp"] = "ST",
            ["SP2-Sport"] = "SP2",
            ["BS2-Işewürlik"] = "BS2",
            ["TU-Syýahatçylyk"] = "TU",
            ["FM-Maşgala"] = "FM",
            ["SP1-Sport"] = "SP1",
            ["WP-Işçi Wiza"] = "WP",
        };

    private static readonly Dictionary<string, string> VisaCategoryNameToKey =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["bir gezeklik"] = "Single",
            ["Iki gezeklik"] = "Double",
            ["köp gezeklik"] = "Multiple",
        };

    private static readonly Dictionary<string, string> VisaPeriodNameToKey =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["1 (bir) aý"] = "Month1",
            ["2 (iki) aý"] = "Month2",
            ["3 (üç) aý"] = "Month3",
            ["4 (dört) aý"] = "Month4",
            ["5 aý"] = "Month5",
            ["6 (alty) aý"] = "Month6",
            ["12 (on iki) aý"] = "Month12",
            ["1 (bir) ýyl"] = "Year1",
            ["7 (ýedi) gün"] = "Day7",
            ["10 (on) gün"] = "Day10",
            ["15 (on bäş) gün"] = "Day15",
            ["20 (yigrimi) gün"] = "Day20",
            ["rugsatnama möhletine çenli"] = "PerWorkPermit",
            ["wiza möhletine çenli"] = "PerVisa",
        };

    private static string ResolveVisaType(LookupBase row) =>
        ResolveCatalogKey(row, VisaTypeSemanticKeys, VisaTypeNameToKey);

    private static string ResolveVisaCategory(LookupBase row) =>
        ResolveCatalogKey(row, VisaCategorySemanticKeys, VisaCategoryNameToKey);

    private static string ResolveVisaPeriod(LookupBase row) =>
        ResolveCatalogKey(row, VisaPeriodSemanticKeys, VisaPeriodNameToKey);

    private static string ResolveCatalogKey(
        LookupBase row,
        HashSet<string> semanticKeys,
        IReadOnlyDictionary<string, string> nameToKey)
    {
        if (TryMapName(PrimaryTitle(row), nameToKey, out var fromName))
            return fromName;

        if (TryMapName(row.LocalizationKey, nameToKey, out var fromLegacyKey))
            return fromLegacyKey;

        if (IsSemanticKey(row.LocalizationKey, semanticKeys))
            return row.LocalizationKey!.Trim();

        return string.Empty;
    }

    private static bool TryMapName(string? name, IReadOnlyDictionary<string, string> nameToKey, out string key)
    {
        key = string.Empty;
        if (string.IsNullOrWhiteSpace(name))
            return false;

        return nameToKey.TryGetValue(name.Trim(), out key!);
    }
}
