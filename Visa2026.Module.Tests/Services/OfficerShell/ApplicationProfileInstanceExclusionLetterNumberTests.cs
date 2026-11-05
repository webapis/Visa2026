using System;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.OfficerShell;
using Xunit;

namespace Visa2026.Module.Tests.Services.OfficerShell;

public class ApplicationProfileInstanceExclusionLetterNumberTests
{
    [Fact]
    public void FormatSeretmezlikLetterNumber_prefers_full_then_application_then_id_prefix()
    {
        var id = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var withFull = new ApplicationProfileInstance
        {
            ID = id,
            FullApplicationNumber = "  SER-9  ",
            ApplicationNumber = "APP-1",
        };
        Assert.Equal(
            "SER-9",
            ApplicationProfileInstanceExclusionQueries.FormatSeretmezlikLetterNumber(withFull));

        withFull.FullApplicationNumber = " ";
        Assert.Equal(
            "APP-1",
            ApplicationProfileInstanceExclusionQueries.FormatSeretmezlikLetterNumber(withFull));

        withFull.ApplicationNumber = null;
        Assert.Equal(
            id.ToString("N")[..8],
            ApplicationProfileInstanceExclusionQueries.FormatSeretmezlikLetterNumber(withFull));
    }

    [Fact]
    public void CountRemainingActiveOnTarget_returns_zero_without_object_space()
    {
        var target = new ApplicationProfileInstance
        {
            ID = Guid.NewGuid(),
            People =
            [
                new Person { ID = Guid.NewGuid() },
                new Person { ID = Guid.NewGuid() },
            ],
        };

        Assert.Equal(
            0,
            ApplicationProfileInstanceExclusionQueries.CountRemainingActiveOnTarget(null!, target));
    }
}
