using Visa2026.Module.BusinessObjects;
using Xunit;

namespace Visa2026.Module.Tests.BusinessObjects;

/// <summary>
/// Pure date math for expiration windows used by state evaluation and ListView severity.
/// </summary>
public sealed class ExpirationLogicHelperTests
{
    private sealed class FakeExpirationItem : IExpirationLogic
    {
        public FakeExpirationItem(DateTime? expirationDate, int daysRemaining)
        {
            ExpirationDate = expirationDate;
            DaysRemaining = daysRemaining;
        }

        public DateTime? ExpirationDate { get; }
        public int DaysRemaining { get; }
    }

    [Fact]
    public void CalculateDaysRemaining_NullOrForceZero_ReturnsZero()
    {
        Assert.Equal(0, ExpirationLogicHelper.CalculateDaysRemaining((DateTime?)null));
        Assert.Equal(0, ExpirationLogicHelper.CalculateDaysRemaining(DateTime.Today.AddDays(10), forceZero: true));
    }

    [Fact]
    public void CalculateDaysRemaining_Future_ReturnsCalendarDays()
    {
        Assert.Equal(5, ExpirationLogicHelper.CalculateDaysRemaining(DateTime.Today.AddDays(5)));
        Assert.Equal(0, ExpirationLogicHelper.CalculateDaysRemaining(DateTime.Today));
    }

    [Fact]
    public void CalculateDaysRemaining_Past_ClampsToZero()
    {
        Assert.Equal(0, ExpirationLogicHelper.CalculateDaysRemaining(DateTime.Today.AddDays(-3)));
    }

    [Fact]
    public void IsExpired_RequiresDateStrictlyBeforeToday()
    {
        Assert.False(ExpirationLogicHelper.IsExpired((DateTime?)null));
        Assert.False(ExpirationLogicHelper.IsExpired(DateTime.Today));
        Assert.False(ExpirationLogicHelper.IsExpired(DateTime.Today.AddDays(1)));
        Assert.True(ExpirationLogicHelper.IsExpired(DateTime.Today.AddDays(-1)));
    }

    [Fact]
    public void IsExpired_NullItem_ReturnsFalse()
    {
        Assert.False(ExpirationLogicHelper.IsExpired((IExpirationLogic)null!));
        Assert.True(ExpirationLogicHelper.IsExpired(new FakeExpirationItem(DateTime.Today.AddDays(-2), 0)));
    }

    [Fact]
    public void DaysOverdue_ZeroWhenNotExpired_ElseCalendarDaysPast()
    {
        Assert.Equal(0, ExpirationLogicHelper.DaysOverdue(null));
        Assert.Equal(0, ExpirationLogicHelper.DaysOverdue(DateTime.Today));
        Assert.Equal(0, ExpirationLogicHelper.DaysOverdue(DateTime.Today.AddDays(2)));
        Assert.Equal(4, ExpirationLogicHelper.DaysOverdue(DateTime.Today.AddDays(-4)));
    }

    [Fact]
    public void CalculateExpirationState_ExpiredWins_NullObjectSpaceStaysActiveWhenNotExpired()
    {
        var expired = new FakeExpirationItem(DateTime.Today.AddDays(-1), 0);
        Assert.Equal(
            ExpirationState.Expired,
            ExpirationLogicHelper.CalculateExpirationState(expired, "Passport", objectSpace: null));

        var active = new FakeExpirationItem(DateTime.Today.AddDays(30), 30);
        Assert.Equal(
            ExpirationState.Active,
            ExpirationLogicHelper.CalculateExpirationState(active, "Passport", objectSpace: null));
    }
}
