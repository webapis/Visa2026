using System;
using System.Collections.Generic;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Localization;

internal static partial class LookupLocalizationKeys
{
    private static readonly HashSet<string> EducationLevelSemanticKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "SpecialSecondary", "Higher", "SchoolStudent", "UnderSchoolAge", "Secondary",
    };

    private static readonly HashSet<string> ApplicationStateSemanticKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "IS_BEING_PREPARED", "1_REVIEW_STARTED", "2_REVIEW_STARTED", "1_REVIEW_APPROVED", "2_REVIEW_APPROVED",
        "1_REVIEW_REJECTED", "2_REVIEW_REJECTED", "PROCESS_STARTED", "PROCESS_CANCELLED", "PROCESS_REJECTED", "PROCESS_ISSUED",
    };

    private static readonly HashSet<string> ApplicationLocationSemanticKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "AT_OFFICE", "AT_THE_MINISTERY_1", "AT_THE_MINISTERY_2", "AT_MIGRATION_SERVICE",
    };

    private static readonly HashSet<string> RegionSemanticKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "AS", "BN", "AH", "MR", "DZ", "LB",
    };

    private static readonly HashSet<string> ValidityDurationSemanticKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "Month3", "Month6", "Year1",
    };

    private static readonly HashSet<string> CheckPointSemanticKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "TurkmenbashyAir", "Etrek", "Sarahs", "AshgabatAir", "Howdan", "DashoguzCity", "Garabogaz", "Farap",
        "Serhetabat", "Ymamnazar", "TurkmenbashySea", "Koneurgench", "Artyk", "TurkmenabatAir", "Tallymerjen",
    };

    private static readonly Dictionary<string, string> EducationLevelNameToKey =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["Orta"] = "Secondary",
            ["Mekdep Okuwçysy"] = "SchoolStudent",
            ["Mekdep Ýaşyna Ýetmedik"] = "UnderSchoolAge",
            ["Mekdep Yasyna Yetmedik"] = "UnderSchoolAge",
            ["Ýörite Orta"] = "SpecialSecondary",
            ["Yorite Orta"] = "SpecialSecondary",
            ["Ýokary"] = "Higher",
            ["Yokary"] = "Higher",
        };

    private static readonly Dictionary<int, string> EducationLevelPdfFormToKey = new()
    {
        [1] = "SpecialSecondary",
        [2] = "Higher",
        [3] = "SchoolStudent",
        [4] = "UnderSchoolAge",
        [5] = "Secondary",
    };

    private static readonly Dictionary<string, string> ApplicationStateNameToKey =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["TAÝÝARLYKDA"] = "IS_BEING_PREPARED",
            ["TAYYARLYKDA"] = "IS_BEING_PREPARED",
            ["1-NJI IŞ YLALAŞYKDA"] = "1_REVIEW_STARTED",
            ["2-NJI IŞ YLALAŞYKDA"] = "2_REVIEW_STARTED",
            ["1-NJI IŞ YLALAŞYK ALYNDY"] = "1_REVIEW_APPROVED",
            ["2-NJI IŞ YLALAŞYK ALYNDY"] = "2_REVIEW_APPROVED",
            ["1-NJI IŞ YLALAŞYK BERILMEDI"] = "1_REVIEW_REJECTED",
            ["2-NJI IŞ YLALAŞYK BERILMEDI"] = "2_REVIEW_REJECTED",
            ["İŞLENMEKDE"] = "PROCESS_STARTED",
            ["ISLENMEKDE"] = "PROCESS_STARTED",
            ["ÝÜZTUTMA ÝATYRYLDY"] = "PROCESS_CANCELLED",
            ["YUZTUTMA YATYRYLDY"] = "PROCESS_CANCELLED",
            ["GARŞYLYK BERILDI"] = "PROCESS_REJECTED",
            ["GARSYLYK BERILDI"] = "PROCESS_REJECTED",
            ["RESMILEŞDİRİLDİ"] = "PROCESS_ISSUED",
            ["RESMILESdirildi"] = "PROCESS_ISSUED",
        };

    private static readonly Dictionary<string, string> ApplicationLocationNameToKey =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["OFISDE"] = "AT_OFFICE",
            ["1-NJI MINISTRLIKDE"] = "AT_THE_MINISTERY_1",
            ["2-NJI MINISTRLIKDE"] = "AT_THE_MINISTERY_2",
            ["MIGRASIÝA GULLUGYNDA"] = "AT_MIGRATION_SERVICE",
            ["MIGRASIYA GULLUGYNDA"] = "AT_MIGRATION_SERVICE",
        };

    private static readonly Dictionary<string, string> RegionNameToKey =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["Aşgabat şäheri"] = "AS",
            ["Asgabat saheri"] = "AS",
            ["Balkan welaýaty"] = "BN",
            ["Ahal welaýaty"] = "AH",
            ["Mary welaýaty"] = "MR",
            ["Daşoguz welaýaty"] = "DZ",
            ["Lebap welaýaty"] = "LB",
        };

    private static readonly Dictionary<string, string> ValidityDurationNameToKey =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["3 aý"] = "Month3",
            ["3 ay"] = "Month3",
            ["6 aý"] = "Month6",
            ["6 ay"] = "Month6",
            ["1 ýyl"] = "Year1",
            ["1 yyl"] = "Year1",
        };

    private static readonly Dictionary<int, string> ValidityDurationDaysToKey = new()
    {
        [90] = "Month3",
        [180] = "Month6",
        [365] = "Year1",
    };

    private static readonly Dictionary<string, string> CheckPointNameToKey =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["Türkmenbaşy howa menzilindäki MGP"] = "TurkmenbashyAir",
            ["Etrek MGP"] = "Etrek",
            ["Sarahs MGP"] = "Sarahs",
            ["Aşgabat şäher howa menzilindäki MGP"] = "AshgabatAir",
            ["Howdan MGP"] = "Howdan",
            ["Daşoguz ş. MGP"] = "DashoguzCity",
            ["Garabogaz MGP"] = "Garabogaz",
            ["Farap MGP"] = "Farap",
            ["Serhetabat ş. MGP"] = "Serhetabat",
            ["Ymamnazar MGP"] = "Ymamnazar",
            ["Türkmenbaşy deňiz menzilindäki MGP"] = "TurkmenbashySea",
            ["Köneürgenç ş. MGP"] = "Koneurgench",
            ["Artyk MGP"] = "Artyk",
            ["Türkmenabat howa menzilindäki MGP"] = "TurkmenabatAir",
            ["Tallymerjen MGP"] = "Tallymerjen",
        };

    private static string ResolveEducationLevel(LookupBase row)
    {
        if (row is EducationLevel level && EducationLevelPdfFormToKey.TryGetValue(level.PdfForm_Code, out var fromPdf))
            return fromPdf;

        return ResolveCatalogKey(row, EducationLevelSemanticKeys, EducationLevelNameToKey);
    }

    private static string ResolveApplicationState(LookupBase row)
    {
        if (!string.IsNullOrWhiteSpace(row.Code))
            return row.Code.Trim();

        return ResolveCatalogKey(row, ApplicationStateSemanticKeys, ApplicationStateNameToKey);
    }

    private static string ResolveApplicationLocation(LookupBase row)
    {
        if (!string.IsNullOrWhiteSpace(row.Code))
            return row.Code.Trim();

        return ResolveCatalogKey(row, ApplicationLocationSemanticKeys, ApplicationLocationNameToKey);
    }

    private static string ResolveRegion(LookupBase row)
    {
        if (row is Region region && !string.IsNullOrWhiteSpace(region.PdfForm_Code))
            return region.PdfForm_Code.Trim().ToUpperInvariant();

        return ResolveCatalogKey(row, RegionSemanticKeys, RegionNameToKey);
    }

    private static string ResolveValidityDuration(LookupBase row)
    {
        if (row is ValidityDuration duration && ValidityDurationDaysToKey.TryGetValue(duration.NumberOfDays, out var fromDays))
            return fromDays;

        return ResolveCatalogKey(row, ValidityDurationSemanticKeys, ValidityDurationNameToKey);
    }

    private static string ResolveCheckPoint(LookupBase row) =>
        ResolveCatalogKey(row, CheckPointSemanticKeys, CheckPointNameToKey);
}
