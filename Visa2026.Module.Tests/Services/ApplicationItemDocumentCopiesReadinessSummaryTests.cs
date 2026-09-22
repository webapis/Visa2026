using Visa2026.Module.Services;
using Visa2026.Module.Services.ApplicationItemLinkedDocuments;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public sealed class ApplicationItemDocumentCopiesReadinessSummaryTests
{
    private static ApplicationItemLinkedDocumentMergedGroup Group(
        string slotKey,
        int fileCount,
        int missingCount,
        bool linkMissing = false)
    {
        var files = Enumerable.Range(0, fileCount)
            .Select(_ => new ApplicationItemLinkedDocumentFileEntry
            {
                ApplicationItemId = Guid.NewGuid(),
                LineLabel = "line",
                File = new ApplicationItemLinkedDocumentFile
                {
                    FileDataId = Guid.NewGuid(),
                    HasContent = true,
                    FileName = "a.pdf"
                }
            })
            .ToList();

        var missing = Enumerable.Range(0, missingCount)
            .Select(_ => new ApplicationItemLinkedDocumentMissingLineEntry
            {
                ApplicationItemId = Guid.NewGuid(),
                LineLabel = "gap",
                LinkMissing = linkMissing
            })
            .ToList();

        return new ApplicationItemLinkedDocumentMergedGroup
        {
            SlotKey = slotKey,
            SlotLabel = slotKey,
            Files = files,
            MissingLines = missing,
            InScopeLineCount = fileCount + missingCount,
            LinesWithFilesCount = fileCount,
            LinkMissing = linkMissing && fileCount == 0
        };
    }

    [Fact]
    public void Compute_Defaults_CountApplicationFormAsReady()
    {
        var summary = ApplicationItemDocumentCopiesReadinessSummary.Compute(null);

        Assert.Equal(1, summary.ReadySlotCount);
        Assert.Equal(0, summary.PartialSlotCount);
        Assert.Equal(0, summary.GapSlotCount);
        Assert.False(summary.HasPackagingGaps);
    }

    [Fact]
    public void Compute_ClassifiesReadyPartialAndGap_ForIncludedSlots()
    {
        var groups = new[]
        {
            Group("Passport.Current", fileCount: 2, missingCount: 0),
            Group("Visa.Current", fileCount: 1, missingCount: 1),
            Group("MedicalRecord.Current", fileCount: 0, missingCount: 1),
            Group("Unknown.Slot", fileCount: 0, missingCount: 1) // excluded by package rules
        };

        var summary = ApplicationItemDocumentCopiesReadinessSummary.Compute(groups);

        // application form + Passport ready
        Assert.Equal(2, summary.ReadySlotCount);
        Assert.Equal(1, summary.PartialSlotCount);
        Assert.Equal(1, summary.GapSlotCount);
        Assert.True(summary.HasPackagingGaps);
    }

    [Fact]
    public void Compute_RespectsPackageOptions_AndOptionalApplicationFormSlot()
    {
        var options = ApplicationItemDocumentPackageOptions.CreateDefaults();
        options.IncludePassportCopies = false;
        options.IncludeVisaCopies = true;

        var groups = new[]
        {
            Group("Passport.Current", fileCount: 0, missingCount: 1),
            Group("Visa.Current", fileCount: 1, missingCount: 0)
        };

        var summary = ApplicationItemDocumentCopiesReadinessSummary.Compute(
            groups,
            options,
            includeApplicationFormSlot: false);

        Assert.Equal(1, summary.ReadySlotCount);
        Assert.Equal(0, summary.PartialSlotCount);
        Assert.Equal(0, summary.GapSlotCount);
        Assert.False(summary.HasPackagingGaps);
    }

    [Fact]
    public void Compute_EducationCurrent_FollowsIncludeDiplomaFiles()
    {
        var options = ApplicationItemDocumentPackageOptions.CreateDefaults();
        options.IncludeDiplomaFiles = false;

        var groups = new[]
        {
            Group("Education.Current", fileCount: 0, missingCount: 2)
        };

        var excluded = ApplicationItemDocumentCopiesReadinessSummary.Compute(groups, options);
        Assert.Equal(1, excluded.ReadySlotCount); // form only
        Assert.Equal(0, excluded.GapSlotCount);

        options.IncludeDiplomaFiles = true;
        var included = ApplicationItemDocumentCopiesReadinessSummary.Compute(groups, options);
        Assert.Equal(1, included.ReadySlotCount);
        Assert.Equal(1, included.GapSlotCount);
    }
}
