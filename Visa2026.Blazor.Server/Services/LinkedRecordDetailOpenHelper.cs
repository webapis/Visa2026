using System;
using System.Reflection;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Editors;
using DevExpress.Persistent.BaseImpl.EF;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services;

namespace Visa2026.Blazor.Server.Services;

public static class LinkedRecordDetailOpenHelper
{
    public static void ShowEditor(XafApplication? application, ApplicationProfileInstancePersonLinkKind kind, Guid objectId)
    {
        var type = PersonLinkReview.ObjectType(kind);
        if (application == null || type == null || objectId == Guid.Empty)
            return;

        var objectSpace = application.CreateObjectSpace(type);
        var target = objectSpace.GetObjectByKey(type, objectId);
        if (target == null)
        {
            objectSpace.Dispose();
            return;
        }

        var detailView = application.CreateDetailView(objectSpace, target, isRoot: true);
        detailView.ViewEditMode = ViewEditMode.Edit;
        application.ShowViewStrategy.ShowView(
            new ShowViewParameters(detailView) { TargetWindow = TargetWindow.NewWindow },
            new ShowViewSource(application.MainWindow, null));
    }

    public static void ShowNewEditor(
        XafApplication? application,
        ApplicationProfileInstancePersonLinkKind kind,
        Guid personId,
        Action<Guid>? onSaved)
    {
        var type = PersonLinkReview.ObjectType(kind);
        if (application == null || type == null || personId == Guid.Empty)
            return;

        var objectSpace = application.CreateObjectSpace(type);
        var created = objectSpace.CreateObject(type);
        var personProperty = created.GetType().GetProperty("Person", BindingFlags.Instance | BindingFlags.Public);
        if (personProperty?.PropertyType == typeof(Person))
        {
            var person = objectSpace.GetObjectByKey<Person>(personId);
            if (person != null)
                personProperty.SetValue(created, person);
        }

        objectSpace.Committed += (_, _) =>
        {
            if (created is not BaseObject bo || bo.ID == Guid.Empty || objectSpace.IsDeletedObject(created))
                return;

            onSaved?.Invoke(bo.ID);
        };

        var detailView = application.CreateDetailView(objectSpace, created, isRoot: true);
        detailView.ViewEditMode = ViewEditMode.Edit;
        try
        {
            application.ShowViewStrategy.ShowView(
                new ShowViewParameters(detailView) { TargetWindow = TargetWindow.NewWindow },
                new ShowViewSource(application.MainWindow, null));
        }
        catch
        {
            objectSpace.Dispose();
            throw;
        }
    }
}