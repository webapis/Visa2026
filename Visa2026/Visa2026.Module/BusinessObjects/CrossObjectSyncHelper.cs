using DevExpress.Persistent.BaseImpl.EF;
using System.Linq;

namespace Visa2026.Module.BusinessObjects
{
    public static class CrossObjectSyncHelper
    {
        public static void SyncOnSave(BaseObject sourceObject)
        {
            // This helper centralizes the logic where saving one object
            // triggers a status update on another related object.

            if (sourceObject is WorkPermitItem wpi)
            {
                if (wpi.WorkPermit?.Application != null && wpi.Employee != null)
                {
                    var appItem = wpi.WorkPermit.Application.ApplicationItems.FirstOrDefault(ai => ai.Person?.ID == wpi.Employee.ID);
                    if (appItem != null)
                    {
                        appItem.WorkPermitItemIsIssued = true;
                    }
                }
            }
            else if (sourceObject is InvitationItem ii)
            {
                if (ii.Invitation?.Application != null && ii.Person != null)
                {
                    var appItem = ii.Invitation.Application.ApplicationItems.FirstOrDefault(ai => ai.Person?.ID == ii.Person.ID);
                    if (appItem != null)
                    {
                        appItem.InvitationItemIsIssued = true;
                    }
                }
            }
            else if (sourceObject is RejectionItem ri)
            {
                if (ri.Rejection?.Application != null && ri.Person != null)
                {
                    var appItem = ri.Rejection.Application.ApplicationItems.FirstOrDefault(ai => ai.Person?.ID == ri.Person.ID);
                    if (appItem != null)
                    {
                        appItem.RejectionIssued = true;
                    }
                }
            }
            else if (sourceObject is Visa visa)
            {
                // Visa can update two different objects

                // 1. Update InvitationItem
                if (visa.HasInvitation && visa.Invitation != null && visa.Passport?.Person != null)
                {
                    var invitationItem = visa.Invitation.InvitationItems.FirstOrDefault(invItem => invItem.Person?.ID == visa.Passport.Person.ID);
                    if (invitationItem != null)
                    {
                        invitationItem.IsUsed = true;
                    }
                }

                // 2. Update ApplicationItem
                if (visa.Application != null && visa.Passport?.Person != null)
                {
                    var appItem = visa.Application.ApplicationItems.FirstOrDefault(ai => ai.Person?.ID == visa.Passport.Person.ID);
                    if (appItem != null)
                    {
                        appItem.VisaIssued = true;
                    }
                }
            }
        }
    }
}