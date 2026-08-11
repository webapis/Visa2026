using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor.Templates;
using Visa2026.Module.BusinessObjects.OfficerShell;

namespace Visa2026.Blazor.Server.Controllers;

/// <summary>
/// Hides the TabbedMDI document tab strip while the officer shell DetailView is active (B6 immersive chrome).
/// </summary>
public sealed class OfficerShellImmersiveTabBarController : WindowController
{
    private ITabbedMdiMainFormTemplate tabbedTemplate;

    public OfficerShellImmersiveTabBarController()
    {
        TargetWindowType = WindowType.Main;
    }

    protected override void OnActivated()
    {
        base.OnActivated();
        Window.TemplateChanged += OnTemplateChanged;
        Frame.ViewChanged += OnViewChanged;
        HookTemplate();
        ApplyTabBarVisibility();
    }

    protected override void OnDeactivated()
    {
        Window.TemplateChanged -= OnTemplateChanged;
        Frame.ViewChanged -= OnViewChanged;
        SetTabBarVisible(true);
        tabbedTemplate = null!;
        base.OnDeactivated();
    }

    private void OnTemplateChanged(object sender, EventArgs e)
    {
        HookTemplate();
        ApplyTabBarVisibility();
    }

    private void OnViewChanged(object sender, ViewChangedEventArgs e) =>
        ApplyTabBarVisibility();

    private void HookTemplate()
    {
        if (Window.Template is ITabbedMdiMainFormTemplate template)
            tabbedTemplate = template;
    }

    private void ApplyTabBarVisibility()
    {
        bool hide = Frame.View is DetailView detailView
            && string.Equals(detailView.Id, OfficerShellViewIds.DetailView, StringComparison.Ordinal);
        SetTabBarVisible(!hide);
    }

    private void SetTabBarVisible(bool visible)
    {
        if (tabbedTemplate == null)
            return;

        const string hideClass = "visa-officer-shell-hide-mdi-tabs";
        var cssClass = tabbedTemplate.TabsModel.CssClass ?? string.Empty;
        if (visible)
            tabbedTemplate.TabsModel.CssClass = cssClass.Replace(hideClass, string.Empty).Trim();
        else if (!cssClass.Contains(hideClass, StringComparison.Ordinal))
            tabbedTemplate.TabsModel.CssClass = string.IsNullOrEmpty(cssClass) ? hideClass : $"{cssClass} {hideClass}";
    }
}
