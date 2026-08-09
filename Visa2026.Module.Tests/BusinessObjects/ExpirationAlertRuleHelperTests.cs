using System;
using Visa2026.Module.BusinessObjects;
using Xunit;

namespace Visa2026.Module.Tests.BusinessObjects;

/// <summary>
/// ObjectSpace-null paths for <see cref="ExpirationAlertRuleHelper"/> (defaults when no rule store).
/// Distinct from <c>ExpirationEvaluationHelper</c> settings-dictionary API covered elsewhere.
/// </summary>
public sealed class ExpirationAlertRuleHelperTests
{
    private sealed class StubExpirationItem : IExpirationLogic
    {
        public DateTime? ExpirationDate { get; init; }
        public int DaysRemaining { get; init; }
    }

    [Fact]
    public void TryGetRule_NullObjectSpaceOrBlankKey_ReturnsNull()
    {
        Assert.Null(ExpirationAlertRuleHelper.TryGetRule(null, ExpirationAlertBusinessObjectKeys.Visa));
        Assert.Null(ExpirationAlertRuleHelper.TryGetRule(null, " "));
        Assert.Null(ExpirationAlertRuleHelper.TryGetRule(null, null));
    }

    [Fact]
    public void GetExpiringSoonDays_WithoutObjectSpace_ReturnsDefault()
    {
        Assert.Equal(
            ExpirationAlertRule.DefaultExpiringSoonDays,
            ExpirationAlertRuleHelper.GetExpiringSoonDays(null, ExpirationAlertBusinessObjectKeys.Visa));
        Assert.Equal(
            ExpirationAlertRule.DefaultExpiringSoonDays,
            ExpirationAlertRuleHelper.GetExpiringSoonDays(null, " "));
    }

    [Fact]
    public void GetExtensionApplicationRequiredDays_WithoutObjectSpace_ReturnsNull()
    {
        Assert.Null(ExpirationAlertRuleHelper.GetExtensionApplicationRequiredDays(
            null, ExpirationAlertBusinessObjectKeys.Visa));
    }

    [Fact]
    public void IsExpiringSoon_NullItemMissingDateOrExpired_ReturnsFalse()
    {
        Assert.False(ExpirationAlertRuleHelper.IsExpiringSoon(
            null!, ExpirationAlertBusinessObjectKeys.Visa, objectSpace: null));
        Assert.False(ExpirationAlertRuleHelper.IsExpiringSoon(
            new StubExpirationItem { ExpirationDate = null, DaysRemaining = 5 },
            ExpirationAlertBusinessObjectKeys.Visa,
            objectSpace: null));
        Assert.False(ExpirationAlertRuleHelper.IsExpiringSoon(
            new StubExpirationItem
            {
                ExpirationDate = DateTime.Today.AddDays(-1),
                DaysRemaining = 0
            },
            ExpirationAlertBusinessObjectKeys.Visa,
            objectSpace: null));
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(1, true)]
    [InlineData(30, true)]
    [InlineData(31, false)]
    public void IsExpiringSoon_UsesDefaultWindowWhenNoObjectSpace(int daysRemaining, bool expected)
    {
        var item = new StubExpirationItem
        {
            ExpirationDate = DateTime.Today.AddDays(daysRemaining),
            DaysRemaining = daysRemaining
        };

        Assert.Equal(
            expected,
            ExpirationAlertRuleHelper.IsExpiringSoon(
                item, ExpirationAlertBusinessObjectKeys.Visa, objectSpace: null));
    }

    [Fact]
    public void IsExtensionApplicationRequired_WithoutObjectSpace_ReturnsFalse()
    {
        var item = new StubExpirationItem
        {
            ExpirationDate = DateTime.Today.AddDays(60),
            DaysRemaining = 60
        };

        Assert.False(ExpirationAlertRuleHelper.IsExtensionApplicationRequired(
            item, ExpirationAlertBusinessObjectKeys.Visa, objectSpace: null));
        Assert.False(ExpirationAlertRuleHelper.IsExtensionApplicationRequired(
            null!, ExpirationAlertBusinessObjectKeys.Visa, objectSpace: null));
        Assert.False(ExpirationAlertRuleHelper.IsExtensionApplicationRequired(
            new StubExpirationItem { ExpirationDate = null, DaysRemaining = 10 },
            ExpirationAlertBusinessObjectKeys.Visa,
            objectSpace: null));
    }
}
