using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Base;

namespace Visa2026.Module.BusinessObjects;

/// <summary>
/// Report Dashboard: Invitation Process from vw_rd_invitation_in_process
/// (one row per roster person on an invitation-producing application that has no invitation yet).
/// </summary>
[DefaultClassOptions]
[NavigationItem(false)]
[DefaultProperty(nameof(PersonName))]
[ModelDefault("Caption", "Invitation Process")]
[ModelDefault("AllowEdit", "False")]
[ModelDefault("AllowNew", "False")]
[ModelDefault("AllowDelete", "False")]
public class VwRdInvitationInProcess
{
    [Key]
    [Browsable(false)]
    public virtual Guid ID { get; set; }

    [Browsable(false)]
    public virtual Guid ApplicationOid { get; set; }

    [Browsable(false)]
    public virtual Guid? PersonOid { get; set; }

    [ForeignKey(nameof(PersonOid))]
    [ModelDefault("Caption", "Full name")]
    public virtual Person Person { get; set; }

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

    [ModelDefault("Caption", "Visa Period")]
    public virtual string VisaPeriodLabel { get; set; }

    [Browsable(false)]
    public virtual DateTime? MigrationSubmittedDate { get; set; }

    [NotMapped]
    [ModelDefault("Caption", "Submitted to migration")]
    public string MigrationSubmittedLabel =>
        MigrationSubmittedDate is { Year: > 1900 } date ? date.ToString("dd.MM.yyyy") : string.Empty;

    [NotMapped]
    [ModelDefault("Caption", "Ministry approval")]
    public string LastApprovalLabel =>
        FormatLastDecision(
            Approval3Date, Approval3Number,
            Approval2Date, Approval2Number,
            Approval1Date, Approval1Number);

    [NotMapped]
    [ModelDefault("AllowEdit", "False")]
    [ModelDefault("Caption", "Invitation validity")]
    public string InvitationValidityLabel => string.Empty;

    [Browsable(false)]
    public virtual DateTime? MigrationDate { get; set; }

    [Browsable(false)]
    public virtual string MigrationResultNumber { get; set; }

    [ModelDefault("Caption", "Migration")]
    public virtual string MigrationLabel { get; set; }

    [Browsable(false)]
    public virtual bool ValidityExtended { get; set; }

    [ModelDefault("Caption", "Process state")]
    public virtual string StatusLabel { get; set; }

    [Browsable(false)]
    public virtual string StatusCssClass { get; set; }

    [Browsable(false)]
    public virtual string ProgressStateCode { get; set; }

    [Browsable(false)]
    public virtual string ChartStatusLabel { get; set; }

    [Browsable(false)]
    public virtual string PeriodCategoryTypeLabel { get; set; }

    [Browsable(false)]
    public virtual bool IsArchived { get; set; }

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

    private static string FormatLastDecision(
        DateTime? laterDate, string? laterNumber,
        DateTime? middleDate, string? middleNumber,
        DateTime? earlierDate, string? earlierNumber)
    {
        if (HasDecision(laterDate, laterNumber))
            return FormatDecision(laterDate, laterNumber);
        if (HasDecision(middleDate, middleNumber))
            return FormatDecision(middleDate, middleNumber);
        if (HasDecision(earlierDate, earlierNumber))
            return FormatDecision(earlierDate, earlierNumber);
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
}
