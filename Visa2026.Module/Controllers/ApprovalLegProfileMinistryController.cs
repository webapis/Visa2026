using System.ComponentModel;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.Persistent.Base;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Localization;

namespace Visa2026.Module.Controllers;

/// <summary>
/// Validates <see cref="ApprovalLegProfile"/> ministry legs and blocks structural edits once referenced by applications.
/// </summary>
public sealed class ApprovalLegProfileMinistryController : ObjectViewController<DetailView, ApprovalLegProfile>
{
    protected override void OnActivated()
    {
        base.OnActivated();
        ObjectSpace.Committing += ObjectSpace_Committing;
    }

    protected override void OnDeactivated()
    {
        ObjectSpace.Committing -= ObjectSpace_Committing;
        base.OnDeactivated();
    }

    private void ObjectSpace_Committing(object sender, CancelEventArgs e)
    {
        ApprovalLegProfileMinistryHelper.PrepareLegsForCommit(ObjectSpace);

        foreach (var contract in ObjectSpace.GetObjectsToSave(false).OfType<ApprovalLegProfile>())
        {
            if (contract.IsActive
                && !ApprovalLegProfileMinistryHelper.TryValidateLegSla(ObjectSpace, contract, out var slaError))
            {
                e.Cancel = true;
                Application.ShowViewStrategy.ShowMessage(
                    slaError ?? VisaUiMessages.Get("MinistryReviewSlaSettings.NotConfigured"),
                    InformationType.Error,
                    6000,
                    InformationPosition.Top);
                return;
            }

            if (ObjectSpace.IsNewObject(contract))
                continue;

            if (!ApprovalLegProfileMinistryHelper.IsProfileReferencedByApplications(contract, ObjectSpace))
                continue;

            var original = ObjectSpace.GetObjectByKey<ApprovalLegProfile>(contract.ID);
            if (original == null)
                continue;

            if (!HasStructuralLegChanges(original, contract))
                continue;

            e.Cancel = true;
            Application.ShowViewStrategy.ShowMessage(
                VisaUiMessages.Get("ApprovalLegProfile.MinistryLegsStructuralEditBlocked"),
                InformationType.Error,
                6000,
                InformationPosition.Top);
            return;
        }
    }

    private static bool HasStructuralLegChanges(ApprovalLegProfile original, ApprovalLegProfile current)
    {
        var originalLegs = original.MinistryLegs?
            .Where(l => l.ApprovingMinistry != null)
            .OrderBy(l => l.Sequence)
            .Select(l => (l.Sequence, l.ApprovingMinistryId))
            .ToList() ?? [];

        var currentLegs = current.MinistryLegs?
            .Where(l => l.ApprovingMinistry != null)
            .OrderBy(l => l.Sequence)
            .Select(l => (l.Sequence, l.ApprovingMinistryId))
            .ToList() ?? [];

        if (originalLegs.Count != currentLegs.Count)
            return true;

        for (var i = 0; i < originalLegs.Count; i++)
        {
            if (originalLegs[i].Sequence != currentLegs[i].Sequence
                || originalLegs[i].ApprovingMinistryId != currentLegs[i].ApprovingMinistryId)
                return true;
        }

        return false;
    }
}
