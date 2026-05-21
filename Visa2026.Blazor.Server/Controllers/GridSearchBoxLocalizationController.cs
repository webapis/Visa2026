using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor.Editors;
using Visa2026.Blazor.Server.Localization;

namespace Visa2026.Blazor.Server.Controllers;

/// <summary>
/// Localizes the DevExpress Blazor grid inline search placeholder ("Text to search..."),
/// which is not driven by XAF Application Model action captions.
/// </summary>
public sealed class GridSearchBoxLocalizationController : ViewController<ListView>
{
    protected override void OnViewControlsCreated()
    {
        base.OnViewControlsCreated();
        if (View.Editor is DxGridListEditor { GridModel: { } gridModel })
        {
            gridModel.SearchBoxNullText = VisaLocalization.GetGridSearchBoxNullText();
        }
    }
}
