using DevExpress.ExpressApp;
using DevExpress.ExpressApp.CloneObject;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Localization;

namespace Visa2026.Module.Controllers;

/// <summary>
/// Post-clone defaults for <see cref="ApplicationProfile"/> (duplicate locked profiles to edit configuration).
/// </summary>
public sealed class ApplicationProfileCloneController : ViewController
{
    private const int CodeMaxLength = 64;

    private CloneObjectViewController? _cloneObjectController;

    public ApplicationProfileCloneController()
    {
        TargetObjectType = typeof(ApplicationProfile);
    }

    protected override void OnActivated()
    {
        base.OnActivated();

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
        if (e.ClonedObject is not ApplicationProfile clone
            || e.SourceObject is not ApplicationProfile source)
        {
            return;
        }

        var suffix = VisaUiMessages.Get("UserReport.CloneNameSuffix");
        clone.Name = UserReportTemplateCloneController.BuildCloneName(source.Name, suffix, 200);
        clone.Code = BuildCloneCode(source.Code, suffix);
        clone.SelectionCode = null;
        clone.IsActive = true;
    }

    internal static string BuildCloneCode(string sourceCode, string suffix)
    {
        var trimmed = string.IsNullOrWhiteSpace(sourceCode) ? "profile" : sourceCode.Trim();
        var combined = $"{trimmed}{suffix}".Replace(' ', '-').ToLowerInvariant();
        return combined.Length <= CodeMaxLength ? combined : combined[..CodeMaxLength];
    }
}
