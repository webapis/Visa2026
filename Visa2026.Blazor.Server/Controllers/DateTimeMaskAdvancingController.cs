using DevExpress.Blazor;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor.Editors;

namespace Visa2026.Blazor.Server.Controllers;

/// <summary>
/// Global keyboard UX: after a date mask section is complete, advance to the next
/// (same behavior as VisaFamilyMembersTextComponent birth date).
/// </summary>
public sealed class DateTimeMaskAdvancingController : ViewController<DetailView>
{
    protected override void OnActivated()
    {
        base.OnActivated();
        View.CustomizeViewItemControl<DateTimePropertyEditor>(this, static editor =>
        {
            ApplyAdvancing(editor.DxDateEditMaskProperties.DateTime);
            ApplyAdvancing(editor.DxDateEditMaskProperties.DateOnly);
        });
    }

    private static void ApplyAdvancing(DevExpress.ExpressApp.Blazor.Components.Models.DxDateTimeMaskPropertiesModel mask)
    {
        mask.CaretMode = MaskCaretMode.Advancing;
        mask.UpdateNextSectionOnCycleChange = true;
    }

    private static void ApplyAdvancing(DevExpress.ExpressApp.Blazor.Components.Models.DxDateOnlyMaskPropertiesModel mask)
    {
        mask.CaretMode = MaskCaretMode.Advancing;
        mask.UpdateNextSectionOnCycleChange = true;
    }
}