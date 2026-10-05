using System;
using System.Linq;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services;

/// <summary>
/// Application form field 30: last issued visa degree, entry type, number, and validity.
/// </summary>
public static class PdfLastIssuedVisaText
{
    public static string Format(Visa visa)
    {
        if (visa == null)
            return null;

        var degree = FirstNonEmpty(visa.VisaType?.NameTm, visa.VisaType?.LocalizationKey);
        var entryType = FirstNonEmpty(visa.VisaCategory?.NameTm, visa.VisaCategory?.LocalizationKey);
        var number = string.IsNullOrWhiteSpace(visa.VisaNumber) ? null : visa.VisaNumber.Trim();
        var period = FormatPeriod(visa);
        var text = string.Join(", ", new[] { degree, entryType, number, period }.Where(part => !string.IsNullOrWhiteSpace(part)));
        return string.IsNullOrWhiteSpace(text) ? null : text;
    }

    private static string FormatPeriod(Visa visa)
    {
        var hasStart = visa.StartDate != default;
        var hasEnd = visa.ExpirationDate is DateTime;
        if (hasStart && hasEnd)
            return $"{visa.StartDate:dd.MM.yyyy}-{visa.ExpirationDate:dd.MM.yyyy}";
        if (hasEnd)
            return $"{visa.ExpirationDate:dd.MM.yyyy}";
        if (hasStart)
            return $"{visa.StartDate:dd.MM.yyyy}";
        return null;
    }

    private static string FirstNonEmpty(params string[] values) =>
        values?.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim();
}
