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
}
