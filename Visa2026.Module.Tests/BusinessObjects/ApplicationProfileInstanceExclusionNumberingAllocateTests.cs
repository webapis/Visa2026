using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Tests.TestSupport;
using Xunit;

namespace Visa2026.Module.Tests.BusinessObjects;

/// <summary>
/// Seretmezlik Allocate shares the company outgoing counter with application numbers.
/// Wrong max(instance, exclusion, seed) causes duplicate LetterNumber / ApplicationNumber collisions.
/// </summary>
public sealed class ApplicationProfileInstanceExclusionNumberingAllocateTests
{
    [Fact]
    public void Allocate_null_args_throw()
    {
        Assert.Throws<ArgumentNullException>(() =>
            ApplicationProfileInstanceExclusionNumbering.Allocate(null!, new ApplicationProfileInstanceExclusion()));
        Assert.Throws<ArgumentNullException>(() =>
            ApplicationProfileInstanceExclusionNumbering.Allocate(
                MultiTypeSeedObjectSpaceStub.Create(), null!));
    }

    [Fact]
    public void Allocate_continues_from_highest_instance_and_exclusion_in_year_scope()
    {
        var numbering = new ApplicationNumberingProfile
        {
            Name = ApplicationNumberingProfile.DefaultProfileName,
            AppNumberPrefix = "CE",
            AppNumberFormat = "{PREFIX}{YEAR}-{NUMBER}",
            ApplicationNumberSeed = 5,
            ApplicationNumberPadding = 4,
        };

        var space = MultiTypeSeedObjectSpaceStub.Create(stub =>
        {
            stub.Seed(numbering);
            stub.Seed(
                new ApplicationProfileInstance
                {
                    AppNumberPrefix = "CE",
                    Year = 2026,
                    Month = 3,
                    ApplicationNumber = "0010",
                },
                new ApplicationProfileInstance
                {
                    AppNumberPrefix = "CE",
                    Year = 2025,
                    Month = 12,
                    ApplicationNumber = "0999",
                });
            stub.Seed(
                new ApplicationProfileInstanceExclusion
                {
                    AppNumberPrefix = "CE",
                    Year = 2026,
                    Month = 1,
                    SequenceNumber = "0012",
                },
                new ApplicationProfileInstanceExclusion
                {
                    AppNumberPrefix = "OTHER",
                    Year = 2026,
                    Month = 1,
                    SequenceNumber = "0500",
                });
        });

        var exclusion = new ApplicationProfileInstanceExclusion
        {
            LetterDate = new DateTime(2026, 9, 15),
        };

        ApplicationProfileInstanceExclusionNumbering.Allocate(space, exclusion);

        Assert.Equal(2026, exclusion.Year);
        Assert.Equal(9, exclusion.Month);
        Assert.Equal("CE", exclusion.AppNumberPrefix);
        // max(instance 10 in 2026, exclusion 12 in 2026, seed 5) + 1
        Assert.Equal("0013", exclusion.SequenceNumber);
        Assert.Equal("CE2026-0013", exclusion.LetterNumber);
    }

    [Fact]
    public void Allocate_month_token_scopes_max_to_same_month()
    {
        var numbering = new ApplicationNumberingProfile
        {
            Name = ApplicationNumberingProfile.DefaultProfileName,
            AppNumberPrefix = "TM",
            AppNumberFormat = "{PREFIX}{YEAR}{MONTH2}-{NUMBER}",
            ApplicationNumberSeed = 0,
            ApplicationNumberPadding = 3,
        };

        var space = MultiTypeSeedObjectSpaceStub.Create(stub =>
        {
            stub.Seed(numbering);
            stub.Seed(new ApplicationProfileInstance
            {
                AppNumberPrefix = "TM",
                Year = 2026,
                Month = 8,
                ApplicationNumber = "040",
            });
            stub.Seed(new ApplicationProfileInstanceExclusion
            {
                AppNumberPrefix = "TM",
                Year = 2026,
                Month = 9,
                SequenceNumber = "007",
            });
        });

        var exclusion = new ApplicationProfileInstanceExclusion
        {
            LetterDate = new DateTime(2026, 9, 1),
        };

        ApplicationProfileInstanceExclusionNumbering.Allocate(space, exclusion);

        Assert.Equal("TM", exclusion.AppNumberPrefix);
        Assert.Equal(2026, exclusion.Year);
        Assert.Equal(9, exclusion.Month);
        // August instance 40 is out of month scope; September exclusion 7 + 1
        Assert.Equal("008", exclusion.SequenceNumber);
        Assert.Equal("TM202609-008", exclusion.LetterNumber);
    }

    [Fact]
    public void Allocate_uses_today_when_letter_date_default()
    {
        var today = DateTime.Today;
        var numbering = new ApplicationNumberingProfile
        {
            Name = ApplicationNumberingProfile.DefaultProfileName,
            AppNumberPrefix = "X",
            AppNumberFormat = "{PREFIX}{NUMBER}",
            ApplicationNumberSeed = 2,
            ApplicationNumberPadding = 2,
        };

        var space = MultiTypeSeedObjectSpaceStub.Create(stub => stub.Seed(numbering));
        var exclusion = new ApplicationProfileInstanceExclusion { LetterDate = default };

        ApplicationProfileInstanceExclusionNumbering.Allocate(space, exclusion);

        Assert.Equal(today.Year, exclusion.Year);
        Assert.Equal(today.Month, exclusion.Month);
        Assert.Equal("X", exclusion.AppNumberPrefix);
        // format has no YEAR/MONTH → scope is prefix-only; seed 2 + 1
        Assert.Equal("03", exclusion.SequenceNumber);
        Assert.Equal("X03", exclusion.LetterNumber);
    }
}
