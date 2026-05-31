using System;
using System.Runtime.CompilerServices;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.ConditionalAppearance;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Controllers;

/// <summary>
/// Applies optional-field visibility for types marked with <see cref="SupportsOptionalDetailFieldsAttribute"/>.
/// </summary>
public sealed class OptionalDetailFieldsController : ViewController<DetailView>
{
    private static readonly ConditionalWeakTable<DetailView, OptionalDetailFieldsController> ActiveControllers = new();

    public static void NotifyShowOptionalFieldsChanged(DetailView detailView)
    {
        if (detailView == null)
        {
            return;
        }

        if (ActiveControllers.TryGetValue(detailView, out OptionalDetailFieldsController controller))
        {
            controller.RefreshAfterToggle();
            return;
        }

        OptionalDetailFieldsVisibilityApplicator.Apply(detailView);
    }

    protected override void OnActivated()
    {
        base.OnActivated();

        if (!OptionalDetailFieldsSupport.Supports(View.ObjectTypeInfo.Type))
        {
            Active["UnsupportedType"] = false;
            return;
        }

        ActiveControllers.Remove(View);
        ActiveControllers.Add(View, this);
        TryEnsureOptionalFieldsVisible(allowAutoExpandOnNewObject: false);
        ObjectSpace.ObjectChanged += ObjectSpace_ObjectChanged;
        View.CurrentObjectChanged += View_CurrentObjectChanged;
    }

    protected override void OnViewControlsCreated()
    {
        base.OnViewControlsCreated();
        TryEnsureOptionalFieldsVisible(allowAutoExpandOnNewObject: false);
    }

    protected override void OnDeactivated()
    {
        ActiveControllers.Remove(View);
        ObjectSpace.ObjectChanged -= ObjectSpace_ObjectChanged;
        View.CurrentObjectChanged -= View_CurrentObjectChanged;
        base.OnDeactivated();
    }

    internal void ApplyOptionalFieldsVisibility() =>
        OptionalDetailFieldsVisibilityApplicator.Apply(View);

    private void RefreshAfterToggle()
    {
        ApplyOptionalFieldsVisibility();
        Frame.GetController<AppearanceController>()?.Refresh();
    }

    private void View_CurrentObjectChanged(object sender, EventArgs e) =>
        TryEnsureOptionalFieldsVisible(allowAutoExpandOnNewObject: false);

    private void ObjectSpace_ObjectChanged(object sender, ObjectChangedEventArgs e)
    {
        if (!ReferenceEquals(e.Object, View.CurrentObject))
        {
            return;
        }

        if (string.Equals(e.PropertyName, nameof(IOptionalDetailFields.ShowOptionalFields), StringComparison.Ordinal))
        {
            RefreshAfterToggle();
            return;
        }

        if (OptionalDetailFieldsSupport.IsOptionalMember(View.ObjectTypeInfo, e.PropertyName))
        {
            TryEnsureOptionalFieldsVisible(allowAutoExpandOnNewObject: true);
        }
    }

    private void TryEnsureOptionalFieldsVisible(bool allowAutoExpandOnNewObject = false)
    {
        if (View.CurrentObject is not IOptionalDetailFields optionalFields)
        {
            return;
        }

        if (!optionalFields.ShowOptionalFields
            && OptionalDetailFieldsSupport.HasPopulatedOptionalFields(View.CurrentObject, ObjectSpace)
            && (allowAutoExpandOnNewObject || !ObjectSpace.IsNewObject(View.CurrentObject)))
        {
            optionalFields.ShowOptionalFields = true;
        }

        if (optionalFields.ShowOptionalFields)
        {
            RefreshAfterToggle();
        }
        else
        {
            ApplyOptionalFieldsVisibility();
        }
    }
}
