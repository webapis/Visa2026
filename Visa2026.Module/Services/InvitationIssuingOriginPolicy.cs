using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.MigrationImport;

namespace Visa2026.Module.Services;

/// <summary>
/// Officer UI: new issued invitations must be created from an <see cref="ApplicationProfileInstance"/>
/// (Issued records) so <see cref="Invitation.ApplicationProfileInstance"/> is authoritative.
/// </summary>
public static class InvitationIssuingOriginPolicy
{
    public static bool RequiresApplicationProfileInstanceOnSave(Invitation? invitation)
    {
        if (invitation == null)
            return false;

        if (MigrationImportContext.IsDataImport)
            return false;

        var objectSpace = ObjectSpaceHelper.Get(invitation);
        if (objectSpace == null || !objectSpace.IsNewObject(invitation))
            return false;

        return true;
    }

    public static bool HasRequiredApplicationProfileInstance(Invitation? invitation)
    {
        if (!RequiresApplicationProfileInstanceOnSave(invitation))
            return true;

        return invitation!.ApplicationProfileInstance != null;
    }
}
