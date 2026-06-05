using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Editors;
using DevExpress.Persistent.BaseImpl.EF;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Controllers;

namespace Visa2026.Blazor.Server.Controllers;

/// <summary>
/// Blazor lookup "New" on <see cref="ApplicationItem"/> reference fields opens a popup
/// <see cref="DetailView"/> while the parent stays open. Hook <see cref="XafApplication.DetailViewCreated"/>
/// to prefill and lock context-driven fields on the new object.
/// </summary>
public sealed class ApplicationItemLookupDefaultsController : ObjectViewController<DetailView, ApplicationItem>
{
    protected override void OnActivated()
    {
        base.OnActivated();
        Application.DetailViewCreated += OnApplicationDetailViewCreated;
    }

    protected override void OnDeactivated()
    {
        Application.DetailViewCreated -= OnApplicationDetailViewCreated;
        base.OnDeactivated();
    }

    private void OnApplicationDetailViewCreated(object? sender, DetailViewCreatedEventArgs e)
    {
        if (View.CurrentObject is not ApplicationItem appItem
            || e.View is not DetailView childDetailView
            || childDetailView.CurrentObject is not BaseObject linkedObject
            || !childDetailView.ObjectSpace.IsNewObject(linkedObject))
        {
            return;
        }

        switch (linkedObject)
        {
            case Visa visa:
                ApplyVisaDefaults(visa, appItem, childDetailView);
                break;
            case Passport:
            case EmployeePositionHistory:
            case Education:
            case AddressOfResidence:
            case MedicalRecord:
            case WorkDuty:
            case EmployeeSalary:
                ApplyPersonLinkedDefaults(linkedObject, appItem, childDetailView);
                break;
        }
    }

    private void ApplyVisaDefaults(Visa visa, ApplicationItem appItem, DetailView visaDetailView)
    {
        if (!ApplicationItemVisaDefaults.TryApply(visa, appItem, visaDetailView.ObjectSpace, Application))
            return;

        LockPropertyWhenReady(visaDetailView, nameof(Visa.Passport), ApplicationItemVisaDefaults.LockPassportEditor);
    }

    private void ApplyPersonLinkedDefaults(BaseObject linkedObject, ApplicationItem appItem, DetailView childDetailView)
    {
        if (!ApplicationItemPersonLinkedDefaults.TryApply(linkedObject, appItem, childDetailView.ObjectSpace, Application))
            return;

        LockPropertyWhenReady(
            childDetailView,
            nameof(Person),
            ApplicationItemPersonLinkedDefaults.LockPersonEditor);
    }

    private static void LockPropertyWhenReady(DetailView detailView, string propertyName, Action<DetailView> lockEditor)
    {
        if (detailView.FindItem(propertyName) != null)
        {
            lockEditor(detailView);
            return;
        }

        void OnControlsCreated(object? sender, EventArgs e)
        {
            if (sender is not DetailView view)
                return;

            view.ControlsCreated -= OnControlsCreated;
            lockEditor(view);
        }

        detailView.ControlsCreated += OnControlsCreated;
    }
}
