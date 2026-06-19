using System.ComponentModel;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor;
using DevExpress.Persistent.Base;
using Microsoft.Extensions.DependencyInjection;
using Visa2026.Blazor.Server.Services;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Localization;

namespace Visa2026.Blazor.Server.Controllers;

/// <summary>Blocks leaving UserReportTemplate DetailView while the Spreadsheet tab has unsaved edits.</summary>
public sealed class UserReportTemplateSpreadsheetCloseGuardController : ViewController<DetailView>
{
    private BlazorWindow? _window;
    private UserReportTemplateSpreadsheetDirtyTracker? _dirtyTracker;

    public UserReportTemplateSpreadsheetCloseGuardController()
    {
        TargetObjectType = typeof(UserReportTemplate);
        TargetViewType = ViewType.DetailView;
    }

    protected override void OnActivated()
    {
        base.OnActivated();
        _dirtyTracker = TryGetService<UserReportTemplateSpreadsheetDirtyTracker>();

        if (Frame is BlazorWindow blazorWindow)
        {
            _window = blazorWindow;
            _window.Closing += Window_Closing;
        }
    }

    protected override void OnDeactivated()
    {
        if (_window != null)
            _window.Closing -= Window_Closing;

        if (View?.CurrentObject is UserReportTemplate template)
            _dirtyTracker?.SetDirty(template.ID, false);

        _dirtyTracker = null;
        _window = null;
        base.OnDeactivated();
    }

    private void Window_Closing(object? sender, CancelEventArgs e)
    {
        var template = View?.CurrentObject as UserReportTemplate;
        if (template == null || template.GetEffectiveOutputFormat() != TemplateOutputFormat.Excel)
            return;

        if (_dirtyTracker == null || !_dirtyTracker.IsDirty(template.ID))
            return;

        e.Cancel = true;
        Application.ShowViewStrategy.ShowMessage(
            VisaUiMessages.Get("UserReport.ExcelSpreadsheet.UnsavedCloseWarning"),
            InformationType.Warning,
            6000);
    }

    private T? TryGetService<T>() where T : class
    {
        try
        {
            return Application?.ServiceProvider?.GetService<T>();
        }
        catch (ObjectDisposedException)
        {
            return null;
        }
    }
}
