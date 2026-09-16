#nullable enable
using ClosedXML.Excel;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.TemplateConvert;
using Visa2026.Module.Services.TemplateScan;
using Visa2026.Module.Services.UserReports;
using Xunit;

namespace Visa2026.Module.Tests.TemplateScan;

public class ScanExcelBtadProbeTests
{
    private static ApplicationProfilePlaceholderSet Set() =>
        new ApplicationProfilePlaceholderSetService(new UserReportPlaceholderCatalogService()).GetSet(
            new ApplicationProfilePlaceholderSetQuery
            {
                Profile = new ApplicationProfile
                {
                    RequirePersonPassport = true,
                    RequirePersonAddressOfResidence = true,
                    RequireBusinessTripAddress = true,
                },
                DataScope = ApplicationProfileTemplateDataScope.Both,
                TemplateKind = ApplicationProfileTemplateKind.Excel,
            });

    [Theory]
    [InlineData("Iş saparynda boljak salgysy")]
    [InlineData("Iş saparyna barýan ýer")]
    public void Resolve_maps_business_trip_address_column_to_BTAD(string header)
    {
        Assert.True(Set().Contains("BTAD"));

        using var ms = new MemoryStream();
        using (var wb = new XLWorkbook())
        {
            var ws = wb.AddWorksheet("Sanaw");
            ws.Cell("L4").Value = header;
            ws.Cell("L5").Value = "Ahal wel, Akbugdaý etr, Çalyk Enerji UYJ.";
            ws.Cell("L5").Style.Fill.BackgroundColor = XLColor.Yellow;
            wb.SaveAs(ms);
        }

        var bytes = ms.ToArray();
        var yellows = new ScanOfficeYellowExtractor().Extract(bytes, ScanSourceKind.Excel);
        var proposal = ScanOfficeFieldPlanBuilder.Build(yellows, Set(), bytes, ScanSourceKind.Excel);
        var plan = new ScanFieldPlanMerger().Merge(new ScanFieldPlanMergeRequest
        {
            PlaceholderSet = Set(),
            ScanKind = ScanKind.FilledSample,
            Proposal = proposal,
        });

        var field = Assert.Single(plan.Fields);
        Assert.Equal("{{.BTAD}}", field.ProposedToken);
        Assert.Equal(ScanFieldConfidence.High, field.Confidence);
        Assert.Contains(field.Alternatives, a =>
            a.ShortCode.Equals("BTAD", StringComparison.OrdinalIgnoreCase)
            && a.ScorePercent >= 80
            && a.Reason.Contains("Column", StringComparison.OrdinalIgnoreCase));

        var ordered = ScanReviewFieldOrder.Order(plan.Fields);
        Assert.Single(ordered);
        Assert.Equal(0, ordered[0].PartIndex);
        Assert.Equal("{{.BTAD}}", ordered[0].ProposedToken);
        Assert.DoesNotContain(".", ordered[0].OrderLabel);
        Assert.True(ScanCompoundYellowParts.IsSingleSpanAddressCode("BTAD"));
        Assert.Empty(ScanCompoundYellowParts.Split(field.LabelText, "{{.BTAD}}"));
        Assert.True(ScanCompoundYellowParts.Split(field.LabelText, null).Count >= 2);
    }
}