using System.Collections;
using System.Reflection;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.Blazor.Components.Models;
using DevExpress.ExpressApp.Blazor.SystemModule;
using DevExpress.ExpressApp.Blazor.Templates.Ribbon.ActionControls;
using DevExpress.ExpressApp.Blazor.Templates.Toolbar.ActionControls;

namespace Visa2026.Blazor.Server.Controllers;

/// <summary>
/// Applies stable selectors to XAF Blazor action controls (Ribbon and Toolbar).
/// Visa2026 uses <c>FormStyle="Ribbon"</c>; view toolbars render split items in open shadow roots.
/// </summary>
internal static class E2eActionControlSelectorSupport
{
    internal static void TryRaiseCustomizeControl(ActionBase? action)
    {
        if (action == null)
        {
            return;
        }

        MethodInfo? method = action.GetType().GetMethod(
            "RaiseCustomizeControl",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            types: Type.EmptyTypes,
            modifiers: null);
        method?.Invoke(action, null);
    }

    internal static bool TryApplyFromActionControl(object control, string testId, ActionBase? action = null)
    {
        object? itemModel = control switch
        {
            DxToolbarItemSimpleActionControl simple => simple.ToolbarItemModel,
            DxToolbarItemSingleChoiceActionControl toolbarChoice => toolbarChoice.ToolbarItemModel,
            DxRibbonItemSimpleActionControl ribbonSimple => ribbonSimple.RibbonItemModel,
            DxRibbonItemSingleChoiceActionControl ribbonChoice => ribbonChoice.RibbonItemModel,
            _ => GetItemModelByReflection(control),
        };

        if (itemModel == null)
        {
            return false;
        }

        ApplyCssClassAndTestId(itemModel, testId);
        ApplyModelCustomCssClass(itemModel, action);
        return true;
    }

    private static void ApplyModelCustomCssClass(object itemModel, ActionBase? action)
    {
        if (action?.Model is not IModelActionBlazor actionModel)
        {
            return;
        }

        string? modelClass = actionModel.CustomCSSClassName;
        if (string.IsNullOrWhiteSpace(modelClass))
        {
            return;
        }

        switch (itemModel)
        {
            case DxToolbarItemModel toolbar:
                toolbar.CssClass = AppendCssClass(toolbar.CssClass, modelClass);
                break;
            case DxRibbonItemModel ribbon:
                ribbon.CssClass = AppendCssClass(ribbon.CssClass, modelClass);
                break;
        }
    }

    internal static void ApplyCssClassAndTestId(object itemModel, string testId)
    {
        string e2eClass = $"e2e-{testId}";
        switch (itemModel)
        {
            case DxToolbarItemModel toolbar:
                toolbar.CssClass = AppendCssClass(toolbar.CssClass, e2eClass);
                toolbar.SetAttribute("data-testid", testId);
                SetHtmlAttributes(toolbar, testId, e2eClass);
                break;
            case DxRibbonItemModel ribbon:
                ribbon.CssClass = AppendCssClass(ribbon.CssClass, e2eClass);
                ribbon.SetAttribute("data-testid", testId);
                SetHtmlAttributes(ribbon, testId, e2eClass);
                break;
            default:
                SetHtmlAttributes(itemModel, testId, e2eClass);
                break;
        }
    }

    private static void SetHtmlAttributes(object itemModel, string testId, string e2eClass)
    {
        PropertyInfo? attributesProperty = itemModel.GetType().GetProperty(
            "Attributes",
            BindingFlags.Instance | BindingFlags.Public);
        if (attributesProperty == null)
        {
            return;
        }

        if (attributesProperty.GetValue(itemModel) is not IDictionary<string, object> attributes)
        {
            attributes = new Dictionary<string, object>();
            attributesProperty.SetValue(itemModel, attributes);
        }

        attributes["data-testid"] = testId;
        attributes["class"] = AppendCssClass(attributes.TryGetValue("class", out object? cls) ? cls?.ToString() : null, e2eClass);
    }

    private static string AppendCssClass(string? existing, string e2eClass)
    {
        if (string.IsNullOrWhiteSpace(existing))
        {
            return e2eClass;
        }

        return existing.Contains(e2eClass, StringComparison.Ordinal)
            ? existing
            : existing + " " + e2eClass;
    }

    private static object? GetItemModelByReflection(object control)
    {
        foreach (PropertyInfo property in control.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            if (property.Name.EndsWith("ItemModel", StringComparison.Ordinal)
                && property.GetValue(control) is { } model)
            {
                return model;
            }
        }

        return null;
    }
}
