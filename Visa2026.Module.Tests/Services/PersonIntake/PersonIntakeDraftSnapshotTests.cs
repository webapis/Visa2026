using System;
using Visa2026.Module.Services.PersonIntake;
using Xunit;

namespace Visa2026.Module.Tests.Services.PersonIntake;

public class PersonIntakeDraftSnapshotTests
{
    [Fact]
    public void Restore_PutsBackScalarsAndCopyList()
    {
        var draft = new PersonIntakePassportDraft
        {
            PassportNumber = "A123",
            IssueDate = new DateTime(2020, 1, 2),
            IsCancelled = false
        };
        var kept = new PersonIntakeDocumentCopyDraft { FileName = "scan.pdf" };
        draft.Copies.Add(kept);
        var cardId = draft.CardId;

        var snapshot = PersonIntakeDraftSnapshot.Take(draft);
        draft.PassportNumber = "B999";
        draft.IssueDate = null;
        draft.IsCancelled = true;
        draft.Copies.Clear();
        draft.Copies.Add(new PersonIntakeDocumentCopyDraft { FileName = "other.pdf" });

        snapshot.Restore();

        Assert.Equal("A123", draft.PassportNumber);
        Assert.Equal(new DateTime(2020, 1, 2), draft.IssueDate);
        Assert.False(draft.IsCancelled);
        Assert.Same(kept, Assert.Single(draft.Copies));
        Assert.Equal(cardId, draft.CardId);
        Assert.True(snapshot.IsFor(draft));
    }

    [Fact]
    public void Restore_PositionOrderDocuments()
    {
        var draft = new PersonIntakePositionDraft { StartDate = new DateTime(2024, 5, 1) };
        var snapshot = PersonIntakeDraftSnapshot.Take(draft);
        draft.StartDate = null;
        draft.OrderDocuments.Add(new PersonIntakeDocumentCopyDraft { FileName = "order.pdf" });

        snapshot.Restore();

        Assert.Equal(new DateTime(2024, 5, 1), draft.StartDate);
        Assert.Empty(draft.OrderDocuments);
    }
}
