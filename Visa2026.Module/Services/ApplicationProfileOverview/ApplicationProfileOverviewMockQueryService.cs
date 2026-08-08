using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ApplicationProfilePicker;

namespace Visa2026.Module.Services.ApplicationProfileOverview;

/// <summary>
/// Prototype overview — merges real <see cref="ApplicationProfile"/> scalars when available;
/// linked-application rows and some summary lines remain mock until slice 10b+.
/// </summary>
public sealed class ApplicationProfileOverviewMockQueryService : IApplicationProfileOverviewQueryService
{
    public ApplicationProfileOverviewSnapshot Load(Guid applicationProfileId, IObjectSpace? objectSpace = null)
    {
        if (objectSpace != null && applicationProfileId != Guid.Empty)
        {
            var profile = objectSpace.GetObjectByKey<ApplicationProfile>(applicationProfileId);
            if (profile != null)
                return MapFromProfile(profile, objectSpace);
        }

        return BuildFallbackMock(applicationProfileId);
    }

    private static ApplicationProfileOverviewSnapshot MapFromProfile(ApplicationProfile profile, IObjectSpace objectSpace)
    {
        var audience = new List<string>();
        if (profile.ForEmployee)
            audience.Add("Employee");
        if (profile.ForFamilyMember)
            audience.Add("Family member");
        if (profile.ForTemporaryVisitor)
            audience.Add("Temporary visitor");

        var liveLines = BuildLiveConfigurationLines(profile);
        var defaults = BuildDefaultRows(profile);
        var legs = profile.ApprovalLegs?
            .OrderBy(l => l.Sequence)
            .Select(l => new ApplicationProfileOverviewLegRow
            {
                Sequence = l.Sequence ?? 0,
                MinistryName = l.ApprovingMinistry?.ShortNameTm ?? "—",
            })
            .ToList() ?? [];

        if (legs.Count == 0)
        {
            legs =
            [
                new() { Sequence = 1, MinistryName = "Türkmenenergo (mock)" },
                new() { Sequence = 2, MinistryName = "Migration service (mock)" },
            ];
        }

        var personToggles = BuildPersonToggles(profile);
        var templates = profile.NestedTemplates?
            .OrderBy(t => t.SortOrder)
            .Select(t => new ApplicationProfileOverviewTemplateRow
            {
                Name = t.TemplateName,
                Kind = t.TemplateKind.ToString(),
            })
            .ToList() ?? [];

        if (templates.Count == 0)
        {
            templates =
            [
                new() { Name = "Invitation package (mock)", Kind = "Word" },
                new() { Name = "Ministry letter (mock)", Kind = "Word" },
            ];
        }

        var linkedApps = BuildLinkedApplicationsMock();
        var locked = ApplicationProfileLockHelper.IsProfileConfigLocked(profile, objectSpace);

        return new ApplicationProfileOverviewSnapshot
        {
            ApplicationProfileId = profile.ID,
            Name = profile.Name,
            Code = profile.Code,
            Description = profile.Description,
            ActionFamilyLabel = ApplicationProfilePickerDisplayHelper.FormatActionFamily(profile.ActionFamily),
            ProgressRouteLabel = ApplicationProfilePickerDisplayHelper.FormatProgressRoute(profile.ProgressRoute),
            AudienceLabels = audience,
            IsConfigLocked = locked,
            IsActive = profile.IsActive,
            LiveConfigurationLines = liveLines,
            PerApplicationDefaults = defaults,
            ApprovalLegs = legs,
            PersonDataToggles = personToggles,
            NestedTemplates = templates,
            LinkedApplications = linkedApps,
            LinkedApplicationCount = linkedApps.Count,
            IsPrototypeMock = true,
        };
    }

    private static List<string> BuildLiveConfigurationLines(ApplicationProfile profile)
    {
        var produce = new List<string>();
        if (profile.ProduceInvitation)
            produce.Add("invitation");
        if (profile.ProduceWorkPermit)
            produce.Add("work permit");
        if (profile.ProduceVisa)
            produce.Add("visa");
        if (profile.ProduceBorderZone)
            produce.Add("border zone");
        if (profile.ProduceWorkLocation)
            produce.Add("work location");

        var cancel = new List<string>();
        if (profile.CancelInvitations)
            cancel.Add("invitations");
        if (profile.CancelWorkPermits)
            cancel.Add("work permits");
        if (profile.CancelVisas)
            cancel.Add("visas");

        var lines = new List<string>
        {
            $"Ministry SLA: {profile.MinistrySlaDays} days",
            $"Migration SLA: {profile.MigrationSlaDays} days",
        };

        if (produce.Count > 0)
            lines.Add("May produce: " + string.Join(", ", produce));
        if (cancel.Count > 0)
            lines.Add("May cancel: " + string.Join(", ", cancel));

        return lines;
    }

    private static List<ApplicationProfileOverviewDefaultRow> BuildDefaultRows(ApplicationProfile profile)
    {
        var rows = new List<ApplicationProfileOverviewDefaultRow>();

        void Add(string label, string? value, bool required)
        {
            if (!required && string.IsNullOrWhiteSpace(value))
                return;

            rows.Add(new ApplicationProfileOverviewDefaultRow
            {
                FieldLabel = label,
                DefaultValue = string.IsNullOrWhiteSpace(value) ? "—" : value,
                Required = required,
            });
        }

        Add("Visa Type", profile.DefaultVisaType?.NameTm ?? profile.DefaultVisaType?.Name, profile.RequireVisaType);
        Add("Visa Category", profile.DefaultVisaCategory?.NameTm ?? profile.DefaultVisaCategory?.Name, profile.RequireVisaCategory);
        Add("Visa Period", profile.DefaultVisaPeriod?.NameTm ?? profile.DefaultVisaPeriod?.Name, profile.RequireVisaPeriod);
        Add("Urgency", profile.DefaultUrgency?.NameTm ?? profile.DefaultUrgency?.Name, profile.RequireUrgency);
        Add("Project Contract", profile.DefaultProjectContract?.NameTm ?? profile.DefaultProjectContract?.Name, profile.RequireProject);
        Add("Migration Service", profile.DefaultMigrationService?.NameTm ?? profile.DefaultMigrationService?.Name, profile.RequireMigrationService);
        Add("Border Zone", profile.DefaultBorderZoneLocation, profile.RequireBorderZone);
        Add("Entry Check Point", profile.DefaultEntryCheckPoint?.NameTm ?? profile.DefaultEntryCheckPoint?.Name, profile.RequireEntryCheckPoint);

        if (rows.Count == 0)
        {
            rows.Add(new ApplicationProfileOverviewDefaultRow
            {
                FieldLabel = "Visa Type",
                DefaultValue = "WP (mock default)",
                Required = true,
            });
        }

        return rows;
    }

    private static List<string> BuildPersonToggles(ApplicationProfile profile)
    {
        var toggles = new List<string>();
        if (profile.RequirePersonPassport)
            toggles.Add("Passport");
        if (profile.RequirePersonEducation)
            toggles.Add("Education");
        if (profile.RequirePersonPosition)
            toggles.Add("Position");
        if (profile.RequirePersonAddressOfResidence)
            toggles.Add("Local address");
        if (profile.RequirePersonVisa)
            toggles.Add("Visa");
        if (profile.RequirePersonInvitationItem)
            toggles.Add("Invitation item");
        if (profile.RequirePersonWorkPermitItem)
            toggles.Add("Work permit item");
        if (profile.RequirePersonTravelHistory)
            toggles.Add("Travel history");

        if (toggles.Count == 0)
            toggles.Add("Passport (mock)");

        return toggles;
    }

    private static List<ApplicationProfileOverviewLinkedAppRow> BuildLinkedApplicationsMock() =>
    [
        new() { FullNumber = "12/-7010", ApplicationDate = "01.08.2026", Status = "Office preparation" },
        new() { FullNumber = "12/-6988", ApplicationDate = "15.07.2026", Status = "Submitted (ministry)" },
        new() { FullNumber = "12/-6901", ApplicationDate = "02.06.2026", Status = "Issued" },
    ];

    private static ApplicationProfileOverviewSnapshot BuildFallbackMock(Guid applicationProfileId) =>
        new()
        {
            ApplicationProfileId = applicationProfileId == Guid.Empty
                ? Guid.Parse("22222222-2222-2222-2222-222222222222")
                : applicationProfileId,
            Name = "Invitation + work permit (employee)",
            Code = "INV_WP_EMP",
            Description = "Prototype profile — mock overview when profile id is not resolved.",
            ActionFamilyLabel = "Issuance",
            ProgressRouteLabel = "Via ministry",
            AudienceLabels = ["Employee"],
            IsConfigLocked = false,
            LiveConfigurationLines =
            [
                "May produce: invitation, work permit, visa",
                "Ministry SLA: 14 days",
                "Migration SLA: 14 days",
            ],
            PerApplicationDefaults =
            [
                new() { FieldLabel = "Visa Type", DefaultValue = "WP", Required = true },
                new() { FieldLabel = "Visa Period", DefaultValue = "6 months", Required = true },
                new() { FieldLabel = "Project Contract", DefaultValue = "Plant expansion", Required = true },
            ],
            ApprovalLegs =
            [
                new() { Sequence = 1, MinistryName = "Türkmenenergo" },
                new() { Sequence = 2, MinistryName = "Migration service" },
            ],
            PersonDataToggles = ["Passport", "Position", "Education"],
            NestedTemplates =
            [
                new() { Name = "Invitation package", Kind = "Word" },
                new() { Name = "Work permit forms", Kind = "PdfForm" },
            ],
            LinkedApplications = BuildLinkedApplicationsMock(),
            LinkedApplicationCount = 3,
            IsPrototypeMock = true,
        };
}
