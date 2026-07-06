using DevExpress.Blazor;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor.Editors;
using DevExpress.ExpressApp.Blazor.Editors.Models;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Model;

namespace Visa2026.Blazor.Server.Controllers;

/// <summary>Ministry legs nested list: disable interactive column sort; keep sequence order.</summary>
public sealed class ApprovalLegProfileMinistryLegGridSortLockController : ViewController<ListView>
{
    public ApprovalLegProfileMinistryLegGridSortLockController()
    {
        TargetObjectType = typeof(ApprovalLegProfileMinistryLeg);
    }

    protected override void OnViewControlsCreated()
    {
        base.OnViewControlsCreated();
        if (View.Id != ApprovalLegProfileMinistryLegViewsUpdater.NestedListViewId)
            return;

        if (View.Editor is not DxGridListEditor gridListEditor)
            return;

        gridListEditor.BeginUpdate();
        try
        {
            gridListEditor.GridModel.AllowSort = false;

            foreach (DxGridDataColumnModel columnModel in gridListEditor.GridDataColumnModels)
                columnModel.AllowSort = false;

            var sequenceColumn = gridListEditor.GridDataColumnModels
                .FirstOrDefault(column => string.Equals(
                    column.FieldName,
                    nameof(ApprovalLegProfileMinistryLeg.Sequence),
                    StringComparison.Ordinal));
            if (sequenceColumn == null)
                return;

            sequenceColumn.AllowSort = false;
            sequenceColumn.SortIndex = 0;
            sequenceColumn.SortOrder = GridColumnSortOrder.Ascending;
        }
        finally
        {
            gridListEditor.EndUpdate();
        }
    }
}