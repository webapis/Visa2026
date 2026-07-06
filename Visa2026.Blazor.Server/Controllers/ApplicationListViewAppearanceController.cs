using DevExpress.ExpressApp;
using DevExpress.ExpressApp.ConditionalAppearance;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Blazor.Server.Controllers;

public sealed class ApplicationListViewAppearanceController : ViewController<ListView>
{
    private const string DisableKey = "ApplicationListViewCssRows";

    public ApplicationListViewAppearanceController()
    {
        TargetObjectType = typeof(Application);
    }

    protected override void OnActivated()
    {
        base.OnActivated();
        if (Frame.GetController<AppearanceController>() is { } appearanceController)
            appearanceController.Active[DisableKey] = false;
    }

    protected override void OnDeactivated()
    {
        if (Frame.GetController<AppearanceController>() is { } appearanceController)
            appearanceController.Active.RemoveItem(DisableKey);
        base.OnDeactivated();
    }
}