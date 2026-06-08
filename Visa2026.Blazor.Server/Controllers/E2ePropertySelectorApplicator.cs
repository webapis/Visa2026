using System.Reflection;
using DevExpress.ExpressApp.Blazor.Components.Models;
using DevExpress.ExpressApp.Blazor.Editors;
using Visa2026.Blazor.Server.Editors;

namespace Visa2026.Blazor.Server.Controllers;

internal static class E2ePropertySelectorApplicator
{
    public static void Apply(BlazorPropertyEditorBase editor, string testId)
    {
        IComponentModel? model = editor.ComponentModel;
        if (model == null)
        {
            return;
        }

        string cssClass = $"e2e-{testId}";

        switch (model)
        {
            case DxTextBoxModel textBox:
                textBox.InputId = testId;
                textBox.CssClass = cssClass;
                textBox.SetAttribute("data-testid", testId);
                return;
            case DxMaskedInputModel<string> maskedInput:
                TrySetInputId(maskedInput, testId);
                maskedInput.CssClass = cssClass;
                maskedInput.SetAttribute("data-testid", testId);
                return;
            case DxMemoModel memo:
                TrySetInputId(memo, testId);
                memo.CssClass = cssClass;
                memo.SetAttribute("data-testid", testId);
                return;
            case VisaFamilyMembersTextModel visaFamily:
                visaFamily.E2eTestId = testId;
                visaFamily.E2eCssClass = cssClass;
                return;
        }

        ApplyGeneric(model, testId, cssClass);
    }

    private static void ApplyGeneric(IComponentModel model, string testId, string cssClass)
    {
        if (model is ComponentModelBase componentModel)
        {
            componentModel.SetAttribute("data-testid", testId);
        }

        TrySetInputId(model, testId);
        TryAppendCssClass(model, cssClass);
    }

    private static void TrySetInputId(object model, string testId)
    {
        PropertyInfo? property = model.GetType().GetProperty("InputId", BindingFlags.Instance | BindingFlags.Public);
        if (property?.CanWrite == true && property.PropertyType == typeof(string))
        {
            property.SetValue(model, testId);
        }
    }

    private static void TryAppendCssClass(object model, string cssClass)
    {
        PropertyInfo? property = model.GetType().GetProperty("CssClass", BindingFlags.Instance | BindingFlags.Public);
        if (property?.CanWrite != true || property.PropertyType != typeof(string))
        {
            return;
        }

        string? current = property.GetValue(model) as string;
        property.SetValue(model, string.IsNullOrWhiteSpace(current) ? cssClass : $"{current} {cssClass}");
    }
}
