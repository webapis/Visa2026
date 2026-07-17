using Visa2026.DataImporter.Legacy.Visa2014;
using Xunit;

namespace Visa2026.DataImporter.Legacy.Visa2014.Tests;

public class Visa2014ApplicationVisaTypeInferenceTests
{
    [Theory]
    [InlineData("App_Inv_And_WP", "WP")]
    [InlineData("App_Visa_and_WP_Ext", "WP")]
    [InlineData("App_Inv_According_to_WP", "WP")]
    [InlineData("App_Visa_Ext_According_to_WP", "WP")]
    [InlineData("App_Inv", "BS1")]
    [InlineData("App_Inv_FM", "FM")]
    [InlineData("App_Visa_Ext_FM", "FM")]
    [InlineData("App_Visa_For_New_Born_FM", "FM")]
    [InlineData("App_Visa_Ext", "EX")]
    [InlineData("App_Exit_Visa", "EX")]
    [InlineData("App_Sevice_Passport", "OF")]
    public void TryInferVisaType_KnownApplicationTypes_ReturnExpectedKey(string applicationType, string expectedKey)
    {
        Assert.True(Visa2014ApplicationVisaTypeInference.TryInferVisaType(applicationType, out var key));
        Assert.Equal(expectedKey, key);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("App_Reg_Check_Out")]
    [InlineData("App_Change_Inv")]
    public void TryInferVisaType_UnknownOrNonPeriodTypes_ReturnFalse(string? applicationType)
    {
        Assert.False(Visa2014ApplicationVisaTypeInference.TryInferVisaType(applicationType, out var key));
        Assert.Equal(string.Empty, key);
    }
}
