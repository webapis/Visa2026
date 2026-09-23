using Visa2026.Module.BusinessObjects;
using Xunit;

namespace Visa2026.Module.Tests.BusinessObjects;

/// <summary>
/// Visa-document cancellation templates must not require Education;
/// extension-request cancels (<c>cancel_visa_ext</c>) keep the catalog flag.
/// </summary>
public sealed class ApplicationProfileEducationPolicyTests
{
    [Fact]
    public void AllowsPersonEducation_null_is_false() =>
        Assert.False(ApplicationProfileEducationPolicy.AllowsPersonEducation(null));

    [Theory]
    [InlineData("cancel_visa", true)]
    [InlineData("CANCEL_VISA", true)]
    [InlineData("cancel_visa_wp", true)]
    [InlineData("Cancel_Visa_Wp", true)]
    [InlineData("cancel_visa_ext", false)]
    [InlineData("get_invitation", false)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData(null, false)]
    public void CodeLooksLikeVisaDocumentCancellation(string? code, bool expected) =>
        Assert.Equal(expected, ApplicationProfileEducationPolicy.CodeLooksLikeVisaDocumentCancellation(code));

    [Fact]
    public void IsVisaDocumentCancellation_true_when_CancelVisas_flag_set() =>
        Assert.True(ApplicationProfileEducationPolicy.IsVisaDocumentCancellation(
            cancelVisas: true,
            code: "get_invitation"));

    [Fact]
    public void IsVisaDocumentCancellation_true_from_cancel_visa_code_without_flag() =>
        Assert.True(ApplicationProfileEducationPolicy.IsVisaDocumentCancellation(
            cancelVisas: false,
            code: "cancel_visa"));

    [Fact]
    public void AllowsPersonEducation_false_for_cancel_visa_profile()
    {
        var profile = new ApplicationProfile
        {
            Code = "cancel_visa",
            CancelVisas = false,
            RequirePersonEducation = true,
        };
        Assert.False(ApplicationProfileEducationPolicy.AllowsPersonEducation(profile));
    }

    [Fact]
    public void AllowsPersonEducation_true_for_cancel_visa_ext()
    {
        var profile = new ApplicationProfile
        {
            Code = "cancel_visa_ext",
            CancelVisas = false,
            RequirePersonEducation = true,
        };
        Assert.True(ApplicationProfileEducationPolicy.AllowsPersonEducation(profile));
    }

    [Fact]
    public void AllowsPersonEducation_true_for_ordinary_issuance()
    {
        var profile = new ApplicationProfile
        {
            Code = "get_invitation",
            ActionFamily = ApplicationProfileActionFamily.Issuance,
        };
        Assert.True(ApplicationProfileEducationPolicy.AllowsPersonEducation(profile));
    }
}
