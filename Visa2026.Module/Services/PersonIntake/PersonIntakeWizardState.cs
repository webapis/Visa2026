using System;
using System.Collections.Generic;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services.PersonIntake;

/// <summary>In-memory wizard state before save (pending toggles and follow-up note).</summary>
public sealed class PersonIntakeWizardState
{
    public HashSet<string> PendingKeys { get; } = new(StringComparer.Ordinal);
    public Dictionary<string, string> RecordNotes { get; } = new(StringComparer.Ordinal);

    public string FollowNote { get; set; }

    public PersonIntakePassportDraft PassportDraft { get; set; } = new();

    public void LoadFromPerson(Person person)
    {
        PendingKeys.Clear();
        RecordNotes.Clear();
        if (person?.PendingItems == null)
            return;

        foreach (var item in person.PendingItems)
        {
            if (item == null || string.IsNullOrWhiteSpace(item.TargetKey))
                continue;

            PendingKeys.Add(item.TargetKey);
            if (!string.IsNullOrWhiteSpace(item.Note))
                RecordNotes[item.TargetKey] = item.Note;
        }

        FollowNote = person.IncompleteNotes ?? string.Empty;
    }
}

public sealed class PersonIntakePassportDraft
{
    public string PassportNumber { get; set; }
    public DateTime IssueDate { get; set; } = default;
    public DateTime ExpirationDate { get; set; } = default;
}