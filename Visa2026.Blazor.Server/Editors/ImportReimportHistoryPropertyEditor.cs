using System;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor.Components.Models;
using DevExpress.ExpressApp.Blazor.Editors;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Model;
using Microsoft.Extensions.DependencyInjection;
using Visa2026.Module.Editors;
using Visa2026.Module.Services.ImportHistory;

namespace Visa2026.Blazor.Server.Editors;

[PropertyEditor(typeof(string), ImportReimportHistoryEditorAliases.History, false)]
public class ImportReimportHistoryPropertyEditor : BlazorPropertyEditorBase, IComplexViewItem
{
    private XafApplication? _application;

    public ImportReimportHistoryPropertyEditor(Type objectType, IModelMemberViewItem model)
        : base(objectType, model)
    {
    }

    public override ImportReimportHistoryModel ComponentModel => (ImportReimportHistoryModel)base.ComponentModel;

    void IComplexViewItem.Setup(IObjectSpace objectSpace, XafApplication application) =>
        _application = application;

    protected override IComponentModel CreateComponentModel()
    {
        var model = new ImportReimportHistoryModel();
        if (_application?.ServiceProvider != null)
            model.Reader = _application.ServiceProvider.GetService<IImportReimportHistoryReader>();
        return model;
    }
}
