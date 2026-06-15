using DevExpress.ExpressApp;
using DevExpress.ExpressApp.SystemModule;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Localization;

namespace Visa2026.Module.Controllers;

/// <summary>Seeded <see cref="ExpirationAlertRule"/> rows — officers edit thresholds only.</summary>
public sealed class ExpirationAlertRuleDeleteController : ViewController
{
    public ExpirationAlertRuleDeleteController()
    {
        TargetObjectType = typeof(ExpirationAlertRule);
    }

    protected override void OnActivated()
    {
        base.OnActivated();
        if (Frame.GetController<DeleteObjectsViewController>() is { } deleteController)
            deleteController.DeleteAction.Active.SetItemValue(nameof(ExpirationAlertRuleDeleteController), false);
    }

    protected override void OnDeactivated()
    {
        if (Frame.GetController<DeleteObjectsViewController>() is { } deleteController)
            deleteController.DeleteAction.Active.RemoveItem(nameof(ExpirationAlertRuleDeleteController));
        base.OnDeactivated();
    }
}

public sealed class ExpirationAlertRuleNewController : ViewController
{
    public ExpirationAlertRuleNewController()
    {
        TargetObjectType = typeof(ExpirationAlertRule);
    }

    protected override void OnActivated()
    {
        base.OnActivated();
        if (Frame.GetController<NewObjectViewController>() is { } newController)
            newController.NewObjectAction.Active.SetItemValue(nameof(ExpirationAlertRuleNewController), false);
    }

    protected override void OnDeactivated()
    {
        if (Frame.GetController<NewObjectViewController>() is { } newController)
            newController.NewObjectAction.Active.RemoveItem(nameof(ExpirationAlertRuleNewController));
        base.OnDeactivated();
    }
}

public sealed class ExpirationAlertRuleSaveController : ObjectViewController<DetailView, ExpirationAlertRule>
{
    protected override void OnActivated()
    {
        base.OnActivated();
        ObjectSpace.ObjectSaving += ObjectSpaceOnObjectSaving;
    }

    protected override void OnDeactivated()
    {
        ObjectSpace.ObjectSaving -= ObjectSpaceOnObjectSaving;
        base.OnDeactivated();
    }

    private void ObjectSpaceOnObjectSaving(object sender, ObjectManipulatingEventArgs e)
    {
        if (e.Object is not ExpirationAlertRule rule)
            return;

        if (!DocumentExpirationAlertConfigurationKeys.All.Contains(rule.BusinessObjectKey))
            throw new UserFriendlyException(VisaUiMessages.Get("ExpirationAlertRule.ConfigurationOnly"));

        if (!DocumentExpirationAlertConfigurationKeys.SupportsExtensionApplicationRequiredDays(rule.BusinessObjectKey))
            rule.ExtensionApplicationRequiredDays = null;
    }
}
