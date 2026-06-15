using System;
using System.Linq;
using DevExpress.ExpressApp;
using Visa2026.Module.Localization;

namespace Visa2026.Module.BusinessObjects;

public static class ProjectContractMinistryHelper
{
    public static int GetLegCount(ProjectContract? contract) =>
        contract?.MinistryLegs?.Count(l => l.ApprovingMinistry != null) ?? 0;

    public static bool HasConfiguredLegs(ProjectContract? contract) =>
        GetLegCount(contract) > 0;

    public static bool TryValidateLegSla(ProjectContract? contract, out string? errorMessage)
    {
        errorMessage = null;
        if (contract == null || !contract.IsActive)
            return true;

        var legs = contract.MinistryLegs?
            .Where(l => l.ApprovingMinistry != null)
            .OrderBy(l => l.Sequence)
            .ToList() ?? [];

        if (legs.Count == 0)
            return true;

        foreach (var leg in legs)
        {
            if (leg.MaxDaysInReview is not > 0)
            {
                errorMessage = VisaUiMessages.Format(
                    "ProjectContract.MinistryLegMaxDaysRequired",
                    leg.Sequence ?? 0);
                return false;
            }

            if (leg.WarningDaysBeforeMax is > 0 && leg.WarningDaysBeforeMax >= leg.MaxDaysInReview)
            {
                errorMessage = VisaUiMessages.Format(
                    "ProjectContract.MinistryLegWarningDaysInvalid",
                    leg.Sequence ?? 0);
                return false;
            }
        }

        return true;
    }

    public static void ApplySnapshot(IObjectSpace objectSpace, Application application, ProjectContract? contract)
    {
        if (application.ApprovalLegSnapshots == null)
            return;

        // Do not call ObservableCollection.Clear() — EF Core change tracking rejects the Reset notification.
        foreach (var existing in application.ApprovalLegSnapshots.ToList())
            objectSpace.Delete(existing);

        if (contract?.MinistryLegs == null)
            return;

        foreach (var leg in contract.MinistryLegs
                     .Where(l => l.ApprovingMinistry != null)
                     .OrderBy(l => l.Sequence))
        {
            var snapshot = objectSpace.CreateObject<ApplicationApprovalLegSnapshot>();
            snapshot.Application = application;
            snapshot.Sequence = leg.Sequence;
            snapshot.ApprovingMinistryId = leg.ApprovingMinistry.ID;
            snapshot.MinistryShortName = leg.ApprovingMinistry.ShortNameTm ?? leg.ApprovingMinistry.NameTm ?? string.Empty;
            snapshot.MinistryNameTm = leg.ApprovingMinistry.NameTm ?? string.Empty;
            snapshot.MaxDaysInReview = leg.MaxDaysInReview;
            snapshot.WarningDaysBeforeMax = leg.WarningDaysBeforeMax;
            application.ApprovalLegSnapshots.Add(snapshot);
        }
    }

    public static bool IsContractReferencedByApplications(ProjectContract contract, IObjectSpace objectSpace) =>
        objectSpace.GetObjectsQuery<Application>()
            .Any(a => a.ProjectContract != null && a.ProjectContract.ID == contract.ID);

    public static string? GetMinistryShortNameForLeg(Application? application, int leg)
    {
        if (application?.ApprovalLegSnapshots == null || leg < 1)
            return null;

        return application.ApprovalLegSnapshots
            .Where(s => s.Sequence == leg)
            .Select(s => s.MinistryShortName)
            .FirstOrDefault(s => !string.IsNullOrWhiteSpace(s));
    }

    public static string? GetMinistryShortNameForProgressStep(
        Application? application,
        string? stateCode,
        string? locationCode)
    {
        if (ApplicationProgressLegCodes.TryParseMinistryLegFromLocationCode(locationCode, out var legFromLocation))
            return GetMinistryShortNameForLeg(application, legFromLocation);

        if (ApplicationProgressLegCodes.TryParseMinistryLegFromStateCode(stateCode, out var legFromState))
            return GetMinistryShortNameForLeg(application, legFromState);

        return null;
    }
}
