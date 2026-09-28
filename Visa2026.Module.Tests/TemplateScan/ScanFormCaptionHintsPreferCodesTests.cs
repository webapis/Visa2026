using Visa2026.Module.Services.TemplateScan;
using Xunit;

namespace Visa2026.Module.Tests.TemplateScan;

public class ScanFormCaptionHintsPreferCodesTests
{
    [Fact]
    public void ExtractParentheticalList_picks_longest_comma_list()
    {
        var text = "Başlyk (A) we (Familiýasy, Ady, Atasynyň ady) beýany";
        Assert.Equal("(Familiýasy, Ady, Atasynyň ady)", ScanFormCaptionHints.ExtractParentheticalList(text));
        Assert.Null(ScanFormCaptionHints.ExtractParentheticalList("no list"));
        Assert.Null(ScanFormCaptionHints.ExtractParentheticalList("(short)"));
    }

    [Fact]
    public void Slots_splits_parenthetical_parts()
    {
        var slots = ScanFormCaptionHints.Slots("Ýazgy (Familiýasy, Ady, Atasynyň ady)");
        Assert.Equal(["Familiýasy", "Ady", "Atasynyň ady"], slots);
        Assert.Empty(ScanFormCaptionHints.Slots(null));
    }

    [Theory]
    [InlineData("Telefon", ScanLetterRole.Applicant, "ACPHN")]
    [InlineData("Telefon", ScanLetterRole.Wekil, "RPPH")]
    [InlineData("Doglan senesi", ScanLetterRole.Applicant, "PDBT")]
    [InlineData("Familiýasy", ScanLetterRole.Applicant, "PFN")]
    [InlineData("Familiýasy", ScanLetterRole.Wekil, "RPFN")]
    [InlineData("Familiýasy", ScanLetterRole.Signatory, "CHFN")]
    public void PreferCodes_routes_common_slots(string slot, ScanLetterRole role, string expectedFirst)
    {
        var codes = ScanFormCaptionHints.PreferCodes(slot, role, nearbyLabel: null);
        Assert.NotEmpty(codes);
        Assert.Equal(expectedFirst, codes[0]);
    }

    [Fact]
    public void PreferCodes_mohlet_contract_nearby_uses_company_dates()
    {
        var codes = ScanFormCaptionHints.PreferCodes(
            "Möhleti",
            ScanLetterRole.Applicant,
            nearbyLabel: "Zähmet şertnamasy");
        Assert.Equal(["CSDT", "CEDT"], codes);
    }

    [Theory]
    [InlineData(ScanLetterRole.Applicant, "PPED")]
    [InlineData(ScanLetterRole.Wekil, "RPPD")]
    [InlineData(ScanLetterRole.Signatory, "CHPE")]
    public void PreferCodes_mohlet_passport_expiry_by_role(ScanLetterRole role, string expected)
    {
        var codes = ScanFormCaptionHints.PreferCodes("Möhleti", role, nearbyLabel: null);
        Assert.Equal([expected], codes);
    }

    [Fact]
    public void PreferCodes_passport_number_and_tax_registration()
    {
        Assert.Equal(["PPN"], ScanFormCaptionHints.PreferCodes("Pasport belgi", ScanLetterRole.Applicant, null));
        Assert.Equal(["RPPN"], ScanFormCaptionHints.PreferCodes("Pasport seriya", ScanLetterRole.Wekil, null));
        Assert.Equal(["ACTAX"], ScanFormCaptionHints.PreferCodes("Hasaba alynan belgi", ScanLetterRole.Applicant, null));
    }

    [Theory]
    [InlineData("PPN", ScanLetterRole.Wekil, "RPPN")]
    [InlineData("PPED", ScanLetterRole.Signatory, "CHPE")]
    [InlineData("PFN", ScanLetterRole.Wekil, "RPFN")]
    [InlineData("ACPHN", ScanLetterRole.Wekil, "RPPH")]
    [InlineData("PPN", ScanLetterRole.Applicant, "PPN")]
    public void RemapByRole_rewrites_passport_and_name_codes(string code, ScanLetterRole role, string expected) =>
        Assert.Equal(expected, ScanFormCaptionHints.RemapByRole(code, role));
}
