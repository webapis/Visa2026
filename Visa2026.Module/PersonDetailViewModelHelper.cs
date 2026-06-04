using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Model;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module;

/// <summary>Resolves Person detail view ids only when they exist in the merged Application Model.</summary>
public static class PersonDetailViewModelHelper
{
    public static string ResolveDetailViewId(XafApplication application, string? listViewId, Person person)
    {
        if (!string.IsNullOrEmpty(listViewId)
            && application.Model.Views[listViewId] is IModelListView listViewModel
            && listViewModel.DetailView != null
            && CanUseDetailView(application, listViewModel.DetailView.Id))
        {
            return listViewModel.DetailView.Id;
        }

        var resolved = PersonDetailViewResolver.Resolve(listViewId, person);
        if (CanUseDetailView(application, resolved))
            return resolved;

        return PersonDetailViewIds.Default;
    }

    public static bool CanUseDetailView(XafApplication application, string detailViewId) =>
        TryGetModelDetailView(application, detailViewId, out _);

    public static bool TryGetModelDetailView(
        XafApplication application,
        string detailViewId,
        out IModelDetailView? modelDetailView)
    {
        modelDetailView = null;
        if (string.IsNullOrEmpty(detailViewId)
            || application.Model.Views[detailViewId] is not IModelDetailView candidate)
        {
            return false;
        }

        if (detailViewId == PersonDetailViewIds.Default)
        {
            if (candidate.ModelClass?.TypeInfo?.Type == typeof(Person))
            {
                modelDetailView = candidate;
                return true;
            }

            return false;
        }

        if (candidate.ModelClass?.TypeInfo?.Type == typeof(Person))
        {
            modelDetailView = candidate;
            return true;
        }

        // Layout-only typed views from Blazor Model.xafml may lack ModelClass until configurator runs.
        if (detailViewId is PersonDetailViewIds.Employee or PersonDetailViewIds.FamilyMember)
        {
            modelDetailView = candidate;
            return true;
        }

        return false;
    }

    public static bool TryCreateDetailView(
        XafApplication application,
        IObjectSpace sourceObjectSpace,
        Person person,
        string detailViewId,
        out DetailView? detailView)
    {
        detailView = null;
        if (!TryGetModelDetailView(application, detailViewId, out IModelDetailView? modelDetailView)
            || modelDetailView == null)
        {
            return false;
        }

        try
        {
            object key = sourceObjectSpace.GetKeyValue(person);
            IObjectSpace detailObjectSpace = application.CreateObjectSpace(typeof(Person));
            object? personInDetailSpace = detailObjectSpace.GetObjectByKey<Person>(key);
            if (personInDetailSpace == null)
                personInDetailSpace = detailObjectSpace.GetObject(person);

            if (personInDetailSpace == null)
                return false;

            detailView = application.CreateDetailView(
                detailObjectSpace,
                modelDetailView,
                true,
                personInDetailSpace);
            return detailView != null;
        }
        catch
        {
            return false;
        }
    }
}
