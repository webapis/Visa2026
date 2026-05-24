using System;
using DevExpress.ExpressApp.DC;
using DevExpress.Persistent.Base;

namespace Visa2026.Module.BusinessObjects.StateNotifications;

/// <summary>
/// In-memory notification row for UI prototyping (not persisted).
/// </summary>
[DomainComponent]
public class BoStateNotificationItem
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public BoStateNotificationSeverity Severity { get; set; }

    public BoStateNotificationStatus Status { get; set; }

    public BoStateNotificationCategory Category { get; set; } = BoStateNotificationCategory.ValidityState;

    public string BoType { get; set; } = string.Empty;

    public string StateCode { get; set; } = string.Empty;

    public string StateLabel { get; set; } = string.Empty;

    public string DisplayKey { get; set; } = string.Empty;

    public string PersonName { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    /// <summary>Short label for missing-data rows (e.g. "Passport", "Diploma copies").</summary>
    public string MissingItemLabel { get; set; }

    public DateTime? EventDate { get; set; }

    public int? DaysRemaining { get; set; }

    public DateTime DetectedAt { get; set; }

    public DateTime? HandledAt { get; set; }

    public string HandledBy { get; set; }

    /// <summary>Target record for "Open" (prototype only).</summary>
    public Guid? TargetBoId { get; set; }

    public string TargetBoTypeName { get; set; } = string.Empty;
}
