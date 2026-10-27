using Visa2026.Module.Services.EmailDocumentCopies;
using Xunit;

namespace Visa2026.Module.Tests.Services.EmailDocumentCopies;

public class EmailContactDirectoryValidationTests
{
    [Theory]
    [InlineData("officer@example.com", true)]
    [InlineData("  officer@example.com  ", true)]
    [InlineData("a@b.c", true)]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData("not-an-email", false)]
    [InlineData("missing-domain@", false)]
    [InlineData("@missing-local.com", false)]
    [InlineData("has space@example.com", false)]
    public void IsValidEmail_ChecksPatternAndLength(string? email, bool expected)
    {
        Assert.Equal(expected, EmailContactDirectory.IsValidEmail(email));
    }

    [Fact]
    public void IsValidEmail_RejectsOver320Characters()
    {
        // local(316) + "@x.co"(5) = 321 characters.
        var local = new string('a', 316);
        Assert.False(EmailContactDirectory.IsValidEmail(local + "@x.co"));
    }
}
