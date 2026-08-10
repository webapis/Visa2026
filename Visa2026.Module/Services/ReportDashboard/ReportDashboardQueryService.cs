using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.EFCore;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Localization;
using Visa2026.Module.Services.ApplicationPersonRoster;

namespace Visa2026.Module.Services.ReportDashboard;

public sealed class ReportDashboardQueryService : IReportDashboardQueryService
{
    /// <summary>Max preview rows returned per panel (UI paginates client-side).</summary>
    private const int PreviewLimit = 10_000;

    public ReportDashboardSnapshot LoadSnapshot(
        IObjectSpace objectSpace,
        int dateRangeMonths = 6,
        ReportDashboardPersonType personType = ReportDashboardPersonType.All)
    {
        _ = dateRangeMonths; // project chips ignore date range for now
        var projects = LoadProjectChips(objectSpace, personType);
        var personRoleCounts = LoadPersonRoleCounts(objectSpace);
        // Category sidebar counts remain mock via Hybrid until vw_rd_snapshot_counts.
        return new ReportDashboardSnapshot
        {
            Projects = projects,
            CategoryCounts = new Dictionary<(ReportDashboardPersonType, ReportDashboardCategory), int>(),
            PersonRoleCounts = personRoleCounts
        };
    }

    public ReportDashboardSubReportListing ListSubReports(
        IObjectSpace objectSpace,
        ReportDashboardPersonType personType,
        ReportDashboardCategory category,
        string projectKey,
        int dateRangeMonths = 6,
        bool includeCompletedApplicationProcesses = false,
        bool includeCancelledApplicationProcesses = false)
    {
        _ = objectSpace;
        _ = personType;
        _ = projectKey;
        _ = dateRangeMonths;
        _ = includeCompletedApplicationProcesses;
        _ = includeCancelledApplicationProcesses;
        return new ReportDashboardSubReportListing
        {
            SubReports = ReportDashboardCatalog.SubReports(category),
            Counts = new Dictionary<string, int>(StringComparer.Ordinal)
        };
    }

    private static IReadOnlyDictionary<ReportDashboardPersonType, int> LoadPersonRoleCounts(IObjectSpace objectSpace)
    {
        if (objectSpace is EFCoreObjectSpace efOs
            && efOs.DbContext is Visa2026EFCoreDbContext db)
        {
            try
            {
                var rows = db.VwRdPersonRole.AsNoTracking().ToList();
                var dict = new Dictionary<ReportDashboardPersonType, int>();
                foreach (ReportDashboardPersonType pt in new[]
                {
                    ReportDashboardPersonType.Employees,
                    ReportDashboardPersonType.FamilyMembers,
                    ReportDashboardPersonType.TemporaryVisitors
                })
                {
                    var roleCode = (int)ReportDashboardCatalog.ToPersonRole(pt);
                    var row = rows.FirstOrDefault(r => r.PersonRoleCode == roleCode);
                    dict[pt] = row == null ? 0 : (int)Math.Min(row.PersonCount, int.MaxValue);
                }
                dict[ReportDashboardPersonType.All] =
                    dict[ReportDashboardPersonType.Employees]
                    + dict[ReportDashboardPersonType.FamilyMembers]
                    + dict[ReportDashboardPersonType.TemporaryVisitors];
                return dict;
            }
            catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UndefinedTable)
            {
                // Fall through.
            }
        }

        return LoadPersonRoleCountsLegacy(objectSpace);
    }

    private static IReadOnlyDictionary<ReportDashboardPersonType, int> LoadPersonRoleCountsLegacy(IObjectSpace objectSpace)
    {
        var dict = new Dictionary<ReportDashboardPersonType, int>();
        foreach (ReportDashboardPersonType pt in new[]
        {
            ReportDashboardPersonType.Employees,
            ReportDashboardPersonType.FamilyMembers,
            ReportDashboardPersonType.TemporaryVisitors
        })
        {
            var role = ReportDashboardCatalog.ToPersonRole(pt);
            dict[pt] = objectSpace.GetObjectsQuery<Person>()
                .Count(p => p.PersonRole == role && !p.IsArchived);
        }
        dict[ReportDashboardPersonType.All] =
            dict[ReportDashboardPersonType.Employees]
            + dict[ReportDashboardPersonType.FamilyMembers]
            + dict[ReportDashboardPersonType.TemporaryVisitors];
        return dict;
    }

    private static int CountCategory(
        IObjectSpace objectSpace,
        ReportDashboardPersonType personType,
        ReportDashboardCategory category,
        DateTime cutoff)
    {
        var role = ReportDashboardCatalog.TryGetPersonRole(personType);
        return category switch
        {
            ReportDashboardCategory.ApplicationViaMinistry =>
                objectSpace.GetObjectsQuery<Application>()
                    .Count(a => a.ApplicationDate >= cutoff
                        && a.ApplicationType != null
                        && a.ApplicationType.ApplicationProgressRoute
                            == ApplicationProgressRouteKind.ViaMinistries),
            ReportDashboardCategory.ApplicationDirectMigration =>
                objectSpace.GetObjectsQuery<Application>()
                    .Count(a => a.ApplicationDate >= cutoff
                        && a.ApplicationType != null
                        && a.ApplicationType.ApplicationProgressRoute
                            == ApplicationProgressRouteKind.DirectToMigrationService),
            ReportDashboardCategory.VisaExtension =>
                objectSpace.GetObjectsQuery<VisaExtensionStatus>()
                    .Count(v => v.Person != null && (role == null || v.Person.PersonRole == role)
                                && (v.ApplicationDate == null || v.ApplicationDate >= cutoff)),
            ReportDashboardCategory.Invitation =>
                objectSpace.GetObjectsQuery<InvitationItem>()
                    .Count(i => i.Person != null && (role == null || i.Person.PersonRole == role)
                                && (i.Invitation == null || i.Invitation.IssuedDate == default || i.Invitation.IssuedDate >= cutoff)),
            ReportDashboardCategory.Registration =>
                objectSpace.GetObjectsQuery<AddressOfResidence>()
                    .Count(a => a.Person != null && (role == null || a.Person.PersonRole == role)
                                && (a.ExpirationDate == null || a.ExpirationDate >= cutoff)),
            ReportDashboardCategory.WorkPermit =>
                objectSpace.GetObjectsQuery<WorkPermitItem>()
                    .Count(w => w.Person != null && (role == null || w.Person.PersonRole == role)
                                && (w.ExpirationDate == null || w.ExpirationDate >= cutoff)),
            ReportDashboardCategory.Travel =>
                TryGetDbContext(objectSpace, out var dbTravel)
                    ? ReportDashboardRosterQueryHelper.CountTravelLines(dbTravel, role, cutoff)
                    : objectSpace.GetObjectsQuery<ApplicationItem>()
                        .Count(a => a.Person != null && (role == null || a.Person.PersonRole == role)
                                    && a.TravelDate != null && a.TravelDate >= cutoff),
            ReportDashboardCategory.AddressOfResidence =>
                TryGetDbContext(objectSpace, out var dbAddr)
                    ? ReportDashboardRosterQueryHelper.CountAddressApplicationLines(dbAddr, role, cutoff)
                    : objectSpace.GetObjectsQuery<ApplicationItem>()
                        .Count(ai => ai.CurrentAddressOfResidence != null
                                    && ai.Person != null && (role == null || ai.Person.PersonRole == role)
                                    && !ai.Person.IsArchived
                                    && ai.Application != null
                                    && ai.Application.ApplicationDate >= cutoff),
            ReportDashboardCategory.BorderZone =>
                objectSpace.GetObjectsQuery<BorderZoneItem>()
                    .Count(b => b.Person != null && (role == null || b.Person.PersonRole == role)
                                && (b.BorderZone == null || b.BorderZone.ExpirationDate == null || b.BorderZone.ExpirationDate >= cutoff)),
            ReportDashboardCategory.Passport =>
                TryGetDbContext(objectSpace, out var dbPassport)
                    ? ReportDashboardRosterQueryHelper.CountPassportApplicationLines(dbPassport, role, cutoff)
                    : objectSpace.GetObjectsQuery<ApplicationItem>()
                        .Count(ai => ai.CurrentPassport != null
                                    && ai.Person != null && (role == null || ai.Person.PersonRole == role)
                                    && !ai.Person.IsArchived
                                    && ai.Application != null
                                    && ai.Application.ApplicationDate != null
                                    && ai.Application.ApplicationDate >= cutoff),
            ReportDashboardCategory.Education =>
                objectSpace.GetObjectsQuery<Education>()
                    .Count(e => e.Person != null && (role == null || e.Person.PersonRole == role)),
            ReportDashboardCategory.PositionHistory =>
                objectSpace.GetObjectsQuery<EmployeePositionHistory>()
                    .Count(h => h.Person != null && (role == null || h.Person.PersonRole == role)),
            ReportDashboardCategory.Subcontractor =>
                objectSpace.GetObjectsQuery<Person>()
                    .Count(p => (role == null || p.PersonRole == role) && !p.IsArchived),
            ReportDashboardCategory.MedicalRecord =>
                objectSpace.GetObjectsQuery<MedicalRecord>()
                    .Count(m => m.Person != null && (role == null || m.Person.PersonRole == role)),
            ReportDashboardCategory.IncompletePersons =>
                objectSpace.GetObjectsQuery<Person>()
                    .Count(p => p.IsDataIncomplete && (role == null || p.PersonRole == role) && !p.IsArchived),
            // Person search with no term lists everyone, so the Overview count is the person count.
            ReportDashboardCategory.PersonSearch =>
                objectSpace.GetObjectsQuery<Person>()
                    .Count(p => role == null || p.PersonRole == role),
            _ => 0
        };
    }

    public ReportDashboardPanelData LoadPanel(
        IObjectSpace objectSpace,
        ReportDashboardPersonType personType,
        ReportDashboardCategory category,
        string projectKey,
        int dateRangeMonths = 6,
        string subReport = "default",
        bool includeArchivedPersons = false,
        bool oneLastValidVisaPerPerson = false,
        bool oneLastValidWorkPermitPerPerson = false,
        bool includeCompletedApplicationProcesses = false,
        bool includeCancelledApplicationProcesses = false,
        bool validVisaPersonsOnly = true,
        string? searchTerm = null)
    {
        // Categories without a Last-N UI must not silently filter Preview by ApplicationDate —
        // Open ListView BuildListCriteria has no date clause (Preview ↔ ListView Total parity).
        var cutoff = dateRangeMonths > 0
            ? DateTime.Today.AddMonths(-dateRangeMonths)
            : DateTime.MinValue;
        var role = ReportDashboardCatalog.TryGetPersonRole(personType);
        var validVisaPersonIds = ResolveValidVisaPersonIds(objectSpace, category, validVisaPersonsOnly);
        var excelHint = ReportDashboardCatalog.ExcelTemplateNameHint(category, subReport);
        var excelConfigured = !string.IsNullOrEmpty(excelHint)
            && objectSpace.GetObjectsQuery<UserReportTemplate>()
                .Any(t => t.TemplateName != null
                    && t.TemplateName.Contains(excelHint)
                    && t.TemplateOutputFormat == TemplateOutputFormat.Excel);

        return category switch
        {
            ReportDashboardCategory.ApplicationViaMinistry
                when ReportDashboardCatalog.UsesApplicationViaMinistryRdListView(subReport) =>
                LoadApplicationViaMinistryFromView(
                    objectSpace, role, projectKey, personType, subReport, excelHint, excelConfigured, cutoff),
            ReportDashboardCategory.ApplicationDirectMigration
                when ReportDashboardCatalog.UsesApplicationDirectMigrationRdListView(subReport) =>
                LoadApplicationDirectMigrationFromView(
                    objectSpace, role, projectKey, personType, subReport, excelHint, excelConfigured, cutoff),
            ReportDashboardCategory.ApplicationViaMinistry
                or ReportDashboardCategory.ApplicationDirectMigration => LoadApplication(
                objectSpace, category, role, projectKey, personType, subReport, excelHint, excelConfigured, cutoff,
                includeCompletedApplicationProcesses, includeCancelledApplicationProcesses),
            ReportDashboardCategory.VisaExtension => LoadVisaExtension(objectSpace, role, projectKey, personType, subReport, excelHint, excelConfigured, cutoff, oneLastValidVisaPerPerson),
            ReportDashboardCategory.Invitation    => LoadInvitation(objectSpace, role, projectKey, personType, subReport, excelHint, excelConfigured, cutoff),
            ReportDashboardCategory.Registration  => LoadRegistration(objectSpace, role, projectKey, personType, subReport, excelHint, excelConfigured, cutoff, validVisaPersonIds),
            ReportDashboardCategory.WorkPermit    => LoadWorkPermit(objectSpace, role, projectKey, personType, subReport, excelHint, excelConfigured, cutoff, includeArchivedPersons, oneLastValidWorkPermitPerPerson, validVisaPersonIds),
            ReportDashboardCategory.Travel        => LoadTravel(objectSpace, role, projectKey, personType, subReport, excelHint, excelConfigured, cutoff, validVisaPersonIds),
            ReportDashboardCategory.AddressOfResidence => LoadAddressOfResidence(objectSpace, role, projectKey, personType, subReport, excelHint, excelConfigured, cutoff, includeArchivedPersons, validVisaPersonIds),
            ReportDashboardCategory.BorderZone    => LoadBorderZone(objectSpace, role, projectKey, personType, subReport, excelHint, excelConfigured, cutoff, validVisaPersonIds),
            ReportDashboardCategory.Passport         => LoadPassport(objectSpace, role, projectKey, personType, subReport, excelHint, excelConfigured, cutoff, includeArchivedPersons, validVisaPersonIds),
            ReportDashboardCategory.Education        => LoadEducation(objectSpace, role, projectKey, personType, subReport, excelHint, excelConfigured, cutoff, includeArchivedPersons, validVisaPersonIds),
            ReportDashboardCategory.PositionHistory  => LoadPositionHistory(objectSpace, role, projectKey, personType, subReport, excelHint, excelConfigured, cutoff, validVisaPersonIds),
            ReportDashboardCategory.Subcontractor    => LoadSubcontractor(objectSpace, role, projectKey, personType, subReport, excelHint, excelConfigured, includeArchivedPersons, validVisaPersonIds),
            ReportDashboardCategory.MedicalRecord   => LoadMedicalRecord(objectSpace, role, projectKey, personType, subReport, excelHint, excelConfigured, cutoff, includeArchivedPersons, validVisaPersonIds),
            ReportDashboardCategory.IncompletePersons => LoadIncompletePersons(objectSpace, role, projectKey, personType, subReport, excelHint, excelConfigured),
            ReportDashboardCategory.PersonSearch     => LoadPersonSearch(objectSpace, role, projectKey, personType, subReport, excelHint, excelConfigured, searchTerm),
            _                                        => EmptyPanel(personType, category, subReport, excelHint, excelConfigured)
        };
    }

    // ---- Project chips ---------------------------------------------------

    private static List<ReportDashboardProjectChip> LoadProjectChips(
        IObjectSpace objectSpace,
        ReportDashboardPersonType personType)
    {
        if (objectSpace is EFCoreObjectSpace efOs
            && efOs.DbContext is Visa2026EFCoreDbContext db)
        {
            try
            {
                var role = ReportDashboardCatalog.TryGetPersonRole(personType);
                IQueryable<VwRdProject> projectQuery = db.VwRdProject
                    .AsNoTracking()
                    .Where(r => r.PersonCount > 0);
                if (role.HasValue)
                    projectQuery = projectQuery.Where(r => r.PersonRoleCode == (int)role.Value);

                var rows = projectQuery
                    .OrderByDescending(r => r.PersonCount)
                    .ThenBy(r => r.ProjectNameTm)
                    .ToList();

                // All tab: same project can appear once per role — sum counts by label.
                var chips = rows
                    .Select(r =>
                    {
                        var label = !string.IsNullOrWhiteSpace(r.ProjectNameTm)
                            ? r.ProjectNameTm!
                            : (r.ProjectNameRaw ?? r.ProjectOid.ToString());
                        return new { Label = label, Count = (int)Math.Min(r.PersonCount, int.MaxValue) };
                    })
                    .GroupBy(x => x.Label, StringComparer.Ordinal)
                    .Select(g => new ReportDashboardProjectChip
                    {
                        Key = g.Key,
                        Label = g.Key,
                        Count = g.Sum(x => x.Count)
                    })
                    .OrderByDescending(c => c.Count)
                    .ThenBy(c => c.Label)
                    .ToList();

                chips.Insert(0, new ReportDashboardProjectChip
                {
                    Key = "All",
                    Label = "All",
                    Count = chips.Sum(c => c.Count)
                });
                return chips;
            }
            catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UndefinedTable)
            {
                // Fall through to legacy EF count.
            }
        }

        return LoadProjectChipsLegacy(objectSpace, personType);
    }

    private static List<ReportDashboardProjectChip> LoadProjectChipsLegacy(
        IObjectSpace objectSpace,
        ReportDashboardPersonType personType)
    {
        var role = ReportDashboardCatalog.TryGetPersonRole(personType);
        var peopleQuery = objectSpace.GetObjectsQuery<Person>()
            .Where(p => !p.IsArchived);
        if (role.HasValue)
            peopleQuery = peopleQuery.Where(p => p.PersonRole == role.Value);
        var people = peopleQuery
            .AsEnumerable()
            .Select(p => p.ProjectContract ?? p.SponsoringEmployee?.ProjectContract)
            .Where(pc => pc != null)
            .GroupBy(pc => pc!.ID)
            .Select(g => new { Project = g.First()!, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ThenBy(x => x.Project.NameTm)
            .ToList();

        var chips = people
            .Select(x =>
            {
                var label = !string.IsNullOrWhiteSpace(x.Project.NameTm)
                    ? x.Project.NameTm!
                    : x.Project.ID.ToString();
                return new ReportDashboardProjectChip
                {
                    Key = label,
                    Label = label,
                    Count = x.Count
                };
            })
            .ToList();

        chips.Insert(0, new ReportDashboardProjectChip { Key = "All", Label = "All", Count = chips.Sum(c => c.Count) });
        return chips;
    }

    // ---- Category loaders -----------------------------------------------

    /// <summary>
    /// Application Status: chart Status = combined
    /// State · Ministry depth · Approval leg · Migration SLA.
    /// Population filtered by <see cref="ApplicationProgressRouteKind"/> (same as nav ListViews).
    /// </summary>
    private static ReportDashboardPanelData LoadApplication(
        IObjectSpace objectSpace,
        ReportDashboardCategory category,
        PersonRecordRole? role, string projectKey,
        ReportDashboardPersonType personType, string subReport,
        string? excelHint, bool excelConfigured, DateTime cutoff,
        bool includeCompletedApplicationProcesses,
        bool includeCancelledApplicationProcesses)
    {
        // Legacy keys (by-progress / by-type / all / type:*) → Application Status.
        if (subReport is "by-progress" or "by-type" or "all" or "default"
            || string.IsNullOrWhiteSpace(subReport)
            || subReport.StartsWith("type:", StringComparison.Ordinal))
            subReport = ReportDashboardCatalog.ApplicationStatusSubReportKey;

        var route = ReportDashboardCatalog.ApplicationProgressRouteFor(category)
            ?? ApplicationProgressRouteKind.ViaMinistries;

        var query = FilterApplications(
            objectSpace, route, role, projectKey, cutoff,
            includeCompletedApplicationProcesses, includeCancelledApplicationProcesses);

        var apps = query
            .OrderByDescending(a => a.ApplicationDate)
            .AsEnumerable()
            .ToList();

        var labeled = apps.Select(a =>
        {
            var status = FormatApplicationCombinedStateLabel(a);
            return (App: a, Status: status);
        }).ToList();

        var groups = labeled
            .GroupBy(x => x.Status, StringComparer.Ordinal)
            .Select(g => (Label: g.Key, Count: g.Count()))
            .OrderByDescending(g => g.Count)
            .ToList();
        var buckets = AssignCategoricalCss(groups);
        var cssByLabel = buckets.ToDictionary(b => b.Label, b => b.CssClass, StringComparer.Ordinal);
        var totalCount = labeled.Count;

        var rows = labeled
            .OrderByDescending(x => x.App.ApplicationDate)
            .Take(PreviewLimit)
            .Select(x =>
            {
                var a = x.App;
                return new ReportDashboardPreviewRow
                {
                    RecordId = a.ID,
                    Name = FirstApplicationPersonName(a),
                    Project = ProjectLabel(a.ProjectContract),
                    ColumnA = a.FullApplicationNumber ?? a.ApplicationNumber ?? string.Empty,
                    ColumnB = FormatDate(a.ApplicationDate),
                    Status = x.Status,
                    StatusCssClass = cssByLabel.TryGetValue(x.Status, out var c) ? c : "st-cat-1"
                };
            })
            .ToList();

        return BuildPanel(
            personType, category, subReport, rows,
            excelHint, excelConfigured, buckets, totalCount, "Application Status");
    }

    private static IQueryable<Application> FilterApplications(
        IObjectSpace objectSpace,
        ApplicationProgressRouteKind route,
        PersonRecordRole? role,
        string projectKey,
        DateTime cutoff,
        bool includeCompletedApplicationProcesses,
        bool includeCancelledApplicationProcesses)
    {
        var query = objectSpace.GetObjectsQuery<Application>()
            .Where(a => (a.ApplicationDate == null || a.ApplicationDate >= cutoff)
                && a.ApplicationType != null
                && a.ApplicationType.ApplicationProgressRoute == route);

        if (role.HasValue)
        {
            var roleValue = role.Value;
            query = query.Where(a =>
                a.People.Any(ap => ap.Person != null && ap.Person.PersonRole == roleValue)
                || a.ApplicationItems.Any(ai =>
                    ai.Person != null && ai.Person.PersonRole == roleValue));
        }

        if (!string.IsNullOrWhiteSpace(projectKey) && projectKey != "All")
        {
            query = query.Where(a => a.ProjectContract != null
                && (a.ProjectContract.Name == projectKey || a.ProjectContract.NameTm == projectKey));
        }

        if (!includeCompletedApplicationProcesses)
        {
            query = query.Where(a =>
                a.LatestProgress == null
                || a.LatestProgress.State == null
                || a.LatestProgress.State.Code != ApplicationProgressStateCodes.ProcessIssued);
        }

        if (!includeCancelledApplicationProcesses)
        {
            query = query.Where(a =>
                a.LatestProgress == null
                || a.LatestProgress.State == null
                || a.LatestProgress.State.Code != ApplicationProgressStateCodes.ProcessCancelled);
        }

        return query;
    }

    private static string FormatApplicationCombinedStateLabel(Application application)
    {
        var state = string.IsNullOrWhiteSpace(application.CurrentState)
            ? "Being Prepared"
            : application.CurrentState.Trim();
        var depth = ApplicationProgressProfileResolver.FormatMinistryReviewDepthLabel(
            ApplicationProgressProfileResolver.GetMinistryReviewDepth(application));
        var leg = application.ApprovalLegProfile == null
            ? "—"
            : (string.IsNullOrWhiteSpace(application.ApprovalLegProfile.NameTm)
                ? (string.IsNullOrWhiteSpace(application.ApprovalLegProfile.Name)
                    ? "—"
                    : application.ApprovalLegProfile.Name.Trim())
                : application.ApprovalLegProfile.NameTm.Trim());
        var migration = string.IsNullOrWhiteSpace(application.MigrationSlaStatement)
            ? "—"
            : application.MigrationSlaStatement.Trim();
        return $"{state} · {depth} · {leg} · {migration}";
    }

    private static string FirstApplicationPersonName(Application application)
    {
        var name = application.ApplicationItems?
            .OrderBy(ai => ai.ID)
            .Select(ai => ai.Person?.FullName)
            .FirstOrDefault(n => !string.IsNullOrWhiteSpace(n));
        if (!string.IsNullOrWhiteSpace(name))
            return name!;
        return application.FullApplicationNumber
            ?? application.ApplicationNumber
            ?? string.Empty;
    }
    private static ReportDashboardPanelData LoadVisaExtension(
        IObjectSpace objectSpace, PersonRecordRole? role, string projectKey,
        ReportDashboardPersonType personType, string subReport,
        string? excelHint, bool excelConfigured, DateTime cutoff,
        bool oneLastValidVisaPerPerson = false)
    {
        if (subReport is "active-by-project" or "visa-state"
            || string.IsNullOrWhiteSpace(subReport) || subReport is "default")
        {
            return LoadVisaActiveByProjectFromView(
                objectSpace, role, projectKey, personType, "active-by-project",
                excelHint, excelConfigured, cutoff, oneLastValidVisaPerPerson);
        }

        if (subReport is "on-extension" or "app-progress")
        {
            return LoadVisaAppProgressFromView(
                objectSpace, role, projectKey, personType, "on-extension",
                excelHint, excelConfigured, cutoff,
                VisaAppProgressPanelMode.OnExtensionByProject);
        }

        if (subReport is "on-extension-by-period-category-type")
        {
            return LoadVisaAppProgressFromView(
                objectSpace, role, projectKey, personType, "on-extension-by-period-category-type",
                excelHint, excelConfigured, cutoff,
                VisaAppProgressPanelMode.OnExtensionByPeriodCategoryType);
        }

        if (subReport is "extension-result")
        {
            return LoadVisaAppProgressFromView(
                objectSpace, role, projectKey, personType, "extension-result",
                excelHint, excelConfigured, cutoff,
                VisaAppProgressPanelMode.ExtensionResultByProject);
        }

        if (subReport is "extension-result-by-period-category-type")
        {
            return LoadVisaAppProgressFromView(
                objectSpace, role, projectKey, personType, "extension-result-by-period-category-type",
                excelHint, excelConfigured, cutoff,
                VisaAppProgressPanelMode.ExtensionResultByPeriodCategoryType);
        }

        if (subReport is "by-period-category-type" or "by-category" or "by-type" or "by-period")
        {
            return LoadVisaByPeriodCategoryTypeFromView(
                objectSpace, role, projectKey, personType, "by-period-category-type",
                excelHint, excelConfigured, cutoff, oneLastValidVisaPerPerson);
        }

        if (subReport is "by-days-remaining")
        {
            return LoadVisaByDaysRemainingFromView(
                objectSpace, role, projectKey, personType, subReport, excelHint, excelConfigured, cutoff,
                oneLastValidVisaPerPerson);
        }

        if (subReport is "extension-required" or "extension-required-by-period-category-type")
        {
            return LoadVisaExtensionRequiredFromView(
                objectSpace, role, projectKey, personType, subReport, excelHint, excelConfigured, cutoff);
        }

        return LoadVisaExtensionLegacy(
            objectSpace, role, projectKey, personType, subReport, excelHint, excelConfigured, cutoff);
    }

    private enum VisaAppProgressPanelMode
    {
        /// <summary>In-flight extension apps; excludes terminal outcomes (Issued/Cancelled/Rejected/*_REVIEW_REJECTED); Status = Project · ProcessState.</summary>
        OnExtensionByProject,
        /// <summary>In-flight extension apps; excludes terminal outcomes; Status = Period · Category · Type · ProcessState.</summary>
        OnExtensionByPeriodCategoryType,
        /// <summary>Terminal outcomes only (Issued/Cancelled/Rejected/*_REVIEW_REJECTED); Status = Project · ProcessState.</summary>
        ExtensionResultByProject,
        /// <summary>Terminal outcomes only; Status = Period · Category · Type · ProcessState.</summary>
        ExtensionResultByPeriodCategoryType
    }

    /// <summary>
    /// Extension Required: last valid visa per person from <c>vw_rd_visa_extension_required</c>,
    /// excluding people on unfinished Visa Extension apps.
    /// Status = nearest days-remaining milestone (0 · 7 · 14 · 30 · 60 · 90 · 180 · 365); urgent first.
    /// </summary>
    private static ReportDashboardPanelData LoadVisaExtensionRequiredFromView(
        IObjectSpace objectSpace, PersonRecordRole? role, string projectKey,
        ReportDashboardPersonType personType, string subReport,
        string? excelHint, bool excelConfigured, DateTime cutoff)
    {
        if (!string.Equals(
                subReport, "extension-required-by-period-category-type", StringComparison.OrdinalIgnoreCase))
            subReport = "extension-required";

        if (objectSpace is not EFCoreObjectSpace efOs
            || efOs.DbContext is not Visa2026EFCoreDbContext db)
        {
            return EmptyPanel(personType, ReportDashboardCategory.VisaExtension, subReport, excelHint, excelConfigured);
        }

        _ = cutoff;
        IQueryable<VwRdVisaExtensionRequired> query = db.VwRdVisaExtensionRequired
            .AsNoTracking()
            .Where(r => !r.IsArchived);
        if (role.HasValue)
            query = query.Where(r => r.PersonRoleCode == (int)role.Value);

        if (!string.IsNullOrWhiteSpace(projectKey) && projectKey != "All")
        {
            query = query.Where(r =>
                r.ProjectNameTm == projectKey
                || r.ProjectName == projectKey
                || r.ProjectNameRaw == projectKey
                || r.StatusLabel == projectKey);
        }

        try
        {
            var today = DateTime.Today;
            var labeled = query.ToList()
                .Select(r =>
                {
                    var exactDays = r.ExpirationDate is DateTime exp
                        ? Math.Max(0, (exp.Date - today).Days)
                        : 0;
                    var milestone = SnapDaysRemainingToMilestone(exactDays);
                    return (
                        Row: r,
                        ExactDays: exactDays,
                        Days: milestone,
                        Status: FormatMilestoneDaysRemainingLabel(milestone));
                })
                .ToList();

            var groups = labeled
                .GroupBy(x => x.Days)
                .Select(g => (Label: FormatMilestoneDaysRemainingLabel(g.Key), Count: g.Count(), Days: g.Key))
                .OrderBy(g => g.Days)
                .Select(g => (g.Label, g.Count))
                .ToList();
            var buckets = AssignCategoricalCss(groups);
            var cssByLabel = buckets.ToDictionary(b => b.Label, b => b.CssClass, StringComparer.Ordinal);
            var totalCount = buckets.Sum(b => b.Count);

            var rows = labeled
                .OrderBy(x => x.Days)
                .ThenBy(x => x.ExactDays)
                .ThenBy(x => x.Row.ExpirationDate)
                .ThenBy(x => x.Row.PersonName)
                .Take(PreviewLimit)
                .Select(x => new ReportDashboardPreviewRow
                {
                    RecordId = x.Row.ID,
                    Name = x.Row.PersonName ?? string.Empty,
                    Project = x.Row.ProjectName ?? string.Empty,
                    ColumnA = x.Row.PassportNumber ?? string.Empty,
                    ColumnB = x.Row.VisaNumber ?? string.Empty,
                    ColumnC = FormatDate(x.Row.ExpirationDate),
                    ColumnD = x.ExactDays.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    Status = x.Status,
                    StatusCssClass = cssByLabel.TryGetValue(x.Status, out var c) ? c : "st-cat-1"
                })
                .ToList();

            return BuildPanel(
                personType, ReportDashboardCategory.VisaExtension, subReport, rows,
                excelHint, excelConfigured, buckets, totalCount);
        }
        catch (Exception ex) when (IsMissingReportDashboardView(ex))
        {
            // View not created yet (Postgres CurrentVisaId naming / ModuleInfo skip) —
            // derive from Active Visa + unfinished Visa Extension apps.
            return LoadVisaExtensionRequiredFromActiveFallback(
                objectSpace, role, projectKey, personType, subReport, excelHint, excelConfigured, cutoff);
        }
    }

    /// <summary>Milestones for Extension Required chart Status (nearest match).</summary>
    private static readonly int[] ExtensionRequiredDayMilestones = [0, 7, 14, 30, 60, 90, 180, 365];

    /// <summary>
    /// Snap exact days remaining to nearest milestone.
    /// Remaining 0 stays <c>0</c>; otherwise prefer lower milestone on a tie (more urgent).
    /// </summary>
    private static int SnapDaysRemainingToMilestone(int days)
    {
        days = Math.Max(0, days);
        if (days == 0)
            return 0;

        // Non-zero: snap among 7+ so 1–3 days become "7 days" (not "0 days").
        var best = ExtensionRequiredDayMilestones[1];
        var bestDist = Math.Abs(days - best);
        for (var i = 2; i < ExtensionRequiredDayMilestones.Length; i++)
        {
            var m = ExtensionRequiredDayMilestones[i];
            var dist = Math.Abs(days - m);
            if (dist < bestDist || (dist == bestDist && m < best))
            {
                best = m;
                bestDist = dist;
            }
        }

        return best;
    }

    private static string FormatMilestoneDaysRemainingLabel(int milestoneDays) =>
        milestoneDays == 1 ? "1 day" : $"{milestoneDays} days";

    private static string FormatExactDaysRemainingColumn(DateTime? expirationDate)
    {
        if (expirationDate is not DateTime exp)
            return string.Empty;
        var days = Math.Max(0, (exp.Date - DateTime.Today).Days);
        return days.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    private static string FormatExactDaysRemainingColumn(int days) =>
        Math.Max(0, days).ToString(System.Globalization.CultureInfo.InvariantCulture);

    /// <summary>
    /// Fallback when <c>vw_rd_visa_extension_required</c> is missing:
    /// last valid visa from <c>vw_rd_visa_by_period</c>, minus people on unfinished extension apps
    /// (<c>vw_rd_visa_app_progress</c> where latest state ≠ PROCESS_ISSUED).
    /// </summary>
    private static ReportDashboardPanelData LoadVisaExtensionRequiredFromActiveFallback(
        IObjectSpace objectSpace, PersonRecordRole? role, string projectKey,
        ReportDashboardPersonType personType, string subReport,
        string? excelHint, bool excelConfigured, DateTime cutoff)
    {
        if (objectSpace is not EFCoreObjectSpace efOs
            || efOs.DbContext is not Visa2026EFCoreDbContext db)
        {
            return EmptyPanel(personType, ReportDashboardCategory.VisaExtension, subReport, excelHint, excelConfigured);
        }

        _ = cutoff;
        try
        {
            IQueryable<VwRdVisaByPeriod> periodQuery = db.VwRdVisaByPeriod
                .AsNoTracking()
                .Where(r => !r.IsArchived);
            if (role.HasValue)
                periodQuery = periodQuery.Where(r => r.PersonRoleCode == (int)role.Value);

            if (!string.IsNullOrWhiteSpace(projectKey) && projectKey != "All")
            {
                periodQuery = periodQuery.Where(r =>
                    r.ProjectNameTm == projectKey
                    || r.ProjectName == projectKey
                    || r.ProjectNameRaw == projectKey);
            }

            var list = TakeOneLastValidVisaPerPerson(
                periodQuery.ToList(), r => r.PersonOid, r => r.ExpirationDate, r => r.ID);

            var unfinishedPeople = db.VwRdVisaAppProgress
                .AsNoTracking()
                .Where(r => !r.IsArchived && r.PersonOid != null)
                .AsEnumerable()
                .Where(r => !ApplicationProgressStateCodes.IsTerminalOutcome(r.ProgressStateCode))
                .Select(r => r.PersonOid!.Value)
                .Distinct()
                .ToHashSet();

            list = list
                .Where(r => r.PersonOid == null || !unfinishedPeople.Contains(r.PersonOid.Value))
                .ToList();

            var today = DateTime.Today;
            var labeled = list.Select(r =>
            {
                var exactDays = r.ExpirationDate is DateTime exp
                    ? Math.Max(0, (exp.Date - today).Days)
                    : 0;
                var milestone = SnapDaysRemainingToMilestone(exactDays);
                return (
                    Row: r,
                    ExactDays: exactDays,
                    Days: milestone,
                    Status: FormatMilestoneDaysRemainingLabel(milestone));
            }).ToList();

            var groups = labeled
                .GroupBy(x => x.Days)
                .Select(g => (Label: FormatMilestoneDaysRemainingLabel(g.Key), Count: g.Count(), Days: g.Key))
                .OrderBy(g => g.Days)
                .Select(g => (g.Label, g.Count))
                .ToList();
            var buckets = AssignCategoricalCss(groups);
            var cssByLabel = buckets.ToDictionary(b => b.Label, b => b.CssClass, StringComparer.Ordinal);
            var totalCount = buckets.Sum(b => b.Count);

            var rows = labeled
                .OrderBy(x => x.Days)
                .ThenBy(x => x.ExactDays)
                .ThenBy(x => x.Row.ExpirationDate)
                .ThenBy(x => x.Row.PersonName)
                .Take(PreviewLimit)
                .Select(x => new ReportDashboardPreviewRow
                {
                    RecordId = x.Row.ID,
                    Name = x.Row.PersonName ?? string.Empty,
                    Project = x.Row.ProjectName ?? string.Empty,
                    ColumnA = x.Row.PassportNumber ?? string.Empty,
                    ColumnB = x.Row.VisaNumber ?? string.Empty,
                    ColumnC = FormatDate(x.Row.ExpirationDate),
                    ColumnD = x.ExactDays.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    Status = x.Status,
                    StatusCssClass = cssByLabel.TryGetValue(x.Status, out var c) ? c : "st-cat-1"
                })
                .ToList();

            return BuildPanel(
                personType, ReportDashboardCategory.VisaExtension, subReport, rows,
                excelHint, excelConfigured, buckets, totalCount);
        }
        catch (Exception ex) when (IsMissingReportDashboardView(ex))
        {
            return EmptyPanel(personType, ReportDashboardCategory.VisaExtension, subReport, excelHint, excelConfigured);
        }
    }

    /// <summary>
    /// Active (valid) visas: Status = Project from <c>vw_rd_visa_active_by_project</c>.
    /// </summary>
    private static ReportDashboardPanelData LoadVisaActiveByProjectFromView(
        IObjectSpace objectSpace, PersonRecordRole? role, string projectKey,
        ReportDashboardPersonType personType, string subReport,
        string? excelHint, bool excelConfigured, DateTime cutoff,
        bool oneLastValidVisaPerPerson = false)
    {
        if (objectSpace is not EFCoreObjectSpace efOs
            || efOs.DbContext is not Visa2026EFCoreDbContext db)
        {
            return LoadVisaExtensionLegacy(
                objectSpace, role, projectKey, personType, subReport, excelHint, excelConfigured, cutoff);
        }

        _ = cutoff;
        IQueryable<VwRdVisaActiveByProject> periodQuery = db.VwRdVisaActiveByProject
            .AsNoTracking()
            .Where(r => !r.IsArchived);
        if (role.HasValue)
            periodQuery = periodQuery.Where(r => r.PersonRoleCode == (int)role.Value);
        if (oneLastValidVisaPerPerson)
            periodQuery = periodQuery.Where(r => r.IsOneLastValidPerPerson);

        if (!string.IsNullOrWhiteSpace(projectKey) && projectKey != "All")
        {
            periodQuery = periodQuery.Where(r =>
                r.ProjectNameTm == projectKey
                || r.ProjectName == projectKey
                || r.ProjectNameRaw == projectKey);
        }

        try
        {
            var list = periodQuery.ToList();

            var labeled = list.Select(r =>
            {
                var status = string.IsNullOrWhiteSpace(r.StatusLabel)
                    ? (string.IsNullOrWhiteSpace(r.ProjectName) ? "(No project)" : r.ProjectName.Trim())
                    : r.StatusLabel.Trim();
                return (Row: r, Status: status);
            }).ToList();

            var groups = labeled
                .GroupBy(x => x.Status, StringComparer.Ordinal)
                .Select(g => (Label: g.Key, Count: g.Count()))
                .OrderByDescending(g => g.Count)
                .ToList();
            var buckets = AssignCategoricalCss(groups);
            var cssByLabel = buckets.ToDictionary(b => b.Label, b => b.CssClass, StringComparer.Ordinal);
            var totalCount = buckets.Sum(b => b.Count);

            var rows = labeled
                .OrderBy(x => x.Status)
                .ThenBy(x => x.Row.ExpirationDate)
                .ThenBy(x => x.Row.PersonName)
                .Take(PreviewLimit)
                .Select(x => new ReportDashboardPreviewRow
                {
                    RecordId = x.Row.ID,
                    Name = x.Row.PersonName ?? string.Empty,
                    Project = x.Row.ProjectName ?? string.Empty,
                    ColumnA = x.Row.PassportNumber ?? string.Empty,
                    ColumnB = x.Row.VisaNumber ?? string.Empty,
                    ColumnC = FormatDate(x.Row.ExpirationDate),
                    ColumnD = FormatExactDaysRemainingColumn(x.Row.DaysRemaining),
                    Status = x.Status,
                    StatusCssClass = cssByLabel.TryGetValue(x.Status, out var c) ? c : "st-cat-1"
                })
                .ToList();

            return BuildPanel(
                personType, ReportDashboardCategory.VisaExtension, subReport, rows,
                excelHint, excelConfigured, buckets, totalCount);
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UndefinedTable)
        {
            return LoadVisaExtensionLegacy(
                objectSpace, role, projectKey, personType, subReport, excelHint, excelConfigured, cutoff);
        }
    }

    /// <summary>
    /// Active (valid) visas: Status = Period · Category · Type from
    /// <c>vw_rd_visa_active_by_period_category_type</c>.
    /// </summary>
    private static ReportDashboardPanelData LoadVisaByPeriodCategoryTypeFromView(
        IObjectSpace objectSpace, PersonRecordRole? role, string projectKey,
        ReportDashboardPersonType personType, string subReport,
        string? excelHint, bool excelConfigured, DateTime cutoff,
        bool oneLastValidVisaPerPerson = false)
    {
        if (objectSpace is not EFCoreObjectSpace efOs
            || efOs.DbContext is not Visa2026EFCoreDbContext db)
        {
            return LoadVisaExtensionLegacy(
                objectSpace, role, projectKey, personType, subReport, excelHint, excelConfigured, cutoff);
        }

        _ = cutoff;
        IQueryable<VwRdVisaActiveByPeriodCategoryType> periodQuery = db.VwRdVisaActiveByPeriodCategoryType
            .AsNoTracking()
            .Where(r => !r.IsArchived);
        if (role.HasValue)
            periodQuery = periodQuery.Where(r => r.PersonRoleCode == (int)role.Value);
        if (oneLastValidVisaPerPerson)
            periodQuery = periodQuery.Where(r => r.IsOneLastValidPerPerson);

        if (!string.IsNullOrWhiteSpace(projectKey) && projectKey != "All")
        {
            periodQuery = periodQuery.Where(r =>
                r.ProjectNameTm == projectKey
                || r.ProjectName == projectKey
                || r.ProjectNameRaw == projectKey);
        }

        try
        {
            var list = periodQuery.ToList();

            var labeled = list.Select(r =>
            {
                var status = string.IsNullOrWhiteSpace(r.StatusLabel)
                    ? "(No period) · (No category) · (No type)"
                    : r.StatusLabel.Trim();
                return (Row: r, Status: status);
            }).ToList();

            var groups = labeled
                .GroupBy(x => x.Status, StringComparer.Ordinal)
                .Select(g => (Label: g.Key, Count: g.Count()))
                .OrderByDescending(g => g.Count)
                .ToList();
            var buckets = AssignCategoricalCss(groups);
            var cssByLabel = buckets.ToDictionary(b => b.Label, b => b.CssClass, StringComparer.Ordinal);
            var totalCount = buckets.Sum(b => b.Count);

            var rows = labeled
                .OrderBy(x => x.Row.PeriodDays)
                .ThenBy(x => x.Status)
                .ThenBy(x => x.Row.PersonName)
                .Take(PreviewLimit)
                .Select(x => new ReportDashboardPreviewRow
                {
                    RecordId = x.Row.ID,
                    Name = x.Row.PersonName ?? string.Empty,
                    Project = x.Row.ProjectName ?? string.Empty,
                    ColumnA = x.Row.PassportNumber ?? string.Empty,
                    ColumnB = x.Row.VisaNumber ?? string.Empty,
                    ColumnC = FormatDate(x.Row.ExpirationDate),
                    ColumnD = FormatExactDaysRemainingColumn(x.Row.DaysRemaining),
                    Status = x.Status,
                    StatusCssClass = cssByLabel.TryGetValue(x.Status, out var c) ? c : "st-cat-1"
                })
                .ToList();

            return BuildPanel(
                personType, ReportDashboardCategory.VisaExtension, subReport, rows,
                excelHint, excelConfigured, buckets, totalCount);
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UndefinedTable)
        {
            return LoadVisaExtensionLegacy(
                objectSpace, role, projectKey, personType, subReport, excelHint, excelConfigured, cutoff);
        }
    }

    /// <summary>
    /// Valid visas from <c>vw_rd_visa_by_days_remaining</c>
    /// (Status = closed days-to-expiry bucket; multiple valid visas per person allowed).
    /// </summary>
    private static ReportDashboardPanelData LoadVisaByDaysRemainingFromView(
        IObjectSpace objectSpace, PersonRecordRole? role, string projectKey,
        ReportDashboardPersonType personType, string subReport,
        string? excelHint, bool excelConfigured, DateTime cutoff,
        bool oneLastValidVisaPerPerson = false)
    {
        if (objectSpace is not EFCoreObjectSpace efOs
            || efOs.DbContext is not Visa2026EFCoreDbContext db)
        {
            return LoadVisaExtensionLegacy(
                objectSpace, role, projectKey, personType, subReport, excelHint, excelConfigured, cutoff);
        }

        _ = cutoff;
        IQueryable<VwRdVisaByDaysRemaining> query = db.VwRdVisaByDaysRemaining
            .AsNoTracking()
            .Where(r => !r.IsArchived);
        if (role.HasValue)
            query = query.Where(r => r.PersonRoleCode == (int)role.Value);

        if (!string.IsNullOrWhiteSpace(projectKey) && projectKey != "All")
        {
            query = query.Where(r =>
                r.ProjectNameTm == projectKey
                || r.ProjectName == projectKey
                || r.ProjectNameRaw == projectKey);
        }

        if (oneLastValidVisaPerPerson)
            query = query.Where(r => r.IsOneLastValidPerPerson);

        try
        {
            var list = query.ToList();

            var statusRows = list
                .GroupBy(r => new { r.StatusLabel, r.StatusCssClass })
                .Select(g => new
                {
                    Label = g.Key.StatusLabel,
                    CssClass = g.Key.StatusCssClass,
                    Count = g.Count()
                })
                .ToList();

            static int RemainingSortKey(string? label) => label switch
            {
                "< 10 days" => 1,
                "< 1 month" => 2,
                "< 3 months" => 3,
                "< 4 months" => 4,
                "< 5 months" => 5,
                "< 6 months" => 6,
                "≥ 6 months" => 7,
                _ => 99
            };

            var buckets = statusRows
                .Select(g => new ReportDashboardStatusBucket
                {
                    Label = g.Label ?? string.Empty,
                    CssClass = g.CssClass ?? "st-pending",
                    Count = g.Count
                })
                .OrderBy(b => RemainingSortKey(b.Label))
                .ToList();

            var totalCount = buckets.Sum(b => b.Count);

            var rows = list
                .OrderBy(r => r.DaysRemaining)
                .ThenBy(r => r.PersonName)
                .Take(PreviewLimit)
                .Select(r => new ReportDashboardPreviewRow
                {
                    RecordId = r.ID,
                    Name = r.PersonName ?? string.Empty,
                    Project = r.ProjectName ?? string.Empty,
                    ColumnA = r.PassportNumber ?? string.Empty,
                    ColumnB = r.VisaNumber ?? string.Empty,
                    ColumnC = FormatDate(r.ExpirationDate),
                    ColumnD = FormatExactDaysRemainingColumn(r.DaysRemaining),
                    Status = r.StatusLabel ?? string.Empty,
                    StatusCssClass = r.StatusCssClass ?? "st-pending"
                })
                .ToList();

            return BuildPanel(
                personType, ReportDashboardCategory.VisaExtension, subReport, rows,
                excelHint, excelConfigured, buckets, totalCount);
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UndefinedTable)
        {
            return LoadVisaExtensionLegacy(
                objectSpace, role, projectKey, personType, subReport, excelHint, excelConfigured, cutoff);
        }
    }

    /// <summary>
    /// Loads Visa State from <c>vw_rd_visa_state</c>
    /// (Extension Started: valid last-visa on visa-extension ApplicationItem;
    /// application progress must not contain PROCESS_CANCELLED).
    /// </summary>
    private static ReportDashboardPanelData LoadVisaStateFromView(
        IObjectSpace objectSpace, PersonRecordRole? role, string projectKey,
        ReportDashboardPersonType personType, string subReport,
        string? excelHint, bool excelConfigured, DateTime cutoff)
    {
        if (objectSpace is not EFCoreObjectSpace efOs
            || efOs.DbContext is not Visa2026EFCoreDbContext db)
        {
            return LoadVisaExtensionLegacy(
                objectSpace, role, projectKey, personType, subReport, excelHint, excelConfigured, cutoff);
        }

        _ = cutoff;
        IQueryable<VwRdVisaState> query = db.VwRdVisaState
            .AsNoTracking()
            .Where(r => !r.IsArchived);
        if (role.HasValue)
            query = query.Where(r => r.PersonRoleCode == (int)role.Value);

        if (!string.IsNullOrWhiteSpace(projectKey) && projectKey != "All")
        {
            query = query.Where(r =>
                r.ProjectNameTm == projectKey
                || r.ProjectName == projectKey
                || r.ProjectNameRaw == projectKey);
        }

        try
        {
            var stateRows = query
                .GroupBy(r => new { r.StateLabel, r.StateCssClass })
                .Select(g => new
                {
                    Label = g.Key.StateLabel,
                    CssClass = g.Key.StateCssClass,
                    Count = g.Count()
                })
                .ToList();

            var buckets = stateRows
                .Select(g => new ReportDashboardStatusBucket
                {
                    Label = g.Label ?? string.Empty,
                    CssClass = g.CssClass ?? "st-pending",
                    Count = g.Count
                })
                .OrderByDescending(b => b.Count)
                .ToList();

            var totalCount = buckets.Sum(b => b.Count);

            var rows = query
                .OrderBy(r => r.ExpirationDate)
                .Take(PreviewLimit)
                .AsEnumerable()
                .Select(r => new ReportDashboardPreviewRow
                {
                    RecordId = r.ID,
                    Name = r.PersonName ?? string.Empty,
                    Project = r.ProjectName ?? string.Empty,
                    ColumnA = r.VisaNumber ?? string.Empty,
                    ColumnB = FormatDate(r.ExpirationDate),
                    Status = r.StateLabel ?? string.Empty,
                    StatusCssClass = r.StateCssClass ?? "st-pending"
                })
                .ToList();

            return BuildPanel(
                personType, ReportDashboardCategory.VisaExtension, subReport, rows,
                excelHint, excelConfigured, buckets, totalCount);
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UndefinedTable)
        {
            return LoadVisaExtensionLegacy(
                objectSpace, role, projectKey, personType, subReport, excelHint, excelConfigured, cutoff);
        }
    }

    /// <summary>
    /// Visa On Extension / Extension Result panels from dedicated wrapper views
    /// (<c>vw_rd_visa_on_extension*</c> / <c>vw_rd_visa_extension_result*</c>).
    /// Population is baked into each view; Status comes from <c>StatusLabel</c>.
    /// </summary>
    private static ReportDashboardPanelData LoadVisaAppProgressFromView(
        IObjectSpace objectSpace, PersonRecordRole? role, string projectKey,
        ReportDashboardPersonType personType, string subReport,
        string? excelHint, bool excelConfigured, DateTime cutoff,
        VisaAppProgressPanelMode mode)
    {
        if (objectSpace is not EFCoreObjectSpace efOs
            || efOs.DbContext is not Visa2026EFCoreDbContext db)
        {
            return LoadVisaExtensionLegacy(
                objectSpace, role, projectKey, personType, subReport, excelHint, excelConfigured, cutoff);
        }

        _ = cutoff;
        var excludeIssued = mode is VisaAppProgressPanelMode.OnExtensionByProject
            or VisaAppProgressPanelMode.OnExtensionByPeriodCategoryType;
        var isExtensionResult = mode is VisaAppProgressPanelMode.ExtensionResultByProject
            or VisaAppProgressPanelMode.ExtensionResultByPeriodCategoryType;

        try
        {
            var list = mode switch
            {
                VisaAppProgressPanelMode.OnExtensionByProject =>
                    QueryVisaAppProgressDedicated(
                        db.VwRdVisaOnExtension.AsNoTracking(), role, projectKey)
                        .Select(r => ToVisaAppProgressRow(r.ID, r.PersonName, r.ProjectName, r.PassportNumber,
                            r.ApplicationNumber, r.ApplicationDate, r.DaysRemainingOnVisa, r.StatusLabel))
                        .ToList(),
                VisaAppProgressPanelMode.OnExtensionByPeriodCategoryType =>
                    QueryVisaAppProgressDedicated(
                        db.VwRdVisaOnExtensionByPeriodCategoryType.AsNoTracking(), role, projectKey)
                        .Select(r => ToVisaAppProgressRow(r.ID, r.PersonName, r.ProjectName, r.PassportNumber,
                            r.ApplicationNumber, r.ApplicationDate, r.DaysRemainingOnVisa, r.StatusLabel))
                        .ToList(),
                VisaAppProgressPanelMode.ExtensionResultByProject =>
                    QueryVisaAppProgressDedicated(
                        db.VwRdVisaExtensionResult.AsNoTracking(), role, projectKey)
                        .Select(r => ToVisaAppProgressRow(r.ID, r.PersonName, r.ProjectName, r.PassportNumber,
                            r.ApplicationNumber, r.ApplicationDate, r.DaysRemainingOnVisa, r.StatusLabel))
                        .ToList(),
                VisaAppProgressPanelMode.ExtensionResultByPeriodCategoryType =>
                    QueryVisaAppProgressDedicated(
                        db.VwRdVisaExtensionResultByPeriodCategoryType.AsNoTracking(), role, projectKey)
                        .Select(r => ToVisaAppProgressRow(r.ID, r.PersonName, r.ProjectName, r.PassportNumber,
                            r.ApplicationNumber, r.ApplicationDate, r.DaysRemainingOnVisa, r.StatusLabel))
                        .ToList(),
                _ => []
            };

            var labeled = list.Select(r =>
            {
                var status = string.IsNullOrWhiteSpace(r.StatusLabel)
                    ? "(No status)"
                    : r.StatusLabel.Trim();
                return (Row: r, Status: status);
            }).ToList();

            var groups = labeled
                .GroupBy(x => x.Status, StringComparer.Ordinal)
                .Select(g => (Label: g.Key, Count: g.Count()))
                .OrderByDescending(g => g.Count)
                .ToList();
            var buckets = isExtensionResult
                ? AssignExtensionResultCss(groups)
                : AssignCategoricalCss(groups);
            var cssByLabel = buckets.ToDictionary(b => b.Label, b => b.CssClass, StringComparer.Ordinal);
            var totalCount = buckets.Sum(b => b.Count);

            var rows = labeled
                .OrderByDescending(x => x.Row.ApplicationDate)
                .Take(PreviewLimit)
                .Select(x => new ReportDashboardPreviewRow
                {
                    RecordId = x.Row.ID,
                    Name = x.Row.PersonName ?? string.Empty,
                    Project = x.Row.ProjectName ?? string.Empty,
                    ColumnA = x.Row.PassportNumber ?? string.Empty,
                    ColumnB = x.Row.ApplicationNumber ?? string.Empty,
                    ColumnC = FormatDate(x.Row.ApplicationDate),
                    ColumnD = excludeIssued && x.Row.DaysRemainingOnVisa is int d
                        ? FormatExactDaysRemainingColumn(d)
                        : string.Empty,
                    Status = x.Status,
                    StatusCssClass = cssByLabel.TryGetValue(x.Status, out var c) ? c : "st-cat-1"
                })
                .ToList();

            return BuildPanel(
                personType, ReportDashboardCategory.VisaExtension, subReport, rows,
                excelHint, excelConfigured, buckets, totalCount);
        }
        catch (PostgresException ex) when (
            ex.SqlState == PostgresErrorCodes.UndefinedTable
            || ex.SqlState == PostgresErrorCodes.UndefinedColumn)
        {
            return LoadVisaExtensionLegacy(
                objectSpace, role, projectKey, personType, subReport, excelHint, excelConfigured, cutoff);
        }
    }

    private sealed record VisaAppProgressPreviewSource(
        Guid ID,
        string? PersonName,
        string? ProjectName,
        string? PassportNumber,
        string? ApplicationNumber,
        DateTime? ApplicationDate,
        int? DaysRemainingOnVisa,
        string? StatusLabel);

    private static VisaAppProgressPreviewSource ToVisaAppProgressRow(
        Guid id, string? personName, string? projectName, string? passportNumber, string? applicationNumber,
        DateTime? applicationDate, int? daysRemainingOnVisa, string? statusLabel) =>
        new(id, personName, projectName, passportNumber, applicationNumber, applicationDate, daysRemainingOnVisa, statusLabel);

    private static IQueryable<T> QueryVisaAppProgressDedicated<T>(
        IQueryable<T> query, PersonRecordRole? role, string projectKey)
        where T : class
    {
        // Filter via EF expression trees on known property names through dynamic cast isn't needed —
        // each caller already materializes typed sets. Keep shared archive/role/project filters below.
        query = query.Where(r => !EF.Property<bool>(r, "IsArchived"));
        if (role.HasValue)
        {
            var roleCode = (int)role.Value;
            query = query.Where(r => EF.Property<int>(r, "PersonRoleCode") == roleCode);
        }

        if (!string.IsNullOrWhiteSpace(projectKey) && projectKey != "All")
        {
            query = query.Where(r =>
                EF.Property<string>(r, "ProjectNameTm") == projectKey
                || EF.Property<string>(r, "ProjectName") == projectKey
                || EF.Property<string>(r, "ProjectNameRaw") == projectKey);
        }

        return query;
    }

    /// <summary>
    /// Chart/table Status for legacy shared <see cref="VwRdVisaAppProgress"/> rows
    /// (ProcessState naming matches <see cref="ApplicationProgress.StatusListLabel"/>).
    /// Dedicated wrapper views expose <c>StatusLabel</c> directly — prefer that path.
    /// </summary>
    private static Dictionary<Guid, string> ResolveVisaAppProgressStatusLabels(
        Visa2026EFCoreDbContext db,
        List<VwRdVisaAppProgress> rows,
        VisaAppProgressPanelMode mode)
    {
        var result = new Dictionary<Guid, string>(rows.Count);

        var codes = rows
            .Select(r => r.ProgressStateCode?.Trim())
            .Where(c => !string.IsNullOrEmpty(c))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var stateByCode = db.ApplicationStates.AsNoTracking()
            .Where(s => s.Code != null && codes.Contains(s.Code))
            .AsEnumerable()
            .Where(s => !string.IsNullOrWhiteSpace(s.Code))
            .GroupBy(s => s.Code!.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        var appIds = rows
            .Where(r => r.ApplicationOid.HasValue)
            .Select(r => r.ApplicationOid!.Value)
            .Distinct()
            .ToList();

        var needVisaDims = mode is VisaAppProgressPanelMode.OnExtensionByPeriodCategoryType
            or VisaAppProgressPanelMode.ExtensionResultByPeriodCategoryType;
        Dictionary<Guid, Application> appsById = [];
        if (appIds.Count > 0)
        {
            IQueryable<Application> appQuery = db.Applications.AsNoTracking()
                .Where(a => appIds.Contains(a.ID))
                .Include(a => a.ApprovalLegSnapshots)
                .Include(a => a.ApprovalLegProfile!)
                    .ThenInclude(p => p.MinistryLegs!)
                    .ThenInclude(l => l.ApprovingMinistry);
            if (needVisaDims)
            {
                appQuery = appQuery
                    .Include(a => a.VisaPeriod)
                    .Include(a => a.VisaCategory)
                    .Include(a => a.VisaType);
            }

            appsById = appQuery.ToList().ToDictionary(a => a.ID);
        }

        foreach (var row in rows)
        {
            appsById.TryGetValue(row.ApplicationOid ?? Guid.Empty, out var app);
            var processState = ResolveOnExtensionProcessStateLabel(row, app, stateByCode);

            if (mode is VisaAppProgressPanelMode.OnExtensionByPeriodCategoryType
                or VisaAppProgressPanelMode.ExtensionResultByPeriodCategoryType)
            {
                var period = LookupLabelOrMissing(app?.VisaPeriod, "(No period)");
                var category = LookupLabelOrMissing(app?.VisaCategory, "(No category)");
                var type = LookupLabelOrMissing(app?.VisaType, "(No type)");
                result[row.ID] = $"{period} · {category} · {type} · {processState}";
            }
            else
            {
                // OnExtensionByProject / ExtensionResultByProject
                var project = string.IsNullOrWhiteSpace(row.ProjectName)
                    ? "(No project)"
                    : row.ProjectName.Trim();
                result[row.ID] = $"{project} · {processState}";
            }
        }

        return result;
    }

    private static string LookupLabelOrMissing(LookupBase? lookup, string missing)
    {
        if (lookup == null)
            return missing;
        var label = LookupLocalization.GetDisplayName(lookup)?.Trim();
        return string.IsNullOrEmpty(label) ? missing : label;
    }

    /// <summary>
    /// Terminal process codes for Invitation Process / Registration On process:
    /// Issued, Cancelled, Process Rejected, and 1st–5th Review Rejected.
    /// </summary>
    private static bool IsInvitationProcessCompleted(string? progressStateCode)
    {
        var code = progressStateCode?.Trim();
        if (string.IsNullOrEmpty(code))
            return false;

        if (string.Equals(code, ApplicationProgressStateCodes.ProcessIssued, StringComparison.OrdinalIgnoreCase)
            || string.Equals(code, ApplicationProgressStateCodes.ProcessRejected, StringComparison.OrdinalIgnoreCase)
            || string.Equals(code, ApplicationProgressStateCodes.ProcessCancelled, StringComparison.OrdinalIgnoreCase)
            || string.Equals(code, ApplicationProgressStateCodes.Review1Rejected, StringComparison.OrdinalIgnoreCase)
            || string.Equals(code, ApplicationProgressStateCodes.Review2Rejected, StringComparison.OrdinalIgnoreCase))
            return true;

        // Legacy multi-leg ministry rejects (3–5) — same “completed” treatment as 1st/2nd.
        return code.EndsWith("_REVIEW_REJECTED", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsInvitationProcessResultState(string? progressStateCode) =>
        IsInvitationProcessCompleted(progressStateCode);

    /// <summary>Fixed CSS for terminal extension outcomes (Issued / Cancelled / Rejected).</summary>
    private static List<ReportDashboardStatusBucket> AssignExtensionResultCss(
        List<(string Label, int Count)> groups)
    {
        static string CssFor(string label)
        {
            if (label.Contains("Issued", StringComparison.OrdinalIgnoreCase)
                || label.Contains("Complete", StringComparison.OrdinalIgnoreCase))
                return "st-approved";
            if (label.Contains("Reject", StringComparison.OrdinalIgnoreCase))
                return "st-expiring";
            if (label.Contains("Cancel", StringComparison.OrdinalIgnoreCase))
                return "st-expiring";
            return "st-cat-1";
        }

        return groups
            .Select(g => new ReportDashboardStatusBucket
            {
                Label = g.Label,
                Count = g.Count,
                CssClass = CssFor(g.Label)
            })
            .ToList();
    }

    private static string ResolveOnExtensionProcessStateLabel(
        VwRdVisaAppProgress row,
        Application? app,
        IReadOnlyDictionary<string, ApplicationState> stateByCode)
    {
        var code = row.ProgressStateCode?.Trim();
        string stateLabel;
        if (string.IsNullOrEmpty(code))
        {
            stateLabel = ApplicationProgressPrimaryStateCodeResolver.ResolveDisplayNameFromLatest(null)
                ?? "Being Prepared";
        }
        else if (stateByCode.TryGetValue(code, out var state))
        {
            stateLabel = LookupLocalization.GetDisplayName(state);
        }
        else
        {
            var catalog = LookupLocalization.GetCatalogDisplayName("application-state", code);
            stateLabel = !string.IsNullOrEmpty(catalog)
                ? catalog
                : (!string.IsNullOrWhiteSpace(row.ProgressStateLabel)
                    ? row.ProgressStateLabel.Trim()
                    : code);
        }

        string? ministry = null;
        if (app != null)
        {
            ministry = ApprovalLegProfileMinistryHelper.GetMinistryShortNameForProgressStep(
                app, code, locationCode: null);
        }

        return ApplicationProgressListLabelHelper.FormatStatusLabel(stateLabel, ministry);
    }

    private static ReportDashboardPanelData LoadVisaExtensionLegacy(
        IObjectSpace objectSpace, PersonRecordRole? role, string projectKey,
        ReportDashboardPersonType personType, string subReport,
        string? excelHint, bool excelConfigured, DateTime cutoff)
    {
        var query = objectSpace.GetObjectsQuery<VisaExtensionStatus>()
            .Where(v => v.Person != null && (role == null || v.Person.PersonRole == role)
                        && (v.ApplicationDate == null || v.ApplicationDate >= cutoff));

        if (!string.IsNullOrWhiteSpace(projectKey) && projectKey != "All")
            query = query.Where(v => v.Application != null && v.Application.ProjectContract != null
                && (v.Application.ProjectContract.Name == projectKey || v.Application.ProjectContract.NameTm == projectKey));

        var rows = query.OrderByDescending(v => v.ApplicationDate).Take(PreviewLimit).AsEnumerable()
            .Select(v =>
            {
                var status = v.CurrentState?.Name
                    ?? (v.DaysRemainingOnVisa is int d && d < 30 ? "Expiring Soon" : "Pending");
                return new ReportDashboardPreviewRow
                {
                    RecordId = v.ID,
                    Name = v.Person?.FullName ?? string.Empty,
                    Project = ProjectLabel(v.Application?.ProjectContract),
                    ColumnA = FormatDate(v.ExpiringVisa?.ExpirationDate),
                    ColumnB = FormatDate(v.ApplicationDate),
                    Status = status,
                    StatusCssClass = StatusCss(status, v.DaysRemainingOnVisa)
                };
            }).ToList();

        return BuildPanel(personType, ReportDashboardCategory.VisaExtension, subReport, rows, excelHint, excelConfigured);
    }

    private static ReportDashboardPanelData LoadInvitation(
        IObjectSpace objectSpace, PersonRecordRole? role, string projectKey,
        ReportDashboardPersonType personType, string subReport,
        string? excelHint, bool excelConfigured, DateTime cutoff)
    {
        // Legacy issued-inv / default → Ready by Project (valid, unused).
        if (subReport is "issued-inv" or "default" or "by-month" || string.IsNullOrWhiteSpace(subReport))
            subReport = "ready-by-project";
        // Legacy separate period/category tabs → combined Period · Category.
        if (subReport is "ready-by-period" or "ready-by-category")
            subReport = "ready-by-period-category";
        // Legacy Invitation Rejected → Process Result.
        if (subReport is "rejected-by-project")
            subReport = "process-result";
        if (subReport is "rejected-by-period-category-type")
            subReport = "process-result-by-period-category-type";

        return subReport switch
        {
            "in-process" or "in-process-by-period-category-type" => LoadInvitationInProcessFromView(
                objectSpace, role, projectKey, personType, subReport, excelHint, excelConfigured, cutoff),
            "process-result" or "process-result-by-period-category-type" => LoadInvitationProcessResult(
                objectSpace, role, projectKey, personType, subReport, excelHint, excelConfigured, cutoff),
            "used" or "used-by-period-category-type" => LoadInvitationUsedFromView(
                objectSpace, role, projectKey, personType, subReport, excelHint, excelConfigured, cutoff),
            "valid-until" or "expired" => LoadInvitationValidUntilFromView(
                objectSpace, role, projectKey, personType, subReport, excelHint, excelConfigured, cutoff),
            "ready-by-period-category" => LoadInvitationReadyFromView(
                objectSpace, role, projectKey, personType, "ready-by-period-category", excelHint, excelConfigured, cutoff),
            _ => LoadInvitationReadyFromView(
                objectSpace, role, projectKey, personType, "ready-by-project", excelHint, excelConfigured, cutoff),
        };
    }

    private static ReportDashboardPanelData LoadInvitationReadyFromView(
        IObjectSpace objectSpace, PersonRecordRole? role, string projectKey,
        ReportDashboardPersonType personType, string subReport,
        string? excelHint, bool excelConfigured, DateTime cutoff)
    {
        var byPeriodCategory = string.Equals(subReport, "ready-by-period-category", StringComparison.OrdinalIgnoreCase);
        if (!byPeriodCategory)
            subReport = "ready-by-project";

        if (objectSpace is not EFCoreObjectSpace efOs
            || efOs.DbContext is not Visa2026EFCoreDbContext db)
        {
            if (byPeriodCategory)
                return LoadInvitationReadyByPeriodCategoryLegacy(
                    objectSpace, role, projectKey, personType, subReport, excelHint, excelConfigured, cutoff);
            return LoadInvitationItemsByProject(
                objectSpace, role, projectKey, personType, subReport, excelHint, excelConfigured, cutoff,
                InvitationItemBucket.Ready);
        }

        try
        {
            IQueryable<VwRdInvitationReady> query = db.VwRdInvitationReady.AsNoTracking();

            if (role.HasValue)
                query = query.Where(r => r.PersonRoleCode == (int)role.Value);

            query = query.Where(r => !r.IsArchived);

            if (cutoff > DateTime.MinValue)
                query = query.Where(r => r.IssuedDate == null || r.IssuedDate >= cutoff);

            if (!string.IsNullOrWhiteSpace(projectKey) && projectKey != "All")
            {
                query = query.Where(r =>
                    r.ProjectNameTm == projectKey
                    || r.ProjectName == projectKey
                    || r.ProjectNameRaw == projectKey
                    || r.StatusLabel == projectKey);
            }

            var list = query.ToList();

            var labeled = list.Select(r =>
            {
                string status;
                if (byPeriodCategory)
                {
                    var period = string.IsNullOrWhiteSpace(r.VisaPeriodLabel) ? "(No period)" : r.VisaPeriodLabel!.Trim();
                    var category = string.IsNullOrWhiteSpace(r.VisaCategoryLabel) ? "(No category)" : r.VisaCategoryLabel!.Trim();
                    var type = string.IsNullOrWhiteSpace(r.VisaTypeLabel) ? "(No type)" : r.VisaTypeLabel!.Trim();
                    status = $"{period} · {category} · {type}";
                }
                else
                {
                    status = string.IsNullOrWhiteSpace(r.StatusLabel) ? "(No project)" : r.StatusLabel!.Trim();
                }
                return (Row: r, Status: status);
            }).ToList();

            var groups = labeled
                .GroupBy(x => x.Status, StringComparer.Ordinal)
                .Select(g => (Label: g.Key, Count: g.Count()))
                .OrderByDescending(g => g.Count)
                .ToList();
            var buckets = AssignCategoricalCss(groups);
            var cssByLabel = buckets.ToDictionary(b => b.Label, b => b.CssClass, StringComparer.Ordinal);

            var rows = labeled
                .OrderByDescending(x => x.Row.ExpirationDate)
                .ThenBy(x => x.Row.PersonName)
                .Take(PreviewLimit)
                .Select(x =>
                {
                    var r = x.Row;
                    return new ReportDashboardPreviewRow
                    {
                        RecordId = r.ID,
                        Name = r.PersonName ?? string.Empty,
                        Project = r.ProjectName ?? string.Empty,
                        ColumnA = r.InvitationNumber ?? string.Empty,
                        ColumnB = FormatDate(r.ExpirationDate),
                        Status = x.Status,
                        StatusCssClass = cssByLabel.TryGetValue(x.Status, out var c) ? c : "st-cat-1"
                    };
                })
                .ToList();

            return BuildPanel(
                personType, ReportDashboardCategory.Invitation, subReport, rows,
                excelHint, excelConfigured, buckets, labeled.Count);
        }
        catch (Exception ex) when (IsMissingReportDashboardView(ex))
        {
            if (byPeriodCategory)
                return LoadInvitationReadyByPeriodCategoryLegacy(
                    objectSpace, role, projectKey, personType, subReport, excelHint, excelConfigured, cutoff);
            return LoadInvitationItemsByProject(
                objectSpace, role, projectKey, personType, subReport, excelHint, excelConfigured, cutoff,
                InvitationItemBucket.Ready);
        }
    }

    private static ReportDashboardPanelData LoadInvitationReadyByPeriodCategoryLegacy(
        IObjectSpace objectSpace, PersonRecordRole? role, string projectKey,
        ReportDashboardPersonType personType, string subReport,
        string? excelHint, bool excelConfigured, DateTime cutoff)
    {
        var today = DateTime.Today;
        var query = FilterInvitationItems(objectSpace, role, projectKey, cutoff)
            .Where(i => !i.IsUsed && !i.IsCancelled && !i.IsChanged
                        && i.Invitation != null
                        && i.Invitation.ExpirationDate != null
                        && i.Invitation.ExpirationDate >= today);

        var labeled = query.AsEnumerable().Select(i =>
        {
            var period = i.Invitation?.VisaPeriod?.NameTm
                ?? i.Invitation?.VisaPeriod?.Name;
            var category = i.Invitation?.VisaCategory?.NameTm
                ?? i.Invitation?.VisaCategory?.Name;
            var type = i.Invitation?.Application?.VisaType?.NameTm
                ?? i.Invitation?.Application?.VisaType?.Name;
            var periodLabel = string.IsNullOrWhiteSpace(period) ? "(No period)" : period!.Trim();
            var categoryLabel = string.IsNullOrWhiteSpace(category) ? "(No category)" : category!.Trim();
            var typeLabel = string.IsNullOrWhiteSpace(type) ? "(No type)" : type!.Trim();
            var status = $"{periodLabel} · {categoryLabel} · {typeLabel}";
            return (Item: i, Status: status, Project: InvitationItemProjectLabel(i));
        }).ToList();

        var groups = labeled
            .GroupBy(x => x.Status, StringComparer.Ordinal)
            .Select(g => (Label: g.Key, Count: g.Count()))
            .OrderByDescending(g => g.Count)
            .ToList();
        var buckets = AssignCategoricalCss(groups);
        var cssByLabel = buckets.ToDictionary(b => b.Label, b => b.CssClass, StringComparer.Ordinal);

        var rows = labeled
            .OrderByDescending(x => x.Item.Invitation?.ExpirationDate)
            .ThenBy(x => x.Item.Person?.FullName)
            .Take(PreviewLimit)
            .Select(x => new ReportDashboardPreviewRow
            {
                RecordId = x.Item.ID,
                Name = x.Item.Person?.FullName ?? string.Empty,
                Project = x.Project,
                ColumnA = x.Item.Invitation?.InvitationNumber ?? string.Empty,
                ColumnB = FormatDate(x.Item.Invitation?.ExpirationDate),
                Status = x.Status,
                StatusCssClass = cssByLabel.TryGetValue(x.Status, out var c) ? c : "st-cat-1"
            })
            .ToList();

        return BuildPanel(
            personType, ReportDashboardCategory.Invitation, subReport, rows,
            excelHint, excelConfigured, buckets, labeled.Count);
    }

    private static ReportDashboardPanelData LoadInvitationValidUntilFromView(
        IObjectSpace objectSpace, PersonRecordRole? role, string projectKey,
        ReportDashboardPersonType personType, string subReport,
        string? excelHint, bool excelConfigured, DateTime cutoff)
    {
        if (subReport is "expired")
            subReport = "valid-until";

        if (objectSpace is not EFCoreObjectSpace efOs
            || efOs.DbContext is not Visa2026EFCoreDbContext db)
        {
            return LoadInvitationValidUntilLegacy(
                objectSpace, role, projectKey, personType, subReport, excelHint, excelConfigured, cutoff);
        }

        try
        {
            IQueryable<VwRdInvitationValidUntil> query = db.VwRdInvitationValidUntil.AsNoTracking();

            if (role.HasValue)
                query = query.Where(r => r.PersonRoleCode == (int)role.Value);

            query = query.Where(r => !r.IsArchived);

            if (cutoff > DateTime.MinValue)
                query = query.Where(r => r.IssuedDate == null || r.IssuedDate >= cutoff);

            if (!string.IsNullOrWhiteSpace(projectKey) && projectKey != "All")
            {
                query = query.Where(r =>
                    r.ProjectNameTm == projectKey
                    || r.ProjectName == projectKey
                    || r.ProjectNameRaw == projectKey);
            }

            var list = query.ToList();
            return BuildInvitationValidUntilPanel(
                personType, subReport, excelHint, excelConfigured, list
                    .Select(r => (
                        Id: r.ID,
                        Name: r.PersonName ?? string.Empty,
                        Project: r.ProjectName ?? string.Empty,
                        InvitationNumber: r.InvitationNumber ?? string.Empty,
                        ExpirationDate: r.ExpirationDate,
                        DaysRemaining: r.DaysRemaining,
                        ValidityLabel: r.ValidityLabel ?? string.Empty,
                        ValidityCssClass: r.ValidityCssClass ?? "st-pending"))
                    .ToList());
        }
        catch (Exception ex) when (IsMissingReportDashboardView(ex))
        {
            // View not created yet (restart / FORCE_XAF_DB_UPDATE) — same population via EF.
            return LoadInvitationValidUntilLegacy(
                objectSpace, role, projectKey, personType, subReport, excelHint, excelConfigured, cutoff);
        }
    }

    private static ReportDashboardPanelData LoadInvitationValidUntilLegacy(
        IObjectSpace objectSpace, PersonRecordRole? role, string projectKey,
        ReportDashboardPersonType personType, string subReport,
        string? excelHint, bool excelConfigured, DateTime cutoff)
    {
        var today = DateTime.Today;
        var query = FilterInvitationItems(objectSpace, role, projectKey, cutoff)
            .Where(i => !i.IsUsed && !i.IsCancelled && !i.IsChanged
                        && i.Invitation != null
                        && i.Invitation.ExpirationDate != null
                        && i.Invitation.ExpirationDate >= today);

        var list = query.AsEnumerable().Select(i =>
        {
            var days = (i.Invitation!.ExpirationDate!.Value.Date - today).Days;
            var (label, css) = InvitationValidUntilBucket(days);
            return (
                Id: i.ID,
                Name: i.Person?.FullName ?? string.Empty,
                Project: InvitationItemProjectLabel(i),
                InvitationNumber: i.Invitation?.InvitationNumber ?? string.Empty,
                ExpirationDate: i.Invitation?.ExpirationDate,
                DaysRemaining: days,
                ValidityLabel: label,
                ValidityCssClass: css);
        }).ToList();

        return BuildInvitationValidUntilPanel(
            personType, subReport, excelHint, excelConfigured, list);
    }

    private static (string Label, string Css) InvitationValidUntilBucket(int daysRemaining) =>
        daysRemaining switch
        {
            < 1 => ("< 1 day", "st-expiring"),
            < 7 => ("< 1 week", "st-expiring"),
            < 14 => ("< 2 weeks", "st-pending"),
            < 21 => ("< 3 weeks", "st-pending"),
            < 30 => ("< 1 month", "st-pending"),
            < 60 => ("< 2 months", "st-approved"),
            < 90 => ("< 3 months", "st-approved"),
            _ => ("≥ 3 months", "st-approved")
        };

    private static int InvitationValidUntilSortKey(string? label) => label switch
    {
        "< 1 day" => 1,
        "< 1 week" => 2,
        "< 2 weeks" => 3,
        "< 3 weeks" => 4,
        "< 1 month" => 5,
        "< 2 months" => 6,
        "< 3 months" => 7,
        "≥ 3 months" => 8,
        _ => 99
    };

    private static ReportDashboardPanelData BuildInvitationValidUntilPanel(
        ReportDashboardPersonType personType,
        string subReport,
        string? excelHint,
        bool excelConfigured,
        List<(Guid Id, string Name, string Project, string InvitationNumber, DateTime? ExpirationDate, int DaysRemaining, string ValidityLabel, string ValidityCssClass)> list)
    {
        var buckets = list
            .GroupBy(r => new { r.ValidityLabel, r.ValidityCssClass })
            .Select(g => new ReportDashboardStatusBucket
            {
                Label = g.Key.ValidityLabel,
                CssClass = g.Key.ValidityCssClass,
                Count = g.Count()
            })
            .OrderBy(b => InvitationValidUntilSortKey(b.Label))
            .ToList();

        var rows = list
            .OrderBy(r => r.DaysRemaining)
            .ThenBy(r => r.Name)
            .Take(PreviewLimit)
            .Select(r => new ReportDashboardPreviewRow
            {
                RecordId = r.Id,
                Name = r.Name,
                Project = r.Project,
                ColumnA = r.InvitationNumber,
                ColumnB = FormatDate(r.ExpirationDate),
                Status = r.ValidityLabel,
                StatusCssClass = r.ValidityCssClass
            })
            .ToList();

        return BuildPanel(
            personType, ReportDashboardCategory.Invitation, subReport, rows,
            excelHint, excelConfigured, buckets, list.Count);
    }

    private static bool IsMissingReportDashboardView(Exception ex)
    {
        for (var e = ex; e != null; e = e.InnerException!)
        {
            if (e is PostgresException pg
                && (pg.SqlState == PostgresErrorCodes.UndefinedTable
                    || pg.SqlState == PostgresErrorCodes.UndefinedColumn))
                return true;

            if (e is Microsoft.Data.SqlClient.SqlException sql
                && (sql.Number == 208 || sql.Number == 207))
                return true;
        }

        return false;
    }

    private static ReportDashboardPanelData LoadInvitationUsedFromView(
        IObjectSpace objectSpace, PersonRecordRole? role, string projectKey,
        ReportDashboardPersonType personType, string subReport,
        string? excelHint, bool excelConfigured, DateTime cutoff)
    {
        var byPeriodCategoryType = string.Equals(
            subReport, "used-by-period-category-type", StringComparison.OrdinalIgnoreCase);
        if (!byPeriodCategoryType)
            subReport = "used";

        if (objectSpace is not EFCoreObjectSpace efOs
            || efOs.DbContext is not Visa2026EFCoreDbContext db)
        {
            return LoadInvitationItemsByProject(
                objectSpace, role, projectKey, personType, subReport, excelHint, excelConfigured, cutoff,
                InvitationItemBucket.Used);
        }

        try
        {
            IQueryable<VwRdInvitationUsed> query = db.VwRdInvitationUsed.AsNoTracking();

            if (role.HasValue)
                query = query.Where(r => r.PersonRoleCode == (int)role.Value);

            query = query.Where(r => !r.IsArchived);

            if (cutoff > DateTime.MinValue)
                query = query.Where(r => r.IssuedDate == null || r.IssuedDate >= cutoff);

            if (!string.IsNullOrWhiteSpace(projectKey) && projectKey != "All")
            {
                query = query.Where(r =>
                    r.ProjectNameTm == projectKey
                    || r.ProjectName == projectKey
                    || r.ProjectNameRaw == projectKey
                    || r.StatusLabel == projectKey);
            }

            var list = query.ToList();
            Dictionary<Guid, string> pctByItemId = [];
            if (byPeriodCategoryType && list.Count > 0)
            {
                var itemIds = list.Select(r => r.ID).ToList();
                pctByItemId = db.InvitationItems.AsNoTracking()
                    .Where(ii => itemIds.Contains(ii.ID))
                    .Include(ii => ii.Invitation!).ThenInclude(inv => inv.VisaPeriod)
                    .Include(ii => ii.Invitation!).ThenInclude(inv => inv.VisaCategory)
                    .Include(ii => ii.Invitation!).ThenInclude(inv => inv.Application!).ThenInclude(a => a.VisaType)
                    .AsEnumerable()
                    .ToDictionary(
                        ii => ii.ID,
                        ii =>
                        {
                            var period = LookupLabelOrMissing(ii.Invitation?.VisaPeriod, "(No period)");
                            var category = LookupLabelOrMissing(ii.Invitation?.VisaCategory, "(No category)");
                            var type = LookupLabelOrMissing(ii.Invitation?.Application?.VisaType, "(No type)");
                            return $"{period} · {category} · {type}";
                        });
            }

            var labeled = list.Select(r =>
            {
                string status;
                if (byPeriodCategoryType)
                    status = pctByItemId.TryGetValue(r.ID, out var pct) ? pct : "(No period) · (No category) · (No type)";
                else
                    status = string.IsNullOrWhiteSpace(r.StatusLabel) ? "(No project)" : r.StatusLabel!.Trim();
                return (Row: r, Status: status);
            }).ToList();

            var groups = labeled
                .GroupBy(x => x.Status, StringComparer.Ordinal)
                .Select(g => (Label: g.Key, Count: g.Count()))
                .OrderByDescending(g => g.Count)
                .ToList();
            var buckets = AssignCategoricalCss(groups);
            var cssByLabel = buckets.ToDictionary(b => b.Label, b => b.CssClass, StringComparer.Ordinal);

            var rows = labeled
                .OrderByDescending(x => x.Row.IssuedDate)
                .ThenBy(x => x.Row.PersonName)
                .Take(PreviewLimit)
                .Select(x => new ReportDashboardPreviewRow
                {
                    RecordId = x.Row.ID,
                    Name = x.Row.PersonName ?? string.Empty,
                    Project = x.Row.ProjectName ?? string.Empty,
                    ColumnA = x.Row.InvitationNumber ?? string.Empty,
                    ColumnB = FormatDate(x.Row.IssuedDate),
                    Status = x.Status,
                    StatusCssClass = cssByLabel.TryGetValue(x.Status, out var c) ? c : "st-cat-1"
                })
                .ToList();

            return BuildPanel(
                personType, ReportDashboardCategory.Invitation, subReport, rows,
                excelHint, excelConfigured, buckets, labeled.Count);
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UndefinedTable
            || ex.SqlState == PostgresErrorCodes.UndefinedColumn)
        {
            return LoadInvitationItemsByProject(
                objectSpace, role, projectKey, personType, subReport, excelHint, excelConfigured, cutoff,
                InvitationItemBucket.Used);
        }
        catch (Exception ex) when (ex.InnerException is Microsoft.Data.SqlClient.SqlException sqlEx
            && (sqlEx.Number == 208 || sqlEx.Number == 207))
        {
            return LoadInvitationItemsByProject(
                objectSpace, role, projectKey, personType, subReport, excelHint, excelConfigured, cutoff,
                InvitationItemBucket.Used);
        }
    }

    private enum InvitationItemBucket
    {
        Ready,
        Used,
        Expired
    }

    private static ReportDashboardPanelData LoadInvitationItemsByProject(
        IObjectSpace objectSpace, PersonRecordRole? role, string projectKey,
        ReportDashboardPersonType personType, string subReport,
        string? excelHint, bool excelConfigured, DateTime cutoff,
        InvitationItemBucket bucket)
    {
        var today = DateTime.Today;
        var query = FilterInvitationItems(objectSpace, role, projectKey, cutoff)
            .Where(i => !i.IsCancelled && !i.IsChanged);

        query = bucket switch
        {
            InvitationItemBucket.Used => query.Where(i => i.IsUsed),
            InvitationItemBucket.Expired => query.Where(i =>
                !i.IsUsed
                && i.Invitation != null
                && i.Invitation.ExpirationDate != null
                && i.Invitation.ExpirationDate < today),
            _ => query.Where(i =>
                !i.IsUsed
                && i.Invitation != null
                && i.Invitation.ExpirationDate != null
                && i.Invitation.ExpirationDate >= today),
        };

        var labeled = query.AsEnumerable().Select(i =>
        {
            var project = InvitationItemProjectLabel(i);
            return (Item: i, Project: string.IsNullOrWhiteSpace(project) ? "(No project)" : project);
        }).ToList();

        var groups = labeled
            .GroupBy(x => x.Project, StringComparer.Ordinal)
            .Select(g => (Label: g.Key, Count: g.Count()))
            .OrderByDescending(g => g.Count)
            .ToList();
        var buckets = AssignCategoricalCss(groups);
        var cssByLabel = buckets.ToDictionary(b => b.Label, b => b.CssClass, StringComparer.Ordinal);

        var rows = labeled
            .OrderByDescending(x => x.Item.Invitation?.IssuedDate ?? DateTime.MinValue)
            .Take(PreviewLimit)
            .Select(x =>
            {
                var i = x.Item;
                var colB = bucket == InvitationItemBucket.Used
                    ? FormatDate(i.Invitation?.IssuedDate)
                    : FormatDate(i.Invitation?.ExpirationDate);
                return new ReportDashboardPreviewRow
                {
                    RecordId = i.ID,
                    Name = i.Person?.FullName ?? string.Empty,
                    Project = x.Project,
                    ColumnA = i.Invitation?.InvitationNumber ?? string.Empty,
                    ColumnB = colB,
                    Status = x.Project,
                    StatusCssClass = cssByLabel.TryGetValue(x.Project, out var c) ? c : "st-cat-1"
                };
            })
            .ToList();

        return BuildPanel(
            personType, ReportDashboardCategory.Invitation, subReport, rows,
            excelHint, excelConfigured, buckets, labeled.Count);
    }

    /// <summary>
    /// Application (direct migration) On Process (A) / Process Complete from vw_rd_*.
    /// Chart Status = Application Type · StatusListLabel (process state).
    /// </summary>
    private static ReportDashboardPanelData LoadApplicationDirectMigrationFromView(
        IObjectSpace objectSpace, PersonRecordRole? role, string projectKey,
        ReportDashboardPersonType personType, string subReport,
        string? excelHint, bool excelConfigured, DateTime cutoff)
    {
        if (!ReportDashboardCatalog.UsesApplicationDirectMigrationRdListView(subReport))
            subReport = ReportDashboardCatalog.AppDirectOnProcessAKey;

        if (objectSpace is not EFCoreObjectSpace efOs
            || efOs.DbContext is not Visa2026EFCoreDbContext db)
        {
            return EmptyPanel(
                personType, ReportDashboardCategory.ApplicationDirectMigration, subReport, excelHint, excelConfigured);
        }

        try
        {
            var completed = ReportDashboardCatalog.UsesApplicationDirectMigrationRdCompletedListView(subReport);

            List<(Guid ID, Guid? ApplicationOid, string? PersonName, string? ProjectName, string? ProjectNameRaw,
                string? ProjectNameTm, int PersonRoleCode, string? ApplicationTypeLabel, string? ApplicationNumber,
                DateTime? ApplicationDate, string? ProgressStateCode, string? StatusLabel)> list;

            if (completed)
            {
                list = db.VwRdApplicationDirectMigrationProcessComplete.AsNoTracking()
                    .Where(r => !r.IsArchived)
                    .AsEnumerable()
                    .Select(r => (
                        r.ID, r.ApplicationOid, r.PersonName, r.ProjectName, r.ProjectNameRaw, r.ProjectNameTm,
                        r.PersonRoleCode, r.ApplicationTypeLabel, r.ApplicationNumber, r.ApplicationDate,
                        r.ProgressStateCode, r.StatusLabel))
                    .ToList();
            }
            else
            {
                list = db.VwRdApplicationDirectMigrationOnProcessA.AsNoTracking()
                    .Where(r => !r.IsArchived)
                    .AsEnumerable()
                    .Select(r => (
                        r.ID, r.ApplicationOid, r.PersonName, r.ProjectName, r.ProjectNameRaw, r.ProjectNameTm,
                        r.PersonRoleCode, r.ApplicationTypeLabel, r.ApplicationNumber, r.ApplicationDate,
                        r.ProgressStateCode, r.StatusLabel))
                    .ToList();
            }

            if (cutoff > DateTime.MinValue)
                list = list.Where(r => r.ApplicationDate == null || r.ApplicationDate >= cutoff).ToList();

            if (!string.IsNullOrWhiteSpace(projectKey) && projectKey != "All")
            {
                list = list.Where(r =>
                    r.ProjectNameTm == projectKey
                    || r.ProjectName == projectKey
                    || r.ProjectNameRaw == projectKey).ToList();
            }

            if (role.HasValue)
            {
                var roleValue = (int)role.Value;
                list = list.Where(r => r.PersonRoleCode == roleValue).ToList();
            }

            var codes = list
                .Select(r => r.ProgressStateCode?.Trim())
                .Where(c => !string.IsNullOrEmpty(c))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var stateByCode = db.ApplicationStates.AsNoTracking()
                .Where(s => s.Code != null && codes.Contains(s.Code))
                .AsEnumerable()
                .Where(s => !string.IsNullOrWhiteSpace(s.Code))
                .GroupBy(s => s.Code!.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            var appIds = list
                .Where(r => r.ApplicationOid.HasValue)
                .Select(r => r.ApplicationOid!.Value)
                .Distinct()
                .ToList();
            Dictionary<Guid, Application> appsById = [];
            if (appIds.Count > 0)
            {
                appsById = db.Applications.AsNoTracking()
                    .Where(a => appIds.Contains(a.ID))
                    .Include(a => a.ApprovalLegSnapshots)
                    .Include(a => a.ApprovalLegProfile!)
                        .ThenInclude(p => p.MinistryLegs!)
                        .ThenInclude(l => l.ApprovingMinistry)
                    .ToList()
                    .ToDictionary(a => a.ID);
            }

            var labeled = list.Select(r =>
            {
                Application? app = null;
                if (r.ApplicationOid is Guid appOid)
                    appsById.TryGetValue(appOid, out app);

                var viaRow = new AppViaMinistryRow(
                    r.ID, r.ApplicationOid, r.PersonName, r.ProjectName, r.ProjectNameRaw, r.ProjectNameTm,
                    r.PersonRoleCode, null, r.ApplicationTypeLabel, null, null, null, null, null,
                    r.ApplicationNumber, r.ApplicationDate, r.ProgressStateCode, r.StatusLabel,
                    null, null, null);
                var processState = ResolveApplicationViaMinistryProcessStateLabel(viaRow, app, stateByCode);
                var appType = string.IsNullOrWhiteSpace(r.ApplicationTypeLabel)
                    ? "(No type)"
                    : r.ApplicationTypeLabel!.Trim();
                var status = appType + " · " + processState;
                return (Row: r, Status: status, ProcessState: processState);
            }).ToList();

            var groups = labeled
                .GroupBy(x => x.Status, StringComparer.Ordinal)
                .Select(g => (Label: g.Key, Count: g.Count()))
                .OrderByDescending(g => g.Count)
                .ToList();
            var buckets = completed ? AssignExtensionResultCss(groups) : AssignCategoricalCss(groups);
            var cssByLabel = buckets.ToDictionary(b => b.Label, b => b.CssClass, StringComparer.Ordinal);

            var rows = labeled
                .OrderByDescending(x => x.Row.ApplicationDate)
                .ThenBy(x => x.Row.PersonName)
                .Take(PreviewLimit)
                .Select(x =>
                {
                    var processCss = StatusCss(x.ProcessState, null);
                    return new ReportDashboardPreviewRow
                    {
                        RecordId = x.Row.ID,
                        Name = x.Row.PersonName ?? string.Empty,
                        Project = x.Row.ProjectName ?? string.Empty,
                        ColumnA = x.Row.ApplicationTypeLabel ?? string.Empty,
                        ColumnB = x.Row.ApplicationNumber ?? string.Empty,
                        ColumnC = FormatDate(x.Row.ApplicationDate),
                        Status = x.Status,
                        StatusCssClass = string.IsNullOrWhiteSpace(processCss)
                            ? (cssByLabel.TryGetValue(x.Status, out var c) ? c : (completed ? "st-expiring" : "st-pending"))
                            : processCss
                    };
                })
                .ToList();

            return BuildPanel(
                personType, ReportDashboardCategory.ApplicationDirectMigration, subReport, rows,
                excelHint, excelConfigured, buckets, labeled.Count);
        }
        catch (Exception ex) when (IsMissingReportDashboardView(ex))
        {
            return EmptyPanel(
                personType, ReportDashboardCategory.ApplicationDirectMigration, subReport, excelHint, excelConfigured);
        }
    }

    /// <summary>
    /// Application (via ministry) dedicated vw_rd_* panels.
    /// Chart Status = Project · StatusListLabel or Period · Category · Type · StatusListLabel.
    /// </summary>
    private static ReportDashboardPanelData LoadApplicationViaMinistryFromView(
        IObjectSpace objectSpace, PersonRecordRole? role, string projectKey,
        ReportDashboardPersonType personType, string subReport,
        string? excelHint, bool excelConfigured, DateTime cutoff)
    {
        if (!ReportDashboardCatalog.UsesApplicationViaMinistryRdListView(subReport))
            subReport = ReportDashboardCatalog.AppViaMinistryInvitationOnProcessKey;

        if (objectSpace is not EFCoreObjectSpace efOs
            || efOs.DbContext is not Visa2026EFCoreDbContext db)
        {
            if (ReportDashboardCatalog.UsesApplicationViaMinistryInvitationOnProcessListView(subReport))
            {
                return LoadApplicationViaMinistryInvitationOnProcessLegacy(
                    objectSpace, role, projectKey, personType, subReport, excelHint, excelConfigured, cutoff);
            }
            return EmptyPanel(personType, ReportDashboardCategory.ApplicationViaMinistry, subReport, excelHint, excelConfigured);
        }

        try
        {
            var list = QueryApplicationViaMinistryRows(db, subReport, projectKey, cutoff, role);
            var byV = ReportDashboardCatalog.UsesApplicationViaMinistryRdByPeriodCategoryType(subReport);
            var completed = ReportDashboardCatalog.UsesApplicationViaMinistryRdCompletedListView(subReport);
            var statusByRow = ResolveApplicationViaMinistryChartStatus(db, list, byV);

            var groups = list
                .GroupBy(r => statusByRow[r.ID], StringComparer.Ordinal)
                .Select(g => (Label: g.Key, Count: g.Count()))
                .OrderByDescending(g => g.Count)
                .ToList();
            var buckets = completed ? AssignExtensionResultCss(groups) : AssignCategoricalCss(groups);
            var cssByLabel = buckets.ToDictionary(b => b.Label, b => b.CssClass, StringComparer.Ordinal);

            var rows = list
                .OrderByDescending(r => r.ApplicationDate)
                .ThenBy(r => r.PersonName)
                .Take(PreviewLimit)
                .Select(r =>
                {
                    var status = statusByRow[r.ID];
                    var processCss = StatusCss(
                        string.IsNullOrWhiteSpace(r.StatusLabel) ? status : r.StatusLabel!, null);
                    var showVisaDims = ReportDashboardCatalog.UsesApplicationViaMinistryInvitationOrVisaExtListView(subReport);
                    var showExtVisaCols = ReportDashboardCatalog.UsesApplicationViaMinistryVisaExtCompletedVisaColumns(subReport);
                    var showInvitationCol =
                        ReportDashboardCatalog.UsesApplicationViaMinistryInvitationCompletedInvitationColumn(subReport);
                    // App # / App Date sit right after whichever issued-document columns the
                    // subreport carries: two for Visa Ext Completed, one for Invitation Completed,
                    // none elsewhere. Order must match TableHeaders for that subreport.
                    return new ReportDashboardPreviewRow
                    {
                        RecordId = r.ID,
                        Name = r.PersonName ?? string.Empty,
                        Project = r.ProjectName ?? string.Empty,
                        ColumnA = r.PositionLabel ?? string.Empty,
                        ColumnB = r.ApplicationTypeLabel ?? string.Empty,
                        ColumnC = showVisaDims ? (r.VisaPeriodLabel ?? string.Empty) : (r.ApplicationNumber ?? string.Empty),
                        ColumnD = showVisaDims ? (r.VisaTypeLabel ?? string.Empty) : FormatDate(r.ApplicationDate),
                        ColumnE = showExtVisaCols
                            ? (r.VisaOnExtensionNumber ?? string.Empty)
                            : showInvitationCol
                                ? (r.InvitationNumber ?? string.Empty)
                                : (showVisaDims ? (r.ApplicationNumber ?? string.Empty) : string.Empty),
                        ColumnF = showExtVisaCols
                            ? (r.IssuedVisaNumber ?? string.Empty)
                            : showInvitationCol
                                ? (r.ApplicationNumber ?? string.Empty)
                                : (showVisaDims ? FormatDate(r.ApplicationDate) : string.Empty),
                        ColumnG = showExtVisaCols
                            ? (r.ApplicationNumber ?? string.Empty)
                            : showInvitationCol ? FormatDate(r.ApplicationDate) : string.Empty,
                        ColumnH = showExtVisaCols ? FormatDate(r.ApplicationDate) : string.Empty,
                        Status = status,
                        StatusCssClass = string.IsNullOrWhiteSpace(processCss)
                            ? (cssByLabel.TryGetValue(status, out var c) ? c : (completed ? "st-expiring" : "st-pending"))
                            : processCss
                    };
                })
                .ToList();

            return BuildPanel(
                personType, ReportDashboardCategory.ApplicationViaMinistry, subReport, rows,
                excelHint, excelConfigured, buckets, list.Count);
        }
        catch (Exception ex) when (IsMissingReportDashboardView(ex))
        {
            if (ReportDashboardCatalog.UsesApplicationViaMinistryInvitationOnProcessListView(subReport))
            {
                return LoadApplicationViaMinistryInvitationOnProcessLegacy(
                    objectSpace, role, projectKey, personType, subReport, excelHint, excelConfigured, cutoff);
            }
            return EmptyPanel(personType, ReportDashboardCategory.ApplicationViaMinistry, subReport, excelHint, excelConfigured);
        }
    }

    private readonly record struct AppViaMinistryRow(
        Guid ID,
        Guid? ApplicationOid,
        string? PersonName,
        string? ProjectName,
        string? ProjectNameRaw,
        string? ProjectNameTm,
        int PersonRoleCode,
        string? PositionLabel,
        string? ApplicationTypeLabel,
        string? VisaPeriodLabel,
        string? VisaTypeLabel,
        string? VisaOnExtensionNumber,
        string? IssuedVisaNumber,
        string? InvitationNumber,
        string? ApplicationNumber,
        DateTime? ApplicationDate,
        string? ProgressStateCode,
        string? StatusLabel,
        string? PeriodLabel,
        string? CategoryLabel,
        string? TypeLabel);

    private static List<AppViaMinistryRow> QueryApplicationViaMinistryRows(
        Visa2026EFCoreDbContext db,
        string subReport,
        string projectKey,
        DateTime cutoff,
        PersonRecordRole? role)
    {
        List<AppViaMinistryRow> list = subReport switch
        {
            ReportDashboardCatalog.AppViaMinistryInvitationOnProcessKey =>
                db.VwRdApplicationViaMinistryInvitationOnProcess.AsNoTracking()
                    .Where(r => !r.IsArchived)
                    .AsEnumerable()
                    .Select(p => new AppViaMinistryRow(
                        p.ID, p.ApplicationOid, p.PersonName, p.ProjectName, p.ProjectNameRaw, p.ProjectNameTm,
                        p.PersonRoleCode, p.PositionLabel, p.ApplicationTypeLabel,
                        p.VisaPeriodLabel, p.VisaTypeLabel,
                        null, null, null,
                        p.ApplicationNumber, p.ApplicationDate, p.ProgressStateCode, p.StatusLabel,
                        null, null, null))
                    .ToList(),
            ReportDashboardCatalog.AppViaMinistryInvitationOnProcessVKey =>
                db.VwRdApplicationViaMinistryInvitationOnProcessByPeriodCategoryType.AsNoTracking()
                    .Where(r => !r.IsArchived)
                    .AsEnumerable()
                    .Select(r => new AppViaMinistryRow(
                        r.ID, r.ApplicationOid, r.PersonName, r.ProjectName, r.ProjectNameRaw, r.ProjectNameTm,
                        r.PersonRoleCode, r.PositionLabel, r.ApplicationTypeLabel,
                        r.VisaPeriodLabel, r.VisaTypeLabel,
                        null, null, null,
                        r.ApplicationNumber, r.ApplicationDate, r.ProgressStateCode, r.StatusLabel,
                        r.PeriodLabel, r.CategoryLabel, r.TypeLabel))
                    .ToList(),
            ReportDashboardCatalog.AppViaMinistryInvitationCompletedKey =>
                db.VwRdApplicationViaMinistryInvitationCompleted.AsNoTracking()
                    .Where(r => !r.IsArchived)
                    .AsEnumerable()
                    .Select(r => new AppViaMinistryRow(
                        r.ID, r.ApplicationOid, r.PersonName, r.ProjectName, r.ProjectNameRaw, r.ProjectNameTm,
                        r.PersonRoleCode, r.PositionLabel, r.ApplicationTypeLabel,
                        r.VisaPeriodLabel, r.VisaTypeLabel,
                        null, null, r.InvitationNumber,
                        r.ApplicationNumber, r.ApplicationDate, r.ProgressStateCode, r.StatusLabel,
                        r.PeriodLabel, r.CategoryLabel, r.TypeLabel))
                    .ToList(),
            ReportDashboardCatalog.AppViaMinistryInvitationCompletedVKey =>
                db.VwRdApplicationViaMinistryInvitationCompletedByPeriodCategoryType.AsNoTracking()
                    .Where(r => !r.IsArchived)
                    .AsEnumerable()
                    .Select(r => new AppViaMinistryRow(
                        r.ID, r.ApplicationOid, r.PersonName, r.ProjectName, r.ProjectNameRaw, r.ProjectNameTm,
                        r.PersonRoleCode, r.PositionLabel, r.ApplicationTypeLabel,
                        r.VisaPeriodLabel, r.VisaTypeLabel,
                        null, null, r.InvitationNumber,
                        r.ApplicationNumber, r.ApplicationDate, r.ProgressStateCode, r.StatusLabel,
                        r.PeriodLabel, r.CategoryLabel, r.TypeLabel))
                    .ToList(),
            ReportDashboardCatalog.AppViaMinistryVisaExtOnProcessKey =>
                db.VwRdApplicationViaMinistryVisaExtensionOnProcess.AsNoTracking()
                    .Where(r => !r.IsArchived)
                    .AsEnumerable()
                    .Select(r => new AppViaMinistryRow(
                        r.ID, r.ApplicationOid, r.PersonName, r.ProjectName, r.ProjectNameRaw, r.ProjectNameTm,
                        r.PersonRoleCode, r.PositionLabel, r.ApplicationTypeLabel,
                        r.VisaPeriodLabel, r.VisaTypeLabel,
                        null, null, null,
                        r.ApplicationNumber, r.ApplicationDate, r.ProgressStateCode, r.StatusLabel,
                        r.PeriodLabel, r.CategoryLabel, r.TypeLabel))
                    .ToList(),
            ReportDashboardCatalog.AppViaMinistryVisaExtOnProcessVKey =>
                db.VwRdApplicationViaMinistryVisaExtensionOnProcessByPeriodCategoryType.AsNoTracking()
                    .Where(r => !r.IsArchived)
                    .AsEnumerable()
                    .Select(r => new AppViaMinistryRow(
                        r.ID, r.ApplicationOid, r.PersonName, r.ProjectName, r.ProjectNameRaw, r.ProjectNameTm,
                        r.PersonRoleCode, r.PositionLabel, r.ApplicationTypeLabel,
                        r.VisaPeriodLabel, r.VisaTypeLabel,
                        null, null, null,
                        r.ApplicationNumber, r.ApplicationDate, r.ProgressStateCode, r.StatusLabel,
                        r.PeriodLabel, r.CategoryLabel, r.TypeLabel))
                    .ToList(),
            ReportDashboardCatalog.AppViaMinistryVisaExtCompletedKey =>
                db.VwRdApplicationViaMinistryVisaExtensionCompleted.AsNoTracking()
                    .Where(r => !r.IsArchived)
                    .AsEnumerable()
                    .Select(r => new AppViaMinistryRow(
                        r.ID, r.ApplicationOid, r.PersonName, r.ProjectName, r.ProjectNameRaw, r.ProjectNameTm,
                        r.PersonRoleCode, r.PositionLabel, r.ApplicationTypeLabel,
                        r.VisaPeriodLabel, r.VisaTypeLabel,
                        r.VisaOnExtensionNumber, r.IssuedVisaNumber, null,
                        r.ApplicationNumber, r.ApplicationDate, r.ProgressStateCode, r.StatusLabel,
                        r.PeriodLabel, r.CategoryLabel, r.TypeLabel))
                    .ToList(),
            ReportDashboardCatalog.AppViaMinistryVisaExtCompletedVKey =>
                db.VwRdApplicationViaMinistryVisaExtensionCompletedByPeriodCategoryType.AsNoTracking()
                    .Where(r => !r.IsArchived)
                    .AsEnumerable()
                    .Select(r => new AppViaMinistryRow(
                        r.ID, r.ApplicationOid, r.PersonName, r.ProjectName, r.ProjectNameRaw, r.ProjectNameTm,
                        r.PersonRoleCode, r.PositionLabel, r.ApplicationTypeLabel,
                        r.VisaPeriodLabel, r.VisaTypeLabel,
                        r.VisaOnExtensionNumber, r.IssuedVisaNumber, null,
                        r.ApplicationNumber, r.ApplicationDate, r.ProgressStateCode, r.StatusLabel,
                        r.PeriodLabel, r.CategoryLabel, r.TypeLabel))
                    .ToList(),
            ReportDashboardCatalog.AppViaMinistryOtherOnProcessKey =>
                db.VwRdApplicationViaMinistryOtherOnProcess.AsNoTracking()
                    .Where(r => !r.IsArchived)
                    .AsEnumerable()
                    .Select(r => new AppViaMinistryRow(
                        r.ID, r.ApplicationOid, r.PersonName, r.ProjectName, r.ProjectNameRaw, r.ProjectNameTm,
                        r.PersonRoleCode, r.PositionLabel, r.ApplicationTypeLabel,
                        r.VisaPeriodLabel, r.VisaTypeLabel,
                        null, null, null,
                        r.ApplicationNumber, r.ApplicationDate, r.ProgressStateCode, r.StatusLabel,
                        r.PeriodLabel, r.CategoryLabel, r.TypeLabel))
                    .ToList(),
            ReportDashboardCatalog.AppViaMinistryOtherCompletedKey =>
                db.VwRdApplicationViaMinistryOtherCompleted.AsNoTracking()
                    .Where(r => !r.IsArchived)
                    .AsEnumerable()
                    .Select(r => new AppViaMinistryRow(
                        r.ID, r.ApplicationOid, r.PersonName, r.ProjectName, r.ProjectNameRaw, r.ProjectNameTm,
                        r.PersonRoleCode, r.PositionLabel, r.ApplicationTypeLabel,
                        r.VisaPeriodLabel, r.VisaTypeLabel,
                        null, null, null,
                        r.ApplicationNumber, r.ApplicationDate, r.ProgressStateCode, r.StatusLabel,
                        r.PeriodLabel, r.CategoryLabel, r.TypeLabel))
                    .ToList(),
            _ => []
        };

        return FilterApplicationViaMinistryRows(list, projectKey, cutoff, role);
    }

    private static List<AppViaMinistryRow> FilterApplicationViaMinistryRows(
        List<AppViaMinistryRow> list,
        string projectKey,
        DateTime cutoff,
        PersonRecordRole? role)
    {
        if (cutoff > DateTime.MinValue)
            list = list.Where(r => r.ApplicationDate == null || r.ApplicationDate >= cutoff).ToList();

        if (!string.IsNullOrWhiteSpace(projectKey) && projectKey != "All")
        {
            list = list.Where(r =>
                r.ProjectNameTm == projectKey
                || r.ProjectName == projectKey
                || r.ProjectNameRaw == projectKey).ToList();
        }

        if (role.HasValue)
        {
            var roleValue = (int)role.Value;
            list = list.Where(r => r.PersonRoleCode == roleValue).ToList();
        }

        return list;
    }

    private static Dictionary<Guid, string> ResolveApplicationViaMinistryChartStatus(
        Visa2026EFCoreDbContext db,
        List<AppViaMinistryRow> rows,
        bool byPeriodCategoryType)
    {
        var result = new Dictionary<Guid, string>(rows.Count);
        var codes = rows
            .Select(r => r.ProgressStateCode?.Trim())
            .Where(c => !string.IsNullOrEmpty(c))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var stateByCode = db.ApplicationStates.AsNoTracking()
            .Where(s => s.Code != null && codes.Contains(s.Code))
            .AsEnumerable()
            .Where(s => !string.IsNullOrWhiteSpace(s.Code))
            .GroupBy(s => s.Code!.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        var appIds = rows
            .Where(r => r.ApplicationOid.HasValue)
            .Select(r => r.ApplicationOid!.Value)
            .Distinct()
            .ToList();
        Dictionary<Guid, Application> appsById = [];
        if (appIds.Count > 0)
        {
            appsById = db.Applications.AsNoTracking()
                .Where(a => appIds.Contains(a.ID))
                .Include(a => a.ApprovalLegSnapshots)
                .Include(a => a.ApprovalLegProfile!)
                    .ThenInclude(p => p.MinistryLegs!)
                    .ThenInclude(l => l.ApprovingMinistry)
                .ToList()
                .ToDictionary(a => a.ID);
        }

        foreach (var row in rows)
        {
            Application? app = null;
            if (row.ApplicationOid is Guid appOid)
                appsById.TryGetValue(appOid, out app);
            var processState = ResolveApplicationViaMinistryProcessStateLabel(row, app, stateByCode);
            if (byPeriodCategoryType)
            {
                var period = string.IsNullOrWhiteSpace(row.PeriodLabel) ? "(No period)" : row.PeriodLabel.Trim();
                var category = string.IsNullOrWhiteSpace(row.CategoryLabel) ? "(No category)" : row.CategoryLabel.Trim();
                var type = string.IsNullOrWhiteSpace(row.TypeLabel) ? "(No type)" : row.TypeLabel.Trim();
                result[row.ID] = period + " · " + category + " · " + type + " · " + processState;
            }
            else
            {
                var project = string.IsNullOrWhiteSpace(row.ProjectName) ? "(No project)" : row.ProjectName.Trim();
                result[row.ID] = project + " · " + processState;
            }
        }

        return result;
    }

    private static string ResolveApplicationViaMinistryProcessStateLabel(
        AppViaMinistryRow row,
        Application? app,
        IReadOnlyDictionary<string, ApplicationState> stateByCode)
    {
        var code = row.ProgressStateCode?.Trim();
        string stateLabel;
        if (string.IsNullOrEmpty(code))
        {
            stateLabel = ApplicationProgressPrimaryStateCodeResolver.ResolveDisplayNameFromLatest(null)
                ?? (!string.IsNullOrWhiteSpace(row.StatusLabel) ? row.StatusLabel.Trim() : "At office");
        }
        else if (stateByCode.TryGetValue(code, out var state))
        {
            stateLabel = LookupLocalization.GetDisplayName(state);
        }
        else
        {
            var catalog = LookupLocalization.GetCatalogDisplayName("application-state", code);
            stateLabel = !string.IsNullOrEmpty(catalog)
                ? catalog
                : (!string.IsNullOrWhiteSpace(row.StatusLabel) ? row.StatusLabel.Trim() : code);
        }

        string? ministry = null;
        if (app != null)
        {
            ministry = ApprovalLegProfileMinistryHelper.GetMinistryShortNameForProgressStep(
                app, code, locationCode: null);
        }

        return ApplicationProgressListLabelHelper.FormatStatusLabel(stateLabel, ministry);
    }

    /// <summary>
    /// EF fallback when <c>vw_rd_application_via_ministry_invitation_on_process</c> is missing.
    /// Same population: ViaMinistries + CanIssueInvitation + non-terminal + no linked Invitation.
    /// </summary>
    private static ReportDashboardPanelData LoadApplicationViaMinistryInvitationOnProcessLegacy(
        IObjectSpace objectSpace, PersonRecordRole? role, string projectKey,
        ReportDashboardPersonType personType, string subReport,
        string? excelHint, bool excelConfigured, DateTime cutoff)
    {
        subReport = ReportDashboardCatalog.AppViaMinistryInvitationOnProcessKey;

        var query = objectSpace.GetObjectsQuery<Application>()
            .Where(a => a.ApplicationType != null
                        && a.ApplicationType.CanIssueInvitation
                        && a.ApplicationType.ApplicationProgressRoute
                            == ApplicationProgressRouteKind.ViaMinistries
                        && (a.ApplicationDate == default || a.ApplicationDate >= cutoff)
                        && !a.Invitations.Any());

        if (!string.IsNullOrWhiteSpace(projectKey) && projectKey != "All")
        {
            query = query.Where(a => a.ProjectContract != null
                && (a.ProjectContract.Name == projectKey || a.ProjectContract.NameTm == projectKey));
        }

        var apps = query.AsEnumerable()
            .Where(a =>
            {
                var code = !string.IsNullOrWhiteSpace(a.LatestPrimaryStateCode)
                    ? a.LatestPrimaryStateCode
                    : a.LatestProgress?.State?.Code;
                return !IsInvitationProcessCompleted(code);
            })
            .ToList();

        var codes = apps
            .Select(a => !string.IsNullOrWhiteSpace(a.LatestPrimaryStateCode)
                ? a.LatestPrimaryStateCode!.Trim()
                : a.LatestProgress?.State?.Code?.Trim())
            .Where(c => !string.IsNullOrEmpty(c))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var stateByCode = codes.Count == 0
            ? new Dictionary<string, ApplicationState>(StringComparer.OrdinalIgnoreCase)
            : objectSpace.GetObjectsQuery<ApplicationState>()
                .Where(s => s.Code != null && codes.Contains(s.Code))
                .AsEnumerable()
                .Where(s => !string.IsNullOrWhiteSpace(s.Code))
                .GroupBy(s => s.Code!.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        var itemRows = new List<(ApplicationItem Item, Application App, string Status, string ProcessState, string Project, string Position, string AppType, string VisaPeriod, string VisaType)>();
        foreach (var a in apps)
        {
            var code = !string.IsNullOrWhiteSpace(a.LatestPrimaryStateCode)
                ? a.LatestPrimaryStateCode!.Trim()
                : a.LatestProgress?.State?.Code?.Trim();
            string stateLabel;
            if (string.IsNullOrEmpty(code))
            {
                stateLabel = ApplicationProgressPrimaryStateCodeResolver.ResolveDisplayNameFromLatest(null)
                    ?? "At office";
            }
            else if (stateByCode.TryGetValue(code, out var state))
            {
                stateLabel = LookupLocalization.GetDisplayName(state);
            }
            else
            {
                var catalog = LookupLocalization.GetCatalogDisplayName("application-state", code);
                stateLabel = !string.IsNullOrEmpty(catalog) ? catalog : code;
            }

            var ministry = ApprovalLegProfileMinistryHelper.GetMinistryShortNameForProgressStep(
                a, code, locationCode: null);
            var processState = ApplicationProgressListLabelHelper.FormatStatusLabel(stateLabel, ministry);
            var project = ProjectLabel(a.ProjectContract);
            if (string.IsNullOrWhiteSpace(project))
                project = "(No project)";
            var chartStatus = $"{project} · {processState}";
            var appType = a.ApplicationType?.NameTm
                ?? a.ApplicationType?.Name
                ?? string.Empty;
            var visaPeriod = a.VisaPeriod?.NameTm ?? a.VisaPeriod?.Name ?? string.Empty;
            var visaType = a.VisaType?.NameTm ?? a.VisaType?.Name ?? string.Empty;

            var items = ApplicationRosterHelper.GetMergeLineItems(objectSpace, a);
            if (items.Count == 0)
                continue;

            foreach (var ai in items)
            {
                if (role.HasValue && (ai.Person == null || ai.Person.PersonRole != role.Value))
                    continue;
                var position = ai.CurrentPositionHistory?.Position?.NameTm
                    ?? ai.CurrentPositionHistory?.Position?.Name
                    ?? string.Empty;
                itemRows.Add((ai, a, chartStatus, processState, project, position, appType, visaPeriod, visaType));
            }
        }

        var groups = itemRows
            .GroupBy(x => x.Status, StringComparer.Ordinal)
            .Select(g => (Label: g.Key, Count: g.Count()))
            .OrderByDescending(g => g.Count)
            .ToList();
        var buckets = AssignCategoricalCss(groups);
        var cssByLabel = buckets.ToDictionary(b => b.Label, b => b.CssClass, StringComparer.Ordinal);

        var rows = itemRows
            .OrderByDescending(x => x.App.ApplicationDate)
            .ThenBy(x => x.Item.Person?.FullName ?? string.Empty)
            .Take(PreviewLimit)
            .Select(x =>
            {
                var progressCss = StatusCss(x.ProcessState, null);
                return new ReportDashboardPreviewRow
                {
                    RecordId = x.Item.ID,
                    Name = x.Item.Person?.FullName
                        ?? x.App.FullApplicationNumber
                        ?? x.App.ApplicationNumber
                        ?? string.Empty,
                    Project = x.Project,
                    ColumnA = x.Position,
                    ColumnB = x.AppType,
                    ColumnC = x.VisaPeriod,
                    ColumnD = x.VisaType,
                    ColumnE = x.App.FullApplicationNumber ?? x.App.ApplicationNumber ?? string.Empty,
                    ColumnF = FormatDate(x.App.ApplicationDate),
                    Status = x.Status,
                    StatusCssClass = string.IsNullOrWhiteSpace(progressCss)
                        ? (cssByLabel.TryGetValue(x.Status, out var c) ? c : "st-pending")
                        : progressCss
                };
            })
            .ToList();

        return BuildPanel(
            personType, ReportDashboardCategory.ApplicationViaMinistry, subReport, rows,
            excelHint, excelConfigured, buckets, itemRows.Count);
    }

    private static ReportDashboardPanelData LoadInvitationInProcessFromView(
        IObjectSpace objectSpace, PersonRecordRole? role, string projectKey,
        ReportDashboardPersonType personType, string subReport,
        string? excelHint, bool excelConfigured, DateTime cutoff)
    {
        var byPeriodCategoryType = string.Equals(
            subReport, "in-process-by-period-category-type", StringComparison.OrdinalIgnoreCase);
        if (!byPeriodCategoryType)
            subReport = "in-process";

        if (objectSpace is not EFCoreObjectSpace efOs
            || efOs.DbContext is not Visa2026EFCoreDbContext db)
        {
            return LoadInvitationInProcess(
                objectSpace, role, projectKey, personType, subReport, excelHint, excelConfigured, cutoff);
        }

        try
        {
            IQueryable<VwRdInvitationInProcess> query = db.VwRdInvitationInProcess.AsNoTracking();

            query = query.Where(r => !r.IsArchived);

            if (cutoff > DateTime.MinValue)
                query = query.Where(r => r.ApplicationDate == null || r.ApplicationDate >= cutoff);

            if (!string.IsNullOrWhiteSpace(projectKey) && projectKey != "All")
            {
                query = query.Where(r =>
                    r.ProjectNameTm == projectKey
                    || r.ProjectName == projectKey
                    || r.ProjectNameRaw == projectKey);
            }

            var list = query.ToList();

            // Match EF: person-type filter = any ApplicationItem with that role (not only first person).
            if (role.HasValue)
            {
                var roleValue = role.Value;
                var appIdsWithRole = db.ApplicationItems
                    .AsNoTracking()
                    .Where(ai => ai.Application != null && ai.Person != null && ai.Person.PersonRole == roleValue)
                    .Select(ai => ai.Application!.ID)
                    .Distinct()
                    .ToHashSet();
                list = list.Where(r => appIdsWithRole.Contains(r.ID)).ToList();
            }

            Dictionary<Guid, Application> appsById = [];
            Dictionary<Guid, string?> primaryCodeByAppId = [];
            if (list.Count > 0)
            {
                var appIds = list.Select(r => r.ID).ToList();
                // LatestPrimaryStateCode is authoritative when LatestProgress FK is unset but scalars are synced.
                primaryCodeByAppId = db.Applications.AsNoTracking()
                    .Where(a => appIds.Contains(a.ID))
                    .Select(a => new { a.ID, a.LatestPrimaryStateCode })
                    .AsEnumerable()
                    .ToDictionary(a => a.ID, a => a.LatestPrimaryStateCode);

                // Completed processes (view ProgressStateCode and/or Application.LatestPrimaryStateCode).
                list = list
                    .Where(r =>
                    {
                        if (IsInvitationProcessCompleted(r.ProgressStateCode))
                            return false;
                        primaryCodeByAppId.TryGetValue(r.ID, out var primary);
                        return !IsInvitationProcessCompleted(primary);
                    })
                    .ToList();

                if (list.Count > 0)
                {
                    var remainingIds = list.Select(r => r.ID).ToList();
                    IQueryable<Application> appQuery = db.Applications.AsNoTracking()
                        .Where(a => remainingIds.Contains(a.ID));
                    if (byPeriodCategoryType)
                    {
                        appQuery = appQuery
                            .Include(a => a.VisaPeriod)
                            .Include(a => a.VisaCategory)
                            .Include(a => a.VisaType);
                    }

                    appsById = appQuery.ToList().ToDictionary(a => a.ID);
                }
            }

            var labeled = list.Select(r =>
            {
                var processState = string.IsNullOrWhiteSpace(r.StatusLabel) ? "Being Prepared" : r.StatusLabel!.Trim();
                string status;
                if (byPeriodCategoryType)
                {
                    appsById.TryGetValue(r.ID, out var app);
                    var period = LookupLabelOrMissing(app?.VisaPeriod, "(No period)");
                    var category = LookupLabelOrMissing(app?.VisaCategory, "(No category)");
                    var type = LookupLabelOrMissing(app?.VisaType, "(No type)");
                    status = $"{period} · {category} · {type} · {processState}";
                }
                else
                {
                    var project = string.IsNullOrWhiteSpace(r.ProjectName)
                        ? "(No project)"
                        : r.ProjectName.Trim();
                    status = $"{project} · {processState}";
                }

                return (Row: r, Status: status, ProcessState: processState);
            }).ToList();

            var groups = labeled
                .GroupBy(x => x.Status, StringComparer.Ordinal)
                .Select(g => (Label: g.Key, Count: g.Count()))
                .OrderByDescending(g => g.Count)
                .ToList();
            var buckets = AssignCategoricalCss(groups);
            var cssByLabel = buckets.ToDictionary(b => b.Label, b => b.CssClass, StringComparer.Ordinal);

            var rows = labeled
                .OrderByDescending(x => x.Row.ApplicationDate)
                .ThenBy(x => x.Row.PersonName)
                .Take(PreviewLimit)
                .Select(x =>
                {
                    var progressCss = StatusCss(x.ProcessState, null);
                    return new ReportDashboardPreviewRow
                    {
                        RecordId = x.Row.ID,
                        Name = x.Row.PersonName ?? string.Empty,
                        Project = x.Row.ProjectName ?? string.Empty,
                        ColumnA = x.Row.ApplicationNumber ?? string.Empty,
                        ColumnB = FormatDate(x.Row.ApplicationDate),
                        Status = x.Status,
                        StatusCssClass = string.IsNullOrWhiteSpace(progressCss)
                            ? (cssByLabel.TryGetValue(x.Status, out var c) ? c : "st-pending")
                            : progressCss
                    };
                })
                .ToList();

            return BuildPanel(
                personType, ReportDashboardCategory.Invitation, subReport, rows,
                excelHint, excelConfigured, buckets, labeled.Count);
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UndefinedTable
            || ex.SqlState == PostgresErrorCodes.UndefinedColumn)
        {
            return LoadInvitationInProcess(
                objectSpace, role, projectKey, personType, subReport, excelHint, excelConfigured, cutoff);
        }
        catch (Exception ex) when (ex.InnerException is Microsoft.Data.SqlClient.SqlException sqlEx
            && (sqlEx.Number == 208 || sqlEx.Number == 207))
        {
            return LoadInvitationInProcess(
                objectSpace, role, projectKey, personType, subReport, excelHint, excelConfigured, cutoff);
        }
    }

    private static ReportDashboardPanelData LoadInvitationInProcess(
        IObjectSpace objectSpace, PersonRecordRole? role, string projectKey,
        ReportDashboardPersonType personType, string subReport,
        string? excelHint, bool excelConfigured, DateTime cutoff)
    {
        var byPeriodCategoryType = string.Equals(
            subReport, "in-process-by-period-category-type", StringComparison.OrdinalIgnoreCase);
        if (!byPeriodCategoryType)
            subReport = "in-process";

        // Invitation-issuing applications still in progress (not completed) and with no Invitation yet.
        // Prefer LatestPrimaryStateCode — LatestProgress FK is often null while the scalar code is synced.
        var query = objectSpace.GetObjectsQuery<Application>()
            .Where(a => a.ApplicationType != null
                        && a.ApplicationType.CanIssueInvitation
                        && (a.ApplicationDate == null || a.ApplicationDate >= cutoff)
                        && !a.Invitations.Any());

        if (role.HasValue)
        {
            var roleValue = role.Value;
            query = query.Where(a =>
                a.People.Any(ap => ap.Person != null && ap.Person.PersonRole == roleValue)
                || a.ApplicationItems.Any(ai =>
                    ai.Person != null && ai.Person.PersonRole == roleValue));
        }

        if (!string.IsNullOrWhiteSpace(projectKey) && projectKey != "All")
        {
            query = query.Where(a => a.ProjectContract != null
                && (a.ProjectContract.Name == projectKey || a.ProjectContract.NameTm == projectKey));
        }

        var labeled = query.AsEnumerable()
            .Where(a =>
            {
                var code = !string.IsNullOrWhiteSpace(a.LatestPrimaryStateCode)
                    ? a.LatestPrimaryStateCode
                    : a.LatestProgress?.State?.Code;
                return !IsInvitationProcessCompleted(code);
            })
            .Select(a =>
        {
            var processState = string.IsNullOrWhiteSpace(a.CurrentState) ? "Being Prepared" : a.CurrentState.Trim();
            string status;
            if (byPeriodCategoryType)
            {
                var period = LookupLabelOrMissing(a.VisaPeriod, "(No period)");
                var category = LookupLabelOrMissing(a.VisaCategory, "(No category)");
                var type = LookupLabelOrMissing(a.VisaType, "(No type)");
                status = $"{period} · {category} · {type} · {processState}";
            }
            else
            {
                var project = ProjectLabel(a.ProjectContract);
                if (string.IsNullOrWhiteSpace(project))
                    project = "(No project)";
                status = $"{project} · {processState}";
            }

            return (App: a, Status: status, ProcessState: processState);
        }).ToList();

        var groups = labeled
            .GroupBy(x => x.Status, StringComparer.Ordinal)
            .Select(g => (Label: g.Key, Count: g.Count()))
            .OrderByDescending(g => g.Count)
            .ToList();
        var buckets = AssignCategoricalCss(groups);
        var cssByLabel = buckets.ToDictionary(b => b.Label, b => b.CssClass, StringComparer.Ordinal);

        var rows = labeled
            .OrderByDescending(x => x.App.ApplicationDate)
            .Take(PreviewLimit)
            .Select(x =>
            {
                var a = x.App;
                var progressCss = StatusCss(x.ProcessState, null);
                return new ReportDashboardPreviewRow
                {
                    RecordId = a.ID,
                    Name = FirstApplicationPersonName(a),
                    Project = ProjectLabel(a.ProjectContract),
                    ColumnA = a.FullApplicationNumber ?? a.ApplicationNumber ?? string.Empty,
                    ColumnB = FormatDate(a.ApplicationDate),
                    Status = x.Status,
                    StatusCssClass = string.IsNullOrWhiteSpace(progressCss)
                        ? (cssByLabel.TryGetValue(x.Status, out var c) ? c : "st-pending")
                        : progressCss
                };
            })
            .ToList();

        return BuildPanel(
            personType, ReportDashboardCategory.Invitation, subReport, rows,
            excelHint, excelConfigured, buckets, labeled.Count);
    }

    /// <summary>
    /// Invitation Process Result (P)/(V): CanIssueInvitation apps with terminal latest progress
    /// (PROCESS_ISSUED / PROCESS_CANCELLED / PROCESS_REJECTED / *_REVIEW_REJECTED).
    /// Uses <see cref="Application.LatestPrimaryStateCode"/> (not LatestProgress FK — often unset).
    /// Status = Project · ProcessState or Period · Category · Type · ProcessState
    /// (ProcessState naming matches Extension Result / <see cref="ApplicationProgress.StatusListLabel"/>).
    /// </summary>
    private static ReportDashboardPanelData LoadInvitationProcessResult(
        IObjectSpace objectSpace, PersonRecordRole? role, string projectKey,
        ReportDashboardPersonType personType, string subReport,
        string? excelHint, bool excelConfigured, DateTime cutoff)
    {
        var byPeriodCategoryType = string.Equals(
            subReport, "process-result-by-period-category-type", StringComparison.OrdinalIgnoreCase);
        if (!byPeriodCategoryType)
            subReport = "process-result";

        var query = objectSpace.GetObjectsQuery<Application>()
            .Where(a => a.ApplicationType != null
                        && a.ApplicationType.CanIssueInvitation
                        && (a.ApplicationDate == null || a.ApplicationDate >= cutoff));

        if (role.HasValue)
        {
            var roleValue = role.Value;
            query = query.Where(a =>
                a.People.Any(ap => ap.Person != null && ap.Person.PersonRole == roleValue)
                || a.ApplicationItems.Any(ai =>
                    ai.Person != null && ai.Person.PersonRole == roleValue));
        }

        if (!string.IsNullOrWhiteSpace(projectKey) && projectKey != "All")
        {
            query = query.Where(a => a.ProjectContract != null
                && (a.ProjectContract.Name == projectKey || a.ProjectContract.NameTm == projectKey));
        }

        var apps = query.AsEnumerable()
            .Where(a =>
            {
                var code = !string.IsNullOrWhiteSpace(a.LatestPrimaryStateCode)
                    ? a.LatestPrimaryStateCode
                    : a.LatestProgress?.State?.Code;
                return IsInvitationProcessResultState(code);
            })
            .ToList();

        Dictionary<string, ApplicationState> stateByCode = [];
        var codes = apps
            .Select(a => (!string.IsNullOrWhiteSpace(a.LatestPrimaryStateCode)
                ? a.LatestPrimaryStateCode
                : a.LatestProgress?.State?.Code)?.Trim())
            .Where(c => !string.IsNullOrEmpty(c))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Cast<string>()
            .ToList();
        if (codes.Count > 0)
        {
            stateByCode = objectSpace.GetObjectsQuery<ApplicationState>()
                .Where(s => s.Code != null && codes.Contains(s.Code))
                .AsEnumerable()
                .Where(s => !string.IsNullOrWhiteSpace(s.Code))
                .GroupBy(s => s.Code!.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
        }

        var labeled = apps.Select(a =>
        {
            var processState = ResolveInvitationProcessResultStateLabel(a, stateByCode);
            string status;
            if (byPeriodCategoryType)
            {
                var period = LookupLabelOrMissing(a.VisaPeriod, "(No period)");
                var category = LookupLabelOrMissing(a.VisaCategory, "(No category)");
                var type = LookupLabelOrMissing(a.VisaType, "(No type)");
                status = $"{period} · {category} · {type} · {processState}";
            }
            else
            {
                var project = ProjectLabel(a.ProjectContract);
                if (string.IsNullOrWhiteSpace(project))
                    project = "(No project)";
                status = $"{project} · {processState}";
            }

            return (App: a, Status: status, ProcessState: processState);
        }).ToList();

        var groups = labeled
            .GroupBy(x => x.Status, StringComparer.Ordinal)
            .Select(g => (Label: g.Key, Count: g.Count()))
            .OrderByDescending(g => g.Count)
            .ToList();
        var buckets = AssignExtensionResultCss(groups);
        var cssByLabel = buckets.ToDictionary(b => b.Label, b => b.CssClass, StringComparer.Ordinal);

        var rows = labeled
            .OrderByDescending(x => x.App.ApplicationDate)
            .Take(PreviewLimit)
            .Select(x =>
            {
                var a = x.App;
                return new ReportDashboardPreviewRow
                {
                    RecordId = a.ID,
                    Name = FirstApplicationPersonName(a),
                    Project = ProjectLabel(a.ProjectContract),
                    ColumnA = a.FullApplicationNumber ?? a.ApplicationNumber ?? string.Empty,
                    ColumnB = FormatDate(a.ApplicationDate),
                    Status = x.Status,
                    StatusCssClass = cssByLabel.TryGetValue(x.Status, out var c) ? c : "st-cat-1"
                };
            })
            .ToList();

        return BuildPanel(
            personType, ReportDashboardCategory.Invitation, subReport, rows,
            excelHint, excelConfigured, buckets, labeled.Count);
    }

    private static string ResolveInvitationProcessResultStateLabel(
        Application app,
        IReadOnlyDictionary<string, ApplicationState> stateByCode)
    {
        var code = (!string.IsNullOrWhiteSpace(app.LatestPrimaryStateCode)
            ? app.LatestPrimaryStateCode
            : app.LatestProgress?.State?.Code)?.Trim();

        string stateLabel;
        if (string.IsNullOrEmpty(code))
        {
            stateLabel = ApplicationProgressPrimaryStateCodeResolver.ResolveDisplayNameFromLatest(null)
                ?? "Being Prepared";
        }
        else if (stateByCode.TryGetValue(code, out var state))
        {
            stateLabel = LookupLocalization.GetDisplayName(state);
        }
        else if (app.LatestProgress?.State != null
                 && string.Equals(app.LatestProgress.State.Code?.Trim(), code, StringComparison.OrdinalIgnoreCase))
        {
            stateLabel = LookupLocalization.GetDisplayName(app.LatestProgress.State);
        }
        else
        {
            var catalog = LookupLocalization.GetCatalogDisplayName("application-state", code);
            stateLabel = !string.IsNullOrEmpty(catalog) ? catalog : code;
        }

        var ministry = ApprovalLegProfileMinistryHelper.GetMinistryShortNameForProgressStep(
            app, code, locationCode: null);
        return ApplicationProgressListLabelHelper.FormatStatusLabel(stateLabel, ministry);
    }

    private static IQueryable<InvitationItem> FilterInvitationItems(
        IObjectSpace objectSpace, PersonRecordRole? role, string projectKey, DateTime cutoff)
    {
        var query = objectSpace.GetObjectsQuery<InvitationItem>()
            .Where(i => i.Person != null
                        && (role == null || i.Person.PersonRole == role)
                        && i.Invitation != null
                        && (i.Invitation.IssuedDate == default || i.Invitation.IssuedDate >= cutoff));

        if (!string.IsNullOrWhiteSpace(projectKey) && projectKey != "All")
        {
            query = query.Where(i =>
                (i.Invitation!.Application != null
                    && i.Invitation.Application.ProjectContract != null
                    && (i.Invitation.Application.ProjectContract.Name == projectKey
                        || i.Invitation.Application.ProjectContract.NameTm == projectKey))
                || (i.Person!.ProjectContract != null
                    && (i.Person.ProjectContract.Name == projectKey
                        || i.Person.ProjectContract.NameTm == projectKey)));
        }

        return query;
    }

    private static string InvitationItemProjectLabel(InvitationItem item)
    {
        var fromApp = ProjectLabel(item.Invitation?.Application?.ProjectContract);
        if (!string.IsNullOrWhiteSpace(fromApp))
            return fromApp;
        return ProjectLabel(item.Person?.ProjectContract);
    }

    private static ReportDashboardPanelData LoadRegistration(
        IObjectSpace objectSpace, PersonRecordRole? role, string projectKey,
        ReportDashboardPersonType personType, string subReport,
        string? excelHint, bool excelConfigured, DateTime cutoff,
        HashSet<Guid>? validVisaPersonIds = null)
    {
        if (ReportDashboardCatalog.IsRegistrationToBeCheckedInSubReport(subReport))
        {
            return LoadToBeCheckedInFromView(
                objectSpace, role, projectKey, personType, subReport, excelHint, excelConfigured, validVisaPersonIds);
        }

        if (ReportDashboardCatalog.IsRegistrationToBeCheckedOutSubReport(subReport))
        {
            return LoadToBeCheckedOutFromView(
                objectSpace, role, projectKey, personType, subReport, excelHint, excelConfigured, validVisaPersonIds);
        }

        if (ReportDashboardCatalog.IsRegistrationOnProcessSubReport(subReport))
        {
            return LoadRegistrationOnProcess(
                objectSpace, role, projectKey, personType, subReport, excelHint, excelConfigured, cutoff, validVisaPersonIds);
        }

        if (string.IsNullOrWhiteSpace(subReport)
            || subReport == "default"
            || ReportDashboardCatalog.IsRegistrationExpiringStateSubReport(subReport)
            || ReportDashboardCatalog.IsRegistrationCheckInPopulationSubReport(subReport))
        {
            return LoadRegistrationFromView(
                objectSpace, role, projectKey, personType, subReport, excelHint, excelConfigured, cutoff, validVisaPersonIds);
        }

        return EmptyPanel(personType, ReportDashboardCategory.Registration, subReport, excelHint, excelConfigured);
    }

    /// <summary>
    /// Registration On process: roster lines on any App_Reg_* type whose Application
    /// is not terminal (same exclude list as Invitation Process). One row per ApplicationPerson (M2M) or legacy ApplicationItem.
    /// Status = ApplicationType · ProcessState (localized; ProcessState matches StatusListLabel).
    /// </summary>
    private static ReportDashboardPanelData LoadRegistrationOnProcess(
        IObjectSpace objectSpace, PersonRecordRole? role, string projectKey,
        ReportDashboardPersonType personType, string subReport,
        string? excelHint, bool excelConfigured, DateTime cutoff,
        HashSet<Guid>? validVisaPersonIds = null)
    {
        subReport = ReportDashboardCatalog.RegistrationOnProcessSubReportKey;
        var regTypes = ReportDashboardCatalog.RegistrationOnProcessApplicationTypeNames;

        if (objectSpace is not EFCoreObjectSpace efOs
            || efOs.DbContext is not Visa2026EFCoreDbContext db)
        {
            return EmptyPanel(personType, ReportDashboardCategory.Registration, subReport, excelHint, excelConfigured);
        }

        try
        {
            var items = ReportDashboardRosterQueryHelper.LoadRegistrationOnProcessLines(
                db, regTypes, cutoff, role, projectKey, validVisaPersonIds)
                .Where(line =>
                {
                    var app = line.Application;
                    var code = !string.IsNullOrWhiteSpace(app.LatestPrimaryStateCode)
                        ? app.LatestPrimaryStateCode
                        : app.LatestProgress?.State?.Code;
                    return !IsInvitationProcessCompleted(code);
                })
                .ToList();

            var codes = items
                .Select(line =>
                {
                    var app = line.Application;
                    var code = !string.IsNullOrWhiteSpace(app.LatestPrimaryStateCode)
                        ? app.LatestPrimaryStateCode
                        : app.LatestProgress?.State?.Code;
                    return code?.Trim();
                })
                .Where(c => !string.IsNullOrEmpty(c))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var stateByCode = codes.Count == 0
                ? new Dictionary<string, ApplicationState>(StringComparer.OrdinalIgnoreCase)
                : db.ApplicationStates.AsNoTracking()
                    .Where(s => s.Code != null && codes.Contains(s.Code))
                    .AsEnumerable()
                    .Where(s => !string.IsNullOrWhiteSpace(s.Code))
                    .GroupBy(s => s.Code!.Trim(), StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            var labeled = items.Select(line =>
            {
                var app = line.Application;
                var appType = LookupLabelOrMissing(app.ApplicationType, "(No type)");
                var processState = ResolveInvitationProcessResultStateLabel(app, stateByCode);
                var status = $"{appType} · {processState}";
                return (Line: line, Status: status, ProcessState: processState);
            }).ToList();

            var groups = labeled
                .GroupBy(x => x.Status, StringComparer.Ordinal)
                .Select(g => (Label: g.Key, Count: g.Count()))
                .OrderByDescending(g => g.Count)
                .ToList();
            var buckets = AssignCategoricalCss(groups);
            var cssByLabel = buckets.ToDictionary(b => b.Label, b => b.CssClass, StringComparer.Ordinal);

            var rows = labeled
                .OrderByDescending(x => x.Line.Application.ApplicationDate)
                .ThenBy(x => x.Line.Person.FullName)
                .Take(PreviewLimit)
                .Select(x =>
                {
                    var app = x.Line.Application;
                    var project = ProjectLabel(app.ProjectContract);
                    if (string.IsNullOrWhiteSpace(project))
                        project = ProjectLabel(x.Line.Person.ProjectContract);
                    var progressCss = StatusCss(x.ProcessState, null);
                    return new ReportDashboardPreviewRow
                    {
                        RecordId = x.Line.RecordId,
                        Name = x.Line.Person.FullName ?? string.Empty,
                        Project = project,
                        ColumnA = app.FullApplicationNumber ?? app.ApplicationNumber ?? string.Empty,
                        ColumnB = FormatDate(app.ApplicationDate),
                        Status = x.Status,
                        StatusCssClass = string.IsNullOrWhiteSpace(progressCss)
                            ? (cssByLabel.TryGetValue(x.Status, out var c) ? c : "st-pending")
                            : progressCss
                    };
                })
                .ToList();

            return BuildPanel(
                personType, ReportDashboardCategory.Registration, subReport, rows,
                excelHint, excelConfigured, buckets, labeled.Count);
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UndefinedTable
            || ex.SqlState == PostgresErrorCodes.UndefinedColumn)
        {
            return EmptyPanel(personType, ReportDashboardCategory.Registration, subReport, excelHint, excelConfigured);
        }
    }

    /// <summary>
    /// <c>vw_rd_to_be_checked_in</c>: valid visas with no registration CurrentVisa link;
    /// chart = days since latest ExternalArrival (one last visa per person).
    /// </summary>
    private static ReportDashboardPanelData LoadToBeCheckedInFromView(
        IObjectSpace objectSpace, PersonRecordRole? role, string projectKey,
        ReportDashboardPersonType personType, string subReport,
        string? excelHint, bool excelConfigured,
        HashSet<Guid>? validVisaPersonIds = null)
    {
        if (objectSpace is not EFCoreObjectSpace efOs
            || efOs.DbContext is not Visa2026EFCoreDbContext db)
        {
            return EmptyPanel(personType, ReportDashboardCategory.Registration, subReport, excelHint, excelConfigured);
        }

        IQueryable<VwRdToBeCheckedIn> query = db.VwRdToBeCheckedIn.AsNoTracking();

        if (role.HasValue)
            query = query.Where(r => r.PersonRoleCode == (int)role.Value);

        if (validVisaPersonIds != null)
            query = query.Where(r => r.PersonOid != null && validVisaPersonIds.Contains(r.PersonOid.Value));

        if (!string.IsNullOrWhiteSpace(projectKey) && projectKey != "All")
        {
            query = query.Where(r =>
                r.ProjectNameTm == projectKey
                || r.ProjectName == projectKey
                || r.ProjectNameRaw == projectKey);
        }

        try
        {
            var list = TakeOneLastValidVisaPerPerson(
                query.ToList(), r => r.PersonOid, r => r.VisaExpirationDate, r => r.ID);

            var countsByLabel = list
                .GroupBy(r => r.EntryBucketLabel ?? string.Empty, StringComparer.Ordinal)
                .ToDictionary(
                    g => g.Key,
                    g => (Count: g.Count(), Css: g.First().EntryBucketCssClass ?? "st-pending"),
                    StringComparer.Ordinal);

            var buckets = ReportDashboardCatalog.RegistrationToBeCheckedInBuckets
                .Select(b =>
                {
                    var has = countsByLabel.TryGetValue(b.Label, out var hit);
                    return new ReportDashboardStatusBucket
                    {
                        Label = b.Label,
                        CssClass = has ? hit.Css : b.CssClass,
                        Count = has ? hit.Count : 0
                    };
                })
                .OrderBy(b => ReportDashboardCatalog.RegistrationToBeCheckedInBucketSortKey(b.Label))
                .ToList();

            var rows = list
                .OrderBy(r => r.DaysSinceEntry ?? int.MaxValue)
                .ThenBy(r => r.PersonName)
                .Take(PreviewLimit)
                .Select(r => new ReportDashboardPreviewRow
                {
                    RecordId = r.ID,
                    Name = r.PersonName ?? string.Empty,
                    Project = r.ProjectName ?? string.Empty,
                    ColumnA = r.VisaNumber ?? string.Empty,
                    ColumnB = FormatDate(r.EntryDate),
                    Status = r.EntryBucketLabel ?? string.Empty,
                    StatusCssClass = r.EntryBucketCssClass ?? "st-pending"
                })
                .ToList();

            return BuildPanel(
                personType, ReportDashboardCategory.Registration, subReport, rows,
                excelHint, excelConfigured, buckets, list.Count);
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UndefinedTable
            || ex.SqlState == PostgresErrorCodes.UndefinedColumn)
        {
            return EmptyPanel(personType, ReportDashboardCategory.Registration, subReport, excelHint, excelConfigured);
        }
    }

    /// <summary>
    /// <c>vw_rd_to_be_checked_out</c>: valid visas expiring within 1 week without Check-Out app;
    /// chart = day buckets to expiry (one last visa per person).
    /// </summary>
    private static ReportDashboardPanelData LoadToBeCheckedOutFromView(
        IObjectSpace objectSpace, PersonRecordRole? role, string projectKey,
        ReportDashboardPersonType personType, string subReport,
        string? excelHint, bool excelConfigured,
        HashSet<Guid>? validVisaPersonIds = null)
    {
        if (objectSpace is not EFCoreObjectSpace efOs
            || efOs.DbContext is not Visa2026EFCoreDbContext db)
        {
            return EmptyPanel(personType, ReportDashboardCategory.Registration, subReport, excelHint, excelConfigured);
        }

        IQueryable<VwRdToBeCheckedOut> query = db.VwRdToBeCheckedOut.AsNoTracking();

        if (role.HasValue)
            query = query.Where(r => r.PersonRoleCode == (int)role.Value);

        if (validVisaPersonIds != null)
            query = query.Where(r => r.PersonOid != null && validVisaPersonIds.Contains(r.PersonOid.Value));

        if (!string.IsNullOrWhiteSpace(projectKey) && projectKey != "All")
        {
            query = query.Where(r =>
                r.ProjectNameTm == projectKey
                || r.ProjectName == projectKey
                || r.ProjectNameRaw == projectKey);
        }

        try
        {
            var list = TakeOneLastValidVisaPerPerson(
                query.ToList(), r => r.PersonOid, r => r.VisaExpirationDate, r => r.ID);

            var countsByLabel = list
                .GroupBy(r => r.ExpiryBucketLabel ?? string.Empty, StringComparer.Ordinal)
                .ToDictionary(
                    g => g.Key,
                    g => (Count: g.Count(), Css: g.First().ExpiryBucketCssClass ?? "st-pending"),
                    StringComparer.Ordinal);

            var buckets = ReportDashboardCatalog.RegistrationToBeCheckedOutBuckets
                .Select(b =>
                {
                    var has = countsByLabel.TryGetValue(b.Label, out var hit);
                    return new ReportDashboardStatusBucket
                    {
                        Label = b.Label,
                        CssClass = has ? hit.Css : b.CssClass,
                        Count = has ? hit.Count : 0
                    };
                })
                .OrderBy(b => ReportDashboardCatalog.RegistrationToBeCheckedOutBucketSortKey(b.Label))
                .ToList();

            var rows = list
                .OrderBy(r => r.DaysRemaining)
                .ThenBy(r => r.PersonName)
                .Take(PreviewLimit)
                .Select(r => new ReportDashboardPreviewRow
                {
                    RecordId = r.ID,
                    Name = r.PersonName ?? string.Empty,
                    Project = r.ProjectName ?? string.Empty,
                    ColumnA = r.VisaNumber ?? string.Empty,
                    ColumnB = FormatDate(r.VisaExpirationDate),
                    Status = r.ExpiryBucketLabel ?? string.Empty,
                    StatusCssClass = r.ExpiryBucketCssClass ?? "st-pending"
                })
                .ToList();

            return BuildPanel(
                personType, ReportDashboardCategory.Registration, subReport, rows,
                excelHint, excelConfigured, buckets, list.Count);
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UndefinedTable
            || ex.SqlState == PostgresErrorCodes.UndefinedColumn)
        {
            return EmptyPanel(personType, ReportDashboardCategory.Registration, subReport, excelHint, excelConfigured);
        }
    }

    /// <summary>
    /// <c>vw_rd_registration</c>: type tabs filter by ApplicationTypeName (Status = process state);
    /// Expiring State = active registration types (no Check-Out), one last visa per person (Status = expiry bucket).
    /// </summary>
    private static ReportDashboardPanelData LoadRegistrationFromView(
        IObjectSpace objectSpace, PersonRecordRole? role, string projectKey,
        ReportDashboardPersonType personType, string subReport,
        string? excelHint, bool excelConfigured, DateTime cutoff,
        HashSet<Guid>? validVisaPersonIds = null)
    {
        if (objectSpace is not EFCoreObjectSpace efOs
            || efOs.DbContext is not Visa2026EFCoreDbContext db)
        {
            return EmptyPanel(personType, ReportDashboardCategory.Registration, subReport, excelHint, excelConfigured);
        }

        if (string.IsNullOrWhiteSpace(subReport) || subReport == "default")
            subReport = ReportDashboardCatalog.DefaultSubReport(ReportDashboardCategory.Registration);

        var useExpiryBuckets = ReportDashboardCatalog.IsRegistrationExpiringStateSubReport(subReport);
        var useCheckInPopulation = ReportDashboardCatalog.IsRegistrationCheckInPopulationSubReport(subReport);
        var useCityBuckets = ReportDashboardCatalog.IsRegistrationCheckInByCitySubReport(subReport);
        var usePeriodCategoryTypeBuckets =
            ReportDashboardCatalog.IsRegistrationCheckInByPeriodCategoryTypeSubReport(subReport);

        IQueryable<VwRdRegistration> query = db.VwRdRegistration.AsNoTracking();

        if (useExpiryBuckets || useCheckInPopulation)
        {
            var types = ReportDashboardCatalog.RegistrationExpiringStateApplicationTypeNames;
            query = query.Where(r => r.ApplicationTypeName != null && types.Contains(r.ApplicationTypeName));
        }
        else
        {
            query = query.Where(r => r.ApplicationTypeName == subReport);
        }

        if (role.HasValue)
            query = query.Where(r => r.PersonRoleCode == (int)role.Value);

        if (validVisaPersonIds != null)
            query = query.Where(r => r.PersonOid != null && validVisaPersonIds.Contains(r.PersonOid.Value));

        if (!string.IsNullOrWhiteSpace(projectKey) && projectKey != "All")
        {
            query = query.Where(r =>
                r.ProjectNameTm == projectKey
                || r.ProjectName == projectKey
                || r.ProjectNameRaw == projectKey);
        }

        if (cutoff > DateTime.MinValue.AddYears(1))
            query = query.Where(r => r.ApplicationDate != null && r.ApplicationDate >= cutoff);

        try
        {
            var list = query.ToList();

            if (useCheckInPopulation)
            {
                list = TakeOneLastValidVisaPerPerson(
                    list, r => r.PersonOid, r => r.VisaExpirationDate, r => r.ID);

                static string CatCss(int index) => (index % 5) switch
                {
                    0 => "st-cat-1",
                    1 => "st-cat-2",
                    2 => "st-cat-3",
                    3 => "st-cat-4",
                    _ => "st-cat-5"
                };

                // Active Registered (V): Period from registration Application (Visa has no VisaPeriod);
                // Category + Type from linked CurrentVisa (Application.VisaType is often default WP).
                Dictionary<Guid, string> pctByItemId = [];
                if (usePeriodCategoryTypeBuckets && list.Count > 0)
                {
                    var itemIds = list.Select(r => r.ID).ToList();
                    pctByItemId = db.ApplicationItems.AsNoTracking()
                        .Where(ai => itemIds.Contains(ai.ID))
                        .Include(ai => ai.Application!).ThenInclude(a => a.VisaPeriod)
                        .Include(ai => ai.CurrentVisa!).ThenInclude(v => v.VisaCategory)
                        .Include(ai => ai.CurrentVisa!).ThenInclude(v => v.VisaType)
                        .AsEnumerable()
                        .ToDictionary(
                            ai => ai.ID,
                            ai =>
                            {
                                var period = LookupLabelOrMissing(ai.Application?.VisaPeriod, "(No period)");
                                var category = LookupLabelOrMissing(ai.CurrentVisa?.VisaCategory, "(No category)");
                                var type = LookupLabelOrMissing(ai.CurrentVisa?.VisaType, "(No type)");
                                return $"{period} · {category} · {type}";
                            });
                }

                string StatusOf(VwRdRegistration r)
                {
                    if (useCityBuckets)
                        return string.IsNullOrWhiteSpace(r.CityLabel) ? "Unknown city" : r.CityLabel.Trim();
                    if (usePeriodCategoryTypeBuckets)
                        return pctByItemId.TryGetValue(r.ID, out var pct)
                            ? pct
                            : "(No period) · (No category) · (No type)";
                    // check-in-by-project
                    return string.IsNullOrWhiteSpace(r.ProjectName) ? "(No project)" : r.ProjectName.Trim();
                }

                var labeled = list.Select(r => (Row: r, Status: StatusOf(r))).ToList();

                var groups = labeled
                    .GroupBy(x => x.Status, StringComparer.OrdinalIgnoreCase)
                    .OrderByDescending(g => g.Count())
                    .ThenBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                var buckets = groups
                    .Select((g, i) => new ReportDashboardStatusBucket
                    {
                        Label = g.Key,
                        CssClass = CatCss(i),
                        Count = g.Count()
                    })
                    .ToList();

                var cssByStatus = buckets.ToDictionary(b => b.Label, b => b.CssClass, StringComparer.OrdinalIgnoreCase);

                var rows = labeled
                    .OrderBy(x => x.Status, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(x => x.Row.PersonName)
                    .Take(PreviewLimit)
                    .Select(x => new ReportDashboardPreviewRow
                    {
                        RecordId = x.Row.ID,
                        Name = x.Row.PersonName ?? string.Empty,
                        Project = x.Row.ProjectName ?? string.Empty,
                        ColumnA = x.Row.VisaNumber ?? string.Empty,
                        ColumnB = FormatDate(x.Row.VisaExpirationDate),
                        Status = x.Status,
                        StatusCssClass = cssByStatus.TryGetValue(x.Status, out var css) ? css : "st-pending"
                    })
                    .ToList();

                return BuildPanel(
                    personType, ReportDashboardCategory.Registration, subReport, rows,
                    excelHint, excelConfigured, buckets, labeled.Count);
            }

            if (useExpiryBuckets)
            {
                list = TakeOneLastValidVisaPerPerson(
                    list, r => r.PersonOid, r => r.VisaExpirationDate, r => r.ID);

                var countsByLabel = list
                    .GroupBy(r => r.ExpiryBucketLabel ?? string.Empty, StringComparer.Ordinal)
                    .ToDictionary(
                        g => g.Key,
                        g => (Count: g.Count(), Css: g.First().ExpiryBucketCssClass ?? "st-pending"),
                        StringComparer.Ordinal);

                // Always include day + week buckets even when count is 0.
                var buckets = ReportDashboardCatalog.RegistrationExpiringStateBuckets
                    .Select(b =>
                    {
                        var has = countsByLabel.TryGetValue(b.Label, out var hit);
                        return new ReportDashboardStatusBucket
                        {
                            Label = b.Label,
                            CssClass = has ? hit.Css : b.CssClass,
                            Count = has ? hit.Count : 0
                        };
                    })
                    .OrderBy(b => ReportDashboardCatalog.RegistrationExpiringStateBucketSortKey(b.Label))
                    .ToList();

                var totalCount = list.Count;

                var rows = list
                    .OrderBy(r => r.DaysRemaining)
                    .ThenBy(r => r.PersonName)
                    .Take(PreviewLimit)
                    .Select(r => new ReportDashboardPreviewRow
                    {
                        RecordId = r.ID,
                        Name = r.PersonName ?? string.Empty,
                        Project = r.ProjectName ?? string.Empty,
                        ColumnA = r.VisaNumber ?? string.Empty,
                        ColumnB = FormatDate(r.VisaExpirationDate),
                        Status = r.ExpiryBucketLabel ?? string.Empty,
                        StatusCssClass = r.ExpiryBucketCssClass ?? "st-pending"
                    })
                    .ToList();

                return BuildPanel(
                    personType, ReportDashboardCategory.Registration, subReport, rows,
                    excelHint, excelConfigured, buckets, totalCount);
            }

            var processBuckets = list
                .GroupBy(r => r.ProgressStateLabel ?? "OFISDE", StringComparer.OrdinalIgnoreCase)
                .Select(g => new ReportDashboardStatusBucket
                {
                    Label = g.Key,
                    CssClass = g.First().ProgressStateCssClass ?? "st-pending",
                    Count = g.Count()
                })
                .OrderByDescending(b => b.Count)
                .ThenBy(b => b.Label)
                .ToList();

            var processTotal = processBuckets.Sum(b => b.Count);

            var processRows = list
                .OrderByDescending(r => r.ApplicationDate)
                .ThenBy(r => r.PersonName)
                .Take(PreviewLimit)
                .Select(r => new ReportDashboardPreviewRow
                {
                    RecordId = r.ID,
                    Name = r.PersonName ?? string.Empty,
                    Project = r.ProjectName ?? string.Empty,
                    ColumnA = r.VisaNumber ?? string.Empty,
                    ColumnB = FormatDate(r.VisaExpirationDate),
                    Status = r.ProgressStateLabel ?? "OFISDE",
                    StatusCssClass = r.ProgressStateCssClass ?? "st-pending"
                })
                .ToList();

            return BuildPanel(
                personType, ReportDashboardCategory.Registration, subReport, processRows,
                excelHint, excelConfigured, processBuckets, processTotal);
        }
        catch (PostgresException ex) when (ex.SqlState is PostgresErrorCodes.UndefinedTable or PostgresErrorCodes.UndefinedColumn)
        {
            return EmptyPanel(personType, ReportDashboardCategory.Registration, subReport, excelHint, excelConfigured);
        }
    }
    private static ReportDashboardPanelData LoadWorkPermit(
        IObjectSpace objectSpace, PersonRecordRole? role, string projectKey,
        ReportDashboardPersonType personType, string subReport,
        string? excelHint, bool excelConfigured, DateTime cutoff,
        bool includeArchivedPersons,
        bool oneLastValidWorkPermitPerPerson = false,
        HashSet<Guid>? validVisaPersonIds = null)
    {
        // View-backed: active-by-project (vw_rd_work_permit_active) + by-days-remaining (vw_rd_work_permit).
        // by-status stays on legacy until promoted.
        if (subReport is "active-by-project"
            || subReport == "default"
            || string.IsNullOrWhiteSpace(subReport))
        {
            return LoadWorkPermitActiveByProjectFromView(
                objectSpace, role, projectKey, personType, "active-by-project", excelHint, excelConfigured, cutoff,
                includeArchivedPersons, oneLastValidWorkPermitPerPerson, validVisaPersonIds);
        }

        if (subReport is "by-days-remaining" or "by-validity")
        {
            return LoadWorkPermitFromView(
                objectSpace, role, projectKey, personType, subReport, excelHint, excelConfigured, cutoff,
                includeArchivedPersons, oneLastValidWorkPermitPerPerson, validVisaPersonIds);
        }

        if (subReport is "extension-result")
        {
            return LoadWorkPermitExtensionResultFromView(
                objectSpace, role, projectKey, personType, subReport, excelHint, excelConfigured, cutoff,
                includeArchivedPersons, validVisaPersonIds);
        }

        return LoadWorkPermitLegacy(objectSpace, role, projectKey, personType, subReport, excelHint, excelConfigured, cutoff, includeArchivedPersons, validVisaPersonIds);
    }

    /// <summary>
    /// WorkPermit Extension Result (P): <c>vw_rd_work_permit_app_progress</c> rows with terminal /
    /// review-reject progress; Status = Project · ProcessState.
    /// </summary>
    private static ReportDashboardPanelData LoadWorkPermitExtensionResultFromView(
        IObjectSpace objectSpace, PersonRecordRole? role, string projectKey,
        ReportDashboardPersonType personType, string subReport,
        string? excelHint, bool excelConfigured, DateTime cutoff,
        bool includeArchivedPersons,
        HashSet<Guid>? validVisaPersonIds = null)
    {
        if (objectSpace is not EFCoreObjectSpace efOs
            || efOs.DbContext is not Visa2026EFCoreDbContext db)
        {
            return LoadWorkPermitExtensionResultLegacy(
                objectSpace, role, projectKey, personType, subReport, excelHint, excelConfigured, cutoff,
                includeArchivedPersons, validVisaPersonIds);
        }

        var resultCodes = ReportDashboardCatalog.WorkPermitExtensionResultStateCodes;
        IQueryable<VwRdWorkPermitAppProgress> query = db.VwRdWorkPermitAppProgress
            .AsNoTracking()
            .Where(r => r.ApplicationDate == null || r.ApplicationDate >= cutoff);

        if (!includeArchivedPersons)
            query = query.Where(r => !r.IsArchived);

        if (role.HasValue)
            query = query.Where(r => r.PersonRoleCode == (int)role.Value);

        if (validVisaPersonIds != null)
            query = query.Where(r => r.PersonOid != null && validVisaPersonIds.Contains(r.PersonOid.Value));

        if (!string.IsNullOrWhiteSpace(projectKey) && projectKey != "All")
        {
            query = query.Where(r =>
                r.ProjectNameTm == projectKey
                || r.ProjectName == projectKey
                || r.ProjectNameRaw == projectKey);
        }

        try
        {
            var list = query.ToList()
                .Where(r => IsWorkPermitExtensionResultState(r.ProgressStateCode, resultCodes))
                .ToList();

            var statusByRow = ResolveWorkPermitAppProgressStatusByProject(db, list);
            var groups = list
                .GroupBy(r => statusByRow[r.ID], StringComparer.Ordinal)
                .Select(g => (Label: g.Key, Count: g.Count()))
                .OrderByDescending(g => g.Count)
                .ToList();
            var buckets = AssignExtensionResultCss(groups);
            var cssByLabel = buckets.ToDictionary(b => b.Label, b => b.CssClass, StringComparer.Ordinal);
            var totalCount = buckets.Sum(b => b.Count);

            var rows = list
                .OrderByDescending(r => r.ApplicationDate)
                .Take(PreviewLimit)
                .Select(r =>
                {
                    var status = statusByRow[r.ID];
                    return new ReportDashboardPreviewRow
                    {
                        RecordId = r.ID,
                        Name = r.PersonName ?? string.Empty,
                        Project = r.ProjectName ?? string.Empty,
                        ColumnA = r.ApplicationNumber ?? string.Empty,
                        ColumnB = FormatDate(r.ApplicationDate),
                        Status = status,
                        StatusCssClass = cssByLabel.TryGetValue(status, out var c) ? c : "st-expiring"
                    };
                })
                .ToList();

            return BuildPanel(
                personType, ReportDashboardCategory.WorkPermit, subReport, rows,
                excelHint, excelConfigured, buckets, totalCount);
        }
        catch (Exception ex) when (IsMissingReportDashboardView(ex))
        {
            return LoadWorkPermitExtensionResultLegacy(
                objectSpace, role, projectKey, personType, subReport, excelHint, excelConfigured, cutoff,
                includeArchivedPersons, validVisaPersonIds);
        }
    }

    private static bool IsWorkPermitExtensionResultState(
        string? progressStateCode,
        IReadOnlyList<string> resultCodes)
    {
        var code = progressStateCode?.Trim();
        if (string.IsNullOrEmpty(code))
            return false;
        foreach (var allowed in resultCodes)
        {
            if (string.Equals(code, allowed, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    private static Dictionary<Guid, string> ResolveWorkPermitAppProgressStatusByProject(
        Visa2026EFCoreDbContext db,
        List<VwRdWorkPermitAppProgress> rows)
    {
        var result = new Dictionary<Guid, string>(rows.Count);
        var codes = rows
            .Select(r => r.ProgressStateCode?.Trim())
            .Where(c => !string.IsNullOrEmpty(c))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var stateByCode = db.ApplicationStates.AsNoTracking()
            .Where(s => s.Code != null && codes.Contains(s.Code))
            .AsEnumerable()
            .Where(s => !string.IsNullOrWhiteSpace(s.Code))
            .GroupBy(s => s.Code!.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        var appIds = rows
            .Where(r => r.ApplicationOid.HasValue)
            .Select(r => r.ApplicationOid!.Value)
            .Distinct()
            .ToList();

        Dictionary<Guid, Application> appsById = [];
        if (appIds.Count > 0)
        {
            appsById = db.Applications.AsNoTracking()
                .Where(a => appIds.Contains(a.ID))
                .Include(a => a.ApprovalLegSnapshots)
                .Include(a => a.ApprovalLegProfile!)
                    .ThenInclude(p => p.MinistryLegs!)
                    .ThenInclude(l => l.ApprovingMinistry)
                .ToList()
                .ToDictionary(a => a.ID);
        }

        foreach (var row in rows)
        {
            appsById.TryGetValue(row.ApplicationOid ?? Guid.Empty, out var app);
            var processState = ResolveWorkPermitAppProgressProcessStateLabel(row, app, stateByCode);
            var project = string.IsNullOrWhiteSpace(row.ProjectName)
                ? "(No project)"
                : row.ProjectName.Trim();
            result[row.ID] = $"{project} · {processState}";
        }

        return result;
    }

    private static string ResolveWorkPermitAppProgressProcessStateLabel(
        VwRdWorkPermitAppProgress row,
        Application? app,
        IReadOnlyDictionary<string, ApplicationState> stateByCode)
    {
        var code = row.ProgressStateCode?.Trim();
        string stateLabel;
        if (string.IsNullOrEmpty(code))
        {
            stateLabel = ApplicationProgressPrimaryStateCodeResolver.ResolveDisplayNameFromLatest(null)
                ?? "Being Prepared";
        }
        else if (stateByCode.TryGetValue(code, out var state))
        {
            stateLabel = LookupLocalization.GetDisplayName(state);
        }
        else
        {
            var catalog = LookupLocalization.GetCatalogDisplayName("application-state", code);
            stateLabel = !string.IsNullOrEmpty(catalog)
                ? catalog
                : (!string.IsNullOrWhiteSpace(row.ProgressStateLabel)
                    ? row.ProgressStateLabel.Trim()
                    : code);
        }

        string? ministry = null;
        if (app != null)
        {
            ministry = ApprovalLegProfileMinistryHelper.GetMinistryShortNameForProgressStep(
                app, code, locationCode: null);
        }

        return ApplicationProgressListLabelHelper.FormatStatusLabel(stateLabel, ministry);
    }

    private static ReportDashboardPanelData LoadWorkPermitExtensionResultLegacy(
        IObjectSpace objectSpace, PersonRecordRole? role, string projectKey,
        ReportDashboardPersonType personType, string subReport,
        string? excelHint, bool excelConfigured, DateTime cutoff,
        bool includeArchivedPersons,
        HashSet<Guid>? validVisaPersonIds)
    {
        var typeNames = ReportDashboardCatalog.WorkPermitExtensionApplicationTypeNames;
        var resultCodes = ReportDashboardCatalog.WorkPermitExtensionResultStateCodes;
        var query = objectSpace.GetObjectsQuery<ApplicationItem>()
            .Where(ai => ai.Person != null
                && ai.CurrentWorkPermitItem != null
                && ai.Application != null
                && ai.Application.ApplicationType != null
                && typeNames.Contains(ai.Application.ApplicationType.Name)
                && (ai.Application.ApplicationDate == default
                    || ai.Application.ApplicationDate >= cutoff)
                && (role == null || ai.Person.PersonRole == role));

        if (!includeArchivedPersons)
            query = query.Where(ai => !ai.Person!.IsArchived);

        if (validVisaPersonIds != null)
            query = query.Where(ai => validVisaPersonIds.Contains(ai.Person!.ID));

        if (!string.IsNullOrWhiteSpace(projectKey) && projectKey != "All")
        {
            query = query.Where(ai =>
                ai.Application!.ProjectContract != null
                && (ai.Application.ProjectContract.Name == projectKey
                    || ai.Application.ProjectContract.NameTm == projectKey));
        }

        var items = query.AsEnumerable().ToList();
        var labeled = new List<(ApplicationItem Item, string Status, string Project)>();
        foreach (var ai in items)
        {
            var app = ai.Application!;
            var latest = app.ProgressHistory?
                .OrderByDescending(p => p.Date)
                .ThenByDescending(p => p.ID)
                .FirstOrDefault();
            var code = latest?.State?.Code ?? app.LatestPrimaryStateCode;
            if (!IsWorkPermitExtensionResultState(code, resultCodes))
                continue;

            var stateLabel = latest != null
                ? (LookupLocalization.GetDisplayName(latest.State) ?? latest.State?.Name ?? code ?? "Unknown")
                : (code ?? "Unknown");
            var ministry = ApprovalLegProfileMinistryHelper.GetMinistryShortNameForProgressStep(
                app, code, locationCode: null);
            var processState = ApplicationProgressListLabelHelper.FormatStatusLabel(stateLabel, ministry);
            var project = ProjectLabel(app.ProjectContract);
            if (string.IsNullOrWhiteSpace(project))
                project = ProjectLabel(ai.Person?.ProjectContract);
            if (string.IsNullOrWhiteSpace(project))
                project = "(No project)";
            labeled.Add((ai, $"{project} · {processState}", project));
        }

        var groups = labeled
            .GroupBy(x => x.Status, StringComparer.Ordinal)
            .Select(g => (Label: g.Key, Count: g.Count()))
            .OrderByDescending(g => g.Count)
            .ToList();
        var buckets = AssignExtensionResultCss(groups);
        var cssByLabel = buckets.ToDictionary(b => b.Label, b => b.CssClass, StringComparer.Ordinal);

        var rows = labeled
            .OrderByDescending(x => x.Item.Application?.ApplicationDate)
            .Take(PreviewLimit)
            .Select(x => new ReportDashboardPreviewRow
            {
                RecordId = x.Item.ID,
                Name = x.Item.Person?.FullName ?? string.Empty,
                Project = x.Project,
                ColumnA = x.Item.Application?.FullApplicationNumber
                    ?? x.Item.Application?.ApplicationNumber
                    ?? string.Empty,
                ColumnB = FormatDate(x.Item.Application?.ApplicationDate == default
                    ? null
                    : x.Item.Application.ApplicationDate),
                Status = x.Status,
                StatusCssClass = cssByLabel.TryGetValue(x.Status, out var c) ? c : "st-expiring"
            })
            .ToList();

        return BuildPanel(
            personType, ReportDashboardCategory.WorkPermit, subReport, rows,
            excelHint, excelConfigured, buckets, labeled.Count);
    }

    /// <summary>
    /// Active WorkPermit (P): valid items from <c>vw_rd_work_permit_active</c>; Status = Project.
    /// Same population as WorkPermit Validity; optional one last per person.
    /// Falls back to <c>vw_rd_work_permit</c> (Project status) then EF legacy if the active view is missing.
    /// </summary>
    private static ReportDashboardPanelData LoadWorkPermitActiveByProjectFromView(
        IObjectSpace objectSpace, PersonRecordRole? role, string projectKey,
        ReportDashboardPersonType personType, string subReport,
        string? excelHint, bool excelConfigured, DateTime cutoff,
        bool includeArchivedPersons,
        bool oneLastValidWorkPermitPerPerson = false,
        HashSet<Guid>? validVisaPersonIds = null)
    {
        if (objectSpace is not EFCoreObjectSpace efOs
            || efOs.DbContext is not Visa2026EFCoreDbContext db)
        {
            return LoadWorkPermitActiveByProjectLegacy(
                objectSpace, role, projectKey, personType, subReport, excelHint, excelConfigured,
                includeArchivedPersons, oneLastValidWorkPermitPerPerson, validVisaPersonIds);
        }

        _ = cutoff;
        try
        {
            IQueryable<VwRdWorkPermitActive> query = db.VwRdWorkPermitActive
                .AsNoTracking();
            if (role.HasValue)
                query = query.Where(r => r.PersonRoleCode == (int)role.Value);

            if (!includeArchivedPersons)
                query = query.Where(r => !r.IsArchived);

            if (validVisaPersonIds != null)
                query = query.Where(r => r.PersonOid != null && validVisaPersonIds.Contains(r.PersonOid.Value));

            if (!string.IsNullOrWhiteSpace(projectKey) && projectKey != "All")
            {
                query = query.Where(r =>
                    r.ProjectNameTm == projectKey
                    || r.ProjectName == projectKey
                    || r.ProjectNameRaw == projectKey
                    || r.StatusLabel == projectKey);
            }

            var list = query.ToList();
            if (oneLastValidWorkPermitPerPerson)
                list = TakeOneLastValidVisaPerPerson(list, r => r.PersonOid, r => r.ExpirationDate, r => r.ID);

            var labeled = list.Select(r =>
            {
                var status = string.IsNullOrWhiteSpace(r.StatusLabel)
                    ? "(No project)"
                    : r.StatusLabel.Trim();
                return (Row: r, Status: status);
            }).ToList();

            return BuildWorkPermitActiveByProjectPanel(
                personType, subReport, excelHint, excelConfigured, labeled,
                r => r.ID, r => r.PersonName, r => r.ProjectName, r => r.WorkPermitNumber, r => r.ExpirationDate);
        }
        catch (Exception ex) when (IsMissingReportDashboardView(ex))
        {
            return LoadWorkPermitActiveByProjectFromValidityView(
                objectSpace, role, projectKey, personType, subReport, excelHint, excelConfigured, cutoff,
                includeArchivedPersons, oneLastValidWorkPermitPerPerson, validVisaPersonIds);
        }
    }

    /// <summary>
    /// Active WorkPermit (P) fallback: same rows as Validity view, chart Status = Project.
    /// </summary>
    private static ReportDashboardPanelData LoadWorkPermitActiveByProjectFromValidityView(
        IObjectSpace objectSpace, PersonRecordRole? role, string projectKey,
        ReportDashboardPersonType personType, string subReport,
        string? excelHint, bool excelConfigured, DateTime cutoff,
        bool includeArchivedPersons,
        bool oneLastValidWorkPermitPerPerson = false,
        HashSet<Guid>? validVisaPersonIds = null)
    {
        if (objectSpace is not EFCoreObjectSpace efOs
            || efOs.DbContext is not Visa2026EFCoreDbContext db)
        {
            return LoadWorkPermitActiveByProjectLegacy(
                objectSpace, role, projectKey, personType, subReport, excelHint, excelConfigured,
                includeArchivedPersons, oneLastValidWorkPermitPerPerson, validVisaPersonIds);
        }

        _ = cutoff;
        try
        {
            IQueryable<VwRdWorkPermit> query = db.VwRdWorkPermit.AsNoTracking();
            if (role.HasValue)
                query = query.Where(r => r.PersonRoleCode == (int)role.Value);

            if (!includeArchivedPersons)
                query = query.Where(r => !r.IsArchived);

            if (validVisaPersonIds != null)
                query = query.Where(r => r.PersonOid != null && validVisaPersonIds.Contains(r.PersonOid.Value));

            if (!string.IsNullOrWhiteSpace(projectKey) && projectKey != "All")
            {
                query = query.Where(r =>
                    r.ProjectNameTm == projectKey
                    || r.ProjectName == projectKey
                    || r.ProjectNameRaw == projectKey);
            }

            var list = query.ToList();
            if (oneLastValidWorkPermitPerPerson)
                list = TakeOneLastValidVisaPerPerson(list, r => r.PersonOid, r => r.ExpirationDate, r => r.ID);

            var labeled = list.Select(r =>
            {
                var status = string.IsNullOrWhiteSpace(r.ProjectName)
                    ? "(No project)"
                    : r.ProjectName.Trim();
                return (Row: r, Status: status);
            }).ToList();

            return BuildWorkPermitActiveByProjectPanel(
                personType, subReport, excelHint, excelConfigured, labeled,
                r => r.ID, r => r.PersonName, r => r.ProjectName, r => r.WorkPermitNumber, r => r.ExpirationDate);
        }
        catch (Exception ex) when (IsMissingReportDashboardView(ex))
        {
            return LoadWorkPermitActiveByProjectLegacy(
                objectSpace, role, projectKey, personType, subReport, excelHint, excelConfigured,
                includeArchivedPersons, oneLastValidWorkPermitPerPerson, validVisaPersonIds);
        }
    }

    private static ReportDashboardPanelData BuildWorkPermitActiveByProjectPanel<T>(
        ReportDashboardPersonType personType,
        string subReport,
        string? excelHint,
        bool excelConfigured,
        List<(T Row, string Status)> labeled,
        Func<T, Guid> id,
        Func<T, string?> personName,
        Func<T, string?> projectName,
        Func<T, string?> workPermitNumber,
        Func<T, DateTime?> expirationDate)
    {
        var groups = labeled
            .GroupBy(x => x.Status, StringComparer.Ordinal)
            .Select(g => (Label: g.Key, Count: g.Count()))
            .OrderByDescending(g => g.Count)
            .ToList();
        var buckets = AssignCategoricalCss(groups);
        var cssByLabel = buckets.ToDictionary(b => b.Label, b => b.CssClass, StringComparer.Ordinal);
        var totalCount = buckets.Sum(b => b.Count);

        var rows = labeled
            .OrderBy(x => x.Status)
            .ThenBy(x => expirationDate(x.Row))
            .ThenBy(x => personName(x.Row))
            .Take(PreviewLimit)
            .Select(x => new ReportDashboardPreviewRow
            {
                RecordId = id(x.Row),
                Name = personName(x.Row) ?? string.Empty,
                Project = projectName(x.Row) ?? string.Empty,
                ColumnA = workPermitNumber(x.Row) ?? string.Empty,
                ColumnB = FormatDate(expirationDate(x.Row)),
                Status = x.Status,
                StatusCssClass = cssByLabel.TryGetValue(x.Status, out var c) ? c : "st-cat-1"
            })
            .ToList();

        return BuildPanel(
            personType, ReportDashboardCategory.WorkPermit, subReport, rows,
            excelHint, excelConfigured, buckets, totalCount);
    }

    /// <summary>
    /// Active WorkPermit (P) EF fallback: valid items; Status = Project.
    /// </summary>
    private static ReportDashboardPanelData LoadWorkPermitActiveByProjectLegacy(
        IObjectSpace objectSpace, PersonRecordRole? role, string projectKey,
        ReportDashboardPersonType personType, string subReport,
        string? excelHint, bool excelConfigured,
        bool includeArchivedPersons,
        bool oneLastValidWorkPermitPerPerson,
        HashSet<Guid>? validVisaPersonIds)
    {
        var today = DateTime.Today;
        var query = objectSpace.GetObjectsQuery<WorkPermitItem>()
            .Where(w => w.Person != null
                && (role == null || w.Person.PersonRole == role)
                && !w.IsCancelled
                && w.ExpirationDate != default
                && w.ExpirationDate.Date >= today);

        if (!includeArchivedPersons)
            query = query.Where(w => !w.Person!.IsArchived);

        if (validVisaPersonIds != null)
            query = query.Where(w => validVisaPersonIds.Contains(w.Person!.ID));

        if (!string.IsNullOrWhiteSpace(projectKey) && projectKey != "All")
        {
            query = query.Where(w =>
                (w.Person!.ProjectContract != null
                    && (w.Person.ProjectContract.Name == projectKey
                        || w.Person.ProjectContract.NameTm == projectKey))
                || (w.WorkPermit != null && w.WorkPermit.Application != null
                    && w.WorkPermit.Application.ProjectContract != null
                    && (w.WorkPermit.Application.ProjectContract.Name == projectKey
                        || w.WorkPermit.Application.ProjectContract.NameTm == projectKey)));
        }

        var list = query.AsEnumerable().ToList();
        if (oneLastValidWorkPermitPerPerson)
            list = TakeOneLastValidVisaPerPerson(list, w => (Guid?)w.Person!.ID, w => (DateTime?)w.ExpirationDate, w => w.ID);

        var labeled = list.Select(w =>
        {
            var project = ProjectLabel(w.Person?.ProjectContract);
            if (string.IsNullOrWhiteSpace(project))
                project = ProjectLabel(w.Person?.SponsoringEmployee?.ProjectContract);
            if (string.IsNullOrWhiteSpace(project))
                project = ProjectLabel(w.WorkPermit?.Application?.ProjectContract);
            if (string.IsNullOrWhiteSpace(project))
                project = "(No project)";
            return (Row: w, Status: project);
        }).ToList();

        return BuildWorkPermitActiveByProjectPanel(
            personType, subReport, excelHint, excelConfigured, labeled,
            w => w.ID,
            w => w.Person?.FullName,
            w =>
            {
                var project = ProjectLabel(w.Person?.ProjectContract);
                if (string.IsNullOrWhiteSpace(project))
                    project = ProjectLabel(w.Person?.SponsoringEmployee?.ProjectContract);
                if (string.IsNullOrWhiteSpace(project))
                    project = ProjectLabel(w.WorkPermit?.Application?.ProjectContract);
                return project;
            },
            w => !string.IsNullOrWhiteSpace(w.WorkPermitNumber) ? w.WorkPermitNumber : w.ASNumber,
            w => w.ExpirationDate == default ? null : w.ExpirationDate);
    }

    /// <summary>
    /// Loads Work Permit By Days Remaining from <c>vw_rd_work_permit</c>
    /// (valid items only; Status = closed days-to-expiry bucket; optional one last per person).
    /// </summary>
    private static ReportDashboardPanelData LoadWorkPermitFromView(
        IObjectSpace objectSpace, PersonRecordRole? role, string projectKey,
        ReportDashboardPersonType personType, string subReport,
        string? excelHint, bool excelConfigured, DateTime cutoff,
        bool includeArchivedPersons,
        bool oneLastValidWorkPermitPerPerson = false,
        HashSet<Guid>? validVisaPersonIds = null)
    {
        if (objectSpace is not EFCoreObjectSpace efOs
            || efOs.DbContext is not Visa2026EFCoreDbContext db)
        {
            return LoadWorkPermitLegacy(objectSpace, role, projectKey, personType, subReport, excelHint, excelConfigured, cutoff, includeArchivedPersons, validVisaPersonIds);
        }

        _ = cutoff;
        IQueryable<VwRdWorkPermit> query = db.VwRdWorkPermit
            .AsNoTracking();
        if (role.HasValue)
            query = query.Where(r => r.PersonRoleCode == (int)role.Value);

        if (!includeArchivedPersons)
            query = query.Where(r => !r.IsArchived);

        if (validVisaPersonIds != null)
            query = query.Where(r => r.PersonOid != null && validVisaPersonIds.Contains(r.PersonOid.Value));

        if (!string.IsNullOrWhiteSpace(projectKey) && projectKey != "All")
        {
            query = query.Where(r =>
                r.ProjectNameTm == projectKey
                || r.ProjectName == projectKey
                || r.ProjectNameRaw == projectKey);
        }

        try
        {
            var list = query.ToList();
            if (oneLastValidWorkPermitPerPerson)
                list = TakeOneLastValidVisaPerPerson(list, r => r.PersonOid, r => r.ExpirationDate, r => r.ID);

            var validityRows = list
                .GroupBy(r => new { r.ValidityLabel, r.ValidityCssClass })
                .Select(g => new
                {
                    Label = g.Key.ValidityLabel,
                    CssClass = g.Key.ValidityCssClass,
                    Count = g.Count()
                })
                .ToList();

            static int RemainingSortKey(string? label) => label switch
            {
                "< 10 days" => 1,
                "< 1 month" => 2,
                "< 3 months" => 3,
                "< 4 months" => 4,
                "< 5 months" => 5,
                "< 6 months" => 6,
                "≥ 6 months" => 7,
                _ => 99
            };

            var buckets = validityRows
                .Select(g => new ReportDashboardStatusBucket
                {
                    Label = g.Label ?? string.Empty,
                    CssClass = g.CssClass ?? "st-pending",
                    Count = g.Count
                })
                .OrderBy(b => RemainingSortKey(b.Label))
                .ToList();

            var totalCount = buckets.Sum(b => b.Count);

            var rows = list
                .OrderBy(r => r.DaysRemaining)
                .ThenBy(r => r.PersonName)
                .Take(PreviewLimit)
                .Select(r => new ReportDashboardPreviewRow
                {
                    RecordId = r.ID,
                    Name = r.PersonName ?? string.Empty,
                    Project = r.ProjectName ?? string.Empty,
                    ColumnA = r.WorkPermitNumber ?? string.Empty,
                    ColumnB = FormatDate(r.ExpirationDate),
                    Status = r.ValidityLabel ?? string.Empty,
                    StatusCssClass = r.ValidityCssClass ?? "st-pending"
                })
                .ToList();

            return BuildPanel(
                personType, ReportDashboardCategory.WorkPermit, subReport, rows,
                excelHint, excelConfigured, buckets, totalCount);
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UndefinedTable)
        {
            return LoadWorkPermitLegacy(objectSpace, role, projectKey, personType, subReport, excelHint, excelConfigured, cutoff, includeArchivedPersons, validVisaPersonIds);
        }
    }

    private static ReportDashboardPanelData LoadWorkPermitLegacy(
        IObjectSpace objectSpace, PersonRecordRole? role, string projectKey,
        ReportDashboardPersonType personType, string subReport,
        string? excelHint, bool excelConfigured, DateTime cutoff,
        bool includeArchivedPersons,
        HashSet<Guid>? validVisaPersonIds = null)
    {
        // Rank last item like PersonCurrentItems (cancelled included); emit only if last is not cancelled.
        var query = objectSpace.GetObjectsQuery<WorkPermitItem>()
            .Where(w => w.Person != null && (role == null || w.Person.PersonRole == role));

        if (!includeArchivedPersons)
            query = query.Where(w => !w.Person!.IsArchived);

        if (validVisaPersonIds != null)
            query = query.Where(w => validVisaPersonIds.Contains(w.Person!.ID));

        if (!string.IsNullOrWhiteSpace(projectKey) && projectKey != "All")
            query = query.Where(w => w.WorkPermit != null && w.WorkPermit.Application != null
                && w.WorkPermit.Application.ProjectContract != null
                && (w.WorkPermit.Application.ProjectContract.Name == projectKey
                    || w.WorkPermit.Application.ProjectContract.NameTm == projectKey));

        _ = cutoff;
        var today = DateTime.Today;
        var latest = query.AsEnumerable()
            .Where(w => w.StartDate != default)
            .GroupBy(w => w.Person!.ID)
            .Select(g => g.OrderByDescending(w => w.StartDate.Date).ThenByDescending(w => w.ID).First())
            .Where(w => !w.IsCancelled)
            .ToList();

        var buckets = latest
            .GroupBy(w => PassportValidityBucket(w.ExpirationDate == default ? null : w.ExpirationDate, today))
            .Select(g => new ReportDashboardStatusBucket
            {
                Label = g.Key,
                Count = g.Count(),
                CssClass = PassportValidityCss(g.First().ExpirationDate == default ? null : g.First().ExpirationDate, today)
            })
            .OrderByDescending(b => b.Count)
            .ToList();

        var rows = latest
            .OrderBy(w => PassportValidityBucket(w.ExpirationDate == default ? null : w.ExpirationDate, today) == "Expired")
            .ThenBy(w => w.ExpirationDate)
            .Take(PreviewLimit)
            .Select(w =>
            {
                DateTime? exp = w.ExpirationDate == default ? null : w.ExpirationDate;
                var status = PassportValidityBucket(exp, today);
                var project = ProjectLabel(w.Person?.ProjectContract);
                if (string.IsNullOrEmpty(project))
                    project = ProjectLabel(w.WorkPermit?.Application?.ProjectContract);
                return new ReportDashboardPreviewRow
                {
                    RecordId = w.ID,
                    Name = w.Person?.FullName ?? string.Empty,
                    Project = project,
                    ColumnA = !string.IsNullOrWhiteSpace(w.WorkPermitNumber) ? w.WorkPermitNumber
                              : (w.ASNumber ?? string.Empty),
                    ColumnB = FormatDate(exp),
                    Status = status,
                    StatusCssClass = PassportValidityCss(exp, today)
                };
            })
            .ToList();

        return BuildPanel(
            personType, ReportDashboardCategory.WorkPermit, subReport, rows,
            excelHint, excelConfigured, buckets, buckets.Sum(b => b.Count));
    }

    private static ReportDashboardPanelData LoadAddressOfResidence(
        IObjectSpace objectSpace, PersonRecordRole? role, string projectKey,
        ReportDashboardPersonType personType, string subReport,
        string? excelHint, bool excelConfigured, DateTime cutoff,
        bool includeArchivedPersons,
        HashSet<Guid>? validVisaPersonIds = null)
    {
        if (objectSpace is not EFCoreObjectSpace efOs
            || efOs.DbContext is not Visa2026EFCoreDbContext db)
        {
            return EmptyPanel(personType, ReportDashboardCategory.AddressOfResidence, subReport, excelHint, excelConfigured);
        }

        HashSet<Guid>? addressIdSet = null;
        if (validVisaPersonIds == null)
        {
            // Valid visa only uses a separate person-based path and ignores Last N months.
            addressIdSet = ReportDashboardRosterQueryHelper.GetLinkedChildIdsInApplicationDateRange(
                db, ApplicationPersonLinkKind.AddressOfResidence, cutoff);
        }

        if (addressIdSet is { Count: 0 })
        {
            return BuildPanel(
                personType, ReportDashboardCategory.AddressOfResidence, subReport,
                new List<ReportDashboardPreviewRow>(),
                excelHint, excelConfigured,
                Array.Empty<ReportDashboardStatusBucket>(), 0);
        }

        IQueryable<AddressOfResidence> query = db.AddressesOfResidence
            .AsNoTracking()
            .Where(a => a.Person != null);

        if (addressIdSet != null)
            query = query.Where(a => addressIdSet.Contains(a.ID));

        if (role.HasValue)
            query = query.Where(a => a.Person!.PersonRole == role.Value);

        if (!includeArchivedPersons)
            query = query.Where(a => !a.Person!.IsArchived);

        if (validVisaPersonIds != null)
            query = query.Where(a => validVisaPersonIds.Contains(a.Person!.ID));

        if (!string.IsNullOrWhiteSpace(projectKey) && projectKey != "All")
        {
            query = query.Where(a => a.Person!.ProjectContract != null
                && (a.Person.ProjectContract.Name == projectKey
                    || a.Person.ProjectContract.NameTm == projectKey));
        }

        var items = query
            .Include(a => a.Person).ThenInclude(p => p!.ProjectContract)
            .Include(a => a.Region)
            .Include(a => a.City)
            .Include(a => a.Lodging)
            .Include(a => a.Hotel)
            .Include(a => a.Hospital)
            .Include(a => a.OtherSite)
            .AsEnumerable()
            .ToList();
        var today = DateTime.Today;

        if (validVisaPersonIds != null)
            items = TakeOneCurrentAddressPerPerson(items, today);

        static string RegionOf(AddressOfResidence a) =>
            a.Region?.NameTm ?? a.Region?.Name ?? "Unknown";

        static string CityOf(AddressOfResidence a) =>
            a.City?.NameTm ?? a.City?.Name ?? "Unknown";

        // FullAddress getter lazy-loads Lodging/Hotel/… — must be eager-loaded above (AsNoTracking).
        static string AddressLabel(AddressOfResidence a)
        {
            try
            {
                var text = a.FullAddress?.Trim();
                return string.IsNullOrWhiteSpace(text) ? "Unknown" : text;
            }
            catch (InvalidOperationException)
            {
                return "Unknown";
            }
        }

        static string AddressTypeOf(AddressOfResidence a) =>
            a.Type switch
            {
                ResidenceType.Lodging => "Lodging",
                ResidenceType.Hotel => "Hotel",
                ResidenceType.PrivateHouse => "Private House",
                ResidenceType.Hospital => "Hospital",
                ResidenceType.Other => "Other",
                _ => "Unknown"
            };

        // Region + City + FullAddress (same composition as AddressOfResidence.DisplayAddress).
        static string AddressWithRegionAndCity(AddressOfResidence a)
        {
            try
            {
                var text = a.DisplayAddress?.Trim();
                if (!string.IsNullOrWhiteSpace(text))
                    return text;
            }
            catch (InvalidOperationException)
            {
                // Fall through to manual compose when nav props are unavailable.
            }

            var parts = new List<string>();
            var region = a.Region?.NameTm ?? a.Region?.Name;
            var city = a.City?.NameTm ?? a.City?.Name;
            if (!string.IsNullOrWhiteSpace(region))
                parts.Add(region.Trim());
            if (!string.IsNullOrWhiteSpace(city))
                parts.Add(city.Trim());
            var address = AddressLabel(a);
            if (address != "Unknown")
                parts.Add(address);
            return parts.Count > 0 ? string.Join(", ", parts) : "Unknown";
        }

        static string RegionCityOf(AddressOfResidence a)
        {
            var region = a.Region?.NameTm ?? a.Region?.Name;
            var city = a.City?.NameTm ?? a.City?.Name;
            if (!string.IsNullOrWhiteSpace(region) && !string.IsNullOrWhiteSpace(city))
                return $"{region.Trim()}, {city.Trim()}";
            if (!string.IsNullOrWhiteSpace(region))
                return region.Trim();
            if (!string.IsNullOrWhiteSpace(city))
                return city.Trim();
            return "Unknown";
        }

        if (subReport is "by-region" or "by-city" or "by-address-type" or "by-address")
        {
            Func<AddressOfResidence, string> groupLabel = subReport switch
            {
                "by-city" => CityOf,
                "by-address-type" => AddressTypeOf,
                "by-address" => AddressWithRegionAndCity,
                _ => RegionOf
            };
            var groups = items
                .GroupBy(groupLabel)
                .Select(g => (Label: g.Key, Count: g.Count()))
                .OrderByDescending(g => g.Count)
                .ToList();
            var buckets = AssignCategoricalCss(groups);
            var cssByLabel = buckets.ToDictionary(b => b.Label, b => b.CssClass, StringComparer.Ordinal);

            var rows = items
                .OrderBy(groupLabel)
                .ThenBy(a => a.Person?.FullName)
                .Take(PreviewLimit)
                .Select(a =>
                {
                    var status = groupLabel(a);
                    // By Address: ColumnA = Region · City; Status/chart = Region + City + FullAddress.
                    var columnA = subReport == "by-address" ? RegionCityOf(a) : AddressLabel(a);
                    return new ReportDashboardPreviewRow
                    {
                        RecordId = a.ID,
                        Name = a.Person?.FullName ?? string.Empty,
                        Project = ProjectLabel(a.Person?.ProjectContract),
                        ColumnA = columnA,
                        ColumnB = FormatDate(a.ExpirationDate),
                        Status = status,
                        StatusCssClass = cssByLabel.TryGetValue(status, out var c) ? c : "st-cat-1"
                    };
                })
                .ToList();

            return BuildPanel(
                personType, ReportDashboardCategory.AddressOfResidence, subReport, rows,
                excelHint, excelConfigured, buckets, buckets.Sum(b => b.Count));
        }

        // Private House Validity: only Private House — lodging/hotel/etc. usually have no registration expiry.
        items = items.Where(a => a.Type == ResidenceType.PrivateHouse).ToList();

        var expiringSoonDays = db.ExpirationAlertRules
            .AsNoTracking()
            .Where(r => r.BusinessObjectKey == ExpirationAlertBusinessObjectKeys.AddressOfResidence)
            .Select(r => (int?)r.ExpiringSoonDays)
            .FirstOrDefault()
            ?? ExpirationAlertRule.DefaultExpiringSoonDays;
        if (expiringSoonDays <= 0)
            expiringSoonDays = ExpirationAlertRule.DefaultExpiringSoonDays;

        var validityGroups = items
            .GroupBy(a => PrivateHouseValidityBucket(a.ExpirationDate, today, expiringSoonDays))
            .Select(g => (Label: g.Key, Count: g.Count()))
            .OrderByDescending(g => g.Count)
            .ToList();

        var validityBuckets = validityGroups
            .Select(g => new ReportDashboardStatusBucket
            {
                Label = g.Label,
                Count = g.Count,
                CssClass = PrivateHouseValidityCss(g.Label)
            })
            .ToList();
        var cssByValidity = validityBuckets.ToDictionary(b => b.Label, b => b.CssClass, StringComparer.Ordinal);

        var previewRows = items
            .OrderBy(a => a.ExpirationDate ?? DateTime.MaxValue)
            .ThenBy(a => a.Person?.FullName)
            .Take(PreviewLimit)
            .Select(a =>
            {
                var status = PrivateHouseValidityBucket(a.ExpirationDate, today, expiringSoonDays);
                return new ReportDashboardPreviewRow
                {
                    RecordId = a.ID,
                    Name = a.Person?.FullName ?? string.Empty,
                    Project = ProjectLabel(a.Person?.ProjectContract),
                    ColumnA = AddressLabel(a),
                    ColumnB = FormatDate(a.ExpirationDate),
                    Status = status,
                    StatusCssClass = cssByValidity.TryGetValue(status, out var c) ? c : PrivateHouseValidityCss(status)
                };
            })
            .ToList();

        return BuildPanel(
            personType, ReportDashboardCategory.AddressOfResidence, subReport, previewRows,
            excelHint, excelConfigured, validityBuckets, validityBuckets.Sum(b => b.Count));
    }

    private static ReportDashboardPanelData LoadTravel(
        IObjectSpace objectSpace, PersonRecordRole? role, string projectKey,
        ReportDashboardPersonType personType, string subReport,
        string? excelHint, bool excelConfigured, DateTime cutoff,
        HashSet<Guid>? validVisaPersonIds = null)
    {
        List<ReportDashboardRosterQueryHelper.TravelLine> travelLines;
        if (TryGetDbContext(objectSpace, out var db))
        {
            travelLines = ReportDashboardRosterQueryHelper.LoadTravelLines(
                db, role, projectKey, cutoff, validVisaPersonIds, PreviewLimit);
        }
        else
        {
            var query = objectSpace.GetObjectsQuery<ApplicationItem>()
                .Where(a => a.Person != null && (role == null || a.Person.PersonRole == role)
                            && a.TravelDate != null && a.TravelDate >= cutoff);

            if (validVisaPersonIds != null)
                query = query.Where(a => validVisaPersonIds.Contains(a.Person!.ID));

            if (!string.IsNullOrWhiteSpace(projectKey) && projectKey != "All")
                query = query.Where(a => a.Application != null && a.Application.ProjectContract != null
                    && (a.Application.ProjectContract.Name == projectKey || a.Application.ProjectContract.NameTm == projectKey));

            travelLines = query.OrderByDescending(a => a.TravelDate).Take(PreviewLimit).AsEnumerable()
                .Select(a => new ReportDashboardRosterQueryHelper.TravelLine(
                    a.ID, a.Person!, a.Application, a.TravelDate))
                .ToList();
        }

        var rows = travelLines.Select(a =>
        {
            const string status = "Approved";
            return new ReportDashboardPreviewRow
            {
                RecordId = a.RecordId,
                Name = a.Person?.FullName ?? string.Empty,
                Project = ProjectLabel(a.Application?.ProjectContract),
                ColumnA = a.Application?.ApplicationNumber ?? string.Empty,
                ColumnB = FormatDate(a.TravelDate),
                Status = status,
                StatusCssClass = StatusCss(status, null)
            };
        }).ToList();

        return BuildPanel(personType, ReportDashboardCategory.Travel, subReport, rows, excelHint, excelConfigured);
    }

    private static ReportDashboardPanelData LoadBorderZone(
        IObjectSpace objectSpace, PersonRecordRole? role, string projectKey,
        ReportDashboardPersonType personType, string subReport,
        string? excelHint, bool excelConfigured, DateTime cutoff,
        HashSet<Guid>? validVisaPersonIds = null)
    {
        var query = objectSpace.GetObjectsQuery<BorderZoneItem>()
            .Where(b => b.Person != null && (role == null || b.Person.PersonRole == role)
                        && (b.BorderZone == null || b.BorderZone.ExpirationDate == null || b.BorderZone.ExpirationDate >= cutoff));

        if (validVisaPersonIds != null)
            query = query.Where(b => validVisaPersonIds.Contains(b.Person!.ID));

        if (!string.IsNullOrWhiteSpace(projectKey) && projectKey != "All")
            query = query.Where(b => b.BorderZone != null && b.BorderZone.Application != null
                && b.BorderZone.Application.ProjectContract != null
                && (b.BorderZone.Application.ProjectContract.Name == projectKey
                    || b.BorderZone.Application.ProjectContract.NameTm == projectKey));

        var rows = query.Take(PreviewLimit).AsEnumerable().Select(b =>
        {
            var status = ExpirationBucket(b.BorderZone?.ExpirationDate, DateTime.Today);
            return new ReportDashboardPreviewRow
            {
                RecordId = b.ID,
                Name = b.Person?.FullName ?? string.Empty,
                Project = ProjectLabel(b.BorderZone?.Application?.ProjectContract),
                ColumnA = b.BorderZone?.BorderZoneNumber ?? string.Empty,
                ColumnB = FormatDate(b.BorderZone?.ExpirationDate),
                Status = status,
                StatusCssClass = StatusCss(status, b.BorderZone?.DaysRemaining)
            };
        }).ToList();

        return BuildPanel(personType, ReportDashboardCategory.BorderZone, subReport, rows, excelHint, excelConfigured);
    }

    private static ReportDashboardPanelData LoadPassport(
        IObjectSpace objectSpace, PersonRecordRole? role, string projectKey,
        ReportDashboardPersonType personType, string subReport,
        string? excelHint, bool excelConfigured, DateTime cutoff,
        bool includeArchivedPersons,
        HashSet<Guid>? validVisaPersonIds = null)
    {
        _ = includeArchivedPersons; // Passport always excludes Person.IsArchived on active sub-reports.
        // Valid visa only: ignore Last N months; one last active passport per valid-visa person
        // (totals align with Visa → By Category + one last valid visa per person).
        if (validVisaPersonIds != null)
        {
            return LoadPassportValidVisaOnly(
                objectSpace, role, projectKey, personType, subReport, excelHint, excelConfigured,
                validVisaPersonIds);
        }

        // Otherwise: ApplicationItem.CurrentPassport in Last N months; one last active passport per person.
        return LoadPassportFromView(
            objectSpace, role, projectKey, personType, subReport, excelHint, excelConfigured, cutoff,
            validVisaPersonIds: null);
    }

    /// <summary>
    /// Passport is active for By Validity / Type / Citizenship when the person is not archived,
    /// the passport is the person's last (latest IssueDate), and it has not been expired for a full month.
    /// </summary>
    private static bool IsActivePassport(
        bool personIsArchived,
        bool isLastPassport,
        DateTime? expirationDate,
        DateTime today)
    {
        if (personIsArchived || !isLastPassport)
            return false;
        if (!expirationDate.HasValue)
            return true;
        return expirationDate.Value.Date > today.AddMonths(-1);
    }

    /// <summary>
    /// Passport when Valid visa only is checked: Last N months is ignored.
    /// One last active passport per person among persons with a valid visa —
    /// same person set as Visa By Category with one last valid visa per person.
    /// </summary>
    private static ReportDashboardPanelData LoadPassportValidVisaOnly(
        IObjectSpace objectSpace, PersonRecordRole? role, string projectKey,
        ReportDashboardPersonType personType, string subReport,
        string? excelHint, bool excelConfigured,
        HashSet<Guid> validVisaPersonIds)
    {
        if (objectSpace is not EFCoreObjectSpace efOs
            || efOs.DbContext is not Visa2026EFCoreDbContext db)
        {
            return LoadPassportLegacyValidVisaOnly(
                objectSpace, role, projectKey, personType, subReport, excelHint, excelConfigured,
                validVisaPersonIds);
        }

        IQueryable<Passport> query = db.Passports
            .AsNoTracking()
            .Include(p => p.Person!).ThenInclude(pe => pe.ProjectContract)
            .Include(p => p.Person!).ThenInclude(pe => pe.Nationality)
            .Include(p => p.Person!).ThenInclude(pe => pe.SponsoringEmployee!).ThenInclude(s => s.ProjectContract)
            .Include(p => p.PassportType)
            .Where(p => p.Person != null
                && p.IssueDate != null
                && !p.Person.IsArchived
                && validVisaPersonIds.Contains(p.Person.ID));

        if (role.HasValue)
            query = query.Where(p => p.Person!.PersonRole == role.Value);

        if (!string.IsNullOrWhiteSpace(projectKey) && projectKey != "All")
        {
            query = query.Where(p =>
                (p.Person!.ProjectContract != null
                    && (p.Person.ProjectContract.Name == projectKey || p.Person.ProjectContract.NameTm == projectKey))
                || (p.Person!.SponsoringEmployee != null
                    && p.Person.SponsoringEmployee.ProjectContract != null
                    && (p.Person.SponsoringEmployee.ProjectContract.Name == projectKey
                        || p.Person.SponsoringEmployee.ProjectContract.NameTm == projectKey)));
        }

        var today = DateTime.Today;
        var passports = query.AsEnumerable()
            .GroupBy(p => p.Person!.ID)
            .Select(g => g
                .OrderByDescending(p => p.IssueDate!.Value.Date)
                .ThenByDescending(p => p.ID)
                .First())
            .Where(p => IsActivePassport(false, isLastPassport: true, p.ExpirationDate, today))
            .ToList();

        return BuildPassportPanelFromPassports(
            passports, personType, subReport, excelHint, excelConfigured, today);
    }

    private static ReportDashboardPanelData LoadPassportLegacyValidVisaOnly(
        IObjectSpace objectSpace, PersonRecordRole? role, string projectKey,
        ReportDashboardPersonType personType, string subReport,
        string? excelHint, bool excelConfigured,
        HashSet<Guid> validVisaPersonIds)
    {
        var query = objectSpace.GetObjectsQuery<Passport>()
            .Where(p => p.Person != null
                && p.IssueDate != null
                && !p.Person.IsArchived
                && validVisaPersonIds.Contains(p.Person.ID));

        if (role.HasValue)
            query = query.Where(p => p.Person!.PersonRole == role.Value);

        if (!string.IsNullOrWhiteSpace(projectKey) && projectKey != "All")
        {
            query = query.Where(p =>
                p.Person!.ProjectContract != null
                && (p.Person.ProjectContract.Name == projectKey || p.Person.ProjectContract.NameTm == projectKey));
        }

        var today = DateTime.Today;
        var passports = query.AsEnumerable()
            .GroupBy(p => p.Person!.ID)
            .Select(g => g
                .OrderByDescending(p => p.IssueDate!.Value.Date)
                .ThenByDescending(p => p.ID)
                .First())
            .Where(p => IsActivePassport(false, isLastPassport: true, p.ExpirationDate, today))
            .ToList();

        return BuildPassportPanelFromPassports(
            passports, personType, subReport, excelHint, excelConfigured, today);
    }

    private static ReportDashboardPanelData BuildPassportPanelFromPassports(
        List<Passport> passports,
        ReportDashboardPersonType personType,
        string subReport,
        string? excelHint,
        bool excelConfigured,
        DateTime today)
    {
        var categorical = subReport is "by-type" or "by-citizenship";

        static string TypeLabelOf(Passport p) =>
            p.PassportType?.NameTm ?? p.PassportType?.Name ?? "Unknown";
        static string CitizenshipLabelOf(Passport p) =>
            p.Person?.Nationality?.NameTm ?? p.Person?.Nationality?.Name ?? "Unknown";
        static string ProjectOf(Passport p)
        {
            var label = ProjectLabel(p.Person?.ProjectContract);
            return !string.IsNullOrEmpty(label)
                ? label
                : ProjectLabel(p.Person?.SponsoringEmployee?.ProjectContract);
        }

        List<ReportDashboardStatusBucket> buckets;
        if (categorical)
        {
            var groups = passports
                .GroupBy(p => subReport == "by-citizenship" ? CitizenshipLabelOf(p) : TypeLabelOf(p))
                .Select(g => (Label: g.Key, Count: g.Count()))
                .OrderByDescending(g => g.Count)
                .ToList();
            buckets = AssignCategoricalCss(groups);
        }
        else
        {
            buckets = passports
                .GroupBy(p => PassportValidityBucket(p.ExpirationDate, today))
                .Select(g => new ReportDashboardStatusBucket
                {
                    Label = g.Key,
                    Count = g.Count(),
                    CssClass = PassportValidityCss(g.First().ExpirationDate, today)
                })
                .OrderByDescending(b => b.Count)
                .ToList();
        }

        var cssByLabel = buckets.ToDictionary(b => b.Label, b => b.CssClass, StringComparer.Ordinal);

        IEnumerable<Passport> preview = categorical
            ? passports.OrderBy(p => subReport == "by-citizenship" ? CitizenshipLabelOf(p) : TypeLabelOf(p))
                .ThenBy(p => p.ExpirationDate)
            : passports.OrderBy(p => PassportValidityBucket(p.ExpirationDate, today) == "Expired")
                .ThenBy(p => p.ExpirationDate);

        var rows = preview
            .Take(PreviewLimit)
            .Select(p =>
            {
                var status = subReport switch
                {
                    "by-type" => TypeLabelOf(p),
                    "by-citizenship" => CitizenshipLabelOf(p),
                    _ => PassportValidityBucket(p.ExpirationDate, today)
                };
                var css = categorical
                    ? (cssByLabel.TryGetValue(status, out var c) ? c : "st-cat-1")
                    : PassportValidityCss(p.ExpirationDate, today);
                return new ReportDashboardPreviewRow
                {
                    RecordId = p.ID,
                    Name = p.Person?.FullName ?? string.Empty,
                    Project = ProjectOf(p),
                    ColumnA = p.PassportNumber ?? string.Empty,
                    ColumnB = FormatDate(p.ExpirationDate),
                    Status = status,
                    StatusCssClass = css
                };
            })
            .ToList();

        return BuildPanel(
            personType, ReportDashboardCategory.Passport, subReport, rows,
            excelHint, excelConfigured, buckets, buckets.Sum(b => b.Count));
    }

    /// <summary>
    /// Loads Passport panel from <c>vw_rd_passport</c> when Valid visa only is off
    /// (ApplicationItems with CurrentPassport; filtered by Application.ApplicationDate;
    /// one last active passport per person by latest IssueDate).
    /// </summary>
    private static ReportDashboardPanelData LoadPassportFromView(
        IObjectSpace objectSpace, PersonRecordRole? role, string projectKey,
        ReportDashboardPersonType personType, string subReport,
        string? excelHint, bool excelConfigured, DateTime cutoff,
        HashSet<Guid>? validVisaPersonIds = null)
    {
        if (objectSpace is not EFCoreObjectSpace efOs
            || efOs.DbContext is not Visa2026EFCoreDbContext db)
        {
            return LoadPassportLegacy(objectSpace, role, projectKey, personType, subReport, excelHint, excelConfigured, cutoff, validVisaPersonIds);
        }

        var categorical = subReport is "by-type" or "by-citizenship";
        var today = DateTime.Today;

        // Last N months only (Valid visa only uses a separate loader that ignores cutoff).
        IQueryable<VwRdPassport> query = db.VwRdPassport
            .AsNoTracking()
            .Where(r => r.ApplicationDate != null && r.ApplicationDate >= cutoff);
        if (role.HasValue)
            query = query.Where(r => r.PersonRoleCode == (int)role.Value);

        // Passport sub-reports never include Person.IsArchived.
        query = query.Where(r => !r.IsArchived);

        if (validVisaPersonIds != null)
            query = query.Where(r => r.PersonOid != null && validVisaPersonIds.Contains(r.PersonOid.Value));

        if (!string.IsNullOrWhiteSpace(projectKey) && projectKey != "All")
        {
            query = query.Where(r =>
                r.ProjectNameTm == projectKey
                || r.ProjectName == projectKey
                || r.ProjectNameRaw == projectKey);
        }

        try
        {
            var list = TakeOneLastPassportPerPerson(query.ToList())
                .Where(r => IsActivePassport(false, isLastPassport: true, r.ExpirationDate, today))
                .ToList();

            List<ReportDashboardStatusBucket> buckets;
            if (categorical)
            {
                var catRows = list
                    .GroupBy(r => subReport == "by-citizenship" ? r.CitizenshipLabel : r.TypeLabel)
                    .Select(g => new { Label = g.Key, Count = g.Count() })
                    .OrderByDescending(g => g.Count)
                    .ToList();

                buckets = AssignCategoricalCss(catRows.Select(t => (t.Label ?? string.Empty, t.Count)).ToList());
            }
            else
            {
                buckets = list
                    .GroupBy(r => new { r.ValidityLabel, r.ValidityCssClass })
                    .Select(g => new ReportDashboardStatusBucket
                    {
                        Label = g.Key.ValidityLabel ?? string.Empty,
                        CssClass = g.Key.ValidityCssClass ?? "st-pending",
                        Count = g.Count()
                    })
                    .OrderByDescending(b => b.Count)
                    .ToList();
            }

            var totalCount = buckets.Sum(b => b.Count);
            var cssByLabel = buckets.ToDictionary(b => b.Label, b => b.CssClass, StringComparer.Ordinal);

            IEnumerable<VwRdPassport> previewQuery = subReport switch
            {
                "by-type" => list.OrderBy(r => r.TypeLabel).ThenBy(r => r.ExpirationDate),
                "by-citizenship" => list.OrderBy(r => r.CitizenshipLabel).ThenBy(r => r.ExpirationDate),
                _ => list.OrderBy(r => r.ValidityLabel == "Expired").ThenBy(r => r.ExpirationDate)
            };

            var rows = previewQuery
                .Take(PreviewLimit)
                .Select(r =>
                {
                    var status = subReport switch
                    {
                        "by-type" => r.TypeLabel ?? string.Empty,
                        "by-citizenship" => r.CitizenshipLabel ?? string.Empty,
                        _ => r.ValidityLabel ?? string.Empty
                    };
                    var css = categorical
                        ? (cssByLabel.TryGetValue(status, out var c) ? c : "st-cat-1")
                        : (r.ValidityCssClass ?? "st-pending");
                    return new ReportDashboardPreviewRow
                    {
                        RecordId = r.PassportOid ?? r.ID,
                        Name = r.PersonName ?? string.Empty,
                        Project = r.ProjectName ?? string.Empty,
                        ColumnA = r.PassportNumber ?? string.Empty,
                        ColumnB = FormatDate(r.ExpirationDate),
                        Status = status,
                        StatusCssClass = css
                    };
                })
                .ToList();

            return BuildPanel(
                personType, ReportDashboardCategory.Passport, subReport, rows,
                excelHint, excelConfigured, buckets, totalCount);
        }
        catch (PostgresException ex) when (ex.SqlState is PostgresErrorCodes.UndefinedTable or PostgresErrorCodes.UndefinedColumn)
        {
            return LoadPassportLegacy(objectSpace, role, projectKey, personType, subReport, excelHint, excelConfigured, cutoff, validVisaPersonIds);
        }
    }

    private static List<ReportDashboardStatusBucket> AssignCategoricalCss(
        List<(string Label, int Count)> groups)
    {
        string[] palette = ["st-cat-1", "st-cat-2", "st-cat-3", "st-cat-4", "st-cat-5"];
        return groups
            .Select((g, i) => new ReportDashboardStatusBucket
            {
                Label = g.Label,
                Count = g.Count,
                CssClass = palette[i % palette.Length]
            })
            .ToList();
    }
    private static ReportDashboardPanelData LoadPassportLegacy(
        IObjectSpace objectSpace, PersonRecordRole? role, string projectKey,
        ReportDashboardPersonType personType, string subReport,
        string? excelHint, bool excelConfigured, DateTime cutoff,
        HashSet<Guid>? validVisaPersonIds = null)
    {
        if (validVisaPersonIds != null)
        {
            return LoadPassportLegacyValidVisaOnly(
                objectSpace, role, projectKey, personType, subReport, excelHint, excelConfigured,
                validVisaPersonIds);
        }

        // Same universe as vw_rd_passport: ApplicationItems with CurrentPassport in date range.
        var query = objectSpace.GetObjectsQuery<ApplicationItem>()
            .Where(ai => ai.CurrentPassport != null
                        && ai.Person != null && (role == null || ai.Person.PersonRole == role)
                        && !ai.Person.IsArchived
                        && ai.Application != null
                        && ai.Application.ApplicationDate != null
                        && ai.Application.ApplicationDate >= cutoff);

        if (validVisaPersonIds != null)
            query = query.Where(ai => validVisaPersonIds.Contains(ai.Person!.ID));

        if (!string.IsNullOrWhiteSpace(projectKey) && projectKey != "All")
        {
            query = query.Where(ai =>
                (ai.Application!.ProjectContract != null
                    && (ai.Application.ProjectContract.Name == projectKey || ai.Application.ProjectContract.NameTm == projectKey))
                || (ai.Person!.ProjectContract != null
                    && (ai.Person.ProjectContract.Name == projectKey || ai.Person.ProjectContract.NameTm == projectKey)));
        }

        var today = DateTime.Today;
        // One last passport per person: latest IssueDate (then passport ID), matching GetCurrentPassport.
        var items = query.AsEnumerable()
            .Where(ai => ai.CurrentPassport != null && ai.Person != null)
            .GroupBy(ai => ai.Person!.ID)
            .Select(g => g
                .OrderByDescending(ai => ai.CurrentPassport!.IssueDate ?? DateTime.MinValue)
                .ThenByDescending(ai => ai.CurrentPassport!.ID)
                .ThenByDescending(ai => ai.Application!.ApplicationDate)
                .First())
            .Where(ai => IsActivePassport(false, isLastPassport: true, ai.CurrentPassport!.ExpirationDate, today))
            .ToList();
        var categorical = subReport is "by-type" or "by-citizenship";

        static string TypeLabelOf(ApplicationItem ai) =>
            ai.CurrentPassport?.PassportType?.NameTm ?? ai.CurrentPassport?.PassportType?.Name ?? "Unknown";
        static string CitizenshipLabelOf(ApplicationItem ai) =>
            ai.Person?.Nationality?.NameTm ?? ai.Person?.Nationality?.Name ?? "Unknown";

        List<ReportDashboardStatusBucket> buckets;
        if (categorical)
        {
            var groups = items
                .GroupBy(ai => subReport == "by-citizenship" ? CitizenshipLabelOf(ai) : TypeLabelOf(ai))
                .Select(g => (Label: g.Key, Count: g.Count()))
                .OrderByDescending(g => g.Count)
                .ToList();
            buckets = AssignCategoricalCss(groups);
        }
        else
        {
            buckets = items
                .GroupBy(ai => PassportValidityBucket(ai.CurrentPassport!.ExpirationDate, today))
                .Select(g => new ReportDashboardStatusBucket
                {
                    Label = g.Key,
                    Count = g.Count(),
                    CssClass = PassportValidityCss(g.First().CurrentPassport!.ExpirationDate, today)
                })
                .OrderByDescending(b => b.Count)
                .ToList();
        }

        var cssByLabel = buckets.ToDictionary(b => b.Label, b => b.CssClass, StringComparer.Ordinal);

        IEnumerable<ApplicationItem> preview = categorical
            ? items.OrderBy(ai => subReport == "by-citizenship" ? CitizenshipLabelOf(ai) : TypeLabelOf(ai))
                .ThenBy(ai => ai.CurrentPassport!.ExpirationDate)
            : items.OrderBy(ai => PassportValidityBucket(ai.CurrentPassport!.ExpirationDate, today) == "Expired")
                .ThenBy(ai => ai.CurrentPassport!.ExpirationDate);

        var rows = preview
            .Take(PreviewLimit)
            .Select(ai =>
            {
                var status = subReport switch
                {
                    "by-type" => TypeLabelOf(ai),
                    "by-citizenship" => CitizenshipLabelOf(ai),
                    _ => PassportValidityBucket(ai.CurrentPassport!.ExpirationDate, today)
                };
                var css = categorical
                    ? (cssByLabel.TryGetValue(status, out var c) ? c : "st-cat-1")
                    : PassportValidityCss(ai.CurrentPassport!.ExpirationDate, today);
                return new ReportDashboardPreviewRow
                {
                    RecordId = ai.CurrentPassport!.ID,
                    Name = ai.Person?.FullName ?? string.Empty,
                    Project = ProjectLabel(ai.Application?.ProjectContract ?? ai.Person?.ProjectContract),
                    ColumnA = ai.CurrentPassport.PassportNumber ?? string.Empty,
                    ColumnB = FormatDate(ai.CurrentPassport.ExpirationDate),
                    Status = status,
                    StatusCssClass = css
                };
            })
            .ToList();

        return BuildPanel(
            personType, ReportDashboardCategory.Passport, subReport, rows,
            excelHint, excelConfigured, buckets, items.Count);
    }
    /// <summary>
    /// Person IDs with at least one valid visa (not cancelled, expiry on or after today).
    /// </summary>
    private static HashSet<Guid> LoadValidVisaPersonIds(Visa2026EFCoreDbContext db)
    {
        var today = DateTime.Today;
        return db.Visas
            .AsNoTracking()
            .Where(v => !v.IsCancelled
                && v.ExpirationDate >= today
                && v.Passport != null
                && v.Passport.Person != null)
            .Select(v => v.Passport!.Person!.ID)
            .ToHashSet();
    }

    private static HashSet<Guid>? ResolveValidVisaPersonIds(
        IObjectSpace objectSpace,
        ReportDashboardCategory category,
        bool validVisaPersonsOnly)
    {
        if (!validVisaPersonsOnly || !ReportDashboardCatalog.SupportsValidVisaPersonsOnly(category))
            return null;
        if (objectSpace is not EFCoreObjectSpace efOs || efOs.DbContext is not Visa2026EFCoreDbContext db)
            return null;
        return LoadValidVisaPersonIds(db);
    }

    /// <summary>
    /// One passport row per person: keep the passport with the latest IssueDate
    /// (ties: highest PassportOid, then latest ApplicationDate). Matches PersonCurrentItems.GetCurrentPassport ranking.
    /// </summary>
    private static List<VwRdPassport> TakeOneLastPassportPerPerson(IEnumerable<VwRdPassport> rows) =>
        rows
            .Where(r => r.PersonOid != null)
            .GroupBy(r => r.PersonOid!.Value)
            .Select(g => g
                .OrderByDescending(r => r.IssueDate ?? DateTime.MinValue)
                .ThenByDescending(r => r.PassportOid ?? r.ID)
                .ThenByDescending(r => r.ApplicationDate ?? DateTime.MinValue)
                .First())
            .ToList();

    /// <summary>
    /// One address per person using PersonCurrentItems.GetCurrentAddressOfResidence ranking.
    /// </summary>
    private static List<AddressOfResidence> TakeOneCurrentAddressPerPerson(
        IEnumerable<AddressOfResidence> rows,
        DateTime today) =>
        rows
            .Where(a => a.Person != null)
            .GroupBy(a => a.Person!.ID)
            .Select(g =>
            {
                var live = g.ToList();
                var stillValid = live
                    .Where(a => !a.ExpirationDate.HasValue || a.ExpirationDate.Value.Date >= today.Date)
                    .ToList();

                return (stillValid.Count > 0 ? stillValid : live)
                    .OrderByDescending(a => a.ExpirationDate?.Date
                        ?? (stillValid.Count > 0 ? DateTime.MaxValue : DateTime.MinValue))
                    .ThenByDescending(a => a.ID)
                    .First();
            })
            .ToList();

    /// <summary>
    /// One position per person using PersonCurrentItems.GetCurrentPositionHistory ranking
    /// (prefer open/current by StatusLabel, then latest StartDate, then ID).
    /// </summary>
    private static List<VwRdPositionHistory> TakeOneCurrentPositionPerPerson(
        IEnumerable<VwRdPositionHistory> rows) =>
        rows
            .Where(r => r.PersonOid != null)
            .GroupBy(r => r.PersonOid!.Value)
            .Select(g =>
            {
                var live = g.ToList();
                var open = live
                    .Where(r => !string.Equals(r.StatusLabel, "Ended", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                return (open.Count > 0 ? open : live)
                    .OrderByDescending(r => r.StartDate ?? DateTime.MinValue)
                    .ThenByDescending(r => r.ID)
                    .First();
            })
            .ToList();

    /// <summary>
    /// One education per person using PersonCurrentItems.GetCurrentEducation ranking
    /// (latest parseable GraduationYear, then highest ID).
    /// </summary>
    private static List<T> TakeOneCurrentEducationPerPerson<T>(
        IEnumerable<T> rows,
        Func<T, Guid?> personOidSelector,
        Func<T, string?> graduationYearSelector,
        Func<T, Guid> idSelector) =>
        rows
            .Where(r => personOidSelector(r) != null)
            .GroupBy(r => personOidSelector(r)!.Value)
            .Select(g => g
                .OrderByDescending(r => ParseEducationGraduationYear(graduationYearSelector(r)))
                .ThenByDescending(r => idSelector(r))
                .First())
            .ToList();

    private static int ParseEducationGraduationYear(string? year) =>
        int.TryParse(year?.Trim(), out var parsed) ? parsed : int.MinValue;

    /// <summary>
    /// One row per person: keep the valid visa with the latest ExpirationDate (ties: highest ID).
    /// Rows without PersonOid stay distinct (grouped by their own ID).
    /// </summary>
    private static List<T> TakeOneLastValidVisaPerPerson<T>(
        IEnumerable<T> rows,
        Func<T, Guid?> personOidSelector,
        Func<T, DateTime?> expirationSelector,
        Func<T, Guid> idSelector) =>
        rows
            .GroupBy(r => personOidSelector(r) ?? idSelector(r))
            .Select(g => g
                .OrderByDescending(r => expirationSelector(r) ?? DateTime.MinValue)
                .ThenByDescending(r => idSelector(r))
                .First())
            .ToList();

    private static ReportDashboardPanelData BuildPanel(
        ReportDashboardPersonType personType,
        ReportDashboardCategory category,
        string subReport,
        List<ReportDashboardPreviewRow> rows,
        string? excelHint,
        bool excelConfigured,
        IReadOnlyList<ReportDashboardStatusBucket>? statusBuckets = null,
        int? totalCount = null,
        string? subReportLabel = null)
    {
        var buckets = statusBuckets?.ToList() ?? rows
            .GroupBy(r => r.Status)
            .Select(g => new ReportDashboardStatusBucket
            {
                Label    = g.Key,
                Count    = g.Count(),
                CssClass = g.First().StatusCssClass
            })
            .OrderByDescending(b => b.Count)
            .ToList();

        var subLabel = !string.IsNullOrWhiteSpace(subReportLabel)
            ? subReportLabel!
            : ReportDashboardCatalog.SubReports(category)
                .FirstOrDefault(s => s.Key == subReport)?.Label
                ?? ReportDashboardCatalog.CategoryLabel(category);

        return new ReportDashboardPanelData
        {
            PersonType            = personType,
            Category              = category,
            SubReport             = subReport,
            Title                 = ReportDashboardCatalog.CategoryLabel(category),
            Subtitle              = $"{ReportDashboardCatalog.PersonTypeLabel(personType)} — {subLabel}",
            TableHeaders          = ReportDashboardCatalog.TableHeaders(category, subReport),
            StatusBuckets         = buckets,
            PreviewRows           = rows,
            TotalCount            = totalCount ?? rows.Count,
            ExcelTemplateNameHint = excelHint,
            ExcelConfigured       = excelConfigured,
            ListViewId            = ReportDashboardCatalog.ResolveListViewTarget(category, subReport).ListViewId
        };
    }

    private static ReportDashboardPanelData LoadEducation(
        IObjectSpace objectSpace, PersonRecordRole? role, string projectKey,
        ReportDashboardPersonType personType, string subReport,
        string? excelHint, bool excelConfigured, DateTime cutoff,
        bool includeArchivedPersons,
        HashSet<Guid>? validVisaPersonIds = null)
    {
        if (subReport is "by-country")
        {
            return LoadEducationByCountryFromView(
                objectSpace, role, projectKey, personType, subReport, excelHint, excelConfigured, cutoff, includeArchivedPersons, validVisaPersonIds);
        }

        if (objectSpace is not EFCoreObjectSpace efOs
            || efOs.DbContext is not Visa2026EFCoreDbContext db)
        {
            return LoadEducationLegacy(
                objectSpace, role, projectKey, personType, subReport, excelHint, excelConfigured, cutoff,
                includeArchivedPersons, validVisaPersonIds);
        }

        IQueryable<VwRdEducation> query = db.VwRdEducation.AsNoTracking();

        if (validVisaPersonIds == null)
        {
            var educationIdSet = ReportDashboardRosterQueryHelper.GetLinkedChildIdsInApplicationDateRange(
                db, ApplicationPersonLinkKind.Education, cutoff);
            if (educationIdSet.Count == 0)
            {
                return BuildPanel(
                    personType, ReportDashboardCategory.Education, subReport,
                    new List<ReportDashboardPreviewRow>(),
                    excelHint, excelConfigured,
                    Array.Empty<ReportDashboardStatusBucket>(), 0);
            }

            query = query.Where(r => educationIdSet.Contains(r.ID));
        }
        else
        {
            // Valid visa only: ignore Last N months; one current education per valid-visa person.
            query = query.Where(r =>
                r.PersonOid != null && validVisaPersonIds.Contains(r.PersonOid.Value));
        }

        // Source of truth: Person.IsArchived (not a separate Education flag).
        if (!includeArchivedPersons)
        {
            query = query.Where(r =>
                r.PersonOid != null
                && db.People.Any(p => p.ID == r.PersonOid && !p.IsArchived));
        }

        if (role.HasValue)
            query = query.Where(r => r.PersonRoleCode == (int)role.Value);

        if (!string.IsNullOrWhiteSpace(projectKey) && projectKey != "All")
        {
            query = query.Where(r =>
                r.ProjectNameTm == projectKey
                || r.ProjectName == projectKey
                || r.ProjectNameRaw == projectKey);
        }

        try
        {
            List<VwRdEducation> list;
            if (validVisaPersonIds != null)
            {
                list = TakeOneCurrentEducationPerPerson(
                    query.ToList(),
                    r => r.PersonOid,
                    r => r.GraduationYear,
                    r => r.ID);
            }
            else
            {
                list = query.ToList();
            }

            var groups = list
                .GroupBy(r => subReport switch
                {
                    "by-country" => r.CountryLabel,
                    "by-specialty" => r.SpecialtyLabel,
                    _ => r.LevelLabel
                })
                .Select(g => new { Label = g.Key, Count = g.Count() })
                .OrderByDescending(g => g.Count)
                .ToList();

            var buckets = AssignCategoricalCss(
                groups.Select(g => (g.Label ?? "Unknown", g.Count)).ToList());
            var cssByLabel = buckets.ToDictionary(b => b.Label, b => b.CssClass, StringComparer.Ordinal);
            var totalCount = buckets.Sum(b => b.Count);

            var rows = list
                .OrderBy(r => subReport switch
                {
                    "by-country" => r.CountryLabel,
                    "by-specialty" => r.SpecialtyLabel,
                    _ => r.LevelLabel
                })
                .ThenBy(r => r.PersonName)
                .Take(PreviewLimit)
                .Select(r =>
                {
                    var status = subReport switch
                    {
                        "by-country" => r.CountryLabel ?? "Unknown",
                        "by-specialty" => r.SpecialtyLabel ?? "Unknown",
                        _ => r.LevelLabel ?? "Unknown"
                    };
                    return new ReportDashboardPreviewRow
                    {
                        RecordId = r.ID,
                        Name = r.PersonName ?? string.Empty,
                        Project = r.ProjectName ?? string.Empty,
                        ColumnA = r.InstitutionName ?? string.Empty,
                        ColumnB = r.GraduationYear ?? string.Empty,
                        Status = status,
                        StatusCssClass = cssByLabel.TryGetValue(status, out var c) ? c : "st-cat-1"
                    };
                })
                .ToList();

            return BuildPanel(
                personType, ReportDashboardCategory.Education, subReport, rows,
                excelHint, excelConfigured, buckets, totalCount);
        }
        catch (PostgresException ex) when (ex.SqlState is PostgresErrorCodes.UndefinedTable or PostgresErrorCodes.UndefinedColumn)
        {
            return LoadEducationLegacy(
                objectSpace, role, projectKey, personType, subReport, excelHint, excelConfigured, cutoff,
                includeArchivedPersons, validVisaPersonIds);
        }
    }

    private static ReportDashboardPanelData LoadEducationByCountryFromView(
        IObjectSpace objectSpace, PersonRecordRole? role, string projectKey,
        ReportDashboardPersonType personType, string subReport,
        string? excelHint, bool excelConfigured, DateTime cutoff,
        bool includeArchivedPersons,
        HashSet<Guid>? validVisaPersonIds = null)
    {
        if (objectSpace is not EFCoreObjectSpace efOs
            || efOs.DbContext is not Visa2026EFCoreDbContext db)
        {
            return LoadEducationLegacy(
                objectSpace, role, projectKey, personType, subReport, excelHint, excelConfigured, cutoff, includeArchivedPersons, validVisaPersonIds);
        }

        IQueryable<VwRdEducationByCountry> query = db.VwRdEducationByCountry.AsNoTracking();

        if (validVisaPersonIds == null)
        {
            var educationIdSet = ReportDashboardRosterQueryHelper.GetLinkedChildIdsInApplicationDateRange(
                db, ApplicationPersonLinkKind.Education, cutoff);
            if (educationIdSet.Count == 0)
            {
                return BuildPanel(
                    personType, ReportDashboardCategory.Education, subReport,
                    new List<ReportDashboardPreviewRow>(),
                    excelHint, excelConfigured,
                    Array.Empty<ReportDashboardStatusBucket>(), 0);
            }

            query = query.Where(r => educationIdSet.Contains(r.ID));
        }
        else
            query = query.Where(r => r.PersonOid != null && validVisaPersonIds.Contains(r.PersonOid.Value));

        if (!includeArchivedPersons)
            query = query.Where(r => !r.IsArchived);

        if (role.HasValue)
            query = query.Where(r => r.PersonRoleCode == (int)role.Value);

        if (!string.IsNullOrWhiteSpace(projectKey) && projectKey != "All")
        {
            query = query.Where(r =>
                r.ProjectNameTm == projectKey
                || r.ProjectName == projectKey
                || r.ProjectNameRaw == projectKey);
        }

        try
        {
            List<VwRdEducationByCountry> list;
            if (validVisaPersonIds != null)
            {
                list = TakeOneCurrentEducationPerPerson(
                    query.ToList(),
                    r => r.PersonOid,
                    r => r.GraduationYear,
                    r => r.ID);
            }
            else
            {
                list = query.ToList();
            }

            var groups = list
                .GroupBy(r => r.CountryLabel)
                .Select(g => new { Label = g.Key, Count = g.Count() })
                .OrderByDescending(g => g.Count)
                .ToList();

            var buckets = AssignCategoricalCss(
                groups.Select(g => (g.Label ?? "Unknown", g.Count)).ToList());
            var cssByLabel = buckets.ToDictionary(b => b.Label, b => b.CssClass, StringComparer.Ordinal);
            var totalCount = buckets.Sum(b => b.Count);

            var rows = list
                .OrderBy(r => r.CountryLabel)
                .ThenBy(r => r.PersonName)
                .Take(PreviewLimit)
                .Select(r =>
                {
                    var status = r.CountryLabel ?? "Unknown";
                    return new ReportDashboardPreviewRow
                    {
                        RecordId = r.ID,
                        Name = r.PersonName ?? string.Empty,
                        Project = r.ProjectName ?? string.Empty,
                        ColumnA = r.InstitutionName ?? string.Empty,
                        ColumnB = r.GraduationYear ?? string.Empty,
                        Status = status,
                        StatusCssClass = cssByLabel.TryGetValue(status, out var c) ? c : "st-cat-1"
                    };
                })
                .ToList();

            return BuildPanel(
                personType, ReportDashboardCategory.Education, subReport, rows,
                excelHint, excelConfigured, buckets, totalCount);
        }
        catch (PostgresException ex) when (ex.SqlState is PostgresErrorCodes.UndefinedTable or PostgresErrorCodes.UndefinedColumn)
        {
            return LoadEducationLegacy(
                objectSpace, role, projectKey, personType, subReport, excelHint, excelConfigured, cutoff,
                includeArchivedPersons, validVisaPersonIds);
        }
    }

    private static ReportDashboardPanelData LoadEducationLegacy(
        IObjectSpace objectSpace, PersonRecordRole? role, string projectKey,
        ReportDashboardPersonType personType, string subReport,
        string? excelHint, bool excelConfigured, DateTime cutoff,
        bool includeArchivedPersons,
        HashSet<Guid>? validVisaPersonIds = null)
    {
        var query = objectSpace.GetObjectsQuery<Education>()
            .Where(e => e.Person != null && (role == null || e.Person.PersonRole == role));

        if (!includeArchivedPersons)
            query = query.Where(e => !e.Person!.IsArchived);

        if (validVisaPersonIds != null)
            query = query.Where(e => validVisaPersonIds.Contains(e.Person!.ID));

        if (!string.IsNullOrWhiteSpace(projectKey) && projectKey != "All")
        {
            query = query.Where(e => e.Person!.ProjectContract != null
                && (e.Person.ProjectContract.Name == projectKey
                    || e.Person.ProjectContract.NameTm == projectKey));
        }

        List<Education> items;
        if (validVisaPersonIds != null)
        {
            // Valid visa only: ignore Last N months; one current education per person.
            items = TakeOneCurrentEducationPerPerson(
                query.AsEnumerable().ToList(),
                e => e.Person?.ID,
                e => e.GraduationYear,
                e => e.ID);
        }
        else if (objectSpace is EFCoreObjectSpace efOs
            && efOs.DbContext is Visa2026EFCoreDbContext db)
        {
            var educationIdSet = ReportDashboardRosterQueryHelper.GetLinkedChildIdsInApplicationDateRange(
                db, ApplicationPersonLinkKind.Education, cutoff);
            items = educationIdSet.Count == 0
                ? new List<Education>()
                : query.Where(e => educationIdSet.Contains(e.ID)).AsEnumerable().ToList();
        }
        else
        {
            var educationIdsInRange = objectSpace.GetObjectsQuery<ApplicationItem>()
                .Where(ai => ai.CurrentEducation != null
                    && ai.Application != null
                    && ai.Application.ApplicationDate >= cutoff)
                .Select(ai => ai.CurrentEducation!.ID)
                .Distinct()
                .ToHashSet();
            items = query.AsEnumerable().Where(e => educationIdsInRange.Contains(e.ID)).ToList();
        }

        static string LevelOf(Education e) =>
            e.EducationLevel?.NameTm ?? e.EducationLevel?.Name ?? "Unknown";
        static string CountryOf(Education e) =>
            e.EducationCountry?.NameTm ?? e.EducationCountry?.Name ?? "Unknown";
        static string SpecialtyOf(Education e) =>
            e.Specialty?.NameTm ?? e.Specialty?.Name ?? "Unknown";
        static string InstitutionOf(Education e) =>
            e.EducationInstitution?.NameTm ?? e.EducationInstitution?.Name ?? string.Empty;

        var groups = items
            .GroupBy(e => subReport switch
            {
                "by-country" => CountryOf(e),
                "by-specialty" => SpecialtyOf(e),
                _ => LevelOf(e)
            })
            .Select(g => (Label: g.Key, Count: g.Count()))
            .OrderByDescending(g => g.Count)
            .ToList();

        var buckets = AssignCategoricalCss(groups);
        var cssByLabel = buckets.ToDictionary(b => b.Label, b => b.CssClass, StringComparer.Ordinal);

        var rows = items
            .OrderBy(e => subReport switch
            {
                "by-country" => CountryOf(e),
                "by-specialty" => SpecialtyOf(e),
                _ => LevelOf(e)
            })
            .ThenBy(e => e.Person?.FullName)
            .Take(PreviewLimit)
            .Select(e =>
            {
                var status = subReport switch
                {
                    "by-country" => CountryOf(e),
                    "by-specialty" => SpecialtyOf(e),
                    _ => LevelOf(e)
                };
                return new ReportDashboardPreviewRow
                {
                    RecordId = e.ID,
                    Name = e.Person?.FullName ?? string.Empty,
                    Project = ProjectLabel(e.Person?.ProjectContract),
                    ColumnA = InstitutionOf(e),
                    ColumnB = e.GraduationYear ?? string.Empty,
                    Status = status,
                    StatusCssClass = cssByLabel.TryGetValue(status, out var c) ? c : "st-cat-1"
                };
            })
            .ToList();

        return BuildPanel(
            personType, ReportDashboardCategory.Education, subReport, rows,
            excelHint, excelConfigured, buckets, buckets.Sum(b => b.Count));
    }

    private static ReportDashboardPanelData LoadPositionHistory(
        IObjectSpace objectSpace, PersonRecordRole? role, string projectKey,
        ReportDashboardPersonType personType, string subReport,
        string? excelHint, bool excelConfigured, DateTime cutoff,
        HashSet<Guid>? validVisaPersonIds = null)
    {
        if (objectSpace is not EFCoreObjectSpace efOs
            || efOs.DbContext is not Visa2026EFCoreDbContext db)
        {
            return EmptyPanel(personType, ReportDashboardCategory.PositionHistory, subReport, excelHint, excelConfigured);
        }

        IQueryable<VwRdPositionHistory> query = db.VwRdPositionHistory
            .AsNoTracking()
            .Where(r => !r.IsArchived);

        if (validVisaPersonIds == null)
        {
            var positionIdSet = ReportDashboardRosterQueryHelper.GetLinkedChildIdsInApplicationDateRange(
                db, ApplicationPersonLinkKind.Position, cutoff);
            if (positionIdSet.Count == 0)
            {
                return BuildPanel(
                    personType, ReportDashboardCategory.PositionHistory, subReport,
                    new List<ReportDashboardPreviewRow>(),
                    excelHint, excelConfigured,
                    Array.Empty<ReportDashboardStatusBucket>(), 0);
            }

            query = query.Where(r => positionIdSet.Contains(r.ID));
        }
        else
        {
            // Valid visa only: ignore Last N months; one current position per valid-visa person.
            query = query.Where(r =>
                r.PersonOid != null && validVisaPersonIds.Contains(r.PersonOid.Value));
        }

        if (role.HasValue)
            query = query.Where(r => r.PersonRoleCode == (int)role.Value);

        if (!string.IsNullOrWhiteSpace(projectKey) && projectKey != "All")
        {
            query = query.Where(r =>
                r.ProjectNameTm == projectKey
                || r.ProjectName == projectKey
                || r.ProjectNameRaw == projectKey);
        }

        try
        {
            var list = query.ToList();
            if (validVisaPersonIds != null)
                list = TakeOneCurrentPositionPerPerson(list);

            if (subReport is "by-position" or "by-actual-position" or "by-status" or "default"
                || string.IsNullOrWhiteSpace(subReport))
            {
                // by-status removed — treat unknown/default as visa Position grouping.
                var byActual = subReport == "by-actual-position";
                var groups = list
                    .GroupBy(r => byActual
                        ? (string.IsNullOrWhiteSpace(r.ActualPositionLabel) ? "Unknown" : r.ActualPositionLabel)
                        : (r.PositionLabel ?? "Unknown"))
                    .Select(g => new { Label = g.Key, Count = g.Count() })
                    .OrderByDescending(g => g.Count)
                    .ToList();

                var buckets = AssignCategoricalCss(
                    groups.Select(g => (g.Label ?? "Unknown", g.Count)).ToList());
                var cssByLabel = buckets.ToDictionary(b => b.Label, b => b.CssClass, StringComparer.Ordinal);
                var totalCount = buckets.Sum(b => b.Count);

                var rows = list
                    .OrderBy(r => byActual ? r.ActualPositionLabel : r.PositionLabel)
                    .ThenByDescending(r => r.StartDate)
                    .Take(PreviewLimit)
                    .Select(r =>
                    {
                        var status = byActual
                            ? (string.IsNullOrWhiteSpace(r.ActualPositionLabel) ? "Unknown" : r.ActualPositionLabel)
                            : (r.PositionLabel ?? "Unknown");
                        return new ReportDashboardPreviewRow
                        {
                            RecordId = r.ID,
                            Name = r.PersonName ?? string.Empty,
                            Project = r.ProjectName ?? string.Empty,
                            ColumnA = r.PositionName ?? string.Empty,
                            ColumnB = FormatDate(r.StartDate),
                            Status = status,
                            StatusCssClass = cssByLabel.TryGetValue(status, out var c) ? c : "st-cat-1"
                        };
                    })
                    .ToList();

                return BuildPanel(
                    personType, ReportDashboardCategory.PositionHistory, subReport, rows,
                    excelHint, excelConfigured, buckets, totalCount);
            }

            return EmptyPanel(personType, ReportDashboardCategory.PositionHistory, subReport, excelHint, excelConfigured);
        }
        catch (PostgresException ex) when (ex.SqlState is PostgresErrorCodes.UndefinedTable or PostgresErrorCodes.UndefinedColumn)
        {
            return EmptyPanel(personType, ReportDashboardCategory.PositionHistory, subReport, excelHint, excelConfigured);
        }
    }

    /// <summary>
    /// One row per Person grouped by <see cref="Person.Subcontractor"/> (By Company).
    /// Master-data category: no ApplicationDate / Last-N filter.
    /// </summary>
    private static ReportDashboardPanelData LoadSubcontractor(
        IObjectSpace objectSpace, PersonRecordRole? role, string projectKey,
        ReportDashboardPersonType personType, string subReport,
        string? excelHint, bool excelConfigured,
        bool includeArchivedPersons,
        HashSet<Guid>? validVisaPersonIds = null)
    {
        var query = objectSpace.GetObjectsQuery<Person>()
            .Where(p => role == null || p.PersonRole == role);

        if (!includeArchivedPersons)
            query = query.Where(p => !p.IsArchived);

        if (validVisaPersonIds != null)
            query = query.Where(p => validVisaPersonIds.Contains(p.ID));

        if (!string.IsNullOrWhiteSpace(projectKey) && projectKey != "All")
        {
            query = query.Where(p => p.ProjectContract != null
                && (p.ProjectContract.Name == projectKey
                    || p.ProjectContract.NameTm == projectKey));
        }

        var list = query.AsEnumerable().ToList();

        static string CompanyOf(Person p) =>
            p.Subcontractor?.NameTm
            ?? p.Subcontractor?.Name
            ?? "Unassigned";

        static string RoleOf(Person p) => p.PersonRole switch
        {
            PersonRecordRole.Employee => "Employee",
            PersonRecordRole.FamilyMember => "Family Member",
            PersonRecordRole.TemporaryVisitor => "Temporary Visitor",
            _ => p.PersonRole.ToString()
        };

        var groups = list
            .GroupBy(CompanyOf)
            .Select(g => (Label: g.Key, Count: g.Count()))
            .OrderByDescending(g => g.Count)
            .ToList();

        var buckets = AssignCategoricalCss(groups);
        var cssByLabel = buckets.ToDictionary(b => b.Label, b => b.CssClass, StringComparer.Ordinal);
        var totalCount = buckets.Sum(b => b.Count);

        var rows = list
            .OrderBy(CompanyOf)
            .ThenBy(p => p.FullName)
            .Take(PreviewLimit)
            .Select(p =>
            {
                var status = CompanyOf(p);
                return new ReportDashboardPreviewRow
                {
                    RecordId = p.ID,
                    Name = p.FullName ?? string.Empty,
                    Project = ProjectLabel(p.ProjectContract),
                    ColumnA = RoleOf(p),
                    ColumnB = FormatDate(p.HireDate == default ? null : p.HireDate),
                    Status = status,
                    StatusCssClass = cssByLabel.TryGetValue(status, out var c) ? c : "st-cat-1"
                };
            })
            .ToList();

        return BuildPanel(
            personType, ReportDashboardCategory.Subcontractor, subReport, rows,
            excelHint, excelConfigured, buckets, totalCount);
    }

    /// <summary>
    /// Incomplete persons: one Preview row per Person; chart buckets count each checked missing-area flag.
    /// </summary>
    private static ReportDashboardPanelData LoadIncompletePersons(
        IObjectSpace objectSpace, PersonRecordRole? role, string projectKey,
        ReportDashboardPersonType personType, string subReport,
        string? excelHint, bool excelConfigured)
    {
        if (objectSpace is not EFCoreObjectSpace efOs
            || efOs.DbContext is not Visa2026EFCoreDbContext db)
        {
            return EmptyPanel(personType, ReportDashboardCategory.IncompletePersons, subReport, excelHint, excelConfigured);
        }

        IQueryable<VwRdIncompletePersonsByMissingArea> query =
            db.VwRdIncompletePersonsByMissingArea.AsNoTracking();

        // No Valid visa only / Include archived toggles — show every incomplete person.
        if (role != null)
            query = query.Where(r => r.PersonRoleCode == (int)role.Value);

        if (!string.IsNullOrWhiteSpace(projectKey) && projectKey != "All")
        {
            query = query.Where(r =>
                r.ProjectName == projectKey
                || r.ProjectNameTm == projectKey
                || r.ProjectNameRaw == projectKey);
        }

        var list = query.AsEnumerable().ToList();

        var groups = new List<(string Label, int Count)>();
        void AddFlag(string label, Func<VwRdIncompletePersonsByMissingArea, bool> pred)
        {
            var count = list.Count(pred);
            if (count > 0)
                groups.Add((label, count));
        }

        AddFlag(PersonIncompleteDataLabels.PersonalData, r => r.MissingPersonalData);
        AddFlag(PersonIncompleteDataLabels.Passport, r => r.MissingPassport);
        AddFlag(PersonIncompleteDataLabels.Cv, r => r.MissingCv);
        AddFlag(PersonIncompleteDataLabels.Photo, r => r.MissingPhoto);
        AddFlag(PersonIncompleteDataLabels.Education, r => r.MissingEducation);
        AddFlag(PersonIncompleteDataLabels.Medical, r => r.MissingMedical);
        AddFlag(PersonIncompleteDataLabels.Address, r => r.MissingAddress);
        AddFlag(PersonIncompleteDataLabels.FamilyDocs, r => r.MissingFamilyDocs);
        AddFlag(PersonIncompleteDataLabels.Other, r => r.MissingOther);

        groups = groups.OrderByDescending(g => g.Count).ToList();
        var buckets = AssignCategoricalCss(groups);
        var totalCount = list.Count;

        var rows = list
            .OrderBy(r => r.PersonName)
            .Take(PreviewLimit)
            .Select(r => new ReportDashboardPreviewRow
            {
                RecordId = r.ID,
                Name = r.PersonName ?? string.Empty,
                Project = r.PersonTypeLabel ?? string.Empty,
                ColumnA = r.MissingAreasLabel ?? string.Empty,
                ColumnB = r.IncompleteNotes ?? string.Empty,
                Status = r.MarkedLabel ?? string.Empty,
                StatusCssClass = "st-pending"
            })
            .ToList();

        return BuildPanel(
            personType, ReportDashboardCategory.IncompletePersons, subReport, rows,
            excelHint, excelConfigured, buckets, totalCount);
    }

    /// <summary>
    /// Person search: one Preview row per Person, narrowed by the officer's term.
    /// An empty term lists everyone under the current person-type tab and project chip.
    /// Chart buckets follow the person's current visa status.
    /// </summary>
    private static ReportDashboardPanelData LoadPersonSearch(
        IObjectSpace objectSpace, PersonRecordRole? role, string projectKey,
        ReportDashboardPersonType personType, string subReport,
        string? excelHint, bool excelConfigured, string? searchTerm)
    {
        if (objectSpace is not EFCoreObjectSpace efOs
            || efOs.DbContext is not Visa2026EFCoreDbContext db)
        {
            return EmptyPanel(personType, ReportDashboardCategory.PersonSearch, subReport, excelHint, excelConfigured);
        }

        IQueryable<VwRdPersonSearch> query = db.VwRdPersonSearch.AsNoTracking();

        if (role != null)
            query = query.Where(r => r.PersonRoleCode == (int)role.Value);

        if (!string.IsNullOrWhiteSpace(projectKey) && projectKey != "All")
        {
            query = query.Where(r =>
                r.ProjectName == projectKey
                || r.ProjectNameTm == projectKey
                || r.ProjectNameRaw == projectKey);
        }

        // Every token must match, same rule as BuildListCriteria so the drill-down Total agrees.
        foreach (var token in ReportDashboardCatalog.PersonSearchTokens(searchTerm))
        {
            var needle = token;
            query = query.Where(r => r.SearchText != null && r.SearchText.Contains(needle));
        }

        var totalCount = query.Count();

        var counts = query
            .GroupBy(r => r.StatusLabel)
            .Select(g => new { Label = g.Key, Count = g.Count() })
            .ToList()
            .ToDictionary(x => x.Label ?? string.Empty, x => x.Count, StringComparer.Ordinal);

        var buckets = PersonSearchStatusOrder
            .Where(counts.ContainsKey)
            .Select(label => new ReportDashboardStatusBucket
            {
                Label = label,
                Count = counts[label],
                CssClass = PersonSearchStatusCss(label)
            })
            .ToList();

        var rows = query
            .OrderBy(r => r.PersonName)
            .Take(PreviewLimit)
            .ToList()
            .Select(r => new ReportDashboardPreviewRow
            {
                RecordId = r.PersonOid ?? r.ID,
                Name = r.PersonName ?? string.Empty,
                Project = r.ProjectName ?? string.Empty,
                ColumnA = r.PersonalNumber ?? string.Empty,
                ColumnB = r.PassportNumber ?? string.Empty,
                ColumnC = r.VisaExpiryLabel ?? string.Empty,
                Status = r.StatusLabel ?? string.Empty,
                StatusCssClass = r.StatusCssClass ?? string.Empty
            })
            .ToList();

        return BuildPanel(
            personType, ReportDashboardCategory.PersonSearch, subReport, rows,
            excelHint, excelConfigured, buckets, totalCount);
    }

    /// <summary>Status ladder for Person search chart buckets (best to worst).</summary>
    private static readonly string[] PersonSearchStatusOrder =
    [
        "Valid",
        "Expiring (<30 days)",
        "Expired",
        ReportDashboardCatalog.PersonSearchNoVisaLabel
    ];

    private static string PersonSearchStatusCss(string label) => label switch
    {
        "Valid" => "st-approved",
        "Expiring (<30 days)" => "st-pending",
        "Expired" => "st-expiring",
        _ => string.Empty
    };

    /// <summary>
    /// Medical records by validity. Last-N uses ApplicationItem.CurrentMedicalRecord;
    /// Valid visa only ignores Last-N and keeps one current medical per person
    /// (latest IssueDate then ID — same as PersonCurrentItems.GetCurrentMedicalRecord).
    /// </summary>
    private static ReportDashboardPanelData LoadMedicalRecord(
        IObjectSpace objectSpace, PersonRecordRole? role, string projectKey,
        ReportDashboardPersonType personType, string subReport,
        string? excelHint, bool excelConfigured, DateTime cutoff,
        bool includeArchivedPersons,
        HashSet<Guid>? validVisaPersonIds = null)
    {
        if (objectSpace is not EFCoreObjectSpace efOs
            || efOs.DbContext is not Visa2026EFCoreDbContext db)
        {
            return EmptyPanel(personType, ReportDashboardCategory.MedicalRecord, subReport, excelHint, excelConfigured);
        }

        HashSet<Guid>? medicalIdSet = null;
        if (validVisaPersonIds == null)
        {
            medicalIdSet = ReportDashboardRosterQueryHelper.GetLinkedChildIdsInApplicationDateRange(
                db, ApplicationPersonLinkKind.MedicalRecord, cutoff);
        }

        if (medicalIdSet is { Count: 0 })
        {
            return BuildPanel(
                personType, ReportDashboardCategory.MedicalRecord, subReport,
                new List<ReportDashboardPreviewRow>(),
                excelHint, excelConfigured,
                Array.Empty<ReportDashboardStatusBucket>(), 0);
        }

        IQueryable<MedicalRecord> query = db.MedicalRecords
            .AsNoTracking()
            .Where(m => m.Person != null);

        if (medicalIdSet != null)
            query = query.Where(m => medicalIdSet.Contains(m.ID));

        if (role.HasValue)
            query = query.Where(m => m.Person!.PersonRole == role.Value);

        if (!includeArchivedPersons)
            query = query.Where(m => !m.Person!.IsArchived);

        if (validVisaPersonIds != null)
            query = query.Where(m => validVisaPersonIds.Contains(m.Person!.ID));

        if (!string.IsNullOrWhiteSpace(projectKey) && projectKey != "All")
        {
            query = query.Where(m => m.Person!.ProjectContract != null
                && (m.Person.ProjectContract.Name == projectKey
                    || m.Person.ProjectContract.NameTm == projectKey));
        }

        var items = query
            .Include(m => m.Person).ThenInclude(p => p!.ProjectContract)
            .AsEnumerable()
            .ToList();
        var today = DateTime.Today;

        if (validVisaPersonIds != null)
            items = TakeOneCurrentMedicalPerPerson(items);

        var validityGroups = items
            .GroupBy(m => ExpirationBucket(m.ExpirationDate, today))
            .Select(g => (Label: g.Key, Count: g.Count()))
            .OrderByDescending(g => g.Count)
            .ToList();

        var validityBuckets = validityGroups
            .Select(g => new ReportDashboardStatusBucket
            {
                Label = g.Label,
                Count = g.Count,
                CssClass = StatusCss(g.Label, null)
            })
            .ToList();
        var cssByValidity = validityBuckets.ToDictionary(b => b.Label, b => b.CssClass, StringComparer.Ordinal);

        var previewRows = items
            .OrderBy(m => m.ExpirationDate ?? DateTime.MaxValue)
            .ThenBy(m => m.Person?.FullName)
            .Take(PreviewLimit)
            .Select(m =>
            {
                var status = ExpirationBucket(m.ExpirationDate, today);
                return new ReportDashboardPreviewRow
                {
                    RecordId = m.ID,
                    Name = m.Person?.FullName ?? string.Empty,
                    Project = ProjectLabel(m.Person?.ProjectContract),
                    ColumnA = m.DocumentNumber ?? string.Empty,
                    ColumnB = FormatDate(m.ExpirationDate),
                    Status = status,
                    StatusCssClass = cssByValidity.TryGetValue(status, out var c) ? c : StatusCss(status, m.DaysRemaining)
                };
            })
            .ToList();

        return BuildPanel(
            personType, ReportDashboardCategory.MedicalRecord, subReport, previewRows,
            excelHint, excelConfigured, validityBuckets, validityBuckets.Sum(b => b.Count));
    }

    /// <summary>
    /// One medical record per person using PersonCurrentItems.GetCurrentMedicalRecord ranking
    /// (latest IssueDate, then ID).
    /// </summary>
    private static List<MedicalRecord> TakeOneCurrentMedicalPerPerson(IEnumerable<MedicalRecord> rows) =>
        rows
            .Where(m => m.Person != null)
            .GroupBy(m => m.Person!.ID)
            .Select(g => g
                .OrderByDescending(m => m.IssueDate == default ? DateTime.MinValue : m.IssueDate.Date)
                .ThenByDescending(m => m.ID)
                .First())
            .ToList();

    private static ReportDashboardPanelData EmptyPanel(
        ReportDashboardPersonType personType,
        ReportDashboardCategory category,
        string subReport,
        string? excelHint,
        bool excelConfigured) =>
        BuildPanel(personType, category, subReport, [], excelHint, excelConfigured);

    // ---- Helpers ---------------------------------------------------------

    private static string ProjectLabel(ProjectContract? project)
    {
        if (project == null) return string.Empty;
        if (!string.IsNullOrWhiteSpace(project.NameTm)) return project.NameTm!;
        return project.Name ?? string.Empty;
    }

    private static string FormatDate(DateTime? value) =>
        value.HasValue ? value.Value.ToString("MMM dd, yyyy") : string.Empty;

    /// <summary>4-bucket passport validity classification.</summary>
    private static string PassportValidityBucket(DateTime? expiration, DateTime today)
    {
        if (!expiration.HasValue) return "Pending";
        var days = (expiration.Value.Date - today).Days;
        if (days < 0)  return "Expired";
        if (days < 30) return "Expiring (<30 days)";
        if (days <= 90) return "Valid (31-90 days)";
        return "Valid (>90 days)";
    }

    private static string PassportValidityCss(DateTime? expiration, DateTime today)
    {
        if (!expiration.HasValue) return "st-pending";
        var days = (expiration.Value.Date - today).Days;
        if (days < 0)  return "st-expiring";
        if (days < 30) return "st-expiring";
        if (days <= 90) return "st-pending";
        return "st-approved";
    }

    private static string ExpirationBucket(DateTime? expiration, DateTime today)
    {
        if (!expiration.HasValue) return "Pending";
        if (expiration.Value.Date < today)           return "Expired";
        if (expiration.Value.Date <= today.AddDays(30)) return "Expiring (<30 days)";
        if (expiration.Value.Date <= today.AddDays(90)) return "Expiring Soon";
        return "Approved";
    }

    /// <summary>
    /// Private House Validity: empty expiry → ExpirationNotSet; otherwise Valid / Expiring / Expired
    /// using AddressOfResidence document-expiration alert days (default 30).
    /// </summary>
    private static string PrivateHouseValidityBucket(DateTime? expiration, DateTime today, int expiringSoonDays)
    {
        if (!expiration.HasValue)
            return "ExpirationNotSet";
        var days = (expiration.Value.Date - today).Days;
        if (days < 0)
            return "Expired";
        if (days <= expiringSoonDays)
            return "Expiring";
        return "Valid";
    }

    private static string PrivateHouseValidityCss(string status) => status switch
    {
        "Valid" => "st-approved",
        "Expiring" => "st-expiring",
        "Expired" => "st-expiring",
        "ExpirationNotSet" => "st-pending",
        _ => "st-pending"
    };

    private static string StatusCss(string status, int? daysRemaining)
    {
        if (daysRemaining is < 30)                                                          return "st-expiring";
        if (status.Contains("Expir",   StringComparison.OrdinalIgnoreCase))                return "st-expiring";
        if (status.Contains("Expired", StringComparison.OrdinalIgnoreCase))                return "st-expiring";
        if (status.Contains("Pending", StringComparison.OrdinalIgnoreCase)
            || status.Contains("Ministry", StringComparison.OrdinalIgnoreCase))            return "st-pending";
        if (status.Contains("Approved", StringComparison.OrdinalIgnoreCase)
            || status.Contains("Active",    StringComparison.OrdinalIgnoreCase)
            || status.Contains("Completed", StringComparison.OrdinalIgnoreCase))           return "st-approved";
        return "st-pending";
    }

    private static bool TryGetDbContext(IObjectSpace objectSpace, out Visa2026EFCoreDbContext db)
    {
        if (objectSpace is EFCoreObjectSpace efOs && efOs.DbContext is Visa2026EFCoreDbContext context)
        {
            db = context;
            return true;
        }

        db = null!;
        return false;
    }
}