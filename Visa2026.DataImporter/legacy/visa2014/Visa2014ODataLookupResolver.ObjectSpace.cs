using DevExpress.ExpressApp;
using DevExpress.ExpressApp.EFCore;
using Bo = Visa2026.Module.BusinessObjects;
using Dto = Visa2026.DataImporter;

namespace Visa2026.DataImporter.Legacy.Visa2014;

internal sealed partial class Visa2014ODataLookupResolver
{
    /// <summary>
    /// Load all catalogs needed for the full legacy import chain (person + application domain)
    /// from the in-process ObjectSpace. Mirrors <see cref="LoadAsync"/> so headless imports resolve
    /// the same lookups the OData path does.
    /// </summary>
    public void LoadFromObjectSpace(IObjectSpace objectSpace, string? tenantCatalogDirectory = null)
    {
        ArgumentNullException.ThrowIfNull(objectSpace);

        // Person / passport / visa / education / employment domain lookups.
        _genders = MapLookupDto<Bo.Gender, Dto.Gender>(objectSpace);
        _countries = MapLookupDto<Bo.Country, Dto.Country>(objectSpace);
        _maritalStatuses = MapLookupDto<Bo.MaritalStatus, Dto.MaritalStatus>(objectSpace);
        _relationships = MapLookupDto<Bo.Relationship, Dto.Relationship>(objectSpace);
        _passportTypes = MapLookupDto<Bo.PassportType, Dto.PassportType>(objectSpace);
        _visaTypes = MapLookupDto<Bo.VisaType, Dto.VisaType>(objectSpace);
        _visaIssuedPlaces = MapLookupDto<Bo.VisaIssuedPlace, Dto.VisaIssuedPlace>(objectSpace);
        _subcontractors = MapLookupDto<Bo.Subcontractor, Dto.Subcontractor>(objectSpace);
        _educationLevels = MapLookupDto<Bo.EducationLevel, Dto.EducationLevel>(objectSpace);
        _educationInstitutions = MapLookupDto<Bo.EducationInstitution, Dto.EducationInstitution>(objectSpace);
        _specialties = MapLookupDto<Bo.Specialty, Dto.Specialty>(objectSpace);
        _positions = MapLookupDto<Bo.Position, Dto.Position>(objectSpace);
        _departments = MapLookupDto<Bo.Department, Dto.Department>(objectSpace);
        _regions = MapLookupDto<Bo.Region, Dto.Region>(objectSpace);
        _applicationStates = MapLookupDto<Bo.ApplicationState, Dto.ApplicationState>(objectSpace);
        _applicationLocations = MapLookupDto<Bo.ApplicationLocation, Dto.ApplicationLocation>(objectSpace);

        // BaseObject (non-LookupBase) lookups — mapped explicitly.
        _actualPositions = MapLookup(objectSpace.GetObjectsQuery<Bo.ActualPosition>(), x => new Dto.ActualPosition
        {
            Id = x.ID,
            Name = x.Name ?? "",
        });
        _lodgings = MapLookup(objectSpace.GetObjectsQuery<Bo.Lodging>(), x => new Dto.Lodging
        {
            Id = x.ID,
            FullAddress = x.FullAddress ?? "",
            CityId = x.City != null ? x.City.ID : null,
        });
        _hotels = MapLookup(objectSpace.GetObjectsQuery<Bo.Hotel>(), x => new Dto.Hotel
        {
            Id = x.ID,
            Name = x.Name ?? "",
            CityId = x.City != null ? x.City.ID : null,
        });
        _hospitals = MapLookup(objectSpace.GetObjectsQuery<Bo.Hospital>(), x => new Dto.Hospital
        {
            Id = x.ID,
            Name = x.Name ?? "",
            CityId = x.City != null ? x.City.ID : null,
        });
        _otherSites = MapLookup(objectSpace.GetObjectsQuery<Bo.OtherSite>(), x => new Dto.OtherSite
        {
            Id = x.ID,
            FullAddress = x.FullAddress ?? "",
            CityId = x.City != null ? x.City.ID : null,
        });

        // Application-domain lookups.
        _applicationTypes = MapLookup(objectSpace.GetObjectsQuery<Bo.ApplicationType>(), x => new Dto.ApplicationType
        {
            Id = x.ID,
            Name = x.Name ?? "",
            NameTm = x.NameTm ?? "",
            Code = x.Code ?? "",
            IsDefault = x.IsDefault,
        });
        _urgencies = MapLookupDto<Bo.Urgency, Dto.Urgency>(objectSpace);
        _visaPeriods = MapLookupDto<Bo.VisaPeriod, Dto.VisaPeriod>(objectSpace);
        _visaCategories = MapLookupDto<Bo.VisaCategory, Dto.VisaCategory>(objectSpace);
        _projectContracts = MapLookupDto<Bo.ProjectContract, Dto.ProjectContract>(objectSpace);
        _approvalLegProfiles = MapLookupDto<Bo.ApprovalLegProfile, Dto.ApprovalLegProfile>(objectSpace);
        // Region navigation must be read here: CityLookupMatcher scopes by region, and Demo/prod
        // often leave City.RegionName null even when RegionID is set.
        _cities = MapLookup(objectSpace.GetObjectsQuery<Bo.City>(), x =>
        {
            var regionName = !string.IsNullOrWhiteSpace(x.RegionName)
                ? x.RegionName
                : x.Region?.NameTm;
            return new Dto.City
            {
                Id = x.ID,
                Name = x.Name ?? "",
                NameTm = x.NameTm ?? "",
                Code = x.Code ?? "",
                IsDefault = x.IsDefault,
                RegionName = regionName ?? "",
                Region = x.Region == null
                    ? null
                    : new Dto.Region
                    {
                        Id = x.Region.ID,
                        Name = x.Region.Name ?? "",
                        NameTm = x.Region.NameTm ?? "",
                        Code = x.Region.Code ?? "",
                        IsDefault = x.Region.IsDefault,
                    },
            };
        });
        _movementPermitLocations = MapLookupDto<Bo.MovementPermitLocation, Dto.MovementPermitLocation>(objectSpace);
        _borderZoneLocations = MapLookupDto<Bo.BorderZoneLocation, Dto.BorderZoneLocation>(objectSpace);
        _checkPoints = MapLookupDto<Bo.CheckPoint, Dto.CheckPoint>(objectSpace);
        _migrationServices = MapLookupDto<Bo.MigrationService, Dto.MigrationService>(objectSpace);

        var lookupCatalogDir = string.IsNullOrWhiteSpace(tenantCatalogDirectory)
            ? null
            : Path.GetDirectoryName(tenantCatalogDirectory);
        if (!string.IsNullOrWhiteSpace(lookupCatalogDir))
            EnrichCityRegionNames(Path.Combine(lookupCatalogDir, "city.json"));

        if (!string.IsNullOrWhiteSpace(tenantCatalogDirectory))
            EnrichSiteCityIdsFromTenantCatalogs(tenantCatalogDirectory);
    }

    private static List<TDto> MapLookup<TBo, TDto>(IQueryable<TBo> query, Func<TBo, TDto> map) =>
        query.AsEnumerable().Select(map).ToList();

    private static List<TDto> MapLookupDto<TBo, TDto>(IObjectSpace objectSpace)
        where TBo : Bo.LookupBase
        where TDto : class, new() =>
        MapLookup(objectSpace.GetObjectsQuery<TBo>(), x =>
        {
            var dto = new TDto();
            var idProperty = typeof(TDto).GetProperty(nameof(Dto.Gender.Id));
            var nameProperty = typeof(TDto).GetProperty(nameof(Dto.Gender.Name));
            var nameTmProperty = typeof(TDto).GetProperty(nameof(Dto.Gender.NameTm));
            var codeProperty = typeof(TDto).GetProperty(nameof(Dto.Gender.Code));
            var isDefaultProperty = typeof(TDto).GetProperty(nameof(Dto.Gender.IsDefault));
            // Required for VisaType / VisaCategory / PassportType / EducationLevel resolve by
            // LocalizationKey. Omitting this caused every in-process Visa to land on default WP.
            var localizationKeyProperty = typeof(TDto).GetProperty(nameof(Dto.VisaType.LocalizationKey));

            idProperty?.SetValue(dto, x.ID);
            nameProperty?.SetValue(dto, x.Name ?? "");
            nameTmProperty?.SetValue(dto, x.NameTm ?? "");
            codeProperty?.SetValue(dto, x.Code ?? "");
            isDefaultProperty?.SetValue(dto, x.IsDefault);
            localizationKeyProperty?.SetValue(dto, x.LocalizationKey ?? "");
            return dto;
        });
}
