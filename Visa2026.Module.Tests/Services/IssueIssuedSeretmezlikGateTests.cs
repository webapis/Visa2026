using System;
using System.Collections.Generic;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.PreviewSlot;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public class IssueIssuedSeretmezlikGateTests
{
    [Fact]
    public void IsExcluded_RequiresNonEmptyIdInSet()
    {
        var id = Guid.NewGuid();
        var set = new HashSet<Guid> { id };
        Assert.True(IssueIssuedSeretmezlikGate.IsExcluded(id, set));
        Assert.False(IssueIssuedSeretmezlikGate.IsExcluded(Guid.Empty, set));
        Assert.False(IssueIssuedSeretmezlikGate.IsExcluded(id, new HashSet<Guid>()));
    }

    [Fact]
    public void RejectIfSelectingExcluded_BlocksNewExcludedPeople()
    {
        var excludedId = Guid.NewGuid();
        var activeId = Guid.NewGuid();
        var letterId = Guid.NewGuid();
        var instance = new ApplicationProfileInstance
        {
            ID = Guid.NewGuid(),
            Exclusions =
            [
                new ApplicationProfileInstanceExclusion
                {
                    ID = letterId,
                    LetterNumber = "11/-001",
                    LetterDate = new DateTime(2026, 11, 4),
                    People =
                    [
                        new ApplicationProfileInstanceExclusionPerson
                        {
                            PersonId = excludedId,
                        },
                    ],
                },
            ],
        };

        var blocked = IssueIssuedSeretmezlikGate.RejectIfSelectingExcluded(
            objectSpace: null,
            instance,
            [
                (excludedId, null, "Habip Excluded"),
                (activeId, null, "Ali Active"),
            ]);
        Assert.NotNull(blocked);
        Assert.Contains("Seretmezlik", blocked, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Habip Excluded", blocked, StringComparison.Ordinal);

        var allowedExisting = IssueIssuedSeretmezlikGate.RejectIfSelectingExcluded(
            objectSpace: null,
            instance,
            [
                (excludedId, Guid.NewGuid(), "Habip Excluded"),
            ]);
        Assert.Null(allowedExisting);

        var allowedActive = IssueIssuedSeretmezlikGate.RejectIfSelectingExcluded(
            objectSpace: null,
            instance,
            [
                (activeId, null, "Ali Active"),
            ]);
        Assert.Null(allowedActive);
    }
}