using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Base;
using Visa2026.Module.Localization;

namespace Visa2026.Module.BusinessObjects;

/// <summary>
/// Report Dashboard: Used Invitations from vw_rd_invitation_used.
/// One invitation line that issued one visa. A later cancellation still counts.
/// </summary>
[DefaultClassOptions]
[NavigationItem(false)]
[DefaultProperty(nameof(InvitationNumber))]
[ModelDefault("Caption", "Used Invitations")]
[ModelDefault("AllowEdit", "False")]
[ModelDefault("AllowNew", "False")]
[ModelDefault("AllowDelete", "False")]
public class VwRdInvitationUsed
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

    [ModelDefault("Caption", "Bölümi")]
    public virtual string DepartmentLabel { get; set; }

    [ModelDefault("Caption", "Subcontractor")]
    public virtual string SubcontractorLabel { get; set; }

    [Browsable(false)]
    public virtual string ApplicationNumber { get; set; }

    [Browsable(false)]
    public virtual DateTime? ApplicationDate { get; set; }

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
    [ModelDefault("Caption", "Expires")]
    public string ExpiresLabel => FormatExpires(ExpirationDate);

    [Browsable(false)]
    public virtual DateTime? IssuedDate { get; set; }

    [ModelDefault("Caption", "Period")]
    public virtual string VisaPeriodLabel { get; set; }

    [Browsable(false)]
    public virtual string VisaCategoryLabel { get; set; }

    [Browsable(false)]
    public virtual string VisaTypeLabel { get; set; }

    [Browsable(false)]
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

    [Browsable(false)]
    public virtual Guid? VisaOid { get; set; }

    [ForeignKey(nameof(VisaOid))]
    [ModelDefault("Caption", "Visa #")]
    public virtual Visa Visa { get; set; }

    [Browsable(false)]
    public virtual string VisaNumber { get; set; }

    [Browsable(false)]
    public virtual DateTime? VisaIssueDate { get; set; }

    [NotMapped]
    [ModelDefault("Caption", "Issued date")]
    public string VisaIssueLabel => FormatDay(VisaIssueDate);

    [ModelDefault("Caption", "Place")]
    public virtual string VisaIssuedPlaceLabel { get; set; }

    [Browsable(false)]
    public virtual DateTime? VisaStartDate { get; set; }

    [NotMapped]
    [ModelDefault("Caption", "Start date")]
    public string VisaStartLabel => FormatDay(VisaStartDate);

    [Browsable(false)]
    public virtual DateTime? VisaEndDate { get; set; }

    [NotMapped]
    [ModelDefault("Caption", "End date")]
    public string VisaEndLabel => FormatDay(VisaEndDate);

    [ModelDefault("Caption", "Project")]
    public virtual string StatusLabel { get; set; }

    [Browsable(false)]
    public virtual string StatusCssClass { get; set; }

    [Browsable(false)]
    public virtual bool IsArchived { get; set; }

    private static string FormatDay(DateTime? date) =>
        date is { } value && value.Year > 1900 ? value.ToString("dd.MM.yyyy") : string.Empty;

    private static string FormatExpires(DateTime? date)
    {
        if (date is not { } value || value.Year <= 1900)
            return string.Empty;

        var days = (value.Date - DateTime.Today).Days;
        var bucket = VwRdInvitationReady.ExpirationBucket(days);
        return value.ToString("MMM dd, yyyy")
            + "\n"
            + ReportDashboardLocalization.Status(bucket.Label);
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
