using System.Text.Json;
using Visa2026.DataImporter;

namespace Visa2026.DataImporter.Legacy.Visa2014;

internal sealed class Visa2014ODataLookupResolver
{
    private List<Gender> _genders = [];
    private List<Country> _countries = [];
    private List<MaritalStatus> _maritalStatuses = [];
    private List<Relationship> _relationships = [];
    private List<ProjectContract> _projectContracts = [];
    private List<PassportType> _passportTypes = [];
    private List<VisaType> _visaTypes = [];
    private List<VisaCategory> _visaCategories = [];
    private List<VisaIssuedPlace> _visaIssuedPlaces = [];
    private List<Subcontractor> _subcontractors = [];
    private List<EducationLevel> _educationLevels = [];
    private List<EducationInstitution> _educationInstitutions = [];
    private List<Specialty> _specialties = [];
    private List<Position> _positions = [];
    private List<Department> _departments = [];
    private List<ActualPosition> _actualPositions = [];
    private List<Region> _regions = [];
    private List<City> _cities = [];
    private List<Lodging> _lodgings = [];
    private List<Hotel> _hotels = [];
    private List<Hospital> _hospitals = [];
    private List<OtherSite> _otherSites = [];
    private List<ApplicationType> _applicationTypes = [];
    private List<Urgency> _urgencies = [];
    private List<VisaPeriod> _visaPeriods = [];
    private List<MovementPermitLocation> _movementPermitLocations = [];
    private List<BorderZoneLocation> _borderZoneLocations = [];
    private List<ApplicationState> _applicationStates = [];
    private List<ApplicationLocation> _applicationLocations = [];
    private List<CheckPoint> _checkPoints = [];

    public async Task LoadAsync(ApiClient api, string? tenantCatalogDirectory = null)
    {
        _genders = await api.GetAllAsync<Gender>("Gender");
        _countries = await api.GetAllAsync<Country>("Country");
        _maritalStatuses = await api.GetAllAsync<MaritalStatus>("MaritalStatus");
        _relationships = await api.GetAllAsync<Relationship>("Relationship");
        _projectContracts = await api.GetAllAsync<ProjectContract>("ProjectContract");
        _passportTypes = await api.GetAllAsync<PassportType>("PassportType");
        _visaTypes = await api.GetAllAsync<VisaType>("VisaType");
        _visaCategories = await api.GetAllAsync<VisaCategory>("VisaCategory");
        _visaIssuedPlaces = await api.GetAllAsync<VisaIssuedPlace>("VisaIssuedPlace");
        _subcontractors = await api.GetAllAsync<Subcontractor>("Subcontractor");
        _educationLevels = await api.GetAllAsync<EducationLevel>("EducationLevel");
        _educationInstitutions = await api.GetAllAsync<EducationInstitution>("EducationInstitution");
        _specialties = await api.GetAllAsync<Specialty>("Specialty");
        _positions = await api.GetAllAsync<Position>("Position");
        _departments = await api.GetAllAsync<Department>("Department");
        _actualPositions = await api.GetAllAsync<ActualPosition>("ActualPosition");
        _regions = await api.GetAllAsync<Region>("Region");
        _cities = await api.GetAllAsync<City>("City");
        _lodgings = await api.GetAllAsync<Lodging>("Lodging");
        _hotels = await api.GetAllAsync<Hotel>("Hotel");
        _hospitals = await api.GetAllAsync<Hospital>("Hospital");
        _otherSites = await api.GetAllAsync<OtherSite>("OtherSite");
        _applicationTypes = await api.GetAllAsync<ApplicationType>("ApplicationType");
        _urgencies = await api.GetAllAsync<Urgency>("Urgency");
        _visaPeriods = await api.GetAllAsync<VisaPeriod>("VisaPeriod");
        _movementPermitLocations = await api.GetAllAsync<MovementPermitLocation>("MovementPermitLocation");
        _borderZoneLocations = await api.GetAllAsync<BorderZoneLocation>("BorderZoneLocation");
        _applicationStates = await api.GetAllAsync<ApplicationState>("ApplicationState");
        _applicationLocations = await api.GetAllAsync<ApplicationLocation>("ApplicationLocation");
        _checkPoints = await api.GetAllAsync<CheckPoint>("CheckPoint");

        var lookupCatalogDir = string.IsNullOrWhiteSpace(tenantCatalogDirectory)
            ? null
            : Path.GetDirectoryName(tenantCatalogDirectory);
        if (!string.IsNullOrWhiteSpace(lookupCatalogDir))
            EnrichCityRegionNames(Path.Combine(lookupCatalogDir, "city.json"));

        if (!string.IsNullOrWhiteSpace(tenantCatalogDirectory))
            EnrichSiteCityIdsFromTenantCatalogs(tenantCatalogDirectory);
    }

    private sealed record CityCatalogRow(string NameTm, string Region);

    private void EnrichCityRegionNames(string catalogPath)
    {
        if (!File.Exists(catalogPath))
            return;

        var rows = LoadCityCatalogRows(catalogPath);
        foreach (var city in _cities)
        {
            if (!string.IsNullOrWhiteSpace(city.RegionName))
                continue;

            var catalogRow = rows.FirstOrDefault(r =>
                Visa2014CatalogMatchHelper.KeysEqual(r.NameTm, city.NameTm));
            if (catalogRow != null)
                city.RegionName = catalogRow.Region;
        }
    }

    private static List<CityCatalogRow> LoadCityCatalogRows(string catalogPath)
    {
        using var stream = File.OpenRead(catalogPath);
        using var doc = JsonDocument.Parse(stream);
        if (!doc.RootElement.TryGetProperty("rows", out var rowsElement) || rowsElement.ValueKind != JsonValueKind.Array)
            return [];

        var rows = new List<CityCatalogRow>();
        foreach (var row in rowsElement.EnumerateArray())
        {
            var nameTm = row.TryGetProperty("NameTm", out var nameEl) ? nameEl.GetString() : null;
            var region = row.TryGetProperty("Region", out var regionEl) ? regionEl.GetString() : null;
            if (string.IsNullOrWhiteSpace(nameTm) || string.IsNullOrWhiteSpace(region))
                continue;

            rows.Add(new CityCatalogRow(nameTm, region));
        }

        return rows;
    }

    private sealed record TenantCatalogRow(string Region, string City, string Scalar);

    private void EnrichSiteCityIdsFromTenantCatalogs(string tenantCatalogDirectory)
    {
        EnrichSiteCityIds(
            _lodgings,
            Path.Combine(tenantCatalogDirectory, "lodging.json"),
            l => l.FullAddress,
            (l, cityId) => l.CityId = cityId,
            useLodgingDedupe: true);
        EnrichSiteCityIds(
            _hotels,
            Path.Combine(tenantCatalogDirectory, "hotel.json"),
            h => h.Name,
            (h, cityId) => h.CityId = cityId,
            useLodgingDedupe: false);
        EnrichSiteCityIds(
            _hospitals,
            Path.Combine(tenantCatalogDirectory, "hospital.json"),
            h => h.Name,
            (h, cityId) => h.CityId = cityId,
            useLodgingDedupe: false);
        EnrichSiteCityIds(
            _otherSites,
            Path.Combine(tenantCatalogDirectory, "other-site.json"),
            s => s.FullAddress,
            (s, cityId) => s.CityId = cityId,
            useLodgingDedupe: true);
    }

    private void EnrichSiteCityIds<T>(
        List<T> sites,
        string catalogPath,
        Func<T, string> scalarSelector,
        Action<T, Guid> assignCityId,
        bool useLodgingDedupe)
    {
        if (!File.Exists(catalogPath))
            return;

        var rows = LoadTenantCatalogRows(catalogPath);
        foreach (var site in sites)
        {
            if (GetRowCityId(site).HasValue)
                continue;

            var scalar = scalarSelector(site);
            var catalogRow = FindTenantCatalogRow(rows, scalar, useLodgingDedupe);
            if (catalogRow == null)
                continue;

            var cityId = ResolveCity(catalogRow.City, catalogRow.Region);
            if (cityId.HasValue)
                assignCityId(site, cityId.Value);
        }
    }

    private static List<TenantCatalogRow> LoadTenantCatalogRows(string catalogPath)
    {
        using var stream = File.OpenRead(catalogPath);
        using var doc = JsonDocument.Parse(stream);
        if (!doc.RootElement.TryGetProperty("rows", out var rowsElement) || rowsElement.ValueKind != JsonValueKind.Array)
            return [];

        var rows = new List<TenantCatalogRow>();
        foreach (var row in rowsElement.EnumerateArray())
        {
            var region = row.TryGetProperty("Region", out var regionEl) ? regionEl.GetString() : null;
            var city = row.TryGetProperty("City", out var cityEl) ? cityEl.GetString() : null;
            var scalar = row.TryGetProperty("FullAddress", out var addressEl)
                ? addressEl.GetString()
                : row.TryGetProperty("Name", out var nameEl) ? nameEl.GetString() : null;
            if (string.IsNullOrWhiteSpace(city) || string.IsNullOrWhiteSpace(scalar))
                continue;

            rows.Add(new TenantCatalogRow(region ?? string.Empty, city, scalar));
        }

        return rows;
    }

    private static TenantCatalogRow? FindTenantCatalogRow(
        IReadOnlyList<TenantCatalogRow> rows,
        string? scalar,
        bool useLodgingDedupe)
    {
        if (string.IsNullOrWhiteSpace(scalar))
            return null;

        foreach (var row in rows)
        {
            if (Visa2014CatalogMatchHelper.KeysEqual(row.Scalar, scalar))
                return row;
        }

        if (!useLodgingDedupe)
            return null;

        foreach (var row in rows)
        {
            var wantKey = Visa2014AddressLineNormalizer.BuildLodgingDedupeKey(row.City, scalar);
            var rowKey = Visa2014AddressLineNormalizer.BuildLodgingDedupeKey(row.City, row.Scalar);
            if (!string.IsNullOrEmpty(wantKey) && wantKey == rowKey)
                return row;
        }

        var scalarPart = Visa2014AddressLineNormalizer.ExtractLodgingDedupeScalar(scalar);
        if (string.IsNullOrEmpty(scalarPart))
            return null;

        TenantCatalogRow? sole = null;
        foreach (var row in rows)
        {
            var catalogScalarPart = Visa2014AddressLineNormalizer.ExtractLodgingDedupeScalar(row.Scalar);
            if (catalogScalarPart != scalarPart)
                continue;

            if (sole != null)
                return null;

            sole = row;
        }

        return sole;
    }

    public void RegisterActualPosition(ActualPosition row)
    {
        if (row.Id != Guid.Empty)
            _actualPositions.Add(row);
    }

    public Guid? ResolveApplicationType(string? name) =>
        ResolveByName(_applicationTypes, name, t => t.Name);

    public Guid? ResolveApplicationState(string? code) =>
        ResolveByCode(_applicationStates, code, s => s.Code);

    public Guid? ResolveApplicationLocation(string? code) =>
        ResolveByCode(_applicationLocations, code, l => l.Code);

    public Guid? ResolveUrgency(string? code) =>
        ResolveByCode(_urgencies, code, u => u.Code);

    public Guid? ResolveVisaPeriod(string? localizationKeyOrCode)
    {
        if (string.IsNullOrWhiteSpace(localizationKeyOrCode))
            return null;

        foreach (var row in _visaPeriods)
        {
            if (VisaPeriodKeyMatches(row, localizationKeyOrCode))
                return row.Id;
        }

        return null;
    }

    public Guid? ResolveMovementPermitLocation(string? nameTm) =>
        ResolveByNameTm(_movementPermitLocations, nameTm, m => m.NameTm);

    public Guid? ResolveCheckPoint(string? nameTmOrCode)
    {
        if (string.IsNullOrWhiteSpace(nameTmOrCode))
            return null;

        var byNameTm = ResolveByNameTm(_checkPoints, nameTmOrCode, c => c.NameTm);
        if (byNameTm.HasValue)
            return byNameTm;

        return ResolveByCode(_checkPoints, nameTmOrCode, c => c.Code);
    }

    public Guid? ResolveBorderZoneLocation(string? commaSeparatedLabels)
    {
        if (string.IsNullOrWhiteSpace(commaSeparatedLabels))
            return null;

        foreach (var part in commaSeparatedLabels.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            if (IsBorderZoneNoneLabel(part))
                continue;

            var id = ResolveByNameTm(_borderZoneLocations, part, b => b.NameTm);
            if (id.HasValue)
                return id;
        }

        return null;
    }

    public Guid? ResolveGender(string? translatedCode) =>
        ResolveByCode(_genders, translatedCode, g => g.Code);

    public Guid? ResolveCountry(string? translatedCode) =>
        ResolveByCode(_countries, translatedCode, c => c.Code);

    public Guid? ResolvePassportType(string? localizationKey)
    {
        if (string.IsNullOrWhiteSpace(localizationKey))
            return ResolveDefaultPassportType();

        foreach (var row in _passportTypes)
        {
            if (PassportTypeKeyMatches(row, localizationKey))
                return row.Id;
        }

        return ResolveDefaultPassportType();
    }

    public Guid? ResolveVisaType(string? localizationKey)
    {
        if (string.IsNullOrWhiteSpace(localizationKey))
            return ResolveDefaultVisaType();

        foreach (var row in _visaTypes)
        {
            if (VisaTypeKeyMatches(row, localizationKey))
                return row.Id;
        }

        return ResolveDefaultVisaType();
    }

    public Guid? ResolveVisaCategory(string? localizationKey)
    {
        if (string.IsNullOrWhiteSpace(localizationKey))
            return ResolveDefaultVisaCategory();

        foreach (var row in _visaCategories)
        {
            if (VisaCategoryKeyMatches(row, localizationKey))
                return row.Id;
        }

        return ResolveDefaultVisaCategory();
    }

    public Guid? ResolveVisaIssuedPlace(string? nameTm) =>
        ResolveByNameTm(_visaIssuedPlaces, nameTm, p => p.NameTm);

    private Guid? ResolveDefaultVisaType()
    {
        var preferred = _visaTypes.FirstOrDefault(v => v.IsDefault);
        if (preferred != null)
            return preferred.Id;

        preferred = _visaTypes.FirstOrDefault(v =>
            string.Equals(v.LocalizationKey, "WP", StringComparison.OrdinalIgnoreCase)
            || string.Equals(v.Code, "WP", StringComparison.OrdinalIgnoreCase));
        return preferred?.Id ?? (_visaTypes.Count > 0 ? _visaTypes[0].Id : null);
    }

    private Guid? ResolveDefaultVisaCategory()
    {
        var preferred = _visaCategories.FirstOrDefault(v => v.IsDefault);
        if (preferred != null)
            return preferred.Id;

        preferred = _visaCategories.FirstOrDefault(v =>
            string.Equals(v.LocalizationKey, "Multiple", StringComparison.OrdinalIgnoreCase));
        return preferred?.Id ?? (_visaCategories.Count > 0 ? _visaCategories[0].Id : null);
    }

    private static bool VisaTypeKeyMatches(VisaType row, string key)
    {
        if (Visa2014CatalogMatchHelper.KeysEqual(row.LocalizationKey, key))
            return true;
        if (Visa2014CatalogMatchHelper.KeysEqual(row.Code, key))
            return true;
        return string.Equals(row.LocalizationKey, key, StringComparison.OrdinalIgnoreCase)
            || string.Equals(row.Code, key, StringComparison.OrdinalIgnoreCase);
    }

    private static bool VisaCategoryKeyMatches(VisaCategory row, string key)
    {
        if (Visa2014CatalogMatchHelper.KeysEqual(row.LocalizationKey, key))
            return true;
        if (Visa2014CatalogMatchHelper.KeysEqual(row.Code, key))
            return true;
        return string.Equals(row.LocalizationKey, key, StringComparison.OrdinalIgnoreCase)
            || string.Equals(row.Code, key, StringComparison.OrdinalIgnoreCase);
    }

    public Guid? ResolveMaritalStatus(string? translatedCode) =>
        ResolveByCode(_maritalStatuses, translatedCode, m => m.Code);

    public Guid? ResolveRelationship(string? translatedNameTm) =>
        ResolveByNameTm(_relationships, translatedNameTm, r => r.NameTm);

    public Guid? ResolveProjectContract(string? translatedCode)
    {
        if (string.IsNullOrWhiteSpace(translatedCode))
            return null;

        // ProjectContract.Code is not mapped in EF — OData rows match by NameTm title prefix.
        var matches = _projectContracts
            .Where(c => ProjectContractTitleMatches(c.NameTm, translatedCode)
                        || Visa2014CatalogMatchHelper.KeysEqual(c.Code, translatedCode))
            .ToList();

        if (matches.Count == 0)
            return null;

        if (matches.Count == 1)
            return matches[0].Id;

        var preferred = matches.FirstOrDefault(c =>
            c.NameTm.Contains("2 ylalaşyk", StringComparison.OrdinalIgnoreCase) ||
            c.NameTm.Contains("2 ylalasyk", StringComparison.OrdinalIgnoreCase));

        return (preferred ?? matches[0]).Id;
    }

    private Guid? ResolveDefaultPassportType()
    {
        var preferred = _passportTypes.FirstOrDefault(p => p.IsDefault);
        if (preferred != null)
            return preferred.Id;

        preferred = _passportTypes.FirstOrDefault(p =>
            string.Equals(p.LocalizationKey, "P", StringComparison.OrdinalIgnoreCase)
            || string.Equals(p.Code, "P", StringComparison.OrdinalIgnoreCase)
            || string.Equals(p.PdfFormCode, "P", StringComparison.OrdinalIgnoreCase));
        if (preferred != null)
            return preferred.Id;

        return _passportTypes.Count > 0 ? _passportTypes[0].Id : null;
    }

    private static bool PassportTypeKeyMatches(PassportType row, string key)
    {
        if (Visa2014CatalogMatchHelper.KeysEqual(row.LocalizationKey, key))
            return true;
        if (Visa2014CatalogMatchHelper.KeysEqual(row.Code, key))
            return true;
        if (Visa2014CatalogMatchHelper.KeysEqual(row.PdfFormCode, key))
            return true;
        return string.Equals(row.LocalizationKey, key, StringComparison.OrdinalIgnoreCase)
            || string.Equals(row.Code, key, StringComparison.OrdinalIgnoreCase)
            || string.Equals(row.PdfFormCode, key, StringComparison.OrdinalIgnoreCase);
    }

    public Guid? ResolveDefaultSubcontractor()
    {
        var preferred = _subcontractors.FirstOrDefault(s => s.IsDefault);
        if (preferred != null)
            return preferred.Id;

        return _subcontractors.Count > 0 ? _subcontractors[0].Id : null;
    }

    public Guid? ResolveEducationLevel(string? localizationKey)
    {
        if (string.IsNullOrWhiteSpace(localizationKey))
            return ResolveDefaultEducationLevel();

        foreach (var row in _educationLevels)
        {
            if (EducationLevelKeyMatches(row, localizationKey))
                return row.Id;
        }

        return ResolveDefaultEducationLevel();
    }

    public Guid? ResolveEducationInstitution(string? nameTm) =>
        ResolveByNameTm(_educationInstitutions, nameTm, i => i.NameTm);

    public Guid? ResolveSpecialty(string? nameTm) =>
        ResolveByNameTm(_specialties, nameTm, s => s.NameTm);

    public Guid? ResolvePosition(string? nameTm) =>
        ResolveByNameTm(_positions, nameTm, p => p.NameTm);

    public Guid? ResolveDepartment(string? nameTm) =>
        ResolveByNameTm(_departments, nameTm, d => d.NameTm);

    public Guid? ResolveActualPosition(string? name) =>
        ResolveByName(_actualPositions, name, a => a.Name);

    public Guid? ResolveRegion(string? nameTm) =>
        ResolveByNameTm(_regions, nameTm, r => r.NameTm);

    public Guid? ResolveCity(string? nameTm, string? regionNameTm = null)
    {
        if (string.IsNullOrWhiteSpace(regionNameTm))
            return ResolveByNameTm(_cities, nameTm, c => c.NameTm);

        foreach (var city in _cities)
        {
            if (!Visa2014CatalogMatchHelper.KeysEqual(city.NameTm, nameTm)
                && !string.Equals(city.NameTm?.Trim(), nameTm?.Trim(), StringComparison.Ordinal))
                continue;

            if (city.Region != null && Visa2014CatalogMatchHelper.KeysEqual(city.Region.NameTm, regionNameTm))
                return city.Id;
            if (Visa2014CatalogMatchHelper.KeysEqual(city.RegionName, regionNameTm))
                return city.Id;
        }

        return ResolveByNameTm(_cities, nameTm, c => c.NameTm);
    }

    public Guid? ResolveLodging(string? cityNameTm, string? regionNameTm, string? fullAddress)
    {
        var exact = ResolveSiteByCityAndScalar(_lodgings, cityNameTm, regionNameTm, fullAddress, l => l.FullAddress);
        if (exact.HasValue)
            return exact;

        var wantKey = Visa2014AddressLineNormalizer.BuildLodgingDedupeKey(cityNameTm, fullAddress);
        if (string.IsNullOrEmpty(wantKey))
            return null;

        if (!ResolveCity(cityNameTm, regionNameTm).HasValue)
            return null;

        var byDedupe = ResolveSiteByLodgingDedupeKey(_lodgings, cityNameTm, regionNameTm, wantKey, l => l.FullAddress);
        if (byDedupe.HasValue)
            return byDedupe;

        return ResolveSiteByRegionScopedDedupeScalar(_lodgings, regionNameTm, fullAddress, l => l.FullAddress);
    }

    public Guid? ResolveHotel(string? cityNameTm, string? regionNameTm, string? name)
    {
        var exact = ResolveSiteByCityAndScalar(_hotels, cityNameTm, regionNameTm, name, h => h.Name);
        if (exact.HasValue)
            return exact;

        if (string.IsNullOrWhiteSpace(name) || !ResolveCity(cityNameTm, regionNameTm).HasValue)
            return null;

        Guid? fallback = null;
        foreach (var row in _hotels)
        {
            if (!Visa2014CatalogMatchHelper.KeysEqual(row.Name, name)
                && !string.Equals(row.Name?.Trim(), name.Trim(), StringComparison.Ordinal))
                continue;

            var rowCityId = GetRowCityId(row);
            if (rowCityId.HasValue && CityBelongsToRegion(rowCityId.Value, regionNameTm!))
                return row.Id;

            fallback ??= row.Id;
        }

        return fallback;
    }

    public Guid? ResolveHospital(string? cityNameTm, string? regionNameTm, string? name) =>
        ResolveSiteByCityAndScalar(_hospitals, cityNameTm, regionNameTm, name, h => h.Name);

    public Guid? ResolveOtherSite(string? cityNameTm, string? regionNameTm, string? fullAddress)
    {
        var exact = ResolveSiteByCityAndScalar(_otherSites, cityNameTm, regionNameTm, fullAddress, s => s.FullAddress);
        if (exact.HasValue)
            return exact;

        var wantKey = Visa2014AddressLineNormalizer.BuildLodgingDedupeKey(cityNameTm, fullAddress);
        if (string.IsNullOrEmpty(wantKey))
            return null;

        if (!ResolveCity(cityNameTm, regionNameTm).HasValue)
            return null;

        var byDedupe = ResolveSiteByLodgingDedupeKey(_otherSites, cityNameTm, regionNameTm, wantKey, s => s.FullAddress);
        if (byDedupe.HasValue)
            return byDedupe;

        return ResolveSiteByRegionScopedDedupeScalar(_otherSites, regionNameTm, fullAddress, s => s.FullAddress);
    }

    private Guid? ResolveSiteByRegionScopedDedupeScalar<T>(
        IEnumerable<T> rows,
        string? regionNameTm,
        string? scalar,
        Func<T, string?> scalarSelector) where T : class
    {
        if (string.IsNullOrWhiteSpace(regionNameTm) || string.IsNullOrWhiteSpace(scalar))
            return null;
        if (!ResolveRegion(regionNameTm).HasValue)
            return null;

        var wantScalar = Visa2014AddressLineNormalizer.ExtractLodgingDedupeScalar(scalar);
        if (string.IsNullOrEmpty(wantScalar))
            return null;

        Guid? fallback = null;
        foreach (var row in rows)
        {
            var rowScalar = Visa2014AddressLineNormalizer.ExtractLodgingDedupeScalar(scalarSelector(row));
            if (rowScalar != wantScalar)
                continue;

            var rowCityId = GetRowCityId(row);
            if (rowCityId.HasValue && CityBelongsToRegion(rowCityId.Value, regionNameTm))
                return GetId(row);

            fallback ??= GetId(row);
        }

        return fallback;
    }

    private bool CityBelongsToRegion(Guid cityId, string regionNameTm)
    {
        var city = _cities.FirstOrDefault(c => c.Id == cityId);
        if (city == null)
            return false;

        if (Visa2014CatalogMatchHelper.KeysEqual(city.RegionName, regionNameTm))
            return true;

        return city.Region != null
            && Visa2014CatalogMatchHelper.KeysEqual(city.Region.NameTm, regionNameTm);
    }

    private Guid? ResolveSiteByLodgingDedupeKey<T>(
        IEnumerable<T> rows,
        string? cityNameTm,
        string? regionNameTm,
        string wantKey,
        Func<T, string?> scalarSelector) where T : class
    {
        var cityId = ResolveCity(cityNameTm, regionNameTm);
        T? preferred = default;
        T? fallback = default;

        foreach (var row in rows)
        {
            var rowKey = Visa2014AddressLineNormalizer.BuildLodgingDedupeKey(cityNameTm, scalarSelector(row));
            if (rowKey != wantKey)
                continue;

            if (cityId.HasValue)
            {
                var rowCityId = GetRowCityId(row);
                if (rowCityId.HasValue && rowCityId.Value == cityId.Value)
                {
                    preferred = row;
                    break;
                }
            }

            fallback ??= row;
        }

        var match = preferred ?? fallback;
        return match != null ? GetId(match) : null;
    }

    private Guid? ResolveSiteByCityAndScalar<T>(
        IEnumerable<T> rows,
        string? cityNameTm,
        string? regionNameTm,
        string? scalar,
        Func<T, string> scalarSelector) where T : class
    {
        var cityId = ResolveCity(cityNameTm, regionNameTm);
        if (!cityId.HasValue || string.IsNullOrWhiteSpace(scalar))
            return null;

        foreach (var row in rows)
        {
            var rowCityId = GetRowCityId(row);
            if (!rowCityId.HasValue || rowCityId.Value != cityId.Value)
                continue;

            var value = scalarSelector(row);
            if (Visa2014CatalogMatchHelper.KeysEqual(value, scalar)
                || string.Equals(value?.Trim(), scalar.Trim(), StringComparison.Ordinal))
                return GetId(row);
        }

        return null;
    }

    private Guid? GetRowCityId<T>(T row)
    {
        var city = GetCity(row);
        if (city != null && city.Id != Guid.Empty)
            return city.Id;

        return row switch
        {
            Lodging l when l.CityId.HasValue => l.CityId,
            Hotel h when h.CityId.HasValue => h.CityId,
            Hospital hs when hs.CityId.HasValue => hs.CityId,
            OtherSite o when o.CityId.HasValue => o.CityId,
            _ => null,
        };
    }

    private static City? GetCity<T>(T row) => row switch
    {
        Lodging l => l.City,
        Hotel h => h.City,
        Hospital hs => hs.City,
        OtherSite o => o.City,
        _ => null,
    };

    private Guid? ResolveDefaultEducationLevel()
    {
        var preferred = _educationLevels.FirstOrDefault(e => e.IsDefault);
        if (preferred != null)
            return preferred.Id;

        preferred = _educationLevels.FirstOrDefault(e =>
            string.Equals(e.LocalizationKey, "SpecialSecondary", StringComparison.OrdinalIgnoreCase));
        return preferred?.Id ?? (_educationLevels.Count > 0 ? _educationLevels[0].Id : null);
    }

    private static bool VisaPeriodKeyMatches(VisaPeriod row, string key)
    {
        if (Visa2014CatalogMatchHelper.KeysEqual(row.LocalizationKey, key))
            return true;
        if (Visa2014CatalogMatchHelper.KeysEqual(row.Code, key))
            return true;
        return string.Equals(row.LocalizationKey, key, StringComparison.OrdinalIgnoreCase)
            || string.Equals(row.Code, key, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsBorderZoneNoneLabel(string label) =>
        Visa2014CatalogMatchHelper.KeysEqual(label, "Ýok")
        || string.Equals(label.Trim(), "Ýok", StringComparison.Ordinal);

    private static bool EducationLevelKeyMatches(EducationLevel row, string key)
    {
        if (Visa2014CatalogMatchHelper.KeysEqual(row.LocalizationKey, key))
            return true;
        if (Visa2014CatalogMatchHelper.KeysEqual(row.Code, key))
            return true;
        return string.Equals(row.LocalizationKey, key, StringComparison.OrdinalIgnoreCase)
            || string.Equals(row.Code, key, StringComparison.OrdinalIgnoreCase);
    }

    private static bool ProjectContractTitleMatches(string? nameTm, string legacyCode)
    {
        if (string.IsNullOrWhiteSpace(nameTm))
            return false;

        var title = nameTm.Trim();
        var code = legacyCode.Trim();
        if (title.StartsWith(code, StringComparison.OrdinalIgnoreCase))
            return true;

        return Visa2014CatalogMatchHelper.KeysEqual(title, code);
    }

    private static Guid? ResolveByCode<T>(
        IEnumerable<T> rows,
        string? key,
        Func<T, string> codeSelector)
    {
        if (string.IsNullOrWhiteSpace(key))
            return null;

        foreach (var row in rows)
        {
            if (Visa2014CatalogMatchHelper.KeysEqual(codeSelector(row), key))
                return GetId(row);
        }

        return null;
    }

    private static Guid? ResolveByNameTm<T>(
        IEnumerable<T> rows,
        string? key,
        Func<T, string> nameSelector) =>
        ResolveByName(rows, key, nameSelector);

    private static Guid? ResolveByName<T>(
        IEnumerable<T> rows,
        string? key,
        Func<T, string> nameSelector)
    {
        if (string.IsNullOrWhiteSpace(key))
            return null;

        T? exact = default;
        var normalizedMatches = new List<T>();
        foreach (var row in rows)
        {
            var name = nameSelector(row);
            if (string.Equals(name?.Trim(), key.Trim(), StringComparison.Ordinal))
                exact = row;

            if (Visa2014CatalogMatchHelper.KeysEqual(name, key))
                normalizedMatches.Add(row);
        }

        if (exact != null)
            return GetId(exact);

        if (normalizedMatches.Count == 0)
            return null;

        var keeper = normalizedMatches
            .OrderByDescending(r => nameSelector(r)?.Trim().Length ?? 0)
            .ThenBy(r => GetId(r))
            .First();
        return GetId(keeper);
    }

    private static Guid GetId<T>(T row) => row switch
    {
        Gender g => g.Id,
        Country c => c.Id,
        MaritalStatus m => m.Id,
        Relationship r => r.Id,
        ProjectContract p => p.Id,
        PassportType pt => pt.Id,
        VisaType vt => vt.Id,
        VisaCategory vc => vc.Id,
        VisaIssuedPlace vip => vip.Id,
        Subcontractor s => s.Id,
        EducationInstitution ei => ei.Id,
        Specialty sp => sp.Id,
        Position p => p.Id,
        Department d => d.Id,
        ActualPosition ap => ap.Id,
        Region r => r.Id,
        City c => c.Id,
        Lodging l => l.Id,
        Hotel h => h.Id,
        Hospital hs => hs.Id,
        OtherSite os => os.Id,
        ApplicationType at => at.Id,
        Urgency u => u.Id,
        VisaPeriod vp => vp.Id,
        MovementPermitLocation mpl => mpl.Id,
        BorderZoneLocation bzl => bzl.Id,
        ApplicationState ast => ast.Id,
        ApplicationLocation al => al.Id,
        CheckPoint cp => cp.Id,
        _ => throw new InvalidOperationException($"Unsupported lookup type {typeof(T).Name}"),
    };
}
