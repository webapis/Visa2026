using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.DC;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.Persistent.Validation;

namespace Visa2026.Module.BusinessObjects;

public enum WordReportGenerationBatchStatus
{
    Queued = 0,
    Running = 1,
    Completed = 2,
    Failed = 3
}

/// <summary>Background Resminamalar (Word/Excel template bundle) job for one <see cref="Application"/>.</summary>
[DefaultClassOptions]
[NavigationItem(false)]
[DefaultProperty(nameof(DisplayName))]
public class WordReportGenerationBatch : BaseObject
{
    public WordReportGenerationBatch()
    {
        CreatedOnUtc = DateTime.UtcNow;
        Status = WordReportGenerationBatchStatus.Queued;
    }

    [NotMapped]
    [Browsable(false)]
    public string DisplayName => $"Resminamalar ({Status}) — {TotalReports} report(s)";

    [RuleRequiredField]
    public virtual DateTime CreatedOnUtc { get; set; }

    [MaxLength(256)]
    public virtual string RequestedBy { get; set; }

    public virtual WordReportGenerationBatchStatus Status { get; set; }

    public virtual int TotalReports { get; set; }

    public virtual int ProcessedReports { get; set; }

    [MaxLength(1024)]
    public virtual string ErrorMessage { get; set; }

    public virtual Guid? ApplicationID { get; set; }

    /// <summary>
    /// JSON array of catalog entry keys queued from the report package dialog.
    /// Null or empty means all applicable reports (legacy batches).
    /// </summary>
    [Browsable(false)]
    [FieldSize(FieldSizeAttribute.Unlimited)]
    public virtual string SelectedReportKeysJson { get; set; }

    [Browsable(false)]
    public virtual Application Application { get; set; }

    [ExpandObjectMembers(ExpandObjectMembers.Never)]
    public virtual FileData ZipFile { get; set; }
}
