using System.Collections.ObjectModel;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.StateEvaluation;
using Visa2026.Module.Services.StateEvaluation.Evaluators;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public class VisaStateEvaluatorTests
{
    [Fact]
    public void Evaluate_Null_ReturnsNoVisa()
    {
        var result = VisaStateEvaluator.Evaluate(null, Settings());
        Assert.Equal("NoVisa", result.StateCode);
        Assert.Equal(StateSeverity.None, result.Severity);
    }

    [Fact]
    public void Evaluate_Cancelled_TakesPriorityOverDates()
    {
        var visa = CreateLinkedVisa(
            expiration: DateTime.Today.AddDays(10),
            cancelled: true);

        var result = VisaStateEvaluator.Evaluate(visa, Settings(expiringSoon: 30));
        Assert.Equal("Cancelled", result.StateCode);
        Assert.Equal(StateSeverity.Critical, result.Severity);
    }

    [Fact]
    public void Evaluate_Archived_WhenNotCurrentOnPerson()
    {
        var person = new Person();
        var passport = new Passport { Person = person };
        person.Passports = new ObservableCollection<Passport> { passport };

        var current = CreateVisa(DateTime.Today.AddDays(60), start: DateTime.Today.AddMonths(-1));
        var archived = CreateVisa(DateTime.Today.AddDays(30), start: DateTime.Today.AddYears(-2));
        current.Passport = passport;
        archived.Passport = passport;
        passport.Visas = new ObservableCollection<Visa> { archived, current };

        var result = VisaStateEvaluator.Evaluate(archived, Settings(expiringSoon: 30));
        Assert.Equal("Archived", result.StateCode);
        Assert.Equal(StateSeverity.None, result.Severity);
    }

    [Fact]
    public void Evaluate_Expired_WhenPastExpiration()
    {
        var visa = CreateLinkedVisa(expiration: DateTime.Today.AddDays(-3));
        var result = VisaStateEvaluator.Evaluate(visa, Settings());
        Assert.Equal("Expired", result.StateCode);
        Assert.Equal(StateSeverity.Critical, result.Severity);
    }

    [Fact]
    public void Evaluate_ExpiringSoon_VariantsByFlags()
    {
        var settings = Settings(expiringSoon: 30);

        var required = CreateLinkedVisa(expiration: DateTime.Today.AddDays(10));
        required.ExtensionRequired = true;
        Assert.Equal("ExpiringSoon", VisaStateEvaluator.Evaluate(required, settings).StateCode);
        Assert.Equal(StateSeverity.Warning, VisaStateEvaluator.Evaluate(required, settings).Severity);

        var notRequired = CreateLinkedVisa(expiration: DateTime.Today.AddDays(10));
        notRequired.ExtensionRequired = false;
        Assert.Equal("ExpiringSoonNotRequired", VisaStateEvaluator.Evaluate(notRequired, settings).StateCode);

        var extended = CreateLinkedVisa(expiration: DateTime.Today.AddDays(10));
        extended.IsExtended = true;
        Assert.Equal("Extended", VisaStateEvaluator.Evaluate(extended, settings).StateCode);
        Assert.Equal(StateSeverity.Info, VisaStateEvaluator.Evaluate(extended, settings).Severity);
    }

    [Fact]
    public void Evaluate_ExtensionApplicationRequired_BetweenWindows()
    {
        // DaysRemaining 60: outside expiring-soon(30), inside extension window(90).
        var visa = CreateLinkedVisa(expiration: DateTime.Today.AddDays(60));
        var settings = Settings(expiringSoon: 30, extensionRequired: 90);

        var result = VisaStateEvaluator.Evaluate(visa, settings);
        Assert.Equal("ExtensionApplicationRequired", result.StateCode);
        Assert.Equal(StateSeverity.Warning, result.Severity);
    }

    [Fact]
    public void Evaluate_Active_Changed_And_Extended_OutsideExpiringWindow()
    {
        var settings = Settings(expiringSoon: 30, extensionRequired: null);

        var active = CreateLinkedVisa(expiration: DateTime.Today.AddDays(120));
        Assert.Equal("Active", VisaStateEvaluator.Evaluate(active, settings).StateCode);

        var changed = CreateLinkedVisa(expiration: DateTime.Today.AddDays(120));
        changed.IsChanged = true;
        Assert.Equal("Changed", VisaStateEvaluator.Evaluate(changed, settings).StateCode);

        var extended = CreateLinkedVisa(expiration: DateTime.Today.AddDays(120));
        extended.IsExtended = true;
        Assert.Equal("Extended", VisaStateEvaluator.Evaluate(extended, settings).StateCode);
    }

    private static StateEvaluationSettings Settings(int expiringSoon = 30, int? extensionRequired = null)
    {
        var rule = new ExpirationAlertRule
        {
            BusinessObjectKey = ExpirationAlertBusinessObjectKeys.Visa,
            DisplayName = "Visa",
            ExpiringSoonDays = expiringSoon,
            ExtensionApplicationRequiredDays = extensionRequired
        };
        return new StateEvaluationSettings(
            ExpirationAlertRule.DefaultExpiringSoonDays,
            new Dictionary<string, ExpirationAlertRule>
            {
                [ExpirationAlertBusinessObjectKeys.Visa] = rule
            });
    }

    private static Visa CreateLinkedVisa(DateTime expiration, bool cancelled = false)
    {
        var person = new Person();
        var passport = new Passport { Person = person };
        person.Passports = new ObservableCollection<Passport> { passport };
        var visa = CreateVisa(expiration, start: DateTime.Today.AddYears(-1), cancelled);
        visa.Passport = passport;
        passport.Visas = new ObservableCollection<Visa> { visa };
        return visa;
    }

    private static Visa CreateVisa(DateTime expiration, DateTime start, bool cancelled = false)
    {
        var visa = new Visa
        {
            ID = Guid.NewGuid(),
            IsCancelled = cancelled,
            ExpirationDate = expiration
        };
        visa.IssueDate = start;
        visa.StartDate = start;
        return visa;
    }
}
