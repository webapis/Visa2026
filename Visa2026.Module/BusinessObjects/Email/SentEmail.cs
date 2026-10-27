using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DevExpress.ExpressApp.ConditionalAppearance;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.DC;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;

namespace Visa2026.Module.BusinessObjects.Email;

public enum SentEmailStatus
{
    Sent = 0,
    Failed = 1
}

/// <summary>
/// One emailed document-copies message (sent or failed). Attachment bytes are not stored; Resend
/// rebuilds the PDFs from the records listed on <see cref="Persons"/>.
/// </summary>
[DefaultClassOptions]
[NavigationItem("Email")]
[ImageName("Actions_Send")]
[DefaultProperty(nameof(Subject))]
[Appearance("SentEmailFailed", AppearanceItemType = "ViewItem", TargetItems = nameof(Status),
    Criteria = "Status = ##Enum#Visa2026.Module.BusinessObjects.Email.SentEmailStatus,Failed#", FontColor = "#C62828")]
public class SentEmail : BaseObject
{
    public SentEmail()
    {
        Persons = new ObservableCollection<SentEmailPerson>();
    }

    [Browsable(false)]
    public virtual DateTime SentOnUtc { get; set; }

    [NotMapped]
    [ModelDefault("DisplayFormat", "{0:dd.MM.yyyy HH:mm}")]
    public DateTime SentOn => DateTime.SpecifyKind(SentOnUtc, DateTimeKind.Utc).ToLocalTime();

    /// <summary>Shared by all parts of one send that was split into several emails.</summary>
    [Browsable(false)]
    public virtual Guid? BatchId { get; set; }

    [Browsable(false)]
    public virtual int PartNumber { get; set; } = 1;

    [Browsable(false)]
    public virtual int PartCount { get; set; } = 1;

    /// <summary>"2 of 3" for split sends; empty for a single email.</summary>
    [NotMapped]
    public string Part => PartCount > 1 ? $"{PartNumber} / {PartCount}" : string.Empty;

    public virtual SentEmailStatus Status { get; set; }

    public virtual ApplicationUser SentBy { get; set; }

    [MaxLength(256)]
    public virtual string SentByName { get; set; }

    /// <summary>Mailbox the message went out from (shared SMTP account or the officer's own mailbox).</summary>
    [MaxLength(320)]
    public virtual string FromAddress { get; set; }

    /// <summary>Semicolon-separated recipient addresses.</summary>
    [FieldSize(FieldSizeAttribute.Unlimited)]
    public virtual string ToAddresses { get; set; }

    [FieldSize(FieldSizeAttribute.Unlimited)]
    public virtual string CcAddresses { get; set; }

    [MaxLength(512)]
    public virtual string Subject { get; set; }

    [FieldSize(FieldSizeAttribute.Unlimited)]
    public virtual string Body { get; set; }

    [MaxLength(2048)]
    public virtual string ErrorMessage { get; set; }

    public virtual int PersonCount { get; set; }

    public virtual int AttachmentCount { get; set; }

    public virtual long AttachmentTotalBytes { get; set; }

    [NotMapped]
    [Browsable(false)]
    public string AttachmentSizeText => $"{AttachmentTotalBytes / 1048576d:0.0} MB";

    [FieldSize(FieldSizeAttribute.Unlimited)]
    public virtual string AttachmentNames { get; set; }

    /// <summary>Where the officer started, for example "Passports (5 records)".</summary>
    [MaxLength(256)]
    public virtual string SourceDescription { get; set; }

    [DevExpress.ExpressApp.DC.Aggregated]
    public virtual IList<SentEmailPerson> Persons { get; set; }
}
