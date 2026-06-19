using System;
using System.Threading.Tasks;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor.Components.Models;
using DevExpress.ExpressApp.Blazor.Editors;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Base;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Visa2026.Blazor.Server.Services;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Editors;
using Visa2026.Module.Localization;
using Visa2026.Module.Services.UserReports;

namespace Visa2026.Blazor.Server.Editors;

[PropertyEditor(typeof(string), UserReportTemplateExcelEditorAliases.SpreadsheetPanel, false)]
public sealed class UserReportTemplateExcelSpreadsheetPropertyEditor : BlazorPropertyEditorBase, IComplexViewItem
{
    private XafApplication _application;

    public UserReportTemplateExcelSpreadsheetPropertyEditor(Type objectType, IModelMemberViewItem model)
        : base(objectType, model)
    {
    }

    public override UserReportTemplateExcelSpreadsheetModel ComponentModel =>
        (UserReportTemplateExcelSpreadsheetModel)base.ComponentModel;

    void IComplexViewItem.Setup(IObjectSpace objectSpace, XafApplication application) =>
        _application = application;

    protected override IComponentModel CreateComponentModel()
    {
        var model = new UserReportTemplateExcelSpreadsheetModel();
        model.ShowMessageRequested = EventCallback.Factory.Create<string>(this, OnShowMessageAsync);
        model.DirtyStateChanged = EventCallback.Factory.Create<bool>(this, OnDirtyStateChangedAsync);
        return model;
    }

    protected override void OnCurrentObjectChanged()
    {
        base.OnCurrentObjectChanged();
        ReadValueCore();
    }

    protected override void ReadValueCore()
    {
        base.ReadValueCore();
        if (ComponentModel == null)
            return;

        if (CurrentObject is not UserReportTemplate template)
        {
            ComponentModel.TemplateId = Guid.Empty;
            ComponentModel.IsExcelTemplate = false;
            ComponentModel.SpreadsheetUrl = string.Empty;
            ComponentModel.CanEdit = false;
            return;
        }

        var templateId = template.ID;
        var isExcel = template.GetEffectiveOutputFormat() == TemplateOutputFormat.Excel;
        ComponentModel.TemplateId = templateId;
        ComponentModel.IsExcelTemplate = isExcel;
        ComponentModel.SpreadsheetUrl = isExcel && templateId != Guid.Empty
            ? $"/user-report-template-spreadsheet/{templateId:D}?embed=true"
            : string.Empty;
        ComponentModel.CanEdit = UserReportTemplateEditAccess.CanEditTemplates();
        ComponentModel.SaveButtonText = VisaUiMessages.Get("UserReport.ExcelSpreadsheet.SaveToTemplate");
        ComponentModel.ReloadButtonText = VisaUiMessages.Get("UserReport.ExcelSpreadsheet.ReloadFromDatabase");
        ComponentModel.StatusSavedText = VisaUiMessages.Get("UserReport.ExcelSpreadsheet.StatusSaved");
        ComponentModel.StatusUnsavedText = VisaUiMessages.Get("UserReport.ExcelSpreadsheet.StatusUnsaved");
        ComponentModel.ReadOnlyText = VisaUiMessages.Get("UserReport.ExcelSpreadsheet.ReadOnly");
        ComponentModel.ReloadConfirmMessage = VisaUiMessages.Get("UserReport.ExcelSpreadsheet.ReloadConfirm");
    }

    private Task OnShowMessageAsync(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return Task.CompletedTask;

        var isError = message.Contains("error", StringComparison.OrdinalIgnoreCase)
            || message.Contains(VisaUiMessages.Get("UserReport.ExcelSpreadsheet.SaveFailed"), StringComparison.OrdinalIgnoreCase);

        _application?.ShowViewStrategy.ShowMessage(
            message,
            isError ? InformationType.Warning : InformationType.Success,
            5000);
        return Task.CompletedTask;
    }

    private Task OnDirtyStateChangedAsync(bool isDirty)
    {
        if (ComponentModel?.TemplateId == Guid.Empty)
            return Task.CompletedTask;

        var tracker = _application?.ServiceProvider?.GetService<UserReportTemplateSpreadsheetDirtyTracker>();
        tracker?.SetDirty(ComponentModel.TemplateId, isDirty);
        return Task.CompletedTask;
    }
}
