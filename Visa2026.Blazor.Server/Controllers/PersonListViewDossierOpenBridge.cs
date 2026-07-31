namespace Visa2026.Blazor.Server.Controllers;

/// <summary>
/// Lets JS interop open the dossier through the active Person ListView controller frame.
/// </summary>
public static class PersonListViewDossierOpenBridge
{
    private static PersonListViewActionLinksController? activeController;

    internal static void Attach(PersonListViewActionLinksController controller) =>
        activeController = controller;

    internal static void Detach(PersonListViewActionLinksController controller)
    {
        if (activeController == controller)
            activeController = null;
    }

    public static bool TryOpenDossier(Guid personId)
    {
        if (activeController == null || personId == Guid.Empty)
            return false;

        activeController.OpenDossierForPerson(personId);
        return true;
    }
}
