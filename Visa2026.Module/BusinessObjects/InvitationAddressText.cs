namespace Visa2026.Module.BusinessObjects;

/// <summary>
/// Letter text for <see cref="InvitationAddress"/>: region, city, and the chosen place, then other places.
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
            MainPlace(address));
        var rest = address.AlternativeAddress?.AddressLine?.Trim();
        if (string.IsNullOrEmpty(first))
            return rest ?? string.Empty;
        if (string.IsNullOrEmpty(rest))
            return first;
        return first + ", " + rest;
    }

    /// <summary>
    /// Application-form field 35: the chosen place, then other invitation places.
    /// Region and city are separate dropdowns.
    /// </summary>
    public static string StayAddress(InvitationAddress? address)
    {
        if (address == null)
            return string.Empty;

        var place = MainPlace(address);
        var rest = address.AlternativeAddress?.AddressLine?.Trim();
        if (string.IsNullOrEmpty(place))
            return rest ?? string.Empty;
        if (string.IsNullOrEmpty(rest))
            return place;
        return place + ", " + rest;
    }

    public static string MainPlace(InvitationAddress address) =>
        address.Type switch
        {
            ResidenceType.Lodging => address.Lodging?.FullAddress?.Trim() ?? string.Empty,
            ResidenceType.Hotel => address.Hotel?.Name?.Trim() ?? string.Empty,
            ResidenceType.Hospital => address.Hospital?.Name?.Trim() ?? string.Empty,
            ResidenceType.Other => address.OtherSite?.FullAddress?.Trim() ?? string.Empty,
            ResidenceType.PrivateHouse => address.PrivateHouseAddress?.Trim() ?? string.Empty,
            _ => string.Empty,
        };
}
