using System;
using System.Runtime.CompilerServices;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.ConditionalAppearance;
using DevExpress.ExpressApp.DC;
using Visa2026.Module.Appearance;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Controllers;

/// <summary>
/// Applies optional-field visibility for types marked with <see cref="SupportsOptionalDetailFieldsAttribute"/>.
/// </summary>
public sealed class OptionalDetailFieldsController : ViewController<DetailView>
{
    private static readonly ConditionalWeakTable<DetailView, OptionalDetailFieldsController> ActiveControllers = new();

    private bool _initializingOptionalFields;

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
        _initializingOptionalFields = true;
        ResetOptionalFieldsCollapsed();
        ObjectSpace.ObjectChanged += ObjectSpace_ObjectChanged;
        View.CurrentObjectChanged += View_CurrentObjectChanged;
    }

    protected override void OnViewControlsCreated()
    {
        base.OnViewControlsCreated();
        ApplyCollapsedOptionalFieldsVisibility();
        _initializingOptionalFields = false;
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

    private void View_CurrentObjectChanged(object sender, EventArgs e)
    {
        _initializingOptionalFields = true;
        ResetOptionalFieldsCollapsed();
        ApplyCollapsedOptionalFieldsVisibility();
        _initializingOptionalFields = false;
    }

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

        if (_initializingOptionalFields
            || !OptionalDetailFieldsSupport.IsOptionalMember(View.ObjectTypeInfo, e.PropertyName))
        {
            return;
        }

        IMemberInfo member = View.ObjectTypeInfo.FindMember(e.PropertyName);
        if (member == null)
        {
            return;
        }

        if (View.CurrentObject is not IOptionalDetailFields optionalFields)
        {
            return;
        }

        if (!optionalFields.ShowOptionalFields
            && OptionalDetailFieldsMetadata.HasMeaningfulOptionalValue(e.NewValue, member.MemberType)
            && !OptionalDetailFieldsMetadata.HasMeaningfulOptionalValue(e.OldValue, member.MemberType))
        {
            optionalFields.ShowOptionalFields = true;
            RefreshAfterToggle();
        }
    }

    private void ResetOptionalFieldsCollapsed()
    {
        if (View.CurrentObject is IOptionalDetailFields optionalFields)
        {
            optionalFields.ShowOptionalFields = false;
        }
    }

    private void ApplyCollapsedOptionalFieldsVisibility() =>
        ApplyOptionalFieldsVisibility();
}
