using Xunit;

namespace Visa2026.DataImporter.Legacy.Visa2014;

public class Visa2014ActualPositionReviewTests
{
    [Theory]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData("-", false)]
    [InlineData("617-", false)] // placeholder — Guess=no-letters path
    [InlineData("Süpervizör", false)]
    [InlineData("Task / description", true)]
    [InlineData("A & B crew", true)]
    [InlineData("Ends with period.", true)]
    public void LooksLikeNonTitle_Heuristic(string? name, bool expected)
    {
        Assert.Equal(expected, Visa2014ActualPositionReview.LooksLikeNonTitle(name!));
    }

    [Fact]
    public void LooksLikeNonTitle_LongNameIsReview()
    {
        var longName = new string('a', 46);
        Assert.True(Visa2014ActualPositionReview.LooksLikeNonTitle(longName));
        Assert.False(Visa2014ActualPositionReview.LooksLikeNonTitle(new string('a', 45)));
    }

    [Fact]
    public void ParseCsvLine_HandlesQuotesAndEscapedQuotes()
    {
        var fields = Visa2014ActualPositionReview.ParseCsvLine("x,\"Name, with comma\",\"He said \"\"hi\"\"\",1,aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

        Assert.Equal(5, fields.Count);
        Assert.Equal("x", fields[0]);
        Assert.Equal("Name, with comma", fields[1]);
        Assert.Equal("He said \"hi\"", fields[2]);
        Assert.Equal("1", fields[3]);
        Assert.Equal("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa", fields[4]);
    }

    [Theory]
    [InlineData("plain", "plain")]
    [InlineData("a,b", "\"a,b\"")]
    [InlineData("say \"hi\"", "\"say \"\"hi\"\"\"")]
    public void Csv_QuotesWhenNeeded(string raw, string expected)
    {
        Assert.Equal(expected, Visa2014ActualPositionReview.Csv(raw));
    }
}
