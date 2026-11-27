using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Security;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.BusinessObjects.PersonIntake;

namespace Visa2026.Module.Services.PersonIntake;

/// <summary>Permission questions the person intake wizard asks; XAF security in the app, a fake in tests.</summary>
public interface IPersonIntakePermissionChecker
{
    bool CanRead(Type type, object target, string memberName);
    bool CanWrite(Type type, object target, string memberName);
    bool CanCreate(Type type);
    bool CanDelete(Type type, object target);
}

/// <summary>
/// XAF native permissions applied to the person intake wizard: who may open it, which fields are visible or
/// editable, and which record steps allow add / edit / remove. Configure in the Roles screen
/// (type, member, and object permissions on <see cref="Person"/> and its child types).
/// </summary>
public sealed class PersonIntakeWizardAccess
{
    public static string ViewOnlyMessage => PersonIntakeText.Get("Access.ViewOnly");

    private readonly IPersonIntakePermissionChecker _checker;
    private readonly IObjectSpace _objectSpace;
    private readonly Person _person;
    private readonly bool _isNew;
    private readonly Dictionary<string, bool> _cache = new(StringComparer.Ordinal);

    public static PersonIntakeWizardAccess Unrestricted { get; } = new(new AllowAllChecker(), null, null, false);

    public PersonIntakeWizardAccess(IPersonIntakePermissionChecker checker, IObjectSpace objectSpace, Person person, bool isNew)
    {
        _checker = checker ?? new AllowAllChecker();
        _objectSpace = objectSpace;
        _person = person;
        _isNew = isNew;
    }

    public static PersonIntakeWizardAccess ForCurrentUser(IObjectSpace objectSpace, Person person)
    {
        if (objectSpace == null || person == null || SecuritySystem.Instance is not IRequestSecurityStrategy)
            return Unrestricted;
        return new PersonIntakeWizardAccess(new XafChecker(objectSpace), objectSpace, person, objectSpace.IsNewObject(person));
    }

    /// <summary>Roles screen switch: Read on "Person intake" turns the wizard on; without it the native Person DetailView opens.</summary>
    public static bool IsWizardEnabled(IObjectSpace objectSpace)
    {
        if (SecuritySystem.Instance is not IRequestSecurityStrategy security)
            return true;
        try
        {
            return security.CanRead(typeof(PersonIntakeWizardHost), objectSpace);
        }
        catch
        {
            return false;
        }
    }

    public bool CanEditPerson => Cached("person:edit", () =>
        _isNew ? _checker.CanCreate(typeof(Person)) : _checker.CanWrite(typeof(Person), _person, null));

    public bool CanDeletePerson => Cached("person:delete", () =>
        _isNew ? _checker.CanCreate(typeof(Person)) : _checker.CanDelete(typeof(Person), _person));

    public bool IsViewOnly => !CanEditPerson;

    /// <summary>Create, Write, and Delete on <see cref="PersonPendingItem"/>.</summary>
    public bool CanChangePendingMarks => Cached("pending", () =>
        _checker.CanCreate(typeof(PersonPendingItem))
        && _checker.CanWrite(typeof(PersonPendingItem), null, null)
        && _checker.CanDelete(typeof(PersonPendingItem), null));

    public bool CanWriteFollowNote => CanWriteMember(nameof(Person.IncompleteNotes));

    /// <summary>Hidden when the role is denied Read on the Person member behind <paramref name="fieldKey"/>.</summary>
    public bool CanReadField(string fieldKey)
    {
        var member = MemberFor(fieldKey);
        return member == null || Cached("r:" + member, () => _checker.CanRead(typeof(Person), PersonTarget, member));
    }

    public bool CanWriteField(string fieldKey)
    {
        var member = MemberFor(fieldKey);
        return member == null ? CanEditPerson : CanWriteMember(member);
    }

    public bool CanTogglePendingField(string fieldKey) =>
        CanChangePendingMarks && CanWriteField(fieldKey);

    public bool CanReadStep(string recordKey)
    {
        var type = RecordType(recordKey);
        return type == null || Cached("sr:" + type.Name, () => _checker.CanRead(type, null, null));
    }

    public bool CanAddRecord(string recordKey)
    {
        if (recordKey == PersonPendingTargets.MedicalRecord)
            return false;
        var type = RecordType(recordKey);
        return CanEditPerson && (type == null || Cached("sc:" + type.Name, () => _checker.CanCreate(type)));
    }

    /// <summary>Edit an existing row (<paramref name="existingId"/>) or fill a new card (null → Create).</summary>
    public bool CanEditRecord(string recordKey, Guid? existingId)
    {
        if (recordKey == PersonPendingTargets.MedicalRecord)
            return false;
        if (!CanEditPerson)
            return false;
        var type = RecordType(recordKey);
        if (type == null)
            return true;
        if (!existingId.HasValue)
            return CanAddRecord(recordKey);
        return Cached($"sw:{type.Name}:{existingId}", () => _checker.CanWrite(type, Load(type, existingId.Value), null));
    }

    /// <summary>Unsaved cards can always be discarded; saved rows need Delete.</summary>
    public bool CanRemoveRecord(string recordKey, Guid? existingId)
    {
        if (recordKey == PersonPendingTargets.MedicalRecord)
            return false;
        if (!CanEditPerson)
            return false;
        if (!existingId.HasValue)
            return true;
        var type = RecordType(recordKey);
        return type == null
            || Cached($"sd:{type.Name}:{existingId}", () => _checker.CanDelete(type, Load(type, existingId.Value)));
    }

    public bool CanTogglePendingStep(string recordKey) =>
        CanChangePendingMarks && CanAddRecord(recordKey);

    /// <summary>
    /// Server-side guard before the wizard writes anything. Returns an officer-facing message, or null.
    /// The secured object space still rejects any remaining member write at commit.
    /// </summary>
    public string FindSaveViolation(Person person, PersonIntakeWizardState state)
    {
        if (person == null || state == null)
            return null;
        if (!CanEditPerson)
            return ViewOnlyMessage;

        var savedPending = (person.PendingItems ?? Enumerable.Empty<PersonPendingItem>())
            .Where(p => p != null && !string.IsNullOrEmpty(p.TargetKey))
            .Select(p => p.TargetKey)
            .Where(k => PersonIntakeAreaMap.TryGetArea(k, out _))
            .ToHashSet(StringComparer.Ordinal);
        var wantedPending = state.PendingKeys
            .Where(k => PersonIntakeAreaMap.TryGetArea(k, out _))
            .ToHashSet(StringComparer.Ordinal);
        if (!CanChangePendingMarks && !savedPending.SetEquals(wantedPending))
            return PersonIntakeText.Get("Access.CannotChangePending");

        foreach (var (key, drafts, saved) in RecordSets(person, state))
        {
            if (state.PendingKeys.Contains(key))
                continue;
            var label = PersonIntakeFieldLabels.Label(key);
            if (drafts.Any(d => !d.ExistingId.HasValue && d.HasContent) && !CanAddRecord(key))
                return PersonIntakeText.Format("Access.CannotAddRecords", label);
            var kept = drafts.Where(d => d.ExistingId.HasValue).Select(d => d.ExistingId.Value).ToHashSet();
            if (saved.Any(id => !kept.Contains(id) && !CanRemoveRecord(key, id)))
                return PersonIntakeText.Format("Access.CannotRemoveRecords", label);
        }

        if (state.OtherDrafts.FamilyRelationDocuments.Any(d => !d.IsExisting)
            && !state.PendingKeys.Contains(PersonPendingTargets.PersonFamilyRelationDocument)
            && !CanAddRecord(PersonPendingTargets.PersonFamilyRelationDocument))
            return PersonIntakeText.Get("Access.CannotAddFamilyRelation");

        return null;
    }

    public static Type RecordType(string recordKey) => recordKey switch
    {
        PersonPendingTargets.Passport => typeof(Passport),
        PersonPendingTargets.Education => typeof(Education),
        PersonPendingTargets.MedicalRecord => typeof(MedicalRecord),
        PersonPendingTargets.AddressOfResidence => typeof(AddressOfResidence),
        PersonPendingTargets.PersonDocument => typeof(PersonDocument),
        PersonPendingTargets.TravelHistory => typeof(TravelHistory),
        PersonPendingTargets.EmployeeSalary => typeof(EmployeeSalary),
        PersonPendingTargets.EmployeePositionHistory => typeof(EmployeePositionHistory),
        PersonPendingTargets.PersonFamilyRelationDocument => typeof(PersonFamilyRelationDocument),
        PersonPendingTargets.WorkDuty => typeof(WorkDuty),
        _ => null
    };

    /// <summary>"Person.X" → "X". Age is derived from date of birth.</summary>
    public static string MemberFor(string fieldKey)
    {
        if (string.IsNullOrEmpty(fieldKey) || !fieldKey.StartsWith("Person.", StringComparison.Ordinal))
            return null;
        if (fieldKey == PersonPendingTargets.Age)
            return nameof(Person.DateOfBirth);
        return fieldKey.Substring("Person.".Length);
    }

    private object PersonTarget => _isNew ? null : _person;

    private bool CanWriteMember(string member) =>
        CanEditPerson && Cached("w:" + member, () =>
            _isNew
                ? _checker.CanWrite(typeof(Person), null, member)
                : _checker.CanWrite(typeof(Person), _person, member));

    private object Load(Type type, Guid id)
    {
        try
        {
            return _objectSpace?.GetObjectByKey(type, id);
        }
        catch
        {
            return null;
        }
    }

    private bool Cached(string key, Func<bool> evaluate)
    {
        if (_cache.TryGetValue(key, out var value))
            return value;
        try
        {
            value = evaluate();
        }
        catch
        {
            value = false;
        }
        _cache[key] = value;
        return value;
    }

    private readonly record struct DraftRef(Guid? ExistingId, bool HasContent);

    private static IEnumerable<(string Key, List<DraftRef> Drafts, List<Guid> Saved)> RecordSets(Person person, PersonIntakeWizardState state)
    {
        static List<Guid> Ids<T>(IEnumerable<T> rows, Func<T, Guid> id) where T : class =>
            (rows ?? Enumerable.Empty<T>()).Where(r => r != null).Select(id).ToList();

        yield return (PersonPendingTargets.Passport,
            state.PassportDrafts.Select(d => new DraftRef(d.ExistingId, d.HasContent)).ToList(),
            Ids(person.Passports, p => p.ID));
        yield return (PersonPendingTargets.Education,
            state.EducationDrafts.Select(d => new DraftRef(d.ExistingId, d.HasContent)).ToList(),
            Ids(person.Educations, e => e.ID));
        if (person.PersonRole == PersonRecordRole.Employee)
        {
            yield return (PersonPendingTargets.PersonDocument,
                state.DocumentDrafts.Select(d => new DraftRef(d.ExistingId, d.HasContent)).ToList(),
                Ids(person.Documents, d => d.ID));
            yield return (PersonPendingTargets.EmployeeSalary,
                state.SalaryDrafts.Select(d => new DraftRef(d.ExistingId, d.HasContent)).ToList(),
                Ids(person.Salaries, s => s.ID));
            yield return (PersonPendingTargets.EmployeePositionHistory,
                state.PositionDrafts.Select(d => new DraftRef(d.ExistingId, d.HasContent)).ToList(),
                Ids(person.PositionHistory, p => p.ID));
            yield return (PersonPendingTargets.WorkDuty,
                state.WorkDutyDrafts.Select(d => new DraftRef(d.ExistingId, d.HasContent)).ToList(),
                Ids(person.WorkDuties, d => d.ID));
        }
    }

    private sealed class AllowAllChecker : IPersonIntakePermissionChecker
    {
        public bool CanRead(Type type, object target, string memberName) => true;
        public bool CanWrite(Type type, object target, string memberName) => true;
        public bool CanCreate(Type type) => true;
        public bool CanDelete(Type type, object target) => true;
    }

    private sealed class XafChecker : IPersonIntakePermissionChecker
    {
        private readonly IObjectSpace _objectSpace;

        public XafChecker(IObjectSpace objectSpace) => _objectSpace = objectSpace;

        private static IRequestSecurityStrategy Security => SecuritySystem.Instance as IRequestSecurityStrategy;

        public bool CanRead(Type type, object target, string memberName) =>
            Security is not { } s || (target == null
                ? s.CanRead(type, _objectSpace, memberName)
                : s.CanRead(_objectSpace, target, memberName));

        public bool CanWrite(Type type, object target, string memberName) =>
            Security is not { } s || (target == null
                ? s.CanWrite(type, _objectSpace, memberName)
                : s.CanWrite(_objectSpace, target, memberName));

        public bool CanCreate(Type type) =>
            Security is not { } s || s.CanCreate(type, _objectSpace);

        public bool CanDelete(Type type, object target) =>
            Security is not { } s || (target == null
                ? s.CanDelete(type, _objectSpace)
                : s.CanDelete(_objectSpace, target));
    }
}
