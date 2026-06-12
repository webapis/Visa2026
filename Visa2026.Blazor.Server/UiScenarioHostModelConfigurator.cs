using System.Reflection;
using DevExpress.ExpressApp;

namespace Visa2026.Blazor.Server;

internal static class UiScenarioHostModelConfigurator
{
    internal static void Apply(XafApplication application)
    {
        if (!EasyTestHostMode.IsEnabled)
        {
            return;
        }

        object options = application.Model.Options;
        PropertyInfo? property = options.GetType().GetProperty("RestoreTabbedMdiLayout");
        property?.SetValue(options, false);
    }
}
