using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services;
using Visa2026.Module.Services.ApplicationItemLinkedDocuments;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public sealed class ApplicationItemDocumentBatchSummaryKindMappingTests
{
    private static ApplicationItemDocumentPackageOptions Defaults() =>
        ApplicationItemDocumentPackageOptions.CreateDefaults();

    [Theory]
    [InlineData("Passport.Current", ApplicationItemDocumentBatchSummaryKind.CurrentPassports)]
    [InlineData("Visa.Current", ApplicationItemDocumentBatchSummaryKind.CurrentVisas)]
    [InlineData("WorkPermit.Current", ApplicationItemDocumentBatchSummaryKind.CurrentWorkPermits)]
    [InlineData("Education.Current", ApplicationItemDocumentBatchSummaryKind.AllDiplomas)]
    public void TryFromSlotKey_MapsKnownSlotsWhenIncludesEnabled(
        string slotKey,
        ApplicationItemDocumentBatchSummaryKind expected)
    {
        var options = Defaults();

        var ok = ApplicationItemDocumentBatchSummaryKindMapping.TryFromSlotKey(
            slotKey, options, out var kind);

        Assert.True(ok);
        Assert.Equal(expected, kind);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Unknown.Slot")]
    [InlineData("passport.current")]
    public void TryFromSlotKey_RejectsBlankUnknownOrWrongCase(string slotKey)
    {
        Assert.False(ApplicationItemDocumentBatchSummaryKindMapping.TryFromSlotKey(
            slotKey, Defaults(), out _));
    }

    [Fact]
    public void TryFromSlotKey_RejectsNullOptions()
    {
        Assert.False(ApplicationItemDocumentBatchSummaryKindMapping.TryFromSlotKey(
            "Passport.Current", null!, out _));
    }

    [Fact]
    public void TryFromSlotKey_IndividualFilesOnly_NeverMaps()
    {
        var options = Defaults();
        options.SupportingZipMergeOption = PdfSupportingZipMergeOption.IndividualFilesOnly;

        Assert.False(ApplicationItemDocumentBatchSummaryKindMapping.TryFromSlotKey(
            "Passport.Current", options, out _));
        Assert.False(ApplicationItemDocumentBatchSummaryKindMapping.TryFromSlotKey(
            "Visa.Current", options, out _));
        Assert.False(ApplicationItemDocumentBatchSummaryKindMapping.TryFromSlotKey(
            "Education.Current", options, out _));
    }

    [Fact]
    public void TryFromSlotKey_RespectsIncludeFlags()
    {
        var options = Defaults();
        options.IncludePassportCopies = false;
        options.IncludeVisaCopies = false;
        options.IncludeWorkPermitCopies = false;
        options.IncludeDiplomaFiles = false;

        Assert.False(ApplicationItemDocumentBatchSummaryKindMapping.TryFromSlotKey(
            "Passport.Current", options, out _));
        Assert.False(ApplicationItemDocumentBatchSummaryKindMapping.TryFromSlotKey(
            "Visa.Current", options, out _));
        Assert.False(ApplicationItemDocumentBatchSummaryKindMapping.TryFromSlotKey(
            "WorkPermit.Current", options, out _));
        Assert.False(ApplicationItemDocumentBatchSummaryKindMapping.TryFromSlotKey(
            "Education.Current", options, out _));
    }

    [Fact]
    public void TryFromSlotKey_EducationRequiresAllEducationsScope()
    {
        var options = Defaults();
        options.DiplomaScope = PdfBatchDiplomaScope.CurrentEducationOnly;

        Assert.False(ApplicationItemDocumentBatchSummaryKindMapping.TryFromSlotKey(
            "Education.Current", options, out _));
    }

    [Fact]
    public void TryFromSlotKey_MergedPdfSummariesOnly_StillMapsWhenIncludesOn()
    {
        var options = Defaults();
        options.SupportingZipMergeOption = PdfSupportingZipMergeOption.MergedPdfSummariesOnly;

        Assert.True(ApplicationItemDocumentBatchSummaryKindMapping.TryFromSlotKey(
            "Passport.Current", options, out var kind));
        Assert.Equal(ApplicationItemDocumentBatchSummaryKind.CurrentPassports, kind);
    }

    [Theory]
    [InlineData(ApplicationItemDocumentBatchSummaryKind.CurrentPassports, "CurrentPassports.pdf")]
    [InlineData(ApplicationItemDocumentBatchSummaryKind.CurrentVisas, "CurrentVisas.pdf")]
    [InlineData(ApplicationItemDocumentBatchSummaryKind.CurrentWorkPermits, "CurrentWorkPermits.pdf")]
    [InlineData(ApplicationItemDocumentBatchSummaryKind.AllDiplomas, "AllDiplomas.pdf")]
    [InlineData((ApplicationItemDocumentBatchSummaryKind)99, "summary.pdf")]
    public void GetDownloadFileName_MatchesKind(
        ApplicationItemDocumentBatchSummaryKind kind,
        string expected)
    {
        Assert.Equal(expected, ApplicationItemDocumentBatchSummaryKindMapping.GetDownloadFileName(kind));
    }

    [Fact]
    public void PackageOptions_ShowMergedDiplomaOption_HiddenForIndividualFilesOnly()
    {
        var options = Defaults();
        Assert.True(options.ShowMergedDiplomaOption);

        options.SupportingZipMergeOption = PdfSupportingZipMergeOption.IndividualFilesOnly;
        Assert.False(options.ShowMergedDiplomaOption);

        options.SupportingZipMergeOption = PdfSupportingZipMergeOption.IndividualFilesAndMergedPdfs;
        options.IncludeDiplomaFiles = false;
        Assert.False(options.ShowMergedDiplomaOption);
    }

    [Fact]
    public void PackageOptions_ApplyTo_CopiesFlagsAndGatesMergedDiploma()
    {
        var options = Defaults();
        options.IncludeMergedDiplomaPdf = true;
        options.IncludePassportCopies = false;
        options.SupportingZipMergeOption = PdfSupportingZipMergeOption.IndividualFilesOnly;

        var target = new PdfBatchEnqueueOptions();
        options.ApplyTo(target);

        Assert.False(target.IncludePassportCopies);
        Assert.True(target.IncludeVisaCopies);
        Assert.Equal(PdfSupportingZipMergeOption.IndividualFilesOnly, target.SupportingZipMergeOption);
        // IndividualFilesOnly hides merged-diploma option → ApplyTo must clear the flag.
        Assert.False(target.IncludeMergedDiplomaPdf);
    }

    [Fact]
    public void PackageOptions_ResetToDefaults_RestoresCreateDefaults()
    {
        var options = Defaults();
        options.IncludePassportCopies = false;
        options.DiplomaScope = PdfBatchDiplomaScope.CurrentEducationOnly;
        options.SupportingZipMergeOption = PdfSupportingZipMergeOption.MergedPdfSummariesOnly;
        options.IncludeMergedDiplomaPdf = true;

        options.ResetToDefaults();
        var fresh = ApplicationItemDocumentPackageOptions.CreateDefaults();

        Assert.Equal(fresh.IncludePassportCopies, options.IncludePassportCopies);
        Assert.Equal(fresh.DiplomaScope, options.DiplomaScope);
        Assert.Equal(fresh.SupportingZipMergeOption, options.SupportingZipMergeOption);
        Assert.Equal(fresh.IncludeMergedDiplomaPdf, options.IncludeMergedDiplomaPdf);
    }
}
