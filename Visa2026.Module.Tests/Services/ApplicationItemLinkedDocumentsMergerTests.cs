using Visa2026.Module.Services.ApplicationItemLinkedDocuments;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public sealed class ApplicationItemLinkedDocumentsMergerTests
{
    private static ApplicationItemLinkedDocumentFile File(Guid id, bool hasContent = true) =>
        new()
        {
            FileDataId = id,
            DocumentRowId = Guid.NewGuid(),
            FileName = "scan.pdf",
            SizeBytes = 10,
            HasContent = hasContent
        };

    private static ApplicationItemLinkedDocumentGroup Group(
        string slotKey,
        string slotLabel,
        bool linkMissing = false,
        params ApplicationItemLinkedDocumentFile[] files) =>
        new()
        {
            SlotKey = slotKey,
            SlotLabel = slotLabel,
            LinkMissing = linkMissing,
            Files = files
        };

    private static ApplicationItemLinkedDocumentsLineSnapshot Line(
        Guid id,
        string label,
        params ApplicationItemLinkedDocumentGroup[] groups) =>
        new()
        {
            ApplicationItemId = id,
            LineLabel = label,
            Groups = groups
        };

    [Fact]
    public void MergeBySlot_NullOrEmpty_ReturnsEmpty()
    {
        Assert.Empty(ApplicationItemLinkedDocumentsMerger.MergeBySlot(null!));
        Assert.Empty(ApplicationItemLinkedDocumentsMerger.MergeBySlot(Array.Empty<ApplicationItemLinkedDocumentsLineSnapshot>()));
    }

    [Fact]
    public void MergeBySlot_SkipsEmptyApplicationItemIdAndBlankSlotKeys()
    {
        var lineId = Guid.NewGuid();
        var fileId = Guid.NewGuid();
        var lines = new[]
        {
            Line(Guid.Empty, "bad", Group("Passport.Current", "Passport", files: File(fileId))),
            Line(lineId, "ok", Group("  ", "blank", files: File(fileId))),
            Line(lineId, "ok", Group("Passport.Current", "Passport", files: File(fileId)))
        };

        var merged = ApplicationItemLinkedDocumentsMerger.MergeBySlot(lines);

        Assert.Single(merged);
        Assert.Equal("Passport.Current", merged[0].SlotKey);
        Assert.Single(merged[0].Files);
    }

    [Fact]
    public void MergeBySlot_PreservesFirstSeenSlotOrderAndLabel()
    {
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var lines = new[]
        {
            Line(a, "A",
                Group("Visa.Current", "Visa label", files: File(Guid.NewGuid())),
                Group("Passport.Current", "Passport A", files: File(Guid.NewGuid()))),
            Line(b, "B",
                Group("Passport.Current", "Passport B ignored label", files: File(Guid.NewGuid())),
                Group("Education.Current", "Diploma", files: File(Guid.NewGuid())))
        };

        var merged = ApplicationItemLinkedDocumentsMerger.MergeBySlot(lines);

        Assert.Equal(new[] { "Visa.Current", "Passport.Current", "Education.Current" }, merged.Select(g => g.SlotKey));
        Assert.Equal("Passport A", merged[1].SlotLabel);
        Assert.Equal(2, merged[1].InScopeLineCount);
        Assert.Equal(2, merged[1].LinesWithFilesCount);
        Assert.Equal(2, merged[1].Files.Count);
    }

    [Fact]
    public void MergeBySlot_IgnoresFilesWithoutContentOrEmptyFileDataId()
    {
        var lineId = Guid.NewGuid();
        var goodId = Guid.NewGuid();
        var lines = new[]
        {
            Line(lineId, "A", Group(
                "Passport.Current",
                "Passport",
                files:
                [
                    File(Guid.Empty),
                    File(Guid.NewGuid(), hasContent: false),
                    File(goodId)
                ]))
        };

        var merged = ApplicationItemLinkedDocumentsMerger.MergeBySlot(lines);

        Assert.Single(merged);
        Assert.Single(merged[0].Files);
        Assert.Equal(goodId, merged[0].Files[0].File.FileDataId);
        Assert.Empty(merged[0].MissingLines);
    }

    [Fact]
    public void MergeBySlot_MarksMissingLinesAndAggregatesLinkMissingOnlyWhenAllMissingAreLinkMissing()
    {
        var withFile = Guid.NewGuid();
        var missingFk = Guid.NewGuid();
        var missingScan = Guid.NewGuid();
        var lines = new[]
        {
            Line(withFile, "Has file", Group("Passport.Current", "Passport", files: File(Guid.NewGuid()))),
            Line(missingFk, "No FK", Group("Passport.Current", "Passport", linkMissing: true)),
            Line(missingScan, "No scan", Group("Passport.Current", "Passport", linkMissing: false))
        };

        var merged = ApplicationItemLinkedDocumentsMerger.MergeBySlot(lines);

        Assert.Single(merged);
        Assert.False(merged[0].LinkMissing);
        Assert.Equal(3, merged[0].InScopeLineCount);
        Assert.Equal(1, merged[0].LinesWithFilesCount);
        Assert.Equal(2, merged[0].MissingLines.Count);
        Assert.Contains(merged[0].MissingLines, m => m.ApplicationItemId == missingFk && m.LinkMissing);
        Assert.Contains(merged[0].MissingLines, m => m.ApplicationItemId == missingScan && !m.LinkMissing);
    }

    [Fact]
    public void MergeBySlot_LinkMissingTrue_WhenEveryInScopeLineMissingFkAndNoFiles()
    {
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var lines = new[]
        {
            Line(a, "A", Group("Visa.Current", "Visa", linkMissing: true)),
            Line(b, "B", Group("Visa.Current", "Visa", linkMissing: true))
        };

        var merged = ApplicationItemLinkedDocumentsMerger.MergeBySlot(lines);

        Assert.Single(merged);
        Assert.True(merged[0].LinkMissing);
        Assert.Empty(merged[0].Files);
        Assert.Equal(2, merged[0].MissingLines.Count);
        Assert.Equal(0, merged[0].LinesWithFilesCount);
    }
}
