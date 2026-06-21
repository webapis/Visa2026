using Visa2026.DataImporter;

namespace Visa2026.DataImporter.Legacy.Visa2014;

internal sealed class Visa2014ODataLookupResolver
{
    private List<Gender> _genders = [];
    private List<Country> _countries = [];
    private List<MaritalStatus> _maritalStatuses = [];
    private List<Relationship> _relationships = [];
    private List<ProjectContract> _projectContracts = [];

    public async Task LoadAsync(ApiClient api)
    {
        _genders = await api.GetAllAsync<Gender>("Gender");
        _countries = await api.GetAllAsync<Country>("Country");
        _maritalStatuses = await api.GetAllAsync<MaritalStatus>("MaritalStatus");
        _relationships = await api.GetAllAsync<Relationship>("Relationship");
        _projectContracts = await api.GetAllAsync<ProjectContract>("ProjectContract");
    }

    public Guid? ResolveGender(string? translatedCode) =>
        ResolveByCode(_genders, translatedCode, g => g.Code);

    public Guid? ResolveCountry(string? translatedCode) =>
        ResolveByCode(_countries, translatedCode, c => c.Code);

    public Guid? ResolveMaritalStatus(string? translatedCode) =>
        ResolveByCode(_maritalStatuses, translatedCode, m => m.Code);

    public Guid? ResolveRelationship(string? translatedNameTm) =>
        ResolveByNameTm(_relationships, translatedNameTm, r => r.NameTm);

    public Guid? ResolveProjectContract(string? translatedCode)
    {
        if (string.IsNullOrWhiteSpace(translatedCode))
            return null;

        var matches = _projectContracts
            .Where(c => Visa2014CatalogMatchHelper.KeysEqual(c.Code, translatedCode))
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
        _ => throw new InvalidOperationException($"Unsupported lookup type {typeof(T).Name}"),
    };
}
