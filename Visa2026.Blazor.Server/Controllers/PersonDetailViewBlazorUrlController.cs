using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor;
using Microsoft.AspNetCore.Components;
using Visa2026.Module;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Blazor.Server.Controllers;

/// <summary>
/// Aligns the browser path with the Person detail view that should be shown for the current record/list context.
/// </summary>
public sealed class PersonDetailViewBlazorUrlController : ObjectViewController<DetailView, Person>
{
    protected override void OnActivated()
    {
        base.OnActivated();
        SyncUrlWithExpectedDetailView();
    }

    private void SyncUrlWithExpectedDetailView()
    {
        if (View.CurrentObject is not Person person)
            return;

        if (Application is not BlazorApplication blazorApplication)
            return;

        NavigationManager? navigationManager = blazorApplication.ServiceProvider?.GetService<NavigationManager>();
        if (navigationManager == null)
            return;

        string expectedViewId = PersonDetailViewModelHelper.ResolveDetailViewId(
            Application,
            PersonDetailViewNavigationContext.SourceListViewIdValue,
            person);

        if (expectedViewId == PersonDetailViewIds.Default)
            return;

        string key = ObjectSpace.GetKeyValueAsString(person);
        if (string.IsNullOrEmpty(key))
            return;

        string expectedPath = "/" + expectedViewId + "/" + key;
        string currentPath = navigationManager.ToAbsoluteUri(navigationManager.Uri).AbsolutePath;
        if (string.Equals(currentPath, expectedPath, StringComparison.OrdinalIgnoreCase))
            return;

        navigationManager.NavigateTo(expectedPath, replace: true);
    }
}
