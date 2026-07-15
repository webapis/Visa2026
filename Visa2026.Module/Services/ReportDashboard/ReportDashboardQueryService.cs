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

    public ReportDashboardSnapshot LoadSnapshot(IObjectSpace objectSpace, int dateRangeMonths = 6)
    {
        var cutoff = DateTime.Today.AddMonths(-dateRangeMonths);
        var projects = LoadProjectChips(objectSpace);
        var counts = new Dictionary<(ReportDashboardPersonType, ReportDashboardCategory), int>();

        foreach (ReportDashboardPersonType personType in Enum.GetValues(typeof(ReportDashboardPersonType)))
        {
            foreach (var category in ReportDashboardCatalog.Categories)
                counts[(personType, category)] = CountCategory(objectSpace, personType, category, cutoff);
        }

        return new ReportDashboardSnapshot { Projects = projects, CategoryCounts = counts };
    }

    private static int CountCategory(
        IObjectSpace objectSpace,
        ReportDashboardPersonType personType,
        ReportDashboardCategory category,
        DateTime cutoff)
    {
        var role = ReportDashboardCatalog.ToPersonRole(personType);
        return category switch
        {
            ReportDashboardCategory.VisaExtension =>
                objectSpace.GetObjectsQuery<VisaExtensionStatus>()
                    .Count(v => v.Person != null && v.Person.PersonRole == role
                                && (v.ApplicationDate == null || v.ApplicationDate >= cutoff)),
            ReportDashboardCategory.Invitation =>
                objectSpace.GetObjectsQuery<InvitationItem>()
                    .Count(i => i.Person != null && i.Person.PersonRole == role
                                && (i.Invitation == null || i.Invitation.StartDate == null || i.Invitation.StartDate >= cutoff)),
            ReportDashboardCategory.Registration =>
                objectSpace.GetObjectsQuery<AddressOfResidence>()
                    .Count(a => a.Person != null && a.Person.PersonRole == role
                                && (a.ExpirationDate == null || a.ExpirationDate >= cutoff)),
            ReportDashboardCategory.WorkPermit =>
                objectSpace.GetObjectsQuery<WorkPermitItem>()
                    .Count(w => w.Person != null && w.Person.PersonRole == role
                                && (w.ExpirationDate == null || w.ExpirationDate >= cutoff)),
            ReportDashboardCategory.Travel =>
                objectSpace.GetObjectsQuery<ApplicationItem>()
                    .Count(a => a.Person != null && a.Person.PersonRole == role
                                && a.TravelDate != null && a.TravelDate >= cutoff),
            ReportDashboardCategory.BorderZone =>
                objectSpace.GetObjectsQuery<BorderZoneItem>()
                    .Count(b => b.Person != null && b.Person.PersonRole == role
                                && (b.BorderZone == null || b.BorderZone.ExpirationDate == null || b.BorderZone.ExpirationDate >= cutoff)),
            ReportDashboardCategory.Passport =>
                objectSpace.GetObjectsQuery<Passport>()
                    .Count(p => p.Person != null && p.Person.PersonRole == role
                                && !p.Person.IsArchived
                                && (p.ExpirationDate == null || p.ExpirationDate >= cutoff)),
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
        bool includeArchivedPersons = false)
    {
        var cutoff = DateTime.Today.AddMonths(-dateRangeMonths);
        var role = ReportDashboardCatalog.ToPersonRole(personType);
        var excelHint = ReportDashboardCatalog.ExcelTemplateNameHint(category);
        var excelConfigured = !string.IsNullOrEmpty(excelHint)
            && objectSpace.GetObjectsQuery<UserReportTemplate>()
                .Any(t => t.TemplateName != null
                    && t.TemplateName.Contains(excelHint)
                    && t.TemplateOutputFormat == TemplateOutputFormat.Excel);

        return category switch
        {
            ReportDashboardCategory.VisaExtension => LoadVisaExtension(objectSpace, role, projectKey, personType, subReport, excelHint, excelConfigured, cutoff),
            ReportDashboardCategory.Invitation    => LoadInvitation(objectSpace, role, projectKey, personType, subReport, excelHint, excelConfigured, cutoff),
            ReportDashboardCategory.Registration  => LoadRegistration(objectSpace, role, projectKey, personType, subReport, excelHint, excelConfigured, cutoff),
            ReportDashboardCategory.WorkPermit    => LoadWorkPermit(objectSpace, role, projectKey, personType, subReport, excelHint, excelConfigured, cutoff),
            ReportDashboardCategory.Travel        => LoadTravel(objectSpace, role, projectKey, personType, subReport, excelHint, excelConfigured, cutoff),
            ReportDashboardCategory.BorderZone    => LoadBorderZone(objectSpace, role, projectKey, personType, subReport, excelHint, excelConfigured, cutoff),
            ReportDashboardCategory.Passport      => LoadPassport(objectSpace, role, projectKey, personType, subReport, excelHint, excelConfigured, cutoff, includeArchivedPersons),
            _                                     => EmptyPanel(personType, category, subReport, excelHint, excelConfigured)
        };
    }

    // ---- Project chips ---------------------------------------------------

    private static List<ReportDashboardProjectChip> LoadProjectChips(IObjectSpace objectSpace)
    {
        var personCounts = objectSpace.GetObjectsQuery<Person>()
            .Where(p => p.ProjectContract != null)
            .GroupBy(p => p.ProjectContract!.ID)
            .Select(g => new { ProjectId = g.Key, Count = g.Count() })
            .ToList()
            .ToDictionary(x => x.ProjectId, x => x.Count);

        var chips = objectSpace.GetObjectsQuery<ProjectContract>()
            .AsEnumerable()
            .Select(p =>
            {
                var label = !string.IsNullOrWhiteSpace(p.NameTm) ? p.NameTm! : (p.Name ?? p.ID.ToString());
                personCounts.TryGetValue(p.ID, out var count);
                return new ReportDashboardProjectChip
                {
                    Key   = !string.IsNullOrWhiteSpace(p.Name) ? p.Name! : label,
                    Label = label,
                    Count = count
                };
            })
            .OrderByDescending(c => c.Count)
            .ThenBy(c => c.Label)
            .ToList();

        chips.Insert(0, new ReportDashboardProjectChip { Key = "All", Label = "All", Count = chips.Sum(c => c.Count) });
        return chips;
    }

    // ---- Category loaders -----------------------------------------------

    private static ReportDashboardPanelData LoadVisaExtension(
        IObjectSpace objectSpace, PersonRecordRole role, string projectKey,
        ReportDashboardPersonType personType, string subReport,
        string? excelHint, bool excelConfigured, DateTime cutoff)
    {
        var query = objectSpace.GetObjectsQuery<VisaExtensionStatus>()
            .Where(v => v.Person != null && v.Person.PersonRole == role
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
        IObjectSpace objectSpace, PersonRecordRole role, string projectKey,
        ReportDashboardPersonType personType, string subReport,
        string? excelHint, bool excelConfigured, DateTime cutoff)
    {
        var query = objectSpace.GetObjectsQuery<InvitationItem>()
            .Where(i => i.Person != null && i.Person.PersonRole == role
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
        IObjectSpace objectSpace, PersonRecordRole role, string projectKey,
        ReportDashboardPersonType personType, string subReport,
        string? excelHint, bool excelConfigured, DateTime cutoff)
    {
        var query = objectSpace.GetObjectsQuery<AddressOfResidence>()
            .Where(a => a.Person != null && a.Person.PersonRole == role
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
        IObjectSpace objectSpace, PersonRecordRole role, string projectKey,
        ReportDashboardPersonType personType, string subReport,
        string? excelHint, bool excelConfigured, DateTime cutoff)
    {
        var query = objectSpace.GetObjectsQuery<WorkPermitItem>()
            .Where(w => w.Person != null && w.Person.PersonRole == role
                        && (w.ExpirationDate == null || w.ExpirationDate >= cutoff));

        if (!string.IsNullOrWhiteSpace(projectKey) && projectKey != "All")
            query = query.Where(w => w.WorkPermit != null && w.WorkPermit.Application != null
                && w.WorkPermit.Application.ProjectContract != null
                && (w.WorkPermit.Application.ProjectContract.Name == projectKey
                    || w.WorkPermit.Application.ProjectContract.NameTm == projectKey));

        var today = DateTime.Today;
        var rows = query.Take(PreviewLimit).AsEnumerable().Select(w =>
        {
            var status = w.IsCancelled ? "Pending" : ExpirationBucket(w.ExpirationDate, today);
            return new ReportDashboardPreviewRow
            {
                RecordId = w.ID,
                Name = w.Person?.FullName ?? string.Empty,
                Project = ProjectLabel(w.WorkPermit?.Application?.ProjectContract),
                ColumnA = w.WorkPermitNumber ?? string.Empty,
                ColumnB = FormatDate(w.ExpirationDate),
                Status = status,
                StatusCssClass = StatusCss(status, w.DaysRemaining)
            };
        }).ToList();

        return BuildPanel(personType, ReportDashboardCategory.WorkPermit, subReport, rows, excelHint, excelConfigured);
    }

    private static ReportDashboardPanelData LoadTravel(
        IObjectSpace objectSpace, PersonRecordRole role, string projectKey,
        ReportDashboardPersonType personType, string subReport,
        string? excelHint, bool excelConfigured, DateTime cutoff)
    {
        var query = objectSpace.GetObjectsQuery<ApplicationItem>()
            .Where(a => a.Person != null && a.Person.PersonRole == role
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
        IObjectSpace objectSpace, PersonRecordRole role, string projectKey,
        ReportDashboardPersonType personType, string subReport,
        string? excelHint, bool excelConfigured, DateTime cutoff)
    {
        var query = objectSpace.GetObjectsQuery<BorderZoneItem>()
            .Where(b => b.Person != null && b.Person.PersonRole == role
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
        IObjectSpace objectSpace, PersonRecordRole role, string projectKey,
        ReportDashboardPersonType personType, string subReport,
        string? excelHint, bool excelConfigured, DateTime cutoff,
        bool includeArchivedPersons)
    {
        // View-backed: by-validity / by-type / by-citizenship (same vw_rd_passport).
        return LoadPassportFromView(
            objectSpace, role, projectKey, personType, subReport, excelHint, excelConfigured, cutoff,
            includeArchivedPersons);
    }

    /// <summary>
    /// Loads Passport panel from <c>vw_rd_passport</c> (latest passport per person).
    /// By default excludes archived persons; pass includeArchivedPersons to include them.
    /// Status/chart dimension depends on <paramref name="subReport"/>:
    /// by-validity → ValidityLabel; by-type → TypeLabel; by-citizenship → CitizenshipLabel (Person.Nationality).
    /// </summary>
    private static ReportDashboardPanelData LoadPassportFromView(
        IObjectSpace objectSpace, PersonRecordRole role, string projectKey,
        ReportDashboardPersonType personType, string subReport,
        string? excelHint, bool excelConfigured, DateTime cutoff,
        bool includeArchivedPersons)
    {
        if (objectSpace is not EFCoreObjectSpace efOs
            || efOs.DbContext is not Visa2026EFCoreDbContext db)
        {
            return LoadPassportLegacy(objectSpace, role, projectKey, personType, subReport, excelHint, excelConfigured, cutoff, includeArchivedPersons);
        }

        _ = cutoff;
        var roleCode = (int)role;
        var categorical = subReport is "by-type" or "by-citizenship";

        IQueryable<VwRdPassport> query = db.VwRdPassport
            .AsNoTracking()
            .Where(r => r.PersonRoleCode == roleCode);

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
                        RecordId = r.ID,
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
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UndefinedTable)
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
        IObjectSpace objectSpace, PersonRecordRole role, string projectKey,
        ReportDashboardPersonType personType, string subReport,
        string? excelHint, bool excelConfigured, DateTime cutoff,
        bool includeArchivedPersons)
    {
        var query = objectSpace.GetObjectsQuery<Passport>()
            .Where(p => p.Person != null && p.Person.PersonRole == role
                        && !p.IsCancelled
                        && (p.ExpirationDate == null || p.ExpirationDate >= cutoff));

        if (!includeArchivedPersons)
            query = query.Where(p => !p.Person!.IsArchived);

        if (!string.IsNullOrWhiteSpace(projectKey) && projectKey != "All")
            query = query.Where(p => p.Person!.ProjectContract != null
                && (p.Person.ProjectContract.Name == projectKey || p.Person.ProjectContract.NameTm == projectKey));

        var today = DateTime.Today;
        // Same rule as vw_rd_passport: one passport per person (latest IssueDate).
        var latest = query.AsEnumerable()
            .GroupBy(p => p.Person!.ID)
            .Select(g => g.OrderByDescending(p => p.IssueDate).ThenByDescending(p => p.ID).First())
            .ToList();

        var buckets = latest
            .GroupBy(p => PassportValidityBucket(p.ExpirationDate, today))
            .Select(g => new ReportDashboardStatusBucket
            {
                Label = g.Key,
                Count = g.Count(),
                CssClass = PassportValidityCss(g.First().ExpirationDate, today)
            })
            .OrderByDescending(b => b.Count)
            .ToList();

        var rows = latest
            .OrderBy(p => PassportValidityBucket(p.ExpirationDate, today) == "Expired")
            .ThenBy(p => p.ExpirationDate)
            .Take(PreviewLimit)
            .Select(p => new ReportDashboardPreviewRow
            {
                RecordId = p.ID,
                Name = p.Person?.FullName ?? string.Empty,
                Project = ProjectLabel(p.Person?.ProjectContract),
                ColumnA = p.PassportNumber ?? string.Empty,
                ColumnB = FormatDate(p.ExpirationDate),
                Status = PassportValidityBucket(p.ExpirationDate, today),
                StatusCssClass = PassportValidityCss(p.ExpirationDate, today)
            })
            .ToList();

        return BuildPanel(
            personType, ReportDashboardCategory.Passport, subReport, rows,
            excelHint, excelConfigured, buckets, latest.Count);
    }
    // ---- Panel builder ---------------------------------------------------

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