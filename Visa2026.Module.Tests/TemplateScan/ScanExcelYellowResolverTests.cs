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
