using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public sealed class ApplicationItemDocumentPackageOptionsTests
{
    [Fact]
    public void ShowMergedDiplomaOption_FalseWhenIndividualFilesOnly()
    {
        var options = ApplicationItemDocumentPackageOptions.CreateDefaults();
        options.IncludeDiplomaFiles = true;
        options.SupportingZipMergeOption = PdfSupportingZipMergeOption.IndividualFilesOnly;

        Assert.False(options.ShowMergedDiplomaOption);
    }

    [Fact]
    public void ShowMergedDiplomaOption_FalseWhenDiplomasExcluded()
    {
        var options = ApplicationItemDocumentPackageOptions.CreateDefaults();
        options.IncludeDiplomaFiles = false;
        options.SupportingZipMergeOption = PdfSupportingZipMergeOption.IndividualFilesAndMergedPdfs;

        Assert.False(options.ShowMergedDiplomaOption);
    }

    [Fact]
    public void ShowMergedDiplomaOption_TrueWhenDiplomasAndMergedAllowed()
    {
        var options = ApplicationItemDocumentPackageOptions.CreateDefaults();
        Assert.True(options.ShowMergedDiplomaOption);
    }

    [Fact]
    public void ApplyTo_ClampsMergedDiplomaWhenOptionHidden()
    {
        var options = ApplicationItemDocumentPackageOptions.CreateDefaults();
        options.SupportingZipMergeOption = PdfSupportingZipMergeOption.IndividualFilesOnly;
        options.IncludeMergedDiplomaPdf = true;

        var target = new PdfBatchEnqueueOptions();
        options.ApplyTo(target);

        Assert.False(target.IncludeMergedDiplomaPdf);
        Assert.Equal(PdfSupportingZipMergeOption.IndividualFilesOnly, target.SupportingZipMergeOption);
    }

    [Fact]
    public void ApplyTo_PassesMergedDiplomaWhenVisibleAndRequested()
    {
        var options = ApplicationItemDocumentPackageOptions.CreateDefaults();
        options.IncludeMergedDiplomaPdf = true;

        var target = new PdfBatchEnqueueOptions();
        options.ApplyTo(target);

        Assert.True(target.IncludeMergedDiplomaPdf);
    }

    [Fact]
    public void ResetToDefaults_RestoresCreateDefaultsShape()
    {
        var options = ApplicationItemDocumentPackageOptions.CreateDefaults();
        options.IncludeDiplomaFiles = false;
        options.IncludePassportCopies = false;
        options.IncludeMergedDiplomaPdf = true;
        options.DiplomaScope = PdfBatchDiplomaScope.CurrentEducationOnly;
        options.SupportingZipMergeOption = PdfSupportingZipMergeOption.IndividualFilesOnly;

        options.ResetToDefaults();

        var defaults = ApplicationItemDocumentPackageOptions.CreateDefaults();
        Assert.Equal(defaults.IncludeDiplomaFiles, options.IncludeDiplomaFiles);
        Assert.Equal(defaults.DiplomaScope, options.DiplomaScope);
        Assert.Equal(defaults.SupportingZipMergeOption, options.SupportingZipMergeOption);
        Assert.Equal(defaults.IncludeMergedDiplomaPdf, options.IncludeMergedDiplomaPdf);
        Assert.Equal(defaults.IncludePassportCopies, options.IncludePassportCopies);
        Assert.Equal(defaults.IncludeFamilyRelationshipCopies, options.IncludeFamilyRelationshipCopies);
    }
}
