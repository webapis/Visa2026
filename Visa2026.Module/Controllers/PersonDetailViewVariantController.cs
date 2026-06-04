using System;
using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Controllers;

/// <summary>
/// Non-Blazor hosts: swaps <see cref="PersonDetailViewIds.Default"/> to the typed detail view for the list context.
/// Blazor uses route-based list navigation in the host project; captions come from <c>Model.xafml</c>.
/// </summary>
public sealed class PersonDetailViewVariantController : ObjectViewController<DetailView, Person>
{
    private bool _redirecting;

    protected override void OnActivated()
    {
        base.OnActivated();
        if (IsBlazorApplication())
            return;

        View.CurrentObjectChanged += OnCurrentObjectChanged;
        TrySwapToTypedDetailView();
    }

    protected override void OnDeactivated()
    {
        if (!IsBlazorApplication())
            View.CurrentObjectChanged -= OnCurrentObjectChanged;

        PersonDetailViewNavigationContext.SourceListViewIdValue = null;
        base.OnDeactivated();
    }

    private void OnCurrentObjectChanged(object? sender, EventArgs e) => TrySwapToTypedDetailView();

    private void TrySwapToTypedDetailView()
    {
        if (_redirecting || View.CurrentObject is not Person person)
            return;

        string targetViewId = PersonDetailViewModelHelper.ResolveDetailViewId(
            Application,
            PersonDetailViewNavigationContext.SourceListViewIdValue,
            person);

        if (targetViewId == PersonDetailViewIds.Default || string.Equals(View.Id, targetViewId, StringComparison.Ordinal))
            return;

        if (!PersonDetailViewModelHelper.TryCreateDetailView(
                Application,
                View.ObjectSpace,
                person,
                targetViewId,
                out DetailView? typedView)
            || typedView == null)
        {
            return;
        }

        try
        {
            _redirecting = true;
            Frame.SetView(typedView);
        }
        finally
        {
            _redirecting = false;
        }
    }

    private bool IsBlazorApplication() =>
        Application.GetType().FullName?.Contains("Blazor", StringComparison.Ordinal) == true;
}
