using System;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services;

/// <summary>
/// Application-instance business-trip destination: AddressOfResidence-style Type +
/// Lodging/Hotel/Hospital/OtherSite (or private-house free text). Not person residence.
/// </summary>
public static class BusinessTripDestinationHelper
{
    public static string? FormatFullAddress(
        ResidenceType? type,
        Lodging? lodging,
        Hotel? hotel,
        Hospital? hospital,
        OtherSite? otherSite,
        string? privateHouseAddress,
        BusinessTripAddress? legacyCatalogAddress = null)
    {
        var fromSites = type switch
        {
            ResidenceType.Lodging => lodging?.FullAddress,
            ResidenceType.Hotel => hotel?.Name,
            ResidenceType.Hospital => hospital?.Name,
            ResidenceType.Other => otherSite?.FullAddress,
            ResidenceType.PrivateHouse => privateHouseAddress,
            _ => null,
        };

        if (!string.IsNullOrWhiteSpace(fromSites))
            return fromSites.Trim();

        if (legacyCatalogAddress != null && !string.IsNullOrWhiteSpace(legacyCatalogAddress.FullAddress))
            return legacyCatalogAddress.FullAddress.Trim();

        return null;
    }

    public static string? FormatFullAddress(ApplicationProfileInstance? instance)
    {
        if (instance == null)
            return null;

        return FormatFullAddress(
            instance.BusinessTripAddressType,
            instance.BusinessTripLodging,
            instance.BusinessTripHotel,
            instance.BusinessTripHospital,
            instance.BusinessTripOtherSite,
            instance.BusinessTripPrivateHouseAddress,
            instance.BusinessTripAddress);
    }

    public static bool IsComplete(ApplicationProfileInstance? instance)
    {
        if (instance == null)
            return false;

        return instance.BusinessTripAddressType switch
        {
            ResidenceType.Lodging => instance.BusinessTripLodging != null,
            ResidenceType.Hotel => instance.BusinessTripHotel != null,
            ResidenceType.Hospital => instance.BusinessTripHospital != null,
            ResidenceType.Other => instance.BusinessTripOtherSite != null,
            ResidenceType.PrivateHouse => !string.IsNullOrWhiteSpace(instance.BusinessTripPrivateHouseAddress),
            _ => instance.BusinessTripAddress != null
                && !string.IsNullOrWhiteSpace(instance.BusinessTripAddress.FullAddress),
        };
    }

    public static void ClearSitesExcept(ApplicationProfileInstance instance, ResidenceType? keep)
    {
        ArgumentNullException.ThrowIfNull(instance);

        if (keep != ResidenceType.Lodging)
            instance.BusinessTripLodging = null;
        if (keep != ResidenceType.Hotel)
            instance.BusinessTripHotel = null;
        if (keep != ResidenceType.Hospital)
            instance.BusinessTripHospital = null;
        if (keep != ResidenceType.Other)
            instance.BusinessTripOtherSite = null;
        if (keep != ResidenceType.PrivateHouse)
            instance.BusinessTripPrivateHouseAddress = null;
    }

    public static void ApplyDefaultsFromProfile(ApplicationProfileInstance instance, ApplicationProfile profile)
    {
        ArgumentNullException.ThrowIfNull(instance);
        ArgumentNullException.ThrowIfNull(profile);

        if (profile.DefaultBusinessTripAddressType != null)
        {
            instance.BusinessTripAddressType = profile.DefaultBusinessTripAddressType;
            ClearSitesExcept(instance, profile.DefaultBusinessTripAddressType);
            instance.BusinessTripLodging = profile.DefaultBusinessTripLodging;
            instance.BusinessTripHotel = profile.DefaultBusinessTripHotel;
            instance.BusinessTripHospital = profile.DefaultBusinessTripHospital;
            instance.BusinessTripOtherSite = profile.DefaultBusinessTripOtherSite;
            if (!string.IsNullOrWhiteSpace(profile.DefaultBusinessTripPrivateHouseAddress))
                instance.BusinessTripPrivateHouseAddress = profile.DefaultBusinessTripPrivateHouseAddress.Trim();
            instance.BusinessTripAddress = null;
            return;
        }

#pragma warning disable CS0618
        if (profile.DefaultBusinessTripAddress != null)
            instance.BusinessTripAddress = profile.DefaultBusinessTripAddress;
#pragma warning restore CS0618
    }
}