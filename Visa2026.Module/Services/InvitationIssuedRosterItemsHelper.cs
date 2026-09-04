using System.Linq;
using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ApplicationPersonRoster;
using Visa2026.Module.Services.MigrationImport;

namespace Visa2026.Module.Services;

/// <summary>
/// Create-time helper: add InvitationItems for roster people not already on another invitation
/// of the same ApplicationProfileInstance. Do not call from Invitation.OnSaving — header edits
/// must not reshuffle people.
/// </summary>
public static class InvitationIssuedRosterItemsHelper
{
    public static void EnsureRosterInvitationItems(Invitation? invitation)
    {
        if (invitation == null || invitation.ApplicationProfileInstance == null)
            return;

        if (MigrationImportContext.IsDataImport)
            return;

        var objectSpace = ObjectSpaceHelper.Get(invitation);
        if (objectSpace == null)
            return;

        var instance = objectSpace.GetObject(invitation.ApplicationProfileInstance);
        if (instance == null)
            return;

        var appId = instance.ID;
        var alreadyOnThisApp = IssuedDocumentLifecycle.WhereInvitationItemNotCancelled(
            objectSpace.GetObjectsQuery<InvitationItem>()
                .Where(ii =>
                    ii.Person != null
                    && ii.Invitation != null
                    && ii.Invitation.ID != invitation.ID
                    && ii.Invitation.ApplicationProfileInstance != null
                    && ii.Invitation.ApplicationProfileInstance.ID == appId))
            .Select(ii => ii.Person!.ID)
            .ToHashSet();

        foreach (var person in ApplicationRosterHelper.GetRosterPeople(instance))
        {
            if (person == null || person.ID == Guid.Empty)
                continue;

            var personId = person.ID;
            if (alreadyOnThisApp.Contains(personId))
                continue;

            if (invitation.InvitationItems?.Any(ii => ii.Person != null && ii.Person.ID == personId) == true)
                continue;

            var trackedPerson = objectSpace.GetObject(person);
            if (trackedPerson == null)
                continue;

            var item = objectSpace.CreateObject<InvitationItem>();
            item.Invitation = invitation;
            item.Person = trackedPerson;
            item.Passport = ApplicationProfileInstancePersonValidItems.ResolvePassport(trackedPerson);
            invitation.InvitationItems ??= new System.Collections.ObjectModel.ObservableCollection<InvitationItem>();
            invitation.InvitationItems.Add(item);
        }
    }
}