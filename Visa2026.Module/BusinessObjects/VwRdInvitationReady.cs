using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Base;
using Visa2026.Module.Localization;

namespace Visa2026.Module.BusinessObjects;

/// <summary>
/// Report Dashboard: Active Invitation (P)/(V) from vw_rd_invitation_ready
/// (issued, unused, not yet expired; one row per person on the invitation).
/// </summary>
[DefaultClassOptions]
[NavigationItem(false)]
[DefaultProperty(nameof(InvitationNumber))]
[ModelDefault("Caption", "Active Invitation")]
[ModelDefault("AllowEdit", "False")]
[ModelDefault("AllowNew", "False")]
[ModelDefault("AllowDelete", "False")]
public class VwRdInvitationReady
{
    [Key]
    [Browsable(false)]
    public virtual Guid ID { get; set; }

    [Browsable(false)]
    public virtual Guid? PersonOid { get; set; }

    [ForeignKey(nameof(PersonOid))]
    [ModelDefault("Caption", "Full name")]
    public virtual Person Person { get; set; }

    [Browsable(false)]
    public virtual Guid? InvitationOid { get; set; }

    [ForeignKey(nameof(InvitationOid))]
    [ModelDefault("Caption", "Invitation #")]
    public virtual Invitation Invitation { get; set; }

    [Browsable(false)]
    public virtual string PersonName { get; set; }

    [ModelDefault("Caption", "Project")]
    public virtual string ProjectName { get; set; }

    [Browsable(false)]
    public virtual string ProjectNameRaw { get; set; }

    [Browsable(false)]
    public virtual string ProjectNameTm { get; set; }

    [Browsable(false)]
    public virtual int PersonRoleCode { get; set; }

    [ModelDefault("Caption", "Position")]
    public virtual string PositionLabel { get; set; }

    [ModelDefault("Caption", "Project")]
    public virtual string DepartmentLabel { get; set; }

    [ModelDefault("Caption", "Firm")]
    public virtual string SubcontractorLabel { get; set; }

    [Browsable(false)]
    public virtual string ApplicationNumber { get; set; }

    [Browsable(false)]
    public virtual DateTime? ApplicationDate { get; set; }

    [NotMapped]
    [ModelDefault("Caption", "Application number")]
    public string ApplicationNumberLabel => ApplicationNumber?.Trim() ?? string.Empty;

    [NotMapped]
    [ModelDefault("Caption", "Application date")]
    public string ApplicationDateLabel =>
        ApplicationDate is { Year: > 1900 } date ? date.ToString("dd.MM.yyyy") : string.Empty;

    [NotMapped]
    [ModelDefault("Caption", "App #")]
    public string ApplicationLabel
    {
        get
        {
            var lines = new List<string>();
            var number = ApplicationNumber?.Trim() ?? string.Empty;
            if (number.Length > 0)
                lines.Add(number);
            if (ApplicationDate is { } date && date.Year > 1900)
                lines.Add(date.ToString("dd.MM.yyyy"));
            return string.Join("\n", lines);
        }
    }

    [Browsable(false)]
    public virtual string InvitationNumber { get; set; }

    [Browsable(false)]
    public virtual DateTime? ExpirationDate { get; set; }

    [NotMapped]
    [ModelDefault("Caption", "Invitation validity")]
    public string ExpiresLabel => FormatExpires(ExpirationDate);

    [NotMapped]
    [ModelDefault("Caption", "NOT")]
    public string RemainingDaysNote => FormatRemainingDaysNote(ExpirationDate);

    /// <summary>Days from today until the invitation expires. Past dates are negative.</summary>
    public static string FormatRemainingDaysNote(DateTime? expiration)
    {
        if (expiration is not { Year: > 1900 } date)
            return string.Empty;

        var days = (date.Date - DateTime.Today).Days;
        return "geçerli gün sayı : " + days.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    public static string RemainingDaysCssClass(DateTime? expiration)
    {
        if (expiration is not { Year: > 1900 } date)
            return string.Empty;

        var days = (date.Date - DateTime.Today).Days;
        if (days < 0)
            return "rd-note--expired";
        if (days < 14)
            return "rd-note--low";
        if (days < 30)
            return "rd-note--soon";
        if (days < 90)
            return "rd-note--mid";
        return "rd-note--far";
    }

    [ModelDefault("DisplayFormat", "{0:dd.MM.yyyy}")]
    [ModelDefault("EditMask", "dd.MM.yyyy")]
    [ModelDefault("Caption", "Written")]
    public virtual DateTime? IssuedDate { get; set; }

    [ModelDefault("Caption", "Visa Period")]
    public virtual string VisaPeriodLabel { get; set; }

    [Browsable(false)]
    public virtual string VisaCategoryLabel { get; set; }

    [Browsable(false)]
    public virtual string VisaTypeLabel { get; set; }

    [ModelDefault("Caption", "Period · Category · Type")]
    public virtual string PeriodCategoryTypeLabel { get; set; }

    [Browsable(false)]
    public virtual DateTime? MigrationDate { get; set; }

    [Browsable(false)]
    public virtual string MigrationResultNumber { get; set; }

    [ModelDefault("Caption", "Migration")]
    public virtual string MigrationLabel { get; set; }

    [Browsable(false)]
    public virtual bool ValidityExtended { get; set; }

    [Browsable(false)]
    public virtual DateTime? Approval1Date { get; set; }

    [Browsable(false)]
    public virtual string Approval1Number { get; set; }

    [Browsable(false)]
    public virtual string Approval1Ministry { get; set; }

    [NotMapped]
    [ModelDefault("Caption", "Approval 1")]
    public string Approval1Label => FormatApproval(Approval1Date, Approval1Number, Approval1Ministry);

    [Browsable(false)]
    public virtual DateTime? Approval2Date { get; set; }

    [Browsable(false)]
    public virtual string Approval2Number { get; set; }

    [Browsable(false)]
    public virtual string Approval2Ministry { get; set; }

    [NotMapped]
    [ModelDefault("Caption", "Approval 2")]
    public string Approval2Label => FormatApproval(Approval2Date, Approval2Number, Approval2Ministry);

    [Browsable(false)]
    public virtual DateTime? Approval3Date { get; set; }

    [Browsable(false)]
    public virtual string Approval3Number { get; set; }

    [Browsable(false)]
    public virtual string Approval3Ministry { get; set; }

    [NotMapped]
    [ModelDefault("Caption", "Approval 3")]
    public string Approval3Label => FormatApproval(Approval3Date, Approval3Number, Approval3Ministry);

    [NotMapped]
    [ModelDefault("Caption", "Ministry approval")]
    public string LastApprovalLabel =>
        FormatLastApproval(
            Approval1Date, Approval1Number,
            Approval2Date, Approval2Number,
            Approval3Date, Approval3Number);

    [ModelDefault("Caption", "Project")]
    public virtual string StatusLabel { get; set; }

    [Browsable(false)]
    public virtual string StatusCssClass { get; set; }

    [Browsable(false)]
    public virtual bool IsArchived { get; set; }

    /// <summary>Same remaining-time buckets as Invitation Validity.</summary>
    public static (string Label, string CssClass) ExpirationBucket(int daysRemaining) =>
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

    private static string FormatExpires(DateTime? date)
    {
        if (date is not { } value || value.Year <= 1900)
            return string.Empty;

        var days = (value.Date - DateTime.Today).Days;
        var bucket = ExpirationBucket(days);
        return value.ToString("MMM dd, yyyy")
            + "\n"
            + ReportDashboardLocalization.Status(bucket.Label);
    }

    private static string FormatLastApproval(
        DateTime? approval1Date, string? approval1Number,
        DateTime? approval2Date, string? approval2Number,
        DateTime? approval3Date, string? approval3Number)
    {
        if (HasDecision(approval3Date, approval3Number))
            return FormatDecision(approval3Date, approval3Number);
        if (HasDecision(approval2Date, approval2Number))
            return FormatDecision(approval2Date, approval2Number);
        if (HasDecision(approval1Date, approval1Number))
            return FormatDecision(approval1Date, approval1Number);
        return string.Empty;
    }

    private static bool HasDecision(DateTime? date, string? number) =>
        date is { Year: > 1900 } || !string.IsNullOrWhiteSpace(number);

    private static string FormatDecision(DateTime? date, string? number)
    {
        var lines = new List<string>();
        if (date is { Year: > 1900 } value)
            lines.Add(value.ToString("dd.MM.yyyy"));
        var letter = number?.Trim() ?? string.Empty;
        if (letter.Length > 0)
            lines.Add("№ " + letter);
        return string.Join("\n", lines);
    }

    private static string FormatApproval(DateTime? date, string? number, string? ministry)
    {
        var name = ministry?.Trim() ?? string.Empty;
        if (name.Length == 0)
            return string.Empty;

        var letter = number?.Trim() ?? string.Empty;
        var lines = new List<string>();
        if (date is { } value && value.Year > 1900)
            lines.Add(value.ToString("dd.MM.yyyy"));
        if (letter.Length > 0)
            lines.Add("№ " + letter);
        if (lines.Count == 0)
            return name;
        lines.Add(name);
        return string.Join("\n", lines);
    }
}
