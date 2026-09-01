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

    [Fact]
    public void Resolve_maps_signatory_name_with_trailing_underscores()
    {
        var set = PlaceholderSet();
        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var candidates = new[] { Candidate("CHFN", "Mehmet ÇIRAK", row: false, rowIndex: null) };

        var mapped = ScanYellowValueHintResolver.Resolve(
            "Mehmet ÇIRAK ___",
            0,
            set,
            candidates,
            used,
            preferHeaderToken: true).Single();

        Assert.Equal("{{ds.CHFN}}", mapped.ProposedToken);
    }

    [Fact]
    public void Catalog_example_maps_signatory_name_when_case_spelling_differs()
    {
        var set = PlaceholderSet();
        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var examples = ScanYellowValueHintResolver.CatalogExampleCandidates(set);

        var mapped = ScanYellowValueHintResolver.Resolve(
            "Mehmet ÇIRAK ___",
            0,
            set,
            examples,
            used,
            preferHeaderToken: true).Single();

        Assert.Contains("CHFN", mapped.ProposedToken!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Build_WordYellow_maps_company_signatory_and_representative()
    {
        var set = PlaceholderSetWord();
        var bytes = ScanOfficeYellowExtractorTests.CreateWordFixture(
            "Mehmet ÇIRAK ___",
            "I-AŞ 476479 Aşgabat ş., Berkararlyk etr. Häkimliği tarapyndan berlen, +993 65 56-13-49_",
            "Nepesowa Tumar Aşyrowna",
            "Nepesowa Tumar Aşyrowna___",
            "__ Hilmi Erol 16.05.1980ý.");

        var yellows = new ScanOfficeYellowExtractor().Extract(bytes, ScanSourceKind.Word);
        var candidates = new[]
        {
            Candidate("PFN", "Nepesowa Tumar Aşyrowna"),
            Candidate("RPFN", "Nepesowa Tumar Aşyrowna", row: false, rowIndex: null),
            Candidate("RPFN", "Nejepowa Gurlar Aglyyowna", row: false, rowIndex: null),
        };

        var proposal = ScanOfficeFieldPlanBuilder.Build(
            yellows,
            set,
            bytes,
            ScanSourceKind.Word,
            candidates);
        var plan = new ScanFieldPlanMerger().Merge(new ScanFieldPlanMergeRequest
        {
            PlaceholderSet = set,
            ScanKind = ScanKind.FilledSample,
            Proposal = proposal,
        });

        var codes = new List<string>();
        foreach (var field in plan.Fields)
        {
            if (TemplateTokenSyntax.TryGetShortCode(field.ProposedToken, out var code))
                codes.Add(code);
        }

        Assert.True(
            codes.Exists(c => c.Equals("CHFN", StringComparison.OrdinalIgnoreCase)
                || c.Equals("ACFNM", StringComparison.OrdinalIgnoreCase)),
            "signatory tokens: " + string.Join(",", codes));
        Assert.Contains(codes, c => c.Equals("RPCL", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(codes, c => c.Equals("PFN", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(codes, c => c.Equals("RPFN", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(codes, c => c.Equals("PDBT", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(plan.Gaps, g => g.LabelText.Contains("Nepesowa", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(plan.Gaps, g => g.LabelText.Contains("I-AŞ", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Build_WordYellow_wekil_caption_maps_sample_name_to_RPFN()
    {
        var set = PlaceholderSetWord();
        var bytes = ScanOfficeYellowExtractorTests.CreateWordWithCaptionThenYellow(
            "we Kärhananyň wiza işleri boýunça ygtyýarly wekili:",
            "Nepesowa Tumar Aşyrowna");
        var yellows = new ScanOfficeYellowExtractor().Extract(bytes, ScanSourceKind.Word);
        var candidates = new[]
        {
            Candidate("PFN", "Nepesowa Tumar Aşyrowna"),
            Candidate("RPFN", "Nejepowa Gurlar Aglyyowna", row: false, rowIndex: null),
        };

        var proposal = ScanOfficeFieldPlanBuilder.Build(
            yellows,
            set,
            bytes,
            ScanSourceKind.Word,
            candidates);

        var mapped = Assert.Single(proposal.Fields);
        Assert.Contains("RPFN", mapped.ProposedToken!, StringComparison.OrdinalIgnoreCase);
        Assert.StartsWith("{{ds.", mapped.ProposedToken!, StringComparison.Ordinal);
        Assert.Equal(ScanFieldScope.Header, mapped.Scope);
        Assert.NotNull(mapped.SourceRegion);

        var plan = new ScanFieldPlanMerger().Merge(new ScanFieldPlanMergeRequest
        {
            PlaceholderSet = set,
            ScanKind = ScanKind.FilledSample,
            Proposal = proposal,
        });
        var kept = Assert.Single(plan.Fields);
        Assert.Contains("RPFN", kept.ProposedToken!, StringComparison.OrdinalIgnoreCase);
        Assert.NotNull(kept.SourceRegion);
    }

    [Fact]
    public void Resolve_prefers_roster_PFN_when_instance_repeats_person_as_wekil()
    {
        var set = PlaceholderSetWord();
        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var candidates = new[]
        {
            Candidate("PFN", "Nepesowa Tumar Aşyrowna"),
            Candidate("RPFN", "Nepesowa Tumar Aşyrowna", row: false, rowIndex: null),
        };

        var mapped = ScanYellowValueHintResolver.Resolve(
            "Nepesowa Tumar Aşyrowna",
            0,
            set,
            candidates,
            used,
            preferHeaderToken: true).Single();

        Assert.Contains("PFN", mapped.ProposedToken!, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("RPFN", mapped.ProposedToken!, StringComparison.OrdinalIgnoreCase);
        Assert.StartsWith("{{.", mapped.ProposedToken!, StringComparison.Ordinal);
    }

    [Fact]
    public void Resolve_keeps_RPFN_for_exact_wekil_name()
    {
        var set = PlaceholderSetWord();
        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var candidates = new[]
        {
            Candidate("PFN", "Nepesowa Tumar Aşyrowna"),
            Candidate("RPFN", "Nejepowa Gurlar Aglyyowna", row: false, rowIndex: null),
        };

        var mapped = ScanYellowValueHintResolver.Resolve(
            "Nejepowa Gurlar Aglyyowna",
            0,
            set,
            candidates,
            used,
            preferHeaderToken: true).Single();

        Assert.Contains("RPFN", mapped.ProposedToken!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RewriteDraft_person_shaped_RPFN_becomes_PFN_when_not_wekil()
    {
        var set = PlaceholderSetWord();
        var draft = new ScanDetectedFieldDraft
        {
            FieldId = "n1",
            Box = ScanBoundingBox.FullPage,
            PageIndex = 0,
            LabelText = "Nepesowa Tumar Aşyrowna",
            ProposedToken = "{{ds.RPFN}}",
            Confidence = ScanFieldConfidence.High,
            Scope = ScanFieldScope.Header,
        };

        var rewritten = ScanRepresentativeNameGuard.RewriteDraft(
            draft,
            set,
            [Candidate("PFN", "Nepesowa Tumar Aşyrowna")]);

        Assert.Equal("{{.PFN}}", rewritten.ProposedToken);
        Assert.Equal(ScanFieldScope.Row, rewritten.Scope);
    }

    [Fact]
    public void RewriteDraft_catalog_wekil_mismatch_rewrites_even_when_instance_RPFN_matches()
    {
        var set = PlaceholderSetWord();
        var draft = new ScanDetectedFieldDraft
        {
            FieldId = "n2",
            Box = ScanBoundingBox.FullPage,
            PageIndex = 0,
            LabelText = "Nepesowa Tumar Aşyrowna",
            ProposedToken = "{{ds.RPFN}}",
            Confidence = ScanFieldConfidence.High,
            Scope = ScanFieldScope.Header,
        };

        var rewritten = ScanRepresentativeNameGuard.RewriteDraft(
            draft,
            set,
            [Candidate("RPFN", "Nepesowa Tumar Aşyrowna", row: false, rowIndex: null)]);

        Assert.Equal("{{.PFN}}", rewritten.ProposedToken);
    }

    [Fact]
    public void RewriteDraft_wekil_caption_forces_RPFN_even_when_sample_is_roster_PFN()
    {
        var set = PlaceholderSetWord();
        var draft = new ScanDetectedFieldDraft
        {
            FieldId = "w1",
            Box = ScanBoundingBox.FullPage,
            PageIndex = 0,
            LabelText = "Nepesowa Tumar Aşyrowna",
            ProposedToken = "{{.PFN}}",
            NearbyLabel = "we Kärhananyň wiza işleri boýunça ygtyýarly wekili",
            Confidence = ScanFieldConfidence.High,
            Scope = ScanFieldScope.Row,
        };

        var rewritten = ScanRepresentativeNameGuard.RewriteDraft(
            draft,
            set,
            [Candidate("PFN", "Nepesowa Tumar Aşyrowna")]);

        Assert.Equal("{{ds.RPFN}}", rewritten.ProposedToken);
        Assert.Equal(ScanFieldScope.Header, rewritten.Scope);
        Assert.Equal(draft.NearbyLabel, rewritten.NearbyLabel);
    }

    [Fact]
    public void RewriteDraft_wekil_caption_does_not_overwrite_RPCL()
    {
        var set = PlaceholderSetWord();
        var draft = new ScanDetectedFieldDraft
        {
            FieldId = "w2",
            Box = ScanBoundingBox.FullPage,
            PageIndex = 0,
            LabelText = "I-AŞ 476479 Aşgabat ş., +993 65 56-13-49",
            ProposedToken = "{{ds.RPCL}}",
            NearbyLabel = "ygtyýarly wekili",
            Confidence = ScanFieldConfidence.High,
            Scope = ScanFieldScope.Header,
        };

        var rewritten = ScanRepresentativeNameGuard.RewriteDraft(draft, set, []);

        Assert.Equal("{{ds.RPCL}}", rewritten.ProposedToken);
    }

    private static ApplicationProfilePlaceholderSet PlaceholderSetWord() =>
        new ApplicationProfilePlaceholderSetService(new UserReportPlaceholderCatalogService()).GetSet(
            new ApplicationProfilePlaceholderSetQuery
            {
                Profile = new ApplicationProfile(),
                DataScope = ApplicationProfileTemplateDataScope.PeopleM2M,
                TemplateKind = ApplicationProfileTemplateKind.Word,
            });
}