using Visa2026.Module.Services.UserReports;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public class FamilyMemberSponsorPhraseTests
{
    [Fact]
    public void Format_SingleDependent_MatchesLetterShape()
    {
        var text = FamilyMemberSponsorPhrase.Format(
            "adamsynyň",
            "İzzet Taşdelen",
            "İşe goýberiş boýunça mehanik-tehnigi");

        Assert.Equal(
            "adamsynyň (İzzet Taşdelen-İşe goýberiş boýunça mehanik-tehnigi)",
            text);
    }

    [Fact]
    public void Format_MultipleRelationships_JoinsOutsideParens()
    {
        var text = FamilyMemberSponsorPhrase.Format(
            "aýalynyň we çagasynyň",
            "İzzet Taşdelen",
            "İşe goýberiş boýunça mehanik-tehnigi");

        Assert.Equal(
            "aýalynyň we çagasynyň (İzzet Taşdelen-İşe goýberiş boýunça mehanik-tehnigi)",
            text);
    }

    [Fact]
    public void Format_Empty_ReturnsEmpty()
    {
        Assert.Equal(string.Empty, FamilyMemberSponsorPhrase.Format(null, null, null));
    }
}