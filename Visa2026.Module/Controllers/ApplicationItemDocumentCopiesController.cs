using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using Microsoft.Extensions.DependencyInjection;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Localization;
using Visa2026.Module.Services.PreviewSlot;

namespace Visa2026.Module.Controllers;

/// <summary>
/// Opens linked document copies for one or more selected <see cref="ApplicationItem"/> rows from ListView.
/// </summary>
public class ApplicationItemDocumentCopiesController : ViewController<ListView>
{
    private SimpleAction viewDocumentCopiesAction;

    public ApplicationItemDocumentCopiesController()
    {
        TargetObjectType = typeof(ApplicationItem);

        viewDocumentCopiesAction = new SimpleAction(this, "ViewApplicationItemDocumentCopies", "View");
        viewDocumentCopiesAction.ImageName = "DocumentCopies";
        viewDocumentCopiesAction.SelectionDependencyType = SelectionDependencyType.Independent;
        viewDocumentCopiesAction.Execute += ViewDocumentCopiesAction_Execute;
    }

    protected override void OnActivated()
    {
        base.OnActivated();
        viewDocumentCopiesAction.Caption = VisaUiMessages.Get("ApplicationItemDocumentCopies.Title");
        View.SelectionChanged += View_SelectionChanged;
        UpdateActionState();
    }

    protected override void OnDeactivated()
    {
        View.SelectionChanged -= View_SelectionChanged;
        base.OnDeactivated();
    }

    private void View_SelectionChanged(object sender, EventArgs e)
    {
        UpdateActionState();
        CloseOwnedDocumentCopiesSlotIfOpen();
    }

    private void UpdateActionState()
    {
        viewDocumentCopiesAction.Enabled["Selection"] = GetSelectedItems().Count > 0;
    }

    private void ViewDocumentCopiesAction_Execute(object sender, SimpleActionExecuteEventArgs e)
    {
        var itemIds = GetSelectedItemIds();
        if (itemIds.Count < 1)
        {
            Application.ShowViewStrategy.ShowMessage(
                VisaUiMessages.Get("Pdf.SelectAtLeastOneItem"),
                InformationType.Warning);
            return;
        }

        OpenDocumentCopiesSlot(itemIds);
    }

    /// <summary>
    /// Selection after open is stale — close the catalog so the officer re-opens for the new scope.
    /// </summary>
    private void CloseOwnedDocumentCopiesSlotIfOpen()
    {
        var slotService = Application.ServiceProvider.GetService<IVisaPreviewSlotService>();
        if (slotService == null)
            return;

        var state = slotService.State;
        if (state.Mode != VisaPreviewSlotMode.DocumentCopies)
            return;

        var ownerViewId = VisaPreviewSlotViewHelper.ResolveOwnerViewId(View);
        if (string.IsNullOrEmpty(ownerViewId)
            || !string.Equals(state.OwnerViewId, ownerViewId, StringComparison.Ordinal))
        {
            return;
        }

        slotService.CloseAsync().GetAwaiter().GetResult();
    }

    private void OpenDocumentCopiesSlot(
        IReadOnlyList<Guid> itemIds,
        IVisaPreviewSlotService? slotService = null,
        string? ownerViewId = null)
    {
        if (itemIds.Count < 1)
            return;

        slotService ??= Application.ServiceProvider.GetService<IVisaPreviewSlotService>();
        if (slotService == null)
        {
            Application.ShowViewStrategy.ShowMessage(
                VisaUiMessages.Get("ApplicationItemDocumentCopies.Preview.Error"),
                InformationType.Error);
            return;
        }

        ownerViewId ??= VisaPreviewSlotViewHelper.ResolveOwnerViewId(View);
        slotService.OpenDocumentCopiesAsync(new DocumentCopiesSlotRequest
        {
            ApplicationItemIds = itemIds,
        }, ownerViewId).GetAwaiter().GetResult();
    }

    private List<Guid> GetSelectedItemIds()
    {
        return GetSelectedItems()
            .Select(item => View.ObjectSpace.GetKeyValue(item))
            .Where(key => key != null)
            .Select(key => key is Guid guid ? guid : Guid.Parse(Convert.ToString(key, System.Globalization.CultureInfo.InvariantCulture)))
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToList();
    }

    private List<ApplicationItem> GetSelectedItems()
    {
        var selected = View.SelectedObjects?
            .OfType<ApplicationItem>()
            .Where(item => item != null)
            .ToList();

        if (selected is { Count: > 0 })
            return selected;

        if (View.CurrentObject is ApplicationItem current)
            return new List<ApplicationItem> { current };

        return new List<ApplicationItem>();
    }
}
