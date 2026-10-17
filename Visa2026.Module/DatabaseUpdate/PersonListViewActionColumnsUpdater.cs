using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;
using DevExpress.ExpressApp.Model.NodeGenerators;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Person typed ListViews: dossier + document-copies icon columns before Full Name.
/// </summary>
public sealed class PersonListViewActionColumnsUpdater : ModelNodesGeneratorUpdater<ModelViewsNodesGenerator>
{
    private static readonly string[] TargetViewIds =
    [
        "Person_ListView_Employees",
        "Person_ListView_FamilyMembers",
        "Person_ListView_TemporaryVisitors",
    ];

    public override void UpdateNode(ModelNode node)
    {
        var views = (IModelViews)node;
        foreach (var viewId in TargetViewIds)
        {
            if (views[viewId] is not IModelListView listView)
                continue;

            var employees = viewId == "Person_ListView_Employees";
            EnsureActionColumns(listView, includeDocumentCopies: !employees);

            if (employees)
            {
                EnsureEmployeeSummaryColumns(listView);
                HideColumn(listView, nameof(Person.DateOfBirth));
                HideColumn(listView, nameof(Person.Age));
                HideColumn(listView, nameof(Person.MaritalStatus));
                HideColumn(listView, nameof(Person.DocumentCopiesListLink));
            }

            if (viewId == "Person_ListView_FamilyMembers")
            {
                EnsureColumn(listView, nameof(Person.PersonalNumber), 3, 120, "Personal Number");
            }
        }
    }

    /// <summary>
    /// Read-only summary columns after Subcontractor (index 10) and before Archived.
    /// Captions come from the member display name so localization applies.
    /// </summary>
    private static void EnsureEmployeeSummaryColumns(IModelListView listView)
    {
        EnsureColumn(listView, nameof(Person.EmployeeListPositionName), 11, 160, null);
        EnsureColumn(listView, nameof(Person.EmployeeListActualPositionName), 12, 180, null);
        EnsureColumn(listView, nameof(Person.EmployeeListDepartmentName), 13, 120, null);
        EnsureColumn(listView, nameof(Person.EmployeeListActiveVisaNumbers), 14, 140, null);
        EnsureColumn(listView, nameof(Person.EmployeeListActiveWorkPermitNumbers), 15, 160, null);
        EnsureColumn(listView, nameof(Person.EmployeeListResidenceAddress), 16, 220, null);
    }

    private static void EnsureActionColumns(IModelListView listView, bool includeDocumentCopies)
    {
        EnsureColumn(listView, nameof(Person.DossierListLink), 0, 56, "Dossier");
        if (includeDocumentCopies)
            EnsureColumn(listView, nameof(Person.DocumentCopiesListLink), 1, 56, "Copies");

        var fullName = listView.Columns[nameof(Person.FullName)]
            ?? listView.Columns.AddNode<IModelColumn>(nameof(Person.FullName));
        fullName.PropertyName = nameof(Person.FullName);
        fullName.Index = 2;
        if (fullName.Width <= 0)
            fullName.Width = 140;
    }

    private static void EnsureColumn(
        IModelListView listView,
        string propertyName,
        int index,
        int width,
        string caption)
    {
        var column = listView.Columns[propertyName]
            ?? listView.Columns.AddNode<IModelColumn>(propertyName);
        column.PropertyName = propertyName;
        column.Index = index;
        column.Width = width;
        if (!string.IsNullOrEmpty(caption))
            column.Caption = caption;
    }

    private static void HideColumn(IModelListView listView, string propertyName)
    {
        var column = listView.Columns[propertyName];
        if (column != null)
            column.Index = -1;
    }
}
