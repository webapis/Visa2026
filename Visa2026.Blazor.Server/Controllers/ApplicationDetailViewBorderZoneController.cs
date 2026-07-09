using System;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Editors;
using Visa2026.Blazor.Server.Editors;
using ApplicationBO = Visa2026.Module.BusinessObjects.Application;

namespace Visa2026.Blazor.Server.Controllers;

/// <summary>
/// Keeps a single Application.BorderZoneLocation editor on Application detail.
/// Hides duplicate editors left after the FK-to-string migration that otherwise
/// appear below the Application Items list.
/// </summary>
public sealed class ApplicationDetailViewBorderZoneController : ViewController<DetailView>
{
    private const string BorderZonePropertyName = nameof(ApplicationBO.BorderZoneLocation);

    public ApplicationDetailViewBorderZoneController()
    {
        TargetObjectType = typeof(ApplicationBO);
        TargetViewId = "Application_DetailView";
    }

    protected override void OnViewControlsCreated()
    {
        base.OnViewControlsCreated();
        HideDuplicateBorderZoneEditors();
    }

    private void HideDuplicateBorderZoneEditors()
    {
        var borderZoneEditors = View.GetItems<PropertyEditor>()
            .Where(IsBorderZonePropertyEditor)
            .ToList();

        if (borderZoneEditors.Count <= 1)
        {
            return;
        }

        // Prefer the multi-select editor that is already visible in the main form column.
        var keep = borderZoneEditors.OfType<CommaSeparatedMultiSelectPropertyEditor>().FirstOrDefault()
            ?? borderZoneEditors[0];

        foreach (var editor in borderZoneEditors)
        {
            if (!ReferenceEquals(editor, keep))
            {
                HideViewItem(editor);
            }
        }
    }

    private static bool IsBorderZonePropertyEditor(PropertyEditor editor) =>
        string.Equals(editor.Id, BorderZonePropertyName, StringComparison.Ordinal)
        || string.Equals(editor.PropertyName, BorderZonePropertyName, StringComparison.Ordinal)
        || string.Equals(editor.MemberInfo?.Name, BorderZonePropertyName, StringComparison.Ordinal);

    private static void HideViewItem(ViewItem item)
    {
        if (item is IAppearanceVisibility visibility)
        {
            visibility.Visibility = ViewItemVisibility.Hide;
        }
    }
}