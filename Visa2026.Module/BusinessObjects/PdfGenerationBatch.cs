using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DevExpress.ExpressApp;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.Persistent.Validation;

namespace Visa2026.Module.BusinessObjects;

public enum PdfGenerationBatchStatus
{
    Queued = 0,
    Running = 1,
    Completed = 2,
    Failed = 3
}

[DefaultClassOptions]
[NavigationItem("Application")]
[DefaultProperty(nameof(DisplayName))]
public class PdfGenerationBatch : BaseObject, IObjectSpaceLink
{
    public PdfGenerationBatch()
    {
        CreatedOnUtc = DateTime.UtcNow;
        Status = PdfGenerationBatchStatus.Queued;
    }

    [NotMapped]
    [Browsable(false)]
    public string DisplayName => $"PDF Batch ({Status}) — {TotalItems} item(s)";

    [RuleRequiredField]
    public virtual DateTime CreatedOnUtc { get; set; }

    [MaxLength(256)]
    public virtual string RequestedBy { get; set; }

    public virtual PdfGenerationBatchStatus Status { get; set; }

    public virtual int TotalItems { get; set; }

    public virtual int ProcessedItems { get; set; }

    [MaxLength(1024)]
    public virtual string ErrorMessage { get; set; }

    [RuleRequiredField]
    [MaxLength(512)]
    public virtual string ItemKeyType { get; set; }

    [RuleRequiredField]
    public virtual string ItemKeysJson { get; set; }

    [ExpandObjectMembers(ExpandObjectMembers.Never)]
    public virtual FileData ZipFile { get; set; }

    #region IObjectSpaceLink
    [NotMapped]
    public IObjectSpace ObjectSpace { get; set; }
    #endregion
}
