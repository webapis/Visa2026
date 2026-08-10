using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Localization;
using Visa2026.Module.Services.ApplicationPersonRoster;

namespace Visa2026.Module.Services.OfficerShell;

/// <summary>
/// Merges selected staged applications into one numbered in-process case and appends the first progress step.
/// </summary>
public sealed class OfficerShellStartProcessService : IOfficerShellStartProcessService
{
    public OfficerShellStartProcessResult Start(IObjectSpace objectSpace, IReadOnlyList<Guid> applicationIds)
    {
        if (objectSpace == null)
            return OfficerShellStartProcessResult.Failed("ObjectSpace is required.");

        if (applicationIds == null || applicationIds.Count == 0)
            return OfficerShellStartProcessResult.Failed("Select at least one staged profile.");

        var ids = applicationIds.Where(id => id != Guid.Empty).Distinct().ToList();
        if (ids.Count == 0)
            return OfficerShellStartProcessResult.Failed("Select at least one staged profile.");

        var applications = ids
            .Select(id => objectSpace.GetObjectByKey<Application>(id))
            .Where(app => app != null)
            .Cast<Application>()
            .ToList();

        if (applications.Count != ids.Count)
            return OfficerShellStartProcessResult.Failed("One or more selected applications were not found.");

        foreach (var application in applications)
        {
            if (!OfficerShellApplicationFilters.IsStagedApplication(application))
                return OfficerShellStartProcessResult.Failed("Only staged profiles (before process number) can be started.");

            if (!OfficerShellApplicationFilters.IsReadyForStartProcess(application))
                return OfficerShellStartProcessResult.Failed("Complete profile and person links on every selected row before starting process.");
        }

        var ordered = applications
            .OrderByDescending(a => a.ApplicationDate)
            .ThenBy(a => a.FullApplicationNumber)
            .ToList();

        var primary = ordered[0];
        var secondaries = ordered.Skip(1).ToList();

        foreach (var secondary in secondaries)
            MergeIntoPrimary(objectSpace, primary, secondary);

        if (!ApplicationProgressProfileResolver.TryValidateProjectContractOnApplication(primary, objectSpace, out var contractError))
            return OfficerShellStartProcessResult.Failed(contractError ?? VisaUiMessages.Get("ApplicationProgress.ProjectContractRequired"));

        var processNumber = OfficerShellProcessNumberAllocator.Allocate(objectSpace);
        primary.ProcessNumber = processNumber;

        var progress = CreateFirstProgressStep(objectSpace, primary, processNumber);
        if (progress == null)
            return OfficerShellStartProcessResult.Failed("Could not resolve the first progress step for this profile route.");

        if (!ApplicationProgressTransitionHelper.TryValidateProgressStep(progress, objectSpace, out var progressError))
            return OfficerShellStartProcessResult.Failed(progressError ?? VisaUiMessages.Get("ApplicationProgress.InvalidForRoute"));

        ApplicationLatestProgressSyncHelper.Sync(primary, objectSpace);

        return OfficerShellStartProcessResult.Succeeded(primary.ID, processNumber, ordered.Count);
    }

    private static void MergeIntoPrimary(IObjectSpace objectSpace, Application primary, Application secondary)
    {
        foreach (var person in ApplicationRosterHelper.GetRosterPeople(secondary))
            ApplicationPersonService.LinkPerson(objectSpace, primary, person);

        if (primary.ProjectContract == null && secondary.ProjectContract != null)
            primary.ProjectContract = secondary.ProjectContract;

        if (primary.ApplicationProfile == null && secondary.ApplicationProfile != null)
            primary.ApplicationProfile = secondary.ApplicationProfile;

        objectSpace.Delete(secondary);
    }

    private static ApplicationProgress? CreateFirstProgressStep(
        IObjectSpace objectSpace,
        Application application,
        string processNumber)
    {
        var progress = objectSpace.CreateObject<ApplicationProgress>();
        progress.Application = application;
        progress.Date = DateTime.Today;

        ApplicationProgressTransitionHelper.TryApplySuggestedNextStep(progress);
        if (progress.State == null)
        {
            var firstCode = ResolveFirstStateCode(application);
            if (string.IsNullOrWhiteSpace(firstCode))
                return null;

            progress.State = objectSpace.GetObjectsQuery<ApplicationState>()
                .FirstOrDefault(state => string.Equals(state.Code, firstCode, StringComparison.OrdinalIgnoreCase));
        }

        if (progress.State == null)
            return null;

        if (ApplicationMigrationSlaHelper.IsMigrationServiceProcessStartedStep(progress.State.Code))
            progress.ProcessNumber = processNumber;

        return progress;
    }

    private static string? ResolveFirstStateCode(Application application)
    {
        var route = ApplicationProgressRouteHelper.GetTypePickerRouteFilter(application);
        if (route == ApplicationProgressRouteKind.DirectToMigrationService)
            return ApplicationProgressStateCodes.ProcessStarted;

        return ApplicationProgressLegCodes.ReviewStarted(1);
    }
}
