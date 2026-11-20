using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ReportDashboard;

namespace Visa2026.Module.Services;

/// <summary>
/// Shared AddressOfResidence catalog: dedupe key, ownership clear, upsert from officer edits.
/// Case Address1/2 and People links are FKs only — no per-case copies.
/// </summary>
public static class AddressOfResidenceCatalogHelper
{
    public static string BuildDedupKey(AddressOfResidence? address)
    {
        if (address == null)
            return string.Empty;
        return BuildDedupKey(
            address.Type,
            address.Region?.NameTm ?? address.Region?.Name,
            address.City?.NameTm ?? address.City?.Name,
            EffectiveStreet(address));
    }

    public static string BuildDedupKey(
        ResidenceType? type,
        string? regionTm,
        string? cityTm,
        string? street)
    {
        var typeKey = type?.ToString() ?? string.Empty;
        var label = AddressOfResidenceReportText.CityAndStreet(regionTm, cityTm, street);
        var folded = PersonSearchTextNormalizer.Fold(label);
        if (string.IsNullOrWhiteSpace(typeKey) && string.IsNullOrWhiteSpace(folded))
            return string.Empty;
        return PersonSearchTextNormalizer.Fold(typeKey) + "|" + folded;
    }

    public static string EffectiveStreet(AddressOfResidence address)
    {
        if (address == null)
            return string.Empty;
        if (!string.IsNullOrWhiteSpace(address.FullAddress))
            return address.FullAddress.Trim();
        return address.Type switch
        {
            ResidenceType.Lodging => address.Lodging?.FullAddress?.Trim() ?? string.Empty,
            ResidenceType.Hotel => address.Hotel?.Name?.Trim() ?? string.Empty,
            ResidenceType.Hospital => address.Hospital?.Name?.Trim() ?? string.Empty,
            ResidenceType.Other => address.OtherSite?.FullAddress?.Trim() ?? string.Empty,
            _ => string.Empty,
        };
    }

    public static void ClearOwnership(AddressOfResidence address)
    {
        if (address == null)
            return;
#pragma warning disable CS0618
        address.Person = null;
        address.OwningApplicationProfileInstance = null;
        address.RosterPerson = null;
#pragma warning restore CS0618
    }

    public static AddressOfResidence? FindByDedupKey(IObjectSpace objectSpace, string dedupKey)
    {
        if (objectSpace == null || string.IsNullOrWhiteSpace(dedupKey))
            return null;

        return objectSpace.GetObjectsQuery<AddressOfResidence>()
            .AsEnumerable()
            .FirstOrDefault(row =>
                string.Equals(BuildDedupKey(row), dedupKey, StringComparison.Ordinal));
    }

    /// <summary>
    /// Apply edit onto a shared catalog row (create or reuse by dedupe key). Prefer updating
    /// <paramref name="preferId"/> when it already matches the edit key.
    /// </summary>
    public static bool TryUpsertFromEdit(
        IObjectSpace objectSpace,
        AddressOfResidenceEdit edit,
        Guid? preferId,
        out AddressOfResidence? address,
        out string? error)
    {
        address = null;
        error = null;
        if (objectSpace == null || edit == null)
        {
            error = "Address was not found.";
            return false;
        }

        if (edit.Type is not ResidenceType type)
        {
            error = "Choose an address type.";
            return false;
        }

        var region = objectSpace.GetObjectByKey<Region>(edit.RegionId ?? Guid.Empty);
        var city = objectSpace.GetObjectByKey<City>(edit.CityId ?? Guid.Empty);
        if (region == null || city == null || city.Region == null || city.Region.ID != region.ID)
        {
            error = "Choose a region and a city in that region.";
            return false;
        }

        Lodging? lodging = null;
        Hotel? hotel = null;
        Hospital? hospital = null;
        OtherSite? other = null;
        string? street = null;
        DateTime? expiration = null;

        switch (type)
        {
            case ResidenceType.Lodging:
                lodging = objectSpace.GetObjectByKey<Lodging>(edit.SiteId ?? Guid.Empty);
                if (lodging == null || lodging.City?.ID != city.ID)
                {
                    error = "Choose a lodging in this city.";
                    return false;
                }
                street = lodging.FullAddress?.Trim();
                break;
            case ResidenceType.Hotel:
                hotel = objectSpace.GetObjectByKey<Hotel>(edit.SiteId ?? Guid.Empty);
                if (hotel == null || hotel.City?.ID != city.ID)
                {
                    error = "Choose a hotel in this city.";
                    return false;
                }
                street = hotel.Name?.Trim();
                break;
            case ResidenceType.Hospital:
                hospital = objectSpace.GetObjectByKey<Hospital>(edit.SiteId ?? Guid.Empty);
                if (hospital == null || hospital.City?.ID != city.ID)
                {
                    error = "Choose a hospital in this city.";
                    return false;
                }
                street = hospital.Name?.Trim();
                break;
            case ResidenceType.Other:
                other = objectSpace.GetObjectByKey<OtherSite>(edit.SiteId ?? Guid.Empty);
                if (other == null || other.City?.ID != city.ID)
                {
                    error = "Choose an other site in this city.";
                    return false;
                }
                street = other.FullAddress?.Trim();
                break;
            case ResidenceType.PrivateHouse:
                street = edit.PrivateHouseAddress?.Trim();
                if (string.IsNullOrWhiteSpace(street))
                {
                    error = "Enter the private house address.";
                    return false;
                }
                if (edit.ExpirationDate == null)
                {
                    error = "Enter the private house expiration date.";
                    return false;
                }
                expiration = edit.ExpirationDate;
                break;
            default:
                error = "Choose an address type.";
                return false;
        }

        var key = BuildDedupKey(type, region.NameTm ?? region.Name, city.NameTm ?? city.Name, street);
        AddressOfResidence? existing = null;
        if (preferId is Guid pid && pid != Guid.Empty)
        {
            existing = objectSpace.GetObjectByKey<AddressOfResidence>(pid);
            if (existing != null && !string.Equals(BuildDedupKey(existing), key, StringComparison.Ordinal))
            {
                // Prefered row would change identity — reuse catalog match instead of mutating away.
                existing = FindByDedupKey(objectSpace, key);
            }
        }
        else
        {
            existing = FindByDedupKey(objectSpace, key);
        }

        if (existing == null)
            existing = objectSpace.CreateObject<AddressOfResidence>();

        existing.DeferPrivateHouseExpiration = false;
        existing.Region = region;
        existing.City = city;
        existing.Type = type;
        existing.Lodging = lodging;
        existing.Hotel = hotel;
        existing.Hospital = hospital;
        existing.OtherSite = other;
        if (type == ResidenceType.PrivateHouse)
        {
            existing.FullAddress = street;
            existing.ExpirationDate = expiration;
        }
        else if (!string.IsNullOrWhiteSpace(street))
        {
            existing.FullAddress = street;
        }

        ClearOwnership(existing);
        address = existing;
        return true;
    }

    public static AddressOfResidence UpsertFromStay(
        IObjectSpace objectSpace,
        InvitationAddress stay)
    {
        ArgumentNullException.ThrowIfNull(objectSpace);
        ArgumentNullException.ThrowIfNull(stay);

        var edit = new AddressOfResidenceEdit
        {
            Type = stay.Type,
            RegionId = stay.Region?.ID,
            CityId = stay.City?.ID,
            SiteId = stay.Type switch
            {
                ResidenceType.Lodging => stay.Lodging?.ID,
                ResidenceType.Hotel => stay.Hotel?.ID,
                ResidenceType.Hospital => stay.Hospital?.ID,
                ResidenceType.Other => stay.OtherSite?.ID,
                _ => null,
            },
            PrivateHouseAddress = stay.PrivateHouseAddress,
            ExpirationDate = DateTime.Today.AddYears(1),
        };

        if (!TryUpsertFromEdit(objectSpace, edit, preferId: null, out var address, out _)
            || address == null)
        {
            // Fallback create for incomplete stay (caller already gated IsComplete).
            address = objectSpace.CreateObject<AddressOfResidence>();
            address.Region = stay.Region;
            address.City = stay.City;
            address.Type = stay.Type;
            address.Lodging = stay.Lodging;
            address.Hotel = stay.Hotel;
            address.Hospital = stay.Hospital;
            address.OtherSite = stay.OtherSite;
            if (stay.Type == ResidenceType.PrivateHouse)
            {
                address.FullAddress = stay.PrivateHouseAddress?.Trim();
                address.DeferPrivateHouseExpiration = true;
            }
            ClearOwnership(address);
        }

        return address;
    }
}