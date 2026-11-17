using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services.PersonIntake;

public sealed class PersonIntakeSaveResult
{
    public bool Succeeded { get; init; }
    public string Message { get; init; }
    public IReadOnlyList<PersonIntakeFinding> Unresolved { get; init; } = Array.Empty<PersonIntakeFinding>();
}

public static class PersonIntakeWizardPersistHelper
{
    public static PersonIntakeSaveResult TrySave(
        IObjectSpace objectSpace,
        Person person,
        PersonIntakeWizardState state,
        string markedBy)
    {
        if (objectSpace == null || person == null || state == null)
            return new PersonIntakeSaveResult { Succeeded = false, Message = "Nothing to save." };

        if (string.IsNullOrWhiteSpace(person.FirstName) || string.IsNullOrWhiteSpace(person.LastName))
            return new PersonIntakeSaveResult { Succeeded = false, Message = "First name and last name are required." };

        SyncPendingItems(objectSpace, person, state);

        if (!state.PendingKeys.Contains(PersonPendingTargets.Passport)
            && person.Passports?.Any(p => p != null) != true
            && !string.IsNullOrWhiteSpace(state.PassportDraft.PassportNumber))
        {
            var passport = objectSpace.CreateObject<Passport>();
            passport.Person = person;
            passport.PassportNumber = state.PassportDraft.PassportNumber.Trim();
            if (state.PassportDraft.IssueDate != default)
                passport.IssueDate = state.PassportDraft.IssueDate;
            if (state.PassportDraft.ExpirationDate != default)
                passport.ExpirationDate = state.PassportDraft.ExpirationDate;
        }

        PersonIntakeCompleteness.ClearPendingScalars(person);

        var unresolved = PersonIntakeCompleteness
            .Evaluate(person, state.PendingKeys)
            .Where(f => f.State == PersonIntakeItemState.Unresolved)
            .ToList();
        if (unresolved.Count > 0)
        {
            return new PersonIntakeSaveResult
            {
                Succeeded = false,
                Message = $"{unresolved.Count} item(s) still need a value or a Not received mark.",
                Unresolved = unresolved
            };
        }

        var pending = PersonIntakeCompleteness
            .Evaluate(person, state.PendingKeys)
            .Where(f => f.State == PersonIntakeItemState.Pending)
            .ToList();

        if (pending.Count > 0)
            person.IncompleteNotes = string.IsNullOrWhiteSpace(state.FollowNote) ? null : state.FollowNote.Trim();
        else
            person.IncompleteNotes = null;

        PersonPendingRollup.Apply(person, replaceLegacy: true, markedBy ?? string.Empty);

        objectSpace.CommitChanges();
        return new PersonIntakeSaveResult { Succeeded = true, Message = pending.Count > 0 ? "Saved as incomplete." : "Saved as complete." };
    }

    private static void SyncPendingItems(IObjectSpace objectSpace, Person person, PersonIntakeWizardState state)
    {
        person.PendingItems ??= new System.Collections.ObjectModel.ObservableCollection<PersonPendingItem>();

        foreach (var existing in person.PendingItems.Where(item => item != null).ToList())
            objectSpace.Delete(existing);

        foreach (var key in state.PendingKeys)
        {
            if (!PersonIntakeAreaMap.TryGetArea(key, out var area))
                continue;

            var row = objectSpace.CreateObject<PersonPendingItem>();
            row.Person = person;
            row.TargetKey = key;
            row.AreaKey = area;
            if (state.RecordNotes.TryGetValue(key, out var note) && !string.IsNullOrWhiteSpace(note))
                row.Note = note.Trim();
        }
    }
}