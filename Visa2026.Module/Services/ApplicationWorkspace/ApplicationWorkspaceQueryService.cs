using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ApplicationPersonRoster;

namespace Visa2026.Module.Services.ApplicationWorkspace;

/// <summary>
/// Loads Application workspace snapshot from live M2M data (slice 10b).
/// </summary>
public sealed class ApplicationWorkspaceQueryService : IApplicationWorkspaceQueryService
{
    public ApplicationWorkspaceSnapshot Load(IObjectSpace objectSpace, Guid applicationId)
    {
        if (objectSpace == null || applicationId == Guid.Empty)
            return Empty(applicationId);

        ApplicationPersonService.RefreshApplication(objectSpace, applicationId);

        var application = objectSpace.GetObjectByKey<Application>(applicationId);
        if (application == null)
            return Empty(applicationId);

        var profile = application.ApplicationProfile;
        var sla = ApplicationProgressSlaHelper.Resolve(application, application.LatestProgress);

        return new ApplicationWorkspaceSnapshot
        {
            ApplicationId = applicationId,
            Header = BuildHeader(application, sla),
            ProgressHistory = BuildProgressHistory(application),
            Profile = BuildProfileSummary(profile),
            ProfileRail = Array.Empty<ApplicationWorkspaceProfileRailItem>(),
            LinkContextItems = BuildLinkContext(profile),
            Tabs = ApplicationWorkspaceTabBuilder.Build(objectSpace, application, profile),
            IsPrototypeMock = false,
        };
    }

    private static ApplicationWorkspaceSnapshot Empty(Guid applicationId) =>
        new() { ApplicationId = applicationId, IsPrototypeMock = false };

    private static ApplicationWorkspaceHeader BuildHeader(Application application, ApplicationProgressSlaResult sla)
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

    private static IReadOnlyList<ApplicationWorkspaceProgressRow> BuildProgressHistory(Application application) =>
        application.ProgressHistory?
            .OrderBy(p => p.Order)
            .Select(p => new ApplicationWorkspaceProgressRow
            {
                State = p.State?.LocalizedDisplayName ?? p.State?.NameTm ?? p.State?.Code ?? "—",
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
            $"Related to: {FormatActionFamily(profile.ActionFamily)}",
            profile.ProgressRoute == ApplicationProgressRouteKind.DirectToMigrationService
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
        if (profile.RequirePersonTravelHistory) items.Add("TravelHistory");
        if (profile.RequirePersonRejectionItem) items.Add("RejectionItem");
        return items;
    }

    private static string FormatActionFamily(ApplicationProfileActionFamily family) => family switch
    {
        ApplicationProfileActionFamily.Cancellation => "Cancellation",
        ApplicationProfileActionFamily.Registration => "Registration",
        ApplicationProfileActionFamily.BusinessTrip => "Business trip",
        _ => "Issuance",
    };

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
        if (profile.ProduceWorkLocation) items.Add("work location");
        return string.Join(", ", items);
    }
}
