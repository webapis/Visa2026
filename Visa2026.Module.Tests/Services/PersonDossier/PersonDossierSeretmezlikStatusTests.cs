using System;
using Visa2026.Module.Services.PersonDossier;
using Xunit;

namespace Visa2026.Module.Tests.Services.PersonDossier;

public class PersonDossierSeretmezlikStatusTests
{
    [Fact]
    public void FormatExcludedStatus_matches_people_links_badge_shape()
    {
        var label = PersonDossierResolver.FormatExcludedStatus("01/-10", new DateTime(2026, 9, 24));

        Assert.Contains("01/-10", label, StringComparison.Ordinal);
        Assert.Contains("24.09.2026", label, StringComparison.Ordinal);
        Assert.DoesNotContain("PersonDossier.", label, StringComparison.Ordinal);
    }
}
