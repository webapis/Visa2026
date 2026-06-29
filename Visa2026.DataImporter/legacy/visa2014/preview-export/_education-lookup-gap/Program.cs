using System.Globalization;
using System.Text;
using System.Text.Json;
using Microsoft.Data.SqlClient;

internal static class EduGapProgram
{
    private const string Conn = "Server=localhost\\SQLEXPRESS;Database=VISA2015;User Id=ReadOnlyUser;Password=159357;TrustServerCertificate=True";

    public static async Task Main()
    {
        var repo = @"c:\Users\webap\Documents\GitHub\Visa2026";
        var instPath = Path.Combine(repo, "Visa2026.Module", "DatabaseUpdate", "LookupCatalogs", "tenant", "education-institution.json");
        var specPath = Path.Combine(repo, "Visa2026.Module", "DatabaseUpdate", "LookupCatalogs", "tenant", "specialty.json");
        var outDir = Path.Combine(repo, "Visa2026.DataImporter", "legacy", "visa2014", "preview-export", "_education-lookup-gap");

        const string instSql = """
            SELECT LTRIM(RTRIM(ei.TitleOfIEducationInstitution)), COUNT(*)
            FROM dbo.Education e INNER JOIN dbo.EducationInstitution ei ON e.EducationInstitution = ei.Oid
            WHERE e.GCRecord IS NULL AND ei.TitleOfIEducationInstitution IS NOT NULL
            GROUP BY LTRIM(RTRIM(ei.TitleOfIEducationInstitution))
            """;

        const string specSql = """
            SELECT LTRIM(RTRIM(s.TitleOfSpeciality)), COUNT(*)
            FROM dbo.Education e INNER JOIN dbo.Speciality s ON e.Spcialty = s.Oid
            WHERE e.GCRecord IS NULL AND s.TitleOfSpeciality IS NOT NULL
            GROUP BY LTRIM(RTRIM(s.TitleOfSpeciality))
            """;

        var legacyInst = await LoadLegacyAsync(instSql);
        var legacySpec = await LoadLegacyAsync(specSql);
        var catInst = LoadCatalog(instPath);
        var catSpec = LoadCatalog(specPath);

        var inst = Analyze(legacyInst, catInst);
        var spec = Analyze(legacySpec, catSpec);

        var report = new
        {
            comparedAt = "2026-06-26",
            institution = new
            {
                catalogRows = catInst.Count,
                catalogDuplicateNormKeys = inst.CatalogDupes,
                legacyDistinct = legacyInst.Count,
                legacyEducationRows = legacyInst.Sum(x => x.Rows),
                mappedEducationRows = inst.MappedRows,
                mappedDistinct = inst.MappedDistinct,
                unmappedDistinct = inst.UnmappedDistinct,
                unmappedEducationRows = inst.UnmappedRows,
                aliasCount = inst.Gaps.Count(g => g.Match == "alias"),
                allGaps = inst.Gaps,
            },
            specialty = new
            {
                catalogRows = catSpec.Count,
                catalogDuplicateNormKeys = spec.CatalogDupes,
                legacyDistinct = legacySpec.Count,
                legacyEducationRows = legacySpec.Sum(x => x.Rows),
                mappedEducationRows = spec.MappedRows,
                mappedDistinct = spec.MappedDistinct,
                unmappedDistinct = spec.UnmappedDistinct,
                unmappedEducationRows = spec.UnmappedRows,
                aliasCount = spec.Gaps.Count(g => g.Match == "alias"),
                allGaps = spec.Gaps,
            },
        };

        var jsonPath = Path.Combine(outDir, "analysis.json");
        await File.WriteAllTextAsync(jsonPath, JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));

        Console.WriteLine($"Institution: {report.institution.mappedEducationRows}/{report.institution.legacyEducationRows} rows mapped, {report.institution.unmappedDistinct} unmapped labels ({report.institution.unmappedEducationRows} rows), {report.institution.aliasCount} aliases");
        Console.WriteLine($"Specialty:   {report.specialty.mappedEducationRows}/{report.specialty.legacyEducationRows} rows mapped, {report.specialty.unmappedDistinct} unmapped labels ({report.specialty.unmappedEducationRows} rows), {report.specialty.aliasCount} aliases");
        Console.WriteLine($"Wrote {jsonPath}");
    }

    private static async Task<List<LegacyLabel>> LoadLegacyAsync(string sql)
    {
        var list = new List<LegacyLabel>();
        await using var c = new SqlConnection(Conn);
        await c.OpenAsync();
        await using var cmd = new SqlCommand(sql, c);
        await using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync())
        {
            var label = r.GetString(0);
            var rows = r.GetInt32(1);
            list.Add(new LegacyLabel(label, rows, NormalizeKey(label)));
        }
        return list;
    }

    private static List<CatalogRow> LoadCatalog(string path)
    {
        var json = File.ReadAllText(path);
        using var doc = JsonDocument.Parse(json);
        var list = new List<CatalogRow>();
        foreach (var row in doc.RootElement.GetProperty("rows").EnumerateArray())
        {
            var name = row.GetProperty("NameTm").GetString() ?? "";
            list.Add(new CatalogRow(name, NormalizeKey(name)));
        }
        return list;
    }

    private static AnalysisResult Analyze(List<LegacyLabel> legacy, List<CatalogRow> catalog)
    {
        var catByNorm = new Dictionary<string, string>(StringComparer.Ordinal);
        var dupeNorms = 0;
        foreach (var c in catalog)
        {
            if (catByNorm.ContainsKey(c.Norm)) dupeNorms++;
            else catByNorm[c.Norm] = c.NameTm;
        }

        int mappedRows = 0, mappedDistinct = 0, unmappedRows = 0, unmappedDistinct = 0;
        var gaps = new List<GapRow>();
        foreach (var l in legacy.OrderByDescending(x => x.Rows))
        {
            if (catByNorm.TryGetValue(l.Norm, out var target))
            {
                mappedRows += l.Rows;
                mappedDistinct++;
                if (!string.Equals(l.Label, target, StringComparison.Ordinal))
                    gaps.Add(new GapRow(l.Label, l.Rows, target, "alias"));
            }
            else
            {
                unmappedRows += l.Rows;
                unmappedDistinct++;
                gaps.Add(new GapRow(l.Label, l.Rows, null, "unmapped"));
            }
        }

        return new AnalysisResult(mappedRows, mappedDistinct, unmappedDistinct, unmappedRows, gaps, dupeNorms);
    }

    private static string NormalizeKey(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        var folded = new StringBuilder();
        foreach (var ch in value.Trim())
            folded.Append(FoldTurkmenChar(ch));
        var decomposed = folded.ToString().Normalize(NormalizationForm.FormD);
        var buffer = new StringBuilder();
        foreach (var ch in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark)
                continue;
            buffer.Append(char.ToLowerInvariant(ch));
        }
        return buffer.ToString();
    }

    private static string FoldTurkmenChar(char ch) => ch switch
    {
        '\u00C4' or '\u00E4' => "a",
        '\u00C7' or '\u00E7' => "c",
        '\u017D' or '\u017E' => "z",
        '\u0147' or '\u0148' => "n",
        '\u00D6' or '\u00F6' => "o",
        '\u015E' or '\u015F' => "s",
        '\u00DC' or '\u00FC' => "u",
        '\u00DD' or '\u00FD' => "y",
        _ => ch.ToString(),
    };

    private sealed record LegacyLabel(string Label, int Rows, string Norm);
    private sealed record CatalogRow(string NameTm, string Norm);
    private sealed record GapRow(string LegacyLabel, int EducationRows, string? TargetNameTm, string Match);
    private sealed record AnalysisResult(int MappedRows, int MappedDistinct, int UnmappedDistinct, int UnmappedRows, List<GapRow> Gaps, int CatalogDupes);
}
