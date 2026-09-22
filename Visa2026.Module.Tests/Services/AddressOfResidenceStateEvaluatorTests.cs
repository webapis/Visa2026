using System.Collections.ObjectModel;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.StateEvaluation;
using Visa2026.Module.Services.StateEvaluation.Evaluators;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public class AddressOfResidenceStateEvaluatorTests
{
    [Fact]
    public void Evaluate_Null_ReturnsNone()
    {
        var result = AddressOfResidenceStateEvaluator.Evaluate(null, Settings());
        Assert.Equal("None", result.StateCode);
        Assert.Equal(StateSeverity.None, result.Severity);
        Assert.Equal("Address", result.BoType);
    }

    [Fact]
    public void Evaluate_Archived_WhenNotCurrentOnPerson()
    {
        var person = new Person();
        var current = CreateAddress(
            person,
            Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            expiration: DateTime.Today.AddYears(1));
        var archived = CreateAddress(
            person,
            Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
            expiration: DateTime.Today.AddDays(-10));
        person.AddressesOfResidence = new ObservableCollection<AddressOfResidence> { archived, current };

        var result = AddressOfResidenceStateEvaluator.Evaluate(archived, Settings(expiringSoon: 30));
        Assert.Equal("Archived", result.StateCode);
        Assert.Equal(StateSeverity.None, result.Severity);
    }

    [Fact]
    public void Evaluate_Active_WhenNoExpiration()
    {
        var person = new Person();
        var address = CreateAddress(person, Guid.NewGuid(), expiration: null);
        person.AddressesOfResidence = new ObservableCollection<AddressOfResidence> { address };

        var result = AddressOfResidenceStateEvaluator.Evaluate(address, Settings());
        Assert.Equal("Active", result.StateCode);
        Assert.Equal(StateSeverity.None, result.Severity);
    }

    [Fact]
    public void Evaluate_Expired_WhenPastExpiration()
    {
        // Sole address still counts as current even when expired (fallback in PersonCurrentItems).
        var address = CreateCurrentAddress(expiration: DateTime.Today.AddDays(-5));
        var result = AddressOfResidenceStateEvaluator.Evaluate(address, Settings());
        Assert.Equal("Expired", result.StateCode);
        Assert.Equal(StateSeverity.Critical, result.Severity);
    }

    [Fact]
    public void Evaluate_ExpiringSoon_WithinConfiguredWindow()
    {
        var address = CreateCurrentAddress(expiration: DateTime.Today.AddDays(12));
        var result = AddressOfResidenceStateEvaluator.Evaluate(address, Settings(expiringSoon: 30));
        Assert.Equal("ExpiringSoon", result.StateCode);
        Assert.Equal(StateSeverity.Warning, result.Severity);
    }

    [Fact]
    public void Evaluate_Active_OutsideExpiringWindow()
    {
        var address = CreateCurrentAddress(expiration: DateTime.Today.AddDays(120));
        var result = AddressOfResidenceStateEvaluator.Evaluate(address, Settings(expiringSoon: 30));
        Assert.Equal("Active", result.StateCode);
        Assert.Equal(StateSeverity.None, result.Severity);
    }

    private static StateEvaluationSettings Settings(int expiringSoon = 30)
    {
        var rule = new ExpirationAlertRule
        {
            BusinessObjectKey = ExpirationAlertBusinessObjectKeys.AddressOfResidence,
            DisplayName = "Address",
            ExpiringSoonDays = expiringSoon
        };
        return new StateEvaluationSettings(
            ExpirationAlertRule.DefaultExpiringSoonDays,
            new Dictionary<string, ExpirationAlertRule>
            {
                [ExpirationAlertBusinessObjectKeys.AddressOfResidence] = rule
            });
    }

    private static AddressOfResidence CreateCurrentAddress(DateTime? expiration)
    {
        var person = new Person();
        var address = CreateAddress(person, Guid.NewGuid(), expiration);
        person.AddressesOfResidence = new ObservableCollection<AddressOfResidence> { address };
        return address;
    }

    private static AddressOfResidence CreateAddress(Person person, Guid id, DateTime? expiration) =>
        new AddressOfResidence
        {
            ID = id,
            Person = person,
            ExpirationDate = expiration
        };
}
