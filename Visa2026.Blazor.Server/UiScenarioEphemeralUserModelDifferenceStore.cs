using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Model.Core;

namespace Visa2026.Blazor.Server;

/// <summary>
/// Does not load or persist per-user model differences (TabbedMDI layout) during UI scenario runs.
/// </summary>
internal sealed class UiScenarioEphemeralUserModelDifferenceStore : ModelDifferenceStore
{
    public override string Name => "UiScenarioEphemeral";

    public override void Load(ModelApplicationBase model)
    {
    }

    public override void SaveDifference(ModelApplicationBase model)
    {
    }
}
