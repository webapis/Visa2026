using Visa2026.Module.BusinessObjects;
using Xunit;

namespace Visa2026.Module.Tests.BusinessObjects;

public sealed class VisaValidityStateHelperTests
{
    [Fact]
    public void Resolve_NullVisa_ReturnsValid()
    {
        Assert.Equal(VisaValidityState.Valid, VisaValidityStateHelper.Resolve(null));
    }

    [Fact]
    public void Resolve_Cancelled_TakesPrecedenceOverChangedAndDates()
    {
        var visa = new Visa
        {
            IsCancelled = true,
            IsChanged = true,
            ExpirationDate = DateTime.Today.AddDays(-10),
        };

        Assert.Equal(VisaValidityState.Cancelled, VisaValidityStateHelper.Resolve(visa));
    }

    [Fact]
    public void Resolve_Changed_TakesPrecedenceOverExpiration()
    {
        var visa = new Visa
        {
            IsChanged = true,
            ExpirationDate = DateTime.Today.AddDays(-10),
        };

        Assert.Equal(VisaValidityState.Changed, VisaValidityStateHelper.Resolve(visa));
    }

    [Fact]
    public void Resolve_ExpiredDate_ReturnsExpired()
    {
        var visa = new Visa { ExpirationDate = DateTime.Today.AddDays(-1) };

        Assert.Equal(VisaValidityState.Expired, VisaValidityStateHelper.Resolve(visa));
    }

    [Fact]
    public void Resolve_NoExpiration_ReturnsValid()
    {
        var visa = new Visa { ExpirationDate = null };

        Assert.Equal(VisaValidityState.Valid, VisaValidityStateHelper.Resolve(visa));
    }

    [Fact]
    public void Resolve_WithinDefaultExpiringWindow_ReturnsExpiring()
    {
        var visa = new Visa
        {
            ExpirationDate = DateTime.Today.AddDays(ExpirationAlertRule.DefaultExpiringSoonDays),
        };

        Assert.Equal(VisaValidityState.Expiring, VisaValidityStateHelper.Resolve(visa, objectSpace: null));
    }

    [Fact]
    public void Resolve_BeyondDefaultExpiringWindow_ReturnsValid()
    {
        var visa = new Visa
        {
            ExpirationDate = DateTime.Today.AddDays(ExpirationAlertRule.DefaultExpiringSoonDays + 1),
        };

        Assert.Equal(VisaValidityState.Valid, VisaValidityStateHelper.Resolve(visa, objectSpace: null));
    }
}
