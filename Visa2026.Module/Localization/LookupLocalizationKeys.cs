using System;
using System.Collections.Generic;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Localization;

/// <summary>Maps global lookup rows to stable <c>LookupStrings.json</c> keys (independent of legacy DB <see cref="LookupBase.LocalizationKey"/> values).</summary>
internal static partial class LookupLocalizationKeys
{
    private static readonly HashSet<string> MaritalSemanticKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "Single", "Minor", "Divorced", "Married", "WidowedNotRemarried", "WidowedRemarried", "Widow",
    };

    private static readonly HashSet<string> GenderSemanticKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "Male", "Female",
    };

    public static string Resolve(LookupBase? row)
    {
        if (row == null)
            return string.Empty;

        return row switch
        {
            Gender => ResolveGender(row),
            MaritalStatus => ResolveMaritalStatus(row),
            ApplicationType => ResolveApplicationType(row),
            VisaType => ResolveVisaType(row),
            VisaCategory => ResolveVisaCategory(row),
            VisaPeriod => ResolveVisaPeriod(row),
            Country => ResolveCountry(row),
            PassportType => ResolvePassportType(row),
            Relationship => ResolveRelationship(row),
            EducationLevel => ResolveEducationLevel(row),
            ApplicationState => ResolveApplicationState(row),
            ApplicationLocation => ResolveApplicationLocation(row),
            Region => ResolveRegion(row),
            ValidityDuration => ResolveValidityDuration(row),
            CheckPoint => ResolveCheckPoint(row),
            Urgency u when !string.IsNullOrWhiteSpace(u.Code) => u.Code.Trim(),
            _ => ResolveStoredKey(row),
        };
    }

    private static string ResolveGender(LookupBase row)
    {
        var fromCode = MapGenderCode(row.Code);
        if (!string.IsNullOrEmpty(fromCode))
            return fromCode;

        var fromLegacyKey = MapGenderCode(row.LocalizationKey);
        if (!string.IsNullOrEmpty(fromLegacyKey))
            return fromLegacyKey;

        if (IsSemanticKey(row.LocalizationKey, GenderSemanticKeys))
            return row.LocalizationKey!.Trim();

        return string.Empty;
    }

    private static string ResolveMaritalStatus(LookupBase row)
    {
        var fromCode = MapMaritalStatusCode(row.Code);
        if (!string.IsNullOrEmpty(fromCode))
            return fromCode;

        var fromLegacyKey = MapMaritalStatusCode(row.LocalizationKey);
        if (!string.IsNullOrEmpty(fromLegacyKey))
            return fromLegacyKey;

        if (IsSemanticKey(row.LocalizationKey, MaritalSemanticKeys))
            return row.LocalizationKey!.Trim();

        return string.Empty;
    }

    private static string ResolveApplicationType(LookupBase row)
    {
        if (!string.IsNullOrWhiteSpace(row.Name))
            return row.Name.Trim();

        if (!string.IsNullOrWhiteSpace(row.LocalizationKey))
            return row.LocalizationKey.Trim();

        return string.Empty;
    }

    private static string ResolveStoredKey(LookupBase row)
    {
        if (!string.IsNullOrWhiteSpace(row.LocalizationKey))
            return row.LocalizationKey.Trim();

        return row.Code?.Trim() ?? string.Empty;
    }

    private static bool IsSemanticKey(string? key, HashSet<string> allowed) =>
        !string.IsNullOrWhiteSpace(key) && allowed.Contains(key.Trim());

    private static string MapGenderCode(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return string.Empty;

        return code.Trim() switch
        {
            "Erkek" => "Male",
            "Aýal" or "Ayal" => "Female",
            _ => string.Empty,
        };
    }

    private static string MapMaritalStatusCode(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return string.Empty;

        return code.Trim() switch
        {
            "Sallah" => "Single",
            "Çaga" => "Minor",
            "Aýrylşan" => "Divorced",
            "Öýlenen" => "Married",
            "Durmuşa Çykmadyk" => "WidowedNotRemarried",
            "Durmuşa Çykan" => "WidowedRemarried",
            "Dul" => "Widow",
            _ => string.Empty,
        };
    }
}
