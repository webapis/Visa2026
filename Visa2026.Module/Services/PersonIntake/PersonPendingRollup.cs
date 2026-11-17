using System;
using System.Linq;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services.PersonIntake;

/// <summary>
/// Writes the person-level incomplete summary from <see cref="PersonPendingItem"/> rows.
/// </summary>
public static class PersonPendingRollup
{
    /// <param name="replaceLegacy">
    /// True on a wizard save. An empty pending set then clears the flag and notes.
    /// False leaves a legacy <see cref="Person.IsDataIncomplete"/> flag in place when no pending rows exist.
    /// </param>
    public static void Apply(Person person, bool replaceLegacy, string markedBy)
    {
        if (person == null)
            return;

        var items = person.PendingItems?
            .Where(item => item != null && !string.IsNullOrWhiteSpace(item.TargetKey))
            .ToList();

        if (items == null || items.Count == 0)
        {
            if (!replaceLegacy)
                return;

            person.IsDataIncomplete = false;
            SetAreas(person, Array.Empty<string>());
            person.IncompleteNotes = null;
            person.IncompleteMarkedOn = null;
            person.IncompleteMarkedBy = null;
            return;
        }

        person.IsDataIncomplete = true;
        SetAreas(person, items.Select(item => item.AreaKey));
        person.IncompleteMarkedOn = DateTime.Now;
        person.IncompleteMarkedBy = string.IsNullOrWhiteSpace(markedBy) ? person.IncompleteMarkedBy : markedBy.Trim();
    }

    private static void SetAreas(Person person, System.Collections.Generic.IEnumerable<string> areaKeys)
    {
        var areas = new System.Collections.Generic.HashSet<string>(areaKeys.Where(area => !string.IsNullOrWhiteSpace(area)));
        person.IncompleteMissingPersonalData = areas.Contains(PersonIncompleteDataLabels.PersonalData);
        person.IncompleteMissingPassport = areas.Contains(PersonIncompleteDataLabels.Passport);
        person.IncompleteMissingCv = areas.Contains(PersonIncompleteDataLabels.Cv);
        person.IncompleteMissingPhoto = areas.Contains(PersonIncompleteDataLabels.Photo);
        person.IncompleteMissingEducation = areas.Contains(PersonIncompleteDataLabels.Education);
        person.IncompleteMissingMedical = areas.Contains(PersonIncompleteDataLabels.Medical);
        person.IncompleteMissingAddress = areas.Contains(PersonIncompleteDataLabels.Address);
        person.IncompleteMissingFamilyDocs = areas.Contains(PersonIncompleteDataLabels.FamilyDocs);
        person.IncompleteMissingOther = areas.Contains(PersonIncompleteDataLabels.Other);
    }
}
