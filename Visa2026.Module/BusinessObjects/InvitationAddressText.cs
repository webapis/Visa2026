namespace Visa2026.Module.BusinessObjects;

/// <summary>
/// Letter text for <see cref="InvitationAddress"/>: first region and city, then the catalog line.
/// </summary>
public static class InvitationAddressText
{
    public static string Format(InvitationAddress? address)
    {
        if (address == null)
            return string.Empty;

        var first = AddressOfResidenceReportText.CityAndStreet(
            address.Region?.NameTm,
            address.City?.NameTm,
            null);
        var rest = address.AlternativeAddress?.AddressLine?.Trim();
        if (string.IsNullOrEmpty(first))
            return rest ?? string.Empty;
        if (string.IsNullOrEmpty(rest))
            return first;
        return first + ", " + rest;
    }
}
