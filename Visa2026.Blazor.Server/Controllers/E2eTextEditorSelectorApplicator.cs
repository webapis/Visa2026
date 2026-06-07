using DevExpress.ExpressApp.Blazor.Components.Models;
using DevExpress.ExpressApp.Blazor.Editors;

namespace Visa2026.Blazor.Server.Controllers;

internal static class E2eTextEditorSelectorApplicator
{
    public static void Apply(BlazorPropertyEditorBase editor, string testId)
    {
        switch (editor.ComponentModel)
        {
            case DxTextBoxModel textBox:
                textBox.InputId = testId;
                textBox.CssClass = $"e2e-{testId}";
                textBox.SetAttribute("data-testid", testId);
                break;
            case DxMaskedInputModel<string> maskedInput:
                maskedInput.CssClass = $"e2e-{testId}";
                maskedInput.SetAttribute("data-testid", testId);
                break;
            case DxMemoModel memo:
                memo.CssClass = $"e2e-{testId}";
                memo.SetAttribute("data-testid", testId);
                break;
        }
    }
}
