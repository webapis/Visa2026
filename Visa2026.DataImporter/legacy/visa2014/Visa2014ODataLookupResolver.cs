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
    private List<Subcontractor> _subcontractors = [];

    public async Task LoadAsync(ApiClient api)
    {
        _genders = await api.GetAllAsync<Gender>("Gender");
        _countries = await api.GetAllAsync<Country>("Country");
        _maritalStatuses = await api.GetAllAsync<MaritalStatus>("MaritalStatus");
        _relationships = await api.GetAllAsync<Relationship>("Relationship");
        _projectContracts = await api.GetAllAsync<ProjectContract>("ProjectContract");
        _passportTypes = await api.GetAllAsync<PassportType>("PassportType");
        _subcontractors = await api.GetAllAsync<Subcontractor>("Subcontractor");
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
        Func<T, string> nameSelector)
    {
        if (string.IsNullOrWhiteSpace(key))
            return null;

        foreach (var row in rows)
        {
            if (Visa2014CatalogMatchHelper.KeysEqual(nameSelector(row), key))
                return GetId(row);
        }

        return null;
    }

    private static Guid GetId<T>(T row) => row switch
    {
        Gender g => g.Id,
        Country c => c.Id,
        MaritalStatus m => m.Id,
        Relationship r => r.Id,
        ProjectContract p => p.Id,
        PassportType pt => pt.Id,
        Subcontractor s => s.Id,
        _ => throw new InvalidOperationException($"Unsupported lookup type {typeof(T).Name}"),
    };
}
