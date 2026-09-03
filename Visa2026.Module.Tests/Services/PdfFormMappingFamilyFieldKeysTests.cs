using Visa2026.Module.Services;
using Xunit;

namespace Visa2026.Module.Tests.Services;

/// <summary>
/// Stable XFA key / NotMapped path contract for visa PDF family block (item 18).
/// Wrong keys silently empty ministry PDFs after manual-only family wiring.
/// </summary>
public sealed class PdfFormMappingFamilyFieldKeysTests
{
    [Fact]
    public void LineKeys_MatchPage1Item18Fields()
    {
        Assert.Equal("topmostSubform[0].Page1[0]._181[0]", PdfFormMappingFamilyFieldKeys.Line1Key);
        Assert.Equal("topmostSubform[0].Page1[0]._182[0]", PdfFormMappingFamilyFieldKeys.Line2Key);
        Assert.Equal("topmostSubform[0].Page1[0]._183[0]", PdfFormMappingFamilyFieldKeys.Line3Key);
    }

    [Fact]
    public void AggregateAndMaritalPaths_MatchApplicationItemNotMappedNames()
    {
        Assert.Equal("Pdf_FamilyMembersAggregateText", PdfFormMappingFamilyFieldKeys.AggregatePath);
        Assert.Equal("Pdf_FamilyMembersMaritalLine1", PdfFormMappingFamilyFieldKeys.MaritalLine1Path);
        Assert.Equal("Pdf_FamilyMembersMaritalLine2", PdfFormMappingFamilyFieldKeys.MaritalLine2Path);
        Assert.Equal("Pdf_FamilyMembersMaritalLine3", PdfFormMappingFamilyFieldKeys.MaritalLine3Path);
    }

    [Fact]
    public void WrongAggregateKey_IsDistinctFromLineKeys()
    {
        Assert.Equal("topmostSubform[0].Page1[0]._241[0]", PdfFormMappingFamilyFieldKeys.WrongAggregateKey);
        Assert.NotEqual(PdfFormMappingFamilyFieldKeys.WrongAggregateKey, PdfFormMappingFamilyFieldKeys.Line1Key);
        Assert.NotEqual(PdfFormMappingFamilyFieldKeys.WrongAggregateKey, PdfFormMappingFamilyFieldKeys.Line2Key);
        Assert.NotEqual(PdfFormMappingFamilyFieldKeys.WrongAggregateKey, PdfFormMappingFamilyFieldKeys.Line3Key);
    }

    [Fact]
    public void EducationPlace_KeyAndPathRemainStable()
    {
        Assert.Equal("topmostSubform[0].Page1[0]._21[0]", PdfFormMappingFamilyFieldKeys.EducationPlaceKey);
        Assert.Equal("Pdf_EducationPlaceOfStudy", PdfFormMappingFamilyFieldKeys.EducationPlacePath);
    }
}
