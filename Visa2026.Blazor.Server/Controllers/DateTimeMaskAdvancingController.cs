using System.Reflection;
using DevExpress.Blazor;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor.Editors;

namespace Visa2026.Blazor.Server.Controllers;

/// <summary>
/// Global keyboard UX: after a date mask section is complete, advance to the next
/// (same behavior as VisaFamilyMembersTextComponent birth date).
/// Also disables mouse-wheel value changes when the editor model exposes AllowMouseWheel.
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
            DisableMouseWheel(editor);
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

    private static void DisableMouseWheel(DateTimePropertyEditor editor)
    {
        // 25.2+ may expose AllowMouseWheel on the component model; ignore if absent.
        try
        {
            var model = editor.ComponentModel;
            if (model is null)
            {
                return;
            }

            var prop = model.GetType().GetProperty("AllowMouseWheel", BindingFlags.Instance | BindingFlags.Public);
            if (prop is null || prop.PropertyType != typeof(bool) || !prop.CanWrite)
            {
                return;
            }

            prop.SetValue(model, false);
        }
        catch
        {
            // Wheel blocking falls back to wwwroot/js/date-edit-no-wheel.js
        }
    }
}
