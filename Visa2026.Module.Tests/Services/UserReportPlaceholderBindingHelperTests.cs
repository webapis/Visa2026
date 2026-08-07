using Visa2026.Module.Services.UserReports;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public class UserReportPlaceholderBindingHelperTests
{
    [Theory]
    [InlineData("IMAGE:Person_Photo", "Person_Photo")]
    [InlineData("image:Person_Photo", "Person_Photo")]
    [InlineData("Person_Photo:img(w:35mm)", "Person_Photo")]
    [InlineData("IMAGE:Person_Photo:img(w:35mm)", "Person_Photo")]
    [InlineData("  Passport_Number  ", "Passport_Number")]
    public void StripFormatterSuffix_NormalizesInjectorAndImgSuffix(string input, string expected)
    {
        Assert.Equal(expected, UserReportPlaceholderBindingHelper.StripFormatterSuffix(input));
    }

    [Fact]
    public void StripFormatterSuffix_NullOrWhitespace_PreservesEmpty()
    {
        Assert.Equal(string.Empty, UserReportPlaceholderBindingHelper.StripFormatterSuffix(null));
        Assert.Equal("   ", UserReportPlaceholderBindingHelper.StripFormatterSuffix("   "));
    }

    [Theory]
    [InlineData("IMAGE:Person_Photo", true)]
    [InlineData(" image:x ", true)]
    [InlineData("Person_Photo", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsImageInjectorToken_DetectsPrefix(string key, bool expected)
    {
        Assert.Equal(expected, UserReportPlaceholderBindingHelper.IsImageInjectorToken(key));
    }

    [Fact]
    public void CoerceMergeValue_KeepsNonEmptyPhotoBytes()
    {
        var bytes = new byte[] { 9, 8, 7 };
        var coerced = UserReportPlaceholderBindingHelper.CoerceMergeValue(bytes, "IMAGE:Person_Photo");
        Assert.Same(bytes, coerced);
    }

    [Fact]
    public void CoerceMergeValue_DropsEmptyOrMissingPhotoBytes()
    {
        Assert.Null(UserReportPlaceholderBindingHelper.CoerceMergeValue(Array.Empty<byte>(), "Person_Photo"));
        Assert.Null(UserReportPlaceholderBindingHelper.CoerceMergeValue("not-bytes", "Person_Photo:img(w:10mm)"));
        Assert.Null(UserReportPlaceholderBindingHelper.CoerceMergeValue(null, "Person_Photo"));
    }

    [Fact]
    public void CoerceMergeValue_TextPaths_UseEmptyStringForNull()
    {
        Assert.Equal(string.Empty, UserReportPlaceholderBindingHelper.CoerceMergeValue(null, "Passport_Number"));
        Assert.Equal("AB123", UserReportPlaceholderBindingHelper.CoerceMergeValue("AB123", "Passport_Number"));
    }
}
