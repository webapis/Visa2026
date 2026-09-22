using System.Collections.ObjectModel;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.StateEvaluation;
using Visa2026.Module.Services.StateEvaluation.Evaluators;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public class PassportStateEvaluatorTests
{
    [Fact]
    public void Evaluate_Null_ReturnsNoPassport()
    {
        var result = PassportStateEvaluator.Evaluate(null, Settings());
        Assert.Equal("NoPassport", result.StateCode);
        Assert.Equal(StateSeverity.None, result.Severity);
    }

    [Fact]
    public void Evaluate_Archived_WhenNotCurrentOnPerson()
    {
        var person = new Person();
        var current = new Passport
        {
            ID = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            Person = person,
            IssueDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Unspecified),
            ExpirationDate = DateTime.Today.AddYears(2)
        };
        var archived = new Passport
        {
            ID = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
            Person = person,
            IssueDate = new DateTime(2018, 1, 1, 0, 0, 0, DateTimeKind.Unspecified),
            ExpirationDate = DateTime.Today.AddDays(10)
        };
        person.Passports = new ObservableCollection<Passport> { archived, current };

        var result = PassportStateEvaluator.Evaluate(archived, Settings(expiringSoon: 30));
        Assert.Equal("Archived", result.StateCode);
        Assert.Equal(StateSeverity.None, result.Severity);
    }

    [Fact]
    public void Evaluate_Expired_WhenPastExpiration()
    {
        var passport = CreateCurrentPassport(expiration: DateTime.Today.AddDays(-5));
        var result = PassportStateEvaluator.Evaluate(passport, Settings());
        Assert.Equal("Expired", result.StateCode);
        Assert.Equal(StateSeverity.Critical, result.Severity);
    }

    [Fact]
    public void Evaluate_ExpiringSoon_WithinConfiguredWindow()
    {
        var passport = CreateCurrentPassport(expiration: DateTime.Today.AddDays(15));
        var result = PassportStateEvaluator.Evaluate(passport, Settings(expiringSoon: 30));
        Assert.Equal("ExpiringSoon", result.StateCode);
        Assert.Equal(StateSeverity.Warning, result.Severity);
    }

    [Fact]
    public void Evaluate_Active_OutsideExpiringWindow()
    {
        var passport = CreateCurrentPassport(expiration: DateTime.Today.AddDays(120));
        var result = PassportStateEvaluator.Evaluate(passport, Settings(expiringSoon: 30));
        Assert.Equal("Active", result.StateCode);
        Assert.Equal(StateSeverity.None, result.Severity);
    }

    private static StateEvaluationSettings Settings(int expiringSoon = 30)
    {
        var rule = new ExpirationAlertRule
        {
            BusinessObjectKey = ExpirationAlertBusinessObjectKeys.Passport,
            DisplayName = "Passport",
            ExpiringSoonDays = expiringSoon
        };
        return new StateEvaluationSettings(
            ExpirationAlertRule.DefaultExpiringSoonDays,
            new Dictionary<string, ExpirationAlertRule>
            {
                [ExpirationAlertBusinessObjectKeys.Passport] = rule
            });
    }

    private static Passport CreateCurrentPassport(DateTime expiration)
    {
        var person = new Person();
        var passport = new Passport
        {
            ID = Guid.NewGuid(),
            Person = person,
            IssueDate = new DateTime(2022, 1, 1, 0, 0, 0, DateTimeKind.Unspecified),
            ExpirationDate = expiration
        };
        person.Passports = new ObservableCollection<Passport> { passport };
        return passport;
    }
}
