using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.PersonIntake;
using Xunit;

namespace Visa2026.Module.Tests.Services.PersonIntake;

public class PersonIntakeWizardAccessTests
{
    [Fact]
    public void PersonWithoutWrite_IsViewOnly_AndSaveIsRefused()
    {
        var checker = new FakeChecker();
        checker.Deny("write", typeof(Person));
        var person = Employee();
        var access = Existing(checker, person);

        Assert.True(access.IsViewOnly);
        Assert.False(access.CanWriteField(PersonPendingTargets.FirstName));
        Assert.False(access.CanAddRecord(PersonPendingTargets.Passport));
        Assert.True(access.CanReadField(PersonPendingTargets.FirstName));
        Assert.Equal(PersonIntakeWizardAccess.ViewOnlyMessage, access.FindSaveViolation(person, new PersonIntakeWizardState()));
    }

    [Fact]
    public void NewPerson_UsesCreate_NotWrite()
    {
        var checker = new FakeChecker();
        checker.Deny("create", typeof(Person));
        var access = new PersonIntakeWizardAccess(checker, null, Employee(), isNew: true);

        Assert.True(access.IsViewOnly);
    }

    [Fact]
    public void MemberReadDenied_HidesOnlyThatField()
    {
        var checker = new FakeChecker();
        checker.Deny("read", typeof(Person), nameof(Person.PersonalNumber));
        var access = Existing(checker, Employee());

        Assert.False(access.CanReadField(PersonPendingTargets.PersonalNumber));
        Assert.True(access.CanReadField(PersonPendingTargets.BirthPlace));
    }

    [Fact]
    public void MemberWriteDenied_LocksOnlyThatField()
    {
        var checker = new FakeChecker();
        checker.Deny("write", typeof(Person), nameof(Person.Nationality));
        var access = Existing(checker, Employee());

        Assert.False(access.IsViewOnly);
        Assert.False(access.CanWriteField(PersonPendingTargets.Nationality));
        Assert.False(access.CanTogglePendingField(PersonPendingTargets.Nationality));
        Assert.True(access.CanWriteField(PersonPendingTargets.Gender));
    }

    [Fact]
    public void AgeFollowsDateOfBirth()
    {
        Assert.Equal(nameof(Person.DateOfBirth), PersonIntakeWizardAccess.MemberFor(PersonPendingTargets.Age));
        Assert.Null(PersonIntakeWizardAccess.MemberFor(PersonPendingTargets.Passport));
    }

    [Fact]
    public void PendingItemDenied_BlocksChangingMarks_ButNotAnUnchangedSave()
    {
        var checker = new FakeChecker();
        checker.Deny("create", typeof(PersonPendingItem));
        var person = Employee();
        person.PendingItems.Add(new PersonPendingItem { TargetKey = PersonPendingTargets.BirthPlace });
        var access = Existing(checker, person);

        Assert.False(access.CanTogglePendingField(PersonPendingTargets.BirthPlace));

        var unchanged = new PersonIntakeWizardState();
        unchanged.PendingKeys.Add(PersonPendingTargets.BirthPlace);
        Assert.Null(access.FindSaveViolation(person, unchanged));

        var changed = new PersonIntakeWizardState();
        changed.PendingKeys.Add(PersonPendingTargets.BirthPlace);
        changed.PendingKeys.Add(PersonPendingTargets.PersonalNumber);
        Assert.NotNull(access.FindSaveViolation(person, changed));
    }

    [Fact]
    public void RecordCreateDenied_HidesAdd_AndRefusesNewCard()
    {
        var checker = new FakeChecker();
        checker.Deny("create", typeof(Passport));
        var person = Employee();
        var access = Existing(checker, person);

        Assert.False(access.CanAddRecord(PersonPendingTargets.Passport));
        Assert.False(access.CanEditRecord(PersonPendingTargets.Passport, null));
        Assert.True(access.CanAddRecord(PersonPendingTargets.Education));

        var state = new PersonIntakeWizardState();
        state.PassportDrafts.Add(new PersonIntakePassportDraft { PassportNumber = "A123" });
        Assert.Contains("cannot add", access.FindSaveViolation(person, state));
    }

    [Fact]
    public void RecordDeleteDenied_KeepsSavedRows_ButUnsavedCardsCanBeDiscarded()
    {
        var checker = new FakeChecker();
        checker.Deny("delete", typeof(Passport));
        var person = Employee();
        var saved = new Passport { ID = Guid.NewGuid() };
        person.Passports = new ObservableCollection<Passport> { saved };
        var access = Existing(checker, person);

        Assert.False(access.CanRemoveRecord(PersonPendingTargets.Passport, saved.ID));
        Assert.True(access.CanRemoveRecord(PersonPendingTargets.Passport, null));

        var removed = new PersonIntakeWizardState();
        Assert.Contains("cannot remove", access.FindSaveViolation(person, removed));

        var kept = new PersonIntakeWizardState();
        kept.PassportDrafts.Add(new PersonIntakePassportDraft { ExistingId = saved.ID, PassportNumber = "A123" });
        Assert.Null(access.FindSaveViolation(person, kept));
    }

    [Fact]
    public void RecordReadDenied_HidesTheStep()
    {
        var checker = new FakeChecker();
        checker.Deny("read", typeof(EmployeeSalary));
        var access = Existing(checker, Employee());

        Assert.False(access.CanReadStep(PersonPendingTargets.EmployeeSalary));
        Assert.True(access.CanReadStep(PersonPendingTargets.Passport));
    }

    private static PersonIntakeWizardAccess Existing(FakeChecker checker, Person person) =>
        new(checker, null, person, isNew: false);

    private static Person Employee()
    {
        var person = new Person();
        PersonRoleHelper.ApplyRole(person, PersonRecordRole.Employee);
        return person;
    }

    private sealed class FakeChecker : IPersonIntakePermissionChecker
    {
        private readonly HashSet<string> _denied = new(StringComparer.Ordinal);

        public void Deny(string operation, Type type, string member = null) =>
            _denied.Add($"{operation}:{type.Name}:{member}");

        private bool Allowed(string operation, Type type, string member) =>
            !_denied.Contains($"{operation}:{type.Name}:")
            && (member == null || !_denied.Contains($"{operation}:{type.Name}:{member}"));

        public bool CanRead(Type type, object target, string memberName) => Allowed("read", type, memberName);
        public bool CanWrite(Type type, object target, string memberName) => Allowed("write", type, memberName);
        public bool CanCreate(Type type) => Allowed("create", type, null);
        public bool CanDelete(Type type, object target) => Allowed("delete", type, null);
    }
}
