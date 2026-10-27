using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;

namespace Visa2026.Module.BusinessObjects.Email;

/// <summary>
/// Per-person line of a <see cref="SentEmail"/>: which document-copies records were attached for
/// this person. This is the "emailed documents" log shown on the person.
/// </summary>
[DefaultClassOptions]
[NavigationItem(false)]
[DefaultProperty(nameof(PersonDisplayName))]
public class SentEmailPerson : BaseObject
{
    [VisibleInListView(false)]
    public virtual SentEmail SentEmail { get; set; }

    [VisibleInListView(false)]
    public virtual Person Person { get; set; }

    /// <summary>Copied at send time so history survives later edits to the person.</summary>
    [VisibleInListView(false)]
    [MaxLength(512)]
    public virtual string PersonDisplayName { get; set; }

    /// <summary>Human-readable record labels, one per line.</summary>
    [FieldSize(FieldSizeAttribute.Unlimited)]
    public virtual string RecordLabels { get; set; }

    /// <summary>Person document-copies record keys, one per line; used by Resend.</summary>
    [Browsable(false)]
    [FieldSize(FieldSizeAttribute.Unlimited)]
    public virtual string RecordKeys { get; set; }

    [MaxLength(512)]
    public virtual string AttachmentFileName { get; set; }

    [NotMapped]
    [ModelDefault("DisplayFormat", "{0:dd.MM.yyyy HH:mm}")]
    public DateTime? SentOn => SentEmail?.SentOn;

    [NotMapped]
    public string ToAddresses => SentEmail?.ToAddresses;

    [NotMapped]
    public string SentByName => SentEmail?.SentByName;

    [NotMapped]
    public SentEmailStatus? Status => SentEmail?.Status;

    [NotMapped]
    public string Part => SentEmail?.Part;
}
