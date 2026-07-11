using Microsoft.Data.Sqlite;

namespace Visa2026.DataImporter.Legacy.Visa2014;

/// <summary>
/// Read-only Turkmenistan Region/City reference for import conflict resolution.
/// </summary>
internal sealed class Visa2014TurkmenistanGeographyStore : IDisposable
{
    private readonly SqliteConnection _conn;
    private static Visa2014TurkmenistanGeographyStore? _cached;
    private static string? _cachedPath;

    private Visa2014TurkmenistanGeographyStore(SqliteConnection conn) => _conn = conn;

    public static Visa2014TurkmenistanGeographyStore? TryOpenDefault()
    {
        var path = ResolveDefaultPath();
        if (path == null || !File.Exists(path))
            return null;
        return Open(path);
    }

    public static Visa2014TurkmenistanGeographyStore Open(string dbPath)
    {
        if (_cached != null && string.Equals(_cachedPath, Path.GetFullPath(dbPath), StringComparison.OrdinalIgnoreCase))
            return _cached;

        SQLitePCL.Batteries_V2.Init();
        var conn = new SqliteConnection($"Data Source={dbPath};Mode=ReadOnly");
        conn.Open();
        _cached?.Dispose();
        _cached = new Visa2014TurkmenistanGeographyStore(conn);
        _cachedPath = Path.GetFullPath(dbPath);
        return _cached;
    }

    public static string? ResolveDefaultPath()
    {
        var dataImporterRoot = Visa2014ContentRoot.FindDataImporterRoot();
        if (dataImporterRoot != null)
        {
            var p = Path.Combine(dataImporterRoot, "legacy", "visa2014", "reference", "turkmenistan-geography.db");
            if (File.Exists(p)) return p;
        }

        var beside = Path.Combine(AppContext.BaseDirectory, "legacy", "visa2014", "reference", "turkmenistan-geography.db");
        if (File.Exists(beside)) return beside;

        var cwd = Path.Combine(Directory.GetCurrentDirectory(), "Visa2026.DataImporter", "legacy", "visa2014", "reference", "turkmenistan-geography.db");
        return File.Exists(cwd) ? cwd : null;
    }

    /// <summary>
    /// Policy (b): if legacy region matches reference for the city, keep it;
    /// if it conflicts, return the reference region name_tm; if unknown city, return false.
    /// </summary>
    public bool TryResolvePreferredRegionNameTm(string? cityNameTm, string? legacyRegionNameTm, out string preferredRegionNameTm)
    {
        preferredRegionNameTm = legacyRegionNameTm ?? "";
        if (string.IsNullOrWhiteSpace(cityNameTm))
            return false;

        var cityKey = Visa2014CatalogMatchHelper.NormalizeKey(cityNameTm);
        using var cmd = _conn.CreateCommand();
        cmd.CommandText = """
            SELECT c.region_code, r.name_tm
            FROM city c
            INNER JOIN region r ON r.code = c.region_code
            WHERE c.name_key = $k
            UNION
            SELECT c.region_code, r.name_tm
            FROM city_alias a
            INNER JOIN city c ON c.id = a.city_id
            INNER JOIN region r ON r.code = c.region_code
            WHERE a.alias_key = $k;
            """;
        cmd.Parameters.AddWithValue("$k", cityKey);

        string? refRegionName = null;
        string? refRegionCode = null;
        using (var reader = cmd.ExecuteReader())
        {
            while (reader.Read())
            {
                refRegionCode = reader.GetString(0);
                refRegionName = reader.GetString(1);
                // First hit is enough when overrides made the city unique.
                break;
            }
        }

        if (string.IsNullOrWhiteSpace(refRegionName) || string.IsNullOrWhiteSpace(refRegionCode))
            return false;

        if (!string.IsNullOrWhiteSpace(legacyRegionNameTm) &&
            RegionMatches(legacyRegionNameTm, refRegionName, refRegionCode))
        {
            preferredRegionNameTm = legacyRegionNameTm;
            return true;
        }

        preferredRegionNameTm = refRegionName;
        return true;
    }

    private bool RegionMatches(string legacyRegionNameTm, string refRegionNameTm, string refRegionCode)
    {
        if (Visa2014CatalogMatchHelper.KeysEqual(legacyRegionNameTm, refRegionNameTm))
            return true;

        using var cmd = _conn.CreateCommand();
        cmd.CommandText = "SELECT code, name_key FROM region;";
        using var reader = cmd.ExecuteReader();
        var legacyKey = Visa2014CatalogMatchHelper.NormalizeKey(legacyRegionNameTm);
        while (reader.Read())
        {
            var code = reader.GetString(0);
            var nameKey = reader.GetString(1);
            if (code == refRegionCode && (legacyKey == nameKey || legacyKey.Contains(nameKey) || nameKey.Contains(legacyKey)))
                return true;
            if (Visa2014CatalogMatchHelper.KeysEqual(legacyRegionNameTm, code))
                return code == refRegionCode;
        }
        return false;
    }

    public void Dispose()
    {
        _conn.Dispose();
        if (ReferenceEquals(_cached, this))
        {
            _cached = null;
            _cachedPath = null;
        }
    }
}