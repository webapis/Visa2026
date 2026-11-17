using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services.ApplicationWorkspace;

/// <summary>
/// On Wiza we Iş Rugsatnamasyny Uzaltmak, after every work permit is issued:
/// the same visa-issue day counter as the list, plus the people who still have no visa.
/// Those names are the passports the office is waiting to receive. The names go away
/// when every visa is issued; the counter stays frozen.
/// </summary>
public sealed class VisaIssuePassportWait
{
    public VisaIssuePassportWait(VisaIssueWindowCalculator.Badge counter, IReadOnlyList<string> names)
    {
        Counter = counter;
        Names = names ?? Array.Empty<string>();
    }

    public VisaIssueWindowCalculator.Badge Counter { get; }

    public IReadOnlyList<string> Names { get; }

    public static VisaIssuePassportWait? TryLoad(IObjectSpace? objectSpace, ApplicationProfileInstance? application)
    {
        if (objectSpace == null || application == null || application.ID == Guid.Empty)
            return null;
        if (!VisaIssueWindowCalculator.Applies(application.ApplicationProfile?.Code))
            return null;

        ApplicationWorkspaceIssuedResultListCoverage.ApplyTo(objectSpace, new[] { application });
        var badge = application.ListViewVisaIssueWindow;
        if (badge == null)
            return null;

        var names = string.Equals(badge.Value.Tone, "done", StringComparison.Ordinal)
            ? Array.Empty<string>()
            : LoadWaitingNames(objectSpace, application);
        return new VisaIssuePassportWait(badge.Value, names);
    }

    /// <summary>Active-roster people who have a work permit on this case and no visa yet.</summary>
    public static IReadOnlyList<string> SelectWaiting(
        IEnumerable<(Guid Id, string Name)> roster,
        IReadOnlySet<Guid> workPermitPersonIds,
        IReadOnlySet<Guid> visaPersonIds)
    {
        if (roster == null)
            return Array.Empty<string>();

        workPermitPersonIds ??= new HashSet<Guid>();
        visaPersonIds ??= new HashSet<Guid>();
        var names = new List<(string Name, Guid Id)>();
        var seen = new HashSet<Guid>();
        foreach (var person in roster)
        {
            if (person.Id == Guid.Empty || !seen.Add(person.Id))
                continue;
            if (!workPermitPersonIds.Contains(person.Id) || visaPersonIds.Contains(person.Id))
                continue;
            names.Add((string.IsNullOrWhiteSpace(person.Name) ? "—" : person.Name.Trim(), person.Id));
        }

        names.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.CurrentCultureIgnoreCase));
        return names.Select(person => person.Name).ToList();
    }

    private static IReadOnlyList<string> LoadWaitingNames(IObjectSpace objectSpace, ApplicationProfileInstance application)
    {
        var roster = ApplicationWorkspaceIssuedResultOverview.RosterPersonIds(application, objectSpace);
        var workPermits = new HashSet<Guid>(objectSpace.GetObjectsQuery<WorkPermitItem>()
            .Where(item => item.WorkPermit != null
                && item.WorkPermit.ApplicationProfileInstance != null
                && item.WorkPermit.ApplicationProfileInstance.ID == application.ID
                && item.Person != null)
            .Select(item => item.Person.ID));
        var visas = new HashSet<Guid>(objectSpace.GetObjectsQuery<Visa>()
            .Where(visa => visa.IssuingApplicationProfileInstance != null
                && visa.IssuingApplicationProfileInstance.ID == application.ID
                && visa.Passport != null
                && visa.Passport.Person != null)
            .Select(visa => visa.Passport.Person.ID));

        var nameById = new Dictionary<Guid, string>();
        if (application.People != null)
        {
            foreach (var person in application.People)
            {
                if (person != null && person.ID != Guid.Empty)
                    nameById[person.ID] = DisplayName(person);
            }
        }

        var missing = roster.Where(id => !nameById.ContainsKey(id)).ToList();
        if (missing.Count > 0)
        {
            foreach (var person in objectSpace.GetObjectsQuery<Person>().Where(person => missing.Contains(person.ID)))
                nameById[person.ID] = DisplayName(person);
        }

        return SelectWaiting(
            roster.Select(id => (id, nameById.TryGetValue(id, out var name) ? name : "—")),
            workPermits,
            visas);
    }

    private static string DisplayName(Person person) =>
        string.IsNullOrWhiteSpace(person.FullName) ? "—" : person.FullName.Trim();
}
