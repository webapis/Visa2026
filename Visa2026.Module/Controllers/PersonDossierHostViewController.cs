using DevExpress.ExpressApp;
using System;
using Visa2026.Module.BusinessObjects.PersonDossier;
using Visa2026.Module.Services.PersonDossier;

namespace Visa2026.Module.Controllers;

/// <summary>
/// Restores <see cref="PersonDossierHost.PersonId"/> when Blazor recreates the non-persistent host.
/// Typed <c>ObjectViewController&lt;DetailView, PersonDossierHost&gt;</c> does not activate for this view.
/// </summary>
public sealed class PersonDossierHostViewController : ViewController<DetailView>
{
    public PersonDossierHostViewController() =>
        TargetViewId = PersonDossierViewIds.DetailView;

    protected override void OnActivated()
    {
        base.OnActivated();
        EnsureHostPersonId();
    }

    protected override void OnViewControlsCreated()
    {
        base.OnViewControlsCreated();
        EnsureHostPersonId();
    }

    private void EnsureHostPersonId()
    {
        var personId = PersonDossierPendingOpenGate.Get(Application);
        if (personId == Guid.Empty)
            return;

        PersonDossierHost host;
        if (View.CurrentObject is PersonDossierHost current)
        {
            host = current;
        }
        else
        {
            host = ObjectSpace.CreateObject<PersonDossierHost>();
            View.CurrentObject = host;
        }

        if (host.PersonId == Guid.Empty)
            host.PersonId = personId;
    }
}
