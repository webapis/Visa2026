#nullable enable

using ClosedXML.Excel;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.TemplateConvert;
using Visa2026.Module.Services.TemplateScan;
using Visa2026.Module.Services.UserReports;
using Xunit;

namespace Visa2026.Module.Tests.TemplateScan;

public class ScanYellowValueHintResolverTests
{
    private static ApplicationProfilePlaceholderSet PlaceholderSet() =>
        new ApplicationProfilePlaceholderSetService(new UserReportPlaceholderCatalogService()).GetSet(
            new ApplicationProfilePlaceholderSetQuery
            {
                Profile = new ApplicationProfile(),
                DataScope = ApplicationProfileTemplateDataScope.Both,
                TemplateKind = ApplicationProfileTemplateKind.Excel,
            });

    private static ValueCandidate Candidate(string shortCode, string raw, bool row = true, int? rowIndex = 0)
    {
        var kind = TemplateValueMatchKeys.Classify(shortCode, raw);
        var keys = TemplateValueMatchKeys.Build(raw, kind);
        var token = row ? $"{{{{.{shortCode}}}}}" : $"{{{{ds.{shortCode}}}}}";
        return new ValueCandidate(shortCode, token, raw, keys[0], kind, rowIndex, keys);
    }

    [Fact]
    public void Resolve_maps_roster_literals_from_case_values()
    {
        var set = PlaceholderSet();
        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var candidates = new[]
        {
            Candidate("PFNM", "Erol"),
            Candidate("PLN", "Hilmi"),
            Candidate("PGND", "Erkek"),
            Candidate("PNAT", "TUR"),
            Candidate("PDBT", "16.05.1980"),
        };

        Assert.Single(ScanYellowValueHintResolver.Resolve("Erol", 0, set, candidates, used));
        Assert.Single(ScanYellowValueHintResolver.Resolve("Hilmi", 0, set, candidates, used));
        Assert.Single(ScanYellowValueHintResolver.Resolve("Erkek", 0, set, candidates, used));
        Assert.Single(ScanYellowValueHintResolver.Resolve("TUR", 0, set, candidates, used));

        var dob = ScanYellowValueHintResolver.Resolve("16.05.1980", 0, set, candidates, used).Single();
        Assert.Contains("PDBT", dob.ProposedToken!, StringComparison.OrdinalIgnoreCase);
        Assert.StartsWith("{{.", dob.ProposedToken!, StringComparison.Ordinal);
    }

    [Fact]
    public void Resolve_disambiguates_ambiguous_country_code_to_PNAT()
    {
        var set = PlaceholderSet();
        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var candidates = new[]
        {
            Candidate("PNAT", "TUR"),
            Candidate("PCBC", "TUR"),
            Candidate("PFAC", "TUR"),
        };

        var mapped = ScanYellowValueHintResolver.Resolve("TUR", 0, set, candidates, used).Single();
        Assert.Contains("PNAT", mapped.ProposedToken!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Resolve_prefers_row_dob_over_application_date_when_both_match()
    {
        var set = PlaceholderSet();
        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var candidates = new[]
        {
            new ValueCandidate("ADAT", "{{ds.ADAT}}", "16.05.1980", "16.05.1980", ValueKind.Date, null, ["16.05.1980"]),
            Candidate("PDBT", "16.05.1980"),
        };

        var mapped = ScanYellowValueHintResolver.Resolve("16.05.1980", 0, set, candidates, used).Single();
        Assert.Contains("PDBT", mapped.ProposedToken!, StringComparison.OrdinalIgnoreCase);
        Assert.StartsWith("{{.", mapped.ProposedToken!, StringComparison.Ordinal);
    }

    [Fact]
    public void Resolve_matches_long_address_substring()
    {
        var set = PlaceholderSet();
        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var fullAddress = "TUR, Emek mahallesi 1234 sokak No:5 Garabogaz";
        var candidates = new[] { Candidate("PFAD", fullAddress) };

        var mapped = ScanYellowValueHintResolver.Resolve("Garabogaz", 0, set, candidates, used).Single();
        Assert.Contains("PFAD", mapped.ProposedToken!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Build_ExcelYellow_maps_person_fields_from_column_headers()
    {
        var set = PlaceholderSet();
        using var ms = new MemoryStream();
        using (var wb = new XLWorkbook())
        {
            var ws = wb.AddWorksheet("Sanaw");
            ws.Cell("A4").Value = "Familiýasy";
            ws.Cell("B4").Value = "Ady";
            ws.Cell("C4").Value = "Raýatlygy";
            ws.Cell("A5").Value = "Erol";
            ws.Cell("A5").Style.Fill.BackgroundColor = XLColor.Yellow;
            ws.Cell("B5").Value = "Hilmi";
            ws.Cell("B5").Style.Fill.BackgroundColor = XLColor.Yellow;
            ws.Cell("C5").Value = "TUR";
            ws.Cell("C5").Style.Fill.BackgroundColor = XLColor.Yellow;
            wb.SaveAs(ms);
        }

        var bytes = ms.ToArray();
        var yellows = new ScanOfficeYellowExtractor().Extract(bytes, ScanSourceKind.Excel);
        Assert.Equal(3, yellows.Count);

        var proposal = ScanOfficeFieldPlanBuilder.Build(yellows, set, bytes, ScanSourceKind.Excel);
        var plan = new ScanFieldPlanMerger().Merge(new ScanFieldPlanMergeRequest
        {
            PlaceholderSet = set,
            ScanKind = ScanKind.FilledSample,
            Proposal = proposal,
        });

        Assert.Equal(3, plan.Fields.Count(f => !string.IsNullOrWhiteSpace(f.ProposedToken)));
        Assert.Empty(plan.Gaps);
        Assert.Contains(plan.Fields, f => f.ProposedToken != null && f.ProposedToken.Contains("PLN", StringComparison.Ordinal));
        Assert.Contains(plan.Fields, f => f.ProposedToken != null && f.ProposedToken.Contains("PFNM", StringComparison.Ordinal));
        Assert.Contains(plan.Fields, f => f.ProposedToken != null && f.ProposedToken.Contains("PNAT", StringComparison.Ordinal));
    }

    [Fact]
    public void Build_ExcelYellow_merger_keeps_compound_dob_cell()
    {
        var set = PlaceholderSet();
        using var ms = new MemoryStream();
        using (var wb = new XLWorkbook())
        {
            var ws = wb.AddWorksheet("Sanaw");
            ws.Cell("D4").Value = "Doglan senesi we ýeri";
            ws.Cell("D5").Value = "16.05.1980, Türkiye/ Üsküdar";
            ws.Cell("D5").Style.Fill.BackgroundColor = XLColor.Yellow;
            wb.SaveAs(ms);
        }

        var bytes = ms.ToArray();
        var yellows = new ScanOfficeYellowExtractor().Extract(bytes, ScanSourceKind.Excel);

        var proposal = ScanOfficeFieldPlanBuilder.Build(yellows, set, bytes, ScanSourceKind.Excel);
        var plan = new ScanFieldPlanMerger().Merge(new ScanFieldPlanMergeRequest
        {
            PlaceholderSet = set,
            ScanKind = ScanKind.FilledSample,
            Proposal = proposal,
        });

        Assert.Single(plan.Fields);
        Assert.Contains("PDBT", plan.Fields[0].ProposedToken!, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(plan.Gaps);
    }
}