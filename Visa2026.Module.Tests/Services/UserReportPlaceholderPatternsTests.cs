using Visa2026.Module.Services.UserReports;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public sealed class UserReportPlaceholderPatternsTests
{
    [Theory]
    [InlineData("{{Name}}", "Name")]
    [InlineData("{{#Items}}", "#Items")]
    [InlineData("{{/Items}}", "/Items")]
    [InlineData("{{.property}}", ".property")]
    public void PlaceholderRegex_CapturesInnerToken(string input, string expectedInner)
    {
        var match = UserReportPlaceholderPatterns.PlaceholderRegex.Match(input);

        Assert.True(match.Success);
        Assert.Equal(expectedInner, match.Groups[1].Value);
    }

    [Fact]
    public void PlaceholderRegex_FindsMultipleTokensInTemplateFragment()
    {
        const string fragment = "Hello {{Person.FullName}} — count {{#Items}}{{.Name}}{{/Items}}";
        var matches = UserReportPlaceholderPatterns.PlaceholderRegex.Matches(fragment);

        Assert.Equal(4, matches.Count);
        Assert.Equal("Person.FullName", matches[0].Groups[1].Value);
        Assert.Equal("#Items", matches[1].Groups[1].Value);
        Assert.Equal(".Name", matches[2].Groups[1].Value);
        Assert.Equal("/Items", matches[3].Groups[1].Value);
    }

    [Theory]
    [InlineData("no braces")]
    [InlineData("{single}")]
    [InlineData("")]
    public void PlaceholderRegex_NonPlaceholders_DoNotMatch(string input)
    {
        Assert.DoesNotMatch(UserReportPlaceholderPatterns.PlaceholderRegex, input);
    }
}
