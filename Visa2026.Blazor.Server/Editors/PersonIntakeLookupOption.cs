using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Editors;
using DevExpress.Persistent.BaseImpl.EF;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.PersonIntake;

namespace Visa2026.Blazor.Server.Editors;

public sealed class PersonIntakeLookupOption
{
    public Guid Oid { get; init; }
    public string Text { get; init; } = string.Empty;
}

public static class PersonIntakeLookupHelper
{
    public static IReadOnlyList<PersonIntakeLookupOption> LoadOptions<T>(IObjectSpace? objectSpace)
        where T : BaseObject =>
        LoadOptions(objectSpace, typeof(T));

    public static IReadOnlyList<PersonIntakeLookupOption> LoadOptions(IObjectSpace? objectSpace, Type? type)
    {
        if (objectSpace == null || type == null || !typeof(BaseObject).IsAssignableFrom(type))
            return Array.Empty<PersonIntakeLookupOption>();

        return objectSpace.GetObjects(type)
            .OfType<BaseObject>()
            .Select(item => new PersonIntakeLookupOption
            {
                Oid = item.ID,
                Text = Caption(item)
            })
            .OrderBy(o => o.Text, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    public static string Caption(BaseObject? item)
    {
        if (item == null)
            return string.Empty;

        if (item is LookupBase lookup)
        {
            var display = lookup.LocalizedDisplayName;
            if (string.IsNullOrWhiteSpace(display))
                display = lookup.NameTm;
            if (!string.IsNullOrWhiteSpace(display))
                return item is Country && !string.IsNullOrWhiteSpace(lookup.Code)
                    ? PersonIntakeLookupSearch.WithCode(lookup.Code, display)
                    : display;
        }

        var text = item.ToString();
        return string.IsNullOrWhiteSpace(text) ? item.ID.ToString("N")[..8] : text;
    }

    public static bool ShowNew(XafApplication? application, Type? type, Action<Guid>? onSaved, string? initialName = null)
    {
        if (application == null || type == null)
            return false;

        var objectSpace = application.CreateObjectSpace(type);
        var created = objectSpace.CreateObject(type);
        ApplyInitialName(created, initialName);
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
            Show(application, detailView);
            return true;
        }
        catch
        {
            objectSpace.Dispose();
            throw;
        }
    }

    public static bool ShowEdit(XafApplication? application, Type? type, Guid id, Action? onSaved)
    {
        if (application == null || type == null || id == Guid.Empty)
            return false;

        var objectSpace = application.CreateObjectSpace(type);
        var target = objectSpace.GetObjectByKey(type, id);
        if (target == null)
        {
            objectSpace.Dispose();
            return false;
        }

        objectSpace.Committed += (_, _) => onSaved?.Invoke();

        var detailView = application.CreateDetailView(objectSpace, target, isRoot: true);
        detailView.ViewEditMode = ViewEditMode.Edit;
        try
        {
            Show(application, detailView);
            return true;
        }
        catch
        {
            objectSpace.Dispose();
            throw;
        }
    }

    private static void ApplyInitialName(object? created, string? initialName)
    {
        var name = initialName?.Trim();
        if (string.IsNullOrEmpty(name) || created == null)
            return;

        if (created is LookupBase lookup)
        {
            lookup.NameTm = name;
            return;
        }

        var property = created.GetType().GetProperty("Name");
        if (property is { CanWrite: true } && property.PropertyType == typeof(string)
            && property.GetCustomAttributes(typeof(ObsoleteAttribute), true).Length == 0)
            property.SetValue(created, name);
    }

    private static void Show(XafApplication application, DetailView detailView)
    {
        var parameters = new ShowViewParameters(detailView)
        {
            TargetWindow = TargetWindow.NewWindow
        };
        var source = application.MainWindow != null
            ? new ShowViewSource(application.MainWindow, null)
            : new ShowViewSource(null, null);
        application.ShowViewStrategy.ShowView(parameters, source);
    }
}