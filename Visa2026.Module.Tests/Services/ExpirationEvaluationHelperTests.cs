using System;
using System.Collections.Generic;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.StateEvaluation;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public sealed class ExpirationEvaluationHelperTests
{
    private sealed class StubExpirationItem : IExpirationLogic
    {
        public DateTime? ExpirationDate { get; init; }
        public int DaysRemaining { get; init; }
    }

    private static StateEvaluationSettings Settings(
        int defaultExpiringSoonDays = 30,
        int? extensionApplicationRequiredDays = null)
    {
        var rules = new Dictionary<string, ExpirationAlertRule>(StringComparer.OrdinalIgnoreCase);
        if (extensionApplicationRequiredDays.HasValue || defaultExpiringSoonDays != ExpirationAlertRule.DefaultExpiringSoonDays)
        {
            rules[ExpirationAlertBusinessObjectKeys.Visa] = new ExpirationAlertRule
            {
                BusinessObjectKey = ExpirationAlertBusinessObjectKeys.Visa,
                DisplayName = "Visa",
                ExpiringSoonDays = defaultExpiringSoonDays,
                ExtensionApplicationRequiredDays = extensionApplicationRequiredDays
            };
        }

        return new StateEvaluationSettings(defaultExpiringSoonDays, rules);
    }

    [Fact]
    public void IsExpiringSoon_NullOrMissingOrExpired_ReturnsFalse()
    {
        var settings = Settings(defaultExpiringSoonDays: 30);

        Assert.False(ExpirationEvaluationHelper.IsExpiringSoon(
            null!, ExpirationAlertBusinessObjectKeys.Visa, settings));
        Assert.False(ExpirationEvaluationHelper.IsExpiringSoon(
            new StubExpirationItem { ExpirationDate = null, DaysRemaining = 5 },
            ExpirationAlertBusinessObjectKeys.Visa,
            settings));
        Assert.False(ExpirationEvaluationHelper.IsExpiringSoon(
            new StubExpirationItem
            {
                ExpirationDate = DateTime.Today.AddDays(-1),
                DaysRemaining = 0
            },
            ExpirationAlertBusinessObjectKeys.Visa,
            settings));
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(30, true)]
    [InlineData(31, false)]
    public void IsExpiringSoon_UsesConfiguredWindow(int daysRemaining, bool expected)
    {
        var settings = Settings(defaultExpiringSoonDays: 30);
        var item = new StubExpirationItem
        {
            ExpirationDate = DateTime.Today.AddDays(daysRemaining),
            DaysRemaining = daysRemaining
        };

        Assert.Equal(
            expected,
            ExpirationEvaluationHelper.IsExpiringSoon(item, ExpirationAlertBusinessObjectKeys.Visa, settings));
    }

    [Fact]
    public void IsExtensionApplicationRequired_WithoutConfiguredWindow_ReturnsFalse()
    {
        var settings = Settings(defaultExpiringSoonDays: 30, extensionApplicationRequiredDays: null);
        var item = new StubExpirationItem
        {
            ExpirationDate = DateTime.Today.AddDays(60),
            DaysRemaining = 60
        };

        Assert.False(ExpirationEvaluationHelper.IsExtensionApplicationRequired(
            item, ExpirationAlertBusinessObjectKeys.Visa, settings));
    }

    [Theory]
    [InlineData(15, false)]  // inside expiring-soon band → not extension-required
    [InlineData(30, false)]  // at expiring-soon boundary → not extension-required
    [InlineData(31, true)]   // just outside expiring-soon, inside extension window
    [InlineData(90, true)]
    [InlineData(91, false)]  // beyond extension window
    public void IsExtensionApplicationRequired_OnlyBetweenExpiringSoonAndExtensionWindow(
        int daysRemaining, bool expected)
    {
        var settings = Settings(defaultExpiringSoonDays: 30, extensionApplicationRequiredDays: 90);
        var item = new StubExpirationItem
        {
            ExpirationDate = DateTime.Today.AddDays(daysRemaining),
            DaysRemaining = daysRemaining
        };

        Assert.Equal(
            expected,
            ExpirationEvaluationHelper.IsExtensionApplicationRequired(
                item, ExpirationAlertBusinessObjectKeys.Visa, settings));
    }

    [Fact]
    public void IsExtensionApplicationRequired_ExpiredItem_ReturnsFalse()
    {
        var settings = Settings(defaultExpiringSoonDays: 30, extensionApplicationRequiredDays: 90);
        var item = new StubExpirationItem
        {
            ExpirationDate = DateTime.Today.AddDays(-3),
            DaysRemaining = 0
        };

        Assert.False(ExpirationEvaluationHelper.IsExtensionApplicationRequired(
            item, ExpirationAlertBusinessObjectKeys.Visa, settings));
    }

    [Fact]
    public void ExpirationLogicHelper_CalculateDaysRemaining_ClampsPastDatesToZero()
    {
        Assert.Equal(0, ExpirationLogicHelper.CalculateDaysRemaining(DateTime.Today.AddDays(-5)));
        Assert.Equal(0, ExpirationLogicHelper.CalculateDaysRemaining((DateTime?)null));
        Assert.Equal(0, ExpirationLogicHelper.CalculateDaysRemaining(DateTime.Today.AddDays(10), forceZero: true));
        Assert.Equal(10, ExpirationLogicHelper.CalculateDaysRemaining(DateTime.Today.AddDays(10)));
    }

    [Fact]
    public void ExpirationLogicHelper_IsExpiredAndDaysOverdue()
    {
        Assert.True(ExpirationLogicHelper.IsExpired(DateTime.Today.AddDays(-2)));
        Assert.False(ExpirationLogicHelper.IsExpired(DateTime.Today));
        Assert.False(ExpirationLogicHelper.IsExpired(DateTime.Today.AddDays(1)));
        Assert.Equal(2, ExpirationLogicHelper.DaysOverdue(DateTime.Today.AddDays(-2)));
        Assert.Equal(0, ExpirationLogicHelper.DaysOverdue(DateTime.Today.AddDays(5)));
    }
}
