using DevExpress.Blazor;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor.Editors;
using DevExpress.ExpressApp.SystemModule;
using System.ComponentModel;
using Visa2026.Blazor.Server.Services;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Blazor.Server.Controllers;

/// <summary>
/// Makes the person document copies ListView link column open the preview slot for that row.
/// </summary>
public sealed class PersonDocumentCopiesListLinkClickController : ViewController<ListView>
{
    private Action<GridCustomizeElementEventArgs>? customizeElementHandler;
    private Action<GridCustomizeElementEventArgs>? previousCustomizeElement;
    private CancellationTokenSource? deferredApplyCts;
    private ListViewProcessCurrentObjectController? processCurrentObjectController;

    public PersonDocumentCopiesListLinkClickController()
    {
        TargetObjectType = typeof(Person);
        TargetViewId = "Person_ListView_Employees;Person_ListView_FamilyMembers;Person_ListView_TemporaryVisitors";
    }

    protected override void OnActivated()
    {
        base.OnActivated();
        processCurrentObjectController = Frame.GetController<ListViewProcessCurrentObjectController>();
        if (processCurrentObjectController != null)
            processCurrentObjectController.CustomHandleProcessSelectedItem += OnCustomHandleProcessSelectedItem;
    }

    protected override void OnViewControlsCreated()
    {
        base.OnViewControlsCreated();
        ApplyLinkClickHandlers();
        ScheduleDeferredApply();
    }

    private void ScheduleDeferredApply()
    {
        deferredApplyCts?.Cancel();
        deferredApplyCts?.Dispose();
        deferredApplyCts = new CancellationTokenSource();
        var token = deferredApplyCts.Token;
        _ = ApplyDeferredAsync(token);
    }

    private async Task ApplyDeferredAsync(CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(150, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        if (View is { IsDisposed: false })
            ApplyLinkClickHandlers();
    }

    private void OnCustomHandleProcessSelectedItem(object? sender, HandledEventArgs e)
    {
        if (PersonDocumentCopiesLinkClickGate.ConsumePending())
            e.Handled = true;
    }

    private void ApplyLinkClickHandlers()
    {
        if (View?.Editor is not DxGridListEditor { GridModel: { } gridModel })
            return;

        if (customizeElementHandler != null)
        {
            gridModel.CustomizeElement = previousCustomizeElement;
            customizeElementHandler = null;
            previousCustomizeElement = null;
        }

        previousCustomizeElement = gridModel.CustomizeElement;
        customizeElementHandler = e =>
        {
            previousCustomizeElement?.Invoke(e);
            ApplyLinkCellStyle(e);
        };
        gridModel.CustomizeElement = customizeElementHandler;
    }

    private void ApplyLinkCellStyle(GridCustomizeElementEventArgs e)
    {
        if (e.ElementType != GridElementType.DataCell || e.VisibleIndex < 0)
            return;

        if (!IsDocumentCopiesLinkColumn(e.Column))
            return;

        if (e.Grid.GetDataItem(e.VisibleIndex) is not Person person)
            return;

        var personId = ObjectSpace.GetKeyValue(person) is Guid guid
            ? guid
            : Guid.Empty;

        if (personId == Guid.Empty)
            return;

        const string linkClass = "app-person-document-copies-link";
        e.CssClass = string.IsNullOrEmpty(e.CssClass) ? linkClass : $"{e.CssClass} {linkClass}";
        e.Attributes["role"] = "button";
        e.Attributes["tabindex"] = "0";
        e.Attributes["title"] = Visa2026.Module.Localization.VisaUiMessages.Get("PersonDocumentCopies.Title");
        e.Attributes["data-person-id"] = personId.ToString("D");
    }

    private static bool IsDocumentCopiesLinkColumn(IGridColumn? column)
    {
        if (column == null)
            return false;

        if (column is DxGridDataColumn dataColumn)
        {
            return string.Equals(
                dataColumn.FieldName,
                nameof(Person.DocumentCopiesListLink),
                StringComparison.Ordinal);
        }

        return string.Equals(
            column.Name,
            nameof(Person.DocumentCopiesListLink),
            StringComparison.Ordinal);
    }

    protected override void OnDeactivated()
    {
        deferredApplyCts?.Cancel();
        deferredApplyCts?.Dispose();
        deferredApplyCts = null;

        if (processCurrentObjectController != null)
        {
            processCurrentObjectController.CustomHandleProcessSelectedItem -= OnCustomHandleProcessSelectedItem;
            processCurrentObjectController = null;
        }

        if (customizeElementHandler != null
            && View?.Editor is DxGridListEditor { GridModel: { } gridModel })
        {
            gridModel.CustomizeElement = previousCustomizeElement;
        }

        customizeElementHandler = null;
        previousCustomizeElement = null;
        base.OnDeactivated();
    }
}
