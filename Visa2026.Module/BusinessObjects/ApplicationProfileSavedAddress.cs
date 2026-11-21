using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DevExpress.ExpressApp.DC;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.Persistent.Validation;

namespace Visa2026.Module.BusinessObjects;

public enum ApplicationProfileSavedAddressField
{
    Address1 = 1,
    Address2 = 2,
}

/// <summary>
/// Tenant saved case Address 1 / Address 2 free-text suggestions (shared across all applications).
/// </summary>
[Table("ApplicationProfileSavedAddresses")]
[DefaultClassOptions]
[NavigationItem(false)]
public class ApplicationProfileSavedAddress : BaseObject
{
    [Browsable(false)]
    public virtual Guid ApplicationProfileId { get; set; }

    [RuleRequiredField]
    [ForeignKey(nameof(ApplicationProfileId))]
    public virtual ApplicationProfile ApplicationProfile { get; set; } = null!;

    [Browsable(false)]
    public virtual Guid ProjectContractId { get; set; }

    public virtual ApplicationProfileSavedAddressField Field { get; set; }

    [Browsable(false)]
    [MaxLength(450)]
    public virtual string TextKey { get; set; } = string.Empty;

    [FieldSize(FieldSizeAttribute.Unlimited)]
    public virtual string? Text { get; set; }

    public virtual int UseCount { get; set; } = 1;

    public virtual DateTime UsedAtUtc { get; set; }
}