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
                                && (i.Invitation == null || i.Invitation.StartDate == null || i.Invitation.StartDate >= cutoff)),
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
                                && (ai.Application.ApplicationDate == null || ai.Application.ApplicationDate >= cutoff)),
            ReportDashboardCategory.Education =>
                objectSpace.GetObjectsQuery<Education>()
                    .Count(e => e.Person != null && (role == null || e.Person.PersonRole == role)),
            ReportDashboardCategory.PositionHistory =>
                objectSpace.GetObjectsQuery<EmployeePositionHistory>()
                    .Count(h => h.Person != null && (role == null || h.Person.PersonRole == role)),
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
        bool includeCancelledApplicationProcesses = false)
    {
        var cutoff = DateTime.Today.AddMonths(-dateRangeMonths);
        var role = ReportDashboardCatalog.TryGetPersonRole(personType);
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
            ReportDashboardCategory.Registration  => LoadRegistration(objectSpace, role, projectKey, personType, subReport, excelHint, excelConfigured, cutoff),
            ReportDashboardCategory.WorkPermit    => LoadWorkPermit(objectSpace, role, projectKey, personType, subReport, excelHint, excelConfigured, cutoff, includeArchivedPersons, oneLastValidWorkPermitPerPerson),
            ReportDashboardCategory.Travel        => LoadTravel(objectSpace, role, projectKey, personType, subReport, excelHint, excelConfigured, cutoff),
            ReportDashboardCategory.BorderZone    => LoadBorderZone(objectSpace, role, projectKey, personType, subReport, excelHint, excelConfigured, cutoff),
            ReportDashboardCategory.Passport         => LoadPassport(objectSpace, role, projectKey, personType, subReport, excelHint, excelConfigured, cutoff, includeArchivedPersons),
            ReportDashboardCategory.Education        => LoadEducation(objectSpace, role, projectKey, personType, subReport, excelHint, excelConfigured, includeArchivedPersons),
            ReportDashboardCategory.PositionHistory  => LoadPositionHistory(objectSpace, role, projectKey, personType, subReport, excelHint, excelConfigured),
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

    private static ReportDashboardPanelData LoadApplication(
        IObjectSpace objectSpace, PersonRecordRole? role, string projectKey,
        ReportDashboardPersonType personType, string subReport,
        string? excelHint, bool excelConfigured, DateTime cutoff,
        bool includeCompletedApplicationProcesses,
        bool includeCancelledApplicationProcesses)
    {
        if (subReport is "by-progress" or "by-type")
        {
            return LoadApplicationFromView(
                objectSpace, role, projectKey, personType, subReport, excelHint, excelConfigured, cutoff,
                includeCompletedApplicationProcesses, includeCancelledApplicationProcesses);
        }

        return LoadApplicationLegacy(
            objectSpace, role, projectKey, personType, subReport, excelHint, excelConfigured, cutoff,
            includeCompletedApplicationProcesses, includeCancelledApplicationProcesses);
    }

    /// <summary>
    /// Loads Application category from <c>vw_rd_application</c>
    /// (one header Application per row; by-progress → ProgressStateLabel; by-type → TypeLabel).
    /// </summary>
    private static ReportDashboardPanelData LoadApplicationFromView(
        IObjectSpace objectSpace, PersonRecordRole? role, string projectKey,
        ReportDashboardPersonType personType, string subReport,
        string? excelHint, bool excelConfigured, DateTime cutoff,
        bool includeCompletedApplicationProcesses,
        bool includeCancelledApplicationProcesses)
    {
        if (objectSpace is not EFCoreObjectSpace efOs
            || efOs.DbContext is not Visa2026EFCoreDbContext db)
        {
            return LoadApplicationLegacy(
                objectSpace, role, projectKey, personType, subReport, excelHint, excelConfigured, cutoff,
                includeCompletedApplicationProcesses, includeCancelledApplicationProcesses);
        }

        // Person-type filter uses first ApplicationItem person (soft); All = no filter.
        IQueryable<VwRdApplication> query = db.VwRdApplication
            .AsNoTracking()
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

        // Default: exclude completed (PROCESS_ISSUED) and cancelled (PROCESS_CANCELLED).
        if (!includeCompletedApplicationProcesses)
        {
            query = query.Where(r =>
                r.ProgressStateCode == null
                || r.ProgressStateCode != ApplicationProgressStateCodes.ProcessIssued);
        }

        if (!includeCancelledApplicationProcesses)
        {
            query = query.Where(r =>
                r.ProgressStateCode == null
                || r.ProgressStateCode != ApplicationProgressStateCodes.ProcessCancelled);
        }

        try
        {
            if (subReport is "by-type")
            {
                var typeGroups = query
                    .GroupBy(r => r.TypeLabel)
                    .Select(g => new { Label = g.Key, Count = g.Count() })
                    .OrderByDescending(g => g.Count)
                    .ToList();

                var buckets = AssignCategoricalCss(
                    typeGroups.Select(g => (g.Label ?? "Unknown", g.Count)).ToList());
                var cssByLabel = buckets.ToDictionary(b => b.Label, b => b.CssClass);
                var totalCount = buckets.Sum(b => b.Count);

                var rows = query
                    .OrderBy(r => r.TypeLabel)
                    .ThenByDescending(r => r.ApplicationDate)
                    .Take(PreviewLimit)
                    .AsEnumerable()
                    .Select(r =>
                    {
                        var status = r.TypeLabel ?? "Unknown";
                        return new ReportDashboardPreviewRow
                        {
                            RecordId = r.ID,
                            Name = r.PersonName ?? string.Empty,
                            Project = r.ProjectName ?? string.Empty,
                            ColumnA = r.ApplicationNumber ?? string.Empty,
                            ColumnB = FormatDate(r.ApplicationDate),
                            Status = status,
                            StatusCssClass = cssByLabel.TryGetValue(status, out var c) ? c : "st-cat-1"
                        };
                    })
                    .ToList();

                return BuildPanel(
                    personType, ReportDashboardCategory.Application, subReport, rows,
                    excelHint, excelConfigured, buckets, totalCount);
            }

            var progressRows = query
                .GroupBy(r => new { r.ProgressStateLabel, r.ProgressStateCssClass })
                .Select(g => new
                {
                    Label = g.Key.ProgressStateLabel,
                    CssClass = g.Key.ProgressStateCssClass,
                    Count = g.Count()
                })
                .ToList();

            var progressBuckets = progressRows
                .Select(g => new ReportDashboardStatusBucket
                {
                    Label = g.Label ?? "Being Prepared",
                    CssClass = g.CssClass ?? "st-pending",
                    Count = g.Count
                })
                .OrderByDescending(b => b.Count)
                .ToList();

            var progressTotal = progressBuckets.Sum(b => b.Count);

            var previewRows = query
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
                    Status = r.ProgressStateLabel ?? "Being Prepared",
                    StatusCssClass = r.ProgressStateCssClass ?? "st-pending"
                })
                .ToList();

            return BuildPanel(
                personType, ReportDashboardCategory.Application, subReport, previewRows,
                excelHint, excelConfigured, progressBuckets, progressTotal);
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UndefinedTable)
        {
            return LoadApplicationLegacy(
                objectSpace, role, projectKey, personType, subReport, excelHint, excelConfigured, cutoff,
                includeCompletedApplicationProcesses, includeCancelledApplicationProcesses);
        }
    }

    private static ReportDashboardPanelData LoadApplicationLegacy(
        IObjectSpace objectSpace, PersonRecordRole? role, string projectKey,
        ReportDashboardPersonType personType, string subReport,
        string? excelHint, bool excelConfigured, DateTime cutoff,
        bool includeCompletedApplicationProcesses,
        bool includeCancelledApplicationProcesses)
    {
        _ = role;
        var query = objectSpace.GetObjectsQuery<Application>()
            .Where(a => a.ApplicationDate >= cutoff);

        if (!string.IsNullOrWhiteSpace(projectKey) && projectKey != "All")
            query = query.Where(a => a.ProjectContract != null
                && (a.ProjectContract.Name == projectKey || a.ProjectContract.NameTm == projectKey));

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

        var rows = query.OrderByDescending(a => a.ApplicationDate).Take(PreviewLimit).AsEnumerable().Select(a =>
        {
            var status = subReport is "by-type"
                ? (a.ApplicationType?.Name ?? a.ApplicationType?.NameTm ?? "Unknown")
                : (string.IsNullOrWhiteSpace(a.CurrentState) ? "Pending" : a.CurrentState);
            return new ReportDashboardPreviewRow
            {
                RecordId = a.ID,
                Name = a.FullApplicationNumber ?? a.ApplicationNumber ?? string.Empty,
                Project = ProjectLabel(a.ProjectContract),
                ColumnA = a.FullApplicationNumber ?? a.ApplicationNumber ?? string.Empty,
                ColumnB = FormatDate(a.ApplicationDate),
                Status = status,
                StatusCssClass = subReport is "by-type" ? "st-cat-1" : StatusCss(status, null)
            };
        }).ToList();

        return BuildPanel(personType, ReportDashboardCategory.Application, subReport, rows, excelHint, excelConfigured);
    }

    private static ReportDashboardPanelData LoadVisaExtension(
        IObjectSpace objectSpace, PersonRecordRole? role, string projectKey,
        ReportDashboardPersonType personType, string subReport,
        string? excelHint, bool excelConfigured, DateTime cutoff,
        bool oneLastValidVisaPerPerson = false)
    {
        if (subReport is "app-progress")
        {
            return LoadVisaAppProgressFromView(
                objectSpace, role, projectKey, personType, subReport, excelHint, excelConfigured, cutoff);
        }

        if (subReport is "visa-state" or "default" || string.IsNullOrWhiteSpace(subReport))
        {
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
        var query = objectSpace.GetObjectsQuery<InvitationItem>()
            .Where(i => i.Person != null && (role == null || i.Person.PersonRole == role)
                        && (i.Invitation == null || i.Invitation.StartDate == null || i.Invitation.StartDate >= cutoff));

        if (!string.IsNullOrWhiteSpace(projectKey) && projectKey != "All")
            query = query.Where(i => i.Invitation != null && i.Invitation.Application != null
                && i.Invitation.Application.ProjectContract != null
                && (i.Invitation.Application.ProjectContract.Name == projectKey
                    || i.Invitation.Application.ProjectContract.NameTm == projectKey));

        var rows = query.Take(PreviewLimit).AsEnumerable().Select(i =>
        {
            var status = i.IsCancelled ? "Expiring Soon" : i.IsUsed ? "Approved" : "Pending";
            return new ReportDashboardPreviewRow
            {
                RecordId = i.ID,
                Name = i.Person?.FullName ?? string.Empty,
                Project = ProjectLabel(i.Invitation?.Application?.ProjectContract),
                ColumnA = i.Invitation?.InvitationNumber ?? string.Empty,
                ColumnB = FormatDate(i.Invitation?.StartDate),
                Status = status,
                StatusCssClass = StatusCss(status, null)
            };
        }).ToList();

        return BuildPanel(personType, ReportDashboardCategory.Invitation, subReport, rows, excelHint, excelConfigured);
    }

    private static ReportDashboardPanelData LoadRegistration(
        IObjectSpace objectSpace, PersonRecordRole? role, string projectKey,
        ReportDashboardPersonType personType, string subReport,
        string? excelHint, bool excelConfigured, DateTime cutoff)
    {
        var query = objectSpace.GetObjectsQuery<AddressOfResidence>()
            .Where(a => a.Person != null && (role == null || a.Person.PersonRole == role)
                        && (a.ExpirationDate == null || a.ExpirationDate >= cutoff));

        if (!string.IsNullOrWhiteSpace(projectKey) && projectKey != "All")
            query = query.Where(a => a.Person!.ProjectContract != null
                && (a.Person.ProjectContract.Name == projectKey || a.Person.ProjectContract.NameTm == projectKey));

        var today = DateTime.Today;
        var rows = query.Take(PreviewLimit).AsEnumerable().Select(a =>
        {
            var status = ExpirationBucket(a.ExpirationDate, today);
            return new ReportDashboardPreviewRow
            {
                RecordId = a.ID,
                Name = a.Person?.FullName ?? string.Empty,
                Project = ProjectLabel(a.Person?.ProjectContract),
                ColumnA = a.FullAddress ?? string.Empty,
                ColumnB = FormatDate(a.ExpirationDate),
                Status = status,
                StatusCssClass = StatusCss(status, a.DaysRemaining)
            };
        }).ToList();

        return BuildPanel(personType, ReportDashboardCategory.Registration, subReport, rows, excelHint, excelConfigured);
    }

    private static ReportDashboardPanelData LoadWorkPermit(
        IObjectSpace objectSpace, PersonRecordRole? role, string projectKey,
        ReportDashboardPersonType personType, string subReport,
        string? excelHint, bool excelConfigured, DateTime cutoff,
        bool includeArchivedPersons,
        bool oneLastValidWorkPermitPerPerson = false)
    {
        // View-backed: by-days-remaining (vw_rd_work_permit). by-status stays on legacy until promoted.
        if (subReport is "by-days-remaining" or "by-validity" or "default" || string.IsNullOrWhiteSpace(subReport))
        {
            return LoadWorkPermitFromView(
                objectSpace, role, projectKey, personType, subReport, excelHint, excelConfigured, cutoff,
                includeArchivedPersons, oneLastValidWorkPermitPerPerson);
        }

        return LoadWorkPermitLegacy(objectSpace, role, projectKey, personType, subReport, excelHint, excelConfigured, cutoff, includeArchivedPersons);
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
        bool oneLastValidWorkPermitPerPerson = false)
    {
        if (objectSpace is not EFCoreObjectSpace efOs
            || efOs.DbContext is not Visa2026EFCoreDbContext db)
        {
            return LoadWorkPermitLegacy(objectSpace, role, projectKey, personType, subReport, excelHint, excelConfigured, cutoff, includeArchivedPersons);
        }

        _ = cutoff;
        IQueryable<VwRdWorkPermit> query = db.VwRdWorkPermit
            .AsNoTracking();
        if (role.HasValue)
            query = query.Where(r => r.PersonRoleCode == (int)role.Value);

        if (!includeArchivedPersons)
            query = query.Where(r => !r.IsArchived);

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
            return LoadWorkPermitLegacy(objectSpace, role, projectKey, personType, subReport, excelHint, excelConfigured, cutoff, includeArchivedPersons);
        }
    }

    private static ReportDashboardPanelData LoadWorkPermitLegacy(
        IObjectSpace objectSpace, PersonRecordRole? role, string projectKey,
        ReportDashboardPersonType personType, string subReport,
        string? excelHint, bool excelConfigured, DateTime cutoff,
        bool includeArchivedPersons)
    {
        // Rank last item like PersonCurrentItems (cancelled included); emit only if last is not cancelled.
        var query = objectSpace.GetObjectsQuery<WorkPermitItem>()
            .Where(w => w.Person != null && (role == null || w.Person.PersonRole == role));

        if (!includeArchivedPersons)
            query = query.Where(w => !w.Person!.IsArchived);

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

    private static ReportDashboardPanelData LoadTravel(
        IObjectSpace objectSpace, PersonRecordRole? role, string projectKey,
        ReportDashboardPersonType personType, string subReport,
        string? excelHint, bool excelConfigured, DateTime cutoff)
    {
        var query = objectSpace.GetObjectsQuery<ApplicationItem>()
            .Where(a => a.Person != null && (role == null || a.Person.PersonRole == role)
                        && a.TravelDate != null && a.TravelDate >= cutoff);

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
        string? excelHint, bool excelConfigured, DateTime cutoff)
    {
        var query = objectSpace.GetObjectsQuery<BorderZoneItem>()
            .Where(b => b.Person != null && (role == null || b.Person.PersonRole == role)
                        && (b.BorderZone == null || b.BorderZone.ExpirationDate == null || b.BorderZone.ExpirationDate >= cutoff));

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
        bool includeArchivedPersons)
    {
        // View-backed: ApplicationItem.CurrentPassport; by-validity / by-type / by-citizenship.
        return LoadPassportFromView(
            objectSpace, role, projectKey, personType, subReport, excelHint, excelConfigured, cutoff,
            includeArchivedPersons);
    }

    /// <summary>
    /// Loads Passport panel from <c>vw_rd_passport</c>
    /// (one ApplicationItem per row with CurrentPassport; filtered by Application.ApplicationDate).
    /// By default excludes archived persons; pass includeArchivedPersons to include them.
    /// Status/chart dimension depends on <paramref name="subReport"/>:
    /// by-validity → ValidityLabel; by-type → TypeLabel; by-citizenship → CitizenshipLabel (Person.Nationality).
    /// </summary>
    private static ReportDashboardPanelData LoadPassportFromView(
        IObjectSpace objectSpace, PersonRecordRole? role, string projectKey,
        ReportDashboardPersonType personType, string subReport,
        string? excelHint, bool excelConfigured, DateTime cutoff,
        bool includeArchivedPersons)
    {
        if (objectSpace is not EFCoreObjectSpace efOs
            || efOs.DbContext is not Visa2026EFCoreDbContext db)
        {
            return LoadPassportLegacy(objectSpace, role, projectKey, personType, subReport, excelHint, excelConfigured, cutoff, includeArchivedPersons);
        }

        var categorical = subReport is "by-type" or "by-citizenship";

        IQueryable<VwRdPassport> query = db.VwRdPassport
            .AsNoTracking()
            .Where(r => r.ApplicationDate == null || r.ApplicationDate >= cutoff);
        if (role.HasValue)
            query = query.Where(r => r.PersonRoleCode == (int)role.Value);

        if (!includeArchivedPersons)
            query = query.Where(r => !r.IsArchived);

        if (!string.IsNullOrWhiteSpace(projectKey) && projectKey != "All")
        {
            query = query.Where(r =>
                r.ProjectNameTm == projectKey
                || r.ProjectName == projectKey
                || r.ProjectNameRaw == projectKey);
        }

        try
        {
            List<ReportDashboardStatusBucket> buckets;
            if (categorical)
            {
                IQueryable<string?> labelQuery = subReport == "by-citizenship"
                    ? query.Select(r => r.CitizenshipLabel)
                    : query.Select(r => r.TypeLabel);

                var catRows = labelQuery
                    .GroupBy(l => l)
                    .Select(g => new { Label = g.Key, Count = g.Count() })
                    .OrderByDescending(g => g.Count)
                    .ToList();

                buckets = AssignCategoricalCss(catRows.Select(t => (t.Label ?? string.Empty, t.Count)).ToList());
            }
            else
            {
                var validityRows = query
                    .GroupBy(r => new { r.ValidityLabel, r.ValidityCssClass })
                    .Select(g => new
                    {
                        Label = g.Key.ValidityLabel,
                        CssClass = g.Key.ValidityCssClass,
                        Count = g.Count()
                    })
                    .ToList();

                buckets = validityRows
                    .Select(g => new ReportDashboardStatusBucket
                    {
                        Label = g.Label ?? string.Empty,
                        CssClass = g.CssClass ?? "st-pending",
                        Count = g.Count
                    })
                    .OrderByDescending(b => b.Count)
                    .ToList();
            }

            var totalCount = buckets.Sum(b => b.Count);
            var cssByLabel = buckets.ToDictionary(b => b.Label, b => b.CssClass, StringComparer.Ordinal);

            IQueryable<VwRdPassport> previewQuery = subReport switch
            {
                "by-type" => query.OrderBy(r => r.TypeLabel).ThenBy(r => r.ExpirationDate),
                "by-citizenship" => query.OrderBy(r => r.CitizenshipLabel).ThenBy(r => r.ExpirationDate),
                _ => query.OrderBy(r => r.ValidityLabel == "Expired").ThenBy(r => r.ExpirationDate)
            };

            var rows = previewQuery
                .Take(PreviewLimit)
                .AsEnumerable()
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
            return LoadPassportLegacy(objectSpace, role, projectKey, personType, subReport, excelHint, excelConfigured, cutoff, includeArchivedPersons);
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
        bool includeArchivedPersons)
    {
        // Same universe as vw_rd_passport: ApplicationItems with CurrentPassport in date range.
        var query = objectSpace.GetObjectsQuery<ApplicationItem>()
            .Where(ai => ai.CurrentPassport != null
                        && ai.Person != null && (role == null || ai.Person.PersonRole == role)
                        && ai.Application != null
                        && (ai.Application.ApplicationDate == null || ai.Application.ApplicationDate >= cutoff));

        if (!includeArchivedPersons)
            query = query.Where(ai => !ai.Person!.IsArchived);

        if (!string.IsNullOrWhiteSpace(projectKey) && projectKey != "All")
        {
            query = query.Where(ai =>
                (ai.Application!.ProjectContract != null
                    && (ai.Application.ProjectContract.Name == projectKey || ai.Application.ProjectContract.NameTm == projectKey))
                || (ai.Person!.ProjectContract != null
                    && (ai.Person.ProjectContract.Name == projectKey || ai.Person.ProjectContract.NameTm == projectKey)));
        }

        var today = DateTime.Today;
        var items = query.AsEnumerable().ToList();
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
    // ---- Panel builder ---------------------------------------------------

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
        int? totalCount = null)
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

        var subLabel = ReportDashboardCatalog.SubReports(category)
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

    private static ReportDashboardPanelData LoadEducation(
        IObjectSpace objectSpace, PersonRecordRole? role, string projectKey,
        ReportDashboardPersonType personType, string subReport,
        string? excelHint, bool excelConfigured,
        bool includeArchivedPersons)
    {
        if (subReport is "by-country")
        {
            return LoadEducationByCountryFromView(
                objectSpace, role, projectKey, personType, subReport, excelHint, excelConfigured, includeArchivedPersons);
        }

        if (objectSpace is not EFCoreObjectSpace efOs
            || efOs.DbContext is not Visa2026EFCoreDbContext db)
        {
            return LoadEducationLegacy(
                objectSpace, role, projectKey, personType, subReport, excelHint, excelConfigured,
                includeArchivedPersons);
        }

        IQueryable<VwRdEducation> query = db.VwRdEducation.AsNoTracking();

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
            IQueryable<string?> labelQuery = subReport switch
            {
                "by-country" => query.Select(r => r.CountryLabel),
                "by-specialty" => query.Select(r => r.SpecialtyLabel),
                _ => query.Select(r => r.LevelLabel)
            };

            var groups = labelQuery
                .GroupBy(l => l)
                .Select(g => new { Label = g.Key, Count = g.Count() })
                .OrderByDescending(g => g.Count)
                .ToList();

            var buckets = AssignCategoricalCss(
                groups.Select(g => (g.Label ?? "Unknown", g.Count)).ToList());
            var cssByLabel = buckets.ToDictionary(b => b.Label, b => b.CssClass, StringComparer.Ordinal);
            var totalCount = buckets.Sum(b => b.Count);

            IQueryable<VwRdEducation> previewQuery = subReport switch
            {
                "by-country" => query.OrderBy(r => r.CountryLabel).ThenBy(r => r.PersonName),
                "by-specialty" => query.OrderBy(r => r.SpecialtyLabel).ThenBy(r => r.PersonName),
                _ => query.OrderBy(r => r.LevelLabel).ThenBy(r => r.PersonName)
            };

            var rows = previewQuery
                .Take(PreviewLimit)
                .AsEnumerable()
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
                objectSpace, role, projectKey, personType, subReport, excelHint, excelConfigured,
                includeArchivedPersons);
        }
    }

    private static ReportDashboardPanelData LoadEducationByCountryFromView(
        IObjectSpace objectSpace, PersonRecordRole? role, string projectKey,
        ReportDashboardPersonType personType, string subReport,
        string? excelHint, bool excelConfigured,
        bool includeArchivedPersons)
    {
        if (objectSpace is not EFCoreObjectSpace efOs
            || efOs.DbContext is not Visa2026EFCoreDbContext db)
        {
            return LoadEducationLegacy(
                objectSpace, role, projectKey, personType, subReport, excelHint, excelConfigured, includeArchivedPersons);
        }

        IQueryable<VwRdEducationByCountry> query = db.VwRdEducationByCountry.AsNoTracking();

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
            var groups = query
                .GroupBy(r => r.CountryLabel)
                .Select(g => new { Label = g.Key, Count = g.Count() })
                .OrderByDescending(g => g.Count)
                .ToList();

            var buckets = AssignCategoricalCss(
                groups.Select(g => (g.Label ?? "Unknown", g.Count)).ToList());
            var cssByLabel = buckets.ToDictionary(b => b.Label, b => b.CssClass, StringComparer.Ordinal);
            var totalCount = buckets.Sum(b => b.Count);

            var rows = query
                .OrderBy(r => r.CountryLabel)
                .ThenBy(r => r.PersonName)
                .Take(PreviewLimit)
                .AsEnumerable()
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
                objectSpace, role, projectKey, personType, subReport, excelHint, excelConfigured,
                includeArchivedPersons);
        }
    }

    private static ReportDashboardPanelData LoadEducationLegacy(
        IObjectSpace objectSpace, PersonRecordRole? role, string projectKey,
        ReportDashboardPersonType personType, string subReport,
        string? excelHint, bool excelConfigured,
        bool includeArchivedPersons)
    {
        var query = objectSpace.GetObjectsQuery<Education>()
            .Where(e => e.Person != null && (role == null || e.Person.PersonRole == role));

        if (!includeArchivedPersons)
            query = query.Where(e => !e.Person!.IsArchived);

        if (!string.IsNullOrWhiteSpace(projectKey) && projectKey != "All")
        {
            query = query.Where(e => e.Person!.ProjectContract != null
                && (e.Person.ProjectContract.Name == projectKey
                    || e.Person.ProjectContract.NameTm == projectKey));
        }

        var items = query.AsEnumerable().ToList();

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
        string? excelHint, bool excelConfigured)
    {
        if (objectSpace is not EFCoreObjectSpace efOs
            || efOs.DbContext is not Visa2026EFCoreDbContext db)
        {
            return EmptyPanel(personType, ReportDashboardCategory.PositionHistory, subReport, excelHint, excelConfigured);
        }

        IQueryable<VwRdPositionHistory> query = db.VwRdPositionHistory
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
            if (subReport is "by-position")
            {
                var groups = query
                    .GroupBy(r => r.PositionLabel)
                    .Select(g => new { Label = g.Key, Count = g.Count() })
                    .OrderByDescending(g => g.Count)
                    .ToList();

                var buckets = AssignCategoricalCss(
                    groups.Select(g => (g.Label ?? "Unknown", g.Count)).ToList());
                var cssByLabel = buckets.ToDictionary(b => b.Label, b => b.CssClass, StringComparer.Ordinal);
                var totalCount = buckets.Sum(b => b.Count);

                var rows = query
                    .OrderBy(r => r.PositionLabel)
                    .ThenByDescending(r => r.StartDate)
                    .Take(PreviewLimit)
                    .AsEnumerable()
                    .Select(r =>
                    {
                        var status = r.PositionLabel ?? "Unknown";
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

            var statusRows = query
                .GroupBy(r => new { r.StatusLabel, r.StatusCssClass })
                .Select(g => new
                {
                    Label = g.Key.StatusLabel,
                    CssClass = g.Key.StatusCssClass,
                    Count = g.Count()
                })
                .ToList();

            var statusBuckets = statusRows
                .Select(g => new ReportDashboardStatusBucket
                {
                    Label = g.Label ?? "Current",
                    CssClass = g.CssClass ?? "st-pending",
                    Count = g.Count
                })
                .OrderByDescending(b => b.Count)
                .ToList();

            var statusTotal = statusBuckets.Sum(b => b.Count);

            var previewRows = query
                .OrderBy(r => r.StatusLabel == "Ended")
                .ThenByDescending(r => r.StartDate)
                .Take(PreviewLimit)
                .AsEnumerable()
                .Select(r => new ReportDashboardPreviewRow
                {
                    RecordId = r.ID,
                    Name = r.PersonName ?? string.Empty,
                    Project = r.ProjectName ?? string.Empty,
                    ColumnA = r.PositionName ?? string.Empty,
                    ColumnB = FormatDate(r.StartDate),
                    Status = r.StatusLabel ?? "Current",
                    StatusCssClass = r.StatusCssClass ?? "st-pending"
                })
                .ToList();

            return BuildPanel(
                personType, ReportDashboardCategory.PositionHistory, subReport, previewRows,
                excelHint, excelConfigured, statusBuckets, statusTotal);
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UndefinedTable)
        {
            return EmptyPanel(personType, ReportDashboardCategory.PositionHistory, subReport, excelHint, excelConfigured);
        }
    }

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