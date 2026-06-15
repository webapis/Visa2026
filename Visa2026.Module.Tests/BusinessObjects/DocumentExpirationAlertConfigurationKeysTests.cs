using Visa2026.Module.BusinessObjects;
using Xunit;

namespace Visa2026.Module.Tests.BusinessObjects;

public class DocumentExpirationAlertConfigurationKeysTests
{
    [Fact]
    public void All_ContainsSixConfigurationFamilies()
    {
        Assert.Equal(6, DocumentExpirationAlertConfigurationKeys.All.Count);
        Assert.Contains(ExpirationAlertBusinessObjectKeys.Passport, DocumentExpirationAlertConfigurationKeys.All);
        Assert.Contains(ExpirationAlertBusinessObjectKeys.Visa, DocumentExpirationAlertConfigurationKeys.All);
        Assert.Contains(ExpirationAlertBusinessObjectKeys.WorkPermitItem, DocumentExpirationAlertConfigurationKeys.All);
        Assert.Contains(ExpirationAlertBusinessObjectKeys.AddressOfResidence, DocumentExpirationAlertConfigurationKeys.All);
        Assert.Contains(ExpirationAlertBusinessObjectKeys.MedicalRecord, DocumentExpirationAlertConfigurationKeys.All);
        Assert.Contains(ExpirationAlertBusinessObjectKeys.Invitation, DocumentExpirationAlertConfigurationKeys.All);
        Assert.DoesNotContain(ExpirationAlertBusinessObjectKeys.BorderZone, DocumentExpirationAlertConfigurationKeys.All);
    }

    [Theory]
    [InlineData(ExpirationAlertBusinessObjectKeys.Visa, true)]
    [InlineData(ExpirationAlertBusinessObjectKeys.WorkPermitItem, true)]
    [InlineData(ExpirationAlertBusinessObjectKeys.Passport, false)]
    [InlineData(ExpirationAlertBusinessObjectKeys.Invitation, false)]
    public void SupportsExtensionApplicationRequiredDays_MatchesVisaAndWorkPermitOnly(
        string businessObjectKey,
        bool expected)
    {
        Assert.Equal(expected, DocumentExpirationAlertConfigurationKeys.SupportsExtensionApplicationRequiredDays(businessObjectKey));
    }

    [Fact]
    public void ListViewCriteria_IncludesAllConfigurationKeys()
    {
        var criteria = DocumentExpirationAlertConfigurationKeys.ListViewCriteria;

        foreach (string key in DocumentExpirationAlertConfigurationKeys.All)
            Assert.Contains($"'{key}'", criteria);
    }
}
