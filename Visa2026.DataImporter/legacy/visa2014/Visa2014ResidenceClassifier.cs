namespace Visa2026.DataImporter.Legacy.Visa2014;

using System.Text.RegularExpressions;

/// <summary>
/// Splits legacy <c>myhmanhana</c> address lines into Hotel vs Hospital catalog types.
/// </summary>
internal static class Visa2014ResidenceClassifier
{
    public static bool IsHotelAddressLine(string? addressLine)
    {
        if (string.IsNullOrWhiteSpace(addressLine))
            return false;

        if (IsHospitalAddressLine(addressLine))
            return false;

        var folded = Visa2014CatalogMatchHelper.NormalizeKey(addressLine);
        if (folded.Contains("myhmanhan", StringComparison.Ordinal))
            return true;

        // Turkmen/Russian hotel: otel, oteli, otely — not the "otel" inside English "hotel".
        if (folded.Contains("oteli", StringComparison.Ordinal)
            || folded.Contains("otely", StringComparison.Ordinal))
            return true;

        return Regex.IsMatch(
            folded,
            @"(^|[^a-z])otel($|[^a-z])",
            RegexOptions.CultureInvariant);
    }

    public static bool IsHospitalAddressLine(string? addressLine)
    {
        if (string.IsNullOrWhiteSpace(addressLine))
            return false;

        var folded = Visa2014CatalogMatchHelper.NormalizeKey(addressLine);

        if (folded.Contains("myhmanhan", StringComparison.Ordinal))
            return false;

        if (folded.Contains("hassahan", StringComparison.Ordinal))
            return true;

        if (folded.Contains("yokanc keseller", StringComparison.Ordinal))
            return true;

        if (folded.Contains("içki kesel", StringComparison.Ordinal)
            || folded.Contains("icki kesel", StringComparison.Ordinal))
            return true;

        return false;
    }

    public static bool IsLodgingSiteLine(string? addressLine)
    {
        if (string.IsNullOrWhiteSpace(addressLine))
            return false;

        if (IsHotelAddressLine(addressLine) || IsHospitalAddressLine(addressLine))
            return false;

        var folded = Visa2014CatalogMatchHelper.NormalizeKey(addressLine);
        if (folded.Contains("uyj", StringComparison.Ordinal))
            return true;
        if (folded.Contains("iscilersaherce", StringComparison.Ordinal))
            return true;
        if (folded.Contains("lojman", StringComparison.Ordinal))
            return true;
        if (folded.Contains("yasayys", StringComparison.Ordinal))
            return true;

        return false;
    }

    public static string MapLojmanResidenceType(string? addressLine)
    {
        if (IsHotelAddressLine(addressLine))
            return "Hotel";
        if (IsHospitalAddressLine(addressLine))
            return "Hospital";
        if (IsLodgingSiteLine(addressLine))
            return "Lodging";
        return "Other";
    }

    /// <summary>
    /// Legacy Patent document type is usually PrivateHouse, but officers sometimes filed hotel
    /// stays (myhmanhanasy / otel) under Patent — reclassify by address line like Lojman.
    /// </summary>
    public static string MapPatentResidenceType(string? addressLine)
    {
        if (IsHotelAddressLine(addressLine))
            return "Hotel";
        if (IsHospitalAddressLine(addressLine))
            return "Hospital";
        if (IsLodgingSiteLine(addressLine))
            return "Lodging";
        return "PrivateHouse";
    }
}
