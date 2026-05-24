using System;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor.Components.Models;
using DevExpress.ExpressApp.Blazor.Editors;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Base;
using Microsoft.AspNetCore.Components;
using Visa2026.Module.BusinessObjects.StateNotifications;
using Visa2026.Module.Editors;
using Visa2026.Module.Localization;
using Visa2026.Module.Services.StateNotifications;

namespace Visa2026.Blazor.Server.Editors;

[PropertyEditor(typeof(string), BoStateNotificationInboxEditorAliases.Inbox, false)]
public class BoStateNotificationInboxPropertyEditor : BlazorPropertyEditorBase, IComplexViewItem
{
    private XafApplication _application;

    public BoStateNotificationInboxPropertyEditor(Type objectType, IModelMemberViewItem model)
        : base(objectType, model)
    {
    }

    public override BoStateNotificationInboxModel ComponentModel => (BoStateNotificationInboxModel)base.ComponentModel;

    void IComplexViewItem.Setup(IObjectSpace objectSpace, XafApplication application) =>
        _application = application;

    protected override IComponentModel CreateComponentModel()
    {
        var model = new BoStateNotificationInboxModel();
        model.Items = BoStateNotificationPrototypeData.CreateSampleNotifications().ToList();
        model.OpenRecordRequested = EventCallback.Factory.Create<BoStateNotificationItem>(this, OnOpenRecordAsync);
        model.ShowMessageRequested = EventCallback.Factory.Create<string>(this, OnShowMessageAsync);
        model.StateChanged = EventCallback.Factory.Create(this, OnStateChangedAsync);
        return model;
    }

    private Task OnStateChangedAsync() => Task.CompletedTask;

    private Task OnShowMessageAsync(string message)
    {
        _application?.ShowViewStrategy.ShowMessage(message, InformationType.Info, 4000);
        return Task.CompletedTask;
    }

    private Task OnOpenRecordAsync(BoStateNotificationItem item)
    {
        if (item == null)
            return Task.CompletedTask;

        var focus = item.Category == BoStateNotificationCategory.DataCompleteness
            ? VisaUiMessages.Format(
                "StateNotification.Message.OpenPrototypeMissing",
                BoStateNotificationDisplayLocalization.MissingItemLabel(item))
            : BoStateNotificationDisplayLocalization.StateLabel(item);
        var text = VisaUiMessages.Format(
            "StateNotification.Message.OpenPrototype",
            item.TargetBoTypeName,
            item.PersonName,
            focus);
        _application?.ShowViewStrategy.ShowMessage(text, InformationType.Info, 6000);
        return Task.CompletedTask;
    }
}
