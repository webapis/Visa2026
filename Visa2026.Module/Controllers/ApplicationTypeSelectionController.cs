using System;
using System.Diagnostics;
using System.Linq;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.SystemModule;
using DevExpress.Persistent.Base;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Localization;
using AppBO = Visa2026.Module.BusinessObjects.Application;
using ApplicationTypeBO = Visa2026.Module.BusinessObjects.ApplicationType;
namespace Visa2026.Module.Controllers;
/// <summary>
/// Resolves <see cref="AppBO.ApplicationTypeQuickCode"/> to <see cref="AppBO.ApplicationType"/>
/// syncs quick code with <see cref="AppBO.ApplicationType"/>, and wires the Blazor inline code picker.
/// </summary>
public class ApplicationTypeSelectionController : ObjectViewController<DetailView, AppBO>
{
    private const string LogPrefix = "[AppTypeQuickCode]";
    private bool _syncing;
    private AppBO _wiredApplication;
    private ILogger<ApplicationTypeSelectionController>? _logger;
    private PropertyEditor _quickCodeEditor;
    private PropertyEditor _applicationTypeDisplayEditor;
    protected override void OnActivated()
    {
        base.OnActivated();
        _logger = Application.ServiceProvider?.GetService<ILogger<ApplicationTypeSelectionController>>();
        ObjectSpace.ObjectChanged += ObjectSpace_ObjectChanged;
        View.CurrentObjectChanged += View_CurrentObjectChanged;
        LogConnectionInfo();
        Log("OnActivated", $"ViewId={View?.Id}, ObjectSpace={ObjectSpace?.GetType().Name}");
        WireCurrentApplication();
    }
    protected override void OnViewControlsCreated()
    {
        base.OnViewControlsCreated();
        SubscribeEditors();
    }
    protected override void OnDeactivated()
    {
        Log("OnDeactivated", null);
        UnwireCurrentApplication();
        View.CurrentObjectChanged -= View_CurrentObjectChanged;
        UnsubscribeEditors();
        ObjectSpace.ObjectChanged -= ObjectSpace_ObjectChanged;
        base.OnDeactivated();
    }
    private void View_CurrentObjectChanged(object sender, EventArgs e)
    {
        Log("View.CurrentObjectChanged", DescribeApplication(ViewCurrentApplication()));
        WireCurrentApplication();
    }
    private void WireCurrentApplication()
    {
        UnwireCurrentApplication();
        var app = ViewCurrentApplication();
        if (app == null)
        {
            Log("WireCurrentApplication", "no current Application");
            return;
        }
        _wiredApplication = app;
        app.ApplicationTypeQuickCodeChanged = OnApplicationTypeQuickCodeChanged;
        Log("WireCurrentApplication", $"{DescribeApplication(app)}, callback attached");
        SyncQuickCodeFromType(app);
    }
    private void UnwireCurrentApplication()
    {
        if (_wiredApplication != null)
        {
            _wiredApplication.ApplicationTypeQuickCodeChanged = null;
            Log("UnwireCurrentApplication", DescribeApplication(_wiredApplication));
            _wiredApplication = null;
        }
    }
    private void SubscribeEditors()
    {
        UnsubscribeEditors();
        _quickCodeEditor = View.FindItem(nameof(AppBO.ApplicationTypeQuickCode)) as PropertyEditor;
        if (_quickCodeEditor != null)
            _quickCodeEditor.ControlValueChanged += QuickCodeEditor_ControlValueChanged;
        _applicationTypeDisplayEditor = View.FindItem(nameof(AppBO.ApplicationType)) as PropertyEditor;
        Log("SubscribeEditors",
            $"quickCodeEditor={EditorState(_quickCodeEditor)}, applicationTypeDisplay={EditorState(_applicationTypeDisplayEditor)}");
    }
    private void UnsubscribeEditors()
    {
        if (_quickCodeEditor != null)
        {
            _quickCodeEditor.ControlValueChanged -= QuickCodeEditor_ControlValueChanged;
            _quickCodeEditor = null;
        }
        _applicationTypeDisplayEditor = null;
    }
    private void OnApplicationTypeQuickCodeChanged(string? raw)
    {
        Log("Setter callback", $"raw='{FormatRaw(raw)}', syncing={_syncing}");
        if (_syncing)
            return;
        ProcessQuickCodeChange(ViewCurrentApplication(), raw, "setter");
    }
    private void QuickCodeEditor_ControlValueChanged(object sender, EventArgs e)
    {
        if (_syncing)
            return;

        var app = ViewCurrentApplication();
        if (app == null)
            return;

        var raw = _quickCodeEditor?.PropertyValue as string ?? app.ApplicationTypeQuickCode;
        Log("ControlValueChanged (quick code)",
            $"editorValue={FormatRaw(raw)}, app.QuickCode={FormatRaw(app.ApplicationTypeQuickCode)}");

        if (IsEmptyQuickCode(raw) && IsEmptyQuickCode(app.ApplicationTypeQuickCode))
        {
            Log("ControlValueChanged (quick code)", "ignored: spurious empty bind/focus");
            return;
        }

        if (!string.Equals(app.ApplicationTypeQuickCode, raw, StringComparison.Ordinal))
        {
            Log("ControlValueChanged (quick code)", "syncing BO via property setter");
            app.ApplicationTypeQuickCode = raw;
        }
    }
    private void ProcessQuickCodeChange(AppBO? app, string? raw, string source)
    {
        Log("ProcessQuickCodeChange",
            $"source={source}, syncing={_syncing}, raw='{FormatRaw(raw)}', app={DescribeApplication(app)}");
        if (_syncing)
        {
            Log("ProcessQuickCodeChange", "skipped: syncing");
            return;
        }
        if (app == null || !ReferenceEquals(app, ViewCurrentApplication()))
        {
            Log("ProcessQuickCodeChange", "skipped: app is null or not current view object");
            return;
        }

        if (IsEmptyQuickCode(raw))
        {
            Log("ProcessQuickCodeChange", "empty quick code -> clear ApplicationType");
            ClearApplicationTypeSelection(app);
            RefreshTypeEditors();
            return;
        }
        if (!TryParseSelectionCode(raw, out var code))
        {
            // User is editing or clearing the code — drop a stale ApplicationType so they can re-enter.
            if (app.ApplicationType != null)
            {
                Log("ProcessQuickCodeChange",
                    $"incomplete quick code (raw='{FormatRaw(raw)}') → clear ApplicationType");
                ClearApplicationTypeSelection(app);
                RefreshTypeEditors();
            }
            else
            {
                Log("ProcessQuickCodeChange", $"not exactly 3 digits yet (raw='{FormatRaw(raw)}')");
            }

            return;
        }
        var typesWithCodes = ObjectSpace.GetObjectsQuery<ApplicationTypeBO>()
            .Count(t => t.SelectionCode != null && t.SelectionCode != "");
        var match = FindApplicationTypeByCode(code);
        Log("Lookup",
            $"code='{code}', typesWithSelectionCode={typesWithCodes}, match={DescribeApplicationType(match)}");
        if (match != null)
        {
            ApplyApplicationTypeMatch(app, match);
            RefreshTypeEditors();
            Log("ProcessQuickCodeChange", $"applied match {DescribeApplicationType(match)}");
            return;
        }
        Log("ProcessQuickCodeChange", $"no match for '{code}' -> clear type, show message");
        ClearApplicationTypeSelection(app);
        SetQuickCodeWithoutResolve(app, code);
        ShowCodeNotFoundMessage(code, typesWithCodes == 0);
        RefreshTypeEditors();
    }
    private AppBO? ViewCurrentApplication() => View.CurrentObject as AppBO;
    private void ObjectSpace_ObjectChanged(object sender, ObjectChangedEventArgs e)
    {
        if (_syncing || e.Object is not AppBO app || !ReferenceEquals(app, ViewCurrentApplication()))
            return;
        if (e.PropertyName == nameof(AppBO.ApplicationType))
        {
            Log("ObjectSpace.ObjectChanged", $"ApplicationType -> {DescribeApplicationType(app.ApplicationType)}");
            SyncQuickCodeFromType(app);
        }
    }
    private ApplicationTypeBO? FindApplicationTypeByCode(string code) =>
        ObjectSpace.FirstOrDefault<ApplicationTypeBO>(t => t.SelectionCode == code);
    private void ApplyApplicationTypeMatch(AppBO app, ApplicationTypeBO match)
    {
        _syncing = true;
        try
        {
            app.ApplicationType = ObjectSpace.GetObject(match);
            SetQuickCodeWithoutResolve(app, match.SelectionCode);
            ObjectSpace.SetModified(app);
            Log("ApplyApplicationTypeMatch",
                $"app={DescribeApplication(app)}, type={DescribeApplicationType(app.ApplicationType)}");
        }
        finally
        {
            _syncing = false;
        }
    }
    private void ClearApplicationTypeSelection(AppBO app)
    {
        _syncing = true;
        try
        {
            app.ApplicationType = null;
            ObjectSpace.SetModified(app);
            Log("ClearApplicationTypeSelection", DescribeApplication(app));
        }
        finally
        {
            _syncing = false;
        }
    }
    private void SetQuickCodeWithoutResolve(AppBO app, string? code)
    {
        _syncing = true;
        try
        {
            app.ApplicationTypeQuickCode = code;
            Log("SetQuickCodeWithoutResolve", $"code='{FormatRaw(code)}'");
        }
        finally
        {
            _syncing = false;
        }
    }
    private void RefreshTypeEditors()
    {
        Log("RefreshTypeEditors",
            $"quickCodeEditor={EditorState(_quickCodeEditor)}, applicationTypeDisplay={EditorState(_applicationTypeDisplayEditor)}");
        RefreshQuickCodeEditor();
        _applicationTypeDisplayEditor?.ReadValue();
        var app = ViewCurrentApplication();
        Log("RefreshTypeEditors", $"after ReadValue: app={DescribeApplication(app)}");
    }
    private void RefreshQuickCodeEditor() => _quickCodeEditor?.ReadValue();
    private void ShowCodeNotFoundMessage(string code, bool noTypesHaveSelectionCodes)
    {
        var message = noTypesHaveSelectionCodes
            ? VisaUiMessages.Get("ApplicationTypeQuickCode.NoCodesInDatabase")
            : VisaUiMessages.Format("ApplicationTypeQuickCode.NotFound", code);
        Log("ShowCodeNotFoundMessage", $"code='{code}', noTypesHaveSelectionCodes={noTypesHaveSelectionCodes}");
        Application.ShowViewStrategy.ShowMessage(
            message,
            InformationType.Error,
            5000,
            InformationPosition.Top);
    }
    private void SyncQuickCodeFromType(AppBO app)
    {
        if (app == null)
            return;
        var code = app.ApplicationType?.SelectionCode;
        if (string.Equals(app.ApplicationTypeQuickCode, code, StringComparison.Ordinal))
        {
            Log("SyncQuickCodeFromType", "already in sync");
            return;
        }
        Log("SyncQuickCodeFromType",
            $"type={DescribeApplicationType(app.ApplicationType)}, quickCode '{FormatRaw(app.ApplicationTypeQuickCode)}' â†’ '{FormatRaw(code)}'");
        SetQuickCodeWithoutResolve(app, code);
    }
    private static bool IsEmptyQuickCode(string? raw) =>
        string.IsNullOrWhiteSpace(raw) || raw.Trim().All(c => !char.IsDigit(c));
    /// <summary>Exactly three digits as entered â€” no left-padding.</summary>
    private static bool TryParseSelectionCode(string? raw, out string code)
    {
        code = string.Empty;
        if (string.IsNullOrWhiteSpace(raw))
            return false;
        var digits = new string(raw.Trim().Where(char.IsDigit).ToArray());
        if (digits.Length != 3)
            return false;
        code = digits;
        return true;
    }
    private void LogConnectionInfo()
    {
        try
        {
            var config = Application.ServiceProvider?.GetService<IConfiguration>();
            var connectionString = config?.GetConnectionString("DefaultConnection");
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                Log("Connection", "DefaultConnection is not set (check launch profile / appsettings)");
                return;
            }

            var builder = new SqlConnectionStringBuilder(connectionString);
            Log("Connection",
                $"DataSource={builder.DataSource}, Database={builder.InitialCatalog}, IntegratedSecurity={builder.IntegratedSecurity} " +
                $"(expect LocalDB profile: (localdb)\\mssqllocaldb / Visa2026)");
        }
        catch (Exception ex)
        {
            Log("Connection", $"could not read connection string: {ex.Message}");
        }
    }

    private void Log(string step, string? detail)
    {
        var line = detail == null
            ? $"{LogPrefix} {step}"
            : $"{LogPrefix} {step}: {detail}";
        Console.WriteLine(line);
        Debug.WriteLine(line);
        _logger?.LogDebug("{LogLine}", line);
    }
    private static string FormatRaw(string? raw) =>
        raw == null ? "<null>" : $"'{raw}' (len={raw.Length})";
    private static string DescribeApplication(AppBO? app)
    {
        if (app == null)
            return "<null>";
        var type = app.ApplicationType;
        return $"AppId={app.ID}, QuickCode={FormatRaw(app.ApplicationTypeQuickCode)}, Type={DescribeApplicationType(type)}";
    }
    private static string DescribeApplicationType(ApplicationTypeBO? type) =>
        type == null
            ? "<null>"
            : $"Id={type.ID}, Name={type.Name}, SelectionCode={FormatRaw(type.SelectionCode)}";
    private static string EditorState(PropertyEditor? editor)
    {
        if (editor == null)
            return "missing";

        var value = editor.PropertyValue;
        return value == null
            ? "ok, PropertyValue=<null>"
            : $"ok, PropertyValue={value} ({value.GetType().Name})";
    }
}
