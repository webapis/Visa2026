using System;
using System.ComponentModel;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;

namespace Visa2026.Module.BusinessObjects;

/// <summary>
/// Roster membership snapshot taken when an officer opens Excel for a Report Dashboard
/// days-remaining sheet (aktiv personeller / aktiv aile üyeleri / çalışma izni listesi).
/// Used to explain total changes (added / removed people) vs the previous Excel run.
/// </summary>
[DefaultClassOptions]
[NavigationItem(false)]
[DisplayName("Report Dashboard roster snapshot")]
[ModelDefault("Caption", "Report Dashboard roster snapshot")]
public class ReportDashboardRosterSnapshot : BaseObject
{
    public virtual DateTime CreatedOnUtc { get; set; } = DateTime.UtcNow;

    /// <summary>Calendar date the roster filter was evaluated for (local report date).</summary>
    public virtual DateTime? AsOfDate { get; set; }

    [FieldSize(128)]
    public virtual string? CreatedByUserName { get; set; }

    /// <summary><see cref="Services.ReportDashboard.ReportDashboardCategory"/> as int.</summary>
    public virtual int Category { get; set; }

    [FieldSize(128)]
    public virtual string SubReportKey { get; set; } = string.Empty;

    /// <summary>
    /// Stable filter key: project, audience, Exclude 0 days remaining, one-last-visa, etc.
    /// </summary>
    [FieldSize(512)]
    public virtual string FilterFingerprint { get; set; } = string.Empty;

    public virtual int TotalCount { get; set; }

    /// <summary>Stable id for this Excel export (also embedded as a short suffix in FileName).</summary>
    public virtual Guid? ExportId { get; set; }

    /// <summary>Download file name used for this export (e.g. aktiv personeller_a1b2c3d4.xlsx).</summary>
    [FieldSize(260)]
    public virtual string? FileName { get; set; }

    /// <summary>
    /// JSON array of {"id":"guid","n":"Name","p":"passport-or-empty"} in export order.
    /// </summary>
    [FieldSize(FieldSizeAttribute.Unlimited)]
    public virtual string MembersJson { get; set; } = "[]";

    /// <summary>
    /// Sticky last membership-change payload (data + date/filter buckets). Copied forward
    /// when an Excel export has no membership moves so the UI still shows the last change.
    /// Not written into the Excel preamble.
    /// </summary>
    [FieldSize(FieldSizeAttribute.Unlimited)]
    public virtual string? LastChangeJson { get; set; }
}
