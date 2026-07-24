using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.EFCore;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services.ReportDashboard;

public sealed class ReportDashboardQueryService : IReportDashboardQueryService
{
    private const int PreviewLimit = 50;

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
            ReportDashboardCategory.Application =>
                objectSpace.GetObjectsQuery<Application>()
                    .Count(a => a.ApplicationDate >= cutoff),
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
                objectSpace.GetObjectsQuery<ApplicationItem>()
                    .Count(a => a.Person != null && (role == null || a.Person.PersonRole == role)
                                && a.TravelDate != null && a.TravelDate >= cutoff),
            ReportDashboardCategory.AddressOfResidence =>
                objectSpace.GetObjectsQuery<ApplicationItem>()
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
                objectSpace.GetObjectsQuery<ApplicationItem>()
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
        bool validVisaPersonsOnly = true)
    {
        var cutoff = DateTime.Today.AddMonths(-dateRangeMonths);
        var role = ReportDashboardCatalog.TryGetPersonRole(personType);
        var validVisaPersonIds = ResolveValidVisaPersonIds(objectSpace, category, validVisaPersonsOnly);
        var excelHint = ReportDashboardCatalog.ExcelTemplateNameHint(category);
        var excelConfigured = !string.IsNullOrEmpty(excelHint)
            && objectSpace.GetObjectsQuery<UserReportTemplate>()
                .Any(t => t.TemplateName != null
                    && t.TemplateName.Contains(excelHint)
                    && t.TemplateOutputFormat == TemplateOutputFormat.Excel);

        return category switch
        {
            ReportDashboardCategory.Application   => LoadApplication(
                objectSpace, role, projectKey, personType, subReport, excelHint, excelConfigured, cutoff,
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
    /// </summary>
    private static ReportDashboardPanelData LoadApplication(
        IObjectSpace objectSpace, PersonRecordRole? role, string projectKey,
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

        var query = FilterApplications(
            objectSpace, role, projectKey, cutoff,
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
            personType, ReportDashboardCategory.Application, subReport, rows,
            excelHint, excelConfigured, buckets, totalCount, "Application Status");
    }

    private static IQueryable<Application> FilterApplications(
        IObjectSpace objectSpace,
        PersonRecordRole? role,
        string projectKey,
        DateTime cutoff,
        bool includeCompletedApplicationProcesses,
        bool includeCancelledApplicationProcesses)
    {
        var query = objectSpace.GetObjectsQuery<Application>()
            .Where(a => a.ApplicationDate == null || a.ApplicationDate >= cutoff);

        if (role.HasValue)
        {
            var roleValue = role.Value;
            query = query.Where(a => a.ApplicationItems.Any(ai =>
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
        if (subReport is "visa-state" or "default" or "app-progress" || string.IsNullOrWhiteSpace(subReport))
        {
            // app-progress removed from Visa tabs — fall through to Visa State.
            return LoadVisaStateFromView(
                objectSpace, role, projectKey, personType, subReport, excelHint, excelConfigured, cutoff);
        }

        if (subReport is "by-category")
        {
            return LoadVisaByCategoryFromView(
                objectSpace, role, projectKey, personType, subReport, excelHint, excelConfigured, cutoff,
                oneLastValidVisaPerPerson);
        }

        if (subReport is "by-type")
        {
            return LoadVisaByTypeFromView(
                objectSpace, role, projectKey, personType, subReport, excelHint, excelConfigured, cutoff,
                oneLastValidVisaPerPerson);
        }

        if (subReport is "by-period")
        {
            return LoadVisaByPeriodFromView(
                objectSpace, role, projectKey, personType, subReport, excelHint, excelConfigured, cutoff,
                oneLastValidVisaPerPerson);
        }

        if (subReport is "by-days-remaining")
        {
            return LoadVisaByDaysRemainingFromView(
                objectSpace, role, projectKey, personType, subReport, excelHint, excelConfigured, cutoff,
                oneLastValidVisaPerPerson);
        }

        return LoadVisaExtensionLegacy(
            objectSpace, role, projectKey, personType, subReport, excelHint, excelConfigured, cutoff);
    }

    /// <summary>
    /// Valid visas from <c>vw_rd_visa_by_category</c>
    /// (Status = VisaCategory only — not Visa State; multiple valid visas per person allowed).
    /// </summary>
    private static ReportDashboardPanelData LoadVisaByCategoryFromView(
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
        IQueryable<VwRdVisaByCategory> query = db.VwRdVisaByCategory
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
            var list = query.ToList();
            if (oneLastValidVisaPerPerson)
                list = TakeOneLastValidVisaPerPerson(list, r => r.PersonOid, r => r.ExpirationDate, r => r.ID);

            var catRows = list
                .GroupBy(r => r.CategoryLabel)
                .Select(g => new { Label = g.Key, Count = g.Count() })
                .OrderByDescending(g => g.Count)
                .ToList();

            var buckets = AssignCategoricalCss(
                catRows.Select(t => (t.Label ?? string.Empty, t.Count)).ToList());
            var totalCount = buckets.Sum(b => b.Count);
            var cssByLabel = buckets.ToDictionary(b => b.Label, b => b.CssClass, StringComparer.Ordinal);

            var rows = list
                .OrderBy(r => r.CategoryLabel)
                .ThenBy(r => r.PersonName)
                .ThenBy(r => r.ExpirationDate)
                .Take(PreviewLimit)
                .Select(r =>
                {
                    var status = r.CategoryLabel ?? string.Empty;
                    return new ReportDashboardPreviewRow
                    {
                        RecordId = r.ID,
                        Name = r.PersonName ?? string.Empty,
                        Project = r.ProjectName ?? string.Empty,
                        ColumnA = r.VisaNumber ?? string.Empty,
                        ColumnB = FormatDate(r.ExpirationDate),
                        Status = status,
                        StatusCssClass = cssByLabel.TryGetValue(status, out var c) ? c : "st-cat-1"
                    };
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
    /// Valid visas from <c>vw_rd_visa_by_type</c>
    /// (Status = VisaType only — not Visa State; multiple valid visas per person allowed).
    /// </summary>
    private static ReportDashboardPanelData LoadVisaByTypeFromView(
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
        IQueryable<VwRdVisaByType> query = db.VwRdVisaByType
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
            var list = query.ToList();
            if (oneLastValidVisaPerPerson)
                list = TakeOneLastValidVisaPerPerson(list, r => r.PersonOid, r => r.ExpirationDate, r => r.ID);

            var typeRows = list
                .GroupBy(r => r.TypeLabel)
                .Select(g => new { Label = g.Key, Count = g.Count() })
                .OrderByDescending(g => g.Count)
                .ToList();

            var buckets = AssignCategoricalCss(
                typeRows.Select(t => (t.Label ?? string.Empty, t.Count)).ToList());
            var totalCount = buckets.Sum(b => b.Count);
            var cssByLabel = buckets.ToDictionary(b => b.Label, b => b.CssClass, StringComparer.Ordinal);

            var rows = list
                .OrderBy(r => r.TypeLabel)
                .ThenBy(r => r.PersonName)
                .ThenBy(r => r.ExpirationDate)
                .Take(PreviewLimit)
                .Select(r =>
                {
                    var status = r.TypeLabel ?? string.Empty;
                    return new ReportDashboardPreviewRow
                    {
                        RecordId = r.ID,
                        Name = r.PersonName ?? string.Empty,
                        Project = r.ProjectName ?? string.Empty,
                        ColumnA = r.VisaNumber ?? string.Empty,
                        ColumnB = FormatDate(r.ExpirationDate),
                        Status = status,
                        StatusCssClass = cssByLabel.TryGetValue(status, out var c) ? c : "st-cat-1"
                    };
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
    /// Valid visas from <c>vw_rd_visa_by_period</c>
    /// (Status = nearest granted period from Start→End: 1 month / 3 months / 6 months / 1 year).
    /// </summary>
    private static ReportDashboardPanelData LoadVisaByPeriodFromView(
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
        IQueryable<VwRdVisaByPeriod> query = db.VwRdVisaByPeriod
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
            var list = query.ToList();
            if (oneLastValidVisaPerPerson)
                list = TakeOneLastValidVisaPerPerson(list, r => r.PersonOid, r => r.ExpirationDate, r => r.ID);

            var statusRows = list
                .GroupBy(r => new { r.StatusLabel, r.StatusCssClass })
                .Select(g => new
                {
                    Label = g.Key.StatusLabel,
                    CssClass = g.Key.StatusCssClass,
                    Count = g.Count()
                })
                .ToList();

            static int PeriodSortKey(string? label) => label switch
            {
                "1 month" => 1,
                "3 months" => 2,
                "6 months" => 3,
                "1 year" => 4,
                _ => 99
            };

            var buckets = statusRows
                .Select(g => new ReportDashboardStatusBucket
                {
                    Label = g.Label ?? string.Empty,
                    CssClass = g.CssClass ?? "st-cat-1",
                    Count = g.Count
                })
                .OrderBy(b => PeriodSortKey(b.Label))
                .ToList();

            var totalCount = buckets.Sum(b => b.Count);

            var rows = list
                .OrderBy(r => r.PeriodDays)
                .ThenBy(r => r.PersonName)
                .Take(PreviewLimit)
                .Select(r => new ReportDashboardPreviewRow
                {
                    RecordId = r.ID,
                    Name = r.PersonName ?? string.Empty,
                    Project = r.ProjectName ?? string.Empty,
                    ColumnA = r.VisaNumber ?? string.Empty,
                    ColumnB = FormatDate(r.ExpirationDate),
                    Status = r.StatusLabel ?? string.Empty,
                    StatusCssClass = r.StatusCssClass ?? "st-cat-1"
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

        try
        {
            var list = query.ToList();
            if (oneLastValidVisaPerPerson)
                list = TakeOneLastValidVisaPerPerson(list, r => r.PersonOid, r => r.ExpirationDate, r => r.ID);

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
                    ColumnA = r.VisaNumber ?? string.Empty,
                    ColumnB = FormatDate(r.ExpirationDate),
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
    /// Loads Visa Application Progress from <c>vw_rd_visa_app_progress</c>
    /// (ApplicationItems on visa-extension types with CurrentVisa; latest progress state).
    /// </summary>
    private static ReportDashboardPanelData LoadVisaAppProgressFromView(
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

        IQueryable<VwRdVisaAppProgress> query = db.VwRdVisaAppProgress
            .AsNoTracking()
            .Where(r => !r.IsArchived)
            .Where(r => r.ApplicationDate == null || r.ApplicationDate >= cutoff);
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
            var progressRows = query
                .GroupBy(r => new { r.ProgressStateLabel, r.ProgressStateCssClass })
                .Select(g => new
                {
                    Label = g.Key.ProgressStateLabel,
                    CssClass = g.Key.ProgressStateCssClass,
                    Count = g.Count()
                })
                .ToList();

            var buckets = progressRows
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
                .OrderByDescending(r => r.ApplicationDate)
                .Take(PreviewLimit)
                .AsEnumerable()
                .Select(r => new ReportDashboardPreviewRow
                {
                    RecordId = r.ID,
                    Name = r.PersonName ?? string.Empty,
                    Project = r.ProjectName ?? string.Empty,
                    ColumnA = r.ApplicationNumber ?? string.Empty,
                    ColumnB = FormatDate(r.ApplicationDate),
                    Status = r.ProgressStateLabel ?? string.Empty,
                    StatusCssClass = r.ProgressStateCssClass ?? "st-pending"
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

        return subReport switch
        {
            "in-process" => LoadInvitationInProcessFromView(
                objectSpace, role, projectKey, personType, subReport, excelHint, excelConfigured, cutoff),
            "rejected-by-project" => LoadInvitationRejectedFromView(
                objectSpace, role, projectKey, personType, subReport, excelHint, excelConfigured, cutoff),
            "used" => LoadInvitationUsedFromView(
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

            var groups = list
                .GroupBy(r => string.IsNullOrWhiteSpace(r.StatusLabel) ? "(No project)" : r.StatusLabel!.Trim(),
                    StringComparer.Ordinal)
                .Select(g => (Label: g.Key, Count: g.Count()))
                .OrderByDescending(g => g.Count)
                .ToList();
            var buckets = AssignCategoricalCss(groups);
            var cssByLabel = buckets.ToDictionary(b => b.Label, b => b.CssClass, StringComparer.Ordinal);

            var rows = list
                .OrderByDescending(r => r.IssuedDate)
                .ThenBy(r => r.PersonName)
                .Take(PreviewLimit)
                .Select(r =>
                {
                    var status = string.IsNullOrWhiteSpace(r.StatusLabel) ? "(No project)" : r.StatusLabel!.Trim();
                    return new ReportDashboardPreviewRow
                    {
                        RecordId = r.ID,
                        Name = r.PersonName ?? string.Empty,
                        Project = string.IsNullOrWhiteSpace(r.ProjectName) ? status : r.ProjectName,
                        ColumnA = r.InvitationNumber ?? string.Empty,
                        ColumnB = FormatDate(r.IssuedDate),
                        Status = status,
                        StatusCssClass = cssByLabel.TryGetValue(status, out var c) ? c : "st-cat-1"
                    };
                })
                .ToList();

            return BuildPanel(
                personType, ReportDashboardCategory.Invitation, subReport, rows,
                excelHint, excelConfigured, buckets, list.Count);
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

    private static ReportDashboardPanelData LoadInvitationInProcessFromView(
        IObjectSpace objectSpace, PersonRecordRole? role, string projectKey,
        ReportDashboardPersonType personType, string subReport,
        string? excelHint, bool excelConfigured, DateTime cutoff)
    {
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

            var groups = list
                .GroupBy(r => string.IsNullOrWhiteSpace(r.StatusLabel) ? "Being Prepared" : r.StatusLabel!.Trim(),
                    StringComparer.Ordinal)
                .Select(g => (Label: g.Key, Count: g.Count()))
                .OrderByDescending(g => g.Count)
                .ToList();
            var buckets = AssignCategoricalCss(groups);
            var cssByLabel = buckets.ToDictionary(b => b.Label, b => b.CssClass, StringComparer.Ordinal);

            var rows = list
                .OrderByDescending(r => r.ApplicationDate)
                .ThenBy(r => r.PersonName)
                .Take(PreviewLimit)
                .Select(r =>
                {
                    var status = string.IsNullOrWhiteSpace(r.StatusLabel) ? "Being Prepared" : r.StatusLabel!.Trim();
                    var progressCss = StatusCss(status, null);
                    return new ReportDashboardPreviewRow
                    {
                        RecordId = r.ID,
                        Name = r.PersonName ?? string.Empty,
                        Project = r.ProjectName ?? string.Empty,
                        ColumnA = r.ApplicationNumber ?? string.Empty,
                        ColumnB = FormatDate(r.ApplicationDate),
                        Status = status,
                        StatusCssClass = string.IsNullOrWhiteSpace(progressCss)
                            ? (cssByLabel.TryGetValue(status, out var c) ? c : "st-pending")
                            : progressCss
                    };
                })
                .ToList();

            return BuildPanel(
                personType, ReportDashboardCategory.Invitation, subReport, rows,
                excelHint, excelConfigured, buckets, list.Count);
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
        // Invitation-issuing applications still in progress (not issued / rejected / cancelled)
        // and with no Invitation header linked yet.
        var query = objectSpace.GetObjectsQuery<Application>()
            .Where(a => a.ApplicationType != null
                        && a.ApplicationType.CanIssueInvitation
                        && (a.ApplicationDate == null || a.ApplicationDate >= cutoff)
                        && !a.Invitations.Any()
                        && (a.LatestProgress == null
                            || a.LatestProgress.State == null
                            || (a.LatestProgress.State.Code != ApplicationProgressStateCodes.ProcessIssued
                                && a.LatestProgress.State.Code != ApplicationProgressStateCodes.ProcessRejected
                                && a.LatestProgress.State.Code != ApplicationProgressStateCodes.ProcessCancelled)));

        if (role.HasValue)
        {
            var roleValue = role.Value;
            query = query.Where(a => a.ApplicationItems.Any(ai =>
                ai.Person != null && ai.Person.PersonRole == roleValue));
        }

        if (!string.IsNullOrWhiteSpace(projectKey) && projectKey != "All")
        {
            query = query.Where(a => a.ProjectContract != null
                && (a.ProjectContract.Name == projectKey || a.ProjectContract.NameTm == projectKey));
        }

        var labeled = query.AsEnumerable().Select(a =>
        {
            var status = string.IsNullOrWhiteSpace(a.CurrentState) ? "Being Prepared" : a.CurrentState.Trim();
            return (App: a, Status: status);
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
                var progressCss = StatusCss(x.Status, null);
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

    private static ReportDashboardPanelData LoadInvitationRejectedFromView(
        IObjectSpace objectSpace, PersonRecordRole? role, string projectKey,
        ReportDashboardPersonType personType, string subReport,
        string? excelHint, bool excelConfigured, DateTime cutoff)
    {
        if (objectSpace is not EFCoreObjectSpace efOs
            || efOs.DbContext is not Visa2026EFCoreDbContext db)
        {
            return LoadInvitationRejectedByProject(
                objectSpace, role, projectKey, personType, subReport, excelHint, excelConfigured, cutoff);
        }

        try
        {
            IQueryable<VwRdInvitationRejected> query = db.VwRdInvitationRejected.AsNoTracking();

            query = query.Where(r => !r.IsArchived);

            if (cutoff > DateTime.MinValue)
                query = query.Where(r => r.RecordDate == null || r.RecordDate >= cutoff);

            if (!string.IsNullOrWhiteSpace(projectKey) && projectKey != "All")
            {
                query = query.Where(r =>
                    r.ProjectNameTm == projectKey
                    || r.ProjectName == projectKey
                    || r.ProjectNameRaw == projectKey
                    || r.StatusLabel == projectKey);
            }

            var list = query.ToList();
            if (role.HasValue)
            {
                var roleValue = role.Value;
                var roleCode = (int)roleValue;
                var appIdsWithRole = db.ApplicationItems
                    .AsNoTracking()
                    .Where(ai => ai.Application != null && ai.Person != null && ai.Person.PersonRole == roleValue)
                    .Select(ai => ai.Application!.ID)
                    .Distinct()
                    .ToHashSet();
                list = list.Where(r =>
                    string.Equals(r.SourceKind, "application", StringComparison.OrdinalIgnoreCase)
                        ? appIdsWithRole.Contains(r.ID)
                        : r.PersonRoleCode == roleCode).ToList();
            }

            var groups = list
                .GroupBy(r => string.IsNullOrWhiteSpace(r.StatusLabel) ? "(No project)" : r.StatusLabel!.Trim(),
                    StringComparer.Ordinal)
                .Select(g => (Label: g.Key, Count: g.Count()))
                .OrderByDescending(g => g.Count)
                .ToList();
            var buckets = AssignCategoricalCss(groups);
            var cssByLabel = buckets.ToDictionary(b => b.Label, b => b.CssClass, StringComparer.Ordinal);

            var rows = list
                .OrderByDescending(r => r.RecordDate)
                .ThenBy(r => r.PersonName)
                .Take(PreviewLimit)
                .Select(r =>
                {
                    var status = string.IsNullOrWhiteSpace(r.StatusLabel) ? "(No project)" : r.StatusLabel!.Trim();
                    return new ReportDashboardPreviewRow
                    {
                        RecordId = r.ID,
                        Name = r.PersonName ?? string.Empty,
                        Project = string.IsNullOrWhiteSpace(r.ProjectName) ? status : r.ProjectName,
                        ColumnA = r.DocumentNumber ?? string.Empty,
                        ColumnB = FormatDate(r.RecordDate),
                        Status = status,
                        StatusCssClass = cssByLabel.TryGetValue(status, out var c) ? c : "st-cat-1"
                    };
                })
                .ToList();

            return BuildPanel(
                personType, ReportDashboardCategory.Invitation, subReport, rows,
                excelHint, excelConfigured, buckets, list.Count);
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UndefinedTable
            || ex.SqlState == PostgresErrorCodes.UndefinedColumn)
        {
            return LoadInvitationRejectedByProject(
                objectSpace, role, projectKey, personType, subReport, excelHint, excelConfigured, cutoff);
        }
        catch (Exception ex) when (ex.InnerException is Microsoft.Data.SqlClient.SqlException sqlEx
            && (sqlEx.Number == 208 || sqlEx.Number == 207))
        {
            return LoadInvitationRejectedByProject(
                objectSpace, role, projectKey, personType, subReport, excelHint, excelConfigured, cutoff);
        }
    }

    private static ReportDashboardPanelData LoadInvitationRejectedByProject(
        IObjectSpace objectSpace, PersonRecordRole? role, string projectKey,
        ReportDashboardPersonType personType, string subReport,
        string? excelHint, bool excelConfigured, DateTime cutoff)
    {
        // UNION: RejectionItems + ProcessRejected apps with no Rejection header.
        var rejectionQuery = objectSpace.GetObjectsQuery<RejectionItem>()
            .Where(ri => ri.Person != null
                         && (role == null || ri.Person.PersonRole == role)
                         && ri.Rejection != null
                         && ri.Rejection.Application != null
                         && ri.Rejection.Application.ApplicationType != null
                         && ri.Rejection.Application.ApplicationType.CanIssueInvitation
                         && (ri.Rejection.Date == default || ri.Rejection.Date >= cutoff));

        if (!string.IsNullOrWhiteSpace(projectKey) && projectKey != "All")
        {
            rejectionQuery = rejectionQuery.Where(ri =>
                ri.Rejection!.Application!.ProjectContract != null
                && (ri.Rejection.Application.ProjectContract.Name == projectKey
                    || ri.Rejection.Application.ProjectContract.NameTm == projectKey));
        }

        var rejectionRows = rejectionQuery.AsEnumerable().Select(ri =>
        {
            var project = ProjectLabel(ri.Rejection?.Application?.ProjectContract);
            if (string.IsNullOrWhiteSpace(project))
                project = ProjectLabel(ri.Person?.ProjectContract);
            if (string.IsNullOrWhiteSpace(project))
                project = "(No project)";
            return (
                RecordId: (Guid?)ri.ID,
                Name: ri.Person?.FullName ?? string.Empty,
                Project: project,
                ColumnA: ri.Rejection?.RejectedDocNumber ?? string.Empty,
                ColumnB: FormatDate(ri.Rejection?.Date),
                SortDate: ri.Rejection?.Date ?? DateTime.MinValue);
        }).ToList();

        var appQuery = objectSpace.GetObjectsQuery<Application>()
            .Where(a => a.ApplicationType != null
                        && a.ApplicationType.CanIssueInvitation
                        && (a.ApplicationDate == default || a.ApplicationDate >= cutoff)
                        && a.LatestProgress != null
                        && a.LatestProgress.State != null
                        && a.LatestProgress.State.Code == ApplicationProgressStateCodes.ProcessRejected
                        && !a.Rejections.Any());

        if (role.HasValue)
        {
            var roleValue = role.Value;
            appQuery = appQuery.Where(a => a.ApplicationItems.Any(ai =>
                ai.Person != null && ai.Person.PersonRole == roleValue));
        }

        if (!string.IsNullOrWhiteSpace(projectKey) && projectKey != "All")
        {
            appQuery = appQuery.Where(a => a.ProjectContract != null
                && (a.ProjectContract.Name == projectKey || a.ProjectContract.NameTm == projectKey));
        }

        rejectionRows.AddRange(appQuery.AsEnumerable().Select(a =>
        {
            var project = ProjectLabel(a.ProjectContract);
            if (string.IsNullOrWhiteSpace(project))
                project = "(No project)";
            return (
                RecordId: (Guid?)a.ID,
                Name: FirstApplicationPersonName(a),
                Project: project,
                ColumnA: a.FullApplicationNumber ?? a.ApplicationNumber ?? string.Empty,
                ColumnB: FormatDate(a.ApplicationDate),
                SortDate: a.ApplicationDate);
        }));

        var groups = rejectionRows
            .GroupBy(x => x.Project, StringComparer.Ordinal)
            .Select(g => (Label: g.Key, Count: g.Count()))
            .OrderByDescending(g => g.Count)
            .ToList();
        var buckets = AssignCategoricalCss(groups);
        var cssByLabel = buckets.ToDictionary(b => b.Label, b => b.CssClass, StringComparer.Ordinal);

        var rows = rejectionRows
            .OrderByDescending(x => x.SortDate)
            .Take(PreviewLimit)
            .Select(x => new ReportDashboardPreviewRow
            {
                RecordId = x.RecordId,
                Name = x.Name,
                Project = x.Project,
                ColumnA = x.ColumnA,
                ColumnB = x.ColumnB,
                Status = x.Project,
                StatusCssClass = cssByLabel.TryGetValue(x.Project, out var c) ? c : "st-cat-1"
            })
            .ToList();

        return BuildPanel(
            personType, ReportDashboardCategory.Invitation, subReport, rows,
            excelHint, excelConfigured, buckets, rejectionRows.Count);
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

        if (string.IsNullOrWhiteSpace(subReport)
            || subReport == "default"
            || ReportDashboardCatalog.IsRegistrationExpiringStateSubReport(subReport)
            || ReportDashboardCatalog.IsRegistrationCheckInByCitySubReport(subReport)
            || ReportDashboardCatalog.IsRegistrationApplicationTypeSubReport(subReport))
        {
            return LoadRegistrationFromView(
                objectSpace, role, projectKey, personType, subReport, excelHint, excelConfigured, cutoff, validVisaPersonIds);
        }

        return EmptyPanel(personType, ReportDashboardCategory.Registration, subReport, excelHint, excelConfigured);
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
        var useCityBuckets = ReportDashboardCatalog.IsRegistrationCheckInByCitySubReport(subReport);

        IQueryable<VwRdRegistration> query = db.VwRdRegistration.AsNoTracking();

        if (useExpiryBuckets || useCityBuckets)
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

            if (useCityBuckets)
            {
                list = TakeOneLastValidVisaPerPerson(
                    list, r => r.PersonOid, r => r.VisaExpirationDate, r => r.ID);

                static string CityCss(int index) => (index % 5) switch
                {
                    0 => "st-cat-1",
                    1 => "st-cat-2",
                    2 => "st-cat-3",
                    3 => "st-cat-4",
                    _ => "st-cat-5"
                };

                var cityGroups = list
                    .GroupBy(r => string.IsNullOrWhiteSpace(r.CityLabel) ? "Unknown city" : r.CityLabel.Trim(),
                        StringComparer.OrdinalIgnoreCase)
                    .OrderByDescending(g => g.Count())
                    .ThenBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                var buckets = cityGroups
                    .Select((g, i) => new ReportDashboardStatusBucket
                    {
                        Label = g.Key,
                        CssClass = CityCss(i),
                        Count = g.Count()
                    })
                    .ToList();

                var cssByCity = buckets.ToDictionary(b => b.Label, b => b.CssClass, StringComparer.OrdinalIgnoreCase);

                var rows = list
                    .OrderBy(r => r.CityLabel)
                    .ThenBy(r => r.PersonName)
                    .Take(PreviewLimit)
                    .Select(r =>
                    {
                        var city = string.IsNullOrWhiteSpace(r.CityLabel) ? "Unknown city" : r.CityLabel.Trim();
                        return new ReportDashboardPreviewRow
                        {
                            RecordId = r.ID,
                            Name = r.PersonName ?? string.Empty,
                            Project = r.ProjectName ?? string.Empty,
                            ColumnA = r.VisaNumber ?? string.Empty,
                            ColumnB = FormatDate(r.VisaExpirationDate),
                            Status = city,
                            StatusCssClass = cssByCity.TryGetValue(city, out var css) ? css : "st-pending"
                        };
                    })
                    .ToList();

                return BuildPanel(
                    personType, ReportDashboardCategory.Registration, subReport, rows,
                    excelHint, excelConfigured, buckets, list.Count);
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
        // View-backed: by-days-remaining (vw_rd_work_permit). by-status stays on legacy until promoted.
        if (subReport is "by-days-remaining" or "by-validity" or "default" || string.IsNullOrWhiteSpace(subReport))
        {
            return LoadWorkPermitFromView(
                objectSpace, role, projectKey, personType, subReport, excelHint, excelConfigured, cutoff,
                includeArchivedPersons, oneLastValidWorkPermitPerPerson, validVisaPersonIds);
        }

        return LoadWorkPermitLegacy(objectSpace, role, projectKey, personType, subReport, excelHint, excelConfigured, cutoff, includeArchivedPersons, validVisaPersonIds);
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
            addressIdSet = db.ApplicationItems
                .AsNoTracking()
                .Where(ai => ai.CurrentAddressOfResidence != null
                    && ai.Application != null
                    && ai.Application.ApplicationDate >= cutoff)
                .Select(ai => ai.CurrentAddressOfResidence!.ID)
                .Distinct()
                .ToHashSet();
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
        var query = objectSpace.GetObjectsQuery<ApplicationItem>()
            .Where(a => a.Person != null && (role == null || a.Person.PersonRole == role)
                        && a.TravelDate != null && a.TravelDate >= cutoff);

        if (validVisaPersonIds != null)
            query = query.Where(a => validVisaPersonIds.Contains(a.Person!.ID));

        if (!string.IsNullOrWhiteSpace(projectKey) && projectKey != "All")
            query = query.Where(a => a.Application != null && a.Application.ProjectContract != null
                && (a.Application.ProjectContract.Name == projectKey || a.Application.ProjectContract.NameTm == projectKey));

        var rows = query.OrderByDescending(a => a.TravelDate).Take(PreviewLimit).AsEnumerable().Select(a =>
        {
            const string status = "Approved";
            return new ReportDashboardPreviewRow
            {
                RecordId = a.ID,
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
            ListViewId            = ReportDashboardCatalog.ListViewId(category)
        };
    }

    /// <summary>
    /// Education used as <see cref="ApplicationItem.CurrentEducation"/> on an application
    /// whose <see cref="Application.ApplicationDate"/> is on/after <paramref name="cutoff"/>.
    /// </summary>
    private static IQueryable<VwRdEducation> FilterEducationByApplicationDate(
        Visa2026EFCoreDbContext db, IQueryable<VwRdEducation> query, DateTime cutoff) =>
        query.Where(r =>
            db.ApplicationItems.Any(ai =>
                ai.CurrentEducation != null
                && ai.CurrentEducation.ID == r.ID
                && ai.Application != null
                && ai.Application.ApplicationDate >= cutoff));

    private static IQueryable<VwRdEducationByCountry> FilterEducationByCountryByApplicationDate(
        Visa2026EFCoreDbContext db, IQueryable<VwRdEducationByCountry> query, DateTime cutoff) =>
        query.Where(r =>
            db.ApplicationItems.Any(ai =>
                ai.CurrentEducation != null
                && ai.CurrentEducation.ID == r.ID
                && ai.Application != null
                && ai.Application.ApplicationDate >= cutoff));

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
            // Used as ApplicationItem.CurrentEducation with Application.ApplicationDate in range.
            query = FilterEducationByApplicationDate(db, query, cutoff);
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
            query = FilterEducationByCountryByApplicationDate(db, query, cutoff);
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
            items = query
                .Where(e =>
                    db.ApplicationItems.Any(ai =>
                        ai.CurrentEducation != null
                        && ai.CurrentEducation.ID == e.ID
                        && ai.Application != null
                        && ai.Application.ApplicationDate >= cutoff))
                .AsEnumerable()
                .ToList();
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
            // Used as ApplicationItem.CurrentPositionHistory with Application.ApplicationDate in range.
            query = query.Where(r =>
                db.ApplicationItems.Any(ai =>
                    ai.CurrentPositionHistory != null
                    && ai.CurrentPositionHistory.ID == r.ID
                    && ai.Application != null
                    && ai.Application.ApplicationDate >= cutoff));
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
            medicalIdSet = db.ApplicationItems
                .AsNoTracking()
                .Where(ai => ai.CurrentMedicalRecord != null
                    && ai.Application != null
                    && ai.Application.ApplicationDate >= cutoff)
                .Select(ai => ai.CurrentMedicalRecord!.ID)
                .Distinct()
                .ToHashSet();
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
}