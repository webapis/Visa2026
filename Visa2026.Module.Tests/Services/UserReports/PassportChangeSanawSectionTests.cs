#nullable enable

using System.Collections.Generic;
using ClosedXML.Excel;
using Visa2026.Module.Services.UserReports;
using Xunit;

namespace Visa2026.Module.Tests.Services.UserReports;

public class PassportChangeSanawSectionTests
{
    [Fact]
    public void IsPreviousBand_true_under_kicirak_title()
    {
        using var workbook = BuildStackedSheet();
        var sheet = workbook.Worksheet(1);

        Assert.True(PassportChangeSanawSection.IsPreviousBand(sheet, 4));
        Assert.False(PassportChangeSanawSection.IsNewBand(sheet, 4));
        Assert.True(PassportChangeSanawSection.IsRosterBand(sheet, 4));
    }

    [Fact]
    public void IsNewBand_true_under_taze_title()
    {
        using var workbook = BuildStackedSheet();
        var sheet = workbook.Worksheet(1);

        Assert.True(PassportChangeSanawSection.IsNewBand(sheet, 7));
        Assert.False(PassportChangeSanawSection.IsPreviousBand(sheet, 7));
    }

    [Fact]
    public void RemapTokenToPrevious_maps_ppn_family_not_rppn()
    {
        Assert.Equal("{{.PRPN}}", PassportChangeSanawSection.RemapTokenToPrevious("{{.PPN}}"));
        Assert.Equal("{{.PRPN}}, {{.PRAT}}", PassportChangeSanawSection.RemapTokenToPrevious("{{.PPN}}, {{.PPAT}}"));
        Assert.Equal("{{ds.RPPN}}", PassportChangeSanawSection.RemapTokenToPrevious("{{ds.RPPN}}"));
        Assert.Equal("{{.PLN}}", PassportChangeSanawSection.RemapTokenToPrevious("{{.PLN}}"));
    }

    [Fact]
    public void Overlay_copies_previous_passport_onto_current_keys()
    {
        var row = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
        {
            ["Passport_Number"] = "NEW",
            ["PPN"] = "NEW",
            ["PreviousPassport_Number"] = "OLD",
            ["PRPN"] = "OLD",
            ["Person_LastName"] = "Aydogan",
        };

        PassportChangeSanawSection.OverlayCurrentPassportFromPrevious(row);

        Assert.Equal("OLD", row["Passport_Number"]);
        Assert.Equal("OLD", row["PPN"]);
        Assert.Equal("OLD", row["PreviousPassport_Number"]);
        Assert.Equal("Aydogan", row["Person_LastName"]);
    }

    private static XLWorkbook BuildStackedSheet()
    {
        var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Sanaw");
        sheet.Cell("A1").Value = "Dasary yurt rayatynyn sanawy";
        sheet.Cell("A2").Value = "Kicirak pasportyn maglumatlary";
        sheet.Cell("B3").Value = "Familiyasy";
        sheet.Cell("C3").Value = "Pasport";
        sheet.Cell("B4").Value = "{{.PLN}}";
        sheet.Cell("C4").Value = "{{.PPN}}";
        sheet.Cell("A5").Value = "Taze pasportyn maglumatlary";
        sheet.Cell("B6").Value = "Familiyasy";
        sheet.Cell("C6").Value = "Pasport";
        sheet.Cell("B7").Value = "{{.PLN}}";
        sheet.Cell("C7").Value = "{{.PPN}}";
        return workbook;
    }
}