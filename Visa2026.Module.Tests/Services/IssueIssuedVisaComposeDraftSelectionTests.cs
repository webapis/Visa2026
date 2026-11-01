using System;
using Visa2026.Module.Services.PreviewSlot;
using Xunit;

namespace Visa2026.Module.Tests.Services;

/// <summary>
/// Create draft people default Include=false (officer opt-in). SelectedUnusedCount must stay 0
/// until Include is flipped, even when passport-ready. Regression for 48c93bf4.
/// </summary>
public sealed class IssueIssuedVisaComposeDraftSelectionTests
{
    [Fact]
    public void SelectedUnusedCount_Zero_WhenReadyPeopleDefaultUnchecked()
    {
        var draft = new IssueIssuedVisaComposeDraft
        {
            Groups =
            [
                new IssueIssuedVisaInvitationGroupDraft
                {
                    InvitationId = Guid.NewGuid(),
                    InvitationNumber = "C1",
                    People =
                    [
                        new IssueIssuedVisaPersonCardDraft
                        {
                            PersonId = Guid.NewGuid(),
                            PersonName = "Ready A",
                            Include = false,
                            AlreadyIssued = false,
                            IsReady = true,
                            PassportId = Guid.NewGuid(),
                        },
                        new IssueIssuedVisaPersonCardDraft
                        {
                            PersonId = Guid.NewGuid(),
                            PersonName = "Ready B",
                            Include = false,
                            AlreadyIssued = false,
                            IsReady = true,
                            PassportId = Guid.NewGuid(),
                        },
                    ],
                },
            ],
        };

        Assert.True(draft.HasUnusedLine);
        Assert.Equal(0, draft.SelectedUnusedCount);
    }

    [Fact]
    public void SelectedUnusedCount_IgnoresAlreadyIssuedEvenWhenIncluded()
    {
        var draft = new IssueIssuedVisaComposeDraft
        {
            Groups =
            [
                new IssueIssuedVisaInvitationGroupDraft
                {
                    InvitationId = Guid.NewGuid(),
                    People =
                    [
                        new IssueIssuedVisaPersonCardDraft
                        {
                            PersonId = Guid.NewGuid(),
                            Include = true,
                            AlreadyIssued = true,
                            IsReady = true,
                        },
                        new IssueIssuedVisaPersonCardDraft
                        {
                            PersonId = Guid.NewGuid(),
                            Include = true,
                            AlreadyIssued = false,
                            IsReady = true,
                        },
                    ],
                },
            ],
        };

        Assert.Equal(1, draft.SelectedUnusedCount);
    }
}
