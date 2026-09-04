namespace Visa2026.Module.BusinessObjects;

/// <summary>
/// Domain capability checks on <see cref="ApplicationType"/> (distinct from UI <c>Show*</c> flags).
/// Membership is seeded via <c>ApplicationTypeConfigurationCatalog.json</c>.
/// </summary>
public static class ApplicationTypeCapabilities
{
    public static bool CanIssueVisa(ApplicationType? applicationType) =>
        applicationType?.CanIssueVisa == true;

    public static bool CanIssueInvitation(ApplicationType? applicationType) =>
        applicationType?.CanIssueInvitation == true;

    public static bool CanIssueWorkPermit(ApplicationType? applicationType) =>
        applicationType?.CanIssueWorkPermit == true;

    /// <summary>
    /// Eligible parent type for <see cref="Visa.IssuingApplicationItem"/> (union of visa- and invitation-issuing).
    /// </summary>
    public static bool CanBeIssuingApplicationProfileInstanceForVisa(ApplicationType? applicationType) =>
        CanIssueVisa(applicationType) || CanIssueInvitation(applicationType);

    public static bool CanIssueVisa(ApplicationProfileInstance? application) =>
        ApplicationProfileConfigurationResolver.CanIssueVisa(application);

    public static bool CanIssueInvitation(ApplicationProfileInstance? application) =>
        ApplicationProfileConfigurationResolver.CanIssueInvitation(application);

    public static bool CanIssueWorkPermit(ApplicationProfileInstance? application) =>
        ApplicationProfileConfigurationResolver.CanIssueWorkPermit(application);

    public static bool CanIssueBorderZone(ApplicationProfileInstance? application) =>
        ApplicationProfileConfigurationResolver.CanIssueBorderZone(application);

    public static bool CanIssueRejection(ApplicationProfileInstance? application) =>
        ApplicationProfileConfigurationResolver.CanIssueRejection(application);

    public static bool CanBeIssuingApplicationProfileInstanceForVisa(ApplicationProfileInstance? application) =>
        ApplicationProfileConfigurationResolver.CanBeIssuingApplicationProfileInstanceForVisa(application);
}