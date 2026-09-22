using System.Collections.ObjectModel;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.StateEvaluation;
using Visa2026.Module.Services.StateEvaluation.Evaluators;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public class MedicalRecordStateEvaluatorTests
{
    [Fact]
    public void Evaluate_Null_ReturnsNone()
    {
        var result = MedicalRecordStateEvaluator.Evaluate(null, Settings());
        Assert.Equal("None", result.StateCode);
        Assert.Equal(StateSeverity.None, result.Severity);
        Assert.Equal("Medical Record", result.BoType);
    }

    [Fact]
    public void Evaluate_Archived_WhenNotCurrentOnPerson()
    {
        var person = new Person();
        var current = CreateRecord(
            person,
            Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            issue: DateTime.Today.AddMonths(-1),
            validityDays: 365);
        var archived = CreateRecord(
            person,
            Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
            issue: DateTime.Today.AddYears(-2),
            validityDays: 365);
        person.MedicalRecords = new ObservableCollection<MedicalRecord> { archived, current };

        var result = MedicalRecordStateEvaluator.Evaluate(archived, Settings(expiringSoon: 30));
        Assert.Equal("Archived", result.StateCode);
        Assert.Equal(StateSeverity.None, result.Severity);
    }

    [Fact]
    public void Evaluate_Active_WhenNoExpiration()
    {
        var person = new Person();
        var record = new MedicalRecord
        {
            ID = Guid.NewGuid(),
            Person = person,
            IssueDate = DateTime.Today.AddMonths(-1)
        };
        person.MedicalRecords = new ObservableCollection<MedicalRecord> { record };

        var result = MedicalRecordStateEvaluator.Evaluate(record, Settings());
        Assert.Equal("Active", result.StateCode);
        Assert.Equal(StateSeverity.None, result.Severity);
        Assert.Null(record.ExpirationDate);
    }

    [Fact]
    public void Evaluate_Expired_WhenPastExpiration()
    {
        var record = CreateCurrentRecord(issue: DateTime.Today.AddDays(-40), validityDays: 30);
        var result = MedicalRecordStateEvaluator.Evaluate(record, Settings());
        Assert.Equal("Expired", result.StateCode);
        Assert.Equal(StateSeverity.Critical, result.Severity);
    }

    [Fact]
    public void Evaluate_ExpiringSoon_WithinConfiguredWindow()
    {
        var record = CreateCurrentRecord(issue: DateTime.Today.AddDays(-350), validityDays: 365);
        var result = MedicalRecordStateEvaluator.Evaluate(record, Settings(expiringSoon: 30));
        Assert.Equal("ExpiringSoon", result.StateCode);
        Assert.Equal(StateSeverity.Warning, result.Severity);
        Assert.True(record.DaysRemaining <= 30);
    }

    [Fact]
    public void Evaluate_Active_OutsideExpiringWindow()
    {
        var record = CreateCurrentRecord(issue: DateTime.Today.AddDays(-10), validityDays: 365);
        var result = MedicalRecordStateEvaluator.Evaluate(record, Settings(expiringSoon: 30));
        Assert.Equal("Active", result.StateCode);
        Assert.Equal(StateSeverity.None, result.Severity);
    }

    private static StateEvaluationSettings Settings(int expiringSoon = 30)
    {
        var rule = new ExpirationAlertRule
        {
            BusinessObjectKey = ExpirationAlertBusinessObjectKeys.MedicalRecord,
            DisplayName = "Medical Record",
            ExpiringSoonDays = expiringSoon
        };
        return new StateEvaluationSettings(
            ExpirationAlertRule.DefaultExpiringSoonDays,
            new Dictionary<string, ExpirationAlertRule>
            {
                [ExpirationAlertBusinessObjectKeys.MedicalRecord] = rule
            });
    }

    private static MedicalRecord CreateCurrentRecord(DateTime issue, int validityDays)
    {
        var person = new Person();
        var record = CreateRecord(person, Guid.NewGuid(), issue, validityDays);
        person.MedicalRecords = new ObservableCollection<MedicalRecord> { record };
        return record;
    }

    private static MedicalRecord CreateRecord(Person person, Guid id, DateTime issue, int validityDays)
    {
        return new MedicalRecord
        {
            ID = id,
            Person = person,
            IssueDate = issue,
            ValidityDuration = new ValidityDuration { NumberOfDays = validityDays }
        };
    }
}
