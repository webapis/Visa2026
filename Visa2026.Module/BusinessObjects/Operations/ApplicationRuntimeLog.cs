using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using DevExpress.ExpressApp.ConditionalAppearance;
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;

namespace Visa2026.Module.BusinessObjects.Operations;

/// <summary>
/// Server-side runtime log row (Error/Critical/optional Warning) from <see cref="Services.RuntimeLogging.ApplicationRuntimeLogLoggerProvider"/>.
/// Navigation: Operations → Runtime errors (administrators).
/// </summary>
[DefaultClassOptions]
[NavigationItem(false)]
[DefaultProperty(nameof(Message))]
[ImageName("BO_Validation")]
[Appearance("ApplicationRuntimeLogCritical", Priority = 10, AppearanceItemType = "ViewItem", TargetItems = "*",
    Criteria = "Severity = ##Enum#Visa2026.Module.BusinessObjects.Operations.ApplicationRuntimeLogSeverity,Critical#",
    Context = "Any", BackColor = "LightCoral")]
[Appearance("ApplicationRuntimeLogError", Priority = 11, AppearanceItemType = "ViewItem", TargetItems = "*",
    Criteria = "Severity = ##Enum#Visa2026.Module.BusinessObjects.Operations.ApplicationRuntimeLogSeverity,Error#",
    Context = "Any", BackColor = "MistyRose")]
[Appearance("ApplicationRuntimeLogWarning", Priority = 12, AppearanceItemType = "ViewItem", TargetItems = "*",
    Criteria = "Severity = ##Enum#Visa2026.Module.BusinessObjects.Operations.ApplicationRuntimeLogSeverity,Warning#",
    Context = "Any", BackColor = "LightGoldenrodYellow")]
[Appearance("ApplicationRuntimeLogFixed", Priority = 13, AppearanceItemType = "ViewItem", TargetItems = "*",
    Criteria = "ResolutionStatus = ##Enum#Visa2026.Module.BusinessObjects.Operations.ApplicationRuntimeLogResolutionStatus,Fixed#",
    Context = "Any", BackColor = "Honeydew")]
public class ApplicationRuntimeLog : BaseObject
{
    [ModelDefault("AllowEdit", "False")]
    [ModelDefault("DisplayFormat", "{0:dd.MM.yyyy HH:mm:ss}")]
    [Index(0)]
    public virtual DateTime OccurredAtUtc { get; set; }

    [ModelDefault("AllowEdit", "False")]
    [Index(1)]
    public virtual ApplicationRuntimeLogSeverity Severity { get; set; }

    [ModelDefault("AllowEdit", "False")]
    [MaxLength(64)]
    public virtual string ErrorCode { get; set; }

    [ModelDefault("AllowEdit", "False")]
    [MaxLength(512)]
    public virtual string Category { get; set; }

    [ModelDefault("AllowEdit", "False")]
    [MaxLength(4000)]
    public virtual string Message { get; set; }

    [ModelDefault("AllowEdit", "False")]
    [MaxLength(512)]
    public virtual string ExceptionType { get; set; }

    [ModelDefault("AllowEdit", "False")]
    [VisibleInListView(false)]
    public virtual string StackTrace { get; set; }

    [ModelDefault("AllowEdit", "False")]
    [MaxLength(128)]
    public virtual string UserName { get; set; }

    [ModelDefault("AllowEdit", "False")]
    [MaxLength(64)]
    public virtual string CorrelationId { get; set; }

    [ModelDefault("AllowEdit", "False")]
    [MaxLength(512)]
    [VisibleInListView(false)]
    public virtual string RequestPath { get; set; }

    [ModelDefault("AllowEdit", "False")]
    [MaxLength(128)]
    [VisibleInListView(false)]
    public virtual string MachineName { get; set; }

    [ModelDefault("AllowEdit", "False")]
    public virtual ApplicationRuntimeLogDeploymentEnvironment DeploymentEnvironment { get; set; }

    [ModelDefault("AllowEdit", "False")]
    [MaxLength(128)]
    [VisibleInListView(false)]
    public virtual string ApplicationVersion { get; set; }

    [ModelDefault("AllowEdit", "False")]
    [VisibleInListView(false)]
    public virtual Guid? RelatedBatchId { get; set; }

    [ModelDefault("AllowEdit", "False")]
    [MaxLength(32)]
    [VisibleInListView(false)]
    public virtual string SentryEventId { get; set; }

    [ModelDefault("AllowEdit", "False")]
    [Index(8)]
    public virtual ApplicationRuntimeLogResolutionStatus ResolutionStatus { get; set; }

    [ModelDefault("AllowEdit", "False")]
    [ModelDefault("DisplayFormat", "{0:dd.MM.yyyy HH:mm:ss}")]
    [VisibleInListView(false)]
    public virtual DateTime? AcknowledgedAtUtc { get; set; }

    [ModelDefault("AllowEdit", "False")]
    [ModelDefault("DisplayFormat", "{0:dd.MM.yyyy HH:mm:ss}")]
    [VisibleInListView(false)]
    public virtual DateTime? ResolvedAtUtc { get; set; }

    [ModelDefault("AllowEdit", "False")]
    [MaxLength(128)]
    [VisibleInListView(false)]
    public virtual string ResolvedBy { get; set; }

    [ModelDefault("AllowEdit", "False")]
    [MaxLength(4000)]
    [VisibleInListView(false)]
    public virtual string ResolutionNotes { get; set; }

    [ModelDefault("AllowEdit", "False")]
    [MaxLength(64)]
    [VisibleInListView(false)]
    public virtual string FixCommitHash { get; set; }

    [ModelDefault("AllowEdit", "False")]
    [MaxLength(128)]
    [VisibleInListView(false)]
    public virtual string AgentRunId { get; set; }

    public override void OnCreated()
    {
        base.OnCreated();
        OccurredAtUtc = DateTime.UtcNow;
        ResolutionStatus = ApplicationRuntimeLogResolutionStatus.Open;
    }
}
