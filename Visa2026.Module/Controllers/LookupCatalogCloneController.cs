using DevExpress.ExpressApp;
using DevExpress.ExpressApp.CloneObject;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Localization;

namespace Visa2026.Module.Controllers;

/// <summary>
/// Post-clone defaults for lookup catalog types that officers clone to create variants
/// (<see cref="ApplicationType"/>, <see cref="VisaPeriod"/>).
/// </summary>
public class LookupCatalogCloneController : ViewController
{
    private const int LookupNameMaxLength = 200;
    private CloneObjectViewController? _cloneObjectController;

    protected override void OnActivated()
    {
        base.OnActivated();

        var objectType = View?.ObjectTypeInfo?.Type;
        if (objectType != typeof(ApplicationType) && objectType != typeof(VisaPeriod))
        {
            Active["UnsupportedType"] = false;
            return;
        }

        _cloneObjectController = Frame.GetController<CloneObjectViewController>();
        if (_cloneObjectController == null)
            return;

        _cloneObjectController.CustomShowClonedObject += OnCustomShowClonedObject;
    }

    protected override void OnDeactivated()
    {
        if (_cloneObjectController != null)
        {
            _cloneObjectController.CustomShowClonedObject -= OnCustomShowClonedObject;
            _cloneObjectController = null;
        }

        base.OnDeactivated();
    }

    private void OnCustomShowClonedObject(object sender, CustomShowClonedObjectEventArgs e)
    {
        var suffix = VisaUiMessages.Get("UserReport.CloneNameSuffix");

        if (e.ClonedObject is ApplicationType applicationTypeClone
            && e.SourceObject is ApplicationType applicationTypeSource)
        {
            ApplyLookupNameClone(applicationTypeClone, applicationTypeSource, suffix);
            applicationTypeClone.SelectionCode = string.Empty;
            applicationTypeClone.LocalizationKey = string.Empty;
            applicationTypeClone.Code = string.Empty;
            applicationTypeClone.IsDefault = false;
            return;
        }

        if (e.ClonedObject is VisaPeriod visaPeriodClone
            && e.SourceObject is VisaPeriod visaPeriodSource)
        {
            ApplyLookupNameClone(visaPeriodClone, visaPeriodSource, suffix);
            visaPeriodClone.LocalizationKey = string.Empty;
            visaPeriodClone.Code = string.Empty;
            visaPeriodClone.IsDefault = false;
        }
    }

    private static void ApplyLookupNameClone(LookupBase clone, LookupBase source, string suffix)
    {
        if (!string.IsNullOrWhiteSpace(source.NameTm))
            clone.NameTm = UserReportTemplateCloneController.BuildCloneName(source.NameTm, suffix, LookupNameMaxLength);

        if (!string.IsNullOrWhiteSpace(source.Name))
            clone.Name = UserReportTemplateCloneController.BuildCloneName(source.Name, suffix, LookupNameMaxLength);
    }
}