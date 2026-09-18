#nullable enable

using ClosedXML.Excel;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.TemplateConvert;
using Visa2026.Module.Services.TemplateScan;
using Visa2026.Module.Services.UserReports;
using Xunit;

namespace Visa2026.Module.Tests.TemplateScan;

public class ScanExcelYellowResolverTests
{
    private static ApplicationProfilePlaceholderSet PlaceholderSet() =>
        new ApplicationProfilePlaceholderSetService(new UserReportPlaceholderCatalogService()).GetSet(
            new ApplicationProfilePlaceholderSetQuery
            {
                Profile = new ApplicationProfile
                {
                    RequirePersonPassport = true,
                    RequirePersonEducation = true,
                    RequirePersonPosition = true,
                    RequirePersonAddressOfResidence = true,
                    RequirePersonWorkPermitItem = true,
                },
                DataScope = ApplicationProfileTemplateDataScope.Both,
                TemplateKind = ApplicationProfileTemplateKind.Excel,
            });

    [Fact]
    public void Resolve_maps_sample_names_from_column_headers_not_case_values()
    {
        var set = PlaceholderSet();
        using var ms = BuildSanawStyleWorkbook();
        var bytes = ms.ToArray();
        var yellows = new ScanOfficeYellowExtractor().Extract(bytes, ScanSourceKind.Excel);

        var fields = ScanExcelYellowResolver.Resolve(bytes, yellows, set);
        var mapped = fields.Where(f => !string.IsNullOrWhiteSpace(f.ProposedToken)).ToList();

        Assert.True(mapped.Count >= 8, $"Expected most roster columns mapped, got {mapped.Count}");
        Assert.Contains(mapped, f => f.ProposedToken!.Contains("PLN", StringComparison.Ordinal));
        Assert.Contains(mapped, f => f.ProposedToken!.Contains("PFNM", StringComparison.Ordinal));
        Assert.Contains(mapped, f => f.LabelText == "Erol");
        Assert.Contains(mapped, f => f.LabelText == "Hilmi");
        Assert.All(mapped, f => Assert.NotEmpty(f.Alternatives));
        Assert.Contains(mapped, f => f.LabelText == "Erol" && f.ProposedToken!.Contains("{{.PLN}}", StringComparison.Ordinal));
    }

    [Fact]
    public void Resolve_yellow_on_row_4_under_headers_uses_row_tokens_not_ds()
    {
        var set = PlaceholderSet();
        using var ms = new MemoryStream();
        using (var wb = new XLWorkbook())
        {
            var ws = wb.AddWorksheet("Sanaw");
            ws.Cell("A1").Value = "Daşary ýurt raýatlarynyň sanawy";
            ws.Cell("A3").Value = "№";
            ws.Cell("B3").Value = "Familiýasy";
            ws.Cell("C3").Value = "Ady";
            ws.Cell("A4").Value = "1";
            ws.Cell("A4").Style.Fill.BackgroundColor = XLColor.Yellow;
            ws.Cell("B4").Value = "Erol";
            ws.Cell("B4").Style.Fill.BackgroundColor = XLColor.Yellow;
            ws.Cell("C4").Value = "Hilmi";
            ws.Cell("C4").Style.Fill.BackgroundColor = XLColor.Yellow;
            wb.SaveAs(ms);
        }

        var bytes = ms.ToArray();
        var yellows = new ScanOfficeYellowExtractor().Extract(bytes, ScanSourceKind.Excel);
        var fields = ScanExcelYellowResolver.Resolve(bytes, yellows, set);

        var erol = Assert.Single(fields, f => f.LabelText == "Erol");
        Assert.Equal(ScanFieldScope.Row, erol.Scope);
        Assert.Equal("{{.PLN}}", erol.ProposedToken);
        Assert.DoesNotContain("ds.PLN", erol.ProposedToken, StringComparison.Ordinal);
        Assert.Contains(fields, f => f.LabelText == "1" && f.ProposedToken!.Contains("{{.RNUM}}", StringComparison.Ordinal));
    }

    [Fact]
    public void Resolve_maps_all_comma_birth_cell_to_three_tokens()
    {
        var set = PlaceholderSet();
        using var ms = new MemoryStream();
        using (var wb = new XLWorkbook())
        {
            var ws = wb.AddWorksheet("Sanaw");
            ws.Cell("D4").Value = "Doglan senesi we ýeri";
            ws.Cell("D5").Value = "05.04.1989, TUR, Fatih";
            ws.Cell("D5").Style.Fill.BackgroundColor = XLColor.Yellow;
            wb.SaveAs(ms);
        }

        var bytes = ms.ToArray();
        var yellows = new ScanOfficeYellowExtractor().Extract(bytes, ScanSourceKind.Excel);
        var fields = ScanExcelYellowResolver.Resolve(bytes, yellows, set);
        var cell = Assert.Single(fields);
        Assert.Equal("05.04.1989, TUR, Fatih", cell.LabelText);
        var codes = TemplateTokenSyntax.GetShortCodes(cell.ProposedToken);
        Assert.Equal(["PDBT", "PCBT", "PBPL"], codes);

        var ordered = ScanReviewFieldOrder.Order([
            new ScanDetectedField
            {
                FieldId = cell.FieldId,
                Box = ScanBoundingBox.FullPage,
                PageIndex = 0,
                LabelText = cell.LabelText,
                ProposedToken = cell.ProposedToken,
                Confidence = cell.Confidence,
                Scope = cell.Scope,
                SourceRegion = cell.SourceRegion,
                Alternatives = cell.Alternatives,
            },
        ]);
        Assert.Equal(["1.1", "1.2", "1.3"], ordered.Select(o => o.DisplayOrder).ToArray());
        Assert.Equal(["05.04.1989", "TUR", "Fatih"], ordered.Select(o => o.LabelText).ToArray());
        Assert.Equal(
            ["PDBT", "PCBT", "PBPL"],
            ordered.Select(o => TemplateTokenSyntax.GetShortCodes(o.ProposedToken).Single()).ToArray());
    }

    [Fact]
    public void Resolve_splits_compound_birth_place_cell()
    {
        var set = PlaceholderSet();
        using var ms = BuildSanawStyleWorkbook();
        var bytes = ms.ToArray();
        var yellows = new ScanOfficeYellowExtractor().Extract(bytes, ScanSourceKind.Excel);

        var fields = ScanExcelYellowResolver.Resolve(bytes, yellows, set);
        var dobCell = fields.FirstOrDefault(f => f.LabelText.Contains("16.05.1980", StringComparison.Ordinal));
        Assert.NotNull(dobCell);
        Assert.Contains("PDBT", dobCell!.ProposedToken!, StringComparison.Ordinal);
        Assert.Contains("PCBT", dobCell.ProposedToken!, StringComparison.Ordinal);
        Assert.Contains("PBPL", dobCell.ProposedToken!, StringComparison.Ordinal);
        Assert.Contains("{{", dobCell.ProposedToken!, StringComparison.Ordinal);
    }

    [Fact]
    public void Build_endToEnd_merger_keeps_compound_cell_template()
    {
        var set = PlaceholderSet();
        using var ms = BuildSanawStyleWorkbook();
        var bytes = ms.ToArray();
        var yellows = new ScanOfficeYellowExtractor().Extract(bytes, ScanSourceKind.Excel);
        var proposal = ScanOfficeFieldPlanBuilder.Build(yellows, set, bytes, ScanSourceKind.Excel);
        var plan = new ScanFieldPlanMerger().Merge(new ScanFieldPlanMergeRequest
        {
            PlaceholderSet = set,
            ScanKind = ScanKind.FilledSample,
            Proposal = proposal,
        });

        Assert.Contains(plan.Fields, f => f.LabelText == "Erol" && f.ProposedToken!.Contains("PLN", StringComparison.Ordinal));
        Assert.Contains(plan.Fields, f => f.LabelText == "Hilmi" && f.ProposedToken!.Contains("PFNM", StringComparison.Ordinal));
    }

    [Fact]
    public void Resolve_maps_hasaba_residence_column_to_ADRS_not_company_address()
    {
        var set = PlaceholderSet();
        using var ms = new MemoryStream();
        using (var wb = new XLWorkbook())
        {
            var ws = wb.AddWorksheet("Sanaw");
            ws.Cell("K2").Value = "Wiza maglumatlary";
            ws.Cell("L2").Value = "Türkmenistandaky salgysy";
            ws.Cell("K3").Value = "A1688318 FM";
            ws.Cell("K3").Style.Fill.BackgroundColor = XLColor.Yellow;
            ws.Cell("L3").Value = "Aşgabat şäheriniň 11-nji (Bagtyýarlyk) etrap, I.Gandyýew köçesi jaý-12";
            ws.Cell("L3").Style.Fill.BackgroundColor = XLColor.Yellow;
            wb.SaveAs(ms);
        }

        var bytes = ms.ToArray();
        var yellows = new ScanOfficeYellowExtractor().Extract(bytes, ScanSourceKind.Excel);
        var fields = ScanExcelYellowResolver.Resolve(bytes, yellows, set);

        var address = Assert.Single(fields, f => f.LabelText.Contains("Bagtyýarlyk", StringComparison.Ordinal));
        Assert.Equal(ScanFieldScope.Row, address.Scope);
        Assert.Equal("{{.ADRS}}", address.ProposedToken);
        Assert.DoesNotContain("ACADR", address.ProposedToken, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(
            address.Alternatives.Take(2),
            a => a.ShortCode.Equals("ACADR", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Resolve_maps_border_zone_from_column_header()
    {
        var set = PlaceholderSet();
        using var ms = BuildSanawStyleWorkbook();
        var bytes = ms.ToArray();
        var yellows = new ScanOfficeYellowExtractor().Extract(bytes, ScanSourceKind.Excel);

        var fields = ScanExcelYellowResolver.Resolve(bytes, yellows, set);
        var borderZone = fields.FirstOrDefault(f => f.LabelText == "Garabogaz");
        Assert.NotNull(borderZone);
        Assert.Contains("ABZLN", borderZone!.ProposedToken!, StringComparison.Ordinal);
        Assert.Equal(ScanFieldConfidence.High, borderZone.Confidence);
    }

    [Fact]
    public void Resolve_maps_added_movement_areas_column_to_case_work_permit_location()
    {
        var set = PlaceholderSet();
        using var ms = new MemoryStream();
        using (var wb = new XLWorkbook())
        {
            var ws = wb.AddWorksheet("Sanaw");
            ws.Cell("A2").Value = "Goşulmaly hereket çäkleri";
            ws.Cell("A3").Value = "Aşgabat şäheri";
            ws.Cell("A3").Style.Fill.BackgroundColor = XLColor.Yellow;
            wb.SaveAs(ms);
        }

        var bytes = ms.ToArray();
        var yellows = new ScanOfficeYellowExtractor().Extract(bytes, ScanSourceKind.Excel);
        var fields = ScanExcelYellowResolver.Resolve(bytes, yellows, set);
        var location = Assert.Single(fields, f => f.LabelText == "Aşgabat şäheri");
        Assert.Equal(ScanFieldScope.Row, location.Scope);
        Assert.Equal("{{.AWPLC}}", location.ProposedToken);
        Assert.Equal(["AWPLC"], TemplateTokenSyntax.GetShortCodes(location.ProposedToken));
        Assert.Equal(ScanFieldConfidence.High, location.Confidence);
    }

    [Fact]
    public void Resolve_maps_valid_to_column_to_linked_work_permit_expiration()
    {
        var set = PlaceholderSet();
        using var ms = new MemoryStream();
        using (var wb = new XLWorkbook())
        {
            var ws = wb.AddWorksheet("Sanaw");
            ws.Cell("A4").Value = "Rugsat edilen möhleti";
            ws.Cell("A5").Value = "01.01.2027";
            ws.Cell("A5").Style.Fill.BackgroundColor = XLColor.Yellow;
            wb.SaveAs(ms);
        }

        var bytes = ms.ToArray();
        var yellows = new ScanOfficeYellowExtractor().Extract(bytes, ScanSourceKind.Excel);
        var fields = ScanExcelYellowResolver.Resolve(bytes, yellows, set);
        var date = Assert.Single(fields, f => f.LabelText == "01.01.2027");
        Assert.Equal("{{.WPED}}", date.ProposedToken);
    }

    [Fact]
    public void Resolve_maps_kicirak_passport_column_to_previous_short_codes()
    {
        var set = PlaceholderSet();
        using var ms = new MemoryStream();
        using (var wb = new XLWorkbook())
        {
            var ws = wb.AddWorksheet("Sanaw");
            ws.Cell("A1").Value = "Kiçirak pasportyň maglumatlary";
            ws.Cell("B2").Value = "Familiýasy";
            ws.Cell("C2").Value = "Pasport belgisi";
            ws.Cell("B3").Value = "Aydogan";
            ws.Cell("B3").Style.Fill.BackgroundColor = XLColor.Yellow;
            ws.Cell("C3").Value = "U24909175";
            ws.Cell("C3").Style.Fill.BackgroundColor = XLColor.Yellow;
            ws.Cell("A5").Value = "Täze pasportyň maglumatlary";
            ws.Cell("B6").Value = "Familiýasy";
            ws.Cell("C6").Value = "Pasport belgisi";
            ws.Cell("B7").Value = "Aydogan";
            ws.Cell("B7").Style.Fill.BackgroundColor = XLColor.Yellow;
            ws.Cell("C7").Value = "U36556957";
            ws.Cell("C7").Style.Fill.BackgroundColor = XLColor.Yellow;
            wb.SaveAs(ms);
        }

        var bytes = ms.ToArray();
        var yellows = new ScanOfficeYellowExtractor().Extract(bytes, ScanSourceKind.Excel);
        var fields = ScanExcelYellowResolver.Resolve(bytes, yellows, set);

        var previousNumber = Assert.Single(fields, f => f.LabelText == "U24909175");
        Assert.Contains("PRPN", previousNumber.ProposedToken!, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("RPPN", previousNumber.ProposedToken!, StringComparison.OrdinalIgnoreCase);

        var currentNumber = Assert.Single(fields, f => f.LabelText == "U36556957");
        Assert.Contains("PPN", currentNumber.ProposedToken!, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("PRPN", currentNumber.ProposedToken!, StringComparison.OrdinalIgnoreCase);

        var previousName = Assert.Single(fields, f => f.LabelText == "Aydogan" && f.SourceRegion is DocumentRegion.ExcelCell { CellReference: "B3" });
        Assert.Contains("PLN", previousName.ProposedToken!, StringComparison.Ordinal);
    }

    [Fact]
    public void Resolve_maps_foreign_address_cell_with_comma_to_country_and_street()
    {
        var set = PlaceholderSet();
        using var ms = new MemoryStream();
        using (var wb = new XLWorkbook())
        {
            var ws = wb.AddWorksheet("Sanaw");
            ws.Cell("M3").Value = "13.1";
            ws.Cell("N3").Value = "13.2";
            ws.Range("M2:N2").Merge();
            ws.Cell("M2").Value = "Daşary ýurtdaky salgysy";
            ws.Cell("M4").Value = "TUR, Pazara evin, Mehmet Ile site 9 N-4";
            ws.Cell("M4").Style.Fill.BackgroundColor = XLColor.Yellow;
            wb.SaveAs(ms);
        }

        var bytes = ms.ToArray();
        var yellows = new ScanOfficeYellowExtractor().Extract(bytes, ScanSourceKind.Excel);
        var fields = ScanExcelYellowResolver.Resolve(bytes, yellows, set);
        var cell = Assert.Single(fields);
        Assert.Equal(["PFAC", "PFAD"], TemplateTokenSyntax.GetShortCodes(cell.ProposedToken));

        var ordered = ScanReviewFieldOrder.Order(
        [
            new ScanDetectedField
            {
                FieldId = cell.FieldId,
                Box = ScanBoundingBox.FullPage,
                PageIndex = 0,
                LabelText = cell.LabelText,
                ProposedToken = cell.ProposedToken,
                Confidence = cell.Confidence,
                Scope = cell.Scope,
                SourceRegion = cell.SourceRegion,
            },
        ]);
        Assert.Equal(2, ordered.Count);
        Assert.Equal(["PFAC"], TemplateTokenSyntax.GetShortCodes(ordered[0].ProposedToken));
        Assert.Equal(["PFAD"], TemplateTokenSyntax.GetShortCodes(ordered[1].ProposedToken));
        Assert.Equal("TUR", ordered[0].LabelText);
    }

    [Fact]
    public void Resolve_maps_split_foreign_address_columns_to_pfac_then_pfad()
    {
        var set = PlaceholderSet();
        using var ms = new MemoryStream();
        using (var wb = new XLWorkbook())
        {
            var ws = wb.AddWorksheet("Sanaw");
            ws.Range("M2:N2").Merge();
            ws.Cell("M2").Value = "Daşary ýurtdaky salgysy";
            ws.Cell("M3").Value = "13.1";
            ws.Cell("N3").Value = "13.2";
            ws.Cell("M4").Value = "TUR";
            ws.Cell("M4").Style.Fill.BackgroundColor = XLColor.Yellow;
            ws.Cell("N4").Value = "Pazara evin, Mehmet Ile site 9 N-4 kapi N-33";
            ws.Cell("N4").Style.Fill.BackgroundColor = XLColor.Yellow;
            wb.SaveAs(ms);
        }

        var bytes = ms.ToArray();
        var yellows = new ScanOfficeYellowExtractor().Extract(bytes, ScanSourceKind.Excel);
        var fields = ScanExcelYellowResolver.Resolve(bytes, yellows, set);
        var country = Assert.Single(fields, f => f.LabelText == "TUR");
        var street = Assert.Single(fields, f => f.LabelText.StartsWith("Pazara", StringComparison.Ordinal));
        Assert.Equal("{{.PFAC}}", country.ProposedToken);
        Assert.Equal("{{.PFAD}}", street.ProposedToken);
        Assert.DoesNotContain("PFAC", street.ProposedToken, StringComparison.OrdinalIgnoreCase);
    }

    private static MemoryStream BuildSanawStyleWorkbook()
    {
        var ms = new MemoryStream();
        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("Sanaw");

        ws.Cell("B4").Value = "Familiýasy";
        ws.Cell("C4").Value = "Ady";
        ws.Cell("D4").Value = "Doglan senesi we ýeri";
        ws.Cell("E4").Value = "Jynsy";
        ws.Cell("F4").Value = "Raýatlygy";
        ws.Cell("G4").Value = "Pasport belgisi we möhleti";
        ws.Cell("H4").Value = "Bilimi we okan ýeri";
        ws.Cell("I4").Value = "Bilimine görä hünäri";
        ws.Cell("J4").Value = "Wezipesi";
        ws.Cell("K4").Value = "Möhleti we gezekligi";
        ws.Cell("L4").Value = "Türkmenistandaky salgysy";
        ws.Cell("M4").Value = "Daşary ýurtdaky salgysy";
        ws.Cell("N4").Value = "Barjak serhet ýakasy";

        void Yellow(int col, string value)
        {
            var cell = ws.Cell(5, col);
            cell.Value = value;
            cell.Style.Fill.BackgroundColor = XLColor.Yellow;
        }

        Yellow(2, "Erol");
        Yellow(3, "Hilmi");
        Yellow(4, "16.05.1980, Türkiye/ Üsküdar");
        Yellow(5, "Erkek");
        Yellow(6, "TUR");
        Yellow(7, "U20352559, 20.06.2028");
        Yellow(8, "Ýokary, Gündogar mediterian uniwersiteti");
        Yellow(9, "elektrik-elektronika inženerçiligi");
        Yellow(10, "Taslamanyň dolandyryş müdiri");
        Yellow(11, "Çakylyk 6 (alty) aý, köp gezeklik");
        Yellow(12, "Garabogaz awtomobil ýol");
        Yellow(13, "TUR, Tatlısu mah. Istanbul");
        Yellow(14, "Garabogaz");

        wb.SaveAs(ms);
        return ms;
    }
}
