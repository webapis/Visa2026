using System;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.CloneObject;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Localization;
using Visa2026.Module.Services.UserReports;

namespace Visa2026.Module.Controllers;

/// <summary>
/// Post-clone defaults for <see cref="UserReportTemplate"/> and gates the built-in CloneObject action
/// to officers who can edit templates.
/// </summary>
public class UserReportTemplateCloneController : ViewController
{
    private CloneObjectViewController? _cloneObjectController;

    public UserReportTemplateCloneController()
    {
        TargetObjectType = typeof(UserReportTemplate);
    }

    protected override void OnActivated()
    {
        base.OnActivated();

        _cloneObjectController = Frame.GetController<CloneObjectViewController>();
        if (_cloneObjectController == null)
            return;

        _cloneObjectController.CustomShowClonedObject += OnCustomShowClonedObject;
        UpdateCloneActionState();
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

    private void UpdateCloneActionState()
    {
        if (_cloneObjectController == null)
            return;

        var canEdit = UserReportTemplateEditAccess.CanEditTemplates();
        _cloneObjectController.Active["UserReportTemplateEdit"] = canEdit;
    }

    private void OnCustomShowClonedObject(object sender, CustomShowClonedObjectEventArgs e)
    {
        if (e.ClonedObject is not UserReportTemplate clone
            || e.SourceObject is not UserReportTemplate source)
        {
            return;
        }

        clone.TemplateName = BuildCloneName(source.TemplateName, VisaUiMessages.Get("UserReport.CloneNameSuffix"));
        clone.IsActive = true;
    }

    internal static string BuildCloneName(string sourceName, string suffix, int maxLength = 255)
    {
        var trimmedSuffix = suffix ?? string.Empty;
        if (string.IsNullOrWhiteSpace(sourceName))
            return trimmedSuffix.TrimStart(' ', '-');

        var baseName = sourceName.TrimEnd();
        var combined = $"{baseName}{trimmedSuffix}";

        if (combined.Length <= maxLength)
            return combined;

        var keepLength = maxLength - trimmedSuffix.Length;
        if (keepLength <= 0)
            return trimmedSuffix[..Math.Min(trimmedSuffix.Length, maxLength)];

        return baseName[..keepLength] + trimmedSuffix;
    }
}