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

    public async Task LoadAsync(ApiClient api)
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
    }

    public void RegisterActualPosition(ActualPosition row)
    {
        if (row.Id != Guid.Empty)
            _actualPositions.Add(row);
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

    private Guid? ResolveDefaultEducationLevel()
    {
        var preferred = _educationLevels.FirstOrDefault(e => e.IsDefault);
        if (preferred != null)
            return preferred.Id;

        preferred = _educationLevels.FirstOrDefault(e =>
            string.Equals(e.LocalizationKey, "SpecialSecondary", StringComparison.OrdinalIgnoreCase));
        return preferred?.Id ?? (_educationLevels.Count > 0 ? _educationLevels[0].Id : null);
    }

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
        _ => throw new InvalidOperationException($"Unsupported lookup type {typeof(T).Name}"),
    };
}
