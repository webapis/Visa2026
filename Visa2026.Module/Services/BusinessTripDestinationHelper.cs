using System;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services;

/// <summary>
/// Application-instance business-trip destination: AddressOfResidence-style Type +
/// Lodging/Hotel/Hospital/OtherSite (or private-house free text). Not person residence.
/// </summary>
public static class BusinessTripDestinationHelper
{
    /// <summary>
    /// Profile-default / site-only path (no case ToRegion/ToCity). Prefer
    /// <see cref="FormatFullAddress(ApplicationProfileInstance)"/> for merge BTAD.
    /// </summary>
    public static string? FormatFullAddress(
        ResidenceType? type,
        Lodging? lodging,
        Hotel? hotel,
        Hospital? hospital,
        OtherSite? otherSite,
        string? privateHouseAddress,
        BusinessTripAddress? legacyCatalogAddress = null)
    {
        // Same CityAndStreet join as ADRS, using the site's own Region/City when present.
        var fromSites = type switch
        {
            ResidenceType.Lodging => AddressOfResidenceReportText.CityAndStreet(
                lodging?.City?.Region?.NameTm ?? lodging?.City?.RegionName,
                lodging?.City?.NameTm,
                lodging?.FullAddress),
            ResidenceType.Hotel => AddressOfResidenceReportText.CityAndStreet(
                null,
                null,
                hotel?.Name),
            ResidenceType.Hospital => AddressOfResidenceReportText.CityAndStreet(
                null,
                null,
                hospital?.Name),
            ResidenceType.Other => AddressOfResidenceReportText.CityAndStreet(
                otherSite?.City?.Region?.NameTm ?? otherSite?.City?.RegionName,
                otherSite?.City?.NameTm,
                otherSite?.FullAddress),
            ResidenceType.PrivateHouse => AddressOfResidenceReportText.CityAndStreet(
                null,
                null,
                privateHouseAddress),
            _ => null,
        };

        if (!string.IsNullOrWhiteSpace(fromSites))
            return fromSites.Trim();

        if (legacyCatalogAddress != null && !string.IsNullOrWhiteSpace(legacyCatalogAddress.FullAddress))
            return legacyCatalogAddress.FullAddress.Trim();

        return null;
    }

    /// <summary>
    /// BTAD — same pattern as ADRS (<see cref="AddressOfResidenceReportText.CityAndStreet"/>):
    /// Region = <see cref="ApplicationProfileInstance.ToRegion"/>,
    /// City = <see cref="ApplicationProfileInstance.ToCity"/>,
    /// street = lodging FullAddress / hotel name / hospital name / other site / private-house text.
    /// </summary>
    public static string? FormatFullAddress(ApplicationProfileInstance? instance)
    {
        if (instance == null)
            return null;

        var text = AddressOfResidenceReportText.CityAndStreet(
            ResolveDestinationRegionNameTm(instance),
            ResolveDestinationCityNameTm(instance),
            ResolveDestinationStreet(instance));

        return string.IsNullOrWhiteSpace(text) ? null : text;
    }

    /// <summary>Street / site line only — parallel to <see cref="AddressOfResidence.FullAddress"/>.</summary>
    private static string? ResolveDestinationStreet(ApplicationProfileInstance instance)
    {
        var site = instance.BusinessTripAddressType switch
        {
            ResidenceType.Lodging => instance.BusinessTripLodging?.FullAddress,
            ResidenceType.Hotel => instance.BusinessTripHotel?.Name,
            ResidenceType.Hospital => instance.BusinessTripHospital?.Name,
            ResidenceType.Other => instance.BusinessTripOtherSite?.FullAddress,
            ResidenceType.PrivateHouse => instance.BusinessTripPrivateHouseAddress,
            _ => null,
        };

        if (!string.IsNullOrWhiteSpace(site))
            return site.Trim();

#pragma warning disable CS0618
        if (instance.BusinessTripAddress != null
            && !string.IsNullOrWhiteSpace(instance.BusinessTripAddress.FullAddress))
            return instance.BusinessTripAddress.FullAddress.Trim();
#pragma warning restore CS0618

        return null;
    }

    private static string? ResolveDestinationRegionNameTm(ApplicationProfileInstance instance)
    {
#pragma warning disable CS0618
        if (!string.IsNullOrWhiteSpace(instance.ToRegion?.NameTm))
            return instance.ToRegion.NameTm;
        if (!string.IsNullOrWhiteSpace(instance.Region?.NameTm))
            return instance.Region.NameTm;
        if (!string.IsNullOrWhiteSpace(instance.ToCity?.Region?.NameTm))
            return instance.ToCity.Region.NameTm;
        if (!string.IsNullOrWhiteSpace(instance.ToCity?.RegionName))
            return instance.ToCity.RegionName;
#pragma warning restore CS0618

        // Fall back like ADRS reading Region from the linked address row.
        return instance.BusinessTripAddressType switch
        {
            ResidenceType.Lodging => instance.BusinessTripLodging?.City?.Region?.NameTm
                ?? instance.BusinessTripLodging?.City?.RegionName,
            ResidenceType.Other => instance.BusinessTripOtherSite?.City?.Region?.NameTm
                ?? instance.BusinessTripOtherSite?.City?.RegionName,
            _ => null,
        };
    }

    private static string? ResolveDestinationCityNameTm(ApplicationProfileInstance instance)
    {
#pragma warning disable CS0618
        if (!string.IsNullOrWhiteSpace(instance.ToCity?.NameTm))
            return instance.ToCity.NameTm;
        if (!string.IsNullOrWhiteSpace(instance.City?.NameTm))
            return instance.City.NameTm;
#pragma warning restore CS0618

        return instance.BusinessTripAddressType switch
        {
            ResidenceType.Lodging => instance.BusinessTripLodging?.City?.NameTm,
            ResidenceType.Other => instance.BusinessTripOtherSite?.City?.NameTm,
            _ => null,
        };
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