using System;
using Visa2026.Module.Services.ApplicationItemLinkedDocuments;
using Visa2026.Module.Services.ApplicationWorkspace;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public class ApplicationWorkspaceDocumentCopiesCompletenessTests
{
    [Fact]
    public void Resolve_no_people_is_empty_roster()
    {
        Assert.Equal(
            ApplicationWorkspaceDocumentCopiesCompleteness.NavStatus.EmptyRoster,
            ApplicationWorkspaceDocumentCopiesCompleteness.Resolve(hasPeople: false, summary: null));
        Assert.Equal(0, ApplicationWorkspaceDocumentCopiesCompleteness.MissingSlotCount(null));
        Assert.False(ApplicationWorkspaceDocumentCopiesCompleteness.IsSlotMissing(null));
    }

    [Fact]
    public void Resolve_gap_and_partial_slots_are_incomplete()
    {
        var summary = ApplicationItemDocumentCopiesReadinessSummary.Compute(
        [
            new ApplicationItemLinkedDocumentMergedGroup
            {
                SlotKey = "Visa.Current",
                Files = Array.Empty<ApplicationItemLinkedDocumentFileEntry>(),
                MissingLines =
                [
                    new ApplicationItemLinkedDocumentMissingLineEntry { ApplicationItemId = Guid.NewGuid() }
                ]
            },
            new ApplicationItemLinkedDocumentMergedGroup
            {
                SlotKey = "Passport.Current",
                Files =
                [
                    new ApplicationItemLinkedDocumentFileEntry
                    {
                        File = new ApplicationItemLinkedDocumentFile { HasContent = true, FileDataId = Guid.NewGuid() }
                    }
                ],
                MissingLines =
                [
                    new ApplicationItemLinkedDocumentMissingLineEntry { ApplicationItemId = Guid.NewGuid() }
                ]
            }
        ]);

        Assert.Equal(1, summary.GapSlotCount);
        Assert.Equal(1, summary.PartialSlotCount);
        Assert.Equal(2, ApplicationWorkspaceDocumentCopiesCompleteness.MissingSlotCount(summary));
        Assert.Equal(
            ApplicationWorkspaceDocumentCopiesCompleteness.NavStatus.Incomplete,
            ApplicationWorkspaceDocumentCopiesCompleteness.Resolve(hasPeople: true, summary));
        Assert.True(ApplicationWorkspaceDocumentCopiesCompleteness.IsSlotMissing(
            new ApplicationItemLinkedDocumentMergedGroup
            {
                SlotKey = "Passport.Current",
                Files =
                [
                    new ApplicationItemLinkedDocumentFileEntry
                    {
                        File = new ApplicationItemLinkedDocumentFile { HasContent = true }
                    }
                ],
                MissingLines =
                [
                    new ApplicationItemLinkedDocumentMissingLineEntry { ApplicationItemId = Guid.NewGuid() }
                ]
            }));
    }

    [Fact]
    public void Resolve_all_slots_ready_is_complete()
    {
        var summary = ApplicationItemDocumentCopiesReadinessSummary.Compute(
        [
            new ApplicationItemLinkedDocumentMergedGroup
            {
                SlotKey = "Passport.Current",
                Files =
                [
                    new ApplicationItemLinkedDocumentFileEntry
                    {
                        File = new ApplicationItemLinkedDocumentFile { HasContent = true, FileDataId = Guid.NewGuid() }
                    }
                ]
            }
        ]);

        Assert.Equal(0, ApplicationWorkspaceDocumentCopiesCompleteness.MissingSlotCount(summary));
        Assert.Equal(
            ApplicationWorkspaceDocumentCopiesCompleteness.NavStatus.Complete,
            ApplicationWorkspaceDocumentCopiesCompleteness.Resolve(hasPeople: true, summary));
        Assert.False(ApplicationWorkspaceDocumentCopiesCompleteness.IsSlotMissing(
            new ApplicationItemLinkedDocumentMergedGroup
            {
                SlotKey = "Passport.Current",
                Files =
                [
                    new ApplicationItemLinkedDocumentFileEntry
                    {
                        File = new ApplicationItemLinkedDocumentFile { HasContent = true }
                    }
                ]
            }));
    }
}