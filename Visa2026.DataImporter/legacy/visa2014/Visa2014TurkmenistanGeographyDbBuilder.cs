using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace Visa2026.DataImporter.Legacy.Visa2014;

/// <summary>
/// Builds turkmenistan-geography.db from Module region.json + city.json + geography-overrides.json.
/// </summary>
internal static class Visa2014TurkmenistanGeographyDbBuilder
{
    public static string Rebuild(string? outputPath = null, bool verbose = false)
    {
        var dataImporterRoot = Visa2014ContentRoot.FindDataImporterRoot()
            ?? throw new InvalidOperationException("Could not locate Visa2026.DataImporter content root.");
        var solutionRoot = Visa2014ContentRoot.FindSolutionRoot()
            ?? throw new InvalidOperationException("Could not locate solution root.");

        var regionJson = Path.Combine(solutionRoot, "Visa2026.Module", "DatabaseUpdate", "LookupCatalogs", "region.json");
        var cityJson = Path.Combine(solutionRoot, "Visa2026.Module", "DatabaseUpdate", "LookupCatalogs", "city.json");

        // Prefer source-tree reference folder (not bin/publish copy) so rebuild updates the tracked DB.
        var sourceReferenceDir = Path.Combine(solutionRoot, "Visa2026.DataImporter", "legacy", "visa2014", "reference");
        var overridesJson = File.Exists(Path.Combine(sourceReferenceDir, "geography-overrides.json"))
            ? Path.Combine(sourceReferenceDir, "geography-overrides.json")
            : Path.Combine(dataImporterRoot, "legacy", "visa2014", "reference", "geography-overrides.json");
        var dbPath = outputPath
            ?? Path.Combine(sourceReferenceDir, "turkmenistan-geography.db");

        if (!File.Exists(regionJson)) throw new FileNotFoundException("region.json not found.", regionJson);
        if (!File.Exists(cityJson)) throw new FileNotFoundException("city.json not found.", cityJson);

        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(dbPath))!);
        if (File.Exists(dbPath))
            File.Delete(dbPath);

        SQLitePCL.Batteries_V2.Init();

        using var conn = new SqliteConnection($"Data Source={dbPath}");
        conn.Open();
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = """
                PRAGMA foreign_keys = ON;
                CREATE TABLE region (
                  code TEXT PRIMARY KEY NOT NULL,
                  name_tm TEXT NOT NULL,
                  name_key TEXT NOT NULL,
                  source TEXT NOT NULL DEFAULT 'region.json'
                );
                CREATE TABLE city (
                  id INTEGER PRIMARY KEY AUTOINCREMENT,
                  name_tm TEXT NOT NULL,
                  name_key TEXT NOT NULL,
                  region_code TEXT NOT NULL REFERENCES region(code),
                  status TEXT NOT NULL DEFAULT 'current',
                  notes TEXT,
                  source TEXT NOT NULL DEFAULT 'city.json',
                  UNIQUE(name_key, region_code)
                );
                CREATE TABLE city_alias (
                  id INTEGER PRIMARY KEY AUTOINCREMENT,
                  city_id INTEGER NOT NULL REFERENCES city(id) ON DELETE CASCADE,
                  alias TEXT NOT NULL,
                  alias_key TEXT NOT NULL,
                  UNIQUE(alias_key, city_id)
                );
                CREATE INDEX ix_city_name_key ON city(name_key);
                CREATE INDEX ix_alias_key ON city_alias(alias_key);
                """;
            cmd.ExecuteNonQuery();
        }

        var regions = LoadRows(regionJson);
        var regionCodeByNameKey = new Dictionary<string, string>(StringComparer.Ordinal);
        using (var tx = conn.BeginTransaction())
        {
            foreach (var row in regions)
            {
                var nameTm = GetString(row, "NameTm");
                var code = GetString(row, "LocalizationKey") ?? GetString(row, "PdfForm_Code");
                if (string.IsNullOrWhiteSpace(nameTm) || string.IsNullOrWhiteSpace(code))
                    continue;

                var nameKey = Visa2014CatalogMatchHelper.NormalizeKey(nameTm);
                using var insert = conn.CreateCommand();
                insert.Transaction = tx;
                insert.CommandText = "INSERT INTO region(code, name_tm, name_key, source) VALUES ($c, $n, $k, 'region.json');";
                insert.Parameters.AddWithValue("$c", code);
                insert.Parameters.AddWithValue("$n", nameTm);
                insert.Parameters.AddWithValue("$k", nameKey);
                insert.ExecuteNonQuery();
                regionCodeByNameKey[nameKey] = code;
            }
            tx.Commit();
        }

        var cities = LoadRows(cityJson);
        var cityIdByNameKey = new Dictionary<string, long>(StringComparer.Ordinal);
        using (var tx = conn.BeginTransaction())
        {
            foreach (var row in cities)
            {
                var nameTm = GetString(row, "NameTm");
                var regionName = GetString(row, "Region") ?? GetString(row, "RegionName");
                if (string.IsNullOrWhiteSpace(nameTm) || string.IsNullOrWhiteSpace(regionName))
                    continue;

                var regionKey = Visa2014CatalogMatchHelper.NormalizeKey(regionName);
                if (!regionCodeByNameKey.TryGetValue(regionKey, out var regionCode))
                {
                    if (verbose)
                        Console.WriteLine($"WRN city '{nameTm}': unknown region '{regionName}'");
                    continue;
                }

                var nameKey = Visa2014CatalogMatchHelper.NormalizeKey(nameTm);
                using var insert = conn.CreateCommand();
                insert.Transaction = tx;
                insert.CommandText = """
                    INSERT INTO city(name_tm, name_key, region_code, status, source)
                    VALUES ($n, $k, $r, 'current', 'city.json')
                    ON CONFLICT(name_key, region_code) DO UPDATE SET name_tm = excluded.name_tm;
                    """;
                insert.Parameters.AddWithValue("$n", nameTm);
                insert.Parameters.AddWithValue("$k", nameKey);
                insert.Parameters.AddWithValue("$r", regionCode);
                insert.ExecuteNonQuery();

                using var idCmd = conn.CreateCommand();
                idCmd.Transaction = tx;
                idCmd.CommandText = "SELECT id FROM city WHERE name_key = $k AND region_code = $r;";
                idCmd.Parameters.AddWithValue("$k", nameKey);
                idCmd.Parameters.AddWithValue("$r", regionCode);
                cityIdByNameKey[nameKey] = (long)idCmd.ExecuteScalar()!;
            }
            tx.Commit();
        }

        if (File.Exists(overridesJson))
            ApplyOverrides(conn, overridesJson, regionCodeByNameKey, cityIdByNameKey, verbose);

        int regionCount, cityCount, aliasCount;
        using (var c = conn.CreateCommand())
        {
            c.CommandText = "SELECT COUNT(*) FROM region;";
            regionCount = Convert.ToInt32(c.ExecuteScalar());
            c.CommandText = "SELECT COUNT(*) FROM city;";
            cityCount = Convert.ToInt32(c.ExecuteScalar());
            c.CommandText = "SELECT COUNT(*) FROM city_alias;";
            aliasCount = Convert.ToInt32(c.ExecuteScalar());
        }

        Console.WriteLine($"OK Geography DB: {regionCount} region(s), {cityCount} city(ies), {aliasCount} alias(es) → {Path.GetFullPath(dbPath)}");
        return Path.GetFullPath(dbPath);
    }

    private static void ApplyOverrides(
        SqliteConnection conn,
        string overridesJson,
        Dictionary<string, string> regionCodeByNameKey,
        Dictionary<string, long> cityIdByNameKey,
        bool verbose)
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(overridesJson));
        if (!doc.RootElement.TryGetProperty("cities", out var cities) || cities.ValueKind != JsonValueKind.Array)
            return;

        using var tx = conn.BeginTransaction();
        foreach (var row in cities.EnumerateArray())
        {
            var nameTm = row.TryGetProperty("nameTm", out var n) ? n.GetString() : null;
            var regionCode = row.TryGetProperty("regionCode", out var rc) ? rc.GetString() : null;
            if (string.IsNullOrWhiteSpace(nameTm) || string.IsNullOrWhiteSpace(regionCode))
                continue;

            var status = row.TryGetProperty("status", out var st) ? st.GetString() ?? "current" : "current";
            var notes = row.TryGetProperty("notes", out var nt) ? nt.GetString() : null;
            var source = row.TryGetProperty("source", out var src) ? src.GetString() ?? "override" : "override";
            var nameKey = Visa2014CatalogMatchHelper.NormalizeKey(nameTm);

            // Remove other-region rows for this name so the override is authoritative.
            using (var del = conn.CreateCommand())
            {
                del.Transaction = tx;
                del.CommandText = "DELETE FROM city WHERE name_key = $k AND region_code <> $r;";
                del.Parameters.AddWithValue("$k", nameKey);
                del.Parameters.AddWithValue("$r", regionCode);
                del.ExecuteNonQuery();
            }

            using (var upsert = conn.CreateCommand())
            {
                upsert.Transaction = tx;
                upsert.CommandText = """
                    INSERT INTO city(name_tm, name_key, region_code, status, notes, source)
                    VALUES ($n, $k, $r, $s, $notes, $src)
                    ON CONFLICT(name_key, region_code) DO UPDATE SET
                      name_tm = excluded.name_tm,
                      status = excluded.status,
                      notes = excluded.notes,
                      source = excluded.source;
                    """;
                upsert.Parameters.AddWithValue("$n", nameTm);
                upsert.Parameters.AddWithValue("$k", nameKey);
                upsert.Parameters.AddWithValue("$r", regionCode);
                upsert.Parameters.AddWithValue("$s", status);
                upsert.Parameters.AddWithValue("$notes", (object?)notes ?? DBNull.Value);
                upsert.Parameters.AddWithValue("$src", source);
                upsert.ExecuteNonQuery();
            }

            long cityId;
            using (var idCmd = conn.CreateCommand())
            {
                idCmd.Transaction = tx;
                idCmd.CommandText = "SELECT id FROM city WHERE name_key = $k AND region_code = $r;";
                idCmd.Parameters.AddWithValue("$k", nameKey);
                idCmd.Parameters.AddWithValue("$r", regionCode);
                cityId = (long)idCmd.ExecuteScalar()!;
            }
            cityIdByNameKey[nameKey] = cityId;

            if (row.TryGetProperty("aliases", out var aliases) && aliases.ValueKind == JsonValueKind.Array)
            {
                foreach (var aliasEl in aliases.EnumerateArray())
                {
                    var alias = aliasEl.GetString();
                    if (string.IsNullOrWhiteSpace(alias))
                        continue;
                    using var a = conn.CreateCommand();
                    a.Transaction = tx;
                    a.CommandText = """
                        INSERT INTO city_alias(city_id, alias, alias_key)
                        VALUES ($id, $a, $k)
                        ON CONFLICT(alias_key, city_id) DO NOTHING;
                        """;
                    a.Parameters.AddWithValue("$id", cityId);
                    a.Parameters.AddWithValue("$a", alias);
                    a.Parameters.AddWithValue("$k", Visa2014CatalogMatchHelper.NormalizeKey(alias));
                    a.ExecuteNonQuery();
                }
            }

            if (verbose)
                Console.WriteLine($"INF override: {nameTm} → {regionCode} ({status})");
        }
        tx.Commit();
        _ = regionCodeByNameKey;
    }

    private static List<Dictionary<string, JsonElement>> LoadRows(string path)
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        if (!doc.RootElement.TryGetProperty("rows", out var rows) || rows.ValueKind != JsonValueKind.Array)
            return [];

        var list = new List<Dictionary<string, JsonElement>>();
        foreach (var row in rows.EnumerateArray())
        {
            var dict = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
            foreach (var prop in row.EnumerateObject())
                dict[prop.Name] = prop.Value.Clone();
            list.Add(dict);
        }
        return list;
    }

    private static string? GetString(Dictionary<string, JsonElement> row, string key)
    {
        if (!row.TryGetValue(key, out var el) || el.ValueKind != JsonValueKind.String)
            return null;
        return el.GetString();
    }
}