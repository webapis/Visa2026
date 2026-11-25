using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;
using DevExpress.ExpressApp.Model.NodeGenerators;
using Visa2026.Module;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Employees and Family Members keep dossier and full name only.
/// Temporary visitors keep the document columns. Incomplete lists are cloned from All.
/// </summary>
public sealed class PersonListViewActionColumnsUpdater : ModelNodesGeneratorUpdater<ModelViewsNodesGenerator>
{
    private static readonly string[] TargetViewIds =
    [
        PersonRosterListViewIds.Employees,
        PersonRosterListViewIds.FamilyMembers,
        PersonRosterListViewIds.TemporaryVisitors,
    ];

    public override void UpdateNode(ModelNode node)
    {
        var views = (IModelViews)node;
        foreach (var viewId in TargetViewIds)
        {
            if (views[viewId] is not IModelListView listView)
                continue;

            // Roster grids page in the database. Client mode loaded every person before the first row painted.
            listView.DataAccessMode = CollectionSourceDataAccessMode.Server;

            if (viewId is PersonRosterListViewIds.Employees or PersonRosterListViewIds.FamilyMembers)
            {
                KeepFullNameAndDossier(listView);
                continue;
            }

            EnsureActionColumns(listView, includeDocumentCopies: true);
            HideColumn(listView, nameof(Person.Gender));
            EnsureEducationColumns(listView, 10);
            EnsureActiveDocumentColumns(listView);

            if (viewId == PersonRosterListViewIds.TemporaryVisitors)
            {
                EnsureColumn(listView, nameof(Person.EmployeeListActivePassport), 15, 166, null);
                EnsureColumn(listView, nameof(Person.EmployeeListResidenceAddress), 16, 220, null);
                var archived = listView.Columns[nameof(Person.IsArchived)];
                if (archived != null)
                    archived.Index = 17;
            }
        }

        PersonIncompleteRosterListViewSync.EnsureAll(views);
    }

    /// <summary>
    /// Employees and Family Members show only the dossier link and full name.
    /// The other roster columns are calculated per row and keep the search grid slow.
    /// </summary>
    public static bool KeepFullNameAndDossier(IModelListView listView)
    {
        EnsureActionColumns(listView, includeDocumentCopies: false);
        var changed = false;
        var fullName = listView.Columns[nameof(Person.FullName)];
        if (fullName != null && fullName.Width < PersonRosterListViewIds.FullNameColumnWidth)
        {
            fullName.Width = PersonRosterListViewIds.FullNameColumnWidth;
            changed = true;
        }
        foreach (IModelColumn column in listView.Columns)
        {
            var name = string.IsNullOrEmpty(column.PropertyName) ? column.Id : column.PropertyName;
            if (name is nameof(Person.DossierListLink) or nameof(Person.FullName)
                or nameof(Person.IncompleteMissingAreasDisplay) or nameof(Person.IncompleteNotes))
                continue;
            if (column.Index < 0)
                continue;

            column.Index = -1;
            changed = true;
        }

        return changed;
    }

    /// <summary>
    /// Missing areas and notes stay on the list model so the Incomplete tab can show them.
    /// The tab controller hides them on All and Search.
    /// </summary>
    public static bool EnsureIncompleteInfoColumns(IModelListView listView)
    {
        var changed = false;
        (string Name, int Index, int Width)[] specs =
        [
            (nameof(Person.IncompleteMissingAreasDisplay), 3, 280),
            (nameof(Person.IncompleteNotes), 4, 220),
        ];
        foreach (var spec in specs)
        {
            var column = listView.Columns[spec.Name] ?? listView.Columns.AddNode<IModelColumn>(spec.Name);
            if (!string.Equals(column.PropertyName, spec.Name, StringComparison.Ordinal))
            {
                column.PropertyName = spec.Name;
                changed = true;
            }

            if (column.Index != spec.Index)
            {
                column.Index = spec.Index;
                changed = true;
            }

            if (column.Width < spec.Width)
            {
                column.Width = spec.Width;
                changed = true;
            }
        }

        return changed;
    }

    /// <summary>Temporary visitors: the three document stacks, before Archived.</summary>
    private static void EnsureActiveDocumentColumns(IModelListView listView)
    {
        var temporary = string.Equals(listView.Id, PersonRosterListViewIds.TemporaryVisitors, StringComparison.Ordinal);
        var start = temporary ? 12 : 14;
        EnsureColumn(listView, nameof(Person.EmployeeListActiveVisaNumbers), start, 193, null);
        EnsureColumn(listView, nameof(Person.EmployeeListActiveWorkPermitNumbers), start + 1, 164, null);
        EnsureColumn(listView, nameof(Person.EmployeeListActiveInvitationNumbers), start + 2, 204, null);

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