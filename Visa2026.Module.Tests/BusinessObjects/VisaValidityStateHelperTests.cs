using Visa2026.Module.BusinessObjects;
using Xunit;

namespace Visa2026.Module.Tests.BusinessObjects;

public class VisaValidityStateHelperTests
{
    [Fact]
    public void Resolve_NullVisa_ReturnsValid()
    {
        Assert.Equal(VisaValidityState.Valid, VisaValidityStateHelper.Resolve(null));
    }

    [Fact]
    public void Resolve_Cancelled_TakesPriorityOverExpiration()
    {
        var visa = new Visa
        {
            IsCancelled = true,
            ExpirationDate = DateTime.Today.AddDays(-10)
        };

        Assert.Equal(VisaValidityState.Cancelled, VisaValidityStateHelper.Resolve(visa));
    }

    [Fact]
    public void Resolve_Changed_TakesPriorityOverExpiration()
    {
        var visa = new Visa
        {
            IsChanged = true,
            ExpirationDate = DateTime.Today.AddDays(-3)
        };

        Assert.Equal(VisaValidityState.Changed, VisaValidityStateHelper.Resolve(visa));
    }

    [Fact]
    public void Resolve_Expired_WhenPastExpirationAndNotCancelled()
    {
        var visa = new Visa
        {
            ExpirationDate = DateTime.Today.AddDays(-1)
        };

        Assert.Equal(VisaValidityState.Expired, VisaValidityStateHelper.Resolve(visa));
    }

    [Fact]
    public void Resolve_Valid_WhenNoExpirationDate()
    {
        var visa = new Visa { ExpirationDate = null };

        Assert.Equal(VisaValidityState.Valid, VisaValidityStateHelper.Resolve(visa));
    }

    [Fact]
    public void Resolve_Expiring_WhenInsideDefaultWindowWithoutObjectSpaceRules()
    {
        // FromObjectSpace(null) still applies DefaultExpiringSoonDays.
        var visa = new Visa
        {
            ExpirationDate = DateTime.Today.AddDays(5)
        };

        Assert.Equal(VisaValidityState.Expiring, VisaValidityStateHelper.Resolve(visa, objectSpace: null));
    }

    [Fact]
    public void Resolve_Valid_WhenBeyondDefaultExpiringSoonWindow()
    {
        var visa = new Visa
        {
            ExpirationDate = DateTime.Today.AddDays(ExpirationAlertRule.DefaultExpiringSoonDays + 1)
        };

        Assert.Equal(VisaValidityState.Valid, VisaValidityStateHelper.Resolve(visa, objectSpace: null));
    }

    [Fact]
    public void Resolve_Cancelled_BeatsChanged()
    {
        var visa = new Visa
        {
            IsCancelled = true,
            IsChanged = true
        };

        Assert.Equal(VisaValidityState.Cancelled, VisaValidityStateHelper.Resolve(visa));
    }
}
