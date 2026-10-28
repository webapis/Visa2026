using Visa2026.Module.Services.EmailDocumentCopies;
using Xunit;

namespace Visa2026.Module.Tests.Services.EmailDocumentCopies;

public class OfficerMailSecretProtectorPrefixTests
{
    private sealed class StubProtector : IOfficerMailSecretProtector
    {
        public string Protect(string plaintext) => IOfficerMailSecretProtector.Prefix + plaintext;
        public string? Unprotect(string? protectedValue) => protectedValue;
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData("plain-password", false)]
    [InlineData("dp:v1:", true)]
    [InlineData("dp:v1:cipher", true)]
    [InlineData("DP:v1:cipher", false)]
    public void IsProtected_RequiresExactPrefix(string? value, bool expected)
    {
        IOfficerMailSecretProtector protector = new StubProtector();
        Assert.Equal(expected, protector.IsProtected(value));
        Assert.Equal("dp:v1:", IOfficerMailSecretProtector.Prefix);
    }
}
