using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using DevExpress.ExpressApp.DC;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;

namespace Visa2026.Module.BusinessObjects;

/// <summary>
/// Extra scan or PDF stored on a progress step. The first file for a slot stays on
/// <see cref="ApplicationProfileInstanceProgress.MinistryLetterFile"/> or
/// <see cref="ApplicationProfileInstanceProgress.SubmissionDocumentFile"/>.
/// </summary>
[Table("ApplicationProfileInstanceProgressFiles")]
[NavigationItem(false)]
[XafDisplayName("Progress file")]
public class ApplicationProfileInstanceProgressFile : BaseObject
{
    [Browsable(false)]
    public virtual Guid ProgressId { get; set; }

    [Browsable(false)]
    public virtual ApplicationProfileInstanceProgress Progress { get; set; }

    [Aggregated, ExpandObjectMembers(ExpandObjectMembers.Never)]
    [VisibleInListView(false)]
    [VisibleInLookupListView(false)]
    public virtual FileData File { get; set; }

    /// <summary><see cref="ApplicationProfileInstanceProgressFiles.MinistryLetter"/> or <see cref="ApplicationProfileInstanceProgressFiles.SubmissionDocument"/>.</summary>
    [Browsable(false)]
    public virtual int Slot { get; set; }

    [Browsable(false)]
    public virtual int SortOrder { get; set; }
}
