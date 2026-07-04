using DevExpress.ExpressApp;
using Bo = Visa2026.Module.BusinessObjects;
using ModuleResidenceType = Visa2026.Module.BusinessObjects.ResidenceType;

namespace Visa2026.DataImporter.Legacy.Visa2014;

internal static class Visa2014AddressOfResidenceImportApplier
{
    internal static Dictionary<string, object?>? BuildODataPayload(
        Dictionary<string, object?> row,
        Visa2014ODataLookupResolver resolver,
        Guid personId)
    {
        if (!TryResolveLookupIds(row, resolver, out var residenceType, out var regionId, out var cityId, out var typeFields))
            return null;

        var payload = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["Person"] = new { ID = personId },
            ["Type"] = residenceType.ToString(),
            ["Region"] = new { ID = regionId!.Value },
            ["City"] = new { ID = cityId!.Value },
        };

        switch (residenceType)
        {
            case ModuleResidenceType.PrivateHouse:
                payload["FullAddress"] = typeFields.FullAddress;
                if (typeFields.ExpirationDate.HasValue)
                    payload["ExpirationDate"] = DateTime.SpecifyKind(typeFields.ExpirationDate.Value, DateTimeKind.Utc);
                break;
            case ModuleResidenceType.Lodging:
                payload["Lodging"] = new { ID = typeFields.SiteLookupId!.Value };
                break;
            case ModuleResidenceType.Hotel:
                payload["Hotel"] = new { ID = typeFields.SiteLookupId!.Value };
                break;
            case ModuleResidenceType.Hospital:
                payload["Hospital"] = new { ID = typeFields.SiteLookupId!.Value };
                break;
            case ModuleResidenceType.Other:
                payload["OtherSite"] = new { ID = typeFields.SiteLookupId!.Value };
                break;
            default:
                return null;
        }

        return payload;
    }

    internal static bool TryCreateOnObjectSpace(
        IObjectSpace objectSpace,
        Bo.Person person,
        Dictionary<string, object?> row,
        Visa2014ODataLookupResolver resolver,
        out Bo.AddressOfResidence? created)
    {
        created = null;
        if (!TryResolveLookupIds(row, resolver, out var residenceType, out var regionId, out var cityId, out var typeFields))
            return false;

        var region = objectSpace.GetObjectByKey<Bo.Region>(regionId!.Value);
        var city = objectSpace.GetObjectByKey<Bo.City>(cityId!.Value);
        if (region == null || city == null)
            return false;

        var address = objectSpace.CreateObject<Bo.AddressOfResidence>();
        address.Person = person;
        address.Type = residenceType;
        address.Region = region;
        address.City = city;

        switch (residenceType)
        {
            case ModuleResidenceType.PrivateHouse:
                address.FullAddress = typeFields.FullAddress;
                address.ExpirationDate = typeFields.ExpirationDate;
                break;
            case ModuleResidenceType.Lodging:
                address.Lodging = objectSpace.GetObjectByKey<Bo.Lodging>(typeFields.SiteLookupId!.Value);
                break;
            case ModuleResidenceType.Hotel:
                address.Hotel = objectSpace.GetObjectByKey<Bo.Hotel>(typeFields.SiteLookupId!.Value);
                break;
            case ModuleResidenceType.Hospital:
                address.Hospital = objectSpace.GetObjectByKey<Bo.Hospital>(typeFields.SiteLookupId!.Value);
                break;
            case ModuleResidenceType.Other:
                address.OtherSite = objectSpace.GetObjectByKey<Bo.OtherSite>(typeFields.SiteLookupId!.Value);
                break;
            default:
                return false;
        }

        created = address;
        return true;
    }

    private static bool TryResolveLookupIds(
        Dictionary<string, object?> row,
        Visa2014ODataLookupResolver resolver,
        out ModuleResidenceType residenceType,
        out Guid? regionId,
        out Guid? cityId,
        out TypeFieldValues typeFields)
    {
        residenceType = default;
        regionId = null;
        cityId = null;
        typeFields = default;

        var typeText = row.GetValueOrDefault("Type") as string;
        if (!Enum.TryParse<ModuleResidenceType>(typeText, ignoreCase: true, out residenceType))
            return false;

        var regionName = row.GetValueOrDefault("Region") as string;
        var cityName = row.GetValueOrDefault("City") as string;
        regionId = resolver.ResolveRegion(regionName);
        cityId = resolver.ResolveCity(cityName, regionName);
        if (!regionId.HasValue || !cityId.HasValue)
            return false;

        switch (residenceType)
        {
            case ModuleResidenceType.PrivateHouse:
                var fullAddress = row.GetValueOrDefault("FullAddress") as string;
                if (string.IsNullOrWhiteSpace(fullAddress))
                    return false;
                DateTime? expiration = null;
                if (DateTime.TryParse(row.GetValueOrDefault("ExpirationDate") as string, out var exp))
                    expiration = exp.Date;
                typeFields = new TypeFieldValues(fullAddress.Trim(), expiration, null);
                return true;

            case ModuleResidenceType.Lodging:
                var lodgingId = resolver.ResolveLodging(cityName, regionName, row.GetValueOrDefault("Lodging") as string);
                if (!lodgingId.HasValue) return false;
                typeFields = new TypeFieldValues(null, null, lodgingId);
                return true;

            case ModuleResidenceType.Hotel:
                var hotelId = resolver.ResolveHotel(cityName, regionName, row.GetValueOrDefault("Hotel") as string);
                if (!hotelId.HasValue) return false;
                typeFields = new TypeFieldValues(null, null, hotelId);
                return true;

            case ModuleResidenceType.Hospital:
                var hospitalId = resolver.ResolveHospital(cityName, regionName, row.GetValueOrDefault("Hospital") as string);
                if (!hospitalId.HasValue) return false;
                typeFields = new TypeFieldValues(null, null, hospitalId);
                return true;

            case ModuleResidenceType.Other:
                var otherSiteId = resolver.ResolveOtherSite(cityName, regionName, row.GetValueOrDefault("OtherSite") as string);
                if (!otherSiteId.HasValue) return false;
                typeFields = new TypeFieldValues(null, null, otherSiteId);
                return true;

            default:
                return false;
        }
    }

    private readonly record struct TypeFieldValues(string? FullAddress, DateTime? ExpirationDate, Guid? SiteLookupId);
}