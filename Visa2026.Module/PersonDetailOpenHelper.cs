using System;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Editors;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module;

/// <summary>
/// Opens a Person DetailView from a key without disposing the view's ObjectSpace.
/// </summary>
public static class PersonDetailOpenHelper
{
    public static bool TryShowDetailView(
        XafApplication application,
        Frame? sourceFrame,
        Guid personId,
        string? sourceListViewId = null,
        ViewEditMode editMode = ViewEditMode.View)
    {
        if (application == null || personId == Guid.Empty)
            return false;

        using var lookupObjectSpace = application.CreateObjectSpace(typeof(Person));
        var person = lookupObjectSpace.GetObjectByKey<Person>(personId);
        if (person == null)
            return false;

        var detailViewId = PersonDetailViewModelHelper.ResolveDetailViewId(
            application,
            sourceListViewId,
            person);

        DetailView? detailView = null;
        if (PersonDetailViewModelHelper.TryCreateDetailView(
                application,
                lookupObjectSpace,
                person,
                detailViewId,
                out detailView)
            && detailView != null)
        {
            detailView.ViewEditMode = editMode;
            Show(application, sourceFrame, detailView);
            return true;
        }

        var detailObjectSpace = application.CreateObjectSpace(typeof(Person));
        var personInDetailSpace = detailObjectSpace.GetObjectByKey<Person>(personId);
        if (personInDetailSpace == null)
        {
            detailObjectSpace.Dispose();
            return false;
        }

        detailView = application.CreateDetailView(detailObjectSpace, personInDetailSpace);
        detailView.ViewEditMode = editMode;
        Show(application, sourceFrame, detailView);
        return true;
    }

    private static void Show(XafApplication application, Frame? sourceFrame, DetailView detailView)
    {
        var frame = sourceFrame ?? application.MainWindow;
        application.ShowViewStrategy.ShowView(
            new ShowViewParameters(detailView) { TargetWindow = TargetWindow.Current },
            new ShowViewSource(frame, null));
    }
}
