using Visa2026.Module.Localization;
using Visa2026.Module.Services.ReportDashboard;
using Xunit;

namespace Visa2026.Module.Tests.Localization;

public sealed class ReportDashboardLocalizationTests
{
    [Fact]
    public void SubReport_BlankKey_ReturnsEnglishFallbackOrEmpty()
    {
        Assert.Equal(string.Empty, ReportDashboardLocalization.SubReport(ReportDashboardCategory.VisaExtension, null));
        Assert.Equal(string.Empty, ReportDashboardLocalization.SubReport(ReportDashboardCategory.VisaExtension, "   "));
        Assert.Equal(
            "Fallback label",
            ReportDashboardLocalization.SubReport(ReportDashboardCategory.VisaExtension, "\t", "Fallback label"));
    }

    [Fact]
    public void SubReport_AddressByValidity_UsesPrivateHouseKey()
    {
        Assert.Equal(
            "By Private House Validity",
            ReportDashboardLocalization.SubReport(ReportDashboardCategory.AddressOfResidence, "by-validity"));
        Assert.Equal(
            "By Private House Validity",
            ReportDashboardLocalization.SubReport(ReportDashboardCategory.AddressOfResidence, "BY-VALIDITY"));
    }

    [Fact]
    public void SubReport_GenericKey_FallsBackWhenCategorySpecificMissing()
    {
        // Generic catalog key ReportDashboard.SubReport.by-validity exists; Passport has no category-specific override.
        Assert.Equal(
            "By Validity",
            ReportDashboardLocalization.SubReport(ReportDashboardCategory.Passport, "by-validity"));
    }

    [Fact]
    public void SubReport_UnknownKey_UsesEnglishFallbackThenKey()
    {
        Assert.Equal(
            "Custom title",
            ReportDashboardLocalization.SubReport(
                ReportDashboardCategory.Education,
                "not-a-real-subreport-key",
                "Custom title"));
        Assert.Equal(
            "not-a-real-subreport-key",
            ReportDashboardLocalization.SubReport(
                ReportDashboardCategory.Education,
                "not-a-real-subreport-key"));
    }

    [Theory]
    [InlineData("Valid", "Valid")]
    [InlineData("Expiring Soon", "Expiring Soon")]
    [InlineData("Unknown city", "Unknown city")]
    [InlineData("(No project)", "(No project)")]
    public void Status_ExactKnownLabels_Localize(string english, string expectedEn)
    {
        Assert.Equal(expectedEn, ReportDashboardLocalization.Status(english));
    }

    [Fact]
    public void Status_Blank_ReturnsEmpty()
    {
        Assert.Equal(string.Empty, ReportDashboardLocalization.Status(null));
        Assert.Equal(string.Empty, ReportDashboardLocalization.Status("  "));
    }

    [Fact]
    public void Status_DotJoinedSegments_LocalizeRecognizedParts()
    {
        // English → English for known buckets; unknown segment preserved.
        Assert.Equal(
            "Valid · Custom bucket",
            ReportDashboardLocalization.Status("Valid · Custom bucket"));
    }

    [Fact]
    public void Status_CommaIncompleteList_LocalizesRecognizedSegments()
    {
        Assert.Equal(
            "Personal data, Passport, Custom area",
            ReportDashboardLocalization.Status("Personal data, Passport, Custom area"));
    }

    [Fact]
    public void Status_UnknownLabel_PassesThrough()
    {
        Assert.Equal("Totally Unknown Bucket", ReportDashboardLocalization.Status("Totally Unknown Bucket"));
    }

    [Theory]
    [InlineData(12, "1 year")]
    [InlineData(24, "2 years")]
    [InlineData(36, "3 years")]
    [InlineData(6, "6 months")]
    [InlineData(18, "18 months")]
    public void PeriodMonthLabel_MapsSpecialYearsAndFormat(int months, string expected)
    {
        Assert.Equal(expected, ReportDashboardLocalization.PeriodMonthLabel(months));
    }

    [Theory]
    [InlineData(ReportDashboardCategory.Passport, "Passport: ApplicationItem.CurrentPassport with Application.ApplicationDate in range")]
    [InlineData(ReportDashboardCategory.Education, "Education: ApplicationItem.CurrentEducation with Application.ApplicationDate in range")]
    [InlineData(ReportDashboardCategory.VisaExtension, "Education: ApplicationItem.CurrentEducation with Application.ApplicationDate in range")]
    public void CategoryDateRangeTitle_UsesCategoryChromeOrEducationDefault(
        ReportDashboardCategory category,
        string expected)
    {
        Assert.Equal(expected, ReportDashboardLocalization.CategoryDateRangeTitle(category));
    }

    [Fact]
    public void Header_KnownAndUnknown()
    {
        Assert.Equal("App #", ReportDashboardLocalization.Header("App #"));
        Assert.Equal("Weird Column", ReportDashboardLocalization.Header("Weird Column"));
    }
}
