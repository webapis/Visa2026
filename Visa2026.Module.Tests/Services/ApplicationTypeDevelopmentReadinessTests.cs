using Visa2026.Module.Services;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public sealed class ApplicationTypeDevelopmentReadinessTests
{
    [Theory]
    [InlineData("App_Inv", "101")]
    [InlineData("app_inv", null)]
    [InlineData(null, "601")]
    public void GetStatus_ReadyByNameOrCode_ReturnsReady(string? name, string? selectionCode)
    {
        Assert.Equal(
            ApplicationTypeReadinessStatus.Ready,
            ApplicationTypeDevelopmentReadiness.GetStatus(name, selectionCode));
        Assert.True(ApplicationTypeDevelopmentReadiness.CanSelectOnApplicationForm(
            ApplicationTypeReadinessStatus.Ready));
    }

    [Theory]
    [InlineData("App_Visa_Ext", "702")]
    [InlineData("App_Visa_Ext", null)]
    [InlineData(null, "702")]
    public void GetStatus_HiddenDeprecated_ReturnsNotReady(string? name, string? selectionCode)
    {
        Assert.True(ApplicationTypeDevelopmentReadiness.IsHiddenFromTypeCodePicker(name, selectionCode));
        Assert.Equal(
            ApplicationTypeReadinessStatus.NotReady,
            ApplicationTypeDevelopmentReadiness.GetStatus(name, selectionCode));
        Assert.False(ApplicationTypeDevelopmentReadiness.CanSelectOnApplicationForm(
            ApplicationTypeReadinessStatus.NotReady));
    }

    [Fact]
    public void GetStatus_UnknownNameAndCode_ReturnsNotReady()
    {
        Assert.Equal(
            ApplicationTypeReadinessStatus.NotReady,
            ApplicationTypeDevelopmentReadiness.GetStatus("Not_A_Real_Type", null));
    }

    [Fact]
    public void GetStatus_UserDefinedVariantWithValidCode_ReturnsPending()
    {
        Assert.Equal(
            ApplicationTypeReadinessStatus.Pending,
            ApplicationTypeDevelopmentReadiness.GetStatus("Custom_Clone_Type", "199"));
        Assert.True(ApplicationTypeDevelopmentReadiness.CanSelectOnApplicationForm(
            ApplicationTypeReadinessStatus.Pending));
    }

    [Theory]
    [InlineData("Custom_Clone_Type", null)]
    [InlineData("Custom_Clone_Type", "")]
    [InlineData("Custom_Clone_Type", "19")]
    [InlineData("Custom_Clone_Type", "abcd")]
    public void GetStatus_UserDefinedNameWithoutValidCode_ReturnsNotReady(string? name, string? selectionCode)
    {
        Assert.Equal(
            ApplicationTypeReadinessStatus.NotReady,
            ApplicationTypeDevelopmentReadiness.GetStatus(name, selectionCode));
    }

    [Fact]
    public void CanSelectOnApplicationForm_OnlyReadyAndPending()
    {
        Assert.True(ApplicationTypeDevelopmentReadiness.CanSelectOnApplicationForm(
            ApplicationTypeReadinessStatus.Ready));
        Assert.True(ApplicationTypeDevelopmentReadiness.CanSelectOnApplicationForm(
            ApplicationTypeReadinessStatus.Pending));
        Assert.False(ApplicationTypeDevelopmentReadiness.CanSelectOnApplicationForm(
            ApplicationTypeReadinessStatus.NotReady));
    }
}
