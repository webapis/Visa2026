#nullable enable

using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Tests.TestSupport;
using Xunit;

namespace Visa2026.Module.Tests.BusinessObjects;

/// <summary>
/// Seretmezlik letter sequences share the company outgoing counter; MaxExclusionSequence
/// must respect null OS and prefix/year/month scope or numbers collide.
/// </summary>
public class ApplicationProfileInstanceExclusionNumberingMaxTests
{
    [Fact]
    public void MaxExclusionSequence_null_object_space_is_zero() =>
        Assert.Equal(0, ApplicationProfileInstanceExclusionNumbering.MaxExclusionSequence(
            null, "ÇE", 2026, 9, scopeByYear: true, scopeByMonth: true));

    [Fact]
    public void MaxExclusionSequence_uses_highest_matching_prefix_in_scope()
    {
        var space = PassthroughObjectSpaceStub.Create(stub =>
        {
            stub.Seed(
                new ApplicationProfileInstanceExclusion
                {
                    AppNumberPrefix = "ÇE",
                    Year = 2026,
                    Month = 9,
                    SequenceNumber = "0012",
                },
                new ApplicationProfileInstanceExclusion
                {
                    AppNumberPrefix = "ÇE",
                    Year = 2026,
                    Month = 9,
                    SequenceNumber = "0007",
                },
                new ApplicationProfileInstanceExclusion
                {
                    AppNumberPrefix = "ÇE",
                    Year = 2026,
                    Month = 8,
                    SequenceNumber = "0099",
                },
                new ApplicationProfileInstanceExclusion
                {
                    AppNumberPrefix = "OTHER",
                    Year = 2026,
                    Month = 9,
                    SequenceNumber = "0500",
                });
        });

        Assert.Equal(12, ApplicationProfileInstanceExclusionNumbering.MaxExclusionSequence(
            space, "ÇE", 2026, 9, scopeByYear: true, scopeByMonth: true));
        Assert.Equal(99, ApplicationProfileInstanceExclusionNumbering.MaxExclusionSequence(
            space, "ÇE", 2026, 8, scopeByYear: true, scopeByMonth: true));
        Assert.Equal(0, ApplicationProfileInstanceExclusionNumbering.MaxExclusionSequence(
            space, "MISSING", 2026, 9, scopeByYear: true, scopeByMonth: true));
    }

    [Fact]
    public void MaxExclusionSequence_ignores_unparseable_sequence_as_zero()
    {
        var space = PassthroughObjectSpaceStub.Create(stub =>
        {
            stub.Seed(new ApplicationProfileInstanceExclusion
            {
                AppNumberPrefix = "ÇE",
                Year = 2026,
                Month = 9,
                SequenceNumber = "not-a-number",
            });
        });

        Assert.Equal(0, ApplicationProfileInstanceExclusionNumbering.MaxExclusionSequence(
            space, "ÇE", 2026, 9, scopeByYear: true, scopeByMonth: true));
    }
}
