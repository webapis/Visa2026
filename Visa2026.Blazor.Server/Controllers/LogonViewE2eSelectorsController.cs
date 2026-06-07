using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.Blazor.Components.Models;
using DevExpress.ExpressApp.Blazor.Editors;
using DevExpress.ExpressApp.Blazor.Templates.Toolbar.ActionControls;
using DevExpress.ExpressApp.Editors;

namespace Visa2026.Blazor.Server.Controllers;

/// <summary>
/// Stable selectors on the login page (user name, password, Log In button).
/// Registered via <see cref="Visa2026BlazorApplication.CreateLogonWindowControllers"/> — logon
/// frames do not pick up all module ViewControllers automatically.
/// </summary>
public sealed class LogonViewE2eSelectorsController : ViewController<DetailView>
{
    internal const string LogonBlazorDetailViewId = "AuthenticationStandardLogonParameters_Blazor_DetailView";
    internal const string LogonDetailViewId = "AuthenticationStandardLogonParameters_DetailView";

    public LogonViewE2eSelectorsController()
    {
        TargetViewType = ViewType.DetailView;
    }

    protected override void OnActivated()
    {
        base.OnActivated();
        if (!IsLogonView(View?.Id))
        {
            return;
        }

        View.CustomizeViewItemControl<StringPropertyEditor>(this, ApplyLogonFieldSelectors);
        HookLogonAction();
    }

    protected override void OnViewControlsCreated()
    {
        base.OnViewControlsCreated();
        if (!IsLogonView(View?.Id))
        {
            return;
        }

        ApplyMemberSelector("UserName", "login-user-name");
        ApplyMemberSelector("Password", "login-password");
    }

    protected override void OnDeactivated()
    {
        UnhookLogonAction();
        base.OnDeactivated();
    }

    private static bool IsLogonView(string? viewId) =>
        viewId == LogonBlazorDetailViewId || viewId == LogonDetailViewId;

    private void ApplyMemberSelector(string memberName, string testId)
    {
        if (View.FindItem(memberName) is BlazorPropertyEditorBase editor)
        {
            E2eTextEditorSelectorApplicator.Apply(editor, testId);
        }
    }

    private void HookLogonAction()
    {
        foreach (Controller controller in Frame.Controllers)
        {
            foreach (ActionBase action in controller.Actions)
            {
                if (action.Id != "Logon")
                {
                    continue;
                }

                action.CustomizeControl += LogonAction_CustomizeControl;
            }
        }
    }

    private void UnhookLogonAction()
    {
        foreach (Controller controller in Frame.Controllers)
        {
            foreach (ActionBase action in controller.Actions)
            {
                if (action.Id != "Logon")
                {
                    continue;
                }

                action.CustomizeControl -= LogonAction_CustomizeControl;
            }
        }
    }

    private static void ApplyLogonFieldSelectors(StringPropertyEditor editor)
    {
        string? testId = editor.PropertyName switch
        {
            "UserName" => "login-user-name",
            "Password" => "login-password",
            _ => null
        };

        if (testId == null)
        {
            return;
        }

        E2eTextEditorSelectorApplicator.Apply(editor, testId);
    }

    private static void LogonAction_CustomizeControl(object? sender, CustomizeControlEventArgs e)
    {
        if (e.Control is DxToolbarItemSimpleActionControl simple)
        {
            ApplyLoginSubmitSelector(simple.ToolbarItemModel);
        }
    }

    private static void ApplyLoginSubmitSelector(DxToolbarItemModel toolbarItem)
    {
        toolbarItem.CssClass = "e2e-login-submit";
        toolbarItem.SetAttribute("data-testid", "login-submit");
    }
}
