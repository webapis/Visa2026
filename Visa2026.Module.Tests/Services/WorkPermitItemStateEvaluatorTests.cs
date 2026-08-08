using System.Collections.ObjectModel;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.StateEvaluation;
using Visa2026.Module.Services.StateEvaluation.Evaluators;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public class WorkPermitItemStateEvaluatorTests
{
    [Fact]
    public void Evaluate_Null_ReturnsNoWorkPermit()
    {
        var result = WorkPermitItemStateEvaluator.Evaluate(null, Settings());
        Assert.Equal("NoWorkPermit", result.StateCode);
        Assert.Equal(StateSeverity.None, result.Severity);
        Assert.Equal("WorkPermit", result.BoType);
    }

    [Fact]
    public void Evaluate_Cancelled_TakesPriorityOverDates()
    {
        var item = CreateCurrentItem(
            start: DateTime.Today.AddYears(-1),
            expiration: DateTime.Today.AddDays(10),
            cancelled: true);

        var result = WorkPermitItemStateEvaluator.Evaluate(item, Settings(expiringSoon: 30));
        Assert.Equal("Cancelled", result.StateCode);
        Assert.Equal(StateSeverity.Critical, result.Severity);
    }

    [Fact]
    public void Evaluate_Archived_WhenNotCurrentOnPerson()
    {
        var person = new Person();
        var current = CreateItem(
            person,
            Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            start: DateTime.Today.AddMonths(-3),
            expiration: DateTime.Today.AddDays(60));
        var archived = CreateItem(
            person,
            Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
            start: DateTime.Today.AddYears(-2),
            expiration: DateTime.Today.AddDays(10));
        person.WorkPermitItems = new ObservableCollection<WorkPermitItem> { archived, current };

        var result = WorkPermitItemStateEvaluator.Evaluate(archived, Settings(expiringSoon: 30));
        Assert.Equal("Archived", result.StateCode);
        Assert.Equal(StateSeverity.None, result.Severity);
    }

    [Fact]
    public void Evaluate_Expired_WhenPastExpiration()
    {
        var item = CreateCurrentItem(
            start: DateTime.Today.AddYears(-1),
            expiration: DateTime.Today.AddDays(-3));
        var result = WorkPermitItemStateEvaluator.Evaluate(item, Settings());
        Assert.Equal("Expired", result.StateCode);
        Assert.Equal(StateSeverity.Critical, result.Severity);
    }

    [Fact]
    public void Evaluate_ExpiringSoon_WithinConfiguredWindow()
    {
        var item = CreateCurrentItem(
            start: DateTime.Today.AddYears(-1),
            expiration: DateTime.Today.AddDays(10));
        var result = WorkPermitItemStateEvaluator.Evaluate(item, Settings(expiringSoon: 30, extensionRequired: 90));
        Assert.Equal("ExpiringSoon", result.StateCode);
        Assert.Equal(StateSeverity.Warning, result.Severity);
    }

    [Fact]
    public void Evaluate_ExtensionApplicationRequired_BetweenWindows()
    {
        // DaysRemaining 60: outside expiring-soon(30), inside extension window(90).
        var item = CreateCurrentItem(
            start: DateTime.Today.AddYears(-1),
            expiration: DateTime.Today.AddDays(60));
        var result = WorkPermitItemStateEvaluator.Evaluate(item, Settings(expiringSoon: 30, extensionRequired: 90));
        Assert.Equal("ExtensionApplicationRequired", result.StateCode);
        Assert.Equal(StateSeverity.Warning, result.Severity);
    }

    [Fact]
    public void Evaluate_Active_OutsideExpiringAndExtensionWindows()
    {
        var item = CreateCurrentItem(
            start: DateTime.Today.AddYears(-1),
            expiration: DateTime.Today.AddDays(120));
        var result = WorkPermitItemStateEvaluator.Evaluate(item, Settings(expiringSoon: 30, extensionRequired: 90));
        Assert.Equal("Active", result.StateCode);
        Assert.Equal(StateSeverity.None, result.Severity);
    }

    private static StateEvaluationSettings Settings(int expiringSoon = 30, int? extensionRequired = null)
    {
        var rule = new ExpirationAlertRule
        {
            BusinessObjectKey = ExpirationAlertBusinessObjectKeys.WorkPermitItem,
            DisplayName = "Work Permit",
            ExpiringSoonDays = expiringSoon,
            ExtensionApplicationRequiredDays = extensionRequired
        };
        return new StateEvaluationSettings(
            ExpirationAlertRule.DefaultExpiringSoonDays,
            new Dictionary<string, ExpirationAlertRule>
            {
                [ExpirationAlertBusinessObjectKeys.WorkPermitItem] = rule
            });
    }

    private static WorkPermitItem CreateCurrentItem(DateTime start, DateTime expiration, bool cancelled = false)
    {
        var person = new Person();
        var item = CreateItem(person, Guid.NewGuid(), start, expiration, cancelled);
        person.WorkPermitItems = new ObservableCollection<WorkPermitItem> { item };
        return item;
    }

    private static WorkPermitItem CreateItem(
        Person person,
        Guid id,
        DateTime start,
        DateTime expiration,
        bool cancelled = false) =>
        new WorkPermitItem
        {
            ID = id,
            Person = person,
            StartDate = start,
            ExpirationDate = expiration,
            IsCancelled = cancelled
        };
}
