using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ReportDashboard;
using Xunit;

namespace Visa2026.Module.Tests.BusinessObjects;

/// <summary>
/// Sanaw replaced salary columns with visa duration/frequency, and Adam sany is family count only.
/// </summary>
public class VwRdWorkPermitSanawVisaDurationLabelTests
{
    [Fact]
    public void VisaDurationLabel_JoinsStartEndAndParenthesizedNumberWithCategory()
    {
        var row = new VwRdWorkPermitSanaw
        {
            VisaStartDate = new DateTime(2025, 1, 15),
            VisaEndDate = new DateTime(2025, 12, 31),
            VisaNumber = " 12 ",
            VisaCategoryLabel = " Ýygydan ",
        };

        Assert.Equal(
            "15.01.2025\n31.12.2025\n(12) Ýygydan",
            row.VisaDurationLabel);
    }

    [Fact]
    public void VisaDurationLabel_CategoryAlone_WhenNumberMissing()
    {
        var row = new VwRdWorkPermitSanaw
        {
            VisaCategoryLabel = "Bir gezeklik",
        };

        Assert.Equal("Bir gezeklik", row.VisaDurationLabel);
    }

    [Fact]
    public void VisaDurationLabel_NumberAlone_WhenCategoryMissing()
    {
        var row = new VwRdWorkPermitSanaw
        {
            VisaNumber = "3",
        };

        Assert.Equal("(3)", row.VisaDurationLabel);
    }

    [Fact]
    public void VisaDurationLabel_SkipsLegacyPlaceholderDates()
    {
        var row = new VwRdWorkPermitSanaw
        {
            VisaStartDate = new DateTime(1900, 1, 1),
            VisaEndDate = new DateTime(1899, 12, 31),
            VisaNumber = "1",
            VisaCategoryLabel = "Ýygydan",
        };

        Assert.Equal("(1) Ýygydan", row.VisaDurationLabel);
    }

    [Fact]
    public void VisaDurationLabel_Empty_WhenAllFieldsBlank()
    {
        Assert.Equal(string.Empty, new VwRdWorkPermitSanaw().VisaDurationLabel);
    }

    [Theory]
    [InlineData(0, "")]
    [InlineData(1, "1")]
    [InlineData(3, "3")]
    public void PersonCountLabel_IsFamilyMemberCount_NotPlusOne(int familyCount, string expected)
    {
        var row = new VwRdWorkPermitSanaw { FamilyMemberCount = familyCount };
        Assert.Equal(expected, row.PersonCountLabel);
    }

    [Fact]
    public void Catalog_SanawHeader_UsesVisaDurationCaption()
    {
        var headers = ReportDashboardCatalog.EnglishTableHeaders(
            ReportDashboardCategory.WorkPermit,
            ReportDashboardCatalog.WorkPermitSanawKey);

        Assert.Equal(16, headers.Length);
        Assert.Equal("Möhleti we gezekligi", headers[10]);
        Assert.Equal("Adam sany", headers[15]);
        Assert.DoesNotContain(headers, h => h.Contains("gazanji", StringComparison.Ordinal));
    }
}
