using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor.Editors;
using Microsoft.AspNetCore.Components;
using Visa2026.Blazor.Server.Localization;

namespace Visa2026.Blazor.Server.Controllers;

/// <summary>
/// Localizes DevExpress Blazor grid chrome (search placeholder, empty-state text)
/// not driven by XAF Application Model captions.
/// </summary>
public sealed class GridSearchBoxLocalizationController : ViewController<ListView>
{
    protected override void OnActivated()
    {
        base.OnActivated();
        ApplyGridLocalization();
    }

    protected override void OnViewControlsCreated()
    {
        base.OnViewControlsCreated();
        ApplyGridLocalization();
    }

    private void ApplyGridLocalization()
    {
        if (View.Editor is not DxGridListEditor { GridModel: { } gridModel })
        {
            return;
        }

        gridModel.SearchBoxNullText = VisaLocalization.GetGridSearchBoxNullText();
        string emptyText = VisaLocalization.GetGridEmptyDataText();
        gridModel.EmptyDataAreaTemplate = _ => builder =>
        {
            builder.OpenElement(0, "span");
            builder.AddContent(1, emptyText);
            builder.CloseElement();
        };
    }
}
