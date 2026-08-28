using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using DevExpress.ExpressApp;
using Microsoft.EntityFrameworkCore;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ApplicationPersonRoster;
using Visa2026.Module.Services.ApplicationProfilePicker;
using Visa2026.Module.Services.OfficerShell;

namespace Visa2026.Module.Services.ApplicationWorkspace;

/// <summary>
/// Loads ApplicationProfileInstance workspace snapshot from live M2M data (slice 10b).
/// </summary>
public sealed class ApplicationWorkspaceQueryService : IApplicationWorkspaceQueryService
{
    public ApplicationWorkspaceSnapshot Load(IObjectSpace objectSpace, Guid applicationId)
    {
        if (objectSpace == null || applicationId == Guid.Empty)
            return Empty(applicationId);

        // Open is read-only. Auto-heal of resolved links runs on Link person, not on every open
        // (RefreshApplication walks every person collection and was discarded without CommitChanges).
        var application = LoadInstance(objectSpace, applicationId);
        if (application == null)
            return Empty(applicationId);

        var profile = application.ApplicationProfile;
        var latest = ApplicationProfileInstanceProgressHelper.GetLatest(application.ProgressHistory, objectSpace);
        var ministrySla = ApplicationProfileInstanceProgressSlaHelper.Resolve(application, latest);
        var sla = ApplicationWorkspaceProgressTimeline.ResolveCurrentSla(application, profile, latest, ministrySla);
        var tabs = ApplicationWorkspaceTabBuilder.Build(objectSpace, application, profile);
        var caseChrome = BuildCaseChrome(application, profile, sla, objectSpace);
        var caseView = ApplicationWorkspaceCaseBuilder.Build(application, profile, tabs, sla, caseChrome, objectSpace);

        return new ApplicationWorkspaceSnapshot
        {
            ApplicationProfileInstanceId = applicationId,
            Header = BuildHeader(application, sla),
            ProgressHistory = BuildProgressHistory(application),
            Profile = BuildProfileSummary(profile),
            ProfileRail = Array.Empty<ApplicationWorkspaceProfileRailItem>(),
            LinkContextItems = BuildLinkContext(profile),
            Tabs = tabs,
            CaseChrome = caseView.Chrome,
            CaseView = caseView,
            IsPrototypeMock = false,
        };
    }

    private static ApplicationProfileInstance? LoadInstance(IObjectSpace objectSpace, Guid applicationId)
    {
        try
        {
            return objectSpace.GetObjectsQuery<ApplicationProfileInstance>()
                .Include(a => a.ProgressHistory)
                    .ThenInclude(p => p.State)
                .Include(a => a.People)
                .Include(a => a.PersonResolvedLinks)
                .Include(a => a.ApplicationProfile)
                    .ThenInclude(p => p!.ApprovalLegs)
                        .ThenInclude(l => l.ApprovingMinistry)
                .Include(a => a.ApplicationProfile)
                    .ThenInclude(p => p!.ApprovalLegVersions)
                        .ThenInclude(v => v.Legs)
                            .ThenInclude(l => l.ApprovingMinistry)
                .Include(a => a.ApprovalLegSnapshots)
                .Include(a => a.LatestProgress)
                    .ThenInclude(p => p!.State)
                .Include(a => a.VisaType)
                .Include(a => a.VisaCategory)
                .Include(a => a.VisaPeriod)
                .Include(a => a.ProjectContract)
                .Include(a => a.Urgency)
                .Include(a => a.MigrationService)
                .Include(a => a.FromCity)
                .Include(a => a.ToCity)
                .Include(a => a.Region)
                .Include(a => a.City)
                    .ThenInclude(c => c.Region)
                .Include(a => a.BusinessTripAddress)
                    .ThenInclude(b => b.City)
                .Include(a => a.EntryCheckPoint)
                .FirstOrDefault(a => a.ID == applicationId);
        }
        catch (Exception)
        {
            return objectSpace.GetObjectByKey<ApplicationProfileInstance>(applicationId);
        }
    }

    private static ApplicationWorkspaceCaseChrome BuildCaseChrome(
        ApplicationProfileInstance application,
        ApplicationProfile? profile,
        ApplicationProfileInstanceProgressSlaResult sla,
        IObjectSpace? objectSpace)
    {
        var processNumber = !string.IsNullOrWhiteSpace(application.ProcessNumber)
            ? application.ProcessNumber.Trim()
            : ApplicationProcessNumberHelper.ResolveFromHistory(application.ProgressHistory) ?? string.Empty;

        var displayNumber = !string.IsNullOrWhiteSpace(application.FullApplicationNumber)
            ? application.FullApplicationNumber.Trim()
            : !string.IsNullOrWhiteSpace(application.ApplicationNumber)
                ? application.ApplicationNumber.Trim()
                : processNumber;

        var familyKey = OfficerShellTemplateFamily.ResolveKey(application);
        var people = ApplicationRosterHelper.GetRosterPeople(application)
            .Select(p => p.FullName)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Cast<string>()
            .ToList();

        var progressSteps = ApplicationWorkspaceProgressTimeline.Build(application, profile, sla, objectSpace);
        var currentStep = ApplicationWorkspaceProgressTimeline.FormatChromeCurrentStep(progressSteps);

        int? slaRemaining = sla.MaxDaysInReview is int maxDays && sla.WorkingDaysInCurrentStep is int elapsed
            ? Math.Max(0, maxDays - elapsed)
            : null;

        return new ApplicationWorkspaceCaseChrome
        {
            DisplayNumber = displayNumber,
            ProcessNumber = processNumber,
            TemplateFamilyKey = familyKey,
            TemplateFamilyLabel = OfficerShellTemplateFamily.GetLabel(familyKey),
            StartedOn = application.ApplicationDate == default
                ? string.Empty
                : application.ApplicationDate.ToString("dd MMM yyyy", CultureInfo.InvariantCulture),
            CurrentStep = currentStep,
            ProjectName = application.ProjectContract?.Name ?? "—",
            SlaDaysRemaining = slaRemaining,
            PeopleNames = people,
            MergedFromCount = people.Count > 1 ? people.Count : null,
            ShowProcessNumber = ApplicationProfileConfigurationResolver.ShowProcessNumber(application),
            ProfileTemplateName = profile?.Name ?? string.Empty,
            ResolvedLinksLocked = ApplicationProfileInstancePersonRosterLockHelper.AreResolvedLinksLocked(application),
        };
    }

    private static ApplicationWorkspaceSnapshot Empty(Guid applicationId) =>
        new() { ApplicationProfileInstanceId = applicationId, IsPrototypeMock = false };

    private static ApplicationWorkspaceHeader BuildHeader(ApplicationProfileInstance application, ApplicationProfileInstanceProgressSlaResult sla)
    {
        var urgency = application.Urgency?.LocalizedDisplayName
            ?? application.Urgency?.NameTm
            ?? string.Empty;

        return new ApplicationWorkspaceHeader
        {
            ApplicationNumber = application.FullApplicationNumber
                ?? application.ApplicationNumber
                ?? string.Empty,
            ApplicationDate = application.ApplicationDate == default
                ? string.Empty
                : application.ApplicationDate.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture),
            Urgency = urgency,
            ProgressStep = Math.Max(1, application.ProgressHistory?.Count ?? 0),
            ProgressTotalSteps = Math.Max(application.ProgressHistory?.Count ?? 1, 6),
            SlaDaysElapsed = sla.WorkingDaysInCurrentStep ?? 0,
            SlaDaysTotal = sla.MaxDaysInReview ?? 0,
        };
    }

    private static IReadOnlyList<ApplicationWorkspaceProgressRow> BuildProgressHistory(ApplicationProfileInstance application) =>
        application.ProgressHistory?
            .OrderBy(p => p.Order)
            .Select(p => new ApplicationWorkspaceProgressRow
            {
                State = ApplicationWorkspaceProgressTimeline.FormatProfileStateLabel(p.State?.Code)
                    is { Length: > 0 } label
                    ? label
                    : (p.State?.Code ?? "—"),
                Date = p.Date == default
                    ? string.Empty
                    : p.Date.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture),
                Description = p.Description ?? string.Empty,
            })
            .ToList()
        ?? [];

    private static ApplicationWorkspaceProfileSummary BuildProfileSummary(ApplicationProfile? profile)
    {
        if (profile == null)
        {
            return new ApplicationWorkspaceProfileSummary
            {
                ProfileId = Guid.Empty,
                Title = "No Application Profile",
                Code = string.Empty,
                Chips = ["Profile not set on this Application"],
            };
        }

        var chips = new List<string>
        {
            $"Related to: {ApplicationProfilePickerDisplayHelper.FormatRelatedTo(profile)}",
            profile.ProgressRoute == ApplicationProfileInstanceProgressRouteKind.DirectToMigrationService
                ? "Direct migration"
                : "Via ministry",
            FormatAudience(profile),
        };

        var produce = FormatProduce(profile);
        if (!string.IsNullOrWhiteSpace(produce))
            chips.Add($"Produces: {produce}");

        return new ApplicationWorkspaceProfileSummary
        {
            ProfileId = profile.ID,
            Title = profile.Name,
            Code = profile.Code,
            Chips = chips,
        };
    }


    private static IReadOnlyList<string> BuildLinkContext(ApplicationProfile? profile)
    {
        if (profile == null)
            return ["Person"];

        var items = new List<string> { "Person" };
        if (profile.RequirePersonPassport) items.Add("Passport");
        if (profile.RequirePersonVisa) items.Add("Visa");
        if (profile.RequirePersonAddressOfResidence) items.Add("AddressOfResidence");
        if (profile.RequirePersonWorkPermitItem) items.Add("WorkPermitItem");
        if (profile.RequirePersonInvitationItem) items.Add("InvitationItem");
        if (profile.RequirePersonBorderZoneItem) items.Add("BorderZoneItem");
        if (profile.RequirePersonEducation) items.Add("Education");
        if (profile.RequirePersonSalary) items.Add("EmployeeSalary");
        if (profile.RequirePersonPosition) items.Add("EmployeePositionHistory");
        if (profile.RequirePersonMedical) items.Add("MedicalRecord");
        if (profile.RequirePersonTravelHistory
            && profile.ActionFamily != ApplicationProfileActionFamily.BusinessTrip)
            items.Add("TravelHistory");
        if (profile.RequirePersonRejectionItem) items.Add("RejectionItem");
        return items;
    }

    private static string FormatAudience(ApplicationProfile profile)
    {
        var parts = new List<string>();
        if (profile.ForEmployee) parts.Add("Employee");
        if (profile.ForFamilyMember) parts.Add("Family");
        if (profile.ForTemporaryVisitor) parts.Add("Visitor");
        return parts.Count == 0 ? "For: —" : "For: " + string.Join(", ", parts);
    }

    private static string FormatProduce(ApplicationProfile profile)
    {
        var items = new List<string>();
        if (profile.ProduceInvitation) items.Add("invitation");
        if (profile.ProduceWorkPermit) items.Add("work permit");
        if (profile.ProduceVisa) items.Add("visa");
        if (profile.ProduceBorderZone) items.Add("border zone");
        if (profile.ProduceRejection) items.Add("rejection");
        if (profile.ProduceWorkLocation) items.Add("work location");
        return string.Join(", ", items);
    }
}
