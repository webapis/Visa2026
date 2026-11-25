using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services.PersonIntake;

/// <summary>
/// Application profile instance progress for the person intake wizard.
/// The label is the same current-status text as the instance list.
/// </summary>
public static class PersonIntakeApplicationProfileInstanceState
{
    public static string Label(ApplicationProfileInstance instance) =>
        instance?.CurrentState ?? string.Empty;

    public static string Css(ApplicationProfileInstance instance) =>
        instance == null
            ? string.Empty
            : Css(ApplicationProfileInstanceProgressColorTab.ResolveBucketKey(instance));

    public static string Css(string bucketKey) => bucketKey switch
    {
        ApplicationProfileInstanceProgressColorTab.Issued => "is-valid",
        ApplicationProfileInstanceProgressColorTab.Rejected => "is-expired",
        ApplicationProfileInstanceProgressColorTab.Cancelled => "is-cancelled",
        ApplicationProfileInstanceProgressColorTab.ApprovalInProgress
            or ApplicationProfileInstanceProgressColorTab.LastApproval => "is-soon",
        _ => "is-info"
    };
}
