using DevExpress.ExpressApp;
using DevExpress.ExpressApp.EFCore;
using Bo = Visa2026.Module.BusinessObjects;
using Dto = Visa2026.DataImporter;

namespace Visa2026.DataImporter.Legacy.Visa2014;

internal sealed partial class Visa2014ODataLookupResolver
{
    /// <summary>Load catalogs needed for Application / ApplicationItem import from in-process ObjectSpace.</summary>
    public void LoadFromObjectSpace(IObjectSpace objectSpace, string? tenantCatalogDirectory = null)
    {
        ArgumentNullException.ThrowIfNull(objectSpace);

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
        _cities = MapLookup(objectSpace.GetObjectsQuery<Bo.City>(), x => new Dto.City
        {
            Id = x.ID,
            Name = x.Name ?? "",
            NameTm = x.NameTm ?? "",
            Code = x.Code ?? "",
            IsDefault = x.IsDefault,
            RegionName = x.RegionName,
        });
        _movementPermitLocations = MapLookupDto<Bo.MovementPermitLocation, Dto.MovementPermitLocation>(objectSpace);
        _borderZoneLocations = MapLookupDto<Bo.BorderZoneLocation, Dto.BorderZoneLocation>(objectSpace);
        _checkPoints = MapLookupDto<Bo.CheckPoint, Dto.CheckPoint>(objectSpace);

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

            idProperty?.SetValue(dto, x.ID);
            nameProperty?.SetValue(dto, x.Name ?? "");
            nameTmProperty?.SetValue(dto, x.NameTm ?? "");
            codeProperty?.SetValue(dto, x.Code ?? "");
            isDefaultProperty?.SetValue(dto, x.IsDefault);
            return dto;
        });
}
