using Visa2026.Module.Services.TemplateScan;
using Xunit;

namespace Visa2026.Module.Tests.TemplateScan;

/// <summary>
/// PreferCodes branches for authority, registration date (senesi), and address/company slots.
/// Complements ScanFormCaptionHintsPreferCodesTests (telefon/name/möhleti/passport/tax).
/// </summary>
public class ScanFormCaptionHintsPreferCodesAuthorityAddressTests
{
    [Theory]
    [InlineData("Nirede berildi", ScanLetterRole.Applicant, "PPAT")]
    [InlineData("Berildi edara", ScanLetterRole.Wekil, "RPPA")]
    [InlineData("Hakimiyet", ScanLetterRole.Signatory, "CHPA")]
    public void PreferCodes_authority_by_role(string slot, ScanLetterRole role, string expected)
    {
        var codes = ScanFormCaptionHints.PreferCodes(slot, role, nearbyLabel: null);
        Assert.Equal([expected], codes);
    }

    [Fact]
    public void PreferCodes_senesi_company_registration_nearby()
    {
        var codes = ScanFormCaptionHints.PreferCodes(
            "Senesi",
            ScanLetterRole.Applicant,
            nearbyLabel: "Karhana hasaba alyş");
        Assert.Equal(["ACRDT"], codes);
    }

    [Fact]
    public void PreferCodes_senesi_birth_or_applicant_nearby()
    {
        var birth = ScanFormCaptionHints.PreferCodes(
            "Senesi",
            ScanLetterRole.Wekil,
            nearbyLabel: "Doglan ýeri");
        Assert.Equal(["PDBT"], birth);

        var applicant = ScanFormCaptionHints.PreferCodes(
            "Senesi",
            ScanLetterRole.Applicant,
            nearbyLabel: null);
        Assert.Equal(["PDBT"], applicant);
    }

    [Fact]
    public void PreferCodes_senesi_ambiguous_offers_registration_then_birth()
    {
        var codes = ScanFormCaptionHints.PreferCodes(
            "Senesi",
            ScanLetterRole.Signatory,
            nearbyLabel: null);
        Assert.Equal(["ACRDT", "PDBT"], codes);
    }

    [Fact]
    public void PreferCodes_foreign_address_slot()
    {
        var codes = ScanFormCaptionHints.PreferCodes(
            "Daşary ýurtdaky salgysy",
            ScanLetterRole.Applicant,
            nearbyLabel: null);
        Assert.Equal(["PFAD", "PFWC"], codes);
    }

    [Theory]
    [InlineData("Ýuridiki salgysy", null, "ACADR")]
    [InlineData("Salgysy", "Karhana ady", "ACADR")]
    [InlineData("Adres", "Iş beriji", "ACADR")]
    public void PreferCodes_company_address(string slot, string? nearby, string expected)
    {
        var codes = ScanFormCaptionHints.PreferCodes(slot, ScanLetterRole.Applicant, nearby);
        Assert.Equal([expected], codes);
    }

    [Fact]
    public void PreferCodes_person_residence_address()
    {
        var codes = ScanFormCaptionHints.PreferCodes(
            "Ýaşaýan salgysy",
            ScanLetterRole.Applicant,
            nearbyLabel: null);
        Assert.Equal(["ADRS"], codes);
    }

    [Fact]
    public void LooksLikeCompanyAddress_and_person_residence()
    {
        Assert.True(ScanFormCaptionHints.LooksLikeCompanyAddress("yuridiki salgy"));
        Assert.True(ScanFormCaptionHints.LooksLikeCompanyAddress("is beriji adresi"));
        Assert.False(ScanFormCaptionHints.LooksLikeCompanyAddress("turkmenistan yasayan salgy"));

        Assert.True(ScanFormCaptionHints.LooksLikePersonResidence("turkmenistan yasayan salgy"));
        Assert.False(ScanFormCaptionHints.LooksLikePersonResidence("yuridiki salgy"));
        Assert.False(ScanFormCaptionHints.LooksLikePersonResidence("turkmenistan"));
    }
}
