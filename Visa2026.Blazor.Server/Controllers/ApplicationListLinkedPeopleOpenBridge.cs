namespace Visa2026.Blazor.Server.Controllers;

/// <summary>
/// Opens the application named on a changed-invitation circle through the active list frame.
/// </summary>
public static class ApplicationListLinkedPeopleOpenBridge
{
    private static ApplicationListViewLinkedPeopleController? activeController;

    internal static void Attach(ApplicationListViewLinkedPeopleController controller) =>
        activeController = controller;

    internal static void Detach(ApplicationListViewLinkedPeopleController controller)
    {
        if (activeController == controller)
            activeController = null;
    }

    public static bool TryOpen(Guid applicationId)
    {
        if (activeController == null || applicationId == Guid.Empty)
            return false;

        activeController.OpenApplication(applicationId);
        return true;
    }
}