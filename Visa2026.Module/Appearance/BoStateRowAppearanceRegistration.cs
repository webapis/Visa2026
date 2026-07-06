using DevExpress.ExpressApp.DC;

namespace Visa2026.Module.Appearance;

/// <summary>
/// Registers ListView row <see cref="AppearanceAttribute"/> rules for <see cref="IBoListRowState"/> types.
/// <see cref="Application"/> ListView row colors are applied in Blazor via
/// <c>ApplicationProgressRowAppearanceController</c> + <c>site.css</c> (not XAF Appearance — too slow on large virtualized grids).
/// </summary>
internal static class BoStateRowAppearanceRegistration
{
    public static void Register(ITypesInfo typesInfo)
    {
        // Application row colors: Blazor CustomizeElement + CSS only (see ApplicationProgressRowAppearanceController).
    }
}
