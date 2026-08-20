using System;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Editors;
using Visa2026.Blazor.Server.Editors;
using ApplicationBO = Visa2026.Module.BusinessObjects.ApplicationProfileInstance;

namespace Visa2026.Blazor.Server.Controllers;

/// <summary>
/// Keeps a single comma-separated editor on ApplicationProfileInstance detail for
/// BorderZoneLocation and MovementPermitLocation. Hides duplicate editors left after
/// FK-to-string migrations.
/// </summary>
public sealed class ApplicationDetailViewBorderZoneController : ViewController<DetailView>
{
    private static readonly string[] MultiSelectPropertyNames =
    [
        nameof(ApplicationBO.BorderZoneLocation),
        nameof(ApplicationBO.MovementPermitLocation),
    ];

    public ApplicationDetailViewBorderZoneController()
    {
        TargetObjectType = typeof(ApplicationBO);
        TargetViewId = "Application_DetailView";
    }

    protected override void OnViewControlsCreated()
    {
        base.OnViewControlsCreated();
        foreach (var propertyName in MultiSelectPropertyNames)
            HideDuplicateEditors(propertyName);
    }

    private void HideDuplicateEditors(string propertyName)
    {
        var editors = View.GetItems<PropertyEditor>()
            .Where(editor => IsPropertyEditor(editor, propertyName))
            .ToList();

        if (editors.Count <= 1)
            return;

        var keep = editors.OfType<CommaSeparatedMultiSelectPropertyEditor>().FirstOrDefault()
            ?? editors[0];

        foreach (var editor in editors)
        {
            if (!ReferenceEquals(editor, keep))
                HideViewItem(editor);
        }
    }

    private static bool IsPropertyEditor(PropertyEditor editor, string propertyName) =>
        string.Equals(editor.Id, propertyName, StringComparison.Ordinal)
        || string.Equals(editor.PropertyName, propertyName, StringComparison.Ordinal)
        || string.Equals(editor.MemberInfo?.Name, propertyName, StringComparison.Ordinal);

    private static void HideViewItem(ViewItem item)
    {
        if (item is IAppearanceVisibility visibility)
        {
            visibility.Visibility = ViewItemVisibility.Hide;
        }
    }
}