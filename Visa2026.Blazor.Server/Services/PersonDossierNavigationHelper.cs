using DevExpress.ExpressApp;
using Visa2026.Module;
using Visa2026.Module.Services.PersonDossier;

namespace Visa2026.Blazor.Server.Services;

/// <summary>
/// Opens the person dossier DetailView from JS / ListView row clicks.
/// </summary>
public sealed class PersonDossierNavigationHelper
{
    private readonly XafApplicationHolder _appHolder;

    public PersonDossierNavigationHelper(XafApplicationHolder appHolder) =>
        _appHolder = appHolder;

    public void OpenDossier(Guid personId, Frame? sourceFrame = null)
    {
        if (personId == Guid.Empty)
            return;

        var application = _appHolder.Application;
        if (application == null)
            return;

        PersonDossierPendingOpenGate.Set(application, personId);

        var detailView = PersonDossierOpenHelper.CreateDossierView(application, personId);
        if (detailView == null)
            return;

        var frame = sourceFrame ?? PersonDossierNavigationContext.SourceFrameValue ?? application.MainWindow;
        if (frame == null)
            return;

        // NewWindow keeps the source ListView tab (Employees / Family / Temporary visitors).
        application.ShowViewStrategy.ShowView(
            new ShowViewParameters(detailView) { TargetWindow = TargetWindow.NewWindow },
            new ShowViewSource(frame, null));
    }
}
