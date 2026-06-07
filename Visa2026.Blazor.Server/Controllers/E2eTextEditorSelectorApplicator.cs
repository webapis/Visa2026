using DevExpress.ExpressApp.Blazor.Editors;

namespace Visa2026.Blazor.Server.Controllers;

internal static class E2eTextEditorSelectorApplicator
{
    public static void Apply(BlazorPropertyEditorBase editor, string testId) =>
        E2ePropertySelectorApplicator.Apply(editor, testId);
}
