using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.EFCore;
using Microsoft.EntityFrameworkCore;
using Visa2026.Module.BusinessObjects;
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
            || city.RegionName.Equals(region.RegionName, StringComparison.CurrentCultureIgnoreCase);
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

    private static IReadOnlyList<ApplicationProfileWizardLookupItem> LoadCities(IObjectSpace objectSpace)
    {
        return QueryCitiesWithRegion(objectSpace)
            .Select(item => new ApplicationProfileWizardLookupItem
            {
                Id = item.ID,
                DisplayName = FormatDisplayName(item),
                RegionId = item.Region?.ID ?? ReadRegionForeignKey(objectSpace, item),
                RegionName = item.Region?.NameTm ?? item.RegionName,
            })
            .OrderBy(item => item.DisplayName, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    private static IEnumerable<City> QueryCitiesWithRegion(IObjectSpace objectSpace)
    {
        if (objectSpace is EFCoreObjectSpace { DbContext: { } dbContext })
        {
            return dbContext.Set<City>()
                .AsNoTracking()
                .Include(city => city.Region)
                .ToList();
        }

        return objectSpace.GetObjectsQuery<City>()
            .Include(city => city.Region)
            .ToList();
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