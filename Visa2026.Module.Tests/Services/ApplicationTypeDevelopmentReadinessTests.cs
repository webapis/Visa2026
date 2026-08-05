using Visa2026.Module.Services;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public sealed class ApplicationTypeDevelopmentReadinessTests
{
    [Theory]
    [InlineData("App_Inv", "101", ApplicationTypeReadinessStatus.Ready)]
    [InlineData("App_Reg_Check_In", "301", ApplicationTypeReadinessStatus.Ready)]
    [InlineData("App_Exit_Visa", "703", ApplicationTypeReadinessStatus.Ready)]
    [InlineData("App_Cancel_Visa", "807", ApplicationTypeReadinessStatus.Ready)]
    public void GetStatus_ReadyByNameOrCode(string name, string code, ApplicationTypeReadinessStatus expected)
    {
        Assert.Equal(expected, ApplicationTypeDevelopmentReadiness.GetStatus(name, code));
        Assert.Equal(expected, ApplicationTypeDevelopmentReadiness.GetStatus(name, null));
        Assert.Equal(expected, ApplicationTypeDevelopmentReadiness.GetStatus(null, code));
    }

    [Theory]
    [InlineData("App_Visa_Ext", "702")]
    [InlineData("App_Visa_Ext", null)]
    [InlineData(null, "702")]
    public void GetStatus_HiddenDeprecated_IsNotReady(string name, string code)
    {
        Assert.True(ApplicationTypeDevelopmentReadiness.IsHiddenFromTypeCodePicker(name, code));
        Assert.Equal(
            ApplicationTypeReadinessStatus.NotReady,
            ApplicationTypeDevelopmentReadiness.GetStatus(name, code));
        Assert.False(ApplicationTypeDevelopmentReadiness.CanSelectOnApplicationForm(
            ApplicationTypeDevelopmentReadiness.GetStatus(name, code)));
    }

    [Theory]
    [InlineData("App_Definitely_Not_In_Map", null)]
    [InlineData("App_Definitely_Not_In_Map", "99")]
    [InlineData("App_Definitely_Not_In_Map", "abc")]
    [InlineData(null, null)]
    public void GetStatus_UnknownNameWithoutValidThreeDigitCode_IsNotReady(string name, string code)
    {
        Assert.Equal(
            ApplicationTypeReadinessStatus.NotReady,
            ApplicationTypeDevelopmentReadiness.GetStatus(name, code));
    }

    [Fact]
    public void GetStatus_UserDefinedVariant_ValidCodeUnknownName_IsPending()
    {
        // 109 is in invitation hundreds group but not a seed code; Name is not in seed map.
        var status = ApplicationTypeDevelopmentReadiness.GetStatus("App_Inv_Custom_Clone", "109");
        Assert.Equal(ApplicationTypeReadinessStatus.Pending, status);
        Assert.True(ApplicationTypeDevelopmentReadiness.CanSelectOnApplicationForm(status));
    }

    [Theory]
    [InlineData(ApplicationTypeReadinessStatus.Ready, true)]
    [InlineData(ApplicationTypeReadinessStatus.Pending, true)]
    [InlineData(ApplicationTypeReadinessStatus.NotReady, false)]
    public void CanSelectOnApplicationForm_AllowsReadyAndPendingOnly(
        ApplicationTypeReadinessStatus status,
        bool expected)
    {
        Assert.Equal(expected, ApplicationTypeDevelopmentReadiness.CanSelectOnApplicationForm(status));
    }

    [Fact]
    public void GetStatus_IsCaseInsensitiveOnNameAndCode()
    {
        Assert.Equal(
            ApplicationTypeReadinessStatus.Ready,
            ApplicationTypeDevelopmentReadiness.GetStatus("app_inv", "101"));
        Assert.True(ApplicationTypeDevelopmentReadiness.IsHiddenFromTypeCodePicker("APP_VISA_EXT", "702"));
    }
}
