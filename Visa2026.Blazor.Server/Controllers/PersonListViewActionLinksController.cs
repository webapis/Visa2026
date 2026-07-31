using DevExpress.Blazor;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.Persistent.Base;
using DevExpress.ExpressApp.Blazor.Editors;
using DevExpress.ExpressApp.Blazor.Editors.Models;
using DevExpress.ExpressApp.SystemModule;
using System.ComponentModel;
using Visa2026.Blazor.Server.Services;
using Visa2026.Module;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Localization;
using Visa2026.Module.Services.PersonDossier;

namespace Visa2026.Blazor.Server.Controllers;

/// <summary>
/// Styles Person ListView action columns (Dossier + Document copies) and suppresses
/// row activation when those cells are clicked. One CustomizeElement chain — two
/// separate controllers fought each other on deferred re-apply.
/// </summary>
public sealed class PersonListViewActionLinksController : ViewController<ListView>
{
    private Action<GridCustomizeElementEventArgs>? customizeElementHandler;
    private Action<GridCustomizeElementEventArgs>? previousCustomizeElement;
    private CancellationTokenSource? deferredApplyCts;
    private ListViewProcessCurrentObjectController? processCurrentObjectController;
    private readonly SimpleAction openDossierFromListAction;
    private Guid dossierOpenPersonId;

    public PersonListViewActionLinksController()
    {
        TargetObjectType = typeof(Person);
        TargetViewId = "Person_ListView_Employees;Person_ListView_FamilyMembers;Person_ListView_TemporaryVisitors";

        // Unspecified = not on View toolbar; must stay Active so DoExecute works
        // (Active["Hidden"]=false deactivates the action and throws error 1007).
        openDossierFromListAction = new SimpleAction(this, "PersonListViewOpenDossier", PredefinedCategory.Unspecified);
        openDossierFromListAction.Caption = "Open dossier";
        openDossierFromListAction.SelectionDependencyType = SelectionDependencyType.Independent;
        openDossierFromListAction.Execute += OnOpenDossierFromListExecute;
    }

    protected override void OnActivated()
    {
        base.OnActivated();
        PersonDossierNavigationContext.SourceFrameValue = Frame;
        PersonListViewDossierOpenBridge.Attach(this);
        processCurrentObjectController = Frame.GetController<ListViewProcessCurrentObjectController>();
        if (processCurrentObjectController != null)
            processCurrentObjectController.CustomHandleProcessSelectedItem += OnCustomHandleProcessSelectedItem;
    }

    protected override void OnViewControlsCreated()
    {
        base.OnViewControlsCreated();
        ApplyColumnOrder();
        ApplyLinkClickHandlers();
        ScheduleDeferredApply();
    }

    private void ApplyColumnOrder()
    {
        if (View?.Editor is not DxGridListEditor gridListEditor)
            return;

        var indexes = new Dictionary<string, int>(StringComparer.Ordinal)
        {
            [nameof(Person.DossierListLink)] = 0,
            [nameof(Person.DocumentCopiesListLink)] = 1,
            [nameof(Person.FullName)] = 2,
            [nameof(Person.PersonalNumber)] = 3,
            [nameof(Person.DateOfBirth)] = 4,
            [nameof(Person.Age)] = 5,
            [nameof(Person.Gender)] = 6,
            [nameof(Person.MaritalStatus)] = 7,
            [nameof(Person.Nationality)] = 8,
            [nameof(Person.ProjectContract)] = 9,
            [nameof(Person.Subcontractor)] = 10,
        };

        gridListEditor.BeginUpdate();
        try
        {
            foreach (DxGridDataColumnModel columnModel in gridListEditor.GridDataColumnModels)
            {
                if (!string.IsNullOrEmpty(columnModel.FieldName)
                    && indexes.TryGetValue(columnModel.FieldName, out int visibleIndex))
                    columnModel.VisibleIndex = visibleIndex;
            }
        }
        finally
        {
            gridListEditor.EndUpdate();
        }
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
        {
            ApplyColumnOrder();
            ApplyLinkClickHandlers();
        }
    }

    private void OnCustomHandleProcessSelectedItem(object? sender, HandledEventArgs e)
    {
        if (PersonDossierLinkClickGate.ConsumePending()
            || PersonDocumentCopiesLinkClickGate.ConsumePending())
        {
            e.Handled = true;
        }
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
            ApplyActionLinkCellStyle(e);
        };
        gridModel.CustomizeElement = customizeElementHandler;
    }

    private void ApplyActionLinkCellStyle(GridCustomizeElementEventArgs e)
    {
        if (e.ElementType != GridElementType.DataCell || e.VisibleIndex < 0)
            return;

        var isDossier = IsColumn(e.Column, nameof(Person.DossierListLink));
        var isCopies = IsColumn(e.Column, nameof(Person.DocumentCopiesListLink));
        if (!isDossier && !isCopies)
            return;

        if (e.Grid.GetDataItem(e.VisibleIndex) is not Person person)
            return;

        var personId = ObjectSpace.GetKeyValue(person) is Guid guid
            ? guid
            : Guid.Empty;

        if (personId == Guid.Empty)
            return;

        var linkClass = isDossier ? "app-person-dossier-link" : "app-person-document-copies-link";
        var label = isDossier
            ? VisaUiMessages.Get("PersonDossier.Action.Open")
            : VisaUiMessages.Get("PersonDocumentCopies.Title");

        e.CssClass = string.IsNullOrEmpty(e.CssClass) ? linkClass : $"{e.CssClass} {linkClass}";
        e.Attributes["role"] = "button";
        e.Attributes["tabindex"] = "0";
        e.Attributes["title"] = label;
        e.Attributes["aria-label"] = label;
        e.Attributes["data-action"] = isDossier ? "person-dossier" : "person-document-copies";
        e.Attributes["data-person-id"] = personId.ToString("D");
    }

    private static bool IsColumn(IGridColumn? column, string fieldName)
    {
        if (column == null)
            return false;

        if (string.Equals(column.Name, fieldName, StringComparison.Ordinal))
            return true;

        if (column is DxGridDataColumn dataColumn
            && string.Equals(dataColumn.FieldName, fieldName, StringComparison.Ordinal))
            return true;

        var resolvedName = ResolveFieldName(column);
        return string.Equals(resolvedName, fieldName, StringComparison.Ordinal);
    }

    private static string? ResolveFieldName(IGridColumn column)
    {
        if (column is DxGridDataColumn dataColumn && !string.IsNullOrEmpty(dataColumn.FieldName))
            return dataColumn.FieldName;

        return column.Name;
    }

    protected override void OnDeactivated()
    {
        PersonDossierNavigationContext.SourceFrameValue = null;
        PersonListViewDossierOpenBridge.Detach(this);
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

    private void OnOpenDossierFromListExecute(object sender, SimpleActionExecuteEventArgs e)
    {
        var personId = dossierOpenPersonId;
        if (personId == Guid.Empty)
            personId = PersonDossierPendingOpenGate.Get(Application);

        if (personId == Guid.Empty)
            return;

        var person = ObjectSpace.GetObjectByKey<Person>(personId);
        if (person == null)
            return;

        var dossierView = PersonDossierOpenHelper.CreateDossierView(Application, ObjectSpace, person);
        if (dossierView == null)
            return;

        e.ShowViewParameters.CreatedView = dossierView;
        // Keep Employees / Family Members / Temporary visitors ListView tab open.
        e.ShowViewParameters.TargetWindow = TargetWindow.NewWindow;
    }

    /// <summary>Opens dossier via the same SimpleAction path as Person DetailView toolbar.</summary>
    internal void OpenDossierForPerson(Guid personId)
    {
        if (personId == Guid.Empty)
            return;

        PersonDossierLinkClickGate.MarkPending();
        PersonDossierPendingOpenGate.Set(Application, personId);
        dossierOpenPersonId = personId;
        openDossierFromListAction.DoExecute();
    }
}
