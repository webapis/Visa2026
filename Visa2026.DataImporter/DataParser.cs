using System;
using System.Globalization;

namespace Visa2026.DataImporter;

public static class DataParser
{
    /// <summary>
    /// Converts strings like "1", "true", "yes" to boolean true.
    /// Everything else (including "0", "false", "no", or empty) returns false.
    /// </summary>
    public static bool IsTextTrue(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return false;
        
        return raw != "0" &&
               !raw.Equals("false", StringComparison.OrdinalIgnoreCase) &&
               !raw.Equals("no", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Attempts to parse a string into its most likely type: DateTime > Int > Decimal > Bool > String.
    /// </summary>
    public static object ParseScalar(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return raw;

        // 1. Try ISO and Roundtrip Date formats
        if (DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTime dt)) 
            return DateTime.SpecifyKind(dt, DateTimeKind.Utc);
        
        // 2. Try common European/CIS formats (DD.MM.YYYY)
        string[] extraFormats = { "dd.MM.yyyy", "dd.MM.yyyy HH:mm:ss", "d.M.yyyy", "d.M.yyyy H:mm:ss" };
        if (DateTime.TryParseExact(raw, extraFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out dt))
            return DateTime.SpecifyKind(dt, DateTimeKind.Utc);

        // 3. Try Numeric types
        if (int.TryParse(raw, out int i)) return i;
        if (decimal.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal d)) return d;

        // 4. Try explicit Boolean strings
        if (raw.Equals("true", StringComparison.OrdinalIgnoreCase) || raw.Equals("yes", StringComparison.OrdinalIgnoreCase)) return true;
        if (raw.Equals("false", StringComparison.OrdinalIgnoreCase) || raw.Equals("no", StringComparison.OrdinalIgnoreCase)) return false;

        // 5. Fallback to raw string
        return raw;
    }
}