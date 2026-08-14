using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ApplicationPersonRoster;

namespace Visa2026.Module.Services.OfficerShell;

/// <summary>
/// In-process queue: Applications with process number or past office preparation.
/// </summary>
public sealed class OfficerShellInProcessQueryService : IOfficerShellInProcessQueryService
{
    public IReadOnlyList<OfficerShellInProcessRow> GetInProcessProfiles(IObjectSpace objectSpace)
    {
        if (objectSpace == null)
            return Array.Empty<OfficerShellInProcessRow>();

        var apps = objectSpace.GetObjectsQuery<ApplicationProfileInstance>()
            .Where(OfficerShellApplicationFilters.IsInProcess)
            .OrderByDescending(a => a.ApplicationDate)
            .ThenBy(a => a.FullApplicationNumber)
            .Take(500)
            .ToList();

        return apps.Select(MapRow).ToList();
    }

    private static OfficerShellInProcessRow MapRow(ApplicationProfileInstance application)
    {
        var person = ApplicationRosterHelper.GetRosterPeople(application).FirstOrDefault();
        var personName = person?.FullName;
        if (string.IsNullOrWhiteSpace(personName))
            personName = "—";
        var profile = application.ApplicationProfile;
        var sla = ApplicationProfileInstanceProgressSlaHelper.Resolve(application, application.LatestProgress);
        var processNumber = !string.IsNullOrWhiteSpace(application.ProcessNumber)
            ? application.ProcessNumber
            : ApplicationProcessNumberHelper.ResolveFromHistory(application.ProgressHistory);

        return new OfficerShellInProcessRow
        {
            ApplicationProfileInstanceId = application.ID,
            ApplicationNumber = application.FullApplicationNumber
                ?? application.ApplicationNumber
                ?? "—",
            ProcessNumber = processNumber,
            PersonName = personName,
            ProfileName = profile?.Name ?? application.ApplicationType?.NameTm ?? "—",
            ProfileCode = profile?.Code ?? application.ApplicationType?.Code,
            ProjectName = application.ProjectContract?.Name,
            StartedOn = application.ApplicationDate == default
                ? string.Empty
                : application.ApplicationDate.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture),
            CurrentStep = application.LatestProgress?.State?.LocalizedDisplayName
                ?? application.LatestProgress?.State?.NameTm
                ?? application.LatestPrimaryStateCode
                ?? "—",
            SlaDaysRemaining = sla.MaxDaysInReview is int maxDays && sla.WorkingDaysInCurrentStep is int elapsed
                ? maxDays - elapsed
                : null,
            Status = "process",
            TemplateFamilyKey = OfficerShellTemplateFamily.ResolveKey(application),
        };
    }
}
