namespace Visa2026.Module.BusinessObjects.StateNotifications;

/// <summary>
/// What kind of issue the notification represents (prototype / future evaluators).
/// </summary>
public enum BoStateNotificationCategory
{
    /// <summary>Date-driven or process state on a document (expired, extension required, etc.).</summary>
    ValidityState = 0,

    /// <summary>Missing BO, missing attachments, or incomplete person profile for applications.</summary>
    DataCompleteness = 1,
}
