using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.EFCore;
using Microsoft.EntityFrameworkCore;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.DatabaseUpdate.LookupCatalogs;
using Visa2026.Module.Localization;

namespace Visa2026.Module.Services.ApplicationProfileWizard;

/// <summary>One catalog row for wizard default-value dropdowns (not a live ObjectSpace entity).</summary>
public sealed class ApplicationProfileWizardLookupItem
{
    public Guid Id { get; init; }
    public string DisplayName { get; init; } = string.Empty;
    public Guid? RegionId { get; init; }
    public string? RegionName { get; init; }
}

/// <summary>Lookup catalogs for wizard Results and fields default-value editors.</summary>
public sealed class ApplicationProfileWizardLookupData
{
    public static ApplicationProfileWizardLookupData Empty { get; } = new();

    public IReadOnlyList<ApplicationProfileWizardLookupItem> VisaTypes { get; init; } = [];
    public IReadOnlyList<ApplicationProfileWizardLookupItem> VisaCategories { get; init; } = [];
    public IReadOnlyList<ApplicationProfileWizardLookupItem> VisaPeriods { get; init; } = [];
    public IReadOnlyList<ApplicationProfileWizardLookupItem> MigrationServices { get; init; } = [];
    public IReadOnlyList<ApplicationProfileWizardLookupItem> ProjectContracts { get; init; } = [];
    public IReadOnlyList<ApplicationProfileWizardLookupItem> Urgencies { get; init; } = [];
    public IReadOnlyList<ApplicationProfileWizardLookupItem> CheckPoints { get; init; } = [];
    public IReadOnlyList<ApplicationProfileWizardLookupItem> Regions { get; init; } = [];
    public IReadOnlyList<ApplicationProfileWizardLookupItem> Cities { get; init; } = [];
    public IReadOnlyList<ApplicationProfileWizardLookupItem> Lodgings { get; init; } = [];
    public IReadOnlyList<ApplicationProfileWizardLookupItem> Hotels { get; init; } = [];
    public IReadOnlyList<ApplicationProfileWizardLookupItem> Hospitals { get; init; } = [];
    public IReadOnlyList<ApplicationProfileWizardLookupItem> OtherSites { get; init; } = [];
#pragma warning disable CS0618
    public IReadOnlyList<ApplicationProfileWizardLookupItem> BusinessTripAddresses { get; init; } = [];
#pragma warning restore CS0618

    /// <summary>
    /// Project contracts (via ministry) or migration services (direct) for nested-template visibility.
    /// Same catalogs as the Application Profile Templates wizard applicability dropdown.
    /// </summary>
    public static IReadOnlyList<ApplicationProfileWizardLookupItem> LoadApplicabilityItems(
        IObjectSpace objectSpace,
        bool viaMinistry)
    {
        if (objectSpace == null)
            return Array.Empty<ApplicationProfileWizardLookupItem>();

        return viaMinistry
            ? LoadItems<ProjectContract>(objectSpace)
            : LoadItems<MigrationService>(objectSpace);
    }

    /// <summary>
    /// Case Resminamalar: This-profile templates may bind to every instance, or only the
    /// Project contract / Migration service already set on this case — not the full catalog.
    /// </summary>
    public static IReadOnlyList<ApplicationProfileWizardLookupItem> LoadApplicabilityItemsForInstance(
        IObjectSpace? objectSpace,
        ApplicationProfileInstance? instance,
        bool viaMinistry)
    {
        if (instance == null)
            return Array.Empty<ApplicationProfileWizardLookupItem>();

        LookupBase? item = viaMinistry ? instance.ProjectContract : instance.MigrationService;
        if (item == null && objectSpace != null && instance.ID != Guid.Empty)
        {
            var loaded = objectSpace.GetObjectsQuery<ApplicationProfileInstance>()
                .Include(i => i.ProjectContract)
                .Include(i => i.MigrationService)
                .FirstOrDefault(i => i.ID == instance.ID);
            item = viaMinistry ? loaded?.ProjectContract : loaded?.MigrationService;
        }

        if (item == null || item.ID == Guid.Empty)
            return Array.Empty<ApplicationProfileWizardLookupItem>();

        return
        [
            new ApplicationProfileWizardLookupItem
            {
                Id = item.ID,
                DisplayName = FormatDisplayName(item),
            }
        ];
    }

    public static ApplicationProfileWizardLookupData Load(IObjectSpace objectSpace)
    {
        if (objectSpace == null)
            return Empty;

        return new ApplicationProfileWizardLookupData
        {
            VisaTypes = LoadItems<VisaType>(objectSpace),
            VisaCategories = LoadItems<VisaCategory>(objectSpace),
            VisaPeriods = LoadItems<VisaPeriod>(objectSpace),
            MigrationServices = LoadItems<MigrationService>(objectSpace),
            ProjectContracts = LoadItems<ProjectContract>(objectSpace),
            Urgencies = LoadItems<Urgency>(objectSpace),
            CheckPoints = LoadItems<CheckPoint>(objectSpace),
            Regions = LoadItems<Region>(objectSpace),
            Cities = LoadCities(objectSpace),
            Lodgings = LoadLodgings(objectSpace),
            Hotels = LoadHotels(objectSpace),
            Hospitals = LoadHospitals(objectSpace),
            OtherSites = LoadOtherSites(objectSpace),
#pragma warning disable CS0618
            BusinessTripAddresses = LoadBusinessTripAddresses(objectSpace),
#pragma warning restore CS0618
        };
    }

    public static IReadOnlyList<ApplicationProfileWizardLookupItem> CitiesForRegion(
        IReadOnlyList<ApplicationProfileWizardLookupItem> cities,
        IReadOnlyList<ApplicationProfileWizardLookupItem> regions,
        Guid? regionId)
    {
        if (cities == null || cities.Count == 0)
            return Array.Empty<ApplicationProfileWizardLookupItem>();

        if (regionId is not Guid id || id == Guid.Empty)
            return cities;

        var region = regions?.FirstOrDefault(r => r.Id == id);
        return cities.Where(city => CityBelongsToRegion(city, id, region)).ToList();
    }

    private static bool CityBelongsToRegion(
        ApplicationProfileWizardLookupItem city,
        Guid regionId,
        ApplicationProfileWizardLookupItem? region)
    {
        if (city.RegionId == regionId)
            return true;

        if (region == null || string.IsNullOrWhiteSpace(city.RegionName))
            return false;

        return city.RegionName.Equals(region.DisplayName, StringComparison.CurrentCultureIgnoreCase)
            || city.RegionName.Equals(region.RegionName, StringComparison.CurrentCultureIgnoreCase)
            || LookupCatalogMatchHelper.KeysEqual(city.RegionName, region.DisplayName)
            || LookupCatalogMatchHelper.KeysEqual(city.RegionName, region.RegionName);
    }

    private static IReadOnlyList<ApplicationProfileWizardLookupItem> LoadItems<T>(IObjectSpace objectSpace)
        where T : LookupBase
    {
        return objectSpace.GetObjects(typeof(T))
            .Cast<T>()
            .Select(item => new ApplicationProfileWizardLookupItem
            {
                Id = item.ID,
                DisplayName = FormatDisplayName(item),
                RegionName = item is Region ? item.NameTm : null,
            })
            .OrderBy(item => item.DisplayName, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    public static IReadOnlyList<ApplicationProfileWizardLookupItem> LoadRegions(IObjectSpace objectSpace)
    {
        if (objectSpace == null)
            return Array.Empty<ApplicationProfileWizardLookupItem>();

        return LoadItems<Region>(objectSpace);
    }

    public static IReadOnlyList<ApplicationProfileWizardLookupItem> LoadCities(IObjectSpace objectSpace)
    {
        if (objectSpace == null)
            return Array.Empty<ApplicationProfileWizardLookupItem>();

        var regions = LoadRegions(objectSpace);
        return QueryCitiesWithRegion(objectSpace)
            .Select(item => ToCityLookupItem(objectSpace, item, regions))
            .OrderBy(item => item.DisplayName, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    internal static string? CatalogRegionNameForCity(params string?[] cityNames)
    {
        var map = CityNameToRegionName.Value;
        if (map.Count == 0 || cityNames == null)
            return null;

        foreach (var name in cityNames)
        {
            var key = LookupCatalogMatchHelper.NormalizeKey(name);
            if (key.Length > 0 && map.TryGetValue(key, out var regionName))
                return regionName;
        }

        return null;
    }

    private static IReadOnlyList<ApplicationProfileWizardLookupItem> LoadLodgings(IObjectSpace objectSpace) =>
        objectSpace.GetObjects(typeof(Lodging))
            .Cast<Lodging>()
            .Select(item => new ApplicationProfileWizardLookupItem
            {
                Id = item.ID,
                DisplayName = item.FullAddress?.Trim() ?? string.Empty,
                RegionId = item.City?.ID,
            })
            .Where(item => !string.IsNullOrWhiteSpace(item.DisplayName))
            .OrderBy(item => item.DisplayName, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

    private static IReadOnlyList<ApplicationProfileWizardLookupItem> LoadHotels(IObjectSpace objectSpace) =>
        objectSpace.GetObjects(typeof(Hotel))
            .Cast<Hotel>()
            .Select(item => new ApplicationProfileWizardLookupItem
            {
                Id = item.ID,
                DisplayName = item.Name?.Trim() ?? string.Empty,
                RegionId = item.City?.ID,
            })
            .Where(item => !string.IsNullOrWhiteSpace(item.DisplayName))
            .OrderBy(item => item.DisplayName, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

    private static IReadOnlyList<ApplicationProfileWizardLookupItem> LoadHospitals(IObjectSpace objectSpace) =>
        objectSpace.GetObjects(typeof(Hospital))
            .Cast<Hospital>()
            .Select(item => new ApplicationProfileWizardLookupItem
            {
                Id = item.ID,
                DisplayName = item.Name?.Trim() ?? string.Empty,
                RegionId = item.City?.ID,
            })
            .Where(item => !string.IsNullOrWhiteSpace(item.DisplayName))
            .OrderBy(item => item.DisplayName, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

    private static IReadOnlyList<ApplicationProfileWizardLookupItem> LoadOtherSites(IObjectSpace objectSpace) =>
        objectSpace.GetObjects(typeof(OtherSite))
            .Cast<OtherSite>()
            .Select(item => new ApplicationProfileWizardLookupItem
            {
                Id = item.ID,
                DisplayName = item.FullAddress?.Trim() ?? string.Empty,
                RegionId = item.City?.ID,
            })
            .Where(item => !string.IsNullOrWhiteSpace(item.DisplayName))
            .OrderBy(item => item.DisplayName, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

#pragma warning disable CS0618
    private static IReadOnlyList<ApplicationProfileWizardLookupItem> LoadBusinessTripAddresses(IObjectSpace objectSpace)
    {
        return objectSpace.GetObjects(typeof(BusinessTripAddress))
            .Cast<BusinessTripAddress>()
            .Select(item => new ApplicationProfileWizardLookupItem
            {
                Id = item.ID,
                DisplayName = string.IsNullOrWhiteSpace(item.FullAddress)
                    ? (item.City?.NameTm ?? item.ID.ToString("D"))
                    : item.FullAddress.Trim(),
            })
            .OrderBy(item => item.DisplayName, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }
#pragma warning restore CS0618

    private static readonly Lazy<IReadOnlyDictionary<string, string>> CityNameToRegionName = new(LoadCityNameToRegionName);

    private static ApplicationProfileWizardLookupItem ToCityLookupItem(
        IObjectSpace objectSpace,
        City item,
        IReadOnlyList<ApplicationProfileWizardLookupItem> regions)
    {
        var displayName = FormatDisplayName(item);
#pragma warning disable CS0618
        var regionName = FirstNonEmpty(
            item.Region?.NameTm,
            item.RegionName,
            CatalogRegionNameForCity(item.NameTm, item.Name, displayName));
#pragma warning restore CS0618
        var regionId = item.Region?.ID
            ?? ReadRegionForeignKey(objectSpace, item)
            ?? MatchRegionId(regions, regionName);

        return new ApplicationProfileWizardLookupItem
        {
            Id = item.ID,
            DisplayName = displayName,
            RegionId = regionId,
            RegionName = regionName,
        };
    }

    private static Guid? MatchRegionId(
        IReadOnlyList<ApplicationProfileWizardLookupItem> regions,
        string? regionName)
    {
        if (regions == null || regions.Count == 0 || string.IsNullOrWhiteSpace(regionName))
            return null;

        var match = regions.FirstOrDefault(region =>
            LookupCatalogMatchHelper.KeysEqual(regionName, region.RegionName)
            || LookupCatalogMatchHelper.KeysEqual(regionName, region.DisplayName)
            || regionName.Equals(region.RegionName, StringComparison.CurrentCultureIgnoreCase)
            || regionName.Equals(region.DisplayName, StringComparison.CurrentCultureIgnoreCase));
        return match?.Id is Guid id && id != Guid.Empty ? id : null;
    }

    private static string? FirstNonEmpty(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
                return value.Trim();
        }

        return null;
    }

    private static IReadOnlyDictionary<string, string> LoadCityNameToRegionName()
    {
        var map = new Dictionary<string, string>(StringComparer.Ordinal);
        var file = LookupCatalogResourceLoader.LoadCatalogFile("city.json");
        if (file?.Rows == null)
            return map;

        foreach (var row in file.Rows)
        {
            if (!TryReadCatalogString(row, "NameTm", out var cityName)
                && !TryReadCatalogString(row, "Name", out cityName))
                continue;
            if (!TryReadCatalogString(row, "Region", out var regionName))
                continue;

            var key = LookupCatalogMatchHelper.NormalizeKey(cityName);
            if (key.Length == 0)
                continue;

            map[key] = regionName;
        }

        return map;
    }

    private static bool TryReadCatalogString(
        Dictionary<string, System.Text.Json.JsonElement> row,
        string key,
        out string value)
    {
        value = string.Empty;
        if (!row.TryGetValue(key, out var element))
            return false;

        var text = element.ValueKind == System.Text.Json.JsonValueKind.String
            ? element.GetString()
            : element.ToString();
        if (string.IsNullOrWhiteSpace(text))
            return false;

        value = text.Trim();
        return true;
    }

    private static IEnumerable<City> QueryCitiesWithRegion(IObjectSpace objectSpace)
    {
        // Tracked entities (not AsNoTracking) so Region lazy-loads when Include is skipped.
        // Demo/prod often leave City.RegionName null even when RegionID is set.
        if (objectSpace is EFCoreObjectSpace { DbContext: { } dbContext })
        {
            return dbContext.Set<City>()
                .Include(city => city.Region)
                .ToList();
        }

        return objectSpace.GetObjects(typeof(City)).Cast<City>();
    }

    private static Guid? ReadRegionForeignKey(IObjectSpace objectSpace, City city)
    {
        if (objectSpace is not EFCoreObjectSpace { DbContext: { } dbContext })
            return null;

        var entry = dbContext.Entry(city);
        foreach (var name in new[] { "RegionID", "RegionId" })
        {
            if (entry.Metadata.FindProperty(name) == null)
                continue;

            var value = entry.Property(name).CurrentValue;
            if (value is Guid guid && guid != Guid.Empty)
                return guid;
        }

        return null;
    }

    private static string FormatDisplayName(LookupBase item)
    {
        var localized = LookupLocalization.GetDisplayName(item);
        if (!string.IsNullOrWhiteSpace(localized))
            return localized;
        if (!string.IsNullOrWhiteSpace(item.NameTm))
            return item.NameTm;
#pragma warning disable CS0618
        if (!string.IsNullOrWhiteSpace(item.Name))
            return item.Name;
#pragma warning restore CS0618
        return string.IsNullOrWhiteSpace(item.Code) ? item.ID.ToString("D") : item.Code;
    }
}