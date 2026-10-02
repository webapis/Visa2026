using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.SystemModule;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ApplicationPersonRoster;

namespace Visa2026.Module.Controllers;

/// <summary>
/// Employees list delete removes the person record. Case roster links are not aggregated on
/// <see cref="Person"/>, so they must be removed first or required-field rules fire
/// (<c>Person</c> / case link fields must not be empty).
/// </summary>
public sealed class PersonDeleteDetachController : ObjectViewController<ObjectView, Person>
{
    private DeleteObjectsViewController? _deleteController;

    protected override void OnActivated()
    {
        base.OnActivated();
        _deleteController = Frame.GetController<DeleteObjectsViewController>();
        if (_deleteController != null)
            _deleteController.Deleting += OnDeleting;
    }

    protected override void OnDeactivated()
    {
        if (_deleteController != null)
            _deleteController.Deleting -= OnDeleting;
        base.OnDeactivated();
    }

    private void OnDeleting(object sender, DeletingEventArgs e)
    {
        foreach (var person in e.Objects.OfType<Person>().ToList())
            ApplicationProfileInstancePersonService.DetachForPersonDelete(ObjectSpace, person);
    }
}