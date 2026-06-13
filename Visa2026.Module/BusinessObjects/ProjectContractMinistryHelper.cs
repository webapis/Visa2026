using System;
using System.Linq;
using DevExpress.ExpressApp;

namespace Visa2026.Module.BusinessObjects;

public static class ProjectContractMinistryHelper
{
    public static int GetLegCount(ProjectContract? contract) =>
        contract?.MinistryLegs?.Count(l => l.ApprovingMinistry != null) ?? 0;

    public static bool HasConfiguredLegs(ProjectContract? contract) =>
        GetLegCount(contract) > 0;

    public static void ApplySnapshot(Application application, ProjectContract? contract)
    {
        if (application.ApprovalLegSnapshots == null)
            return;

        application.ApprovalLegSnapshots.Clear();
        if (contract?.MinistryLegs == null)
            return;

        foreach (var leg in contract.MinistryLegs
                     .Where(l => l.ApprovingMinistry != null)
                     .OrderBy(l => l.Sequence))
        {
            application.ApprovalLegSnapshots.Add(new ApplicationApprovalLegSnapshot
            {
                Application = application,
                Sequence = leg.Sequence,
                ApprovingMinistryId = leg.ApprovingMinistry.ID,
                MinistryShortName = leg.ApprovingMinistry.ShortNameTm ?? leg.ApprovingMinistry.NameTm ?? string.Empty,
                MinistryNameTm = leg.ApprovingMinistry.NameTm ?? string.Empty
            });
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
