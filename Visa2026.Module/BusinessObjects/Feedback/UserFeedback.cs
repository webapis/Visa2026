using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DevExpress.ExpressApp.ConditionalAppearance;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.Persistent.Validation;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.BusinessObjects.Feedback;

/// <summary>
/// Officer-submitted issue or feedback for the development team (page context, screenshot, attachments).
/// Navigation: Operations only — see <see cref="DatabaseUpdate.UserFeedbackModelUpdater"/>.
/// </summary>
[DefaultClassOptions]
[NavigationItem(false)]
[DefaultProperty(nameof(Summary))]
[ImageName("BO_Note")]
[Appearance("UserFeedbackTypeBug", Priority = 10, AppearanceItemType = "ViewItem", TargetItems = "FeedbackType",
    Criteria = "FeedbackType = ##Enum#Visa2026.Module.BusinessObjects.Feedback.UserFeedbackType,Bug#",
    Context = "Any", BackColor = "LightSalmon")]
[Appearance("UserFeedbackTypeIdea", Priority = 11, AppearanceItemType = "ViewItem", TargetItems = "FeedbackType",
    Criteria = "FeedbackType = ##Enum#Visa2026.Module.BusinessObjects.Feedback.UserFeedbackType,Idea#",
    Context = "Any", BackColor = "LightGreen")]
[Appearance("UserFeedbackTypeQuestion", Priority = 12, AppearanceItemType = "ViewItem", TargetItems = "FeedbackType",
    Criteria = "FeedbackType = ##Enum#Visa2026.Module.BusinessObjects.Feedback.UserFeedbackType,Question#",
    Context = "Any", BackColor = "LightSkyBlue")]
[Appearance("UserFeedbackTypeOther", Priority = 13, AppearanceItemType = "ViewItem", TargetItems = "FeedbackType",
    Criteria = "FeedbackType = ##Enum#Visa2026.Module.BusinessObjects.Feedback.UserFeedbackType,Other#",
    Context = "Any", BackColor = "Gainsboro")]
[Appearance("UserFeedbackStatusNew", Priority = 20, AppearanceItemType = "ViewItem", TargetItems = "Status",
    Criteria = "Status = ##Enum#Visa2026.Module.BusinessObjects.Feedback.UserFeedbackStatus,New#",
    Context = "Any", BackColor = "LightGoldenrodYellow")]
[Appearance("UserFeedbackStatusInProgress", Priority = 21, AppearanceItemType = "ViewItem", TargetItems = "Status",
    Criteria = "Status = ##Enum#Visa2026.Module.BusinessObjects.Feedback.UserFeedbackStatus,InProgress#",
    Context = "Any", BackColor = "LightSkyBlue")]
[Appearance("UserFeedbackStatusFixed", Priority = 22, AppearanceItemType = "ViewItem", TargetItems = "Status",
    Criteria = "Status = ##Enum#Visa2026.Module.BusinessObjects.Feedback.UserFeedbackStatus,Fixed#",
    Context = "Any", BackColor = "LightGreen")]
[Appearance("UserFeedbackStatusWontFix", Priority = 23, AppearanceItemType = "ViewItem", TargetItems = "Status",
    Criteria = "Status = ##Enum#Visa2026.Module.BusinessObjects.Feedback.UserFeedbackStatus,WontFix#",
    Context = "Any", BackColor = "Gainsboro")]
public class UserFeedback : BaseObject
{
    [RuleRequiredField]
    public virtual UserFeedbackType? FeedbackType { get; set; }

    public virtual UserFeedbackSeverity? Severity { get; set; } = UserFeedbackSeverity.Medium;

    [RuleRequiredField]
    [MaxLength(200)]
    public virtual string Summary { get; set; }

    [MaxLength(8000)]
    public virtual string Description { get; set; }

    [Index(2)]
    [VisibleInListView(true)]
    [VisibleInDetailView(true)]
    [ModelDefault("AllowEdit", "False")]
    public virtual UserFeedbackStatus? Status { get; set; } = UserFeedbackStatus.New;

    [ModelDefault("AllowEdit", "False")]
    [MaxLength(500)]
    public virtual string PageUrl { get; set; }

    [ModelDefault("AllowEdit", "False")]
    [MaxLength(200)]
    public virtual string ViewId { get; set; }

    [ModelDefault("AllowEdit", "False")]
    [MaxLength(300)]
    public virtual string ContextBoTypeName { get; set; }

    [ModelDefault("AllowEdit", "False")]
    public virtual Guid? ContextBoId { get; set; }

    [ModelDefault("AllowEdit", "False")]
    [MaxLength(80)]
    public virtual string AppVersion { get; set; }

    [Aggregated]
    [ExpandObjectMembers(ExpandObjectMembers.Never)]
    public virtual FileData Screenshot { get; set; }

    [Aggregated]
    [ExpandObjectMembers(ExpandObjectMembers.Never)]
    public virtual FileData Attachment { get; set; }

    [RuleRequiredField]
    [ModelDefault("AllowEdit", "False")]
    public virtual ApplicationUser SubmittedBy { get; set; }

    [ModelDefault("AllowEdit", "False")]
    [ModelDefault("DisplayFormat", "{0:dd.MM.yyyy HH:mm}")]
    public virtual DateTime SubmittedAt { get; set; }

    [ModelDefault("DisplayFormat", "{0:dd.MM.yyyy HH:mm}")]
    public virtual DateTime? FixedAt { get; set; }

    [ModelDefault("AllowEdit", "False")]
    public virtual ApplicationUser FixedBy { get; set; }

    [MaxLength(4000)]
    [VisibleInListView(false)]
    public virtual string DevNotes { get; set; }

    [MaxLength(500)]
    [VisibleInListView(false)]
    public virtual string ExternalIssueUrl { get; set; }

    [NotMapped]
    [Browsable(false)]
    public bool IsOpen => Status is UserFeedbackStatus.New or UserFeedbackStatus.InProgress;

    public override void OnCreated()
    {
        base.OnCreated();
        FeedbackType ??= UserFeedbackType.Bug;
        Severity ??= UserFeedbackSeverity.Medium;
        Status ??= UserFeedbackStatus.New;
        SubmittedAt = DateTime.Now;
    }
}
