using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.Blazor;
using DevExpress.ExpressApp.Templates;
using DevExpress.Persistent.Base;

namespace Visa2026.Blazor.Server.Controllers;

/// <summary>
/// Toolbar action to close all TabbedMDI tabs in one click.
/// Uses the same <see cref="BlazorMdiShowViewStrategy"/> API as the built-in tab
/// context menu (<c>TabbedMdiContextMenuController</c>).
/// </summary>
public sealed class CloseTabsToolbarController : WindowController
{
    public const string CloseAllActionId = "VisaCloseAllTabs";

    private readonly SimpleAction closeAllAction;

    public CloseTabsToolbarController()
    {
        TargetWindowType = WindowType.Main;

        closeAllAction = new SimpleAction(this, CloseAllActionId, PredefinedCategory.View)
        {
            Caption = "Close all tabs",
            ToolTip = "Close all open tabs",
            ImageName = "Action_CloseAllTabs",
            PaintStyle = ActionItemPaintStyle.CaptionAndImage,
            SelectionDependencyType = SelectionDependencyType.Independent,
        };
        closeAllAction.Execute += CloseAllAction_Execute;
    }

    private async void CloseAllAction_Execute(object sender, SimpleActionExecuteEventArgs e)
    {
        if (Application.ShowViewStrategy is BlazorMdiShowViewStrategy strategy)
        {
            await strategy.CloseAllWindows().ConfigureAwait(true);
        }
    }
}