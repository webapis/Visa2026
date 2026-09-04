using System;
using System.Linq;
using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.MigrationImport;

namespace Visa2026.Module.Services;

/// <summary>
/// Path A (instance-side create): optional <see cref="Visa.IssuingInvitationItem"/> when
/// <see cref="Visa.IssuingApplicationProfileInstance"/> is already set. Does not guess the issuing instance.
/// Path B (legacy import) sets <see cref="Visa.IssuingApplicationProfileInstance"/> in DataImporter.
/// Matches only issued output lines (<c>Invitation.ApplicationProfileInstance</c> = issuing instance),
/// never input M2M <see cref="ApplicationProfileInstance.InvitationItems"/>.
/// </summary>
public static class VisaIssuingLinkPathAMatcher
{
    /// <summary>
    /// Applies issuing-invitation-item defaults at most once while <paramref name="visa"/> is still a new object
    /// and <see cref="Visa.IssuingApplicationProfileInstance"/> is already known.
    /// </summary>
    public static void TryApplyOnce(Visa visa)
    {
        if (visa == null || visa.PathAIssuingLinksApplied)
            return;

        if (MigrationImportContext.IsDataImport)
            return;

        if (visa.IssuingApplicationProfileInstance == null)
            return;

        var objectSpace = ObjectSpaceHelper.Get(visa);
        if (objectSpace == null || !objectSpace.IsNewObject(visa))
            return;

        var person = visa.Passport?.Person;
        if (person == null)
            return;

        visa.PathAIssuingLinksApplied = true;

        if (!VisaIssuingApplicationProfileInstanceHelper.CanIssueInvitationForVisa(visa))
        {
            visa.IssuingInvitationItem = null;
            return;
        }

        var issuingApplication = visa.IssuingApplicationProfileInstance;
        var invitationItem = FindIssuingInvitationItem(objectSpace, visa, person.ID, issuingApplication);
        if (invitationItem == null)
            return;

        visa.IssuingInvitationItem = invitationItem;
    }

    private static InvitationItem? FindIssuingInvitationItem(
        IObjectSpace objectSpace,
        Visa visa,
        Guid personId,
        ApplicationProfileInstance? issuingApplication)
    {
        if (issuingApplication == null)
            return null;

        var visaId = visa.ID;
        var issueDate = visa.IssueDate;
        var applicationId = issuingApplication.ID;
        var applicationDate = issuingApplication.ApplicationDate.Date;

        var linkedInvitationIds = objectSpace.GetObjectsQuery<Visa>()
            .Where(v => v.ID != visaId && v.IssuingInvitationItem != null)
            .Select(v => v.IssuingInvitationItem!.ID)
            .ToList();

        var query = IssuedDocumentLifecycle.WhereInvitationItemNotUsed(
            IssuedDocumentLifecycle.WhereInvitationItemNotChanged(
                IssuedDocumentLifecycle.WhereInvitationItemNotCancelled(
                    objectSpace.GetObjectsQuery<InvitationItem>()
                        .Where(ii => ii.Person != null && ii.Person.ID == personId)
                        .Where(ii => ii.Invitation != null)
                        .Where(ii => ii.Invitation.ApplicationProfileInstance != null
                            && ii.Invitation.ApplicationProfileInstance.ID == applicationId))));

        if (linkedInvitationIds.Count > 0)
            query = query.Where(ii => !linkedInvitationIds.Contains(ii.ID));

        var candidates = query
            .OrderByDescending(ii => ii.Invitation!.IssuedDate)
            .ThenByDescending(ii => ii.Invitation!.ID)
            .AsEnumerable()
            .Where(ii => ii.Invitation!.IssuedDate.Date > applicationDate)
            .ToList();

        if (issueDate != default)
        {
            candidates = candidates
                .Where(ii => ii.Invitation!.IssuedDate.Date < issueDate.Date)
                .ToList();
        }

        return candidates.FirstOrDefault();
    }
}
