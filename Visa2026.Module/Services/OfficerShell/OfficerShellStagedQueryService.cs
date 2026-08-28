using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ApplicationPersonRoster;

namespace Visa2026.Module.Services.OfficerShell;

/// <summary>
/// Staged queue: Applications before process number is assigned (office preparation).
/// </summary>
public sealed class OfficerShellStagedQueryService : IOfficerShellStagedQueryService
{
    public IReadOnlyList<OfficerShellStagedRow> GetStagedProfiles(IObjectSpace objectSpace)
    {
        if (objectSpace == null)
            return Array.Empty<OfficerShellStagedRow>();

        var apps = objectSpace.GetObjectsQuery<ApplicationProfileInstance>()
            .Where(OfficerShellApplicationFilters.IsStaged)
            .OrderByDescending(a => a.ApplicationDate)
            .ThenBy(a => a.FullApplicationNumber)
            .Take(500)
            .ToList();

        return apps.Select(MapRow).ToList();
    }

    private static OfficerShellStagedRow MapRow(ApplicationProfileInstance application)
    {
        var person = ApplicationRosterHelper.GetRosterPeople(application).FirstOrDefault();
        var personName = person?.FullName;
        if (string.IsNullOrWhiteSpace(personName))
            personName = "—";
        var profile = application.ApplicationProfile;
        var hasPeople = ApplicationRosterHelper.GetRosterPersonCountInMemory(application) > 0;
        var hasProfile = profile != null;
        var readiness = hasPeople && hasProfile ? "ready" : "incomplete";
        var missing = new List<string>();
        if (!hasProfile)
            missing.Add("Profile");
        if (!hasPeople)
            missing.Add("Person");

        return new OfficerShellStagedRow
        {
            ApplicationProfileInstanceId = application.ID,
            PersonName = personName,
            ProfileName = profile?.Name ?? application.ApplicationType?.NameTm ?? "—",
            ProfileCode = profile?.Code ?? application.ApplicationType?.Code,
            ProfileId = profile?.ID,
            ProjectName = application.ProjectContract?.Name,
            TemplateFamilyKey = OfficerShellTemplateFamily.ResolveKey(application),
            StagedOn = application.ApplicationDate == default
                ? string.Empty
                : application.ApplicationDate.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture),
            Readiness = readiness,
            IsSelectable = readiness == "ready",
            MissingSummary = missing.Count == 0 ? null : string.Join(", ", missing),
            SearchHaystack = OfficerShellApplicationSearch.BuildHaystack(
                application,
                personName,
                profile?.Name,
                application.ApplicationType?.NameTm,
                application.ProjectContract?.Name),
        };
    }
}
