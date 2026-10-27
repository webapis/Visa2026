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

            HideColumn(listView, nameof(Person.Gender));

            if (employees)
            {
                EnsureEmployeeSummaryColumns(listView);
                EnsureColumn(listView, nameof(Person.EmployeeListCvCopies), 20, 160, null);
                HideColumn(listView, nameof(Person.DateOfBirth));
                HideColumn(listView, nameof(Person.Age));
                HideColumn(listView, nameof(Person.MaritalStatus));
                HideColumn(listView, nameof(Person.DocumentCopiesListLink));
                HideColumn(listView, nameof(Person.PersonalNumber));
                HideColumn(listView, nameof(Person.ProjectContract));
            }
            else
            {
                var temporary = viewId == "Person_ListView_TemporaryVisitors";
                EnsureEducationColumns(listView, temporary ? 10 : 12);
                EnsureActiveDocumentColumns(listView);
            }

            if (viewId == "Person_ListView_FamilyMembers")
            {
                EnsureColumn(listView, nameof(Person.PersonalNumber), 3, 120, "Personal Number");
                EnsureColumn(listView, nameof(Person.EmployeeListActivePassport), 11, 120, null);
                EnsureColumn(listView, nameof(Person.EmployeeListResidenceAddress), 17, 220, null);
                EnsureColumn(listView, nameof(Person.EmployeeListFamilyProofCopies), 18, 160, null);
                var archived = listView.Columns[nameof(Person.IsArchived)];
                if (archived != null)
                    archived.Index = 19;
            }
            else if (viewId == "Person_ListView_TemporaryVisitors")
            {
                EnsureColumn(listView, nameof(Person.EmployeeListActivePassport), 15, 120, null);
                EnsureColumn(listView, nameof(Person.EmployeeListResidenceAddress), 16, 220, null);
                var archived = listView.Columns[nameof(Person.IsArchived)];
                if (archived != null)
                    archived.Index = 17;
            }
        }
    }

    /// <summary>
    /// Read-only summary columns after Subcontractor (index 10) and before Archived.
    /// Captions come from the member display name so localization applies.
    /// </summary>
    private static void EnsureEmployeeSummaryColumns(IModelListView listView)
    {
        EnsureColumn(listView, nameof(Person.EmployeeListActivePassport), 3, 120, null);
        EnsureEducationColumns(listView, 11);
        EnsureColumn(listView, nameof(Person.EmployeeListPositionName), 13, 160, null);
        EnsureColumn(listView, nameof(Person.EmployeeListActualPositionName), 14, 180, null);
        EnsureColumn(listView, nameof(Person.EmployeeListDepartmentName), 15, 120, null);
        EnsureColumn(listView, nameof(Person.EmployeeListActiveVisaNumbers), 16, 140, null);
        EnsureColumn(listView, nameof(Person.EmployeeListActiveWorkPermitNumbers), 17, 160, null);
        EnsureColumn(listView, nameof(Person.EmployeeListActiveInvitationNumbers), 18, 150, null);
        EnsureColumn(listView, nameof(Person.EmployeeListResidenceAddress), 19, 220, null);
    }

    /// <summary>Family members and temporary visitors: the same three document stacks, before Archived.</summary>
    private static void EnsureActiveDocumentColumns(IModelListView listView)
    {
        var temporary = string.Equals(listView.Id, "Person_ListView_TemporaryVisitors", StringComparison.Ordinal);
        var start = temporary ? 12 : 14;
        EnsureColumn(listView, nameof(Person.EmployeeListActiveVisaNumbers), start, 140, null);
        EnsureColumn(listView, nameof(Person.EmployeeListActiveWorkPermitNumbers), start + 1, 160, null);
        EnsureColumn(listView, nameof(Person.EmployeeListActiveInvitationNumbers), start + 2, 150, null);

        var archived = listView.Columns[nameof(Person.IsArchived)];
        if (archived != null)
            archived.Index = start + 3;
    }

    private static void EnsureEducationColumns(IModelListView listView, int start)
    {
        EnsureColumn(listView, nameof(Person.EmployeeListEducationPlace), start, 160, null);
        EnsureColumn(listView, nameof(Person.EmployeeListEducationProfession), start + 1, 150, null);
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
