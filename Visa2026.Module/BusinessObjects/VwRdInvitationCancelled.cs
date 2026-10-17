using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Base;
using Visa2026.Module.Localization;

namespace Visa2026.Module.BusinessObjects;

/// <summary>
/// Report Dashboard: Cancelled Invitation from vw_rd_invitation_cancelled.
/// One invitation line cancelled by a completed cancellation application.
/// </summary>
[DefaultClassOptions]
[NavigationItem(false)]
[DefaultProperty(nameof(InvitationNumber))]
[ModelDefault("Caption", "Cancelled Invitation")]
[ModelDefault("AllowEdit", "False")]
[ModelDefault("AllowNew", "False")]
[ModelDefault("AllowDelete", "False")]
public class VwRdInvitationCancelled
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

    [Browsable(false)]
    public virtual string InvitationNumber { get; set; }

    [Browsable(false)]
    public virtual DateTime? IssuedDate { get; set; }

    [NotMapped]
    [ModelDefault("Caption", "Issued date")]
    public string IssuedLabel => FormatDay(IssuedDate);

    [Browsable(false)]
    public virtual DateTime? ExpirationDate { get; set; }

    [NotMapped]
    [ModelDefault("Caption", "Expires")]
    public string ExpiresLabel => FormatExpires(ExpirationDate);

    [ModelDefault("Caption", "Period")]
    public virtual string VisaPeriodLabel { get; set; }

    [Browsable(false)]
    public virtual Guid? CancelApplicationOid { get; set; }

    [ForeignKey(nameof(CancelApplicationOid))]
    [Browsable(false)]
    public virtual ApplicationProfileInstance CancelApplication { get; set; }

    [Browsable(false)]
    public virtual string CancelApplicationNumber { get; set; }

    [NotMapped]
    [ModelDefault("Caption", "App #")]
    public string CancelApplicationLabel => CancelApplicationNumber?.Trim() ?? string.Empty;

    [Browsable(false)]
    public virtual DateTime? CancelApplicationDate { get; set; }

    [NotMapped]
    [ModelDefault("Caption", "Date")]
    public string CancelDateLabel => FormatDay(CancelApplicationDate);

    [Browsable(false)]
    public virtual DateTime? MigrationDate { get; set; }

    [Browsable(false)]
    public virtual string MigrationResultNumber { get; set; }

    [ModelDefault("Caption", "Migration")]
    public virtual string MigrationLabel { get; set; }

    [Browsable(false)]
    public virtual string ProjectName { get; set; }

    [Browsable(false)]
    public virtual string ProjectNameRaw { get; set; }

    [Browsable(false)]
    public virtual string ProjectNameTm { get; set; }

    [ModelDefault("Caption", "Bölümi")]
    public virtual string DepartmentLabel { get; set; }

    [Browsable(false)]
    public virtual int PersonRoleCode { get; set; }

    [Browsable(false)]
    public virtual string StatusLabel { get; set; }

    [Browsable(false)]
    public virtual string StatusCssClass { get; set; }

    [Browsable(false)]
    public virtual bool IsArchived { get; set; }

    private static string FormatDay(DateTime? date)
    {
        if (date is not { } value || value.Year <= 1900)
            return string.Empty;
        return value.ToString("dd.MM.yyyy");
    }

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
}