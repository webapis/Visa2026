using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp;
using Microsoft.EntityFrameworkCore;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ApplicationProfilePicker;
using Visa2026.Module.Services.ApplicationProfileWizard;
using Visa2026.Module.Services.UserReports;

namespace Visa2026.Module.Services.ApplicationProfileOverview;

/// <summary>
/// Live Application Profile overview — configuration, defaults, legs, nested templates,
/// and linked <see cref="ApplicationProfileInstance"/> rows. Mock snapshot only when the
/// profile id cannot be resolved (designer / missing object space).
/// </summary>
public sealed class ApplicationProfileOverviewQueryService : IApplicationProfileOverviewQueryService
{
    public const int LinkedApplicationsDisplayCap = 25;

    public ApplicationProfileOverviewSnapshot Load(Guid applicationProfileId, IObjectSpace? objectSpace = null)
    {
        if (objectSpace != null && applicationProfileId != Guid.Empty)
        {
            var profile = LoadProfile(objectSpace, applicationProfileId);
            if (profile != null)
                return MapFromProfile(profile, objectSpace);
        }

        return BuildFallbackMock(applicationProfileId);
    }

    private static ApplicationProfile? LoadProfile(IObjectSpace objectSpace, Guid applicationProfileId)
    {
        try
        {
            return objectSpace.GetObjectsQuery<ApplicationProfile>()
                .Include(p => p.ApprovalLegVersions)
                    .ThenInclude(v => v.Legs)
                        .ThenInclude(l => l.ApprovingMinistry)
                .Include(p => p.ApprovalLegs)
                    .ThenInclude(l => l.ApprovingMinistry)
                .Include(p => p.NestedTemplates)
                .Include(p => p.DefaultVisaType)
                .Include(p => p.DefaultVisaCategory)
                .Include(p => p.DefaultVisaPeriod)
                .Include(p => p.DefaultUrgency)
                .Include(p => p.DefaultProjectContract)
                .Include(p => p.DefaultMigrationService)
                .Include(p => p.DefaultEntryCheckPoint)
                .FirstOrDefault(p => p.ID == applicationProfileId);
        }
        catch (Exception)
        {
            return objectSpace.GetObjectByKey<ApplicationProfile>(applicationProfileId);
        }
    }

    internal static ApplicationProfileOverviewSnapshot MapFromProfile(
        ApplicationProfile profile,
        IObjectSpace? objectSpace)
    {
        var audience = new List<string>();
        if (profile.ForEmployee)
            audience.Add("Employee");
        if (profile.ForFamilyMember)
            audience.Add("Family member");
        if (profile.ForTemporaryVisitor)
            audience.Add("Temporary visitor");

        var versions = ApplicationProfileApprovalLegVersionHelper.GetOrderedVersions(profile)
            .Select(v => new ApplicationProfileOverviewVersionRow
            {
                Name = string.IsNullOrWhiteSpace(v.Name) ? $"Version {v.Sequence}" : v.Name,
                IsDefault = v.IsDefault,
                Legs = ApplicationProfileApprovalLegVersionHelper.GetOrderedLegs(v)
                    .Select((l, i) => new ApplicationProfileOverviewLegRow
                    {
                        Sequence = i + 1,
                        MinistryName = l.ApprovingMinistry?.LocalizedDisplayName
                            ?? l.ApprovingMinistry?.ShortNameTm
                            ?? l.ApprovingMinistry?.NameTm
                            ?? "—",
                    })
                    .ToList(),
            })
            .ToList();

        var legs = versions.Count > 0
            ? versions.FirstOrDefault(v => v.IsDefault)?.Legs ?? versions[0].Legs
            : profile.ApprovalLegs?
                .OrderBy(l => l.Sequence)
                .Select(l => new ApplicationProfileOverviewLegRow
                {
                    Sequence = l.Sequence ?? 0,
                    MinistryName = l.ApprovingMinistry?.LocalizedDisplayName
                        ?? l.ApprovingMinistry?.ShortNameTm
                        ?? l.ApprovingMinistry?.NameTm
                        ?? "—",
                })
                .ToList() ?? [];

        var templates = profile.NestedTemplates?
            .OrderBy(t => t.SortOrder)
            .Select(t => new ApplicationProfileOverviewTemplateRow
            {
                Name = string.IsNullOrWhiteSpace(t.TemplateName) ? "—" : t.TemplateName,
                Kind = FormatTemplateKind(t.TemplateKind),
                Scope = FormatCatalogScope(t.CatalogScope),
                DataScope = FormatDataScope(t.DataScope),
                Category = FormatCategory(t.CategoryKey),
            })
            .ToList() ?? [];

        var (linkedRows, linkedCount) = LoadLinkedApplications(profile, objectSpace);
        var locked = ApplicationProfileLockHelper.IsProfileConfigLocked(profile, objectSpace);
        var viaMinistry = profile.ProgressRoute
            == ApplicationProfileInstanceProgressRouteKind.ViaMinistries;

        return new ApplicationProfileOverviewSnapshot
        {
            ApplicationProfileId = profile.ID,
            Name = profile.Name,
            Code = profile.Code,
            SelectionCode = profile.SelectionCode,
            Description = profile.Description,
            ActionFamilyLabel = ApplicationProfilePickerDisplayHelper.FormatActionFamily(profile.ActionFamily),
            ProgressRouteLabel = ApplicationProfilePickerDisplayHelper.FormatProgressRoute(profile.ProgressRoute),
            IsViaMinistry = viaMinistry,
            IsAlwaysAvailable = string.IsNullOrWhiteSpace(profile.ApplicabilityCriteria),
            ApplicabilityCriteria = profile.ApplicabilityCriteria,
            AudienceLabels = audience,
            IsConfigLocked = locked,
            IsActive = profile.IsActive,
            LiveConfigurationLines = BuildLiveConfigurationLines(profile),
            Organization = ApplicationProfileWizardOrganizationSnapshot.Load(objectSpace),
            MinistrySlaDays = profile.MinistrySlaDays,
            MigrationSlaDays = profile.MigrationSlaDays,
            ProgressStates = [],
            PerApplicationDefaults = BuildDefaultRows(profile),
            ApprovalLegs = legs,
            ApprovalLegVersions = versions,
            PersonDataToggles = BuildPersonToggles(profile),
            NestedTemplates = templates,
            LinkedApplications = linkedRows,
            LinkedApplicationCount = linkedCount,
            IsPrototypeMock = false,
        };
    }

    internal static ApplicationProfileOverviewLinkedAppRow MapLinkedRow(ApplicationProfileInstance instance)
    {
        var caption = ApplicationProcessNumberHelper.FormatDisplayCaption(instance);
        var status = instance.LatestProgress?.State?.LocalizedDisplayName
            ?? instance.LatestProgress?.State?.NameTm
            ?? instance.LatestProgressDisplay;

        return new ApplicationProfileOverviewLinkedAppRow
        {
            ApplicationProfileInstanceId = instance.ID,
            FullNumber = string.IsNullOrWhiteSpace(caption) ? "—" : caption,
            ApplicationDate = instance.ApplicationDateText,
            Status = string.IsNullOrWhiteSpace(status) ? "—" : status,
        };
    }

    internal static (IReadOnlyList<ApplicationProfileOverviewLinkedAppRow> Rows, int Total) LoadLinkedApplications(
        ApplicationProfile profile,
        IObjectSpace? objectSpace)
    {
        try
        {
            if (objectSpace != null && profile.ID != Guid.Empty)
            {
                var query = objectSpace.GetObjectsQuery<ApplicationProfileInstance>()
                    .Where(a => a.ApplicationProfile != null && a.ApplicationProfile.ID == profile.ID);
                var total = query.Count();
                var page = query
                    .OrderByDescending(a => a.ApplicationDate)
                    .Take(LinkedApplicationsDisplayCap)
                    .ToList();
                return (page.Select(MapLinkedRow).ToList(), total);
            }
        }
        catch (Exception)
        {
            // Designer / non-queryable object space — fall through to in-memory collection.
        }

        var instances = profile.Instances?.ToList() ?? [];
        var rows = instances
            .OrderByDescending(a => a.ApplicationDate)
            .Take(LinkedApplicationsDisplayCap)
            .Select(MapLinkedRow)
            .ToList();
        return (rows, instances.Count);
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
        if (profile.ProduceRejection)
            produce.Add("rejection");
        if (profile.ProduceWorkLocation)
            produce.Add("work location");

        var cancel = new List<string>();
        if (profile.CancelInvitations)
            cancel.Add("invitations");
        if (profile.CancelWorkPermits)
            cancel.Add("work permits");
        if (profile.CancelVisas)
            cancel.Add("visas");
        if (profile.CancelBorderZonePermits)
            cancel.Add("border zone permits");
        if (profile.CancelApplicationProfileInstances)
            cancel.Add("applications");

        var lines = new List<string>();

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

        Add("Visa Type", LookupLabel(profile.DefaultVisaType), profile.RequireVisaType);
        Add("Visa Category", LookupLabel(profile.DefaultVisaCategory), profile.RequireVisaCategory);
        Add("Visa Period", LookupLabel(profile.DefaultVisaPeriod), profile.RequireVisaPeriod);
        Add("Urgency", LookupLabel(profile.DefaultUrgency), profile.RequireUrgency);
        Add("Project Contract", LookupLabel(profile.DefaultProjectContract), profile.RequireProject);
        Add("Migration Service", LookupLabel(profile.DefaultMigrationService), profile.RequireMigrationService);
        Add("Border Zone", profile.DefaultBorderZoneLocation, profile.RequireBorderZone);
        Add("Entry Check Point", LookupLabel(profile.DefaultEntryCheckPoint), profile.RequireEntryCheckPoint);
        Add("Start date", null, profile.RequireStartDate);
        Add("End date", null, profile.RequireEndDate);
        Add("Region (city)", null, profile.RequireRegionCity);
        Add("Business trip address", null, profile.RequireBusinessTripAddress);
        Add("Work permit location", null, profile.RequireWorkPermitLocation);
        Add("Entry date", null, profile.RequireEntryDate);

        return rows;
    }

    private static string? LookupLabel(LookupBase? lookup)
    {
        if (lookup == null)
            return null;

        if (!string.IsNullOrWhiteSpace(lookup.LocalizedDisplayName))
            return lookup.LocalizedDisplayName;
        return string.IsNullOrWhiteSpace(lookup.NameTm) ? null : lookup.NameTm;
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
            toggles.Add("Address of residence");
        if (profile.RequirePersonVisa)
            toggles.Add("Visa");
        if (profile.RequirePersonInvitationItem)
            toggles.Add("Invitation item");
        if (profile.RequirePersonWorkPermitItem)
            toggles.Add("Work permit item");
        if (profile.RequirePersonBorderZoneItem)
            toggles.Add("Border zone item");
        if (profile.RequirePersonSalary)
            toggles.Add("Salary");
        if (profile.RequirePersonMedical)
            toggles.Add("Medical");
        if (profile.RequirePersonRejectionItem)
            toggles.Add("Rejection item");
        if (profile.RequirePersonTravelHistory)
            toggles.Add("Travel history");
        return toggles;
    }

    private static string FormatTemplateKind(ApplicationProfileTemplateKind kind) => kind switch
    {
        ApplicationProfileTemplateKind.Excel => "Excel",
        ApplicationProfileTemplateKind.PdfForm => "PDF form",
        _ => "Word",
    };

    private static string FormatCatalogScope(ApplicationProfileTemplateCatalogScope scope) => scope switch
    {
        ApplicationProfileTemplateCatalogScope.Category => "Shared",
        ApplicationProfileTemplateCatalogScope.Global => "Shared",
        _ => "Profile-specific",
    };

    private static string FormatDataScope(ApplicationProfileTemplateDataScope scope) => scope switch
    {
        ApplicationProfileTemplateDataScope.ApplicationHeader => "ApplicationProfileInstance header",
        ApplicationProfileTemplateDataScope.Both => "Header + M2M",
        _ => "People (M2M)",
    };

    private static string? FormatCategory(string? key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return null;

        var chip = ApplicationProfileWizardTemplateCatalog.CategoryChips
            .FirstOrDefault(c => c.Key.Equals(key, StringComparison.OrdinalIgnoreCase));
        return string.IsNullOrWhiteSpace(chip.Label) ? key : chip.Label;
    }

    private static ApplicationProfileOverviewSnapshot BuildFallbackMock(Guid applicationProfileId) =>
        new()
        {
            ApplicationProfileId = applicationProfileId == Guid.Empty
                ? Guid.Parse("22222222-2222-2222-2222-222222222222")
                : applicationProfileId,
            Name = "Invitation + work permit (employee)",
            Code = "INV_WP_EMP",
            SelectionCode = "101",
            Description = "Prototype profile — mock overview when profile id is not resolved.",
            ActionFamilyLabel = "Issuance",
            ProgressRouteLabel = "Via ministry",
            IsViaMinistry = true,
            IsAlwaysAvailable = true,
            AudienceLabels = ["Employee"],
            IsConfigLocked = false,
            LiveConfigurationLines =
            [
                "May produce: invitation, work permit, visa",
            ],
            MinistrySlaDays = 14,
            MigrationSlaDays = 14,
            ProgressStates = [],
            PerApplicationDefaults =
            [
                new() { FieldLabel = "Visa Type", DefaultValue = "WP", Required = true },
                new() { FieldLabel = "Visa Period", DefaultValue = "6 months", Required = true },
            ],
            ApprovalLegs =
            [
                new() { Sequence = 1, MinistryName = "Türkmenenergo" },
                new() { Sequence = 2, MinistryName = "Migration service" },
            ],
            PersonDataToggles = ["Passport", "Position", "Education"],
            NestedTemplates =
            [
                new() { Name = "Invitation package", Kind = "Word", Scope = "Profile-specific", DataScope = "People (M2M)" },
                new() { Name = "Work permit forms", Kind = "Excel", Scope = "Shared", DataScope = "Header + M2M", Category = "Work permit" },
            ],
            LinkedApplications = [],
            LinkedApplicationCount = 0,
            IsPrototypeMock = true,
        };
}
