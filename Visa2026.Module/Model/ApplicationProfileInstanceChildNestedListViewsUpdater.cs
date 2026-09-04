using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;
using DevExpress.ExpressApp.Model.NodeGenerators;

namespace Visa2026.Module.Model;

/// <summary>
/// Child BO → ApplicationProfileInstances nested lists are browse-only
/// (person-related children: officers link Person only).
/// Invitation / WorkPermit / BorderZone / Rejection / IssuedVisas headers are 1:N on the instance (May produce), not skip-nav.
/// </summary>
public sealed class ApplicationProfileInstanceChildNestedListViewsUpdater : ModelNodesGeneratorUpdater<ModelViewsNodesGenerator>
{
    internal static readonly string[] ListViewIds =
    [
        "Passport_ApplicationProfileInstances_ListView",
        "Visa_ApplicationProfileInstances_ListView",
        "Education_ApplicationProfileInstances_ListView",
        "AddressOfResidence_ApplicationProfileInstances_ListView",
        "EmployeePositionHistory_ApplicationProfileInstances_ListView",
        "EmployeeSalary_ApplicationProfileInstances_ListView",
        "MedicalRecord_ApplicationProfileInstances_ListView",
        "WorkDuty_ApplicationProfileInstances_ListView",
        "InvitationItem_ApplicationProfileInstances_ListView",
        "WorkPermitItem_ApplicationProfileInstances_ListView",
        "BorderZoneItem_ApplicationProfileInstances_ListView",
        "TravelHistory_ApplicationProfileInstances_ListView",
        "ExternalArrival_ApplicationProfileInstances_ListView",
        "ExternalDeparture_ApplicationProfileInstances_ListView",
        "InternalArrival_ApplicationProfileInstances_ListView",
        "InternalDeparture_ApplicationProfileInstances_ListView",
    ];

    public override void UpdateNode(ModelNode node)
    {
        var views = (IModelViews)node;
        foreach (var listViewId in ListViewIds)
        {
            if (views[listViewId] is not IModelListView listView)
                continue;

            PersonNestedListViewsUpdater.ConfigureReadOnlyNestedListView(listView);
            PersonNestedListViewsUpdater.EnsureApplicationDateAndProfileColumns(listView);
        }
    }
}